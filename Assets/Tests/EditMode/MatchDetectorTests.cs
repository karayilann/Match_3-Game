using System.Collections.Generic;
using Core.Data.Tiles;
using Gameplay.Board;
using Gameplay.Matching;
using NUnit.Framework;

namespace Tests
{
    public sealed class MatchDetectorTests
    {
        private readonly List<GridCoord> _matches = new List<GridCoord>();
        private readonly List<SpecialTileCreation> _creations = new List<SpecialTileCreation>();

        private bool Detect(BoardModel board, GridCoord action = default)
        {
            MatchDetector detector = new MatchDetector(board.Width, board.Height);
            return detector.TryFindMatches(board, _matches, _creations, action);
        }

        private TileType CreatedType()
        {
            Assert.AreEqual(1, _creations.Count, "expected exactly one special tile creation");
            return _creations[0].CreatedType;
        }

        [Test]
        public void NoMatchOnCheckerBoard()
        {
            BoardModel board = TestBoard.Parse("RBRB", "BRBR", "RBRB", "BRBR");
            Assert.IsFalse(Detect(board));
            Assert.AreEqual(0, _creations.Count);
        }

        [Test]
        public void HorizontalThreeIsMatchedWithoutSpecial()
        {
            BoardModel board = TestBoard.Parse("RBGB", "BRBG", "GGGB", "BRBG");
            Assert.IsTrue(Detect(board));
            Assert.AreEqual(3, _matches.Count);
            Assert.AreEqual(0, _creations.Count);
        }

        [Test]
        public void VerticalThreeIsMatched()
        {
            BoardModel board = TestBoard.Parse("RBGB", "RYBG", "RGYB", "BRBG");
            Assert.IsTrue(Detect(board));
            Assert.AreEqual(3, _matches.Count);
        }

        [Test]
        public void FourInARowCreatesRocket()
        {
            BoardModel board = TestBoard.Parse("RBGB", "BYBG", "GGGG", "BRBY");
            Assert.IsTrue(Detect(board));
            Assert.AreEqual(4, _matches.Count);
            Assert.AreEqual(TileType.VerticalRocket, CreatedType());
        }

        [Test]
        public void FourInAColumnCreatesHorizontalRocket()
        {
            BoardModel board = TestBoard.Parse("RBGY", "RYBG", "RGYB", "RBPY");
            Assert.IsTrue(Detect(board));
            Assert.AreEqual(4, _matches.Count);
            Assert.AreEqual(TileType.HorizontalRocket, CreatedType());
        }

        [Test]
        public void FiveInARowCreatesColorBomb()
        {
            BoardModel board = TestBoard.Parse("RBGBY", "BYBGB", "GGGGG", "BRBYR");
            Assert.IsTrue(Detect(board));
            Assert.AreEqual(5, _matches.Count);
            Assert.AreEqual(TileType.ColorBomb, CreatedType());
        }

        [Test]
        public void LShapeCreatesBomb()
        {
            BoardModel board = TestBoard.Parse("RBGBY", "RYBGB", "RRRYB", "BGBYR");
            Assert.IsTrue(Detect(board));
            Assert.AreEqual(5, _matches.Count);
            Assert.AreEqual(TileType.Bomb, CreatedType());
        }

        [Test]
        public void LShapeWithLongArmCreatesOnlyOneBomb()
        {
            BoardModel board = TestBoard.Parse("BYBGB", "RGYBY", "RBGYB", "RRRRR");
            Assert.IsTrue(Detect(board));
            Assert.AreEqual(1, _creations.Count);
            Assert.AreEqual(TileType.Bomb, _creations[0].CreatedType);
        }

        [Test]
        public void TShapeCreatesBomb()
        {
            BoardModel board = TestBoard.Parse("BYBGB", "GRYBG", "YRBGY", "RRRYB");
            Assert.IsTrue(Detect(board));
            Assert.AreEqual(TileType.Bomb, CreatedType());
        }

        [Test]
        public void TwoByTwoSquareCreatesPropeller()
        {
            BoardModel board = TestBoard.Parse("RBGB", "BYYG", "GYYB", "BRBG");
            Assert.IsTrue(Detect(board));
            Assert.AreEqual(4, _matches.Count);
            Assert.AreEqual(TileType.Propeller, CreatedType());
        }

        [Test]
        public void LockedCellsAreNeverMatched()
        {
            BoardModel board = TestBoard.Parse("RBGB", "BRBG", "XXXB", "BRBG");
            Assert.IsFalse(Detect(board));
        }

        [Test]
        public void NonPlayableCellsBreakRuns()
        {
            BoardModel board = TestBoard.Parse("RBGB", "BRBG", "GG#G", "BRBY");
            Assert.IsFalse(Detect(board));
        }

        [Test]
        public void SpecialTilesDoNotJoinRuns()
        {
            BoardModel board = TestBoard.Parse("RBGB", "BRBG", "GGGB", "BRBG");
            board.GetCell(TestBoard.C(1, 1)).Type = TileType.Bomb;
            Assert.IsFalse(Detect(board));
        }
    }
}
