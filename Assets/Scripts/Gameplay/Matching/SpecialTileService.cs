using System;
using System.Collections.Generic;
using Core.Data.Tiles;
using Gameplay.Board;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay.Matching
{
    public static class SpecialTileService
    {
        public static bool TryExecuteCombo(
            BoardModel board,
            GridCoord coordA,
            GridCoord coordB,
            TileType typeA,
            TileType typeB,
            List<GridCoord> outAffectedCells)
        {
            if (!TileTypeLibrary.IsSpecialTile(typeA) && !TileTypeLibrary.IsSpecialTile(typeB))
            {
                return false;
            }

            outAffectedCells.Clear();

            bool isRocketA = typeA == TileType.HorizontalRocket || typeA == TileType.VerticalRocket;
            bool isRocketB = typeB == TileType.HorizontalRocket || typeB == TileType.VerticalRocket;
            bool isBombA = typeA == TileType.Bomb;
            bool isBombB = typeB == TileType.Bomb;
            bool isColorA = typeA == TileType.ColorBomb;
            bool isColorB = typeB == TileType.ColorBomb;
            bool isPropellerA = typeA == TileType.Propeller;
            bool isPropellerB = typeB == TileType.Propeller;

            if (isColorA && isColorB)
            {
                ClearAllPlayable(board, outAffectedCells);
                return true;
            }

            if (isColorA || isColorB)
            {
                TileType otherType = isColorA ? typeB : typeA;
                GridCoord center = isColorA ? coordA : coordB;
                GridCoord other = isColorA ? coordB : coordA;

                if (TileTypeLibrary.IsStandardGem(otherType))
                {
                    board.GetCell(center).Type = otherType;
                    ClearAllOfType(board, otherType, outAffectedCells);
                }
                else
                {
                    ConvertMostCommonColorToSpecial(board, center, other, otherType, outAffectedCells);
                }
                return true;
            }

            if (isBombA && isBombB)
            {
                AddArea(board, coordB, 2, outAffectedCells);
                return true;
            }

            if ((isRocketA && isBombB) || (isBombA && isRocketB))
            {
                AddRowsAndColumns(board, coordB, 1, outAffectedCells);
                return true;
            }

            if (isRocketA && isRocketB)
            {
                AddCross(board, coordB, outAffectedCells);
                return true;
            }

            if (isPropellerA || isPropellerB)
            {
                AddPropellerPlus(board, coordB, outAffectedCells);
                return true;
            }

            TileType special = TileTypeLibrary.IsSpecialTile(typeA) ? typeA : typeB;
            GridCoord target = TileTypeLibrary.IsSpecialTile(typeA) ? coordA : coordB;
            ExecuteSingle(board, target, special, outAffectedCells);
            return true;
        }

        public static void ExecuteSingle(
            BoardModel board,
            GridCoord coord,
            TileType type,
            List<GridCoord> outAffectedCells)
        {
            switch (type)
            {
                case TileType.HorizontalRocket:
                    AddRow(board, coord.Y, outAffectedCells);
                    break;
                case TileType.VerticalRocket:
                    AddColumn(board, coord.X, outAffectedCells);
                    break;
                case TileType.Bomb:
                    AddArea(board, coord, 1, outAffectedCells);
                    break;
                case TileType.ColorBomb:
                    outAffectedCells.Add(coord);
                    ClearNearestColor(board, coord, outAffectedCells);
                    break;
                case TileType.Propeller:
                    AddPropellerSingle(board, coord, outAffectedCells);
                    break;
            }
        }

        private static void ClearAllPlayable(BoardModel board, List<GridCoord> outCells)
        {
            byte width = board.Width;
            byte height = board.Height;

            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    ref GridCell cell = ref board.GetCell(coord);
                    if (cell.IsPlayable && cell.Type != TileType.None)
                    {
                        outCells.Add(coord);
                    }
                }
            }
        }

        private static void ClearAllOfType(BoardModel board, TileType type, List<GridCoord> outCells)
        {
            byte width = board.Width;
            byte height = board.Height;

            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    ref GridCell cell = ref board.GetCell(coord);
                    if (cell.IsPlayable && cell.Type == type)
                    {
                        outCells.Add(coord);
                    }
                }
            }
        }

        private static void ClearNearestColor(BoardModel board, GridCoord origin, List<GridCoord> outCells)
        {
            TileType chosenColor = FindNearestStandardGemColor(board, origin);
            if (chosenColor != TileType.None)
            {
                ClearAllOfType(board, chosenColor, outCells);
            }
        }

        private static TileType FindNearestStandardGemColor(BoardModel board, GridCoord origin)
        {
            byte width = board.Width;
            byte height = board.Height;
            int bestDistance = int.MaxValue;
            TileType bestColor = TileType.None;

            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    if (coord.Equals(origin))
                    {
                        continue;
                    }

                    ref GridCell cell = ref board.GetCell(coord);
                    if (!cell.IsPlayable || cell.IsLocked || !TileTypeLibrary.IsStandardGem(cell.Type))
                    {
                        continue;
                    }

                    int distance = Mathf.Abs(x - origin.X) + Mathf.Abs(y - origin.Y);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestColor = cell.Type;
                        if (distance == 1)
                        {
                            return bestColor;
                        }
                    }
                }
            }

            return bestColor;
        }

        /// <summary>
        /// Picks the most frequent color so the combo is deterministic and always hits the biggest
        /// group, then turns every tile of that color into the special. Detonation is left to the
        /// match step's chain reaction; the color bomb cell itself is converted too, otherwise the
        /// chain reaction would treat it as a standalone bomb and clear a random color.
        /// Rockets alternate orientation so the result covers both rows and columns.
        /// </summary>
        private static void ConvertMostCommonColorToSpecial(BoardModel board, GridCoord colorBombCoord, GridCoord partnerCoord, TileType special, List<GridCoord> outCells)
        {
            Span<int> counts = stackalloc int[256];
            byte width = board.Width;
            byte height = board.Height;

            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    ref GridCell cell = ref board.GetCell(new GridCoord(x, y));
                    if (cell.IsPlayable && !cell.IsLocked && TileTypeLibrary.IsStandardGem(cell.Type))
                    {
                        counts[(byte)cell.Type]++;
                    }
                }
            }

            TileType chosenColor = TileType.None;
            int best = 0;
            TileType[] playable = TileTypeLibrary.PLAYABLE_TYPES;
            for (int i = 0; i < playable.Length; i++)
            {
                int count = counts[(byte)playable[i]];
                if (count > best)
                {
                    best = count;
                    chosenColor = playable[i];
                }
            }

            board.GetCell(colorBombCoord).Type = special;
            outCells.Add(colorBombCoord);
            outCells.Add(partnerCoord);

            if (chosenColor == TileType.None)
            {
                return;
            }

            bool isRocket = special == TileType.HorizontalRocket || special == TileType.VerticalRocket;
            int index = 0;

            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    ref GridCell cell = ref board.GetCell(coord);
                    if (!cell.IsPlayable || cell.IsLocked || cell.Type != chosenColor)
                    {
                        continue;
                    }

                    TileType detonation = special;
                    if (isRocket)
                    {
                        detonation = (index & 1) == 0 ? TileType.HorizontalRocket : TileType.VerticalRocket;
                    }

                    index++;
                    cell.Type = detonation;
                    cell.Durability = 0;
                    outCells.Add(coord);
                }
            }
        }

        private static void AddRow(BoardModel board, byte y, List<GridCoord> outCells)
        {
            for (byte x = 0; x < board.Width; x++)
            {
                GridCoord c = new GridCoord(x, y);
                if (board.GetCell(c).IsPlayable)
                {
                    outCells.Add(c);
                }
            }
        }

        private static void AddColumn(BoardModel board, byte x, List<GridCoord> outCells)
        {
            for (byte y = 0; y < board.Height; y++)
            {
                GridCoord c = new GridCoord(x, y);
                if (board.GetCell(c).IsPlayable)
                {
                    outCells.Add(c);
                }
            }
        }

        private static void AddCross(BoardModel board, GridCoord center, List<GridCoord> outCells)
        {
            AddRow(board, center.Y, outCells);
            AddColumn(board, center.X, outCells);
        }

        private static void AddRowsAndColumns(BoardModel board, GridCoord center, int spread, List<GridCoord> outCells)
        {
            for (int dy = -spread; dy <= spread; dy++)
            {
                int y = center.Y + dy;
                if (y >= 0 && y < board.Height)
                {
                    AddRow(board, (byte)y, outCells);
                }
            }

            for (int dx = -spread; dx <= spread; dx++)
            {
                int x = center.X + dx;
                if (x >= 0 && x < board.Width)
                {
                    AddColumn(board, (byte)x, outCells);
                }
            }
        }

        private static void AddArea(BoardModel board, GridCoord center, int radius, List<GridCoord> outCells)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = center.X + dx;
                    int y = center.Y + dy;
                    if (x >= 0 && x < board.Width && y >= 0 && y < board.Height)
                    {
                        GridCoord c = new GridCoord((byte)x, (byte)y);
                        if (board.GetCell(c).IsPlayable)
                        {
                            outCells.Add(c);
                        }
                    }
                }
            }
        }

        private static void AddPropellerSingle(BoardModel board, GridCoord center, List<GridCoord> outCells)
        {
            outCells.Add(center);
            TryAddCoord(board, center.X + 1, center.Y, outCells);
            TryAddCoord(board, center.X - 1, center.Y, outCells);
            TryAddCoord(board, center.X, center.Y + 1, outCells);
            TryAddCoord(board, center.X, center.Y - 1, outCells);

            GridCoord? target = FindPriorityTarget(board, center);
            if (target.HasValue)
            {
                outCells.Add(target.Value);
            }
        }

        private static void AddPropellerPlus(BoardModel board, GridCoord center, List<GridCoord> outCells)
        {
            AddArea(board, center, 1, outCells);

            for (int i = 0; i < 3; i++)
            {
                GridCoord? target = FindPriorityTarget(board, center);
                if (target.HasValue)
                {
                    outCells.Add(target.Value);
                }
            }
        }

        private static void TryAddCoord(BoardModel board, int x, int y, List<GridCoord> outCells)
        {
            if (x >= 0 && x < board.Width && y >= 0 && y < board.Height)
            {
                GridCoord c = new GridCoord((byte)x, (byte)y);
                if (board.GetCell(c).IsPlayable)
                {
                    outCells.Add(c);
                }
            }
        }

        private static GridCoord? FindPriorityTarget(BoardModel board, GridCoord center)
        {
            for (byte y = 0; y < board.Height; y++)
            {
                for (byte x = 0; x < board.Width; x++)
                {
                    GridCoord c = new GridCoord(x, y);
                    ref GridCell cell = ref board.GetCell(c);
                    if (cell.IsPlayable && cell.IsLocked)
                    {
                        return c;
                    }
                }
            }

            for (byte y = 0; y < board.Height; y++)
            {
                for (byte x = 0; x < board.Width; x++)
                {
                    GridCoord c = new GridCoord(x, y);
                    ref GridCell cell = ref board.GetCell(c);
                    if (cell.IsPlayable && cell.Type != TileType.None && !c.Equals(center))
                    {
                        return c;
                    }
                }
            }

            return null;
        }
    }
}
