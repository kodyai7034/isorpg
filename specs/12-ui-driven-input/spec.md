# System 12: UI-Driven Input with Polish — Specification

## Overview

Complete rework of player input: mouse-primary with keyboard navigation support. All gameplay flows through on-screen menus. Arrow keys navigate menus, Enter confirms, Escape cancels. WASD pans camera, Q/E rotates. Every interaction has audio + visual feedback. Menu art generated via Gemini.

---

## 1. What's Being Removed

All `Input.GetKeyDown` calls for gameplay actions in:
- `SelectActionState` (M, A, W, U keys)
- `SelectAbilityState` (1-4, Escape keys)
- `MoveTargetState` (Escape key — keep right-click cancel)
- `ActionTargetState` (Escape key — keep right-click cancel)

**Kept:** WASD camera pan, scroll zoom, left/right mouse clicks on tiles.

---

## 2. ActionMenuUI (Rework)

### Visual Design
- Panel slides in from bottom-right with easing (0.3s DoTween/AnimationCurve)
- Fantasy-themed panel background (Gemini-generated ornate border)
- 4 buttons stacked vertically: Move (blue), Act (red), Wait (grey), Undo (gold)
- Each button has 4 visual states: normal, hover (1.05x scale), pressed (0.95x), disabled (desaturated)
- Disabled buttons visible but greyed out — not hidden

### Audio
- Hover any button → soft tick
- Click enabled button → confirm chime
- Click disabled button → dull thud
- Menu slide in → subtle "whoosh"

### Event Flow
```
GameEvents.ShowActionMenu raised by SelectActionState
  → ActionMenuUI slides in, enables/disables buttons per args
  → Player hovers button → tick sound + scale tween
  → Player clicks button → confirm sound + GameEvents.ActionXxxSelected raised
  → SelectActionState receives event → transitions

GameEvents.HideActionMenu raised by SelectActionState.Exit
  → ActionMenuUI slides out
```

---

## 3. AbilityMenuUI (Rework)

### Visual Design
- Replaces/overlays the action menu when Act is selected
- Scrollable list of ability entries with icon + name + MP cost
- Cancel button at bottom
- Abilities with insufficient MP are greyed but visible (shows "Not enough MP" on hover)

### Audio
- Hover ability → tick
- Select ability → confirm chime
- Cancel → back whoosh
- Hover insufficient MP ability → dull tone

### Event Flow
```
GameEvents.ShowAbilityMenu raised by SelectAbilityState
  → AbilityMenuUI slides in with ability list
  → Player clicks ability → GameEvents.AbilitySelected raised
  → Player clicks Cancel → GameEvents.AbilitySelectionCancelled raised

GameEvents.HideAbilityMenu raised by SelectAbilityState.Exit
  → AbilityMenuUI slides out
```

---

## 4. Tile Interaction (Mouse Only)

### MoveTargetState
```
Enter:
  - Show movement range (blue pulsing tiles)
  - Hide action menu
  - Subscribe to IsometricGrid.OnTileClicked

Execute:
  - Path preview on mouse hover (cascade highlight)
  - Left-click valid tile → confirm sound → create MoveCommand → PerformMoveState
  - Right-click anywhere → cancel sound → clear overlays → SelectActionState

Exit:
  - Clear overlays
  - Unsubscribe from tile events
```

### ActionTargetState
```
Enter:
  - Show attack range (red pulsing tiles)
  - Subscribe to IsometricGrid.OnTileClicked

Execute:
  - Hover valid target → show damage preview tooltip + tension sound
  - Left-click valid target → confirm sound → PerformActionState
  - Right-click → cancel sound → SelectAbilityState

Exit:
  - Clear overlays
  - Hide tooltip
  - Unsubscribe from tile events
```

---

## 5. Turn Transition UI

### TurnBannerUI (New)
When a unit's turn starts:
1. Banner slides in from left (0.4s ease-out)
2. Shows unit name + team color + small portrait placeholder
3. Chime sound plays
4. Banner holds for 0.8s
5. Banner slides out (0.3s ease-in)
6. Then SelectActionState begins (for player) or AI acts (for enemy)

### AI Turn Indicator
- Small "thinking..." label with animated dots appears during AI evaluation
- Subtle processing sound or silence

---

## 6. UIAnimator (New Utility)

Centralized tween/animation utility for all UI motion:

```csharp
public static class UIAnimator
{
    /// <summary>Slide a RectTransform from off-screen to target position.</summary>
    public static Coroutine SlideIn(MonoBehaviour host, RectTransform target,
        Vector2 fromOffset, float duration, System.Action onComplete = null);

    /// <summary>Slide a RectTransform off-screen.</summary>
    public static Coroutine SlideOut(MonoBehaviour host, RectTransform target,
        Vector2 toOffset, float duration, System.Action onComplete = null);

    /// <summary>Scale punch effect (grow then shrink back).</summary>
    public static Coroutine PunchScale(MonoBehaviour host, Transform target,
        float punchScale, float duration);

    /// <summary>Pulsing opacity animation for tile overlays.</summary>
    public static Coroutine PulseAlpha(MonoBehaviour host, SpriteRenderer target,
        float minAlpha, float maxAlpha, float speed);
}
```

Uses `AnimationCurve.EaseInOut` — no external dependencies (no DOTween).

---

## 7. SFXManager (New)

Centralized sound effect player:

```csharp
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("UI Sounds")]
    public AudioClip menuTick;
    public AudioClip menuConfirm;
    public AudioClip menuCancel;
    public AudioClip menuInvalid;

    [Header("Battle Sounds")]
    public AudioClip turnStart;
    public AudioClip attackHit;
    public AudioClip attackMiss;
    public AudioClip heal;
    public AudioClip unitDied;
    public AudioClip victory;
    public AudioClip defeat;

    public void PlayTick();
    public void PlayConfirm();
    public void PlayCancel();
    public void PlayInvalid();
    public void Play(AudioClip clip);
}
```

For MVP: clips can be null — SFXManager gracefully skips null clips. Audio is additive, not blocking. Sounds generated or sourced later.

---

## 8. Gemini-Generated Art Assets

Use the `/asset` pipeline or direct Gemini calls to generate:

| Asset | Description | Format |
|-------|-------------|--------|
| Menu panel BG | Ornate fantasy border, dark interior, semi-transparent | 256x512 PNG |
| Button normal | Fantasy-styled button, raised look | 200x48 PNG |
| Button hover | Same button, brighter/glowing border | 200x48 PNG |
| Button pressed | Same button, depressed/darker | 200x48 PNG |
| Button disabled | Same button, desaturated/grey | 200x48 PNG |
| Turn banner BG | Scrollwork banner, team-colored variants | 512x64 PNG |
| Ability icons (4) | Attack sword, Fire flame, Cure sparkle, Poison vial | 32x32 each |
| Status icons (6) | Poison, Haste, Slow, Protect, Shell, Regen | 16x16 each |

Style constraints for Gemini prompts:
```
Pixel art, fantasy RPG style, limited color palette,
clean pixel edges, transparent background, game UI element,
Final Fantasy Tactics aesthetic
```

---

## 9. Battle State Changes Summary

| State | Before (keyboard) | After (UI-driven) |
|-------|-------------------|-------------------|
| SelectActionState | `Input.GetKeyDown(M/A/W/U)` | Subscribe to GameEvents, Execute is empty |
| SelectAbilityState | `Input.GetKeyDown(1-4/Esc)` | Subscribe to GameEvents, Execute is empty |
| MoveTargetState | `Input.GetKeyDown(Esc)` | Right-click only + tile click events |
| ActionTargetState | `Input.GetKeyDown(Esc)` | Right-click only + tile click events |

---

## 10. Tasks

- [ ] 12.1 Create UIAnimator utility (SlideIn, SlideOut, PunchScale, PulseAlpha)
- [ ] 12.2 Create SFXManager singleton (null-safe, graceful skip)
- [ ] 12.3 Create TurnBannerUI (slide-in name banner on turn start)
- [ ] 12.4 Rework ActionMenuUI — slide animation, hover/press feedback, audio hooks
- [ ] 12.5 Rework AbilityMenuUI — slide animation, MP gating visual, audio hooks
- [ ] 12.6 Rewrite SelectActionState — remove all Input.GetKeyDown, event-only
- [ ] 12.7 Rewrite SelectAbilityState — remove all Input.GetKeyDown, event-only
- [ ] 12.8 Update MoveTargetState — remove Escape key, right-click only
- [ ] 12.9 Update ActionTargetState — remove Escape key, right-click only
- [ ] 12.10 Update QuickSetup — create visible, functional UI with all components
- [ ] 12.11 Add tile overlay pulsing animation (breathing effect)
- [ ] 12.12 Generate menu art via Gemini (panel BG, buttons, icons)
- [ ] 12.13 Generate placeholder SFX (or source free clips)
- [ ] 12.14 Integration test — full battle playable with mouse only
