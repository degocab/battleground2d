// SliceGridSystem.cs
//
// What this does (super simple):
// - Imagine the battlefield is covered in big square tiles (“slices”).
// - Every frame, we look at every FormationGroup’s AABB (BoundsMin/BoundsMax).
// - We figure out which tiles that AABB overlaps.
// - We add “strength” into those tiles based on overlap area (so big overlap = bigger contribution).
// - Then we Debug.Draw the tiles so you can SEE a heatmap-ish grid.
//
// IMPORTANT: This system does NOT move units.
// It only reads formation group bounds and writes slice summary data.

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

// Utility functions for converting world positions <-> slice grid cells
public static class SliceGridUtil
{
    // This must be bigger than the max number of cells you’ll ever have along X
    // (Think of it as "grid width" for hashing.)
    public const int YMultiplier = 100000;

    // THIS IS YOUR SLICE SIZE DIAL.
    // Bigger = fewer slices, cheaper, coarser.
    // Smaller = more slices, more detail, more expensive.
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
// Put this late so it reads final positions after combat/movement.
// If you don’t have SetAnimationTypeSystem, you can change this to something late in your frame.
[UpdateAfter(typeof(SetAnimationTypeSystem))]
public partial class SliceGridSystem : SystemBase
{
    // Global map: sliceKey -> accumulated info (ally strength, enemy strength, etc.)
    public static NativeHashMap<int, SliceAccum> SliceMap;

    private EntityQuery _groupsQuery;

    // Where your grid starts in the world. Usually zero is fine.
    private static readonly float2 _origin = new float2(-32f, -16f);

    protected override void OnCreate()
    {
        // We only care about formation GROUPS (not units), because groups have BoundsMin/BoundsMax.
        _groupsQuery = GetEntityQuery(
            ComponentType.ReadOnly<FormationGroupComponent>(),
            ComponentType.Exclude<DeadTagComponent>()
        );

        // Pre-allocate space. If you see capacity issues later, increase this number.
        SliceMap = new NativeHashMap<int, SliceAccum>(4096, Allocator.Persistent);

        //_origin = float2.zero;
    }

    protected override void OnDestroy()
    {
        if (SliceMap.IsCreated) SliceMap.Dispose();
    }

    protected override void OnUpdate()
    {
        // Don’t run if the game isn’t actually playing.
        if (GetSingleton<GameStateComponent>().CurrentState != GameState.Playing)
            return;

        // Start fresh every frame (you can change to “every 0.25s” later).
        SliceMap.Clear();

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
            .ForEach((in FormationGroupComponent group) =>
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
                float strength = group.PriorGroupCount * (group.FormationType == FormationType.Phalanx
        ? 1.25f
        : 1.0f);
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

        // ============================
        // 2) DEBUG DRAW THE SLICES
        // ============================
        //
        // This draws only the cells that got any contributions.
        // Green = ally-dominant
        // Red   = enemy-dominant
        // Yellow= roughly even

        DebugDrawActiveSlices(SliceMap, _origin, 0.12f);
    }

    // Draw every active slice cell as a square, colored by control
    static void DebugDrawActiveSlices(NativeHashMap<int, SliceAccum> map, float2 origin, float duration)
    {
        if (!map.IsCreated || map.Count() == 0) return;

        // Get arrays of keys/values so we can iterate fast
        var kva = map.GetKeyValueArrays(Allocator.Temp);

        for (int i = 0; i < kva.Keys.Length; i++)
        {
            int key = kva.Keys[i];
            SliceAccum acc = kva.Values[i];

            // “Control” is -1 to +1:
            // +1 means all ally, -1 means all enemy
            float sum = acc.AllyStrength + acc.EnemyStrength;
            float control = (sum <= 1e-5f) ? 0f : (acc.AllyStrength - acc.EnemyStrength) / sum;

            Color col;
            if (control > 0.2f) col = Color.green;
            else if (control < -0.2f) col = Color.red;
            else col = Color.yellow;

            // Convert key -> cell coordinate -> world-space bounds
            int2 cell = SliceGridUtil.DecodeKey(key);
            SliceGridUtil.CellBounds(cell, origin, out var min, out var max);

            // Draw the square
            DrawCell(min, max, col, duration);

            // Optional: draw a small “strength bar” in the center
            float mag = math.saturate(sum / 10f); // tweak divisor to match your game scale
            DrawCellCenterTick((min + max) * 0.5f, mag, col, duration);
        }

        kva.Dispose();
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
