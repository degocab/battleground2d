using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

//https://youtu.be/hP4Vu6JbzSo?t=739
// following code monkey quadrant system
// working to grab all units
// next is convert it to jobs
//com.unity.entities@0.16.0-preview.21
//unity 2020.1.1f1


public struct QuadrantEntity : IComponentData
{
    public TypeEnum typeEnum;

    public enum TypeEnum
    {
        Unit,
        Target
    }
}

public struct QuadrantData
{
    public Entity Entity;
    public float2 Position;
    public QuadrantEntity QuadrantEntity;
    public AnimationComponent AnimationComponent;
}

[UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
//[UpdateBefore(typeof(MovementSystem))]
[UpdateBefore(typeof(PlayerControlSystem))]
[UpdateAfter(typeof(GridSystem))]
public class QuadrantSystem : SystemBase
{
    public const int QuadrantYMultiplier = 1000;
    public const int quadrantCellSize = 5;
    private EndSimulationEntityCommandBufferSystem _ecbSystem;
    private EntityQuery _query;

    public static NativeMultiHashMap<int, QuadrantData> QuadrantMultiHashMap;

    protected override void OnCreate()
    {

        _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        _query = GetEntityQuery(
ComponentType.ReadOnly<Translation>(),
ComponentType.ReadOnly<CommandData>(),
ComponentType.Exclude<CommanderComponent>());
        QuadrantMultiHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);


        base.OnCreate();
    }
    protected override void OnDestroy()
    {
        QuadrantMultiHashMap.Dispose();
        base.OnDestroy();
    }

    //convert position to quadrant
    public static int GetPositionHashMapKey(float2 position)
    {
        return (int)(math.floor(position.x / quadrantCellSize) + (QuadrantYMultiplier * math.floor(position.y / quadrantCellSize)));
    }

    private static void DebugDrawQuadrant(float2 position)
    {
        Vector2 lowerLeft = new Vector2(math.floor(position.x / quadrantCellSize) * quadrantCellSize, math.floor(position.y / quadrantCellSize) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector2(+1, +0) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector2(+0, +1) * quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector2(+1, +0) * quadrantCellSize, lowerLeft + new Vector2(+1, +1) * quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector2(+0, +1) * quadrantCellSize, lowerLeft + new Vector2(+1, +1) * quadrantCellSize);
        //Debug.Log(GetPositionHashMapKey(position) + " " + position);
    }

    private static int GetEntityCountInHashMap(NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap, int hashMapKey)
    {
        QuadrantData entity;
        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        int count = 0;
        if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out entity, out nativeMultiHashMapIterator))
        {
            do
            {
                count++;
            } while (quadrantMultiHashMap.TryGetNextValue(out entity, ref nativeMultiHashMapIterator));
        }
        return count;
    }

    [BurstCompile]
    private struct SetQuadrantDataHashMapJob : IJobChunk
    {

        [ReadOnly] public ComponentTypeHandle<Translation> translationTypeHandle;
        [ReadOnly] public ComponentTypeHandle<QuadrantEntity> quadrantEntityTypeHandle;
        [ReadOnly] public ComponentTypeHandle<AnimationComponent> AnimationComponentTypeHandle;
        [ReadOnly] public EntityTypeHandle entityTypeHandle;
        public NativeMultiHashMap<int, QuadrantData>.ParallelWriter quadrantMultiHashMap;
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var translations = chunk.GetNativeArray(translationTypeHandle);
            var entities = chunk.GetNativeArray(entityTypeHandle);
            var quadrantEntities = chunk.GetNativeArray(quadrantEntityTypeHandle);
            NativeArray<AnimationComponent> chunkAnimationComponents = chunk.GetNativeArray(AnimationComponentTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var animationComponent = chunkAnimationComponents[i];  

                float2 translation2d = translations[i].Value.xy;
                int hashMapKey = GetPositionHashMapKey(translation2d);
                quadrantMultiHashMap.Add(hashMapKey, new QuadrantData
                {
                    Entity = entities[i],
                    Position = translation2d,
                    AnimationComponent = animationComponent,
                    QuadrantEntity = quadrantEntities[i]
                });
            }
        }
    }




    protected override void OnUpdate()
    {
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;
        EntityQuery entityQuery = GetEntityQuery(typeof(Translation), typeof(QuadrantEntity), typeof(AnimationComponent));
        QuadrantMultiHashMap.Clear();
        var entityCount = entityQuery.CalculateEntityCount();
        if (entityCount > QuadrantMultiHashMap.Capacity)
        {
            QuadrantMultiHashMap.Capacity = entityCount;
        }
        var job = new SetQuadrantDataHashMapJob
        {
            translationTypeHandle = GetComponentTypeHandle<Translation>(true),
            quadrantEntityTypeHandle = GetComponentTypeHandle<QuadrantEntity>(true),
            entityTypeHandle = GetEntityTypeHandle(),
            quadrantMultiHashMap = QuadrantMultiHashMap.AsParallelWriter(),
            AnimationComponentTypeHandle = GetComponentTypeHandle<AnimationComponent>(true)
        };
        Dependency = job.ScheduleParallel(entityQuery, Dependency);
        Dependency.Complete();



        //Entities.WithAll<Unit>().WithNone<CommanderComponent>().ForEach((Entity entity, ref Translation translation) =>
        //{
        //    int hashMapKey = GetPositionHashMapKey(translation.Value.xy);
        //    quadrantMultiHashMap.Add(hashMapKey, entity);

        //}).Run();

        // Get mouse position in screen space
        float3 mousePosition = Input.mousePosition;

        // Optionally convert to world space (for 2D or 3D use)
        float3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        // If you're using 2D, drop the Z axis
        float2 worldMouse2D = worldPosition.xy;
        int gridSize = 25; // 25 blocks wide and tall
        float blockSize = 5f; // each block 5x5 units
        float halfGridSize = gridSize * blockSize * 0.5f;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                float2 pos = new float2(x * blockSize - halfGridSize, y * blockSize - halfGridSize);
                DebugDrawQuadrant(pos);
            }
        }
        //Debug.Log(GetEntityCountInHashMap(quadrantMultiHashMap, GetPositionHashMapKey(worldMouse2D)));
        //Debug.Log(GetEntityCountInHashMap(quadrantMultiHashMap, GetPositionHashMapKey(worldMouse2D)));

        //var key = GetPositionHashMapKey(worldMouse2D);
        //int count = GetEntityCountInHashMap(quadrantMultiHashMap, key);
        //Debug.Log($"Entities in cell {key}: {count}");
    }
}
