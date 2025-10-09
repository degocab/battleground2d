using System.Linq;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AttackResolutionSystem))]
[UpdateBefore(typeof(DeathSystem))]
[BurstCompile]
public partial class ApplyDamageSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithName("ApplyDamage")
            //.WithAll<DamageComponent>()
            .WithAll<AttackEventBuffer>()
            .ForEach((Entity entity, int entityInQueryIndex,
            ref AttackComponent attackComponent,
            ref AttackCooldownComponent cooldown,
                     ref HealthComponent health,
                     ref CombatState combatState,
                     ref DynamicBuffer<AttackEventBuffer> attacks) =>
            {
                //health.Health -= damage.Value;

                //attackComponent.isTakingDamage = true;
                //ecb.RemoveComponent<DamageComponent>(entityInQueryIndex, entity);

                //if (health.Health <= 0)
                //{
                //    health.isDying = true;
                //    health.timeRemaining = health.deathAnimationDuration;
                //}

                if (attacks.Length == 0)
                {
                    //attackComponent.isTakingDamage = false;
                    //Debug.Log("no attacks in buffer");
                    return;
                }

                float totalDamage = 0;

                for (int i = 0; i < attacks.Length; i++)
                {
                    totalDamage += attacks[i].Damage;
                }

                health.Health -= totalDamage;
                //TODO: set to true if this doesnt trigger animation?
                attackComponent.isTakingDamage = true;
                //combatState.CurrentState = CombatState.State.TakingDamage;

                cooldown.takingDmgTimeRemaining = cooldown.takeDamageCooldownDuration;

                attacks.Clear(); // Clear buffer for reuse

                if (health.Health <= 0)
                {
                    health.isDying = true;
                    health.timeRemaining = health.deathAnimationDuration;
                }

                ////DEBUG: Add debug output to verify damage is being applied
                //#if UNITY_EDITOR
                //if (health.Health > 100)
                //{
                //    UnityEngine. Debug.Log($"Entity {entity.Index} took {totalDamage} damage. New health: {health.Health}"); 
                //}
                //#endif


            }).ScheduleParallel();

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}