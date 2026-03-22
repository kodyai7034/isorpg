using System.Collections;
using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.UI
{
    /// <summary>
    /// Spawns floating damage/heal numbers over units.
    /// Subscribes to DamageDealt and HealingDealt events.
    /// Numbers float upward and fade out over time.
    /// </summary>
    public class DamageNumberUI : MonoBehaviour
    {
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float fadeDuration = 1f;
        [SerializeField] private Canvas worldSpaceCanvas;

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

            // Find unit position from registry (or use a position lookup)
            // For now, spawn at a default position — BattleManager should set up a position resolver
            SpawnNumber(args.Amount.ToString(), Color.red);
        }

        private void OnHealingDealt(HealingDealtArgs args)
        {
            if (args.Amount <= 0) return;
            SpawnNumber(args.Amount.ToString(), Color.green);
        }

        /// <summary>
        /// Spawn a floating damage number at a world position.
        /// </summary>
        /// <param name="worldPos">World-space position to spawn at.</param>
        /// <param name="amount">Damage/heal amount.</param>
        /// <param name="color">Text color (red for damage, green for heal).</param>
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

        private void SpawnNumber(string text, Color color)
        {
            // Without world position, spawn at screen center (placeholder)
            if (damageNumberPrefab == null) return;
            var pos = Camera.main != null
                ? Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, 0))
                : Vector3.zero;
            SpawnAt(pos, text, color);
        }

        private void SpawnAt(Vector3 worldPos, string text, Color color)
        {
            if (damageNumberPrefab == null) return;

            var parent = worldSpaceCanvas != null ? worldSpaceCanvas.transform : transform;
            var go = Instantiate(damageNumberPrefab, parent);
            go.transform.position = worldPos;

            var tmp = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text;
                tmp.color = color;
            }

            // Also try TextMeshPro for world-space
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

                // Float upward
                go.transform.position = startPos + Vector3.up * (floatSpeed * t);

                // Fade out
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
