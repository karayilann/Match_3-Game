using System.Collections.Generic;
using Core.Data.Goals;
using Core.Data.Level;
using Core.Data.Tiles;
using Gameplay.Board;
using Gameplay.Goals;
using Gameplay.Persistence;
using Gameplay.Scoring;
using NUnit.Framework;
using Signals;

namespace Tests
{
    public sealed class ServiceTests
    {
        private const int TEST_LEVEL_ID = 987654;

        private readonly List<LevelData> _levels = new List<LevelData>();

        [TearDown]
        public void TearDown()
        {
            LevelSignals.Reset();
            SaveLoadService.ClearSavedGame(TEST_LEVEL_ID);

            for (int i = 0; i < _levels.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(_levels[i]);
            }

            _levels.Clear();
        }

        private LevelLimitService CreateLimit(LevelLimitType type, byte moves, float seconds)
        {
            LevelData level = TestFactory.CreateLevel(type, moves, seconds);
            _levels.Add(level);
            return new LevelLimitService(level);
        }

        [Test]
        public void MoveLimitRunsOutAfterTheLastMove()
        {
            LevelLimitService limit = CreateLimit(LevelLimitType.Moves, 2, 0f);
            limit.ConsumeMove();
            Assert.IsTrue(limit.HasRemainingLimit);
            limit.ConsumeMove();
            Assert.IsFalse(limit.HasRemainingLimit);
            limit.ConsumeMove();
            Assert.AreEqual(0, limit.RemainingMoves);
        }

        [Test]
        public void LimitReachedIsAnnouncedOnlyOnce()
        {
            LevelLimitService limit = CreateLimit(LevelLimitType.Moves, 1, 0f);
            int fired = 0;
            LevelSignals.OnLevelLimitReached += () => fired++;

            limit.ConsumeMove();
            limit.NotifyIfLimitReached();
            limit.NotifyIfLimitReached();

            Assert.AreEqual(1, fired);
        }

        [Test]
        public void ExtraMovesReopenTheLimit()
        {
            LevelLimitService limit = CreateLimit(LevelLimitType.Moves, 1, 0f);
            limit.ConsumeMove();
            limit.NotifyIfLimitReached();
            limit.AddMoves(5);
            Assert.IsTrue(limit.HasRemainingLimit);
            Assert.AreEqual(5, limit.RemainingMoves);
        }

        [Test]
        public void TimeLimitCountsDownAndStopsAtZero()
        {
            LevelLimitService limit = CreateLimit(LevelLimitType.Time, 0, 3f);
            limit.TickTime(2f);
            Assert.IsTrue(limit.HasRemainingLimit);
            limit.TickTime(5f);
            Assert.IsFalse(limit.HasRemainingLimit);
            Assert.AreEqual(0f, limit.RemainingTime);
        }

        [Test]
        public void TickTimeReportsExpiryOnlyOnTheFrameItRunsOut()
        {
            LevelLimitService limit = CreateLimit(LevelLimitType.Time, 0, 3f);

            Assert.IsFalse(limit.TickTime(2f));
            Assert.IsTrue(limit.TickTime(2f));
            Assert.IsFalse(limit.TickTime(2f));
        }

        [Test]
        public void ContinueBonusAddsSecondsOnTimedLevels()
        {
            LevelLimitService limit = CreateLimit(LevelLimitType.Time, 0, 1f);
            limit.TickTime(1f);

            limit.ApplyContinueBonus(5);

            Assert.IsTrue(limit.HasRemainingLimit);
            Assert.Greater(limit.RemainingTime, 0f);
            Assert.AreEqual(0, limit.RemainingMoves);
        }

        [Test]
        public void ContinueBonusAddsMovesOnMoveLevels()
        {
            LevelLimitService limit = CreateLimit(LevelLimitType.Moves, 1, 0f);
            limit.ConsumeMove();

            limit.ApplyContinueBonus(5);

            Assert.AreEqual(5, limit.RemainingMoves);
        }

        [Test]
        public void AddMovesClampsInsteadOfWrappingPastByteRange()
        {
            LevelLimitService limit = CreateLimit(LevelLimitType.Moves, 250, 0f);

            limit.AddMoves(10);

            Assert.AreEqual(byte.MaxValue, limit.RemainingMoves);
        }

        [Test]
        public void ScoreGrowsByPointsPerTile()
        {
            ScoreService score = new ScoreService(10);
            score.AddScoreForClearedTiles(3);
            score.AddScoreForClearedTiles(0);
            score.AddScoreForClearedTiles(-4);
            Assert.AreEqual(30, score.CurrentScore);
        }

        [Test]
        public void CalculateStarsReturnsCorrectTiers()
        {
            ScoreService score = new ScoreService(10, 500, 1200, 2200);
            Assert.AreEqual(0, score.CalculateStars(0));
            Assert.AreEqual(0, score.CalculateStars(499));
            Assert.AreEqual(1, score.CalculateStars(500));
            Assert.AreEqual(1, score.CalculateStars(1199));
            Assert.AreEqual(2, score.CalculateStars(1200));
            Assert.AreEqual(2, score.CalculateStars(2199));
            Assert.AreEqual(3, score.CalculateStars(2200));
            Assert.AreEqual(3, score.CalculateStars(5000));
        }

        [Test]
        public void GoalCountsStopAtTargetAndCompleteOnce()
        {
            GoalService goals = new GoalService(new List<LevelGoalEntry> { new LevelGoalEntry(TileType.Red, 2), new LevelGoalEntry(TileType.Blue, 1) });
            int completed = 0;
            LevelSignals.OnAllGoalsCompleted += () => completed++;

            goals.OnTileCleared(TileType.Red);
            goals.OnTileCleared(TileType.Red);
            goals.OnTileCleared(TileType.Red);
            Assert.AreEqual(2, goals.GetCurrentCount(0));
            Assert.IsFalse(goals.IsAllCompleted);

            goals.OnTileCleared(TileType.Blue);
            goals.OnTileCleared(TileType.Blue);
            Assert.IsTrue(goals.IsAllCompleted);
            Assert.AreEqual(1, completed);
        }

        [Test]
        public void LevelWithoutGoalsStartsCompleted()
        {
            Assert.IsTrue(new GoalService(new List<LevelGoalEntry>()).IsAllCompleted);
        }

        [Test]
        public void SaveAndLoadRoundTripKeepsBoardScoreMovesAndGoals()
        {
            BoardModel board = TestBoard.Parse("RBG", "YXP");
            LevelLimitService limit = CreateLimit(LevelLimitType.Moves, 9, 0f);
            limit.ConsumeMove();
            ScoreService score = new ScoreService(10);
            score.AddScoreForClearedTiles(7);
            GoalService goals = new GoalService(new List<LevelGoalEntry> { new LevelGoalEntry(TileType.Red, 5) });
            goals.OnTileCleared(TileType.Red);
            goals.OnTileCleared(TileType.Red);

            SaveLoadService.SaveGame(TEST_LEVEL_ID, board, limit, score, goals);

            BoardModel loadedBoard = new BoardModel(3, 2);
            LevelLimitService loadedLimit = CreateLimit(LevelLimitType.Moves, 20, 0f);
            ScoreService loadedScore = new ScoreService(10);
            GoalService loadedGoals = new GoalService(new List<LevelGoalEntry> { new LevelGoalEntry(TileType.Red, 5) });
            byte[] scratch = new byte[1];

            Assert.IsTrue(SaveLoadService.TryLoadGame(TEST_LEVEL_ID, loadedBoard, loadedLimit, loadedScore, loadedGoals, scratch));

            Assert.AreEqual(TileType.Box, loadedBoard.GetCell(TestBoard.C(1, 0)).Type);
            Assert.IsTrue(loadedBoard.GetCell(TestBoard.C(1, 0)).IsLocked);
            Assert.AreEqual(2, loadedBoard.GetCell(TestBoard.C(1, 0)).Durability);
            Assert.AreEqual(8, loadedLimit.RemainingMoves);
            Assert.AreEqual(70, loadedScore.CurrentScore);
            Assert.AreEqual(2, loadedGoals.GetCurrentCount(0));
        }

        [Test]
        public void LoadRejectsASaveFromAnotherBoardSize()
        {
            BoardModel board = TestBoard.Parse("RBG", "YRP");
            LevelLimitService limit = CreateLimit(LevelLimitType.Moves, 9, 0f);
            SaveLoadService.SaveGame(TEST_LEVEL_ID, board, limit, new ScoreService(10), new GoalService(new List<LevelGoalEntry>()));

            BoardModel other = new BoardModel(4, 4);
            Assert.IsFalse(SaveLoadService.TryLoadGame(TEST_LEVEL_ID, other, limit, new ScoreService(10), new GoalService(new List<LevelGoalEntry>()), new byte[0]));
        }

        [Test]
        public void StarAwardedSignalReceivesCorrectStarIndex()
        {
            int receivedStar = -1;
            LevelSignals.OnStarAwarded += idx => receivedStar = idx;

            LevelSignals.RaiseStarAwarded(2);

            Assert.AreEqual(2, receivedStar);
            LevelSignals.Reset();
        }
    }
}
