using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace View
{
    /// <summary>
    /// Two curtains that part to reveal the new screen. The screen swap happens in the same frame the
    /// curtains appear fully closed, so game logic stays synchronous and nothing flashes.
    /// </summary>
    public sealed class ScreenTransitionView : MonoBehaviour
    {
        private const int SORTING_ORDER = 5000;
        private const float HOLD_SECONDS = 0.08f;

        private static readonly Color CURTAIN_COLOR = new Color(0.55f, 0.78f, 0.95f, 1f);

        [SerializeField] private float _openDuration = 0.5f;

        private GameObject _root;
        private RectTransform _rootRect;
        private RectTransform _left;
        private RectTransform _right;

        private void Awake()
        {
            _root = new GameObject("ScreenTransitionCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);

            Canvas canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SORTING_ORDER;
            _rootRect = (RectTransform)_root.transform;

            _left = CreateCurtain("Left", 0f, 0.5f);
            _right = CreateCurtain("Right", 0.5f, 1f);
            _root.SetActive(false);
        }

        private void OnDestroy()
        {
            Tween.StopAll(_left);
            Tween.StopAll(_right);
        }

        private RectTransform CreateCurtain(string objectName, float anchorMinX, float anchorMaxX)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(_rootRect, false);
            rect.anchorMin = new Vector2(anchorMinX, 0f);
            rect.anchorMax = new Vector2(anchorMaxX, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = CURTAIN_COLOR;
            return rect;
        }

        public void PlayOpen()
        {
            Tween.StopAll(_left);
            Tween.StopAll(_right);

            _left.anchoredPosition = Vector2.zero;
            _right.anchoredPosition = Vector2.zero;
            _root.SetActive(true);

            float travel = _rootRect.rect.width * 0.5f + 8f;
            Tween.UIAnchoredPositionX(_left, -travel, _openDuration, Ease.InCubic, startDelay: HOLD_SECONDS);
            Tween.UIAnchoredPositionX(_right, travel, _openDuration, Ease.InCubic, startDelay: HOLD_SECONDS)
                .OnComplete(_root, static target => target.SetActive(false), warnIfTargetDestroyed: false);
        }
    }
}
