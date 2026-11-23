using System.Collections.Generic;
using Core.Data.Tiles;
using Gameplay.Board;
using Gameplay.Matching;
using NUnit.Framework;

namespace Tests
{
    public sealed class SpecialTileServiceTests
    {
        private readonly List<GridCoord> _affected = new List<GridCoord>();

        [SetUp]
        public void SetUp()
        {
            _affected.Clear();
        }

        private static BoardModel Open(int size)
        {
            string[] rows = new string[size];
            for (int i = 0; i < size; i++)
            {
                rows[i] = new string('R', size);
            }

            return TestBoard.Parse(rows);
        }

        private int DistinctCount()
        {
            return new HashSet<GridCoord>(_affected).Count;
        }

        [Test]
        public void HorizontalRocketClearsItsRow()
        {
            BoardModel board = Open(5);
            SpecialTileService.ExecuteSingle(board, TestBoard.C(2, 3), TileType.HorizontalRocket, _affected);
            Assert.AreEqual(5, DistinctCount());
            foreach (GridCoord c in _affected)
            {
                Assert.AreEqual(3, c.Y);
            }
        }

        [Test]
        public void VerticalRocketClearsItsColumn()
        {
            BoardModel board = Open(5);
            SpecialTileService.ExecuteSingle(board, TestBoard.C(2, 3), TileType.VerticalRocket, _affected);
            Assert.AreEqual(5, DistinctCount());
            foreach (GridCoord c in _affected)
            {
                Assert.AreEqual(2, c.X);
            }
        }

        [Test]
        public void BombClearsThreeByThree()
        {
            BoardModel board = Open(5);
            SpecialTileService.ExecuteSingle(board, TestBoard.C(2, 2), TileType.Bomb, _affected);
            Assert.AreEqual(9, DistinctCount());
        }

        [Test]
        public void BombInCornerIsClippedToTheBoard()
        {
            BoardModel board = Open(5);
            SpecialTileService.ExecuteSingle(board, TestBoard.C(0, 0), TileType.Bomb, _affected);
            Assert.AreEqual(4, DistinctCount());
        }

        [Test]
        public void RocketsSkipNonPlayableCells()
        {
            BoardModel board = TestBoard.Parse("RR#RR", "RRRRR");
            SpecialTileService.ExecuteSingle(board, TestBoard.C(0, 1), TileType.HorizontalRocket, _affected);
            Assert.AreEqual(4, DistinctCount());
        }

        [Test]
        public void TwoRocketsMakeACross()
        {
            BoardModel board = Open(5);
            bool ok = SpecialTileService.TryExecuteCombo(board, TestBoard.C(1, 2), TestBoard.C(2, 2), TileType.HorizontalRocket, TileType.VerticalRocket, _affected);
            Assert.IsTrue(ok);
            Assert.AreEqual(9, DistinctCount());
        }

        [Test]
        public void TwoBombsClearFiveByFive()
        {
            BoardModel board = Open(7);
            bool ok = SpecialTileService.TryExecuteCombo(board, TestBoard.C(2, 3), TestBoard.C(3, 3), TileType.Bomb, TileType.Bomb, _affected);
            Assert.IsTrue(ok);
            Assert.AreEqual(25, DistinctCount());
        }

        [Test]
        public void RocketPlusBombClearsThreeRowsAndThreeColumns()
        {
            BoardModel board = Open(7);
            bool ok = SpecialTileService.TryExecuteCombo(board, TestBoard.C(2, 3), TestBoard.C(3, 3), TileType.HorizontalRocket, TileType.Bomb, _affected);
            Assert.IsTrue(ok);
            Assert.AreEqual(3 * 7 + 3 * 7 - 9, DistinctCount());
        }

        [Test]
        public void TwoColorBombsClearTheWholeBoard()
        {
            BoardModel board = Open(5);
            bool ok = SpecialTileService.TryExecuteCombo(board, TestBoard.C(1, 1), TestBoard.C(2, 1), TileType.ColorBomb, TileType.ColorBomb, _affected);
            Assert.IsTrue(ok);
            Assert.AreEqual(25, DistinctCount());
        }

        [Test]
        public void ColorBombWithGemClearsEveryGemOfThatColor()
        {
            BoardModel board = TestBoard.Parse("RBR", "BRB", "RBR");
            bool ok = SpecialTileService.TryExecuteCombo(board, TestBoard.C(0, 0), TestBoard.C(1, 0), TileType.ColorBomb, TileType.Blue, _affected);
            Assert.IsTrue(ok);
            Assert.AreEqual(5, DistinctCount());
        }

        [Test]
        public void ColorBombWithRocketTurnsEveryTileOfMostCommonColorIntoRockets()
        {
            BoardModel board = TestBoard.Parse("RRRR", "RRRR", "RRRR", "RRRB");
            bool ok = SpecialTileService.TryExecuteCombo(board, TestBoard.C(0, 0), TestBoard.C(1, 0), TileType.ColorBomb, TileType.HorizontalRocket, _affected);
            Assert.IsTrue(ok);
            Assert.AreEqual(15, DistinctCount());
            Assert.AreEqual(TileType.Blue, board.GetCell(TestBoard.C(3, 0)).Type);

            int rockets = 0;
            for (int i = 0; i < _affected.Count; i++)
            {
                TileType type = board.GetCell(_affected[i]).Type;
                Assert.IsTrue(type == TileType.HorizontalRocket || type == TileType.VerticalRocket);
                rockets++;
            }
            Assert.Greater(rockets, 0);
        }

        [Test]
        public void ColorBombWithBombTurnsEveryTileOfMostCommonColorIntoBombs()
        {
            BoardModel board = TestBoard.Parse("RRBBB", "BBBBB", "BBBBB", "BBBBB", "BBBBB");
            bool ok = SpecialTileService.TryExecuteCombo(board, TestBoard.C(4, 4), TestBoard.C(3, 4), TileType.ColorBomb, TileType.Bomb, _affected);
            Assert.IsTrue(ok);
            Assert.AreEqual(23, DistinctCount());

            for (int i = 0; i < _affected.Count; i++)
            {
                Assert.AreEqual(TileType.Bomb, board.GetCell(_affected[i]).Type);
            }
        }

        [Test]
        public void ColorBombSingleClearsNearestPlayableColor()
        {
            BoardModel board = TestBoard.Parse("B..", "...", "GGG");
            board.GetCell(TestBoard.C(1, 2)).Type = TileType.ColorBomb;
            SpecialTileService.ExecuteSingle(board, TestBoard.C(1, 2), TileType.ColorBomb, _affected);
            Assert.AreEqual(2, DistinctCount());
            Assert.IsTrue(_affected.Contains(TestBoard.C(1, 2)));
            Assert.IsTrue(_affected.Contains(TestBoard.C(0, 2)));
        }

        [Test]
        public void ComboIsRejectedWhenNeitherTileIsSpecial()
        {
            BoardModel board = Open(3);
            Assert.IsFalse(SpecialTileService.TryExecuteCombo(board, TestBoard.C(0, 0), TestBoard.C(1, 0), TileType.Red, TileType.Blue, _affected));
        }
    }
}
