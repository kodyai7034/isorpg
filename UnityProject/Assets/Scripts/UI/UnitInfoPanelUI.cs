using UnityEngine;
using UnityEngine.UI;
using IsoRPG.Core;
using IsoRPG.Units;

namespace IsoRPG.UI
{
    /// <summary>
    /// Displays stats for the selected or hovered unit.
    /// Subscribes to damage/healing events to animate HP bar changes.
    /// </summary>
    public class UnitInfoPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMPro.TextMeshProUGUI nameText;
        [SerializeField] private TMPro.TextMeshProUGUI levelJobText;
        [SerializeField] private TMPro.TextMeshProUGUI statsText;
        [SerializeField] private Slider hpBar;
        [SerializeField] private Slider mpBar;
        [SerializeField] private TMPro.TextMeshProUGUI hpText;
        [SerializeField] private TMPro.TextMeshProUGUI mpText;
        [SerializeField] private Image teamColorBorder;

        private UnitInstance _currentUnit;

        private void OnEnable()
        {
            GameEvents.DamageDealt.Subscribe(OnDamageDealt);
            GameEvents.HealingDealt.Subscribe(OnHealingDealt);
        }

        private void OnDisable()
        {
            GameEvents.DamageDealt.Unsubscribe(OnDamageDealt);
            GameEvents.HealingDealt.Unsubscribe(OnHealingDealt);
        }

        /// <summary>Show info for a unit. Call on hover or selection.</summary>
        public void ShowUnit(UnitInstance unit)
        {
            if (unit == null)
            {
                Hide();
                return;
            }

            _currentUnit = unit;
            gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>Hide the panel.</summary>
        public void Hide()
        {
            _currentUnit = null;
            gameObject.SetActive(false);
        }

        private void Refresh()
        {
            if (_currentUnit == null) return;

            var u = _currentUnit;

            if (nameText != null)
                nameText.text = u.Name;

            if (levelJobText != null)
                levelJobText.text = $"Lv.{u.Level} {u.CurrentJob}";

            if (statsText != null)
                statsText.text = $"PA:{u.Stats.PhysicalAttack}  MA:{u.Stats.MagicAttack}  SPD:{u.Stats.Speed}";

            if (hpBar != null)
            {
                hpBar.maxValue = u.Stats.MaxHP;
                hpBar.value = u.CurrentHP;
            }

            if (mpBar != null)
            {
                mpBar.maxValue = u.Stats.MaxMP;
                mpBar.value = u.CurrentMP;
            }

            if (hpText != null)
                hpText.text = $"{u.CurrentHP}/{u.Stats.MaxHP}";

            if (mpText != null)
                mpText.text = $"{u.CurrentMP}/{u.Stats.MaxMP}";

            if (teamColorBorder != null)
            {
                teamColorBorder.color = u.Team == 0
                    ? new Color(0.3f, 0.5f, 0.9f)
                    : new Color(0.9f, 0.3f, 0.3f);
            }
        }

        private void OnDamageDealt(DamageDealtArgs args)
        {
            if (_currentUnit != null && _currentUnit.Id == args.TargetId)
                Refresh();
        }

        private void OnHealingDealt(HealingDealtArgs args)
        {
            if (_currentUnit != null && _currentUnit.Id == args.TargetId)
                Refresh();
        }
    }
}
