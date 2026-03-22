using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using IsoRPG.Core;

namespace IsoRPG.UI
{
    /// <summary>
    /// Shows available abilities when Act is selected.
    /// Subscribes to GameEvents.ShowAbilityMenu / HideAbilityMenu.
    /// Button clicks raise GameEvents.AbilitySelected.
    /// No direct coupling to battle states.
    /// </summary>
    public class AbilityMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject abilityEntryPrefab;
        [SerializeField] private Transform entryContainer;
        [SerializeField] private Button cancelButton;

        private readonly List<GameObject> _entries = new();

        private void Awake()
        {
            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => GameEvents.AbilitySelectionCancelled.Raise());
            Hide();
        }

        private void OnEnable()
        {
            GameEvents.ShowAbilityMenu.Subscribe(OnShowRequested);
            GameEvents.HideAbilityMenu.Subscribe(Hide);
        }

        private void OnDisable()
        {
            GameEvents.ShowAbilityMenu.Unsubscribe(OnShowRequested);
            GameEvents.HideAbilityMenu.Unsubscribe(Hide);
        }

        private void OnShowRequested(AbilityMenuRequestArgs args)
        {
            ClearEntries();
            gameObject.SetActive(true);

            if (args.Abilities == null || abilityEntryPrefab == null || entryContainer == null)
                return;

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

                    var captured = ability;
                    button.onClick.AddListener(() =>
                    {
                        if (canAfford)
                            GameEvents.AbilitySelected.Raise(captured);
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
