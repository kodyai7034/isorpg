using UnityEngine;

namespace IsoRPG.UI
{
    /// <summary>
    /// Central reference holder for all battle UI components.
    /// Scene-specific singleton (no DontDestroyOnLoad).
    /// Avoids FindObjectOfType at runtime — all references assigned in editor.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        /// <summary>Scene-scoped singleton instance.</summary>
        public static UIManager Instance { get; private set; }

        [Header("UI Components")]
        public TurnOrderBarUI TurnOrderBar;
        public ActionMenuUI ActionMenu;
        public AbilityMenuUI AbilityMenu;
        public UnitInfoPanelUI UnitInfoPanel;
        public DamageNumberUI DamageNumbers;
        public BattleResultPanelUI BattleResult;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[UIManager] Duplicate instance detected. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
