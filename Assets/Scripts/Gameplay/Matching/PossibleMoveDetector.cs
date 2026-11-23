using System.Collections.Generic;
using Core.Data.Tiles;
using Gameplay.Board;

namespace Gameplay.Matching
{
    public sealed class PossibleMoveDetector : IPossibleMoveDetector
    {
        private readonly byte _width;
        private readonly byte _height;
        private readonly IMatchDetector _matchDetector;
        private readonly List<GridCoord> _scratchBuffer;

        public PossibleMoveDetector(byte width, byte height, IMatchDetector matchDetector)
        {
            _width = width;
            _height = height;
            _matchDetector = matchDetector;
            _scratchBuffer = new List<GridCoord>();
        }

        public bool HasPossibleMove(BoardModel board)
        {
            for (byte y = 0; y < _height; y++)
            {
                for (byte x = 0; x < _width; x++)
                {
                    ref GridCell cell = ref board.GetCell(new GridCoord(x, y));
                    if (cell.IsPlayable && !cell.IsLocked && TileTypeLibrary.IsSpecialTile(cell.Type))
                    {
                        return true;
                    }
                }
            }

            return TryFindPossibleMove(board, out _, out _);
        }

        public bool TryFindPossibleMove(BoardModel board, out GridCoord from, out GridCoord to)
        {
            for (byte y = 0; y < _height; y++)
            {
                for (byte x = 0; x < _width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);

                    if (x < _width - 1)
                    {
                        GridCoord right = new GridCoord((byte)(x + 1), y);
                        if (TryEvaluateSwap(board, coord, right))
                        {
                            from = coord;
                            to = right;
                            return true;
                        }
                    }

                    if (y < _height - 1)
                    {
                        GridCoord up = new GridCoord(x, (byte)(y + 1));
                        if (TryEvaluateSwap(board, coord, up))
                        {
                            from = coord;
                            to = up;
                            return true;
                        }
                    }
                }
            }

            from = default;
            to = default;
            return false;
        }

        private bool TryEvaluateSwap(BoardModel board, GridCoord coordA, GridCoord coordB)
        {
            ref GridCell cellA = ref board.GetCell(coordA);
            ref GridCell cellB = ref board.GetCell(coordB);

            if (!cellA.IsPlayable || !cellB.IsPlayable || cellA.IsLocked || cellB.IsLocked)
            {
                return false;
            }

            if (cellA.Type == TileType.None || cellB.Type == TileType.None)
            {
                return false;
            }

            if (cellA.Type == TileType.Ingredient || cellB.Type == TileType.Ingredient)
            {
                return false;
            }

            if (TileTypeLibrary.IsSpecialTile(cellA.Type) || TileTypeLibrary.IsSpecialTile(cellB.Type))
            {
                return true;
            }

            if (cellA.Type == cellB.Type)
            {
                return false;
            }

            SwapTypes(board, coordA, coordB);
            bool hasMatch = _matchDetector.TryFindMatches(board, _scratchBuffer);
            SwapTypes(board, coordA, coordB);

            return hasMatch;
        }

        private static void SwapTypes(BoardModel board, GridCoord coordA, GridCoord coordB)
        {
            ref GridCell cellA = ref board.GetCell(coordA);
            ref GridCell cellB = ref board.GetCell(coordB);

            (cellA.Type, cellB.Type) = (cellB.Type, cellA.Type);
        }
    }
}
