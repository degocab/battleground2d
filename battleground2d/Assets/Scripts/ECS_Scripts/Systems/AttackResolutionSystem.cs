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
        var damageComponentFromEntity = GetComponentDataFromEntity<DamageComponent>(true);
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        var attackEventBufferFromEntity = GetBufferFromEntity<AttackEventBuffer>(false);

        Dependency = Entities
            .WithName("AttackResolutionJob")
            .WithReadOnly(translationFromEntity)
            //.WithReadOnly(damageComponentFromEntity)
            //.WithNativeDisableParallelForRestriction(attackEventBufferFromEntity)
            .WithAll<AttackEventComponent>()
            .ForEach((Entity entity, int entityInQueryIndex,
                    ref AttackComponent attack,
                     in AttackEventComponent attackEvent,
                     in Translation translation
                     ) =>
            {
                // Check if target still exists and is in range
                if (translationFromEntity.HasComponent(attackEvent.TargetEntity))
                {
                    float3 targetPos = translationFromEntity[attackEvent.TargetEntity].Value;
                    //float distance = math.distance(translation.Value, targetPos);

                    //if (distance <= attack.Range)
                    if (CombatUtils.IsTargetInRange(translation.Value, targetPos, attack.Range))
                    {
                        //if (!damageComponentFromEntity.HasComponent(attackEvent.TargetEntity))
                        //{
                        //ecb.AddComponent(entityInQueryIndex, attackEvent.TargetEntity, new DamageComponent
                        //{
                        //    Value = attackEvent.Damage,
                        //    SourceEntity = attackEvent.SourceEntity
                        //});
                        //}
                        //else
                        //{
                        //    // Optional: if DamageComponent exists, you could do something else here,
                        //    // like accumulate damage or skip.
                        //}


                        //ecb.AddComponent(entityInQueryIndex, attackEvent.TargetEntity, new DamageComponent
                        //{
                        //    Value = attackEvent.Damage,
                        //    SourceEntity = attackEvent.SourceEntity
                        //});







                        // Buffer doesn't exist, add it first then append
                        ecb.AddBuffer<AttackEventBuffer>(entityInQueryIndex, attackEvent.TargetEntity);
                        ecb.AppendToBuffer(entityInQueryIndex, attackEvent.TargetEntity, new AttackEventBuffer
                        {
                            Attacker = attackEvent.SourceEntity,
                            Damage = attackEvent.Damage,
                            DamageType = 0
                        });

                    }
                }

                ecb.RemoveComponent<AttackEventComponent>(entityInQueryIndex, entity);

            }).ScheduleParallel(Dependency);

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}
