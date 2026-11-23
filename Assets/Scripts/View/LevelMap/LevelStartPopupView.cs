using System.Collections.Generic;
using Core.Data.Goals;
using Core.Data.Level;
using Core.Data.Tiles;
using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using View.Popups;

namespace View.LevelMap
{
    public sealed class LevelStartPopupView : PopupView
    {

        private const string CREATE_GOAL_LABEL = "Make";
        private const string NO_BOOSTER_SELECTED_TEXT = "No booster selected";

        /// <summary>
        /// Labels name the action, not the color: gem sprites are themed and do not match
        /// the TileType color names, so the icon carries the identity.
        /// </summary>
        private static readonly string[] GOAL_LABELS =
        {
            "", "Collect", "Collect", "Collect", "Collect", "Collect", "Collect",
            "Break", "Break", "Melt", "Blast", "Blast", "Blast", "Blast", "Blast", "Drop"
        };

        [Header("Text & Titles")]
        [SerializeField] private TextMeshProUGUI _levelTitleText;
        [SerializeField] private TextMeshProUGUI _limitText;
        [SerializeField] private TextMeshProUGUI _boosterSummaryText;

        [Header("Goals")]
        [SerializeField] private TileSpriteDatabase _spriteDatabase;
        [SerializeField] private LevelGoalCardView[] _goalCards;

        [Header("Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _closeButton;

        [Header("Pre-Game Boosters")]
        [SerializeField] private PreBoosterCardView _rocketBombCard;
        [SerializeField] private PreBoosterCardView _colorBombCard;
        [SerializeField] private PreBoosterCardView _extraMovesCard;

        private int _selectedLevelIndex = 1;
        private PreGameBoosterFlags _activeBoosters = PreGameBoosterFlags.None;

        private int _rocketBombCount;
        private int _colorBombCount;
        private int _extraMovesCount;

        protected override void Awake()
        {
            base.Awake();

            if (_playButton != null)
            {
                _playButton.onClick.AddListener(HandlePlayClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }

            _rocketBombCard.Button.onClick.AddListener(ToggleRocketBomb);
            _colorBombCard.Button.onClick.AddListener(ToggleColorBomb);
            _extraMovesCard.Button.onClick.AddListener(ToggleExtraMoves);

            UpdateBoosterVisuals();
        }

        private void OnEnable()
        {
            EconomySignals.OnPreBoosterCountChanged += HandlePreBoosterCountChanged;
        }

        private void OnDisable()
        {
            EconomySignals.OnPreBoosterCountChanged -= HandlePreBoosterCountChanged;
        }

        private void HandlePreBoosterCountChanged(PreGameBoosterFlags flag, int count)
        {
            switch (flag)
            {
                case PreGameBoosterFlags.RocketBomb:
                    _rocketBombCount = count;
                    break;
                case PreGameBoosterFlags.ColorBomb:
                    _colorBombCount = count;
                    break;
                case PreGameBoosterFlags.ExtraMoves:
                    _extraMovesCount = count;
                    break;
                default:
                    return;
            }

            if (IsOpen)
            {
                UpdateBoosterVisuals();
            }
        }

        public void Open(int levelIndex, LevelData levelData, List<LevelGoalEntry> goals)
        {
            _selectedLevelIndex = levelIndex;
            _activeBoosters = PreGameBoosterFlags.None;

            if (_levelTitleText != null)
            {
                _levelTitleText.text = "LEVEL " + levelIndex;
            }

            BindGoals(goals);
            BindLimit(levelData);
            UpdateBoosterVisuals();
            Open();
        }

        private void BindGoals(List<LevelGoalEntry> goals)
        {
            int goalCount = goals != null ? goals.Count : 0;
            for (int i = 0; i < _goalCards.Length; i++)
            {
                LevelGoalCardView card = _goalCards[i];
                if (i >= goalCount)
                {
                    card.SetVisible(false);
                    continue;
                }

                LevelGoalEntry goal = goals[i];
                card.SetVisible(true);
                card.Bind(_spriteDatabase.GetSprite(goal.TargetType), goal.TargetCount, GetGoalLabel(goal));
            }
        }

        private void BindLimit(LevelData levelData)
        {
            if (_limitText == null)
            {
                return;
            }

            if (levelData == null)
            {
                _limitText.text = string.Empty;
                return;
            }

            if (levelData.LimitType == LevelLimitType.Time)
            {
                _limitText.SetText("{0:0} SECONDS", levelData.TimeLimitSeconds);
                return;
            }

            _limitText.SetText("{0:0} MOVES", levelData.MoveLimit);
        }

        private static string GetGoalLabel(LevelGoalEntry goal)
        {
            if (goal.Kind == GoalKind.Create)
            {
                return CREATE_GOAL_LABEL;
            }

            byte index = (byte)goal.TargetType;
            return index < GOAL_LABELS.Length ? GOAL_LABELS[index] : string.Empty;
        }

        private void ToggleRocketBomb()
        {
            ToggleBooster(PreGameBoosterFlags.RocketBomb, _rocketBombCount, _rocketBombCard);
        }

        private void ToggleColorBomb()
        {
            ToggleBooster(PreGameBoosterFlags.ColorBomb, _colorBombCount, _colorBombCard);
        }

        private void ToggleExtraMoves()
        {
            ToggleBooster(PreGameBoosterFlags.ExtraMoves, _extraMovesCount, _extraMovesCard);
        }

        private void ToggleBooster(PreGameBoosterFlags flag, int count, PreBoosterCardView card)
        {
            if ((_activeBoosters & flag) == 0 && count <= 0)
            {
                return;
            }

            _activeBoosters ^= flag;
            UpdateBoosterVisuals();
            card.Punch();
        }

        private void UpdateBoosterVisuals()
        {
            _rocketBombCard.Bind(_rocketBombCount, (_activeBoosters & PreGameBoosterFlags.RocketBomb) != 0);
            _colorBombCard.Bind(_colorBombCount, (_activeBoosters & PreGameBoosterFlags.ColorBomb) != 0);
            _extraMovesCard.Bind(_extraMovesCount, (_activeBoosters & PreGameBoosterFlags.ExtraMoves) != 0);

            if (_boosterSummaryText == null)
            {
                return;
            }

            int selectedCount = CountSelectedBoosters();
            if (selectedCount == 0)
            {
                _boosterSummaryText.text = NO_BOOSTER_SELECTED_TEXT;
                return;
            }

            _boosterSummaryText.SetText("{0:0} boosters selected", selectedCount);
        }

        private int CountSelectedBoosters()
        {
            int count = 0;
            if ((_activeBoosters & PreGameBoosterFlags.RocketBomb) != 0)
            {
                count++;
            }

            if ((_activeBoosters & PreGameBoosterFlags.ColorBomb) != 0)
            {
                count++;
            }

            if ((_activeBoosters & PreGameBoosterFlags.ExtraMoves) != 0)
            {
                count++;
            }

            return count;
        }

        private void HandlePlayClicked()
        {
            Close();
            UiSignals.RaiseStartLevelWithBoosters(_selectedLevelIndex, _activeBoosters);
        }
    }
}
