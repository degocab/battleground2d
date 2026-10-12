========================
PHASE 1 — FINALIZE COMBAT SYSTEM
========================

[x] Fix player only able to attack in original quadrant
[x] Fix player attacks not setting the right direction
[x] Fix attacks being delayed
[x] Replace all combat bools with CombatState / EntityState
[x] Replace isTakingDamage
[x] Replace isDefending
[x] Replace isBlocking
[x] Replace isAttacking
[x] Fix AI units taking damage animation looping
[x] Add block phase system
[x] Add player block
[x] Add AI block
[x] Fix AI blocking regardless of direction
[x] Setup death phase
[x] Delete dead entities
[x] Play death animation
[x] Fix sprite layer for dead units
[x] Fix units dying in idle or invalid states
[x] Make view direction independent of movement direction
[x] Update AI animation direction logic
[x] Update Player animation direction logic


========================
PHASE 2 — FORMATIONS & TACTICS (MVP)
========================

[x] Add FormationComponent
[x] Add FormationGroupComponent
[x] Create FormationCombatSystem
[x] Create FormationMovementSystem
[x] Implement Hold Position behavior
[x] Decouple unit commands from formations
[x] Move formation data ownership to FormationGroup
[x] Stop per-unit FindTarget before clash

[x] Implement basic Advance command
[x] Implement straight-line formation advance
[x] Implement basic attack-move logic

[ ] Implement basic Charge command
[ ] Require valid target for Charge
[ ] Add stamina / morale bonuses for Charge

[x] Add formation collision / avoidance
[x] Fix hard formation bugs (stuck units, NaNs, exit combat issues)
[x] Ensure formations can move into combat
[x] Ensure formations fight correctly
[x] Allow basic break-off / reposition behavior


========================
PHASE 3 — BATTLE LOOP & GLOBAL FRONTLINE (UPDATED)
========================

[x] FrontlineSlice grid accumulation (heatmap analytics from formation AABBs)

----------------------------------------
3A — SLICE STATE + FRONTLINE NARRATIVE
----------------------------------------
[x] Add persistent SliceState layer (separate from per-frame accumulation)
[x] Derive per-slice signals: smoothed control + intensity + momentum
[x] Define SliceState machine (e.g. Empty / Dominated / Contested + Stable / Pressured / Collapsing / Broken)
[x] Add hysteresis / time-gates to prevent flicker
[x] Neighbor influence rules (collapse/break spreads pressure to adjacent slices)
	[x] - review code that sets enemy/ally pressured state, it is only setting eenemy/ally dominant.
	[x] - review states to make sure it is clear what we want
[x] Offscreen update mode (cheaper tick + decay for unseen slices) - dont need this yet

----------------------------------------
3B-0 — CORE DATA FLOW STABILIZATION
----------------------------------------
[x] Remove shared multi-writer HasTarget usage
[x] Create CombatTarget component (entity-only target reference)
[x] Convert FindTargetSystem to write CombatTarget only
[x] Convert TargetValidationSystem to validate/clear CombatTarget
[x] Convert TargetReevaluationSystem to update CombatTarget without movement side effects
[ ] Remove cached TargetPosition usage from targeting flow
[x] Create movement goal input components / sources
	[x] FormationSlotGoal
	[x] OrderMoveIntent
	[-] future: PursuitIntent / ChargeIntent
[x] Create final MoveGoal component
[x] Implement MoveGoalResolverSystem
	[x] reads movement goal sources
	[x] can derive MoveGoal from CombatTarget position when behavior requires pursuit/charge
	[x] applies priority rules for final movement goal
[x] Make MoveGoalResolverSystem the only writer of MoveGoal
[x] Refactor MovementSystem to read MoveGoal only
[x] Add clean arrive/stop behavior
	[x] stop radius deadzone
	[x] no overshoot
	[x] no jitter near goal
[x] Add MovementLock (or equivalent) for combat stop/hold behavior
[x] Combat writes lock/state, not direct movement goals
[x] Remove HasTarget reads/writes from:
	[x] ProcessOrderSystem
	[x] FormationCombatSystem
	[x] targeting systems
	[x] movement systems
[x] Delete HasTarget component once migration is complete

----------------------------------------
3B — COMMANDS + LOCAL AWARENESS (ANTI-RTS)
----------------------------------------
[x] Create Command entity concept (persistent identity: commander + formations owned)
	- owner of formations
	- command-level state only

[x] Add dynamic Command label generation from slice context (role + pressure + area)
	- coarse battlefield identity
	- no gameplay dependency

[x] Generic Command-local awareness (player Command is the first test case)
	[x] configurable 14-unit observation radius centered on the physical commander
	[x] circle-versus-bounds intersection for slices and formations
	[x] live slice state, formation status, and FormationCaptainReport capture
	[x] CommandKnownSlice and CommandKnownFormation buffers
	[x] 10-second frozen stale memory with confidence decay
	[x] aggregate CommandAwareness summary for future tactical AI
	[x] global-debug-gated circle, known-slice, and known-formation visualization
	[x] create FormationCaptainReport (fake captain / informational only)
		- tied to formation
		- reads local slice state
		- reports upward to owned command
	[x] build CommandAwareness from direct observations + in-range formation reports
		- no global battlefield truth
		- player/AI only knows reported local state
	[ ] future: physical captain + messenger relay
		- real communication layer
		- not MVP

[ ] Global awareness: optional coarse “Left/Center/Right” pressure summary (delayed/stale)
	- aggregated slice pressure
	- intentionally not exact

[ ] AI Command awareness: formations-local slice reads + reaction delay (no omniscient instant flips)
	- AI reads CommandAwareness, not raw slices
	- nearby/local owned info only
	- delay before reacting
	- no instant battlefield-wide state changes
----------------------------------------
3C — FORMATIONS REGISTER + SLICE-INFLUENCED BEHAVIOR
----------------------------------------
[ ] Register formations into slices (area-of-operations window, not just nearest point)
[ ] Formation behaviors react to SliceState (stable/pressured/collapsing/broken)
[ ] Player presence modifier (nearby slice stabilizes faster / reduces rout chance)

----------------------------------------
3D — ORDERS AS INTENT (MVP VERSION)
----------------------------------------
[ ] Implement Command-level intents: Hold / Advance / Pull Back / Charge (MVP set)
[ ] Propagate intents to formations with small delay + optional staggering
[ ] Formation eligibility checks (e.g. Charge requires target/contact + cohesion; abort if collapsing)
[ ] Debug/telemetry: visualize current intent + formation compliance (for tuning)

----------------------------------------
3E — FULL BATTLE LOOP + RUN-THE-LINE
----------------------------------------
[ ] Battle state flow: Setup → Running → Victory/Defeat
[ ] Player can pan across entire battlefield / move between hotspots
[ ] Ensure visual continuity across slices (no popping or resets when crossing)
[ ] Minimal HUD: local slice strip + pressure state text/icons (readable while fighting)
[ ] AFK test: battle evolves meaningfully without player input
[ ] Run-the-line test: moving across battlefield shows continuous, coherent combat
========================
DECOUPLING PASS — OWNERSHIP + PIPELINES
========================

[ ] Audit `.Complete()` usage
    [ ] List every `Dependency.Complete()` / `.Complete()` in runtime systems (ignore debug/setup)
    [ ] Replace each with proper system ordering, ECB usage, or gather/apply job split

[ ] Define authoritative owners / pipelines (single-writer rule)
    [ ] Combat pipeline owns `CombatState.CurrentState` (only the state machine writes it)
    [ ] Damage pipeline owns `Health` and `Death/DiedTag`
    [ ] Motion / Physics pipeline owns `Translation`
    [ ] Slice pipeline owns `SliceState`
    [ ] Formation pipeline owns formation structural state (slots, cohesion, group state)

[ ] Ban cross-pipeline writes
    [ ] Player / AI / Formation systems do NOT write `Translation`
    [ ] Damage / Attack systems do NOT write `CombatState.CurrentState`
    [ ] Non-slice systems do NOT write `SliceState`
    [ ] Non-formation systems do NOT write formation structural state

[ ] Replace cross-system state writes with intents / events
    [ ] Add `MoveIntent` / `DesiredVelocity` (gameplay → physics)
    [ ] Add `DamageEvent` or `DamageReaction (timer)` (damage → combat state machine)
    [ ] Add `CombatIntent` (attack / block / defend) (control / AI → combat state machine)
    [ ] Ensure events are consumed and cleared by the owning resolve system

[ ] Physics “feel” confirmation
    [ ] Keep collision correction inside Motion / Physics pipeline (immediate translation correction allowed)
    [ ] Confirm no late gameplay systems assume post-collision positions unless explicitly ordered

[ ] Phase boundaries and ordering
    [ ] Document per-frame order:
        Input / Orders → Targeting → Combat → Damage / Death → Motion / Collision → Animation
    [ ] Enforce ordering via `[UpdateBefore]`, `[UpdateAfter]`, or custom system groups


========================
PHASE 4 — CO-OP & MESSAGING
========================

[ ] Add support for 2-player input
[ ] Share SectorSimulationSystem between players
[ ] Allow each player to issue formation commands
[ ] Sync sector and formation updates between players

[ ] Create MessageEntity
[ ] Add message source player
[ ] Add message destination player
[ ] Add message payload (help / retreat / attack)
[ ] Attach message to current sector

[ ] Implement AI messenger units
[ ] Spawn messenger on message send
[ ] Allow messenger to travel sector-to-sector
[ ] Allow messenger to die or be delayed
[ ] Deliver UI notification on arrival

[ ] Support player-as-messenger traversal


========================
PHASE 5 — UNIT VARIETY & AI COMMANDER
========================

[ ] Add unit type enum (infantry, cavalry, archer, etc.)
[ ] Create AI Commander system
[ ] Reinforce weak sectors
[ ] Initiate pushes
[ ] Retreat collapsing sectors
[ ] Shift reserves intelligently

[ ] Add archer units
[ ] Implement basic projectile logic
[ ] Integrate archers with combat + sector systems


========================
PHASE 6 — PERFORMANCE & SCALING
========================

[ ] Separate combat systems by responsibility
[ ] Optimize targeting queries
[ ] Optimize movement systems
[ ] Optimize collision systems
[ ] Introduce group-based physics
[ ] Add corpse persistence system

[ ] Create UnitData GPU struct
[ ] Implement initial GPU collision compute shader
[ ] Create GPUCollisionSystem
[ ] Integrate GPU collisions with CPU quadrant system
[ ] Replace CPU collision resolution with GPU results
[ ] Ensure rendering uses post-GPU positions

[ ] Implement GPU grid broad-phase
[ ] Implement collision-pair compaction
[ ] Remove CPU readback bottlenecks
[ ] Keep full physics loop on GPU
[ ] Add pressure-wave / organic motion behaviors


========================
PHASE 7 — COMBAT FEEL & POLISH
========================

[ ] Smooth attack animations
[ ] Smooth block animations
[ ] Smooth death animations
[ ] Fix attack direction & hitboxes
[ ] Add unit crit chance
[ ] Add defend-while-moving state
[ ] Fix units freezing during attack


========================
PHASE 8 — ADVANCED FEATURES & CINEMATIC MOMENTS
========================

[ ] Add cavalry charge impact physics
[ ] Improve arrow physics
[ ] Add environmental / background design
[ ] Add dynamic rank promotion
[ ] Add advanced radial formation commands
[ ] Add commander / captain crit systems
[ ] Add backwards-walking animations
