using System;
using Core.Data.Level;
using Gameplay.Board;
using Gameplay.Goals;
using Gameplay.Scoring;
using Signals;

namespace Gameplay.Persistence
{
    /// <summary>
    /// Owns when the current level is saved or discarded. The services it watches are rebuilt for every level,
    /// so it is bound once per level while the signal subscriptions follow the owner's enable state.
    /// </summary>
    public sealed class GamePersistenceController
    {
        private readonly Action _onSave;
        private readonly Action _onClear;
        private LevelData _levelData;
        private BoardModel _boardModel;
        private LevelLimitService _limitService;
        private ScoreService _scoreService;
        private GoalService _goalService;

        public GamePersistenceController()
        {
            _onSave = Save;
            _onClear = Clear;
        }

        public void Bind(LevelData levelData, BoardModel boardModel, LevelLimitService limitService, ScoreService scoreService, GoalService goalService)
        {
            _levelData = levelData;
            _boardModel = boardModel;
            _limitService = limitService;
            _scoreService = scoreService;
            _goalService = goalService;
        }

        public void Subscribe()
        {
            BoardSignals.OnBoardSettled += _onSave;
            LevelSignals.OnAllGoalsCompleted += _onClear;
            LevelSignals.OnLevelLimitReached += _onClear;
        }

        public void Unsubscribe()
        {
            BoardSignals.OnBoardSettled -= _onSave;
            LevelSignals.OnAllGoalsCompleted -= _onClear;
            LevelSignals.OnLevelLimitReached -= _onClear;
        }

        public bool TryLoad(byte[] goalScratch)
        {
            return SaveLoadService.TryLoadGame(_levelData.LevelId, _boardModel, _limitService, _scoreService, _goalService, goalScratch);
        }

        public void Save()
        {
            if (_levelData == null || _boardModel == null || _goalService == null || _limitService == null)
            {
                return;
            }

            if (_goalService.IsAllCompleted || !_limitService.HasRemainingLimit)
            {
                return;
            }

            SaveLoadService.SaveGame(_levelData.LevelId, _boardModel, _limitService, _scoreService, _goalService);
        }

        public void Clear()
        {
            if (_levelData != null)
            {
                SaveLoadService.ClearSavedGame(_levelData.LevelId);
            }
        }
    }
}
