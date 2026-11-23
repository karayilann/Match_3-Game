using System.Collections.Generic;
using Core.Data.Goals;
using Core.Data.Tiles;
using Gameplay.Goals;
using Gameplay.Ingredients;
using NUnit.Framework;

namespace Tests
{
    public sealed class GoalKindTests
    {
        private static GoalService Make(params LevelGoalEntry[] entries)
        {
            return new GoalService(new List<LevelGoalEntry>(entries));
        }

        [Test]
        public void CreateGoalIgnoresClearedTiles()
        {
            GoalService goals = Make(new LevelGoalEntry(TileType.Bomb, 2, GoalKind.Create));
            goals.OnTileCleared(TileType.Bomb, TestBoard.C(0, 0));
            Assert.AreEqual(0, goals.GetCurrentCount(0));
            goals.OnTileCreated(TileType.Bomb, TestBoard.C(0, 0));
            Assert.AreEqual(1, goals.GetCurrentCount(0));
        }

        [Test]
        public void ClearGoalIgnoresCreatedTiles()
        {
            GoalService goals = Make(new LevelGoalEntry(TileType.Red, 2));
            goals.OnTileCreated(TileType.Red, TestBoard.C(0, 0));
            Assert.AreEqual(0, goals.GetCurrentCount(0));
        }

        [Test]
        public void CreateGoalCompletesLevel()
        {
            GoalService goals = Make(new LevelGoalEntry(TileType.Propeller, 1, GoalKind.Create));
            goals.OnTileCreated(TileType.Propeller, TestBoard.C(1, 1));
            Assert.IsTrue(goals.IsAllCompleted);
        }

        [Test]
        public void MissingIngredientsSubtractsThoseAlreadyOnBoard()
        {
            GoalService goals = Make(new LevelGoalEntry(TileType.Ingredient, 3));
            Gameplay.Board.BoardModel board = TestBoard.Parse("RRB", "BRB", "RBR");
            board.GetCell(TestBoard.C(0, 2)).Type = TileType.Ingredient;
            Assert.AreEqual(2, IngredientService.GetMissing(board, goals));
        }

        [Test]
        public void IngredientIsCollectedOnlyAtTheBottomOfItsColumn()
        {
            Gameplay.Board.BoardModel board = TestBoard.Parse("RRB", "BRB", "RBR");
            board.GetCell(TestBoard.C(0, 0)).Type = TileType.Ingredient;
            board.GetCell(TestBoard.C(1, 1)).Type = TileType.Ingredient;
            List<GridCoord> exits = new List<GridCoord>();
            IngredientService.CollectExitCoords(board, exits);
            Assert.AreEqual(1, exits.Count);
            Assert.AreEqual(TestBoard.C(0, 0), exits[0]);
        }
    }
}
