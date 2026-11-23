using Core.Data.Tiles;
using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View.HUD
{
    public sealed class GoalPanelView : MonoBehaviour
    {
        [SerializeField] private TileSpriteDatabase _spriteDatabase;
        [SerializeField] private GoalItemView[] _goalItems;
        [SerializeField] private GameObject _itemsContainer;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private TMP_Text _toggleIndicatorText;
        [SerializeField] private bool _isOpen = true;

        private byte[] _lastCurrent;
        private byte[] _lastTarget;
        private TileType[] _lastType;

        private void Awake()
        {
            _lastCurrent = new byte[_goalItems.Length];
            _lastTarget = new byte[_goalItems.Length];
            _lastType = new TileType[_goalItems.Length];
            UpdateToggleStateVisuals();
        }

        private void OnEnable()
        {
            LevelSignals.OnGoalsInitialized += HandleGoalsInitialized;
            LevelSignals.OnGoalUpdated += HandleGoalUpdated;
            GoalFlightTracker.OnArrived += HandleFlightArrived;

            if (_toggleButton != null)
            {
                _toggleButton.onClick.AddListener(TogglePanel);
            }
        }

        private void OnDisable()
        {
            LevelSignals.OnGoalsInitialized -= HandleGoalsInitialized;
            LevelSignals.OnGoalUpdated -= HandleGoalUpdated;
            GoalFlightTracker.OnArrived -= HandleFlightArrived;

            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveListener(TogglePanel);
            }
        }

        public void TogglePanel()
        {
            SetPanelOpen(!_isOpen);
        }

        public void SetPanelOpen(bool isOpen)
        {
            if (_isOpen == isOpen)
            {
                return;
            }

            _isOpen = isOpen;
            UpdateToggleStateVisuals();
            UiSignals.RaiseGoalPanelToggled(_isOpen);
        }

        private void UpdateToggleStateVisuals()
        {
            if (_toggleIndicatorText != null)
            {
                _toggleIndicatorText.SetText(_isOpen ? "GOALS -" : "GOALS +");
            }
        }

        private void HandleGoalsInitialized(byte goalCount)
        {
            int length = _goalItems.Length;
            for (int i = 0; i < length; i++)
            {
                _lastTarget[i] = 0;
                _goalItems[i].SetVisible(i < goalCount);
            }
        }

        private void HandleGoalUpdated(byte goalIndex, TileType targetType, byte currentCount, byte targetCount)
        {
            if (goalIndex >= _goalItems.Length)
            {
                return;
            }

            _lastCurrent[goalIndex] = currentCount;
            _lastTarget[goalIndex] = targetCount;
            _lastType[goalIndex] = targetType;
            _goalItems[goalIndex].SetGoalType(targetType);
            BindItem(goalIndex);
        }

        /// <summary>
        /// Tiles still in flight are subtracted so the counter drops when the icon lands, not when the tile is cleared.
        /// </summary>
        private void BindItem(int goalIndex)
        {
            TileType type = _lastType[goalIndex];
            int shown = Mathf.Max(0, _lastCurrent[goalIndex] - GoalFlightTracker.GetPending(type));
            _goalItems[goalIndex].Bind(_spriteDatabase.GetSprite(type), (byte)shown, _lastTarget[goalIndex]);
        }

        private void HandleFlightArrived(TileType type)
        {
            for (int i = 0; i < _goalItems.Length; i++)
            {
                if (_lastType[i] == type && _lastTarget[i] > 0)
                {
                    BindItem(i);
                }
            }
        }
    }
}
