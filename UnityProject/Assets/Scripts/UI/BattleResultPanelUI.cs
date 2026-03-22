using System;
using UnityEngine;
using UnityEngine.UI;
using IsoRPG.Core;

namespace IsoRPG.UI
{
    /// <summary>
    /// Victory/defeat overlay panel. Subscribes to BattleEnded event.
    /// </summary>
    public class BattleResultPanelUI : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI resultText;
        [SerializeField] private TMPro.TextMeshProUGUI detailText;
        [SerializeField] private Button continueButton;

        /// <summary>Fired when the player clicks Continue/Retry.</summary>
        public event Action OnContinue;

        private void Awake()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(() => OnContinue?.Invoke());
            Hide();
        }

        private void OnEnable()
        {
            GameEvents.BattleEnded.Subscribe(OnBattleEnded);
        }

        private void OnDisable()
        {
            GameEvents.BattleEnded.Unsubscribe(OnBattleEnded);
        }

        private void OnBattleEnded(BattleEndedArgs args)
        {
            gameObject.SetActive(true);

            if (resultText != null)
            {
                resultText.text = args.Result == BattleResult.Victory ? "VICTORY" : "DEFEAT";
                resultText.color = args.Result == BattleResult.Victory
                    ? new Color(1f, 0.85f, 0.2f)
                    : new Color(0.8f, 0.2f, 0.2f);
            }

            if (detailText != null)
                detailText.text = $"Battle completed in {args.TurnsElapsed} turns";

            if (continueButton != null)
            {
                var btnText = continueButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (btnText != null)
                    btnText.text = args.Result == BattleResult.Victory ? "Continue" : "Retry";
            }
        }

        /// <summary>Hide the result panel.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
