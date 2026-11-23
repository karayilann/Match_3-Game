using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Core.Data.Tiles;
using Gameplay.Board;
using Signals;

namespace Gameplay.Input
{
    public sealed class BoardInputHandler : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _swipeThreshold = 50f;

        private GridToWorldConverter _converter;
        private InputAction _pressAction;
        private InputAction _positionAction;
        private Vector2 _startScreenPosition;
        private GridCoord _startCoord;
        private bool _inputEnabled = true;
        private bool _hasValidStart;
        private bool _isFinaleSkipEnabled;

        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(8);
        private PointerEventData _pointerEventData;

        private bool _hasBufferedSwipe;
        private GridCoord _bufferedStart;
        private GridCoord _bufferedTarget;

        public void Init(GridToWorldConverter converter)
        {
            _converter = converter;
            _isFinaleSkipEnabled = false;

            if (_pressAction != null)
            {
                return;
            }

            _pressAction = new InputAction(binding: "<Pointer>/press");
            _positionAction = new InputAction(binding: "<Pointer>/position");
            _pressAction.started += OnPointerDown;
            _pressAction.canceled += OnPointerUp;
            _pressAction.Enable();
            _positionAction.Enable();
        }

        /// <summary>
        /// The handler outlives each level, so a stale converter would keep turning menu and
        /// popup touches into swaps on a board whose views were already released.
        /// </summary>
        public void Deactivate()
        {
            _converter = null;
            _inputEnabled = false;
            _hasValidStart = false;
            _hasBufferedSwipe = false;
            _isFinaleSkipEnabled = false;
        }

        public void SetFinaleSkipEnabled(bool enabled)
        {
            _isFinaleSkipEnabled = enabled;
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;

            if (enabled && _hasBufferedSwipe)
            {
                _hasBufferedSwipe = false;
                BoardSignals.RaiseTileSwiped(_bufferedStart, _bufferedTarget);
            }
        }

        public void ClearBuffer()
        {
            _hasBufferedSwipe = false;
            _hasValidStart = false;
        }

        private void OnDestroy()
        {
            if (_pressAction == null)
            {
                return;
            }

            _pressAction.started -= OnPointerDown;
            _pressAction.canceled -= OnPointerUp;
            _pressAction.Dispose();
            _positionAction.Dispose();
        }

        private void OnPointerDown(InputAction.CallbackContext context)
        {
            if (_converter == null)
            {
                return;
            }

            if (_isFinaleSkipEnabled)
            {
                _hasValidStart = false;
                LevelSignals.RaiseFinaleSkipRequested();
                return;
            }

            _startScreenPosition = _positionAction.ReadValue<Vector2>();

            if (IsPointerOverUi(_startScreenPosition))
            {
                _hasValidStart = false;
                return;
            }

            Vector3 worldPosition = _camera.ScreenToWorldPoint(_startScreenPosition);
            worldPosition.z = 0f;

            _hasValidStart = _converter.TryToGridCoord(worldPosition, out _startCoord);
        }

        /// <summary>
        /// IsPointerOverGameObject reads last frame's UI state when called from an InputAction
        /// callback, so the current position is raycast against the UI directly instead.
        /// </summary>
        private bool IsPointerOverUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (_pointerEventData == null)
            {
                _pointerEventData = new PointerEventData(eventSystem);
            }

            _pointerEventData.position = screenPosition;
            _raycastResults.Clear();
            eventSystem.RaycastAll(_pointerEventData, _raycastResults);
            return _raycastResults.Count > 0;
        }

        private void OnPointerUp(InputAction.CallbackContext context)
        {
            if (!_hasValidStart)
            {
                return;
            }

            _hasValidStart = false;

            Vector2 endScreenPosition = _positionAction.ReadValue<Vector2>();
            Vector2 swipeDelta = endScreenPosition - _startScreenPosition;

            if (swipeDelta.sqrMagnitude < _swipeThreshold * _swipeThreshold)
            {
                if (_inputEnabled)
                {
                    BoardSignals.RaiseCellTapped(_startCoord);
                }
                return;
            }

            if (TryCalculateSwipeTarget(_startCoord, swipeDelta.normalized, out GridCoord targetCoord))
            {
                if (_inputEnabled)
                {
                    BoardSignals.RaiseTileSwiped(_startCoord, targetCoord);
                }
                else
                {
                    _hasBufferedSwipe = true;
                    _bufferedStart = _startCoord;
                    _bufferedTarget = targetCoord;
                }
            }
        }

        private bool TryCalculateSwipeTarget(GridCoord startCoord, Vector2 direction, out GridCoord targetCoord)
        {
            int targetX = startCoord.X;
            int targetY = startCoord.Y;

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                targetX += direction.x > 0 ? 1 : -1;
            }
            else
            {
                targetY += direction.y > 0 ? 1 : -1;
            }

            if (targetX < 0 || targetY < 0 || !_converter.IsValidCoord(new GridCoord((byte)targetX, (byte)targetY)))
            {
                targetCoord = default;
                return false;
            }

            targetCoord = new GridCoord((byte)targetX, (byte)targetY);
            return true;
        }
    }
}
