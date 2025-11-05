using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Networking.Types;

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
        float currentTime = (float)SystemAPI.Time.ElapsedTime;
        var transformFromEntity = GetComponentDataFromEntity<LocalTransform>(true);
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        var defenseFromEntity = GetComponentDataFromEntity<DefenseComponent>(true);
        var animationFromEntity = GetComponentDataFromEntity<AnimationComponent>(true);
        var attackComponentFromEntity = GetComponentDataFromEntity<AttackComponent>(true);
        var combatStateDataFromEntity = GetComponentDataFromEntity<CombatState>(true);

        Dependency = Entities
            .WithName("AttackResolutionJob")
            .WithReadOnly(transformFromEntity)
            .WithReadOnly(defenseFromEntity)
            .WithReadOnly(animationFromEntity)
            .WithReadOnly(attackComponentFromEntity)
            .WithReadOnly(combatStateDataFromEntity)
            .WithAll<AttackEventComponent>()
            .ForEach((Entity entity, int entityInQueryIndex,
                    ref FormationComponent formation,
                    //ref AttackComponent attack,
                    in CombatState combatState,
                     in AttackEventComponent attackEvent,
                     in LocalTransform transform
                     ,in AnimationComponent animationComponent
                     ) =>
            {
                // Check if target still exists and is in range
                if (transformFromEntity.HasComponent(attackEvent.TargetEntity))
                {
                    //settting formatoin status to engagnge so unit can leave formatoin breifly
                    formation.Status = FormationStatus.Engaged;



                    float3 targetPos = transformFromEntity[attackEvent.TargetEntity].Position;
                    //bool isTargetDefending = attackComponentFromEntity[attackEvent.TargetEntity].isDefending;
                    bool isTargetDefending = combatStateDataFromEntity[attackEvent.TargetEntity].CurrentState == CombatState.State.Defending;
                    var attack = attackComponentFromEntity[entity];
                    if (ShouldAttackLand(attack.Range, 
                        //animationComponent.Direction
                        attackEvent.AttackerDirection
                        , attackEvent, transform.Position, targetPos,
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
                        }
                        else if (isTargetDefending && IsDefendingInHemicircleDirection(
                            attackEvent.AttackerDirection
                            , defenderAnimation.Direction))
                        {
                            ecb.AddBuffer<DefendEventBuffer>(entityInQueryIndex, attackEvent.TargetEntity);
                            ecb.AppendToBuffer(entityInQueryIndex, attackEvent.TargetEntity, new DefendEventBuffer
                            {
                                //TODO: add force to apply physics later on???
                            });

                        }
                        else
                        {
                            ecb.AddBuffer<AttackEventBuffer>(entityInQueryIndex, attackEvent.TargetEntity);
                            ecb.AppendToBuffer(entityInQueryIndex, attackEvent.TargetEntity, new AttackEventBuffer
                            {
                                Attacker = attackEvent.SourceEntity,
                                Damage = attackEvent.Damage,
                                DamageType = 0
                            });
                        }
                    }
                    else
                    {
                        //Debug.Log("attack event buffer not added");
                    }
                }
                ecb.RemoveComponent<AttackEventComponent>(entityInQueryIndex, entity);
            }).WithBurst().ScheduleParallel(Dependency);
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
        return true;
    }

    private static bool AreDirectionsOpposite(EntitySpawner.Direction attacker, EntitySpawner.Direction defender)
    {
        //DefenseSystem.LogDirection(defender, attacker);

        return (attacker == EntitySpawner.Direction.Left && defender == EntitySpawner.Direction.Right) ||
               (attacker == EntitySpawner.Direction.Right && defender == EntitySpawner.Direction.Left) ||
               (attacker == EntitySpawner.Direction.Up && defender == EntitySpawner.Direction.Down) ||
               (attacker == EntitySpawner.Direction.Down && defender == EntitySpawner.Direction.Up);
    }

    private static bool IsDefendingInHemicircleDirection(EntitySpawner.Direction attacker, EntitySpawner.Direction defender)
    {
        //DefenseSystem.LogDirection(defender, attacker);

        return (attacker == EntitySpawner.Direction.Left && (defender == EntitySpawner.Direction.Right || defender == EntitySpawner.Direction.Up || defender == EntitySpawner.Direction.Down)) ||
               (attacker == EntitySpawner.Direction.Right && (defender == EntitySpawner.Direction.Left || defender == EntitySpawner.Direction.Up || defender == EntitySpawner.Direction.Down)) ||
               (attacker == EntitySpawner.Direction.Up && (defender == EntitySpawner.Direction.Down || defender == EntitySpawner.Direction.Left || defender == EntitySpawner.Direction.Right)) ||
               (attacker == EntitySpawner.Direction.Down && (defender == EntitySpawner.Direction.Up || defender == EntitySpawner.Direction.Left || defender == EntitySpawner.Direction.Right));
    }
}
public struct DefendEventBuffer : IBufferElementData
{
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
    ref CombatState combatState,
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

    private static bool AreDirectionsOpposite(EntitySpawner.Direction dir1, EntitySpawner.Direction dir2)
    {
        return (dir1 == EntitySpawner.Direction.Left && dir2 == EntitySpawner.Direction.Right) ||
               (dir1 == EntitySpawner.Direction.Right && dir2 == EntitySpawner.Direction.Left) ||
               (dir1 == EntitySpawner.Direction.Up && dir2 == EntitySpawner.Direction.Down) ||
               (dir1 == EntitySpawner.Direction.Down && dir2 == EntitySpawner.Direction.Up);
    }

    public static void LogDirection(string source, EntitySpawner.Direction direction)
    {
        switch (direction)
        {
            case EntitySpawner.Direction.Up:
                Debug.Log($"{source} Up");
                break;
            case EntitySpawner.Direction.Down:
                Debug.Log($"{source} Down");
                break;
            case EntitySpawner.Direction.Left:
                Debug.Log($"{source} Left");
                break;
            case EntitySpawner.Direction.Right:
                Debug.Log($"{source} Right");
                break;
            default:
                break;
        }
    }

    internal static void LogDirection(EntitySpawner.Direction defender, EntitySpawner.Direction attacker)
    {
        Debug.Log($"defender: {GetDirection(defender)}| attacker:{GetDirection(attacker)}");
    }

    private static string GetDirection(EntitySpawner.Direction direction)
    {
        switch (direction)
        {
            case EntitySpawner.Direction.Up:
                return "Up";
            case EntitySpawner.Direction.Down:
                return "Down";
            case EntitySpawner.Direction.Left:
                return "Left";
            case EntitySpawner.Direction.Right:
            default:
                return "Right";

        }
    }
}