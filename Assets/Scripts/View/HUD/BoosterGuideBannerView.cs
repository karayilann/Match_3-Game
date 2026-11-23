using Core.Data.Boosters;
using PrimeTween;
using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View.HUD
{
    /// <summary>
    /// The banner is built in code under the host's canvas so it needs no scene wiring.
    /// </summary>
    public sealed class BoosterGuideBannerView : MonoBehaviour
    {
        private const float FADE_DURATION = 0.18f;
        private const string HAMMER_MESSAGE = "Tap the tile you want to break";
        private const string GLOVE_MESSAGE = "Swipe a tile to swap it with its neighbor";
        private const string CANCEL_SUFFIX = "\nTap the booster again to cancel";

        private static readonly Color BACKGROUND_COLOR = new Color(0.05f, 0.1f, 0.2f, 0.88f);

        [SerializeField] private RectTransform _parent;
        [SerializeField] private Vector2 _anchor = new Vector2(0.5f, 0.78f);

        private CanvasGroup _group;
        private TMP_Text _label;
        private Tween _fadeTween;

        private void Awake()
        {
            Build();
        }

        private void OnEnable()
        {
            EconomySignals.OnBoosterSelected += HandleBoosterSelected;
            _group.alpha = 0f;
            _group.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            EconomySignals.OnBoosterSelected -= HandleBoosterSelected;
            if (_fadeTween.isAlive)
            {
                _fadeTween.Stop();
            }
        }

        private void Build()
        {
            RectTransform host = _parent != null ? _parent : (RectTransform)transform;

            GameObject root = new GameObject("BoosterGuideBanner", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            RectTransform rect = (RectTransform)root.transform;
            rect.SetParent(host, false);
            rect.anchorMin = _anchor;
            rect.anchorMax = _anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 120f);

            Image background = root.GetComponent<Image>();
            background.color = BACKGROUND_COLOR;
            background.raycastTarget = false;

            _group = root.GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            GameObject textObject = new GameObject("Label", typeof(RectTransform));
            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 8f);
            textRect.offsetMax = new Vector2(-24f, -8f);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 36f;
            text.color = Color.white;
            text.raycastTarget = false;
            _label = text;
        }

        private void HandleBoosterSelected(BoosterType type)
        {
            if (_fadeTween.isAlive)
            {
                _fadeTween.Stop();
            }

            if (type == BoosterType.None)
            {
                _fadeTween = Tween.Alpha(_group, 0f, FADE_DURATION)
                    .OnComplete(_group.gameObject, static target => target.SetActive(false), warnIfTargetDestroyed: false);
                return;
            }

            string message = type == BoosterType.Hammer ? HAMMER_MESSAGE : GLOVE_MESSAGE;
            _label.SetText(message + CANCEL_SUFFIX);
            _group.gameObject.SetActive(true);
            _fadeTween = Tween.Alpha(_group, 1f, FADE_DURATION);
        }
    }
}
