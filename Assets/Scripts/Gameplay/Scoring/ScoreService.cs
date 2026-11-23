using System.Runtime.CompilerServices;
using Signals;

namespace Gameplay.Scoring
{
    public sealed class ScoreService
    {
        private readonly int _pointsPerTile;
        private readonly int _targetScore1Star;
        private readonly int _targetScore2Star;
        private readonly int _targetScore3Star;
        private int _currentScore;

        public int CurrentScore
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentScore;
        }

        public int PointsPerTile => _pointsPerTile;
        public int TargetScore1Star => _targetScore1Star;
        public int TargetScore2Star => _targetScore2Star;
        public int TargetScore3Star => _targetScore3Star;

        public ScoreService(int pointsPerTile, int targetScore1Star = 500, int targetScore2Star = 1200, int targetScore3Star = 2200)
        {
            _pointsPerTile = pointsPerTile;
            _targetScore1Star = targetScore1Star;
            _targetScore2Star = targetScore2Star;
            _targetScore3Star = targetScore3Star;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CalculateStars(int score)
        {
            if (score >= _targetScore3Star)
            {
                return 3;
            }
            if (score >= _targetScore2Star)
            {
                return 2;
            }
            if (score >= _targetScore1Star)
            {
                return 1;
            }
            return 0;
        }

        public int CurrentStars => CalculateStars(_currentScore);

        public void BroadcastInitialState()
        {
            LevelSignals.RaiseScoreChanged(_currentScore);
        }

        public void RestoreScore(int score)
        {
            _currentScore = score;
            LevelSignals.RaiseScoreChanged(_currentScore);
        }

        public void AddScoreForClearedTiles(int tileCount)
        {
            if (tileCount <= 0)
            {
                return;
            }

            _currentScore += tileCount * _pointsPerTile;
            LevelSignals.RaiseScoreChanged(_currentScore);
        }
    }
}
