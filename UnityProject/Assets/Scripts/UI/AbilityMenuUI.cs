using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using IsoRPG.Core;

namespace IsoRPG.UI
{
    /// <summary>
    /// Polished ability selection menu with slide animation, hover feedback, and audio.
    /// Subscribes to GameEvents.ShowAbilityMenu/HideAbilityMenu.
    /// Button clicks raise GameEvents.AbilitySelected or AbilitySelectionCancelled.
    /// </summary>
    public class AbilityMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject abilityEntryPrefab;
        [SerializeField] private Transform entryContainer;
        [SerializeField] private Button cancelButton;
        [SerializeField] private float slideDuration = 0.3f;

        private RectTransform _panelRect;
        private readonly List<GameObject> _entries = new();
        private bool _isVisible;

        private void Awake()
        {
            _panelRect = GetComponent<RectTransform>();

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(() =>
                {
                    SFXManager.Instance?.PlayCancel();
                    GameEvents.AbilitySelectionCancelled.Raise();
                });
                AddHoverFeedback(cancelButton);
            }

            // Subscribe BEFORE Hide — must persist when inactive
            GameEvents.ShowAbilityMenu.Subscribe(OnShowRequested);
            GameEvents.HideAbilityMenu.Subscribe(OnHideRequested);

            Hide();
        }

        private void OnDestroy()
        {
            GameEvents.ShowAbilityMenu.Unsubscribe(OnShowRequested);
            GameEvents.HideAbilityMenu.Unsubscribe(OnHideRequested);
        }

        private void OnShowRequested(AbilityMenuRequestArgs args)
        {
            ClearEntries();
            gameObject.SetActive(true);
            _isVisible = true;

            if (args.Abilities != null && abilityEntryPrefab != null && entryContainer != null)
            {
                foreach (var ability in args.Abilities)
                {
                    if (ability == null || ability.SlotType != AbilitySlotType.Action)
                        continue;

                    var entry = Instantiate(abilityEntryPrefab, entryContainer);
                    _entries.Add(entry);

                    var text = entry.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (text != null)
                        text.text = $"{ability.AbilityName}  MP:{ability.MPCost}";

                    var button = entry.GetComponent<Button>();
                    if (button != null)
                    {
                        bool canAfford = args.CurrentMP >= ability.MPCost;
                        button.interactable = canAfford;

                        // Greyed out visual for unaffordable
                        if (!canAfford)
                        {
                            var img = entry.GetComponent<Image>();
                            if (img != null) img.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                            if (text != null) text.color = new Color(0.5f, 0.5f, 0.5f);
                        }

                        var captured = ability;
                        button.onClick.AddListener(() =>
                        {
                            if (canAfford)
                            {
                                SFXManager.Instance?.PlayConfirm();
                                UIAnimator.PunchScale(this, button.transform, 0.92f, 0.12f);
                                GameEvents.AbilitySelected.Raise(captured);
                            }
                            else
                            {
                                SFXManager.Instance?.PlayInvalid();
                            }
                        });

                        AddHoverFeedback(button);
                    }
                }
            }

            if (_panelRect != null)
                UIAnimator.SlideIn(this, _panelRect, new Vector2(300, 0), slideDuration);
        }

        private void OnHideRequested()
        {
            if (!_isVisible) return;
            _isVisible = false;

            if (_panelRect != null)
                UIAnimator.SlideOut(this, _panelRect, new Vector2(300, 0), slideDuration,
                    () => { ClearEntries(); gameObject.SetActive(false); });
            else
            {
                ClearEntries();
                gameObject.SetActive(false);
            }
        }

        public void Hide()
        {
            _isVisible = false;
            ClearEntries();
            gameObject.SetActive(false);
        }

        private void ClearEntries()
        {
            foreach (var entry in _entries)
                if (entry != null) Destroy(entry);
            _entries.Clear();
        }

        private void AddHoverFeedback(Button btn)
        {
            if (btn == null) return;
            var trigger = btn.gameObject.AddComponent<EventTrigger>();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            {
                if (btn.interactable)
                {
                    SFXManager.Instance?.PlayTick();
                    UIAnimator.ScaleTo(this, btn.transform, 1.05f, 0.08f);
                }
            });
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => UIAnimator.ScaleTo(this, btn.transform, 1f, 0.08f));
            trigger.triggers.Add(exit);
        }
    }
}
