using UnityEngine;

namespace Gameplay.Board
{
    public sealed class CameraFitter : MonoBehaviour
    {
        private const float BOARD_MARGIN_CELLS = 0.5f;

        [SerializeField] private Camera _camera;
        [SerializeField] private SpriteRenderer _backgroundRenderer;
        [SerializeField] private float _verticalOffsetCells = 0.65f;

        [Header("HUD Reserved Areas")]
        [SerializeField] private RectTransform _topReservedRect;
        [SerializeField] private RectTransform _bottomReservedRect;

        private readonly Vector3[] _corners = new Vector3[4];

        public void Fit(byte boardWidth, byte boardHeight, float cellSize, float horizontalPaddingCells = 0.75f, float verticalPaddingCells = 3.6f)
        {
            float screenAspect = (float)Screen.width / Screen.height;
            float targetWidthUnits = (boardWidth + horizontalPaddingCells) * cellSize;

            float orthoSize;
            float cameraY;

            if (TryGetReservedPixels(out float topPixels, out float bottomPixels))
            {
                float availablePixels = Screen.height - topPixels - bottomPixels;
                float targetHeightUnits = (boardHeight + BOARD_MARGIN_CELLS * 2f) * cellSize;

                float orthoForHeight = targetHeightUnits * Screen.height / (2f * availablePixels);
                float orthoForWidth = (targetWidthUnits * 0.5f) / screenAspect;
                orthoSize = Mathf.Max(orthoForHeight, orthoForWidth);

                float unitsPerPixel = orthoSize * 2f / Screen.height;
                float regionCenterPixels = bottomPixels + availablePixels * 0.5f;
                cameraY = -(regionCenterPixels - Screen.height * 0.5f) * unitsPerPixel;
            }
            else
            {
                float targetHeightUnits = (boardHeight + verticalPaddingCells) * cellSize;
                orthoSize = Mathf.Max((targetWidthUnits * 0.5f) / screenAspect, targetHeightUnits * 0.5f);
                cameraY = _verticalOffsetCells * cellSize;
            }

            _camera.orthographicSize = orthoSize;

            Transform camTransform = _camera.transform;
            Vector3 pos = camTransform.position;
            pos.x = 0f;
            pos.y = cameraY;
            camTransform.position = pos;

            FitBackground(orthoSize, screenAspect, pos);
        }

        /// <summary>
        /// HUD rects of an overlay canvas report world corners in screen pixels, so the free band between the
        /// top and bottom HUD can be read directly. Inactive or unset rects fall back to fixed padding.
        /// </summary>
        private bool TryGetReservedPixels(out float topPixels, out float bottomPixels)
        {
            topPixels = 0f;
            bottomPixels = 0f;

            if (_topReservedRect == null || _bottomReservedRect == null
                || !_topReservedRect.gameObject.activeInHierarchy || !_bottomReservedRect.gameObject.activeInHierarchy)
            {
                return false;
            }

            _topReservedRect.GetWorldCorners(_corners);
            topPixels = Screen.height - _corners[0].y;

            _bottomReservedRect.GetWorldCorners(_corners);
            bottomPixels = _corners[1].y;

            return Screen.height - topPixels - bottomPixels > Screen.height * 0.3f;
        }

        private void FitBackground(float orthoSize, float screenAspect, Vector3 cameraPos)
        {
            if (_backgroundRenderer == null || _backgroundRenderer.sprite == null)
            {
                return;
            }

            Transform bgTransform = _backgroundRenderer.transform;
            bgTransform.position = new Vector3(cameraPos.x, cameraPos.y, 0f);

            Vector2 spriteSize = _backgroundRenderer.sprite.bounds.size;
            float viewHeight = orthoSize * 2f;
            float viewWidth = viewHeight * screenAspect;

            float scaleX = viewWidth / spriteSize.x;
            float scaleY = viewHeight / spriteSize.y;
            float uniformScale = Mathf.Max(scaleX, scaleY) * 1.05f;

            bgTransform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        }
    }
}
