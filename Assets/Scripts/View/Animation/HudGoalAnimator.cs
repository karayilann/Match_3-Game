using Core.Data.Tiles;
using PrimeTween;
using Signals;
using UnityEngine;
using UnityEngine.UI;
using View.HUD;

namespace View.Animation
{
    public sealed class HudGoalAnimator : MonoBehaviour
    {
        private static readonly Vector3 OPEN_SCALE = Vector3.one;
        private static readonly Vector3 CLOSED_SCALE = new Vector3(1f, 0f, 1f);

        [SerializeField] private RectTransform _panelContainer;
        [SerializeField] private GoalItemView[] _goalItems;
        [SerializeField] private Vector3 _punchScaleStrength = new Vector3(0.22f, 0.22f, 0f);
        [SerializeField] private Vector3 _punchRotationStrength = new Vector3(0f, 0f, 5f);
        [SerializeField] private Color _barHighlightColor = new Color(1f, 0.92f, 0.35f, 1f);
        [SerializeField] private float _itemAnimDuration = 0.25f;
        [SerializeField] private float _panelToggleDuration = 0.22f;
        [SerializeField] private int _frequency = 6;

        private byte[] _lastCounts;
        private TileType[] _types;
        private bool[] _initializedFlags;
        private Color[] _baseBarColors;

        private void Awake()
        {
            int count = _goalItems != null ? _goalItems.Length : 0;
            _lastCounts = new byte[count];
            _types = new TileType[count];
            _initializedFlags = new bool[count];
            _baseBarColors = new Color[count];

            for (int i = 0; i < count; i++)
            {
                Image fill = _goalItems[i].ProgressBarFill;
                if (fill != null)
                {
                    _baseBarColors[i] = fill.color;
                }
            }
        }

        private void OnEnable()
        {
            LevelSignals.OnGoalsInitialized += HandleGoalsInitialized;
            LevelSignals.OnGoalUpdated += HandleGoalUpdated;
            UiSignals.OnGoalPanelToggled += HandleGoalPanelToggled;
            GoalFlightTracker.OnArrived += HandleFlightArrived;
        }

        private void OnDisable()
        {
            LevelSignals.OnGoalsInitialized -= HandleGoalsInitialized;
            LevelSignals.OnGoalUpdated -= HandleGoalUpdated;
            UiSignals.OnGoalPanelToggled -= HandleGoalPanelToggled;
            GoalFlightTracker.OnArrived -= HandleFlightArrived;

            if (_panelContainer != null)
            {
                Tween.CompleteAll(_panelContainer);
            }

            if (_goalItems == null)
            {
                return;
            }

            for (int i = 0; i < _goalItems.Length; i++)
            {
                if (_goalItems[i] == null)
                {
                    continue;
                }

                Tween.CompleteAll(_goalItems[i].RootTransform);
                Tween.CompleteAll(_goalItems[i].ProgressBarFill);
            }
        }

        private void HandleGoalsInitialized(byte goalCount)
        {
            for (int i = 0; i < _initializedFlags.Length; i++)
            {
                _initializedFlags[i] = false;
                _lastCounts[i] = 0;
            }
        }

        /// <summary>
        /// Animates the goal slot with scale punch, rotation tilt, and progress bar highlight flash when a target tile is cleared.
        /// </summary>
        private void HandleGoalUpdated(byte goalIndex, TileType targetType, byte currentCount, byte targetCount)
        {
            if (goalIndex >= _goalItems.Length)
            {
                return;
            }

            if (!_initializedFlags[goalIndex])
            {
                _initializedFlags[goalIndex] = true;
                _types[goalIndex] = targetType;
                _lastCounts[goalIndex] = currentCount;
                return;
            }

            if (currentCount == _lastCounts[goalIndex])
            {
                return;
            }

            _lastCounts[goalIndex] = currentCount;
            _types[goalIndex] = targetType;

            if (GoalFlightTracker.GetPending(targetType) > 0)
            {
                return;
            }

            PlayItemPunch(goalIndex);
        }

        private void HandleFlightArrived(TileType type)
        {
            for (int i = 0; i < _types.Length; i++)
            {
                if (_initializedFlags[i] && _types[i] == type)
                {
                    PlayItemPunch(i);
                }
            }
        }

        private void PlayItemPunch(int goalIndex)
        {
            GoalItemView item = _goalItems[goalIndex];
            RectTransform root = item.RootTransform;
            Image barFill = item.ProgressBarFill;

            if (!root.gameObject.activeInHierarchy)
            {
                return;
            }

            Tween.CompleteAll(root);
            Tween.PunchScale(root, _punchScaleStrength, _itemAnimDuration, _frequency);
            Tween.PunchLocalRotation(root, _punchRotationStrength, _itemAnimDuration, _frequency);

            if (barFill != null && barFill.gameObject.activeInHierarchy)
            {
                Tween.CompleteAll(barFill);
                barFill.color = _barHighlightColor;
                Tween.Color(barFill, _baseBarColors[goalIndex], _itemAnimDuration);
            }
        }

        private void HandleGoalPanelToggled(bool isOpen)
        {
            if (_panelContainer == null)
            {
                return;
            }

            Tween.CompleteAll(_panelContainer);

            if (isOpen)
            {
                _panelContainer.gameObject.SetActive(true);
                _panelContainer.localScale = CLOSED_SCALE;
                Tween.Scale(_panelContainer, OPEN_SCALE, _panelToggleDuration, Ease.OutBack);
            }
            else
            {
                Tween.Scale(_panelContainer, CLOSED_SCALE, _panelToggleDuration, Ease.InBack)
                    .OnComplete(_panelContainer.gameObject, static target => target.SetActive(false));
            }
        }
    }
}

