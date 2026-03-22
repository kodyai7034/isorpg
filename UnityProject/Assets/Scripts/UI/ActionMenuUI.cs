using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using IsoRPG.Core;

namespace IsoRPG.UI
{
    /// <summary>
    /// Polished action menu with slide animation, hover/press feedback, and audio.
    /// Subscribes to GameEvents.ShowActionMenu/HideActionMenu.
    /// Button clicks raise GameEvents.ActionXxxSelected.
    /// </summary>
    public class ActionMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button moveButton;
        [SerializeField] private Button actButton;
        [SerializeField] private Button waitButton;
        [SerializeField] private Button undoButton;

        [Header("Animation")]
        [SerializeField] private float slideDuration = 0.3f;

        private RectTransform _panelRect;
        private bool _isVisible;

        private void Awake()
        {
            _panelRect = GetComponent<RectTransform>();

            WireButton(moveButton, () => GameEvents.ActionMoveSelected.Raise());
            WireButton(actButton, () => GameEvents.ActionActSelected.Raise());
            WireButton(waitButton, () => GameEvents.ActionWaitSelected.Raise());
            WireButton(undoButton, () => GameEvents.ActionUndoSelected.Raise());

            AddHoverFeedback(moveButton);
            AddHoverFeedback(actButton);
            AddHoverFeedback(waitButton);
            AddHoverFeedback(undoButton);

            Hide();
        }

        private void OnEnable()
        {
            GameEvents.ShowActionMenu.Subscribe(OnShowRequested);
            GameEvents.HideActionMenu.Subscribe(OnHideRequested);
        }

        private void OnDisable()
        {
            GameEvents.ShowActionMenu.Unsubscribe(OnShowRequested);
            GameEvents.HideActionMenu.Unsubscribe(OnHideRequested);
        }

        private void OnShowRequested(ActionMenuRequestArgs args)
        {
            if (moveButton != null) moveButton.interactable = args.CanMove;
            if (actButton != null) actButton.interactable = args.CanAct;
            if (undoButton != null)
            {
                undoButton.interactable = args.CanUndo;
                undoButton.gameObject.SetActive(args.CanUndo);
            }

            gameObject.SetActive(true);
            _isVisible = true;

            if (_panelRect != null)
                UIAnimator.SlideIn(this, _panelRect, new Vector2(300, 0), slideDuration);
        }

        private void OnHideRequested()
        {
            if (!_isVisible) return;
            _isVisible = false;

            if (_panelRect != null)
                UIAnimator.SlideOut(this, _panelRect, new Vector2(300, 0), slideDuration,
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
                {
                    SFXManager.Instance?.PlayInvalid();
                }
            });
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
