# Battleground2D — Complete Conversation Handoff

> This document consolidates the recent Battleground2D architecture discussions so the work can continue in Codex without relying on the original chat transcript.
>
> `Battleground2D_Architecture.md` and `Roadmap.md` remain the project's source of truth. When this handoff records a newer decision that conflicts with those files, the conflict is called out explicitly and the living documents should be updated.

---

## 1. Project role and working agreement

Battleground2D is a Unity DOTS, top-down 2D, large-scale battle simulation. The assistant/Codex should act as a senior gameplay and engine architect.

The assistant should:

- Challenge architectural decisions when appropriate instead of simply agreeing.
- Favor clean ECS ownership, maintainability, and long-term scalability over quick implementation.
- Apply performance-first ECS design.
- Enforce the single-writer rule.
- Follow Pragmatic Programmer principles: simple over clever, small systems, eliminate duplication, refactor continuously, and build for change.
- Treat `Battleground2D_Architecture.md` as a living document and update it whenever an important architectural or gameplay decision is accepted.
- Treat `Roadmap.md` as the living implementation checklist.
- Clearly distinguish implemented code, discussed design, and future ideas.

---

## 2. Game vision

The goal is a massive-scale, cinematic battle simulator in which the player commands from inside the battle rather than behaving like an omniscient RTS camera.

The battle should feel:

- Epic and cinematic.
- Chaotic but understandable locally.
- Tactical and believable.
- Dynamic rather than static.
- Emergent rather than heavily scripted.
- Capable of continuing and evolving without player input.

The intended hierarchy is:

```text
General
  ↓
Command
  ↓
Captain
  ↓
Formation
  ↓
Unit
```

Responsibilities:

- **General:** overall strategy.
- **Command:** battlefield-level interpretation and decisions.
- **Captain:** local observation and reports.
- **Formation:** tactical execution.
- **Unit:** movement, combat, and animation.

Inspirations:

- **Battle of Gaugamela:** enormous scale, coordinated commands, reserves, flanks, breakthroughs, adaptation, and leadership determining the battle.
- **Alexander (2004):** Alexander personally leading decisive attacks, commanders participating physically, elite guards following and protecting commanders, and large cinematic maneuvers.
- **Battle of the Bastards:** formations collapsing into dense chaos, soldiers becoming surrounded, a shifting frontline, and local confusion in which nobody understands the whole battlefield.

Long-term scale is thousands of units, eventually reaching tens or hundreds of thousands if technically feasible. Earlier performance testing was stable around 6,000 units at roughly 30 FPS on low-end hardware, while 20,000 units had previously become unusably slow. The aspirational scale has been discussed as 100,000+ and potentially around 240,000, but architecture and profiling must decide what is practical.

The central gameplay idea can be summarized as **dynamic battles**: simple interacting systems should create changing pressure, collapses, counterattacks, breakthroughs, and cinematic stories.

---

## 3. Architectural principles already established

### 3.1 Single-writer ECS

Every authoritative component must have one owner/writer.

Examples:

| Component/data | Authoritative writer |
|---|---|
| `CombatState` | Combat state machine |
| `Health` and death state | Damage/death pipeline |
| `Translation` / physical position | Movement or physics pipeline |
| `MoveGoal` | `MovementGoalResolverSystem` |
| `SliceState` | Slice pipeline / `SliceGridSystem` |
| Formation structural data | Formation pipeline |
| `CommandAwareness` | `CommandAwarenessSystem` |

Other systems communicate by writing intents, requests, events, or source data. They must not bypass the owning resolver.

### 3.2 Intent pipeline

The desired general flow is:

```text
Player / AI
    ↓
Orders
    ↓
Order processing
    ↓
Intents
    ↓
Resolvers
    ↓
Movement / combat / gameplay systems
```

High-level gameplay systems should state what they want, not directly manipulate low-level execution state.

### 3.3 Small responsibilities

Each system should answer one main question. If the responsibility can naturally be described with “and,” it should be examined for a split.

### 3.4 Formations are the tactical objects

Formations—not individual units—are the primary objects for command-level tactics. A formation owns or coordinates:

- Cohesion.
- Shape and slots.
- Positioning.
- Advance.
- Charge.
- Defend/hold.
- Break-off or withdrawal.
- Reaction to local pressure.

Units execute formation and combat behavior but should not independently become command-level thinkers.

---

## 4. Existing major systems and current state

### 4.1 Combat — complete/stable foundation

The combat work already includes:

- `CombatState` replacing many combat booleans.
- States such as Idle, Seeking, Attacking, Defending, Blocking, TakingDamage, and Dying.
- Player and AI block behavior.
- Directional attack/block corrections.
- Attack resolution.
- Damage application.
- Death handling, animation, sprite ordering, and entity deletion.
- View direction separated from movement direction.
- AI and player animation-direction fixes.

The combat pipeline is conceptually:

```text
Targeting
  ↓
Combat intent
  ↓
Combat state machine
  ↓
Attack resolution
  ↓
Damage
  ↓
Death
  ↓
Animation
```

A prior Defend behavior detail was that entering Defend starts a five-second cooldown/timer, while invalid or out-of-range targets can return the unit to Seeking. This should be checked against current code before relying on it.

### 4.2 Formations — MVP mostly complete

Implemented or substantially working:

- `FormationComponent`.
- `FormationGroupComponent`.
- Formation group ownership of formation-level data.
- `FormationMovementSystem`.
- `FormationCombatSystem`.
- Hold Position.
- Basic Advance.
- Straight-line formation movement.
- Basic attack-move.
- Formation collision/avoidance.
- Break-off/reposition behavior.
- Fixes for stuck units, NaNs, combat exits, and formation entry into combat.
- Removal of pre-clash per-unit target searches.

Still listed as incomplete in the current Roadmap:

- Basic Charge.
- Charge requiring a valid target.
- Stamina/morale benefits for Charge.

### 4.3 Slice/frontline system — implemented truth/telemetry foundation

The battlefield is divided into `8 × 8` logical grid slices. Previously discussed grid details included an origin around `(-32, -16)` and a key scheme using a large Y multiplier such as `100000`; current code remains authoritative.

Per-frame accumulation tracks values such as:

- Allied strength/presence.
- Enemy strength/presence.
- Intensity.

Persistent slice state smooths and remembers battlefield conditions:

- Control.
- Intensity.
- Momentum.
- Tactical state.

Known tactical-state names discussed include `Empty`, `Clash`, `AllyAdvantage`, and `EnemyAdvantage`. Older roadmap language also mentions Dominated/Contested and Stable/Pressured/Collapsing/Broken; the active enum in code must be treated as authoritative.

Previously discussed tuning constants included:

```text
ControlSmoothing          = 0.25
IntensitySmoothing        = 0.25
InactiveDecayRate         = 0.85
PruneAfterSeconds         = 2.5
PruneIntensityThreshold   = 0.05
StateUpdateInterval       = 0.15
PresenceMin               = 0.5
ClashPresenceMin          = 5
AdvantageControlMin       = 0.25
MomentumThreshold         = 0.05
```

These are historical discussion values and must be verified against the current source before modification.

The slice pipeline also includes smoothing, time gates/hysteresis, decay/pruning, and neighbor influence so pressure does not flicker and local collapse can influence nearby areas.

---

## 5. Core targeting and movement cleanup

The Phase 3B-0 cleanup established distinct ownership for target references and final movement goals.

### 5.1 Target ownership

- `CombatTarget` stores only the target entity reference.
- `FindTargetSystem` writes/assigns `CombatTarget`.
- `TargetValidationSystem` validates or clears `CombatTarget`.
- `TargetReevaluationSystem` may change `CombatTarget` without producing movement side effects.
- The old shared `HasTarget` component was removed from order, formation combat, targeting, and movement systems and then deleted.

### 5.2 Movement sources and resolution

Movement inputs/sources include:

- `FormationSlotGoal`.
- `OrderMoveIntent`.
- A target position derived from `CombatTarget` when current behavior allows pursuit or charge.
- Possible future `PursuitIntent` and `ChargeIntent`.

The final movement pipeline is:

```text
FormationSlotGoal ─┐
OrderMoveIntent ───┼─→ MovementGoalResolverSystem → MoveGoal → MovementSystem
CombatTarget ──────┘
```

Rules:

- `MovementGoalResolverSystem` is the only writer of `MoveGoal`.
- Movement reads only `MoveGoal`.
- Arrive/stop behavior includes a dead zone, avoids overshoot, and avoids jitter near the destination.
- `MovementLock` or equivalent lets combat/hold behavior stop motion without other systems directly erasing or overwriting movement goals.
- Combat writes combat/lock state, not `MoveGoal` directly.

### 5.3 Remaining discrepancy

The current `Roadmap.md` still marks removal of cached `TargetPosition` from the targeting flow as incomplete, even though a later conversation summary described Phase 3B-0 as complete. Until the code proves otherwise, this item remains **not confirmed complete**.

---

## 6. Order-system cleanup discussion

The important distinction was:

```text
Order = what the commander/formation was told to accomplish
Target acquisition = an internal operation needed to accomplish it
Behavior = how the formation currently carries it out
```

### 6.1 `Attack` versus `FindTarget`

- `Attack` is the real order.
- `FindTarget` is an internal targeting operation, not a player-facing or commander-level order.
- Issuing Attack leaves the active order as Attack.
- If a unit/formation executing Attack lacks a valid `CombatTarget`, `FindTargetSystem` locates one.
- Target acquisition must never silently replace the active Attack order with a FindTarget order.
- Combat and movement systems react to the acquired `CombatTarget` according to current behavior.

### 6.2 Cleaned factory direction

The desired `OrderFactory` surface was:

```csharp
CreateIdleOrder()
CreateMoveOrder(...)
CreateAttackOrder()
CreateDefendOrder(...)
CreateMarchOrder()
CreateChargeOrder()
CreateMoveDirectionalOrder(...)
```

The exact current signatures must be taken from the repository.

### 6.3 Input mapping discussed

The cleaned test/input mapping was approximately:

| Index | Resulting order |
|---:|---|
| 0 | Charge |
| 1 | March |
| 2 | MoveDirectionalRange |
| 3 | Defend |
| 4 | MoveTo |
| 5 | Idle |
| 6 | Attack |
| 7 | MoveTo/custom movement |
| 8 | Attack/custom attack |

This mapping was described as approximate because the source method and key bindings may have changed.

### 6.4 Status

The conceptual cleanup is accepted, but the current code should be inspected before claiming every old `FindTarget` order path or legacy factory method is gone. Orders-as-Intent remains scheduled for Phase 3D and should not be expanded prematurely while the awareness boundary is being established.

---

## 7. Formation Captain reporting and local tactical state

`FormationCaptainReport` has been created as an MVP informational layer. The captain does not need to be a physical character yet.

Its role is:

- Be tied to a formation/formation group.
- Observe the formation’s local area of responsibility.
- Summarize local slice evidence.
- Report upward to an owned Command.
- Avoid making command decisions itself.

A reporting system was discussed as scanning each formation group, using its formation position/bounds or area-of-responsibility AABB, gathering relevant slices, calculating a local summary, and writing only that formation’s report. Commander knowledge filtering belongs to the later Command awareness layer, not the captain report producer.

### 7.1 Captain states

States discussed include:

- `Idle`
- `Holding`
- `Winning`
- `SlightEdge`
- `Pressured`
- `Collapsing`
- `Broken`
- `Unknown`

An early control-based classification used thresholds approximately like:

```text
control >=  0.75 → Holding
control >=  0.35 → Winning
control >=  0.10 → SlightEdge
control >  -0.10 → Holding
control >  -0.35 → Pressured
control >  -0.75 → Collapsing
otherwise         → Unknown
```

Feedback was that Collapsing occurred too early and a fight could begin as Unknown. The revised behavior must account for intensity and presence, not only control:

- If there is no meaningful intensity/presence, use `Idle`.
- Do not declare Collapsing too early.
- Avoid `Unknown` merely because a clash has just started.
- `Broken` should represent a genuinely failed formation, not just a locally negative control number.

The exact final thresholds are not confirmed in this handoff and should be read from current code.

### 7.2 Desired report contents

The report should contain enough information to preserve who observed what, where, and when. A discussed shape was:

```csharp
public struct FormationCaptainReport : IComponentData
{
    public Entity Command;
    public Entity Formation;

    public float2 ReportPosition;
    public FormationCaptainState State;

    public float Control;
    public float Intensity;
    public float Momentum;

    public double ObservedTime;
}
```

The report position should come from the formation anchor, bounds center, or another formation-level representative point—not an arbitrary individual unit.

### 7.3 Future extension

The current report is informational and effectively immediate once its delivery condition is satisfied. Future physical captains and messenger units should change **delivery**, not force a redesign of observation, memory, awareness, or AI consumers.

---

## 8. AI tactical decision factory discussion

The key separation is:

```text
Order = what the formation must accomplish
Captain state/report = what is happening locally
Tactical decision/behavior = how it currently attempts the order
```

For example, an Attack order under pressure remains Attack; it is executed defensively. The formation should not mutate its high-level order every time local pressure changes.

### 8.1 Attack MVP mapping

The discussed/implemented Attack behavior mapping was:

| Captain state | Tactical decision | Formation-group result |
|---|---|---|
| `Broken` | `Retreat` | `FormationGroupStatus.Broken` |
| `Collapsing` | `FightingWithdrawal` | `Disengaging` |
| `Pressured` | `DefensiveAttack` | `Engaged` |
| Other states | `NormalAttack` | `Engaged` |

### 8.2 Structure discussed or substantially implemented

- `TacticalDecisionFactory`.
- `AttackDecisionFactory`.
- `FormationBehaviorFactory`.
- `FormationTacticalDecision` ECS component.
- Decision-processing system.
- Minimal behavior-execution system.
- `FormationBehavior` components/systems.
- Optional formation-spawner initialization.

A later commit-style summary indicated this work was substantially implemented, including:

- FormationCaptain behavior-decision pipeline.
- Order-specific tactical-decision factories.
- Reusable formation behavior architecture.
- Captain-state evaluation.
- `FormationBehavior` components.
- Alive-unit/intensity cleanup.
- Expanded formation debugging and visualization.
- Slice refinements.
- Spawn/test configuration changes.
- General ECS cleanup.

However, current repository code is still the authority for exactly which types and mappings exist.

### 8.3 Correct future input

The tactical decision factory remains valid, but command-level AI must ultimately receive information through the imperfect-knowledge pipeline. It must not query the global slice map directly.

Formation-local behavior may use its own captain/local state, while Command-level planning uses `CommandAwareness` and produces orders.

---

## 9. Critical architecture correction: truth versus knowledge

The current `Battleground2D_Architecture.md` says:

> “Slices represent local battlefield knowledge, not objective truth.”

The newer accepted design separates objective simulation evidence from perceived knowledge:

```text
SliceState
  = objective simulation telemetry / battlefield evidence

FormationCaptainReport
  = local observation of that evidence

CommandKnownSlice + CommandAwareness
  = delayed, incomplete, stale perceived knowledge
```

The complete pipeline is:

```text
SliceState (simulation truth)
        ↓
Direct command observation + FormationCaptainReport
        ↓
Delivery, ownership, range, and age filtering
        ↓
CommandKnownSlice memory
        ↓
CommandAwareness summary
        ↓
AI Decision Factory
        ↓
Orders
        ↓
Formation tactical behavior
        ↓
Units
```

Why this separation matters:

- If slices themselves are “knowledge,” truth and perception become mixed.
- Messenger delays become difficult to model.
- Stale intelligence becomes ambiguous.
- Missing reports and commander isolation become awkward.
- UI may accidentally expose omniscient data.
- AI may accidentally read live global truth.

Therefore, `SliceState` should remain an objective internal simulation layer, while observation/report/delivery/memory create imperfect knowledge.

**Document discrepancy:** the attached Architecture file has not yet incorporated this correction even though a previous response said it had been updated. It should be corrected during the next Architecture update.

---

## 10. `CommandAwareness` is generic and primarily supports AI Commands

The first awareness proposal focused too much on the player. The corrected decision is:

> `CommandAwareness` is a shared capability for every Command—player or AI. The AI is its primary gameplay consumer. Player debug visualization and future UI are alternate views of the same knowledge data.

Rules:

- The awareness system processes all Commands, not only entities with `PlayerTag`.
- The awareness radius belongs to the physical commander/command representative, not specifically to the player.
- Each Command has its own origin, radius, observations, reports, memory, confidence, and summary.
- AI Commands use their own local radius and delivered reports to make decisions.
- AI decision systems read `CommandAwareness`, never the raw global `SliceStateMap`.
- Owning a formation grants authority to command it; ownership does **not** grant live knowledge of everything around it.
- A distant owned formation must not turn into an RTS scouting camera.
- The player physically sees the nearby battlefield, so the player may need less artificial visualization during ordinary play; the data still powers summaries, reports, and optional tactical UI.

Generic flow:

```text
Physical commander position
        ↓
Slices inside direct-observation radius
        +
Delivered captain reports
        +
Stale remembered information
        ↓
CommandAwareness
        ↓
AI planning / player information
```

Only the player Command currently exists, so it will be used to build and test the generic system. It is effectively standing in as the first “AI Command” test case.

---

## 11. Proposed awareness data model

Names and exact storage can still be adjusted to match the installed Unity Entities version, but the responsibilities should remain separate.

### 11.1 Per-command configuration

```csharp
public struct CommandAwarenessConfig : IComponentData
{
    public float DirectObservationRadius;
    public float ReportDeliveryRadius;
    public float MemoryDuration;
}
```

Starting tuning values discussed:

- Direct observation radius: approximately `40` world units.
- Report delivery radius: approximately `48` world units.
- Memory duration: approximately `10` seconds.

With `8 × 8` slices, a 40-unit radius reaches roughly five slices outward.

These are starting test values, not final game balance.

### 11.2 Direct observation

An early player-specific name was `PlayerSliceObservation`. Because the design is now generic, a better name may be `CommandSliceObservation`.

Possible transient buffer element:

```csharp
public struct CommandSliceObservation : IBufferElementData
{
    public int SliceKey;
    public float Control;
    public float Intensity;
    public float Momentum;
    public double ObservedTime;
}
```

Responsibilities:

- Produced from each Command’s physical position and direct-observation radius.
- Contains only currently observed slices.
- Is transient/source data, not long-term memory.
- Has a single observation-system writer.

### 11.3 Persistent known-slice memory

```csharp
public struct CommandKnownSlice : IBufferElementData
{
    public int SliceKey;

    public float Control;
    public float Intensity;
    public float Momentum;

    public double LastObservedTime;
    public AwarenessSource Source;
    public float Confidence;
}
```

Possible source enum:

```csharp
public enum AwarenessSource : byte
{
    DirectObservation,
    CaptainReport,
    Memory
}
```

This buffer represents what a specific Command currently believes about known areas. It supports AI reasoning, debugging, and future player UI.

### 11.4 Command-level summary

```csharp
public struct CommandAwareness : IComponentData
{
    public CommandPressureState Pressure;

    public float Control;
    public float Intensity;
    public float Momentum;
    public float Confidence;

    public int KnownSliceCount;
    public int ReceivedReportCount;
    public double LastUpdatedTime;
}
```

This is the coarse summary consumed by command-level AI and decision factories.

`CommandAwarenessSystem` should be the only writer of persistent `CommandKnownSlice` knowledge and the final `CommandAwareness` summary.

### 11.5 Possible system separation

A clean split would be:

1. `CommandObservationSystem`
   - Reads Command position/config and objective `SliceState`.
   - Writes current direct observations only.

2. `FormationCaptainReportSystem`
   - Reads formation-local slice evidence.
   - Writes each formation’s latest local report.

3. `CommandAwarenessSystem`
   - Determines which reports are delivered.
   - Merges direct observations, delivered reports, and old memory.
   - Updates confidence and age.
   - Writes `CommandKnownSlice` and `CommandAwareness`.

4. Debug/UI systems
   - Read knowledge only.
   - Never mutate awareness and never read global truth for player-facing information.

This split preserves single-writer ownership and makes messenger delivery replaceable later.

---

## 12. Position, visibility, delivery, and memory rules

### 12.1 Awareness origin

Use the physical commander entity’s position. The abstract Command entity may own identity and state, but the physical representative provides the observation origin.

The mapping between Command and physical commander must be explicit, for example through an entity reference. Do not infer the player only through a global singleton because future battles will contain multiple Commands.

### 12.2 Direct observation intersection

Test whether the observation circle intersects a slice’s bounds, rather than checking only the slice center. Center-only tests cause abrupt popping near boundaries.

Later hysteresis could use different enter/leave distances, for example:

- Enter visibility at 40 units.
- Leave visibility at 44 units.

### 12.3 Captain-report delivery MVP

A captain report is delivered when:

```text
Report belongs to the Command
AND report/formation is within communication distance of the commander
AND report is recent enough
```

For the MVP, proximity substitutes for actual communication. A nearby captain can tell the commander what is happening in that formation’s area of responsibility, extending knowledge beyond direct personal observation.

A distant owned captain may still generate a local report, but the Command does not receive fresh information until a delivery condition is met.

Future replacement:

```text
MVP delivery: captain is in communication radius
Future delivery: messenger physically reaches commander
```

The rest of `CommandAwareness` should remain unchanged.

### 12.4 Merge precedence

When several sources describe the same area:

1. Newer direct observation.
2. Fresh delivered captain report.
3. Existing/stale memory.
4. Unknown.

Important nuance: source type alone should not blindly overwrite newer information. A report should not replace a newer direct observation, and an older direct observation should not replace a newer delivered report. Recency and source confidence both matter.

### 12.5 Stale knowledge

When a slice leaves observation and no report refreshes it:

- Preserve the last known values.
- Mark the source as memory or derive memory from age.
- Lower confidence over time.
- Do not continue reading/updating it from objective `SliceState`.
- After `MemoryDuration`, remove it or mark it Unknown.

This creates the desired “run the line” experience. A commander can inspect one part of the battlefield and remember what was happening, but cannot know that the situation remained unchanged after leaving.

### 12.6 No global omniscient summary

Older plans mention a coarse Left/Center/Right global summary. The more recent constraint is:

- No exact global awareness in normal gameplay.
- A Left/Center/Right summary, if retained before messenger systems exist, should be debug/dev-only.
- A gameplay summary may exist later only when produced by delayed/stale reports or messenger delivery.
- Player and AI should not use an omniscient battlefield-wide summary.

---

## 13. Debugging plan: use the player Command as the first AI Command

Because only the player Command exists, use it to validate the generic Command awareness system.

The debug view should intentionally show:

1. The physical commander origin.
2. The direct-observation radius as a debug circle.
3. Slices currently directly observed.
4. Areas known through delivered captain reports.
5. Remembered/stale slices and their age/confidence.
6. Unknown areas.
7. The final coarse `CommandAwareness` values/state.

Suggested visual distinctions:

| Knowledge state | Debug treatment |
|---|---|
| Direct observation | Bright, accurate outline/color |
| Captain report | Different color, icon, or marker |
| Recent memory | Faded/desaturated |
| Old memory | Heavily faded or `?` |
| Unknown | Hidden or optional debug-only outline |

The global game-manager debug toggle discussed elsewhere should gate `Debug.Draw`, gizmos, and debug text so visualization can be toggled during Inspector play mode.

The debug rendering must be a reader only. It must not become the owner of awareness data or perform hidden perception calculations.

---

## 14. Future player-awareness UI

The debug visualization is also a prototype for a gameplay feature: showing the player what their Command currently knows.

The dependency should be:

```text
CommandAwareness / CommandKnownSlice
        ├── AI decision systems
        ├── Debug visualization
        └── Player awareness UI
```

Do not convert `Debug.Draw` code directly into gameplay UI. Both debug and UI should independently read the same knowledge components.

Possible gameplay UI uses:

- A tactical map showing known local areas.
- A battlefield overlay.
- A local pressure/status strip.
- Captain report icons or a diegetic report feed.
- Fresh information shown brightly.
- Older intelligence fading/desaturating.
- Unknown regions hidden.
- A visible distinction between personally observed information and captain-reported information.

The UI must never read raw `SliceState`, because doing so would reveal objective battlefield truth and break the anti-RTS design.

The player already physically sees nearby fighting, so the UI should explain knowledge and reports rather than redundantly covering the entire combat view in debug colors.

---

## 15. First playable validation scenario

Set up three formations:

1. One fighting beside the player/commander.
2. One fighting outside direct observation but close enough for its captain report to be delivered.
3. One fighting far from the commander and outside report-delivery range.

Expected results:

- Nearby slices appear as live direct observations.
- The nearby captain expands the Command’s knowledge beyond personal observation.
- The distant formation does not provide live Command knowledge merely because it is owned.
- Walking away causes knowledge to become stale rather than continuing to update.
- Confidence visibly declines with age.
- Expired information becomes Unknown or disappears after the configured memory duration.
- Returning to an area immediately refreshes direct observations.
- Moving close enough to the distant captain/formation finally delivers or refreshes its information.
- The coarse `CommandAwareness` output changes only from information the Command legitimately knows.

Future AI validation:

- Create an AI Command with the same components.
- Give it a physical commander position.
- Confirm it gets different knowledge than the player when positioned elsewhere.
- Confirm its decision factory reacts to `CommandAwareness` after a deliberate reaction delay.
- Confirm it does not react instantly to distant raw-slice changes.

---

## 16. Recommended implementation order

The immediate work should establish the awareness boundary before expanding Orders-as-Intent.

1. Correct the architecture language: `SliceState` is objective telemetry; reports and awareness are imperfect knowledge.
2. Confirm the existing `FormationCaptainReport` fields and single writer.
3. Add `CommandAwarenessConfig` to the Command prefab/spawn path.
4. Add an explicit reference from each Command to its physical commander/awareness origin.
5. Add a generic current-observation buffer such as `CommandSliceObservation`.
6. Implement `CommandObservationSystem` for every Command, not just `PlayerTag`.
7. Use circle-versus-slice-bounds intersection and draw the debug radius.
8. Ensure captain reports contain origin, observed time, control, intensity, momentum, and state as needed.
9. Implement MVP report-delivery filtering by ownership, proximity, and freshness.
10. Add persistent `CommandKnownSlice` memory.
11. Implement merge precedence and confidence decay.
12. Aggregate legitimate known information into `CommandAwareness`.
13. Make `CommandAwarenessSystem` the sole writer of final knowledge/summary data.
14. Add debug visualization for direct observation, reports, memory, unknown areas, and the coarse summary.
15. Run the three-formation playable test.
16. Add a second AI Command and verify the system needs no redesign.
17. Connect command-level AI decision logic to `CommandAwareness` with a reaction delay.
18. Later connect player gameplay UI to `CommandKnownSlice`/`CommandAwareness`.
19. Resume the Phase 3D Orders-as-Intent expansion after awareness ownership is stable.

---

## 17. What is implemented, discussed, and not yet confirmed

### Confirmed by the current Roadmap

- Combat phase tasks are complete.
- Formation MVP is substantially complete except Charge work.
- Persistent slice/frontline state is complete.
- Targeting/movement ownership cleanup is mostly complete.
- Command entity concept exists.
- Dynamic Command label generation exists.
- `FormationCaptainReport` exists.

### Discussed or apparently substantially implemented, but must be checked in code

- Tactical decision factories and formation behavior pipeline.
- Attack decision mapping by captain state.
- Captain-state thresholds and intensity handling.
- Expanded captain/formation debug visualization.
- Exact `OrderFactory` cleanup.
- Exact removal of `FindTarget` as an order type.
- Whether cached `TargetPosition` is fully removed.

### Not implemented/unfinished according to the current Roadmap and latest discussion

- Generic per-Command direct observation radius.
- Persistent known-slice memory and staleness.
- Report-delivery filtering.
- Final `CommandAwareness` aggregation.
- AI Command reaction delay.
- AI Command using `CommandAwareness` as its only command-level battlefield input.
- Gameplay player-awareness UI.
- Physical captains and messenger delivery.
- Gameplay-safe delayed/stale global summaries.
- Phase 3D Command intents and propagation.
- Phase 3E full battle loop.

---

## 18. Open implementation decisions that should be resolved from code or ADRs

These items were not fully settled and should not be guessed silently:

1. **Command-to-physical-commander link:** exact component/entity-reference structure.
2. **Observation storage:** transient dynamic buffer versus temporary native collection feeding the awareness writer.
3. **Known-slice storage:** per-Command dynamic buffer versus a centralized map keyed by `(Command, SliceKey)`; start simple unless profiling proves a problem.
4. **Report granularity:** one formation summary versus per-slice report entries for a formation’s area of responsibility.
5. **Report delivery distance:** commander-to-captain point distance versus commander-to-formation-bounds distance.
6. **Confidence formula:** linear decay, stepped freshness bands, or source-dependent decay.
7. **Memory expiration:** delete entries or retain Unknown records for UI history/debugging.
8. **Summary weighting:** control weighted by intensity, formation strength, confidence, age, or a combination.
9. **Pressure classification:** exact thresholds and hysteresis for Stable/Pressured/Collapsing/Broken.
10. **Reaction delay:** fixed interval versus per-Command planner cooldown/jitter.
11. **Captain report cadence:** every slice update, fixed interval, or only on meaningful state change.
12. **Current Unity API constraints:** the project uses an older Entities/DOTS version, so proposed modern-looking sample types/APIs must be adapted to the repository rather than copied blindly.

---

## 19. Architecture constraints for all future code

- Never let AI command logic read the global slice map directly.
- Never let player UI read global objective slice truth.
- Never equate ownership with live knowledge.
- Never let target acquisition replace the active Attack order.
- Never let behavior changes rewrite the strategic purpose of an order without an explicit higher-level decision.
- Never introduce multiple writers for `MoveGoal`, `CombatState`, `SliceState`, `CommandKnownSlice`, or `CommandAwareness`.
- Avoid per-unit work for command awareness; query Commands, formation reports, and slice windows.
- Keep debug systems read-only.
- Make messenger units a delivery-layer replacement, not an awareness-system rewrite.
- Preserve stale knowledge rather than granting magical live updates.
- Prefer formation-level aggregation and fixed/limited update cadences for performance.
- Do not jump into the full Orders-as-Intent Phase 3D until the Command awareness input boundary is stable.

---

## 20. Exact point to resume

The immediate next task is:

```text
Existing FormationCaptainReport
        +
Generic Command-local direct observation
        ↓
CommandAwarenessSystem
        ↓
CommandKnownSlice memory + CommandAwareness summary
```

The player Command is the initial test Command. Draw its awareness radius and knowledge states to validate the generic system, but do not write player-specific architecture.

Before implementing, inspect the current versions of:

- Command components and spawn/ownership code.
- The physical player/commander entity reference and position component.
- `FormationCaptainReport`.
- `FormationCaptainReportSystem`.
- Slice state structs and the slice-state map.
- Existing command perception/awareness components, if any.
- Tactical decision factories and formation behavior components.
- Current `OrderType`, `OrderData`, `OrderFactory`, and `ProcessOrderSystem`.
- Debug toggle and current gizmo/debug drawing conventions.

Then implement the smallest generic vertical slice:

1. One Command.
2. One physical awareness origin.
3. One observation radius.
4. Live observed slices.
5. Debug circle and slice coloring.
6. No AI decisions yet.

After direct observation works, add captain delivery, stale memory, final aggregation, and AI consumption in separate steps.

---

## 21. Paste-ready continuation instruction

Use this prompt with the project files and repository:

> Continue Battleground2D from the conversation handoff. Treat `Battleground2D_Architecture.md` and `Roadmap.md` as source-of-truth living documents, but apply the newer accepted correction that `SliceState` is objective simulation telemetry while captain reports and `CommandAwareness` represent imperfect knowledge. The immediate target is a generic Command awareness system for both player and AI Commands. Use the existing player Command only as the first debug/test Command. First inspect the current Command, captain-report, slice, order, and behavior code; then propose the smallest single-writer implementation that visualizes the Command radius and directly observed slices without introducing player-only architecture or omniscient AI.

