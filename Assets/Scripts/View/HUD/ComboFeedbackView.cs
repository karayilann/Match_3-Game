using System.Collections.Generic;
using PrimeTween;
using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View.HUD
{
    /// <summary>
    /// Rising score popups at the cleared area and a centered combo callout that grows with the cascade.
    /// All text objects are built once in Awake and reused.
    /// </summary>
    public sealed class ComboFeedbackView : MonoBehaviour
    {
        private const int POPUP_CAPACITY = 12;
        private const float POPUP_RISE_PIXELS = 115f;
        private const float POPUP_DURATION = 0.78f;
        private const float CALLOUT_DURATION = 1.05f;
        private const float CALLOUT_FADE_DURATION = 0.30f;
        private const float CALLOUT_RISE_PIXELS = 32f;
        private const float CALLOUT_HEIGHT_RATIO = 0.60f;

        private static readonly string[] TIER_LABELS = { null, "Nice!", "Great!", "Awesome!", "Amazing!" };

        private static readonly Color[] TIER_TOP_COLORS =
        {
            Color.white,
            new Color(0.95f, 0.99f, 1f, 1f),
            new Color(0.95f, 1f, 0.92f, 1f),
            new Color(1f, 0.98f, 0.86f, 1f),
            new Color(1f, 0.94f, 0.96f, 1f)
        };

        private static readonly Color[] TIER_BOTTOM_COLORS =
        {
            Color.white,
            new Color(0.48f, 0.88f, 1f, 1f),
            new Color(0.50f, 0.96f, 0.58f, 1f),
            new Color(1f, 0.78f, 0.25f, 1f),
            new Color(1f, 0.48f, 0.72f, 1f)
        };

        private static readonly Color TEXT_OUTLINE_COLOR = new Color(0.05f, 0.09f, 0.20f, 0.95f);
        private static readonly Color TEXT_SHADOW_COLOR = new Color(0.02f, 0.04f, 0.10f, 0.75f);
        private static readonly Color SOFT_BACKDROP_COLOR = new Color(0.04f, 0.08f, 0.18f, 0.42f);
        private static readonly Color SCORE_TOP_COLOR = new Color(1f, 1f, 0.92f, 1f);
        private static readonly Color SCORE_BOTTOM_COLOR = new Color(1f, 0.86f, 0.38f, 1f);
        private static readonly Vector3 CALLOUT_START_EULER = new Vector3(0f, 0f, -3f);

        private static Sprite _softGlowSprite;

        [SerializeField] private Canvas _canvas;
        [SerializeField] private Camera _worldCamera;

        private readonly List<TMP_Text> _popups = new List<TMP_Text>(POPUP_CAPACITY);
        private RectTransform _canvasRect;
        private RectTransform _calloutRoot;
        private CanvasGroup _calloutGroup;
        private TMP_Text _calloutText;
        private int _nextPopup;
        private int _activeTier;

        private void Awake()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            _canvasRect = (RectTransform)_canvas.transform;

            for (int i = 0; i < POPUP_CAPACITY; i++)
            {
                _popups.Add(CreateScorePopupText());
            }

            CreateCallout();
        }

        private void OnEnable()
        {
            BoardSignals.OnMatchScored += HandleMatchScored;
        }

        private void OnDisable()
        {
            BoardSignals.OnMatchScored -= HandleMatchScored;

            for (int i = 0; i < _popups.Count; i++)
            {
                StopPopup(_popups[i]);
            }

            StopCallout();
        }

        private static void StopPopup(TMP_Text text)
        {
            Tween.StopAll(text.rectTransform);
            Tween.StopAll(text);
            text.gameObject.SetActive(false);
        }

        private void StopCallout()
        {
            if (_calloutRoot == null)
            {
                return;
            }

            Tween.StopAll(_calloutRoot);
            Tween.StopAll(_calloutGroup);
            _activeTier = 0;
            _calloutRoot.gameObject.SetActive(false);
        }

        private TMP_Text CreateScorePopupText()
        {
            GameObject go = new GameObject("ScorePopup", typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(_canvasRect, false);
            rect.sizeDelta = new Vector2(340f, 96f);

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 50f;
            text.fontStyle = FontStyles.Bold;
            text.raycastTarget = false;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(SCORE_TOP_COLOR, SCORE_TOP_COLOR, SCORE_BOTTOM_COLOR, SCORE_BOTTOM_COLOR);

            ConfigureSdfOutlineAndShadow(text, 0.24f, 0.20f);
            go.SetActive(false);
            return text;
        }

        private void CreateCallout()
        {
            GameObject rootGo = new GameObject("ComboCallout", typeof(RectTransform), typeof(CanvasGroup));
            _calloutRoot = (RectTransform)rootGo.transform;
            _calloutRoot.SetParent(_canvasRect, false);
            _calloutRoot.sizeDelta = new Vector2(540f, 128f);

            _calloutGroup = rootGo.GetComponent<CanvasGroup>();
            _calloutGroup.blocksRaycasts = false;
            _calloutGroup.interactable = false;

            GameObject glowGo = new GameObject("SoftBackdrop", typeof(RectTransform), typeof(Image));
            RectTransform glowRect = (RectTransform)glowGo.transform;
            glowRect.SetParent(_calloutRoot, false);
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;

            Image glowImage = glowGo.GetComponent<Image>();
            glowImage.sprite = GetOrCreateSoftGlowSprite();
            glowImage.color = SOFT_BACKDROP_COLOR;
            glowImage.raycastTarget = false;

            GameObject textGo = new GameObject("CalloutLabel", typeof(RectTransform));
            RectTransform textRect = (RectTransform)textGo.transform;
            textRect.SetParent(_calloutRoot, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 74f;
            text.fontStyle = FontStyles.Bold;
            text.raycastTarget = false;
            text.enableVertexGradient = true;

            ConfigureSdfOutlineAndShadow(text, 0.27f, 0.24f);
            _calloutText = text;
            rootGo.SetActive(false);
        }

        private static Sprite GetOrCreateSoftGlowSprite()
        {
            if (_softGlowSprite != null)
            {
                return _softGlowSprite;
            }

            const int width = 64;
            const int height = 32;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color32[] pixels = new Color32[width * height];
            float invHalfW = 2f / (width - 1f);
            float invHalfH = 2f / (height - 1f);

            for (int y = 0; y < height; y++)
            {
                float ny = y * invHalfH - 1f;
                for (int x = 0; x < width; x++)
                {
                    float nx = x * invHalfW - 1f;
                    float distSq = nx * nx + ny * ny;
                    float t = Mathf.Clamp01(1f - distSq);
                    byte alpha = (byte)(t * t * 255f);
                    pixels[y * width + x] = new Color32(255, 255, 255, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            _softGlowSprite = Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            return _softGlowSprite;
        }

        private static void ConfigureSdfOutlineAndShadow(TextMeshProUGUI text, float outlineWidth, float underlayDilate)
        {
            Material mat = text.fontMaterial;
            mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, TEXT_OUTLINE_COLOR);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);

            mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, TEXT_SHADOW_COLOR);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.45f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, underlayDilate);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.25f);

            text.UpdateMeshPadding();
        }

        private void HandleMatchScored(Vector3 worldPosition, int points, byte cascadeStep, byte clearedCount)
        {
            if (points > 0)
            {
                ShowScorePopup(worldPosition, points);
            }

            int tier = GetTier(cascadeStep, clearedCount);
            if (tier > 0)
            {
                ShowCallout(tier);
            }
        }

        private static int GetTier(byte cascadeStep, byte clearedCount)
        {
            if (cascadeStep >= 5 || clearedCount >= 12)
            {
                return 4;
            }

            if (cascadeStep == 4 || clearedCount >= 9)
            {
                return 3;
            }

            if (cascadeStep == 3 || clearedCount >= 6)
            {
                return 2;
            }

            return cascadeStep == 2 || clearedCount >= 5 ? 1 : 0;
        }

        private void ShowScorePopup(Vector3 worldPosition, int points)
        {
            Camera cam = _worldCamera != null ? _worldCamera : Camera.main;
            if (cam == null)
            {
                return;
            }

            TMP_Text text = _popups[_nextPopup];
            _nextPopup = (_nextPopup + 1) % POPUP_CAPACITY;

            Vector3 screen = cam.WorldToScreenPoint(worldPosition);
            Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvasRect, screen, uiCamera, out Vector3 start);

            RectTransform rect = text.rectTransform;
            Tween.StopAll(rect);
            Tween.StopAll(text);

            rect.position = start;
            rect.localScale = Vector3.one * 0.6f;
            text.SetText("+{0}", points);
            text.color = Color.white;
            text.gameObject.SetActive(true);
            rect.SetAsLastSibling();

            float rise = POPUP_RISE_PIXELS * _canvas.scaleFactor;
            Tween.Scale(rect, Vector3.one, 0.20f, Ease.OutBack);
            Tween.PositionY(rect, start.y + rise, POPUP_DURATION, Ease.OutQuad);
            Tween.Alpha(text, 0f, POPUP_DURATION * 0.38f, Ease.InQuad, startDelay: POPUP_DURATION * 0.62f)
                .OnComplete(text.gameObject, static target => target.SetActive(false), warnIfTargetDestroyed: false);
        }

        /// <summary>
        /// A smaller follow-up cascade must not overwrite a higher-tier callout that is still on screen.
        /// </summary>
        private void ShowCallout(int tier)
        {
            if (_calloutRoot.gameObject.activeSelf && tier < _activeTier)
            {
                return;
            }

            _activeTier = tier;
            Tween.StopAll(_calloutRoot);
            Tween.StopAll(_calloutGroup);

            Color topColor = TIER_TOP_COLORS[tier];
            Color bottomColor = TIER_BOTTOM_COLORS[tier];

            _calloutText.SetText(TIER_LABELS[tier]);
            _calloutText.color = Color.white;
            _calloutText.colorGradient = new VertexGradient(topColor, topColor, bottomColor, bottomColor);

            _calloutGroup.alpha = 1f;
            _calloutRoot.gameObject.SetActive(true);
            _calloutRoot.SetAsLastSibling();

            Vector2 size = _canvasRect.rect.size;
            float startY = size.y * (CALLOUT_HEIGHT_RATIO - 0.5f);
            _calloutRoot.anchoredPosition = new Vector2(0f, startY);
            _calloutRoot.localScale = Vector3.zero;
            _calloutRoot.localEulerAngles = CALLOUT_START_EULER;

            float targetScale = 1f + (tier - 1) * 0.05f;
            Tween.Scale(_calloutRoot, Vector3.one * targetScale, 0.24f, Ease.OutBack);
            Tween.LocalRotation(_calloutRoot, Quaternion.identity, 0.24f, Ease.OutBack);
            Tween.UIAnchoredPositionY(_calloutRoot, startY + CALLOUT_RISE_PIXELS, CALLOUT_DURATION, Ease.OutQuad);
            Tween.Alpha(_calloutGroup, 0f, CALLOUT_FADE_DURATION, Ease.InQuad, startDelay: CALLOUT_DURATION - CALLOUT_FADE_DURATION)
                .OnComplete(this, static self =>
                {
                    self._activeTier = 0;
                    self._calloutRoot.gameObject.SetActive(false);
                }, warnIfTargetDestroyed: false);
        }
    }
}
