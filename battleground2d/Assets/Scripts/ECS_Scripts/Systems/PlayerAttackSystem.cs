using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CollisionQuadrantSystem))] // Run AFTER quadrant system updates
[UpdateBefore(typeof(CollisionDetectionSystem))]
public partial class PlayerAttackSystem : SystemBase
{
    private EntityQuery _playerQuery;
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    EntitySpawner entitySpawner;
    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        _playerQuery = GetEntityQuery(
            ComponentType.ReadWrite<AttackComponent>(),
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadOnly<AnimationComponent>(),
            ComponentType.ReadOnly<PlayerInputComponent>()
        );


    }

    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;



        // Wait for CollisionQuadrantSystem to complete its update
        var quadrantSystem = World.GetExistingSystem<CollisionQuadrantSystem>();
        quadrantSystem.Update();

        float currentTime = (float)Time.ElapsedTime;
        var translationFromEntity = GetComponentDataFromEntity<Translation>(true);
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        // Use the quadrant system's dependency to ensure data is ready
        //Dependency = JobHandle.CombineDependencies(Dependency, quadrantSystem.World.);


        if (entitySpawner == null)
        {
            entitySpawner = GameObject.Find("GameManager").GetComponent<EntitySpawner>();
        }

        // Get the value from GameObject land BEFORE scheduling the job
        bool DrawDebugLines = entitySpawner != null ? entitySpawner.DrawDebugLines : false;


        Dependency = new PlayerAttackJob
        {
            CurrentTime = currentTime,
            ECB = ecb,
            TranslationFromEntity = translationFromEntity,
            AnimationTypeHandle = GetComponentTypeHandle<AnimationComponent>(true),
            AttackTypeHandle = GetComponentTypeHandle<AttackComponent>(false),
            TranslationTypeHandle = GetComponentTypeHandle<Translation>(true),
            EntityTypeHandle = GetEntityTypeHandle(),
            QuadrantMap = CollisionQuadrantSystem.collisionQuadrantMap,
            drawDebugLines = DrawDebugLines
        }.ScheduleParallel(_playerQuery, Dependency);

        _ecbSystem.AddJobHandleForProducer(Dependency);

    }

    [BurstCompile]
    private struct PlayerAttackJob : IJobChunk
    {
        public float CurrentTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;
        public bool drawDebugLines;

        [ReadOnly] public ComponentTypeHandle<AnimationComponent> AnimationTypeHandle;
        public ComponentTypeHandle<AttackComponent> AttackTypeHandle;
        [ReadOnly] public ComponentTypeHandle<Translation> TranslationTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        [ReadOnly] public NativeMultiHashMap<int, CollisionQuadrantData> QuadrantMap;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var animations = chunk.GetNativeArray(AnimationTypeHandle);
            var attacks = chunk.GetNativeArray(AttackTypeHandle);
            var translations = chunk.GetNativeArray(TranslationTypeHandle);
            var entities = chunk.GetNativeArray(EntityTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var animation = animations[i];
                var attack = attacks[i];
                var translation = translations[i];
                var entity = entities[i];

                // Only process if attacking and attack rate ready
                if (!attack.isAttacking || CurrentTime - attack.LastAttackTime < 1f / attack.AttackRate)
                    continue;


                float2 movingPosDownToSpriteBase = new float2(translation.Value.x, translation.Value.y - .125f);

                // Get attack cone based on direction
                float2 attackDirection = GetDirection(animation.Direction, movingPosDownToSpriteBase);

                // Find targets in attack cone using quadrant system
                FindTargetsInRectangle(entity, movingPosDownToSpriteBase, attackDirection, attack.Range, chunkIndex, ECB, animation, drawDebugLines);

                attack.LastAttackTime = CurrentTime;
                attacks[i] = attack;
            }
        }

        private float2 GetDirection(EntitySpawner.Direction direction, float2 position)
        {
            // Return cone direction vector based on facing
            switch (direction)
            {
                case EntitySpawner.Direction.Up:
                    return new float2(0, 1);
                case EntitySpawner.Direction.Down:
                    return new float2(0, -1);
                case EntitySpawner.Direction.Left:
                    return new float2(-1, 0);
                case EntitySpawner.Direction.Right:
                default:
                    return new float2(1, 0);
            }
        }

        private void FindTargetsInRectangle(Entity attacker, float2 attackerPos, float2 attackDirection,
                                  float range, int chunkIndex, EntityCommandBuffer.ParallelWriter ecb, AnimationComponent animation, bool debug)
        {
            float unitWidth = 0.125f;
            float targetRadius = 0.125f;

            if (debug)
            {
                // Debug draw the attack rectangle
                float2 perpendicular = new float2(attackDirection.y, -attackDirection.x);
                float2 startLeft = attackerPos - perpendicular * unitWidth;
                float2 startRight = attackerPos + perpendicular * unitWidth;
                float2 endLeft = startLeft + attackDirection * range;
                float2 endRight = startRight + attackDirection * range;

                // Draw attack rectangle
                Debug.DrawLine(new Vector3(startLeft.x, startLeft.y, 0), new Vector3(endLeft.x, endLeft.y, 0), Color.red, 1f);
                Debug.DrawLine(new Vector3(startRight.x, startRight.y, 0), new Vector3(endRight.x, endRight.y, 0), Color.red, 1f);
                Debug.DrawLine(new Vector3(startLeft.x, startLeft.y, 0), new Vector3(startRight.x, startRight.y, 0), Color.red, 1f);
                Debug.DrawLine(new Vector3(endLeft.x, endLeft.y, 0), new Vector3(endRight.x, endRight.y, 0), Color.red, 1f);

            }
            // Get relevant quadrant offsets based on attack direction
            var quadrantOffsets = GetRelevantQuadrantOffsets(animation.Direction);

            int baseX = (int)math.floor(attackerPos.x / CollisionQuadrantSystem.quadrantCellSize);
            int baseY = (int)math.floor(attackerPos.y / CollisionQuadrantSystem.quadrantCellSize);

            int hitCount = 0;
            NativeHashSet<Entity> alreadyHit = new NativeHashSet<Entity>(10, Allocator.Temp);

            // Check only relevant quadrants based on attack direction
            for (int i = 0; i < quadrantOffsets.Length; i++)
            {
                int2 offset = quadrantOffsets[i];
                int x = baseX + offset.x;
                int y = baseY + offset.y;
                int hash = x + y * CollisionQuadrantSystem.quadrantYMultiplier;

                if (!QuadrantMap.TryGetFirstValue(hash, out var targetData, out var iterator))
                    continue;

                do
                {
                    Entity targetEntity = targetData.entity;

                    // Skip if already hit this frame or invalid
                    if (alreadyHit.Contains(targetEntity) || targetEntity == attacker || !TranslationFromEntity.HasComponent(targetEntity))
                        continue;

                    // Skip same unit types
                    if (animation.UnitType == targetData.unitType)
                        continue;

                    float2 targetPos = targetData.position.xy;

                    // Early distance check - skip if too far for performance
                    float distSq = math.distancesq(attackerPos, targetPos);
                    float maxRangeSq = (range + targetRadius + unitWidth) * (range + targetRadius + unitWidth);
                    if (distSq > maxRangeSq)
                        continue;

                    // Check if target circle intersects attack rectangle
                    bool hits = IsInAttackRectangle(attackerPos, attackDirection, targetPos, range, unitWidth, targetRadius);

                    // Debug draw target circle with hit status (only for nearby targets)
                    if (distSq < (range * 2) * (range * 2)) // Only draw if reasonably close
                    {
                        DrawDebugCircle(targetPos, targetRadius, hits ? Color.green : Color.white, debug);
                        if (debug)
                        {
                            Debug.DrawLine(new Vector3(attackerPos.x, attackerPos.y, 0),
                                                new Vector3(targetPos.x, targetPos.y, 0),
                                                hits ? Color.green : Color.blue, 1f);
                        }
                    }

                    if (hits)
                    {
                        hitCount++;
                        alreadyHit.Add(targetEntity);
                        ecb.AddComponent(chunkIndex, targetEntity, new AttackEventComponent
                        {
                            TargetEntity = targetEntity,
                            Damage = 20f,
                            SourceEntity = attacker
                        });
                    }
                }
                while (QuadrantMap.TryGetNextValue(out targetData, ref iterator));
            }

            quadrantOffsets.Dispose();

            alreadyHit.Dispose();

            // Debug log hit count
            if (hitCount > 0)
            {
                Debug.Log($"Attack hit {hitCount} targets");
            }
        }


        private NativeArray<int2> GetRelevantQuadrantOffsets(EntitySpawner.Direction direction)
        {
            // Create NativeArray with Temp allocator (Burst-compatible)
            var offsets = new NativeArray<int2>(6, Allocator.Temp);

            switch (direction)
            {
                case EntitySpawner.Direction.Right:
                    offsets[0] = new int2(0, 0);   // Current quadrant
                    offsets[1] = new int2(1, 0);   // Right
                    offsets[2] = new int2(1, 1);   // Right + Up
                    offsets[3] = new int2(1, -1);  // Right + Down
                    offsets[4] = new int2(0, 1);   // Up (for width)
                    offsets[5] = new int2(0, -1);  // Down (for width)
                    break;

                case EntitySpawner.Direction.Left:
                    offsets[0] = new int2(0, 0);   // Current quadrant
                    offsets[1] = new int2(-1, 0);  // Left
                    offsets[2] = new int2(-1, 1);  // Left + Up
                    offsets[3] = new int2(-1, -1); // Left + Down
                    offsets[4] = new int2(0, 1);   // Up (for width)
                    offsets[5] = new int2(0, -1);  // Down (for width)
                    break;

                case EntitySpawner.Direction.Up:
                    offsets[0] = new int2(0, 0);   // Current quadrant
                    offsets[1] = new int2(0, 1);   // Up
                    offsets[2] = new int2(1, 1);   // Up + Right
                    offsets[3] = new int2(-1, 1);  // Up + Left
                    offsets[4] = new int2(1, 0);   // Right (for width)
                    offsets[5] = new int2(-1, 0);  // Left (for width)
                    break;

                case EntitySpawner.Direction.Down:
                    offsets[0] = new int2(0, 0);   // Current quadrant
                    offsets[1] = new int2(0, -1);  // Down
                    offsets[2] = new int2(1, -1);  // Down + Right
                    offsets[3] = new int2(-1, -1); // Down + Left
                    offsets[4] = new int2(1, 0);   // Right (for width)
                    offsets[5] = new int2(-1, 0);  // Left (for width)
                    break;

                default:
                    // Fallback - resize for 3x3
                    offsets.Dispose();
                    offsets = new NativeArray<int2>(9, Allocator.Temp);
                    offsets[0] = new int2(-1, -1); offsets[1] = new int2(0, -1); offsets[2] = new int2(1, -1);
                    offsets[3] = new int2(-1, 0); offsets[4] = new int2(0, 0); offsets[5] = new int2(1, 0);
                    offsets[6] = new int2(-1, 1); offsets[7] = new int2(0, 1); offsets[8] = new int2(1, 1);
                    break;
            }

            return offsets;
        }

        //private NativeArray<int2> GetRelevantQuadrantOffsets(EntitySpawner.Direction direction)
        //{
        //    // Return only the quadrants that could possibly be in the attack direction
        //    // This reduces from 9 quadrants to typically 3-5 quadrants

        //    switch (direction)
        //    {
        //        case EntitySpawner.Direction.Right:
        //            return new NativeArray<int2>(new int2[]
        //            {
        //        new int2(0, 0),   // Current quadrant
        //        new int2(1, 0),   // Right
        //        new int2(1, 1),   // Right + Up
        //        new int2(1, -1),  // Right + Down
        //        new int2(0, 1),   // Up (for width)
        //        new int2(0, -1)   // Down (for width)
        //            }, Allocator.Temp);

        //        case EntitySpawner.Direction.Left:
        //            return new NativeArray<int2>(new int2[]
        //            {
        //        new int2(0, 0),   // Current quadrant
        //        new int2(-1, 0),  // Left
        //        new int2(-1, 1),  // Left + Up
        //        new int2(-1, -1), // Left + Down
        //        new int2(0, 1),   // Up (for width)
        //        new int2(0, -1)   // Down (for width)
        //            }, Allocator.Temp);

        //        case EntitySpawner.Direction.Up:
        //            return new NativeArray<int2>(new int2[]
        //            {
        //        new int2(0, 0),   // Current quadrant
        //        new int2(0, 1),   // Up
        //        new int2(1, 1),   // Up + Right
        //        new int2(-1, 1),  // Up + Left
        //        new int2(1, 0),   // Right (for width)
        //        new int2(-1, 0)   // Left (for width)
        //            }, Allocator.Temp);

        //        case EntitySpawner.Direction.Down:
        //            return new NativeArray<int2>(new int2[]
        //            {
        //        new int2(0, 0),   // Current quadrant
        //        new int2(0, -1),  // Down
        //        new int2(1, -1),  // Down + Right
        //        new int2(-1, -1), // Down + Left
        //        new int2(1, 0),   // Right (for width)
        //        new int2(-1, 0)   // Left (for width)
        //            }, Allocator.Temp);

        //        default:
        //            // Fallback to 3x3 area if direction is unknown
        //            return new NativeArray<int2>(new int2[]
        //            {
        //        new int2(-1, -1), new int2(0, -1), new int2(1, -1),
        //        new int2(-1, 0),  new int2(0, 0),  new int2(1, 0),
        //        new int2(-1, 1),  new int2(0, 1),  new int2(1, 1)
        //            }, Allocator.Temp);
        //    }
        //}


        private bool IsInAttackRectangle(float2 attackerPos, float2 attackDirection, float2 targetPos,
                                       float range, float unitWidth, float targetRadius)
        {
            float2 toTarget = targetPos - attackerPos;

            // Project target position onto attack direction
            float forwardDistance = math.dot(toTarget, attackDirection);

            // Calculate perpendicular distance
            float2 perpendicular = new float2(attackDirection.y, -attackDirection.x);
            float sideDistance = math.abs(math.dot(toTarget, perpendicular));

            // Check if target circle intersects with the attack rectangle (expanded by target radius)

            // Case 1: Main rectangle body (expanded by target radius)
            if (forwardDistance >= -targetRadius && forwardDistance <= range + targetRadius &&
                sideDistance <= unitWidth + targetRadius)
            {
                // We're in the expanded bounding box, now check specific regions

                // Main rectangle area
                if (forwardDistance >= 0 && forwardDistance <= range && sideDistance <= unitWidth)
                    return true;

                // Check the semicircular cap at the start (attacker position)
                if (forwardDistance < 0)
                {
                    float2 startToTarget = targetPos - attackerPos;
                    return math.length(startToTarget) <= targetRadius;
                }

                // Check the semicircular cap at the end (attack tip)
                if (forwardDistance > range)
                {
                    float2 tipPos = attackerPos + attackDirection * range;
                    float2 tipToTarget = targetPos - tipPos;
                    return math.length(tipToTarget) <= targetRadius;
                }

                // Check the sides (expanded by target radius)
                return true;
            }

            return false;
        }

        private void DrawDebugCircle(float2 center, float radius, Color color, bool debug)
        {
            int segments = 12;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (float)i / segments * math.PI * 2;
                float angle2 = (float)(i + 1) / segments * math.PI * 2;

                float2 p1 = center + new float2(math.cos(angle1), math.sin(angle1)) * radius;
                float2 p2 = center + new float2(math.cos(angle2), math.sin(angle2)) * radius;

                if (debug)
                {
                    Debug.DrawLine(new Vector3(p1.x, p1.y, 0), new Vector3(p2.x, p2.y, 0), color, 1f);
                }
            }
        }
    }
}

