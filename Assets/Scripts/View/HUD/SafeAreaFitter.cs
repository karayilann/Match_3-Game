using UnityEngine;

namespace View.HUD
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _appliedSafeArea;
        private int _appliedWidth;
        private int _appliedHeight;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        /// <summary>
        /// Re-applies on rotation, window resize or display cutout changes. The comparison is a few
        /// value compares per frame and allocates nothing.
        /// </summary>
        private void Update()
        {
            if (Screen.safeArea != _appliedSafeArea || Screen.width != _appliedWidth || Screen.height != _appliedHeight)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            _appliedSafeArea = safeArea;
            _appliedWidth = screenWidth;
            _appliedHeight = screenHeight;

            float inverseWidth = 1f / screenWidth;
            float inverseHeight = 1f / screenHeight;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x *= inverseWidth;
            anchorMin.y *= inverseHeight;
            anchorMax.x *= inverseWidth;
            anchorMax.y *= inverseHeight;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
