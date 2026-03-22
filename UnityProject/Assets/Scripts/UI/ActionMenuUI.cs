using UnityEngine;
using UnityEngine.UI;
using IsoRPG.Core;

namespace IsoRPG.UI
{
    /// <summary>
    /// Context-sensitive action menu during player turns.
    /// Subscribes to GameEvents.ShowActionMenu / HideActionMenu.
    /// Button clicks raise GameEvents.ActionMoveSelected etc.
    /// No direct coupling to battle states.
    /// </summary>
    public class ActionMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button moveButton;
        [SerializeField] private Button actButton;
        [SerializeField] private Button waitButton;
        [SerializeField] private Button undoButton;

        private void Awake()
        {
            if (moveButton != null) moveButton.onClick.AddListener(() => GameEvents.ActionMoveSelected.Raise());
            if (actButton != null) actButton.onClick.AddListener(() => GameEvents.ActionActSelected.Raise());
            if (waitButton != null) waitButton.onClick.AddListener(() => GameEvents.ActionWaitSelected.Raise());
            if (undoButton != null) undoButton.onClick.AddListener(() => GameEvents.ActionUndoSelected.Raise());
            Hide();
        }

        private void OnEnable()
        {
            GameEvents.ShowActionMenu.Subscribe(OnShowRequested);
            GameEvents.HideActionMenu.Subscribe(Hide);
        }

        private void OnDisable()
        {
            GameEvents.ShowActionMenu.Unsubscribe(OnShowRequested);
            GameEvents.HideActionMenu.Unsubscribe(Hide);
        }

        private void OnShowRequested(ActionMenuRequestArgs args)
        {
            gameObject.SetActive(true);
            if (moveButton != null) moveButton.interactable = args.CanMove;
            if (actButton != null) actButton.interactable = args.CanAct;
            if (undoButton != null)
            {
                undoButton.interactable = args.CanUndo;
                undoButton.gameObject.SetActive(args.CanUndo);
            }
        }

        /// <summary>Hide the action menu.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
