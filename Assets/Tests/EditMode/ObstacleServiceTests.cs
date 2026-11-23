using System.Collections.Generic;
using Core.Data.Obstacles;
using Core.Data.Tiles;
using Gameplay.Board;
using Gameplay.Obstacles;
using NUnit.Framework;

namespace Tests
{
    public sealed class ObstacleServiceTests
    {
        private ObstacleDatabase _database;

        [TearDown]
        public void TearDown()
        {
            if (_database != null)
            {
                UnityEngine.Object.DestroyImmediate(_database);
            }
        }

        private ObstacleService Create(BoardModel board, bool canTakeDamage = true)
        {
            _database = TestFactory.CreateObstacleDatabase(TestFactory.BoxDefinition(canTakeDamage));
            return new ObstacleService(board.Width, board.Height, _database);
        }

        [Test]
        public void ObstacleTakesAtMostOneDamagePerResolve()
        {
            BoardModel board = TestBoard.Parse("RXR");
            ObstacleService service = Create(board);
            List<GridCoord> matched = new List<GridCoord> { TestBoard.C(0, 0), TestBoard.C(2, 0) };

            service.ResolveDamage(board, matched);

            Assert.AreEqual(1, board.GetCell(TestBoard.C(1, 0)).Durability);
            Assert.IsTrue(board.GetCell(TestBoard.C(1, 0)).IsLocked);
        }

        [Test]
        public void DestroyedObstacleLosesItsLockAndIsReported()
        {
            BoardModel board = TestBoard.Parse("RX");
            board.GetCell(TestBoard.C(1, 0)).Durability = 1;
            ObstacleService service = Create(board);

            List<GridCoord> destroyed = service.ResolveDamage(board, new List<GridCoord> { TestBoard.C(0, 0) });

            Assert.AreEqual(1, destroyed.Count);
            Assert.IsFalse(board.GetCell(TestBoard.C(1, 0)).IsLocked);
        }

        [Test]
        public void ObstacleThatIgnoresAdjacentDamageIsUntouched()
        {
            BoardModel board = TestBoard.Parse("RX");
            ObstacleService service = Create(board, false);

            service.ResolveDamage(board, new List<GridCoord> { TestBoard.C(0, 0) });

            Assert.AreEqual(2, board.GetCell(TestBoard.C(1, 0)).Durability);
        }

        [Test]
        public void DamagedButSurvivingObstacleIsListedForFeedback()
        {
            BoardModel board = TestBoard.Parse("RX");
            ObstacleService service = Create(board);

            service.ResolveDamage(board, new List<GridCoord> { TestBoard.C(0, 0) });

            Assert.AreEqual(1, service.DamagedBuffer.Count);
        }

        [Test]
        public void DamageCellBreaksObstacleOnLastHit()
        {
            BoardModel board = TestBoard.Parse("X");
            ObstacleService service = Create(board);

            Assert.IsFalse(service.DamageCell(board, TestBoard.C(0, 0)));
            Assert.IsTrue(service.DamageCell(board, TestBoard.C(0, 0)));
            Assert.IsFalse(board.GetCell(TestBoard.C(0, 0)).IsLocked);
        }
    }
}
