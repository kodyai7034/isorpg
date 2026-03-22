# System 8: UI Layer — Specification

## Overview

All battle HUD elements built as event-driven MonoBehaviours. Every UI component subscribes to GameEvents or instance events — zero polling. This replaces the temporary keyboard input from System 5 with clickable menus.

---

## 1. Architecture

All UI lives on a dedicated Canvas (Screen Space - Overlay) in the Battle scene. UI scripts are in the `IsoRPG.UI` assembly which depends on Core, Battle, and Units but nothing depends on UI.

```
BattleCanvas (Screen Space - Overlay)
├── TurnOrderBar (top)
├── ActionMenu (bottom-right, context-sensitive)
├── AbilityMenu (bottom-right, replaces ActionMenu when acting)
├── UnitInfoPanel (bottom-left, shows selected/hovered unit)
├── DamageNumbers (world-space, floating popups)
├── AbilityPreview (tooltip near cursor)
└── BattleResultPanel (center, shown on victory/defeat)
```

---

## 2. TurnOrderBarUI

Shows the next 8-10 units in turn order.

### 2.1 Behavior
- On `GameEvents.TurnStarted`: refresh the preview via `CTSystem.PreviewTurnOrder`
- On `GameEvents.TurnEnded`: refresh
- On `GameEvents.UnitDied`: refresh (dead unit removed from preview)
- Each entry shows: unit name, team color indicator, small portrait placeholder

### 2.2 Interface
```csharp
public class TurnOrderBarUI : MonoBehaviour
{
    [SerializeField] private GameObject turnEntryPrefab;
    [SerializeField] private Transform entryContainer;
    [SerializeField] private int previewCount = 10;

    private void RefreshTurnOrder(List<UnitInstance> allUnits);
}
```

---

## 3. ActionMenuUI

Context-sensitive action menu during player turns.

### 3.1 Buttons
| Button | Shown When | Action |
|--------|-----------|--------|
| Move | Not yet moved | Fires `OnMoveSelected` |
| Act | Not yet acted | Fires `OnActSelected` |
| Wait | Always | Fires `OnWaitSelected` |
| Undo | Commands exist this turn | Fires `OnUndoSelected` |

### 3.2 Behavior
- Shows on `GameEvents.TurnStarted` (if player unit)
- Hides during MoveTargetState, ActionTargetState
- Re-shows after move/act completes
- Hides on `GameEvents.TurnEnded`

### 3.3 Events (instance, not global)
```csharp
public event Action OnMoveSelected;
public event Action OnActSelected;
public event Action OnWaitSelected;
public event Action OnUndoSelected;
```

SelectActionState subscribes to these instead of checking keyboard input.

---

## 4. AbilityMenuUI

Shows available abilities when Act is selected.

### 4.1 Behavior
- Populated with ability names, MP costs, and range info
- Grayed out if insufficient MP
- Click selects ability → fires `OnAbilitySelected(AbilityData)`
- Back button returns to ActionMenu

### 4.2 Interface
```csharp
public class AbilityMenuUI : MonoBehaviour
{
    public event Action<AbilityData> OnAbilitySelected;
    public event Action OnCancelled;

    public void Show(AbilityData[] abilities, int currentMP);
    public void Hide();
}
```

---

## 5. UnitInfoPanelUI

Shows stats for the selected or hovered unit.

### 5.1 Display
- Unit name, level, job
- HP bar (green → yellow → red)
- MP bar (blue)
- PA, MA, Speed stats
- Active status effect icons
- Team color border

### 5.2 Behavior
- On tile hover with unit: show that unit's info
- On unit selected (active turn): show active unit's info persistently
- On `GameEvents.DamageDealt` / `HealingDealt`: animate HP bar change

---

## 6. DamageNumberUI

Floating damage/heal numbers that pop up over units.

### 6.1 Behavior
- On `GameEvents.DamageDealt`: spawn red number at target position, float upward, fade out
- On `GameEvents.HealingDealt`: spawn green number
- On miss: spawn "MISS" text
- Uses world-space Canvas or screen-space with WorldToScreenPoint

### 6.2 Interface
```csharp
public class DamageNumberUI : MonoBehaviour
{
    [SerializeField] private GameObject damageNumberPrefab;

    public void SpawnDamageNumber(Vector3 worldPos, int amount, Color color);
    public void SpawnMissText(Vector3 worldPos);
}
```

---

## 7. AbilityPreviewUI

Tooltip showing predicted damage/hit% when hovering over a target during ActionTargetState.

### 7.1 Display
- Ability name
- Predicted damage (or healing amount)
- Hit chance %
- Status effect that would be applied

### 7.2 Behavior
- Shows near cursor when hovering a valid target in ActionTargetState
- Updates in real-time as mouse moves between targets
- Hides when cursor is off a valid target

---

## 8. BattleResultPanelUI

Victory/defeat screen overlay.

### 8.1 Behavior
- On `GameEvents.BattleEnded`: show result panel
- Victory: "VICTORY" text, turns elapsed, continue button
- Defeat: "DEFEAT" text, retry button
- Buttons fire events for scene transition (future)

---

## 9. State Machine Integration

Replace keyboard input in battle states with UI events:

### SelectActionState
- Subscribe to `ActionMenuUI.OnMoveSelected`, `OnActSelected`, `OnWaitSelected`, `OnUndoSelected`
- Show ActionMenu on Enter, hide on Exit

### SelectAbilityState
- Subscribe to `AbilityMenuUI.OnAbilitySelected`, `OnCancelled`
- Show AbilityMenu on Enter, hide on Exit

### MoveTargetState / ActionTargetState
- Hide ActionMenu/AbilityMenu on Enter
- Keep right-click/Escape cancel (mouse-driven, not keyboard)

---

## 10. UIManager

Central reference holder for all UI components. Avoids FindObjectOfType at runtime.

```csharp
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public TurnOrderBarUI TurnOrderBar;
    public ActionMenuUI ActionMenu;
    public AbilityMenuUI AbilityMenu;
    public UnitInfoPanelUI UnitInfoPanel;
    public DamageNumberUI DamageNumbers;
    public AbilityPreviewUI AbilityPreview;
    public BattleResultPanelUI BattleResult;
}
```

Singleton with `DontDestroyOnLoad` NOT applied (UI is scene-specific).

---

## 11. Test Coverage

UI components are MonoBehaviour-heavy and require PlayMode tests. For System 8, focus on:
- Verify event subscription/unsubscription lifecycle (no dangling references)
- Verify ActionMenuUI shows correct buttons based on context flags
- Unit tests for any pure logic helpers (damage preview calculation)
