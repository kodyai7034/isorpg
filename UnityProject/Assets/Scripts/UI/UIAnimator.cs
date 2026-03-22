using System;
using System.Collections;
using UnityEngine;

namespace IsoRPG.UI
{
    /// <summary>
    /// Centralized tween/animation utility for all UI motion.
    /// Uses coroutines + AnimationCurve — no external dependencies.
    /// </summary>
    public static class UIAnimator
    {
        private static readonly AnimationCurve EaseOut = AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>Slide a RectTransform from an offset to its current position.</summary>
        public static Coroutine SlideIn(MonoBehaviour host, RectTransform target,
            Vector2 fromOffset, float duration = 0.3f, Action onComplete = null)
        {
            return host.StartCoroutine(SlideCoroutine(target, fromOffset, Vector2.zero, duration, onComplete));
        }

        /// <summary>Slide a RectTransform from its current position to an offset.</summary>
        public static Coroutine SlideOut(MonoBehaviour host, RectTransform target,
            Vector2 toOffset, float duration = 0.25f, Action onComplete = null)
        {
            return host.StartCoroutine(SlideCoroutine(target, Vector2.zero, toOffset, duration, onComplete));
        }

        /// <summary>Scale punch — grow then shrink back. Good for button press.</summary>
        public static Coroutine PunchScale(MonoBehaviour host, Transform target,
            float punchScale = 1.1f, float duration = 0.15f)
        {
            return host.StartCoroutine(PunchScaleCoroutine(target, punchScale, duration));
        }

        /// <summary>Hover scale — smoothly scale to target size.</summary>
        public static Coroutine ScaleTo(MonoBehaviour host, Transform target,
            float targetScale, float duration = 0.1f)
        {
            return host.StartCoroutine(ScaleToCoroutine(target, targetScale, duration));
        }

        /// <summary>Pulsing opacity for tile overlays (breathing effect).</summary>
        public static Coroutine PulseAlpha(MonoBehaviour host, SpriteRenderer target,
            float minAlpha = 0.4f, float maxAlpha = 0.8f, float speed = 2f)
        {
            return host.StartCoroutine(PulseAlphaCoroutine(target, minAlpha, maxAlpha, speed));
        }

        private static IEnumerator SlideCoroutine(RectTransform target,
            Vector2 fromOffset, Vector2 toOffset, float duration, Action onComplete)
        {
            if (target == null) yield break;

            var basePos = target.anchoredPosition - fromOffset; // original position
            var from = basePos + fromOffset;
            var to = basePos + toOffset;

            // Snap to start
            target.anchoredPosition = from;
            target.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curved = EaseOut.Evaluate(t);
                target.anchoredPosition = Vector2.Lerp(from, to, curved);
                yield return null;
            }

            target.anchoredPosition = to;
            onComplete?.Invoke();
        }

        private static IEnumerator PunchScaleCoroutine(Transform target, float punchScale, float duration)
        {
            if (target == null) yield break;

            var original = target.localScale;
            var punched = original * punchScale;
            float half = duration * 0.4f;

            // Scale up
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                target.localScale = Vector3.Lerp(original, punched, EaseOut.Evaluate(t));
                yield return null;
            }

            // Scale back
            elapsed = 0f;
            float remaining = duration - half;
            while (elapsed < remaining)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / remaining);
                target.localScale = Vector3.Lerp(punched, original, EaseOut.Evaluate(t));
                yield return null;
            }

            target.localScale = original;
        }

        private static IEnumerator ScaleToCoroutine(Transform target, float targetScale, float duration)
        {
            if (target == null) yield break;

            var from = target.localScale;
            var to = Vector3.one * targetScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.Lerp(from, to, EaseOut.Evaluate(t));
                yield return null;
            }

            target.localScale = to;
        }

        private static IEnumerator PulseAlphaCoroutine(SpriteRenderer target,
            float minAlpha, float maxAlpha, float speed)
        {
            if (target == null) yield break;

            while (target != null && target.gameObject.activeInHierarchy)
            {
                float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
                var c = target.color;
                c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
                target.color = c;
                yield return null;
            }
        }
    }
}
