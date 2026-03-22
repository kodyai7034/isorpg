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

### UI-Driven Player Input (No Keyboard Bindings)
All player actions are driven through **on-screen UI menus and mouse clicks** — never raw keyboard shortcuts. The player interacts exclusively through:
- **Clickable action menu** (Move, Act, Wait, Undo buttons)
- **Clickable ability menu** (list of abilities with MP costs)
- **Mouse clicks on tiles** (select movement destination, select attack target)
- **Mouse hover** for tooltips, path preview, and unit info

No gameplay actions are bound to keyboard keys. Keyboard shortcuts are only for non-gameplay functions (camera pan with WASD, zoom with scroll). The game must be fully playable with mouse alone. Battle states must NEVER use `Input.GetKeyDown` for gameplay actions — all gameplay input flows through UI button events via GameEvents.

### UI Polish & Feedback (Juice)

Menus are the primary interface in a tactics RPG — they must feel buttery smooth with constant feedback. Every player interaction gets at least two forms of feedback (audio + visual).

**Audio Feedback (required on every interaction):**
- Menu cursor move / hover → soft tick/click
- Button confirm / select → satisfying "accept" chime
- Cancel / back → soft "whoosh" or lower-pitched click
- Invalid action (greyed out button) → dull buzz/thud
- Hover over enemy unit → tension note
- Hover over ally unit → warm tone
- Undo → reverse "swoosh"
- Turn start → brief announcement chime
- Victory → fanfare
- Defeat → somber tone

**Visual Feedback (required on every interaction):**
- Button hover → scale up slightly (1.05x) with ease-in-out tween
- Button press → quick scale down (0.95x) then bounce back
- Selected/active button → glowing border pulse or highlight sweep
- Greyed-out buttons → desaturated + 50% transparency
- Menu appear → slide in from edge with easing (never instant pop)
- Menu disappear → slide out or fade (never instant vanish)
- Turn start → unit portrait slides in, name banner animates across screen
- Damage numbers → punch scale (start 2x big, shrink to 1x) + screen shake on crits
- Healing numbers → gentle float up with green glow

**Tile/Cursor Feedback:**
- Tile hover → subtle bounce or glow pulse
- Movement range tiles → gentle pulsing opacity (breathing effect)
- Attack range tiles → sharper red pulse
- Path preview → tiles light up sequentially in a cascade
- Valid target hover → enemy highlight + damage preview tooltip appears
- Invalid target → cursor tint red or X indicator

**Transition Feedback:**
- Turn transition → "whoosh" + banner showing unit name + team color
- AI thinking → animated ellipsis or thinking indicator
- Victory → screen flash + particle burst + fanfare
- Defeat → screen darken + somber tone + slow fade

**AI-Generated Menu Art (via Gemini):**
- Menu panel backgrounds (ornate fantasy borders, parchment/stone textures)
- Button art (4 states per button: normal, hover, pressed, disabled)
- Unit portrait frames (team-colored ornate borders)
- Turn banner background (scrollwork or flag motif)
- Ability icons (one unique icon per ability)
- Status effect icons (one per status type)
- All art must be consistent pixel art style matching the game's aesthetic

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
