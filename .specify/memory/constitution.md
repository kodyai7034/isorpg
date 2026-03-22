# IsoRPG — Project Constitution

## Vision
A Final Fantasy Tactics-inspired isometric tactics RPG built in Unity, featuring deep job/class systems, elevation-aware grid combat, undo/rewind mechanics, and AI-generated pixel art assets.

## Core Principles

1. **Gameplay First**: Every system serves the tactical combat loop. No feature bloat.
2. **Cross-Platform**: Unity targeting PC (Windows/Mac/Linux) first, console and mobile as stretch goals.
3. **AI-Assisted Assets**: Use Gemini + PixelLab pipeline for character sprites, tiles, and animations. Minimize manual art dependency.
4. **Production-Ready Code**: Never take shortcuts. No "fastest" or "easiest" approach — always the production-ready, ship-it answer. Best practices in every file, every commit.
5. **Modular & Extensible**: Every system is built to be extended without rewriting. Interfaces over concrete types. Composition over inheritance. New abilities, jobs, status effects, and AI behaviors plug in without touching existing code.
6. **Spec-Driven**: Features are specified before implementation. Tests validate specs.
7. **Rewind-Ready**: All game actions use the Command Pattern from day one. Undo/rewind (CHARIOT-style) is a first-class architectural concern, not a bolt-on.

## Architecture Mandates

These patterns are non-negotiable across the entire codebase. Every system must be built as if it's shipping tomorrow — no prototyping mindset, no "we'll fix it later."

### Interfaces & Abstractions First
Define behavior through interfaces, not concrete classes. New content (abilities, status effects, AI behaviors, terrain types) plugs in by implementing an interface — never by modifying existing code. Follow SOLID principles, especially Open/Closed: open for extension, closed for modification.

### Command Pattern for All Actions
Every game action (Move, Attack, UseAbility, Wait, UseItem) is an `ICommand` object with `Execute()` and `Undo()`. This enables:
- Undo/rewind (Tactics Ogre CHARIOT, Fire Emblem Divine Pulse)
- AI evaluation (execute hypothetically, score, undo)
- Replay recording (serialize command log)
- Multiplayer (send commands over network)

### Event-Driven Communication
Game systems communicate via C# `event`/`Action` delegates, not polling or direct references. Key events:
- `OnTurnStarted`, `OnTurnEnded`
- `OnUnitMoved`, `OnUnitDamaged`, `OnUnitDied`
- `OnAbilityUsed`, `OnStatusApplied`

UI, camera, audio, and VFX subscribe to events — they never poll game state.

### Data/View Separation
- **Data layer**: Pure C# classes (`UnitInstance`, `BattleContext`, `CTSystem`). Zero MonoBehaviour dependency. Fully unit-testable.
- **View layer**: MonoBehaviours (`UnitView`, `TileView`, `EffectView`) that observe the data layer via events and update visuals.
- **Never** put game logic in MonoBehaviours.

### Input Abstraction
Player input, AI decisions, and network commands all produce the same `ICommand` objects. The battle system does not know or care where commands come from.

### Defensive & Robust Code
- Validate at system boundaries — never trust external input.
- Guard against infinite loops, null references, and out-of-bounds access.
- Use `TryGet` patterns over raw lookups. Return sentinel values, not `default`.
- Fail loudly in editor (asserts, exceptions), fail gracefully in builds.
- No magic numbers — constants and enums with clear names.
- Every public API has XML doc comments describing contract and edge cases.

## Tech Stack

- **Engine**: Unity 6 (6000.3.11f1) with Universal Render Pipeline (URP)
- **Language**: C# with .NET Standard 2.1
- **Rendering**: 2D sprite-based isometric with custom sorting order
- **UI**: Unity UI Toolkit (or TextMeshPro + Canvas for complex menus)
- **Data**: ScriptableObjects for static game data + Command Pattern for runtime actions
- **State Management**: Hierarchical state machine for battle flow, event-driven observers for cross-system communication
- **Asset Loading**: Unity Addressables (not Resources folder)
- **Backend**: Supabase (auth, database, realtime for future multiplayer)
- **Asset Pipeline**: Gemini API (keyframes) + PixelLab API (interpolation/characters)
- **Testing**: Unity Test Framework (NUnit) for unit + integration tests
- **Build**: Unity Build Pipeline, CI via GitHub Actions
- **Source Control**: GitHub, PR-per-system workflow

## Scope Constraints

- **MVP**: Single-player tactical battles on handcrafted maps with undo/rewind
- **V1**: Story mode with 5-10 battles, 4-6 job classes, progression, save/load
- **V2**: PvP multiplayer, procedural maps, expanded job tree, replay system
- **Out of Scope (for now)**: Full 3D models, MMO features, open world, squad-based combat

## Code Standards

- C# naming conventions (PascalCase methods/properties, camelCase locals)
- No MonoBehaviour for pure game logic — separate data/logic from presentation
- ScriptableObjects for static game data (jobs, abilities, terrain types)
- All game actions implement `ICommand` with `Execute()` and `Undo()`
- All cross-system communication via C# events
- All formulas documented and unit-tested
- Assembly Definitions for module boundaries
- No `static` ID counters — use GUIDs for entity identity

## Development Workflow

Each system is developed in this cycle:
1. **Spec** — Write speckit spec (`specs/<system>/spec.md`)
2. **Implement** — Code the system with tests
3. **Code Review** — `/codex-review` or architect review
4. **PR** — Submit PR to GitHub from feature branch
5. **Merge** — Merge to main after review passes
