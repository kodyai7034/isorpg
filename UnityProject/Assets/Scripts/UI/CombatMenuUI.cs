using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using IsoRPG.Core;

namespace IsoRPG.UI
{
    /// <summary>
    /// Combat sub-menu: Attack (basic melee), Skills (opens skill list), Wait (skip action).
    /// Shown when player selects Act from the action menu.
    /// </summary>
    public class CombatMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button attackButton;
        [SerializeField] private Button skillsButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button cancelButton;

        private RectTransform _panelRect;
        private bool _isVisible;

        private void Awake()
        {
            _panelRect = GetComponent<RectTransform>();

            WireButton(attackButton, () => GameEvents.CombatAttackSelected.Raise());
            WireButton(skillsButton, () => GameEvents.CombatSkillsSelected.Raise());
            WireButton(skipButton, () => GameEvents.CombatSkipSelected.Raise());
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(() =>
                {
                    SFXManager.Instance?.PlayCancel();
                    GameEvents.CombatCancelled.Raise();
                });
                AddHoverFeedback(cancelButton);
            }

            GameEvents.ShowCombatMenu.Subscribe(OnShowRequested);
            GameEvents.HideCombatMenu.Subscribe(OnHideRequested);

            Hide();
        }

        private void OnDestroy()
        {
            GameEvents.ShowCombatMenu.Unsubscribe(OnShowRequested);
            GameEvents.HideCombatMenu.Unsubscribe(OnHideRequested);
        }

        private void OnShowRequested(CombatMenuRequestArgs args)
        {
            if (skillsButton != null)
                skillsButton.interactable = args.HasSkills;

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

        public void Hide()
        {
            _isVisible = false;
            gameObject.SetActive(false);
        }

        private void WireButton(Button btn, System.Action action)
        {
            if (btn == null) return;
            btn.onClick.AddListener(() =>
            {
                if (btn.interactable)
                {
                    SFXManager.Instance?.PlayConfirm();
                    UIAnimator.PunchScale(this, btn.transform, 0.92f, 0.12f);
                    action?.Invoke();
                }
                else
                    SFXManager.Instance?.PlayInvalid();
            });
            AddHoverFeedback(btn);
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
