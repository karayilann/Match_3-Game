using Core.Data.Tiles;
using Gameplay.Board;
using Gameplay.Matching;
using NUnit.Framework;

namespace Tests
{
    public sealed class PossibleMoveDetectorTests
    {
        private static PossibleMoveDetector Create(BoardModel board)
        {
            return new PossibleMoveDetector(board.Width, board.Height, new MatchDetector(board.Width, board.Height));
        }

        [Test]
        public void FindsMoveThatCompletesARun()
        {
            BoardModel board = TestBoard.Parse("RBRB", "BRBR", "RRBR", "YGYG");
            Assert.IsTrue(Create(board).TryFindPossibleMove(board, out GridCoord from, out GridCoord to));
            Assert.IsTrue(board.IsValidCoord(from) && board.IsValidCoord(to));
        }

        [Test]
        public void ReportsNoMoveOnDeadBoard()
        {
            BoardModel board = TestBoard.Parse("RBGY", "GYRB", "RBGY", "GYRB");
            Assert.IsFalse(Create(board).HasPossibleMove(board));
        }

        [Test]
        public void LockedCellsCannotBeSwapped()
        {
            BoardModel board = TestBoard.Parse("RBGY", "GYRB", "RBGY", "GYRB");
            board.GetCell(TestBoard.C(0, 0)).Flags |= CellFlags.Locked;
            Assert.IsFalse(Create(board).HasPossibleMove(board));
        }

        [Test]
        public void SpecialTileCountsAsPossibleMove()
        {
            BoardModel board = TestBoard.Parse("RBGY", "GYRB", "RBGY", "GYRB");
            board.GetCell(TestBoard.C(1, 1)).Type = TileType.Bomb;
            PossibleMoveDetector detector = Create(board);
            Assert.IsTrue(detector.HasPossibleMove(board));
            Assert.IsTrue(detector.TryFindPossibleMove(board, out _, out _));
        }

        [Test]
        public void IngredientIsNotSwappedAsAMove()
        {
            BoardModel board = TestBoard.Parse("RBGY", "GYRB", "RBGY", "GYRB");
            board.GetCell(TestBoard.C(1, 1)).Type = TileType.Ingredient;
            Assert.IsFalse(Create(board).HasPossibleMove(board));
        }

        [Test]
        public void SearchLeavesTheBoardUnchanged()
        {
            BoardModel board = TestBoard.Parse("RBRB", "BRBR", "RRBR", "YGYG");
            TileType[] before = Snapshot(board);
            Create(board).TryFindPossibleMove(board, out _, out _);
            CollectionAssert.AreEqual(before, Snapshot(board));
        }

        private static TileType[] Snapshot(BoardModel board)
        {
            TileType[] types = new TileType[board.Width * board.Height];
            for (byte y = 0; y < board.Height; y++)
            {
                for (byte x = 0; x < board.Width; x++)
                {
                    types[board.ToIndex(new GridCoord(x, y))] = board.GetCell(new GridCoord(x, y)).Type;
                }
            }

            return types;
        }
    }
}
