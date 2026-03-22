using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IsoRPG.Core;
using IsoRPG.Battle;
using IsoRPG.Units;

namespace IsoRPG.UI
{
    /// <summary>
    /// Displays the next N units in CT turn order.
    /// Subscribes to battle events and auto-refreshes when turns change or units die.
    /// </summary>
    public class TurnOrderBarUI : MonoBehaviour
    {
        [SerializeField] private GameObject turnEntryPrefab;
        [SerializeField] private Transform entryContainer;
        [SerializeField] private int previewCount = 10;

        private List<UnitInstance> _allUnits;
        private readonly List<GameObject> _entries = new();

        /// <summary>
        /// Initialize with the unit list. Called by BattleManager after spawning units.
        /// </summary>
        public void Initialize(List<UnitInstance> allUnits)
        {
            _allUnits = allUnits;
            Refresh();
        }

        private void OnEnable()
        {
            GameEvents.TurnStarted.Subscribe(OnTurnChanged);
            GameEvents.TurnEnded.Subscribe(OnTurnEnded);
            GameEvents.UnitDied.Subscribe(OnUnitDied);
        }

        private void OnDisable()
        {
            GameEvents.TurnStarted.Unsubscribe(OnTurnChanged);
            GameEvents.TurnEnded.Unsubscribe(OnTurnEnded);
            GameEvents.UnitDied.Unsubscribe(OnUnitDied);
        }

        private void OnTurnChanged(TurnStartedArgs args) => Refresh();
        private void OnTurnEnded(TurnEndedArgs args) => Refresh();
        private void OnUnitDied(UnitDiedArgs args) => Refresh();

        private void Refresh()
        {
            // Clear existing entries
            foreach (var entry in _entries)
            {
                if (entry != null) Destroy(entry);
            }
            _entries.Clear();

            if (_allUnits == null || turnEntryPrefab == null || entryContainer == null)
                return;

            var preview = CTSystem.PreviewTurnOrder(_allUnits, previewCount);

            foreach (var unit in preview)
            {
                var entry = Instantiate(turnEntryPrefab, entryContainer);
                _entries.Add(entry);

                // Set name text
                var text = entry.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null)
                    text.text = unit.Name;

                // Set team color
                var img = entry.GetComponent<Image>();
                if (img != null)
                {
                    img.color = unit.Team == 0
                        ? new Color(0.3f, 0.5f, 0.9f, 0.8f)   // blue for player
                        : new Color(0.9f, 0.3f, 0.3f, 0.8f);   // red for enemy
                }
            }
        }
    }
}
