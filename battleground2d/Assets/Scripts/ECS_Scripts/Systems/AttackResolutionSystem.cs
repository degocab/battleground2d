using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(ApplyDamageSystem))]
[UpdateAfter(typeof(CombatSystem))]
public partial class AttackResolutionSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    private EntityQuery _attackEventQuery;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        _attackEventQuery = GetEntityQuery(ComponentType.ReadWrite<AttackEventComponent>());
    }

    protected override void OnUpdate()
    {
        float currentTime = (float)Time.ElapsedTime;
        var translationFromEntity = GetComponentDataFromEntity<Translation>(true);
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        var defenseFromEntity = GetComponentDataFromEntity<DefenseComponent>(true);
        var animationFromEntity = GetComponentDataFromEntity<AnimationComponent>(true);
        var attackComponentFromEntity = GetComponentDataFromEntity<AttackComponent>(true);
        var combatStateDataFromEntity = GetComponentDataFromEntity<CombatState>(true);

        Dependency = Entities
            .WithName("AttackResolutionJob")
            .WithReadOnly(translationFromEntity)
            .WithReadOnly(defenseFromEntity)
            .WithReadOnly(animationFromEntity)
            .WithReadOnly(attackComponentFromEntity)
            .WithReadOnly(combatStateDataFromEntity)
            .WithAll<AttackEventComponent>()
            .ForEach((Entity entity, int entityInQueryIndex,
                    //ref AttackComponent attack,
                    in CombatState combatState,
                     in AttackEventComponent attackEvent,
                     in Translation translation
                     ,in AnimationComponent animationComponent
                     ) =>
            {
                // Check if target still exists and is in range
                if (translationFromEntity.HasComponent(attackEvent.TargetEntity))
                {
                    float3 targetPos = translationFromEntity[attackEvent.TargetEntity].Value;
                    //bool isTargetDefending = attackComponentFromEntity[attackEvent.TargetEntity].isDefending;
                    bool isTargetDefending = combatStateDataFromEntity[attackEvent.TargetEntity].CurrentState == CombatState.State.Defending;
                    var attack = attackComponentFromEntity[entity];
                    if (ShouldAttackLand(attack.Range, animationComponent.Direction, attackEvent, translation.Value, targetPos,
                               currentTime, defenseFromEntity, animationFromEntity))
                    {
                        //combatState.CurrentState = CombatState.State.TakingDamage;
                        // Buffer doesn't exist, add it first then append

                        //Debug.Log($"target: {animationFromEntity[attackEvent.TargetEntity].UnitType.ToString()} is defending:{isTargetDefending}");
                        //Debug.Log($"attacker: {animationComponent.UnitType.ToString()} is defending:{isTargetDefending}");


                        var defenderAnimation = animationFromEntity[attackEvent.TargetEntity];

                        if (!isTargetDefending)
                        {
                            ecb.AddBuffer<AttackEventBuffer>(entityInQueryIndex, attackEvent.TargetEntity);
                            ecb.AppendToBuffer(entityInQueryIndex, attackEvent.TargetEntity, new AttackEventBuffer
                            {
                                Attacker = attackEvent.SourceEntity,
                                Damage = attackEvent.Damage,
                                DamageType = 0
                            });
                        }else if (isTargetDefending && !AreDirectionsOpposite(animationComponent.Direction, defenderAnimation.Direction))
                        {
                            ecb.AddBuffer<AttackEventBuffer>(entityInQueryIndex, attackEvent.TargetEntity);
                            ecb.AppendToBuffer(entityInQueryIndex, attackEvent.TargetEntity, new AttackEventBuffer
                            {
                                Attacker = attackEvent.SourceEntity,
                                Damage = attackEvent.Damage,
                                DamageType = 0
                            });
                        }
                        else
                        {
                            //Debug.Log("blocked attack");
                            ecb.AddBuffer<DefendEventBuffer>(entityInQueryIndex, attackEvent.TargetEntity);
                            ecb.AppendToBuffer(entityInQueryIndex, attackEvent.TargetEntity, new DefendEventBuffer
                            {
                                //TODO: add force to apply physics later on???
                            });

                        }

                    }
                    else
                    {
                        //Debug.Log("attack event buffer not added");

                    }
                }
                ecb.RemoveComponent<AttackEventComponent>(entityInQueryIndex, entity);
            }).ScheduleParallel(Dependency);
        _ecbSystem.AddJobHandleForProducer(Dependency);
    }

    private static bool ShouldAttackLand(float range, EntitySpawner.Direction attackerFacing, AttackEventComponent attackEvent, float3 attackerPos,
                                float3 defenderPos, float currentTime,
                                ComponentDataFromEntity<DefenseComponent> defenseFromEntity,
                                ComponentDataFromEntity<AnimationComponent> animationFromEntity)
    {
        //// 1. Check if within strike timing window
        //float timeSinceAttack = currentTime - attackEvent.AttackTime;
        //if (timeSinceAttack < attackEvent.WindUpTime ||
        //    timeSinceAttack > attackEvent.WindUpTime + attackEvent.StrikeTime)
        //    return false;

        // 2. Check range
        if (!CombatUtils.IsTargetInRange(attackerPos, defenderPos, range/*attackEvent.Range*/))
            return false;

        // 3. Check facing and defense
        var defenderAnimation = animationFromEntity[attackEvent.TargetEntity];
        //bool isDefending = defenseFromEntity.HasComponent(attackEvent.TargetEntity) &&
        //                  defenseFromEntity[attackEvent.TargetEntity].IsBlocking;

        return CanHitBasedOnFacingSimple(attackerPos.xy, defenderPos.xy,
                                 attackerFacing/*attackEvent.AttackerFacing*/, defenderAnimation.Direction//,
                                 /*isDefending*/);
    }
    private static bool CanHitBasedOnFacingSimple(float2 attackerPos, float2 defenderPos, EntitySpawner.Direction attackerFacing,
                                         EntitySpawner.Direction defenderFacing
                                            /*,bool isDefending*/)
    {
        // Simple directional checks for 4-way system
        bool areFacingEachOther = AreDirectionsOpposite(attackerFacing, defenderFacing);
        if (!areFacingEachOther)
        {
            // Attacker might be hitting from side/back
            return true; // Always allow hits from non-frontal angles
        }

        //// If facing each other and defender is blocking
        //if (
        //    isDefending && areFacingEachOther)
        //{
        //    return false; // Perfect block when facing each other
        //}

        return true;
    }

    private static bool AreDirectionsOpposite(EntitySpawner.Direction dir1, EntitySpawner.Direction dir2)
    {
        return (dir1 == EntitySpawner.Direction.Left && dir2 == EntitySpawner.Direction.Right) ||
               (dir1 == EntitySpawner.Direction.Right && dir2 == EntitySpawner.Direction.Left) ||
               (dir1 == EntitySpawner.Direction.Up && dir2 == EntitySpawner.Direction.Down) ||
               (dir1 == EntitySpawner.Direction.Down && dir2 == EntitySpawner.Direction.Up);
    }
}
public struct DefendEventBuffer : IBufferElementData
{
    //public Entity Attacker;
    //public float Damage;
    //public int DamageType; // use int for enum-like simplicity
}

[UpdateAfter(typeof(AttackResolutionSystem))]
[UpdateBefore(typeof(ApplyDamageSystem))]
public partial class DefenseSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var defenseFromEntity = GetComponentDataFromEntity<DefenseComponent>(true);
        var animationFromEntity = GetComponentDataFromEntity<AnimationComponent>(true);
        var hasTargetFromEntity = GetComponentDataFromEntity<HasTarget>(true);
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();



        Entities
    .WithName("DefenseResolutionJob")
    .WithAll<DefendEventBuffer>()
    .WithReadOnly(hasTargetFromEntity)
    .ForEach((Entity entity, int entityInQueryIndex,
    ref AttackComponent attackComponent,
    ref DefenseComponent defense,
    ref CombatState  combatState,
             ref DynamicBuffer<DefendEventBuffer> defends) =>
    {
        if (defends.Length == 0)
        {
            //attackComponent.isTakingDamage = false;
                //Debug.Log("no defends in buffer"); 
            return;
        }
        // Check if entity has HasTarget component - use the existing hasTargetFromEntity
        bool hasTargetComponent = hasTargetFromEntity.HasComponent(entity);

        if (!hasTargetComponent)
        {
            ecb.AddComponent(entityInQueryIndex, entity, new HasTarget
            {
                Type = HasTarget.TargetType.Entity,
                TargetEntity = Entity.Null,
                TargetPosition = float2.zero
            });
        }
        for (int i = 0; i < defends.Length; i++)
        {
            //Debug.Log("Attack blocked by AI!!!!!!");
            //reset block trigger?
            //defense.IsBlocking = true;
            //defense.BlockDuration = .2f;
            //combatState.CurrentState = CombatState.State.Blocking;
        }
        //defense.IsBlocking = true;
        defense.BlockDuration = .2f;
        combatState.CurrentState = CombatState.State.Blocking;
        //TODO: set to true if this doesnt trigger animation?

        defends.Clear(); // Clear buffer for reuse

    }).WithBurst().ScheduleParallel();

    }

    //private static bool CanBlockAttack(Entity defender, Entity attacker,
    //                          ComponentDataFromEntity<DefenseComponent> defenseFromEntity,
    //                          ComponentDataFromEntity<AnimationComponent> animationFromEntity, AttackComponent attackComponent)
    //{
    //    if (!defenseFromEntity.HasComponent(defender) || !animationFromEntity.HasComponent(defender))
    //        return false;

    //    var defense = defenseFromEntity[defender];
    //    var defenderAnim = animationFromEntity[defender];
    //    var attackerAnim = animationFromEntity[attacker];

    //    // Only block if actively blocking and facing the right direction
    //    return attackComponent.isDefending &&
    //           AreDirectionsOpposite(defenderAnim.Direction, attackerAnim.Direction);
    //}

    private static bool AreDirectionsOpposite(EntitySpawner.Direction dir1, EntitySpawner.Direction dir2)
    {
        return (dir1 == EntitySpawner.Direction.Left && dir2 == EntitySpawner.Direction.Right) ||
               (dir1 == EntitySpawner.Direction.Right && dir2 == EntitySpawner.Direction.Left) ||
               (dir1 == EntitySpawner.Direction.Up && dir2 == EntitySpawner.Direction.Down) ||
               (dir1 == EntitySpawner.Direction.Down && dir2 == EntitySpawner.Direction.Up);
    }
}