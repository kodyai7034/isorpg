using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using IsoRPG.Battle;

namespace IsoRPG.UI
{
    /// <summary>
    /// Shows available abilities when Act is selected.
    /// Grays out abilities with insufficient MP. Click fires OnAbilitySelected.
    /// </summary>
    public class AbilityMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject abilityEntryPrefab;
        [SerializeField] private Transform entryContainer;
        [SerializeField] private Button cancelButton;

        /// <summary>Fired when an ability is selected.</summary>
        public event Action<AbilityData> OnAbilitySelected;
        /// <summary>Fired when cancel/back is pressed.</summary>
        public event Action OnCancelled;

        private readonly List<GameObject> _entries = new();

        private void Awake()
        {
            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => OnCancelled?.Invoke());
            Hide();
        }

        /// <summary>
        /// Show ability list. Abilities with cost > currentMP are grayed out.
        /// </summary>
        /// <param name="abilities">Available abilities.</param>
        /// <param name="currentMP">Unit's current MP for affordability check.</param>
        public void Show(AbilityData[] abilities, int currentMP)
        {
            ClearEntries();
            gameObject.SetActive(true);

            if (abilities == null || abilityEntryPrefab == null || entryContainer == null)
                return;

            foreach (var ability in abilities)
            {
                if (ability == null || ability.SlotType != AbilitySlotType.Action)
                    continue;

                var entry = Instantiate(abilityEntryPrefab, entryContainer);
                _entries.Add(entry);

                // Set label
                var text = entry.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null)
                    text.text = $"{ability.AbilityName}  MP:{ability.MPCost}";

                // Set button interactability
                var button = entry.GetComponent<Button>();
                if (button != null)
                {
                    bool canAfford = currentMP >= ability.MPCost;
                    button.interactable = canAfford;

                    var captured = ability; // capture for lambda
                    button.onClick.AddListener(() =>
                    {
                        if (canAfford)
                            OnAbilitySelected?.Invoke(captured);
                    });
                }
            }
        }

        /// <summary>Hide the ability menu.</summary>
        public void Hide()
        {
            ClearEntries();
            gameObject.SetActive(false);
        }

        private void ClearEntries()
        {
            foreach (var entry in _entries)
            {
                if (entry != null) Destroy(entry);
            }
            _entries.Clear();
        }
    }
}
