using System.Collections.Generic;
using Core.Data.Tiles;
using PrimeTween;
using Signals;
using UnityEngine;
using UnityEngine.UI;

namespace View.HUD
{
    /// <summary>
    /// Flies a copy of a collected goal tile along a curved path to its HUD slot. Flights are pooled
    /// and the path is evaluated by a static callback, so a big match allocates nothing.
    /// </summary>
    public sealed class GoalFlyView : MonoBehaviour
    {
        private const int FLIGHT_CAPACITY = 24;
        private const float STAGGER_SECONDS = 0.03f;

        [SerializeField] private Canvas _canvas;
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private float _flightDuration = 0.55f;
        [SerializeField] private float _iconSize = 90f;
        [SerializeField] private float _arcHeight = 160f;

        private sealed class Flight
        {
            public RectTransform Rect;
            public Image Icon;
            public Vector3 Start;
            public Vector3 Control;
            public Vector3 End;
            public TileType Type;
            public bool InUse;
        }

        private readonly List<Flight> _flights = new List<Flight>(FLIGHT_CAPACITY);
        private GoalItemView[] _goalItems;
        private RectTransform _canvasRect;
        private int _spawnedThisFrame;
        private int _lastSpawnFrame = -1;

        private void Awake()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            _canvasRect = (RectTransform)_canvas.transform;
            _goalItems = GetComponentsInChildren<GoalItemView>(true);

            for (int i = 0; i < FLIGHT_CAPACITY; i++)
            {
                _flights.Add(CreateFlight());
            }
        }

        private void OnEnable()
        {
            BoardSignals.OnGoalTileFlyRequested += HandleFlyRequested;
            LevelSignals.OnGoalsInitialized += HandleGoalsInitialized;
        }

        private void OnDisable()
        {
            BoardSignals.OnGoalTileFlyRequested -= HandleFlyRequested;
            LevelSignals.OnGoalsInitialized -= HandleGoalsInitialized;

            for (int i = 0; i < _flights.Count; i++)
            {
                Flight flight = _flights[i];
                if (flight.InUse)
                {
                    Tween.StopAll(flight);
                    flight.InUse = false;
                    flight.Rect.gameObject.SetActive(false);
                }
            }

            GoalFlightTracker.Reset();
        }

        private void HandleGoalsInitialized(byte goalCount)
        {
            GoalFlightTracker.Reset();
        }

        private Flight CreateFlight()
        {
            GameObject go = new GameObject("GoalFlight", typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(_canvasRect, false);
            rect.sizeDelta = new Vector2(_iconSize, _iconSize);

            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            go.SetActive(false);

            return new Flight { Rect = rect, Icon = image };
        }

        private GoalItemView FindItem(TileType type)
        {
            for (int i = 0; i < _goalItems.Length; i++)
            {
                GoalItemView item = _goalItems[i];
                if (item.gameObject.activeInHierarchy && item.GoalType == type)
                {
                    return item;
                }
            }

            return null;
        }

        private Flight AcquireFlight()
        {
            for (int i = 0; i < _flights.Count; i++)
            {
                if (!_flights[i].InUse)
                {
                    return _flights[i];
                }
            }

            return null;
        }

        private void HandleFlyRequested(Vector3 worldPosition, TileType type)
        {
            GoalItemView item = FindItem(type);
            Flight flight = AcquireFlight();
            Camera cam = _worldCamera != null ? _worldCamera : Camera.main;

            if (item == null || flight == null || cam == null || item.IconSprite == null)
            {
                return;
            }

            Vector3 screen = cam.WorldToScreenPoint(worldPosition);
            Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvasRect, screen, uiCamera, out Vector3 start);

            flight.Start = start;
            flight.End = item.RootTransform.position;
            flight.Control = (start + flight.End) * 0.5f + Vector3.up * _arcHeight * _canvas.scaleFactor;
            flight.Type = type;
            flight.InUse = true;
            flight.Icon.sprite = item.IconSprite;
            flight.Rect.position = start;
            flight.Rect.localScale = Vector3.one;
            flight.Rect.gameObject.SetActive(true);

            if (_lastSpawnFrame != Time.frameCount)
            {
                _lastSpawnFrame = Time.frameCount;
                _spawnedThisFrame = 0;
            }

            float delay = _spawnedThisFrame * STAGGER_SECONDS;
            _spawnedThisFrame++;

            GoalFlightTracker.Add(type);

            Tween.Custom(flight, 0f, 1f, _flightDuration, static (target, t) =>
            {
                float u = 1f - t;
                target.Rect.position = u * u * target.Start + 2f * u * t * target.Control + t * t * target.End;
                target.Rect.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(t * Mathf.PI));
            }, Ease.InSine, startDelay: delay).OnComplete(flight, static target =>
            {
                target.InUse = false;
                target.Rect.gameObject.SetActive(false);
                GoalFlightTracker.Arrive(target.Type);
            }, warnIfTargetDestroyed: false);
        }
    }
}
