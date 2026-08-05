using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static EntitySpawner;
using static UnityEngine.EventSystems.EventTrigger;



[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SliceGridSystem))]
[DisableAutoCreation]
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
        float deltaTime = Time.DeltaTime;

        //1 build command owned formations sum of AABB  
        var groupLookup = GetComponentDataFromEntity<FormationGroupComponent>(true);
        var groupCaptainLookup = GetComponentDataFromEntity<FormationCaptainComponent>(false);

        //with readonly
        var sliceStateMap = SliceGridSystem.SliceStateMap;


        Entities
            .WithName("SliceAwareness_UpdatePerCommand")
            .WithReadOnly(groupLookup)
            .ForEach((Entity cmdEntity,
                      ref DynamicBuffer<OwnedFormationGroup> owned,
                      ref CommandPerception perception,
                      in CommandComponent command,
                      in Translation commanderTranslation) =>
            {
                if (owned.Length == 0)
                    return;

                float2 commanderPos = commanderTranslation.Value.xy;


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

                    float2 groupCenter = (gMin + gMax) * 0.5f;

                    float distanceSq = math.lengthsq(groupCenter - commanderPos);
                    float awarenessRadiusSq = 40f; // 100 units radius, tune later
                    if (distanceSq > awarenessRadiusSq) // 100 units radius, tune later
                        continue;


                    aabbMin = math.min(aabbMin, gMin);
                    aabbMax = math.max(aabbMax, gMax);
                }
                if (!math.all(math.isfinite(aabbMin)) || !math.all(math.isfinite(aabbMax)))
                {
                    perception.Intensity01 = 0f;
                    perception.Control = 0f;
                    perception.Momentum = 0f;
                    perception.Pressure = CommandPressureState.Unknown; // if you have it
                    return;
                }

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

                        if (!sliceStateMap.TryGetValue(key, out var s))
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
                else pressureState = CommandPressureState.Unknown;

                // Write perception
                perception.PrevControl = control;
                perception.Control = control;
                perception.Intensity01 = maxIntensity;
                perception.Momentum = momentum;
                perception.Pressure = pressureState;
                perception.IntelVersion += 1;

            })//.ScheduleParallel();
        .WithoutBurst()
        .Run();

        //2 find all slices from state map that intersect with AABB  

        //3 summarize  



    //    Entities
    //.WithName("FormationCaptainReportSystem")
    //.ForEach((
    //    ref FormationCaptainComponent report,
    //    in FormationGroupComponent formationGroup,
    //    in Translation translation
    //) =>
    //{
    //    // 1. Use formation group position as the report location
    //    float2 formationPos = formationGroup.AnchorPosition;

    //    // 2. Find current / nearest slice from formation position
    //    // var slice = FindSliceForPosition(formationPos);

    //// 3. Copy slice summary into report
    //// report.CurrentSliceEntity = slice.Entity;
    //// report.Pressure = slice.Pressure;
    //// report.AllyStrength = slice.AllyStrength;
    //// report.EnemyStrength = slice.EnemyStrength;
    //// report.ThreatDirection = slice.ThreatDirection;
    //     report.FormationPosition = formationGroup.AnchorPosition;
    //report.SlicePressureStatus = ; // Used to determine if the slice is under pressure and needs to adjust position
    //report.LastUpdatedTime;
    //    report.IsValid;

    //// 4. Mark report fresh
    //report.LastUpdatedTime += deltaTime;
    //    report.IsValid = true;
    //})
    //.ScheduleParallel();


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
    Broken,
    Unknown
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



[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SliceGridSystem))]
public partial class FormationCaptainReportSystem : SystemBase
{
    private static readonly float2 _origin = new float2(-32f, -16f);
    private float _stateUpdateTimer;
    private const float StateUpdateInterval = 0.15f; // 6.66 Hz (nice starting point)

    protected override void OnUpdate()
    {

        float dt = Time.DeltaTime;
        double now = Time.ElapsedTime;

        var sliceStateMap = SliceGridSystem.SliceStateMap;

        _stateUpdateTimer += dt;
        if (_stateUpdateTimer >= StateUpdateInterval)
        {
            // Consume exactly one tick (keeps cadence stable even if dt varies)
            _stateUpdateTimer -= StateUpdateInterval;

        Entities
            .WithName("FormationCaptainReport_UpdateFromSlices")
            .WithReadOnly(sliceStateMap)
            .WithNone<DeadTagComponent>()
            .ForEach((
                ref FormationCaptainComponent captain,
                in FormationGroupComponent group) =>
            {
                float2 aMin = group.BoundsMin;
                float2 aMax = group.BoundsMax;

                //if (aMax.x <= aMin.x || aMax.y <= aMin.y)
                //{
                //    captain.State = FormationCaptainState.Collapsing;
                //    captain.SliceCount = 0;
                //    return;
                //}

                bool isAlly = group.UnitType == UnitType.Ally;

                int2 cellMin = SliceGridUtil.WorldToCell(aMin, _origin);
                int2 cellMax = SliceGridUtil.WorldToCell(aMax, _origin);

                float controlSum = 0f;
                float intensitySum = 0f;
                float weightSum = 0f;
                float maxIntensity = 0f;
                int primarySliceKey = 0;
                int sliceCount = 0;

                for (int y = cellMin.y; y <= cellMax.y; y++)
                {
                    for (int x = cellMin.x; x <= cellMax.x; x++)
                    {
                        int key = SliceGridUtil.EncodeKey(new int2(x, y));

                        if (!sliceStateMap.TryGetValue(key, out var s))
                            continue;

                        float control = s.SmoothedControl;

                        // Convert global slice control into this formation's POV
                        if (!isAlly)
                            control = -control;

                        float intensity = s.SmoothedIntensity;
                        //float weight = math.max(0.001f, intensity);

                        controlSum += control;// * weight;
                        intensitySum += intensity;// * weight;
                        //weightSum += weight;
                        sliceCount++;

                        //if (intensity > maxIntensity)
                        //{
                        //    maxIntensity = intensity;
                        //    primarySliceKey = key;
                        //}
                    }
                }

                float finalControl = controlSum;/// sliceCount;//> 0f ? controlSum / weightSum : 0f;
                //if (isAlly && group.FormationID == 1)
                //{
                //    Debug.Log(FormatControlEnum(finalControl));
                //}
                FormationCaptainState state = captain.State;
                var intensityAvg = intensitySum / sliceCount;
                state = FormatControlEnum(finalControl, intensityAvg, captain.SliceCount);
                //if (sliceCount == 0)
                //    state = FormationCaptainState.Idle;
                //else if (finalControl > 0.25f)
                //    state = FormationCaptainState.Winning;
                //else if (finalControl < -0.15f)
                //    state = FormationCaptainState.Collapsing;
                //else if (finalControl == 0f)
                //    state = FormationCaptainState.Pressured;
                //else
                //    state = FormationCaptainState.Holding;

                captain.FormationPosition = (aMin + aMax) * 0.5f;
                captain.State = state;
                captain.Control = finalControl;
                captain.Intensity = intensityAvg; //avg intensity
                captain.PrimarySliceKey = primarySliceKey;
                captain.SliceCount = sliceCount;
            })
            .WithoutBurst()
            .Run();
        }


    }
    private static string FormatControl(float control)
    {
        if (control >= 0.75f) return "Dominating";
        if (control >= 0.35f) return "Winning";
        if (control >= 0.10f) return "Slight Edge";
        if (control > -0.10f) return "Even";
        if (control > -0.35f) return "Pressured";
        if (control > -0.75f) return "Collapsing";
        return "Broken";
    }
    private static FormationCaptainState FormatControlEnum(
        float control,
        float intensityAvg,
        int sliceCount)
    {
        if (sliceCount == 0)
            return FormationCaptainState.Unknown;

        // quiet / no meaningful engagement
        if (intensityAvg < 0.02f)
            return FormationCaptainState.Idle;

        if (control >= 0.60f)
            return FormationCaptainState.Winning;

        if (control >= 0.20f)
            return FormationCaptainState.SlightEdge;

        if (control > -0.20f)
            return FormationCaptainState.Holding;

        if (control > -0.60f)
            return FormationCaptainState.Pressured;

        // collapse only if actually hot AND losing badly
        if (intensityAvg >= 0.15f)
            return FormationCaptainState.Collapsing;

        return FormationCaptainState.Pressured;
    }
    private static FormationCaptainState GetCaptainState(float control, float intensity)
    {
        if (intensity < 0.05f) return FormationCaptainState.Idle;
        if (control >= 0.35f) return FormationCaptainState.Winning;
        if (control <= -0.50f) return FormationCaptainState.Collapsing;
        if (control <= -0.15f) return FormationCaptainState.Pressured;
        return FormationCaptainState.Holding;
    }
}
