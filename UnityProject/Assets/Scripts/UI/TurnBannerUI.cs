using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using IsoRPG.Core;

namespace IsoRPG.UI
{
    /// <summary>
    /// Animated banner that slides in when a unit's turn starts.
    /// Shows unit name + team color. Slides out after a brief hold.
    /// </summary>
    public class TurnBannerUI : MonoBehaviour
    {
        [SerializeField] private RectTransform bannerRect;
        [SerializeField] private TMPro.TextMeshProUGUI nameText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private float slideInDuration = 0.4f;
        [SerializeField] private float holdDuration = 0.8f;
        [SerializeField] private float slideOutDuration = 0.3f;

        private Coroutine _activeAnimation;

        private void OnEnable()
        {
            GameEvents.TurnStarted.Subscribe(OnTurnStarted);
        }

        private void OnDisable()
        {
            GameEvents.TurnStarted.Unsubscribe(OnTurnStarted);
        }

        private void Start()
        {
            if (bannerRect != null)
                bannerRect.gameObject.SetActive(false);
        }

        private void OnTurnStarted(TurnStartedArgs args)
        {
            // Find unit name from all UnitViews in scene
            string unitName = "Unknown";
            int team = 0;
            var views = FindObjectsByType<Units.UnitView>(FindObjectsSortMode.None);
            foreach (var view in views)
            {
                if (view.Unit != null && view.Unit.Id == args.UnitId)
                {
                    unitName = view.Unit.Name;
                    team = view.Unit.Team;
                    break;
                }
            }

            if (_activeAnimation != null)
                StopCoroutine(_activeAnimation);

            _activeAnimation = StartCoroutine(ShowBanner(unitName, team));
        }

        private IEnumerator ShowBanner(string unitName, int team)
        {
            if (bannerRect == null) yield break;

            // Configure
            if (nameText != null)
                nameText.text = unitName;

            if (backgroundImage != null)
            {
                backgroundImage.color = team == 0
                    ? new Color(0.15f, 0.3f, 0.6f, 0.9f)
                    : new Color(0.6f, 0.15f, 0.15f, 0.9f);
            }

            // Slide in from left
            bannerRect.gameObject.SetActive(true);
            var startPos = new Vector2(-600, bannerRect.anchoredPosition.y);
            var endPos = new Vector2(0, bannerRect.anchoredPosition.y);

            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / slideInDuration);
                bannerRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }
            bannerRect.anchoredPosition = endPos;

            SFXManager.Instance?.PlayTurnStart();

            // Hold
            yield return new WaitForSeconds(holdDuration);

            // Slide out to right
            var outPos = new Vector2(600, endPos.y);
            elapsed = 0f;
            while (elapsed < slideOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / slideOutDuration);
                bannerRect.anchoredPosition = Vector2.Lerp(endPos, outPos, t);
                yield return null;
            }

            bannerRect.gameObject.SetActive(false);
            _activeAnimation = null;
        }
    }
}
