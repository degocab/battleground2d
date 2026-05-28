using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static EntitySpawner;

public struct SliceAccum
{
    public float AllyStrength;
    public float EnemyStrength;
    public float Intensity; // optional (you can ignore for now)
}

public enum SliceTacticalState : byte
{
    Empty = 0,         // basically no meaningful presence
    Clash = 1,         // both sides present, real fight, no clear winner
    AllyAdvantage = 2, // allies have the upper hand here
    EnemyAdvantage = 3 // enemies have the upper hand here
}

public enum FormationCaptainState : byte
{
    Idle = 0,
    Holding = 1,
    Pressured = 2,
    Winning = 3,
    Collapsing = 4,
    Broken = 5,
    SlightEdge = 6,
    Unknown = 7
}
public struct SliceStateData
{
    // Smoothed values (decision-ready)
    public float SmoothedControl;     // [-1..+1]
    public float SmoothedIntensity;   // >= 0 (scaled to your game)
    public float Momentum;            // control delta per update tick

    // For computing momentum and decay
    public float PrevSmoothedControl;
    public double LastSeenTime;       // Time.ElapsedTime when last observed
    public double LastUpdateTime;     // Time.ElapsedTime when we last updated this cell

    // Later state machine fields (kept now so you don't refactor again)
    public SliceTacticalState State;
    public float TimeInState;
}

public static class SliceGridUtil
{
    public const int YMultiplier = 100000;
    public const float CellSize = 8f;

    // Convert a world position into an integer cell coordinate (x,y)
    public static int2 WorldToCell(float2 worldPos, float2 origin)
    {
        // Shift by origin, divide by cell size, then floor to get a cell index
        float2 p = (worldPos - origin) / CellSize;
        return (int2)math.floor(p);
    }

    // Convert (cellX, cellY) into a single int key for hashing
    public static int EncodeKey(int2 cell)
        => cell.x + cell.y * YMultiplier;

    // --- The next few helpers are ONLY so negative world coordinates work correctly ---
    // If your battlefield can go negative (x < 0 or y < 0), this matters.
    static int FloorDiv(int a, int b)
    {
        int q = a / b;
        int r = a % b;
        if (r != 0 && ((r > 0) != (b > 0))) q--; // adjust toward -infinity
        return q;
    }

    public static int2 DecodeKey(int key)
    {
        // Recover y using floor division, then x from remainder
        int y = FloorDiv(key, YMultiplier);
        int x = key - y * YMultiplier;
        return new int2(x, y);
    }

    // Given a cell coordinate, return the world-space AABB of that cell (min/max corners)
    public static void CellBounds(int2 cell, float2 origin, out float2 min, out float2 max)
    {
        min = origin + (float2)cell * CellSize;
        max = min + new float2(CellSize, CellSize);
    }

    // Area of an AABB
    public static float AreaAabb(float2 min, float2 max)
    {
        float2 s = max - min;
        return math.max(0f, s.x) * math.max(0f, s.y);
    }

    // Area overlap between two AABBs (if no overlap, returns 0)
    public static float OverlapAreaAabb(float2 aMin, float2 aMax, float2 bMin, float2 bMax)
    {
        float2 oMin = math.max(aMin, bMin);
        float2 oMax = math.min(aMax, bMax);

        float2 s = oMax - oMin;
        if (s.x <= 0f || s.y <= 0f) return 0f;

        return s.x * s.y;
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SetAnimationTypeSystem))]
public partial class SliceGridSystem : SystemBase
{
    // Global map: sliceKey -> accumulated info (ally strength, enemy strength, etc.)
    public static NativeHashMap<int, SliceAccum> SliceMap;
    private EntityQuery _groupsQuery;
    // Where your grid starts in the world. Usually zero is fine.
    private static readonly float2 _origin = new float2(-32f, -16f);

    // Persistent slice state map: sliceKey -> persistent state data
    public static NativeHashMap<int, SliceStateData> SliceStateMap;
    // Timer for persistent updates
    private float _stateUpdateTimer;

    // Tune these without changing logic
    private const float StateUpdateInterval = 0.15f; // 6.66 Hz (nice starting point)
    private const float ControlSmoothing = 0.25f;    // 0..1 per state tick (higher = snappier)
    private const float IntensitySmoothing = 0.25f;
    private const float InactiveDecayRate = 0.85f;   // per state tick (lower = decays faster)
    private const double PruneAfterSeconds = 2.5;    // remove cell if unseen for this long and low intensity
    private const float PruneIntensityThreshold = 0.05f;

    // Intensity: how "big and contested" this cell is.
    // We’ll convert raw contested strength into 0..1 using this.
    private const float IntensityScale = 500f; // tweak based on your unit counts


    protected override void OnCreate()
    {
        // We only care about formation GROUPS (not units), because groups have BoundsMin/BoundsMax.
        _groupsQuery = GetEntityQuery(
            ComponentType.ReadOnly<FormationGroupComponent>(),
            ComponentType.Exclude<DeadTagComponent>()
        );

        // Pre-allocate space. If you see capacity issues later, increase this number.
        SliceMap = new NativeHashMap<int, SliceAccum>(4096, Allocator.Persistent);
        SliceStateMap = new NativeHashMap<int, SliceStateData>(4096, Allocator.Persistent);

        _stateUpdateTimer = 0f;
    }

    protected override void OnDestroy()
    {
        if (SliceMap.IsCreated) SliceMap.Dispose();
        if (SliceStateMap.IsCreated) SliceStateMap.Dispose();
    }

    protected override void OnUpdate()
    {
        // Don’t run if the game isn’t actually playing.
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;



        float dt = Time.DeltaTime;
        double now = Time.ElapsedTime;


        // Start fresh every frame (you can change to “every 0.25s” later).
        SliceMap.Clear();

        var groupCaptainLookup = GetComponentDataFromEntity<FormationCaptainComponent>(false);



        // ============================
        // 1) ACCUMULATE SLICE DATA
        // ============================
        //
        // NOTE: This runs on the main thread (Run()).
        // That’s because NativeHashMap is annoying to safely update in parallel.
        // MVP goal: correct + debuggable first. Optimize later.
        Entities
            .WithName("SliceAccumulate_AABBOverlap")
            .WithNone<DeadTagComponent>()
            .ForEach((Entity entity, ref FormationCaptainComponent formationCaptainComponent, in FormationGroupComponent group) =>
            {
                // This formation group’s bounds in world space:
                float2 aMin = group.BoundsMin;
                float2 aMax = group.BoundsMax;

                // If bounds are nonsense, skip
                float aabbArea = SliceGridUtil.AreaAabb(aMin, aMax);
                if (aabbArea <= 1e-6f) return;

                // ----------------------------
                // HOW STRONG IS THIS FORMATION?
                // ----------------------------
                // Right now, this is a placeholder.
                // Replace this later with:
                // strength = unitCount * formationTypeFactor * morale
                //float strength = 1f;
                float strength = math.max(0f, group.CurrentUnitCount) * (group.FormationType == FormationType.Phalanx ? 1.25f : 1.0f);
                formationCaptainComponent.FormationPosition = (aMin + aMax) * 0.5f; // Update captain's formation position for later use
                formationCaptainComponent.SlicePressureStatus = strength; // Update slice pressure status for later use
                formationCaptainComponent.CurrentSlice = 0f; // Reset current slice for now (you can implement slice assignment logic later)





                // ----------------------------
                // IS THIS ALLY OR ENEMY?
                // ----------------------------
                // Adjust this line to your own enum/logic.
                // If your UnitType is something else, change it.
                bool isAlly = group.UnitType == UnitType.Ally;

                // Find which slice cells this AABB touches
                int2 cellMin = SliceGridUtil.WorldToCell(aMin, _origin);
                int2 cellMax = SliceGridUtil.WorldToCell(aMax, _origin);

                // Loop over every cell in that range
                for (int y = cellMin.y; y <= cellMax.y; y++)
                {
                    for (int x = cellMin.x; x <= cellMax.x; x++)
                    {
                        int2 cell = new int2(x, y);

                        // The world bounds of this cell (a square)
                        SliceGridUtil.CellBounds(cell, _origin, out var cMin, out var cMax);

                        // How much does the formation overlap this cell?
                        float overlap = SliceGridUtil.OverlapAreaAabb(aMin, aMax, cMin, cMax);
                        if (overlap <= 0f) continue;

                        // Convert overlap area -> fraction (0..1)
                        float frac = overlap / aabbArea;

                        // Contribution into this cell
                        float contrib = strength * frac;

                        // Hash key for this cell
                        int key = SliceGridUtil.EncodeKey(cell);

                        // Read old value (if any), then add to it
                        if (!SliceMap.TryGetValue(key, out var acc))
                            acc = default;

                        if (isAlly) acc.AllyStrength += contrib;
                        else acc.EnemyStrength += contrib;

                        // Write back updated accumulator
                        SliceMap[key] = acc;
                    }
                }
            }).WithoutBurst().Run();

        // ----------------------------
        // 2) PERSISTENT SLICE STATE UPDATE (TIMER)
        // ----------------------------
        _stateUpdateTimer += dt;
        if (_stateUpdateTimer >= StateUpdateInterval)
        {
            // Consume exactly one tick (keeps cadence stable even if dt varies)
            _stateUpdateTimer -= StateUpdateInterval;

            UpdatePersistentSliceState(now);
        }

        // ----------------------------
        // 3) DEBUG DRAW
        // ----------------------------
        // You can draw either the snapshot OR the smoothed persistent map.
        // Snapshot draw (your old behavior):
        // DebugDrawActiveSlices(SliceMap, _origin, 0.12f);

        // Smoothed draw (recommended once you add SliceState):
        //DebugDrawSliceStates(SliceStateMap, _origin, 0.12f);
    }

    private void UpdatePersistentSliceState(double now)
    {
        // Track which keys were seen this tick
        var seen = new NativeHashSet<int>(math.max(16, SliceMap.Count()), Allocator.Temp);

        // Snapshot arrays from the per-frame accumulation
        var snapshotKva = SliceMap.GetKeyValueArrays(Allocator.Temp);

        // ---------- SINGLE PASS: update/create SliceState entries ----------
        for (int i = 0; i < snapshotKva.Keys.Length; i++)
        {
            int key = snapshotKva.Keys[i];
            SliceAccum acc = snapshotKva.Values[i];

            float ally = acc.AllyStrength;
            float enemy = acc.EnemyStrength;
            float sum = ally + enemy;

            // Nothing here? Skip. We'll handle decay/prune below.
            if (sum <= 1e-5f)
                continue;

            // Who owns this slice right now?  [-1..+1]
            // +1 = all ally, -1 = all enemy, 0 = perfectly contested
            float rawControl = (ally - enemy) / sum;

            // Intensity: how much *overlap* there is.
            // Simple & readable: contested power = min(ally, enemy)
            float contestedPower = math.min(ally, enemy);

            // Normalized 0..1 intensity (for debug/logic)
            float rawIntensity01 = math.saturate(contestedPower / IntensityScale);

            seen.Add(key);

            // Fetch previous state if it exists
            if (!SliceStateMap.TryGetValue(key, out var s))
            {
                // New cell: seed smoothing with raw values
                s = new SliceStateData
                {
                    SmoothedControl = rawControl,
                    PrevSmoothedControl = rawControl,
                    SmoothedIntensity = rawIntensity01,
                    Momentum = 0f,
                    LastSeenTime = now,
                    LastUpdateTime = now,
                    State = SliceTacticalState.Empty,
                    TimeInState = 0f
                };

                // Initial classification based on these values
                UpdateSliceTacticalState(ref s, ally, enemy);

                SliceStateMap[key] = s;
                continue;
            }

            // We have a previous state: update it in-place

            // Store previous smoothed control for momentum
            s.PrevSmoothedControl = s.SmoothedControl;

            // Smooth toward current raw values (simple lerp)
            s.SmoothedControl = math.lerp(s.SmoothedControl, rawControl, ControlSmoothing);
            s.SmoothedIntensity = math.lerp(s.SmoothedIntensity, rawIntensity01, IntensitySmoothing);

            // Momentum = change in control since last tick (still very simple)
            s.Momentum = s.SmoothedControl - s.PrevSmoothedControl;

            // Timing (kept for possible future use)
            s.LastSeenTime = now;
            s.LastUpdateTime = now;

            // Simple state update
            UpdateSliceTacticalState(ref s, ally, enemy);

            SliceStateMap[key] = s;
        }

        snapshotKva.Dispose();

        // ---------- Decay / prune cells that were not seen this tick ----------
        var stateKva = SliceStateMap.GetKeyValueArrays(Allocator.Temp);
        var keysToRemove = new NativeList<int>(Allocator.Temp);

        for (int i = 0; i < stateKva.Keys.Length; i++)
        {
            int key = stateKva.Keys[i];
            SliceStateData s = stateKva.Values[i];

            if (seen.Contains(key))
                continue;

            // Not seen this tick: gently decay toward neutral/empty
            s.SmoothedControl = math.lerp(s.SmoothedControl, 0f, 1f - InactiveDecayRate);
            s.SmoothedIntensity *= InactiveDecayRate;
            s.Momentum = 0f;
            s.LastUpdateTime = now;
            s.TimeInState += StateUpdateInterval;

            double unseenFor = now - s.LastSeenTime;

            // If it's been unseen for a while and is basically cold, remove it
            if (unseenFor >= PruneAfterSeconds && s.SmoothedIntensity <= PruneIntensityThreshold)
            {
                keysToRemove.Add(key);
            }
            else
            {
                SliceStateMap[key] = s;
            }
        }

        for (int i = 0; i < keysToRemove.Length; i++)
        {
            SliceStateMap.Remove(keysToRemove[i]);
        }

        stateKva.Dispose();
        keysToRemove.Dispose();
        seen.Dispose();

        //we need to check if any neighboring slices have a stronger influence and apply that influence
        ApplyNeighborInfluence();

    }
    private void ApplyNeighborInfluence()
    {
        if (!SliceStateMap.IsCreated || SliceStateMap.Count() == 0)
            return;

        var kva = SliceStateMap.GetKeyValueArrays(Allocator.Temp);
        var baseStates = new NativeHashMap<int, SliceTacticalState>(kva.Keys.Length, Allocator.Temp);

        for (int i = 0; i < kva.Keys.Length; i++)
        {
            baseStates.TryAdd(kva.Keys[i], kva.Values[i].State);
        }

        int2[] neighborOffsets = new[]
        {
        new int2(-1, 0),
        new int2(1, 0),
        new int2(0, -1),
        new int2(0, 1),
    };

        for (int i = 0; i < kva.Keys.Length; i++)
        {
            int key = kva.Keys[i];
            SliceStateData s = kva.Values[i];

            // Only let neighbors influence pure Clash tiles.
            if (s.State != SliceTacticalState.Clash)
                continue;

            int2 cell = SliceGridUtil.DecodeKey(key);

            int allyAdvNeighbors = 0;
            int enemyAdvNeighbors = 0;

            for (int n = 0; n < neighborOffsets.Length; n++)
            {
                int2 neighborCell = cell + neighborOffsets[n];
                int neighborKey = SliceGridUtil.EncodeKey(neighborCell);

                if (!baseStates.TryGetValue(neighborKey, out var nState))
                    continue;

                if (nState == SliceTacticalState.AllyAdvantage)
                    allyAdvNeighbors++;
                else if (nState == SliceTacticalState.EnemyAdvantage)
                    enemyAdvNeighbors++;
            }

            // If one side has strictly more advantaged neighbors (and at least 1),
            // let that advantage bleed into this Clash cell.
            if (allyAdvNeighbors > enemyAdvNeighbors && allyAdvNeighbors > 0)
            {
                s.State = SliceTacticalState.AllyAdvantage;
            }
            else if (enemyAdvNeighbors > allyAdvNeighbors && enemyAdvNeighbors > 0)
            {
                s.State = SliceTacticalState.EnemyAdvantage;
            }

            SliceStateMap[key] = s;
        }

        baseStates.Dispose();
        kva.Dispose();
    }


    /// <summary>
    /// Returns friendly advantage 0..1:
    /// 0   = other side fully controls this slice
    /// 0.5 = perfectly even
    /// 1   = your side fully controls it
    /// </summary>
    public static float GetFriendlyAdvantage(in SliceStateData s, bool isAlly)
    {
        // If the viewer is Ally:
        //    +1 control becomes 1 advantage
        //    -1 control becomes 0 advantage
        // If the viewer is Enemy: 
        //    +1 control becomes 0 advantage
        //    -1 control becomes 1 advantage

        float c = s.SmoothedControl; // -1..+1

        if (!isAlly)
            c = -c;

        // Convert -1..+1 into 0..1 range:
        return (c * 0.5f) + 0.5f;
    }
    private static void UpdateSliceTacticalState(ref SliceStateData s, float allyStrength, float enemyStrength)
    {
        // --- Tunables (keep these readable) ---
        const float PresenceMin = 0.5f;   // below -> Empty
        const float ClashPresenceMin = 5f;     // both sides at least this -> real clash
        const float AdvantageControlMin = 0.25f;  // how much control counts as a noticeable edge
        const float MomentumThreshold = 0.05f;  // swing to let momentum decide borderline cases

        float sum = allyStrength + enemyStrength;
        float control = s.SmoothedControl;              // [-1..+1], + = ally, - = enemy
        float controlAbs = math.abs(control);
        float contestedPower = math.min(allyStrength, enemyStrength);
        float momentum = s.Momentum;

        SliceTacticalState newState;

        // 1) Empty: almost no units in this slice
        if (sum < PresenceMin)
        {
            newState = SliceTacticalState.Empty;
        }
        else if (contestedPower < ClashPresenceMin)
        {
            // 2) One side is basically just holding this tile with little opposition.
            // Whichever side has more here gets "Advantage".
            newState = (control >= 0f)
                ? SliceTacticalState.AllyAdvantage
                : SliceTacticalState.EnemyAdvantage;
        }
        else
        {
            // 3) Real clash: both sides present in meaningful numbers.

            bool strongMomentumAlly = momentum > MomentumThreshold;
            bool strongMomentumEnemy = momentum < -MomentumThreshold;

            // If no clear control edge and no big swing -> plain Clash.
            if (controlAbs < AdvantageControlMin && !strongMomentumAlly && !strongMomentumEnemy)
            {
                newState = SliceTacticalState.Clash;
            }
            else
            {
                // Someone has an edge – decide who.
                // Priority: control sign; momentum only helps in borderline cases.
                if (control > 0f || strongMomentumAlly)
                    newState = SliceTacticalState.AllyAdvantage;
                else
                    newState = SliceTacticalState.EnemyAdvantage;
            }
        }

        // Simple time-in-state bookkeeping (you may use it later)
        if (newState == s.State)
        {
            s.TimeInState += StateUpdateInterval;
        }
        else
        {
            s.State = newState;
            s.TimeInState = 0f;
        }
    }

    static void DebugDrawSliceStates(NativeHashMap<int, SliceStateData> map, float2 origin, float duration)
    {
        if (!map.IsCreated || map.Count() == 0)
            return;

        var kva = map.GetKeyValueArrays(Allocator.Temp);

        for (int i = 0; i < kva.Keys.Length; i++)
        {
            int key = kva.Keys[i];
            SliceStateData s = kva.Values[i];

            int2 cell = SliceGridUtil.DecodeKey(key);
            SliceGridUtil.CellBounds(cell, origin, out var min, out var max);

            // Base color by tactical state
            Color col = TacticalColor(s.State);

            // Draw the cell box
            DrawCell(min, max, col, duration);

            // Center tick scaled by intensity (SmoothedIntensity is 0..1)
            float mag = math.saturate(s.SmoothedIntensity);
            float2 center = (min + max) * 0.5f;
            DrawCellCenterTick(center, mag, col, duration);
        }

        kva.Dispose();
    }

    // Color based purely on tactical state
    static Color TacticalColor(SliceTacticalState state)
    {
        switch (state)
        {
            case SliceTacticalState.Empty:
                return new Color(0.3f, 0.3f, 0.3f, 0.6f); // grey

            case SliceTacticalState.Clash:
                return Color.yellow;                      // hot fight, no clear edge

            case SliceTacticalState.AllyAdvantage:
                return Color.green;                       // allies ahead

            case SliceTacticalState.EnemyAdvantage:
                return Color.red;                         // enemies ahead

            default:
                return Color.white;
        }
    }



    // Draw a square AABB using Debug.DrawLine
    static void DrawCell(float2 min, float2 max, Color color, float duration)
    {
        Vector3 bl = new Vector3(min.x, min.y, 0);
        Vector3 br = new Vector3(max.x, min.y, 0);
        Vector3 tr = new Vector3(max.x, max.y, 0);
        Vector3 tl = new Vector3(min.x, max.y, 0);

        Debug.DrawLine(bl, br, color, duration);
        Debug.DrawLine(br, tr, color, duration);
        Debug.DrawLine(tr, tl, color, duration);
        Debug.DrawLine(tl, bl, color, duration);

        // diagonal helps “see the fill”
        Debug.DrawLine(bl, tr, color, duration);
    }

    // Draw a little line at the cell center showing magnitude
    static void DrawCellCenterTick(float2 center, float mag01, Color color, float duration)
    {
        float tick = 0.25f + 0.75f * mag01; // 0.25..1.0 world units
        Vector3 a = new Vector3(center.x - tick * 0.5f, center.y, 0);
        Vector3 b = new Vector3(center.x + tick * 0.5f, center.y, 0);
        Debug.DrawLine(a, b, color, duration);
    }
}
