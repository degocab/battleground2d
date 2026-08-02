# Battleground2D --- Architecture & Vision

> Living architecture document. Update this whenever a major
> architectural or gameplay decision is made.

------------------------------------------------------------------------

# Vision

Battleground2D is a **large-scale cinematic 2D battle simulator**
focused on commanding from within the battle instead of acting as an
all-seeing RTS player.

The goal is to create battles that feel **epic, chaotic, tactical, and
believable**, where the player experiences the confusion and momentum of
historical warfare while still influencing its outcome through
leadership.

## Inspirational References

### The Battle of Gaugamela (331 BC)

-   Massive armies fighting across a wide battlefield.
-   Command hierarchy and coordinated maneuvers.
-   Reserves, flanking, breakthroughs, and battlefield adaptation.
-   Leadership determines victory more than individual combat.

### *Alexander* (2004)

-   Cinematic scale.
-   Alexander personally leading decisive cavalry charges.
-   Elite guards protecting commanders while allowing them to lead from
    the front.
-   Battles that evolve instead of remaining static.

### Battle of the Bastards (*Game of Thrones*)

-   Dense formations collapsing into chaos.
-   Dynamic front lines.
-   Units becoming surrounded.
-   Local confusion where no one understands the entire battlefield.
-   The player should occasionally feel overwhelmed while still finding
    opportunities to influence the fight.

------------------------------------------------------------------------

## Long-Term Vision

The ultimate goal is to simulate **truly massive battles** while
maintaining believable tactics and performance.

The battlefield should support:

-   Thousands to eventually hundreds of thousands of units.
-   Multiple commanders operating independently.
-   Battles that continue evolving even without player input.
-   Emergent stories created by interacting systems rather than scripted
    events.
-   Cinematic moments that naturally arise from gameplay.

The player should feel like they are participating in history rather
than watching it.

------------------------------------------------------------------------

# Core Design Pillars

-   Dynamic battles that naturally evolve.
-   Cinematic presentation through gameplay, not scripted sequences.
-   Local battlefield awareness instead of omniscience.
-   Formations are the foundation of tactics.
-   Simple systems combining into emergent behavior.
-   Performance-first ECS architecture.
-   Historical-inspired command hierarchy.

------------------------------------------------------------------------

# Design Philosophy

## Gameplay

-   Information is imperfect.
-   Decisions should have consequences.
-   Formations matter more than individual units.
-   Battles should remain interesting without scripted events.
-   Systems should create emergent gameplay.

## Programming

Follow Pragmatic Programmer principles:

-   Simple over clever.
-   Small systems.
-   Refactor continuously.
-   Eliminate duplication.
-   Build for change.

------------------------------------------------------------------------

# ECS Standards

## Single Writer Rule

Every component has exactly one owner.

Examples:

-   CombatState → Combat State Machine
-   MoveGoal → MovementGoalResolver
-   SliceState → SliceGrid System
-   Health → Damage Pipeline

## Intent Pipeline

Player / AI ↓ Orders ↓ Order Processing ↓ Intents ↓ Resolvers ↓ Gameplay
Systems

Gameplay never directly controls low-level systems.

## System Responsibilities

Each system should answer one question:

> "What is my single responsibility?"

If the answer contains "and", split the system.

------------------------------------------------------------------------

# AI Hierarchy

General ↓ Command ↓ Captain ↓ Formation ↓ Unit

General = strategy

Command = battlefield decisions

Captain = local awareness & reports

Formation = tactical execution

Unit = movement, combat, animation

------------------------------------------------------------------------

# Battlefield Awareness

Information intentionally flows upward.

Slice State ↓ FormationCaptainReport ↓ CommandAwareness ↓ AI Decision
Factory ↓ Orders

Neither the AI nor the player should possess perfect battlefield
knowledge.

Future: - Physical captains - Messenger units - Delayed communication

------------------------------------------------------------------------

# Combat Pipeline

Targeting ↓ Combat Intent ↓ Combat State ↓ Attack Resolution ↓ Damage ↓
Death ↓ Animation

------------------------------------------------------------------------

# Movement Pipeline

Orders ↓ Order Intents ↓ MoveGoalResolver ↓ MoveGoal ↓ MovementSystem

MovementSystem is the only system that moves entities.

------------------------------------------------------------------------

# Formation Philosophy

Formations---not individual units---are the primary tactical objects.

Responsibilities:

-   Cohesion
-   Positioning
-   Advance
-   Charge
-   Defend
-   Break off
-   React to battlefield pressure

------------------------------------------------------------------------

# Slice System

Slices represent **objective simulation telemetry**. They contain the
battlefield evidence from which local knowledge is produced; neither AI
decision systems nor player-facing UI may consume the global slice map
directly.

Slices communicate:

-   Control
-   Pressure
-   Momentum
-   Stability

Their purpose is to feed observation and reporting systems, not directly
control gameplay.

## Command Awareness

Each Command owns its own imperfect battlefield knowledge. The physical
commander supplies the center of a configurable observation circle. Slices
and formation bounds intersecting that circle are observed in real time.

The knowledge pipeline is:

SliceState (objective telemetry) → direct observation / captain reports →
CommandKnownSlice and CommandKnownFormation memory → CommandAwareness → AI
planning and player information.

Observed information is refreshed immediately. Information outside the
observation circle remains frozen, loses confidence, and expires after its
configured memory duration. Owning a formation does not grant live knowledge
of that formation.

The current player test radius is 14 world units, chosen to approximate the
player's zoomed-out Tab camera view. This remains a tuning value rather than an
architectural constant.

`CommandAwarenessSystem` is the only writer of Command-owned knowledge and its
aggregate awareness summary. Debug and UI systems are read-only consumers.

------------------------------------------------------------------------

# Current Priorities

1.  Validate Command-local observation and stale memory in ECS_Scene.
2.  Add explicit captain-report delivery rules beyond direct observation.
3.  Drive AI decisions from CommandAwareness.
4.  Continue Orders-as-Intent pipeline.
5.  Expand formation behaviors.
6.  Elite Guards.
7.  Morale and battle flow.

------------------------------------------------------------------------

# Coding Checklist

Before adding a feature ask:

-   Does this violate single-writer?
-   Does this belong in an existing pipeline?
-   Can this become data instead of logic?
-   Does it increase coupling?
-   Is it independently testable?

------------------------------------------------------------------------

# Definition of Done

A feature is complete when:

-   Ownership is clear.
-   Responsibilities are isolated.
-   Logic is reusable.
-   Performance is acceptable.
-   Gameplay is more interesting.
