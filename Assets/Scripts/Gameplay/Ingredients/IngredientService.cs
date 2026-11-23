using System.Collections.Generic;
using Core.Data.Goals;
using Core.Data.Tiles;
using Gameplay.Board;
using Gameplay.Goals;
using UnityEngine;
using View;

namespace Gameplay.Ingredients
{
    public static class IngredientService
    {
        private const int INITIAL_PLACEMENT_LIMIT = 2;

        public static int CountOnBoard(BoardModel board)
        {
            int count = 0;

            for (byte y = 0; y < board.Height; y++)
            {
                for (byte x = 0; x < board.Width; x++)
                {
                    if (board.GetCell(new GridCoord(x, y)).Type == TileType.Ingredient)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Spawning only covers what is still missing, so a level never runs out of ingredients
        /// and never floods the board with more than the goal asks for.
        /// </summary>
        public static int GetMissing(BoardModel board, GoalService goals)
        {
            return goals.GetRemaining(TileType.Ingredient, GoalKind.Clear) - CountOnBoard(board);
        }

        /// <summary>
        /// A tile is collected once nothing playable lies below it, which also covers boards
        /// whose bottom row has holes.
        /// </summary>
        public static void CollectExitCoords(BoardModel board, List<GridCoord> outCoords)
        {
            outCoords.Clear();

            for (byte x = 0; x < board.Width; x++)
            {
                for (byte y = 0; y < board.Height; y++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    ref GridCell cell = ref board.GetCell(coord);

                    if (cell.IsPlayable)
                    {
                        if (cell.Type == TileType.Ingredient)
                        {
                            outCoords.Add(coord);
                        }

                        break;
                    }
                }
            }
        }

        public static void PlaceInitial(BoardModel board, GemView[] views, GoalService goalService, TileSpriteDatabase spriteDatabase)
        {
            int toPlace = Mathf.Min(GetMissing(board, goalService), INITIAL_PLACEMENT_LIMIT);

            for (int attempt = 0; attempt < 32 && toPlace > 0; attempt++)
            {
                byte x = (byte)Random.Range(0, board.Width);
                GridCoord coord = FindTopPlayable(board, x);

                ref GridCell cell = ref board.GetCell(coord);
                if (!cell.IsPlayable || cell.IsLocked || cell.Type == TileType.Ingredient)
                {
                    continue;
                }

                cell.Type = TileType.Ingredient;
                GemView view = views[board.ToIndex(coord)];
                if (view != null)
                {
                    view.SetSprite(spriteDatabase.GetSprite(TileType.Ingredient));
                }

                toPlace--;
            }
        }

        private static GridCoord FindTopPlayable(BoardModel board, byte x)
        {
            for (int y = board.Height - 1; y >= 0; y--)
            {
                GridCoord coord = new GridCoord(x, (byte)y);
                if (board.GetCell(coord).IsPlayable)
                {
                    return coord;
                }
            }

            return new GridCoord(x, 0);
        }
    }
}
