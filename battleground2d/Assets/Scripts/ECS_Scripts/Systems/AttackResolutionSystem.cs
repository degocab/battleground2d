using System;
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

        Dependency = Entities
            .WithName("AttackResolutionJob")
            .WithReadOnly(translationFromEntity)
            .WithReadOnly(defenseFromEntity)
            .WithReadOnly(animationFromEntity)
            .WithAll<AttackEventComponent>()
            .ForEach((Entity entity, int entityInQueryIndex,
                    ref AttackComponent attack,
                    //ref CombatState combatState,
                     in AttackEventComponent attackEvent,
                     in Translation translation
                     ,in AnimationComponent animationComponent
                     ) =>
            {
                // Check if target still exists and is in range
                if (translationFromEntity.HasComponent(attackEvent.TargetEntity))
                {
                    float3 targetPos = translationFromEntity[attackEvent.TargetEntity].Value;

                    if (ShouldAttackLand(attack.Range, animationComponent.Direction, attackEvent, translation.Value, targetPos,
                               currentTime, defenseFromEntity, animationFromEntity))
                    {
                        //combatState.CurrentState = CombatState.State.TakingDamage;
                        // Buffer doesn't exist, add it first then append

                        Debug.Log("Adding attack event buffer");
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
                        Debug.Log("attack event buffer not added");

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
        bool isDefending = defenseFromEntity.HasComponent(attackEvent.TargetEntity) &&
                          defenseFromEntity[attackEvent.TargetEntity].IsBlocking;

        return CanHitBasedOnFacingSimple(attackerPos.xy, defenderPos.xy,
                                 attackerFacing/*attackEvent.AttackerFacing*/, defenderAnimation.Direction,
                                 isDefending);
    }
    private static bool CanHitBasedOnFacingSimple(float2 attackerPos, float2 defenderPos, EntitySpawner.Direction attackerFacing,
                                         EntitySpawner.Direction defenderFacing,
                                         
                                         bool isDefending)
    {
        // Simple directional checks for 4-way system
        bool areFacingEachOther = AreDirectionsOpposite(attackerFacing, defenderFacing);
        if (!areFacingEachOther)
        {
            // Attacker might be hitting from side/back
            return true; // Always allow hits from non-frontal angles
        }

        // If facing each other and defender is blocking
        if (isDefending && areFacingEachOther)
        {
            return false; // Perfect block when facing each other
        }

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
