using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static EntitySpawner;

public enum AwarenessSource : byte
{
    DirectObservation,
    Memory
}

public struct CommandAwarenessConfig : IComponentData
{
    public float ObservationRadius;
    public float MemoryDuration;
}

public struct CommandKnownSlice : IBufferElementData
{
    public int SliceKey;
    public float Control;
    public float Intensity;
    public float Momentum;
    public SliceTacticalState State;
    public double LastObservedTime;
    public float Confidence;
    public AwarenessSource Source;
}

public struct CommandKnownFormation : IBufferElementData
{
    public Entity Formation;
    public UnitType Faction;
    public float2 BoundsMin;
    public float2 BoundsMax;
    public FormationStatusEnum Status;
    public FormationCaptainState CaptainState;
    public float CaptainControl;
    public float CaptainIntensity;
    public float CaptainMomentum;
    public int AliveUnitCount;
    public byte HasCaptainReport;
    public double LastObservedTime;
    public float Confidence;
    public AwarenessSource Source;
}

public struct CommandAwareness : IComponentData
{
    public uint IntelVersion;
    public float Control;
    public float Intensity;
    public float Momentum;
    public float Confidence;
    public CommandPressureState Pressure;
    public int KnownSliceCount;
    public int FriendlyFormationCount;
    public int EnemyFormationCount;
    public double LastUpdatedTime;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FormationCaptainReportSystem))]
public partial class CommandAwarenessSystem : SystemBase
{
    private static readonly float2 SliceOrigin = new float2(-32f, -16f);
    private EntityQuery _commandQuery;
    private EntityQuery _formationQuery;

    protected override void OnCreate()
    {
        _commandQuery = GetEntityQuery(
            ComponentType.ReadOnly<CommandComponent>(),
            ComponentType.ReadOnly<CommandAwarenessConfig>(),
            ComponentType.ReadWrite<CommandAwareness>(),
            ComponentType.ReadWrite<CommandPerception>(),
            ComponentType.ReadWrite<CommandKnownSlice>(),
            ComponentType.ReadWrite<CommandKnownFormation>());

        _formationQuery = GetEntityQuery(ComponentType.ReadOnly<FormationGroupComponent>());
    }

    protected override void OnUpdate()
    {
        if (!SliceGridSystem.SliceStateMap.IsCreated)
            return;

        Dependency.Complete();

        double now = Time.ElapsedTime;
        var translations = GetComponentDataFromEntity<Translation>(true);
        var captains = GetComponentDataFromEntity<FormationCaptainComponent>(true);

        var commandEntities = _commandQuery.ToEntityArray(Allocator.TempJob);
        var commands = _commandQuery.ToComponentDataArray<CommandComponent>(Allocator.TempJob);
        var configs = _commandQuery.ToComponentDataArray<CommandAwarenessConfig>(Allocator.TempJob);
        var formationEntities = _formationQuery.ToEntityArray(Allocator.TempJob);
        var formations = _formationQuery.ToComponentDataArray<FormationGroupComponent>(Allocator.TempJob);
        var sliceStates = SliceGridSystem.SliceStateMap.GetKeyValueArrays(Allocator.TempJob);

        for (int commandIndex = 0; commandIndex < commandEntities.Length; commandIndex++)
        {
            Entity commandEntity = commandEntities[commandIndex];
            CommandComponent command = commands[commandIndex];
            CommandAwarenessConfig config = configs[commandIndex];

            if (command.AwarenessOrigin == Entity.Null || !translations.HasComponent(command.AwarenessOrigin))
                continue;

            float2 origin = translations[command.AwarenessOrigin].Value.xy;
            float radius = math.max(0f, config.ObservationRadius);
            float memoryDuration = math.max(0.01f, config.MemoryDuration);

            DynamicBuffer<CommandKnownSlice> knownSlices = EntityManager.GetBuffer<CommandKnownSlice>(commandEntity);
            DynamicBuffer<CommandKnownFormation> knownFormations = EntityManager.GetBuffer<CommandKnownFormation>(commandEntity);

            AgeSliceMemory(ref knownSlices, now, memoryDuration);
            AgeFormationMemory(ref knownFormations, now, memoryDuration);

            for (int i = 0; i < sliceStates.Keys.Length; i++)
            {
                int key = sliceStates.Keys[i];
                int2 cell = SliceGridUtil.DecodeKey(key);
                SliceGridUtil.CellBounds(cell, SliceOrigin, out float2 min, out float2 max);
                if (!CircleIntersectsAabb(origin, radius, min, max))
                    continue;

                SliceStateData state = sliceStates.Values[i];
                UpsertSlice(ref knownSlices, new CommandKnownSlice
                {
                    SliceKey = key,
                    Control = state.SmoothedControl,
                    Intensity = state.SmoothedIntensity,
                    Momentum = state.Momentum,
                    State = state.State,
                    LastObservedTime = now,
                    Confidence = 1f,
                    Source = AwarenessSource.DirectObservation
                });
            }

            for (int i = 0; i < formationEntities.Length; i++)
            {
                FormationGroupComponent group = formations[i];
                float2 min = group.BoundsMin;
                float2 max = group.BoundsMax;
                if (max.x < min.x || max.y < min.y)
                    min = max = group.AnchorPosition;

                if (!CircleIntersectsAabb(origin, radius, min, max))
                    continue;

                Entity formationEntity = formationEntities[i];
                bool hasCaptain = captains.HasComponent(formationEntity);
                FormationCaptainComponent captain = hasCaptain
                    ? captains[formationEntity]
                    : default;

                UpsertFormation(ref knownFormations, new CommandKnownFormation
                {
                    Formation = formationEntity,
                    Faction = group.UnitType,
                    BoundsMin = min,
                    BoundsMax = max,
                    Status = group.FormationGroupStatus,
                    CaptainState = captain.State,
                    CaptainControl = captain.Control,
                    CaptainIntensity = captain.Intensity,
                    CaptainMomentum = captain.Momentum,
                    AliveUnitCount = group.AliveUnitCount,
                    HasCaptainReport = (byte)(hasCaptain ? 1 : 0),
                    LastObservedTime = now,
                    Confidence = 1f,
                    Source = AwarenessSource.DirectObservation
                });
            }

            CommandAwareness awareness = EntityManager.GetComponentData<CommandAwareness>(commandEntity);
            BuildSummary(ref awareness, knownSlices, knownFormations, command.FactionType, now);
            EntityManager.SetComponentData(commandEntity, awareness);

            // Keep the existing consumer-facing component synchronized during migration.
            CommandPerception perception = EntityManager.GetComponentData<CommandPerception>(commandEntity);
            perception.IntelVersion = awareness.IntelVersion;
            perception.PrevControl = perception.Control;
            perception.Control = awareness.Control;
            perception.Intensity01 = awareness.Intensity;
            perception.Momentum = awareness.Momentum;
            perception.Pressure = awareness.Pressure;
            EntityManager.SetComponentData(commandEntity, perception);
        }

        sliceStates.Dispose();
        formations.Dispose();
        formationEntities.Dispose();
        configs.Dispose();
        commands.Dispose();
        commandEntities.Dispose();
    }

    private static void AgeSliceMemory(ref DynamicBuffer<CommandKnownSlice> buffer, double now, float duration)
    {
        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            CommandKnownSlice entry = buffer[i];
            float age = (float)(now - entry.LastObservedTime);
            if (age >= duration)
            {
                buffer.RemoveAt(i);
                continue;
            }

            entry.Source = AwarenessSource.Memory;
            entry.Confidence = math.saturate(1f - age / duration);
            buffer[i] = entry;
        }
    }

    private static void AgeFormationMemory(ref DynamicBuffer<CommandKnownFormation> buffer, double now, float duration)
    {
        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            CommandKnownFormation entry = buffer[i];
            float age = (float)(now - entry.LastObservedTime);
            if (age >= duration)
            {
                buffer.RemoveAt(i);
                continue;
            }

            entry.Source = AwarenessSource.Memory;
            entry.Confidence = math.saturate(1f - age / duration);
            buffer[i] = entry;
        }
    }

    private static void UpsertSlice(ref DynamicBuffer<CommandKnownSlice> buffer, CommandKnownSlice value)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].SliceKey != value.SliceKey)
                continue;
            buffer[i] = value;
            return;
        }
        buffer.Add(value);
    }

    private static void UpsertFormation(ref DynamicBuffer<CommandKnownFormation> buffer, CommandKnownFormation value)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].Formation != value.Formation)
                continue;
            buffer[i] = value;
            return;
        }
        buffer.Add(value);
    }

    private static void BuildSummary(
        ref CommandAwareness awareness,
        DynamicBuffer<CommandKnownSlice> slices,
        DynamicBuffer<CommandKnownFormation> formations,
        UnitType faction,
        double now)
    {
        float weightedControl = 0f;
        float weightedMomentum = 0f;
        float weightSum = 0f;
        float maxIntensity = 0f;
        float confidenceSum = 0f;

        for (int i = 0; i < slices.Length; i++)
        {
            CommandKnownSlice slice = slices[i];
            float commandControl = faction == UnitType.Ally ? slice.Control : -slice.Control;
            float commandMomentum = faction == UnitType.Ally ? slice.Momentum : -slice.Momentum;
            float weight = math.max(0.001f, slice.Intensity) * slice.Confidence;
            weightedControl += commandControl * weight;
            weightedMomentum += commandMomentum * weight;
            weightSum += weight;
            maxIntensity = math.max(maxIntensity, slice.Intensity * slice.Confidence);
            confidenceSum += slice.Confidence;
        }

        int friendly = 0;
        int enemy = 0;
        for (int i = 0; i < formations.Length; i++)
        {
            if (formations[i].Faction == faction) friendly++;
            else enemy++;
        }

        awareness.Control = weightSum > 0f ? weightedControl / weightSum : 0f;
        awareness.Momentum = weightSum > 0f ? weightedMomentum / weightSum : 0f;
        awareness.Intensity = maxIntensity;
        awareness.Confidence = slices.Length > 0 ? confidenceSum / slices.Length : 0f;
        awareness.KnownSliceCount = slices.Length;
        awareness.FriendlyFormationCount = friendly;
        awareness.EnemyFormationCount = enemy;
        awareness.Pressure = ClassifyPressure(awareness.Control, awareness.Intensity, awareness.Momentum);
        awareness.IntelVersion++;
        awareness.LastUpdatedTime = now;
    }

    private static CommandPressureState ClassifyPressure(float control, float intensity, float momentum)
    {
        if (intensity < 0.05f) return CommandPressureState.Stable;
        if (control < -0.50f && intensity > 0.25f && momentum < 0f) return CommandPressureState.Broken;
        if (control < -0.25f && intensity > 0.20f && momentum < 0f) return CommandPressureState.Collapsing;
        if (control < -0.10f && intensity > 0.15f) return CommandPressureState.Pressured;
        return CommandPressureState.Stable;
    }

    public static bool CircleIntersectsAabb(float2 center, float radius, float2 min, float2 max)
    {
        float2 closest = math.clamp(center, min, max);
        return math.lengthsq(closest - center) <= radius * radius;
    }
}
