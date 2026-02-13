using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static EntitySpawner;
using static UnityEngine.EventSystems.EventTrigger;



[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SliceGridSystem))]
public partial class SliceAwarenessSystem : SystemBase
{
    protected override void OnCreate()
    {
    }

    protected override void OnDestroy()
    {
    }

    protected override void OnUpdate()
    {
        //1 build command owned formations sum of AABB  
        var groupLookup = GetComponentDataFromEntity<FormationGroupComponent>(true);

        Entities
            .WithName("SliceAwareness_UpdatePerCommand")
            .WithReadOnly(groupLookup)
            .ForEach((Entity cmdEntity,
                      ref DynamicBuffer<OwnedFormationGroup> owned,
                      ref CommandPerception perception,
                      in CommandComponent command) =>
            {
                if (owned.Length == 0)
                    return;

                // -----------------------
                // 1) Build AoR AABB
                // -----------------------
                float2 aabbMin = new float2(float.PositiveInfinity, float.PositiveInfinity);
                float2 aabbMax = new float2(float.NegativeInfinity, float.NegativeInfinity);

                for (int i = 0; i < owned.Length; i++)
                {
                    var fgEntity = owned[i].Value;
                    if (!groupLookup.HasComponent(fgEntity))
                        continue;

                    var group = groupLookup[fgEntity];
                    float2 gMin = group.BoundsMin;
                    float2 gMax = group.BoundsMax;

                    if (gMax.x <= gMin.x || gMax.y <= gMin.y)
                        continue;

                    aabbMin = math.min(aabbMin, gMin);
                    aabbMax = math.max(aabbMax, gMax);
                }

                if (!math.all(math.isfinite(aabbMin)) || !math.all(math.isfinite(aabbMax)))
                    return;

                // Optional padding
                // float2 pad = new float2(16f, 16f);
                // aabbMin -= pad;
                // aabbMax += pad;

                // -----------------------
                // 2) Find intersecting slices (by cell range)
                // -----------------------
                // NOTE: use the SAME origin/cellsize as SliceGridSystem
                float2 origin = new float2(-32f, -16f);

                int2 cellMin = SliceGridUtil.WorldToCell(aabbMin, origin);
                int2 cellMax = SliceGridUtil.WorldToCell(aabbMax, origin);

                // -----------------------
                // 3) Summarize
                // -----------------------
                float controlSum = 0f;
                float weightSum = 0f;
                float maxIntensity = 0f;

                // Flip control so + means "good for this command"
                bool isAllyCmd = command.FactionType == UnitType.Ally;

                for (int y = cellMin.y; y <= cellMax.y; y++)
                {
                    for (int x = cellMin.x; x <= cellMax.x; x++)
                    {
                        int key = SliceGridUtil.EncodeKey(new int2(x, y));

                        if (!SliceGridSystem.SliceStateMap.TryGetValue(key, out var s))
                            continue;

                        float intensity = s.SmoothedIntensity; // 0..1
                        maxIntensity = math.max(maxIntensity, intensity);

                        float c = s.SmoothedControl;          // -1..+1 (ally+)
                        if (!isAllyCmd) c = -c;               // convert to command POV

                        // Weight by intensity so "hot" slices matter more
                        float w = math.max(0.001f, intensity);
                        controlSum += c * w;
                        weightSum += w;
                    }
                }

                float control = (weightSum > 0f) ? (controlSum / weightSum) : 0f;

                // Momentum: delta control since last intel update
                float momentum = control - perception.PrevControl;

                // Pressure: simple classification (tune later)
                CommandPressureState pressureState;
                if (maxIntensity < 0.05f) pressureState = CommandPressureState.Stable;
                else if (control < -0.50f && maxIntensity > 0.25f && momentum < 0f) pressureState = CommandPressureState.Broken;
                else if (control < -0.25f && maxIntensity > 0.20f && momentum < 0f) pressureState = CommandPressureState.Collapsing;
                else if (control < -0.10f && maxIntensity > 0.15f) pressureState = CommandPressureState.Pressured;
                else pressureState = CommandPressureState.Stable;

                // Write perception
                perception.PrevControl = control;
                perception.Control = control;
                perception.Intensity01 = maxIntensity;
                perception.Momentum = momentum;
                perception.Pressure = pressureState;
                perception.IntelVersion += 1;

            })
            .WithoutBurst()
            .Run();

        //2 find all slices from state map that intersect with AABB  

        //3 summarize  
    }
}

public struct CommandPerception : IComponentData
{
    public uint IntelVersion;

    // Aggregated slice signals (command-scoped)
    public float Control;        // [-1..+1] from command POV
    public float Intensity01;    // [0..1]
    public float Momentum;       // delta of Control

    public CommandPressureState Pressure;

    // Internal bookkeeping (needed to compute momentum cleanly)
    public float PrevControl;
}

public enum CommandPressureState
{
    Stable,
    Pressured,
    Collapsing,
    Broken
}
public struct CommandComponent : IComponentData
{
    public UnitType FactionType;
    public int CommandID;          // Left / Center / Right (or enum later)
}
public struct OwnedFormationGroup : IBufferElementData
{
    public Entity Value; // FormationGroup entity
}
