using System;
using UnityEngine;
using UnityEngine.UI;

namespace IsoRPG.UI
{
    /// <summary>
    /// Context-sensitive action menu during player turns.
    /// Shows Move/Act/Wait/Undo buttons based on current turn state.
    /// Battle states subscribe to the events instead of polling keyboard input.
    /// </summary>
    public class ActionMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button moveButton;
        [SerializeField] private Button actButton;
        [SerializeField] private Button waitButton;
        [SerializeField] private Button undoButton;

        /// <summary>Fired when Move is clicked.</summary>
        public event Action OnMoveSelected;
        /// <summary>Fired when Act is clicked.</summary>
        public event Action OnActSelected;
        /// <summary>Fired when Wait is clicked.</summary>
        public event Action OnWaitSelected;
        /// <summary>Fired when Undo is clicked.</summary>
        public event Action OnUndoSelected;

        private void Awake()
        {
            if (moveButton != null) moveButton.onClick.AddListener(() => OnMoveSelected?.Invoke());
            if (actButton != null) actButton.onClick.AddListener(() => OnActSelected?.Invoke());
            if (waitButton != null) waitButton.onClick.AddListener(() => OnWaitSelected?.Invoke());
            if (undoButton != null) undoButton.onClick.AddListener(() => OnUndoSelected?.Invoke());
            Hide();
        }

        /// <summary>
        /// Show the action menu with buttons enabled/disabled based on context.
        /// </summary>
        /// <param name="canMove">Whether the unit can still move.</param>
        /// <param name="canAct">Whether the unit can still act.</param>
        /// <param name="canUndo">Whether there are commands to undo.</param>
        public void Show(bool canMove, bool canAct, bool canUndo)
        {
            gameObject.SetActive(true);
            if (moveButton != null) moveButton.interactable = canMove;
            if (actButton != null) actButton.interactable = canAct;
            if (undoButton != null)
            {
                undoButton.interactable = canUndo;
                undoButton.gameObject.SetActive(canUndo);
            }
        }

        /// <summary>Hide the action menu.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
