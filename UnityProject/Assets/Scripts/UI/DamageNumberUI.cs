using System.Collections;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Units;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.UI
{
    /// <summary>
    /// Spawns floating damage/heal numbers over units.
    /// Subscribes to DamageDealt and HealingDealt events.
    /// Numbers float upward and fade out over time.
    /// Locates unit positions via UnitView.FindByEntityId.
    /// </summary>
    public class DamageNumberUI : MonoBehaviour
    {
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float fadeDuration = 1f;

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

        private void OnDamageDealt(DamageDealtArgs args)
        {
            if (args.Amount <= 0) return;
            var pos = FindUnitWorldPos(args.TargetId);
            SpawnAt(pos, args.Amount.ToString(), Color.red);
        }

        private void OnHealingDealt(HealingDealtArgs args)
        {
            if (args.Amount <= 0) return;
            var pos = FindUnitWorldPos(args.TargetId);
            SpawnAt(pos, args.Amount.ToString(), Color.green);
        }

        /// <summary>
        /// Spawn a floating damage number at a world position.
        /// </summary>
        public void SpawnDamageNumber(Vector3 worldPos, int amount, Color color)
        {
            SpawnAt(worldPos, amount.ToString(), color);
        }

        /// <summary>
        /// Spawn a "MISS" text at a world position.
        /// </summary>
        public void SpawnMissText(Vector3 worldPos)
        {
            SpawnAt(worldPos, "MISS", Color.gray);
        }

        private Vector3 FindUnitWorldPos(EntityId targetId)
        {
            // Find the UnitView by searching scene — acceptable for infrequent damage events
            var views = FindObjectsByType<UnitView>(FindObjectsSortMode.None);
            foreach (var view in views)
            {
                if (view.Unit != null && view.Unit.Id == targetId)
                    return view.transform.position + Vector3.up * 0.3f;
            }

            // Fallback: screen center
            if (Camera.main != null)
                return Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            return Vector3.zero;
        }

        private void SpawnAt(Vector3 worldPos, string text, Color color)
        {
            if (damageNumberPrefab == null) return;

            var go = Instantiate(damageNumberPrefab, transform);
            go.transform.position = worldPos;

            var tmp = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text;
                tmp.color = color;
            }

            var tmpWorld = go.GetComponentInChildren<TMPro.TextMeshPro>();
            if (tmpWorld != null)
            {
                tmpWorld.text = text;
                tmpWorld.color = color;
            }

            StartCoroutine(AnimateAndDestroy(go, color));
        }

        private IEnumerator AnimateAndDestroy(GameObject go, Color startColor)
        {
            float elapsed = 0f;
            var startPos = go.transform.position;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;

                go.transform.position = startPos + Vector3.up * (floatSpeed * t);

                var tmp = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    var c = startColor;
                    c.a = 1f - t;
                    tmp.color = c;
                }

                var tmpWorld = go.GetComponentInChildren<TMPro.TextMeshPro>();
                if (tmpWorld != null)
                {
                    var c = startColor;
                    c.a = 1f - t;
                    tmpWorld.color = c;
                }

                yield return null;
            }

            Destroy(go);
        }
    }
}
