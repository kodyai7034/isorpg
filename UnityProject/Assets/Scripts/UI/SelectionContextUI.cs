using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using IsoRPG.Core;

namespace IsoRPG.UI
{
    /// <summary>
    /// Contextual UI panel shown during tile selection (move destination, attack target, heal target).
    /// Displays a label describing what the player is selecting, plus a Cancel button.
    ///
    /// Driven entirely by GameEvents — no direct references from battle states.
    /// Extensible: any future selection mode (AoE placement, item targeting, summon placement)
    /// works by raising ShowSelectionContext with appropriate args.
    ///
    /// Cancel button raises GameEvents.SelectionCancelled. Battle states subscribe to this
    /// alongside right-click cancel for consistent behavior.
    /// </summary>
    public class SelectionContextUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMPro.TextMeshProUGUI labelText;
        [SerializeField] private TMPro.TextMeshProUGUI sublabelText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Image panelBackground;

        private RectTransform _panelRect;
        private bool _isVisible;

        private static readonly Color MoveColor = new(0.12f, 0.2f, 0.4f, 0.92f);
        private static readonly Color AttackColor = new(0.4f, 0.1f, 0.1f, 0.92f);
        private static readonly Color HealColor = new(0.1f, 0.35f, 0.15f, 0.92f);

        private void Awake()
        {
            _panelRect = GetComponent<RectTransform>();

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(() =>
                {
                    SFXManager.Instance?.PlayCancel();
                    UIAnimator.PunchScale(this, cancelButton.transform, 0.92f, 0.12f);
                    GameEvents.SelectionCancelled.Raise();
                });

                // Hover feedback
                var trigger = cancelButton.gameObject.AddComponent<EventTrigger>();

                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ =>
                {
                    SFXManager.Instance?.PlayTick();
                    UIAnimator.ScaleTo(this, cancelButton.transform, 1.05f, 0.08f);
                });
                trigger.triggers.Add(enter);

                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ =>
                    UIAnimator.ScaleTo(this, cancelButton.transform, 1f, 0.08f));
                trigger.triggers.Add(exit);
            }

            // Subscribe before Hide so events persist when inactive
            GameEvents.ShowSelectionContext.Subscribe(OnShowRequested);
            GameEvents.HideSelectionContext.Subscribe(OnHideRequested);

            Hide();
        }

        private void OnDestroy()
        {
            GameEvents.ShowSelectionContext.Unsubscribe(OnShowRequested);
            GameEvents.HideSelectionContext.Unsubscribe(OnHideRequested);
        }

        private void OnShowRequested(SelectionContextArgs args)
        {
            if (labelText != null) labelText.text = args.Label;
            if (sublabelText != null) sublabelText.text = args.Sublabel;

            if (panelBackground != null)
            {
                panelBackground.color = args.Mode switch
                {
                    SelectionMode.Move => MoveColor,
                    SelectionMode.Attack => AttackColor,
                    SelectionMode.Heal => HealColor,
                    _ => MoveColor,
                };
            }

            gameObject.SetActive(true);
            _isVisible = true;

            if (_panelRect != null)
                UIAnimator.SlideIn(this, _panelRect, new Vector2(300, 0), 0.25f);
        }

        private void OnHideRequested()
        {
            if (!_isVisible) return;
            _isVisible = false;

            if (_panelRect != null)
                UIAnimator.SlideOut(this, _panelRect, new Vector2(300, 0), 0.2f,
                    () => gameObject.SetActive(false));
            else
                gameObject.SetActive(false);
        }

        /// <summary>Immediate hide without animation.</summary>
        public void Hide()
        {
            _isVisible = false;
            gameObject.SetActive(false);
        }
    }
}
