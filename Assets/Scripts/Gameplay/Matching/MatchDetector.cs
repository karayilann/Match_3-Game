using System;
using System.Collections.Generic;
using Core.Data.Tiles;
using Gameplay.Board;

namespace Gameplay.Matching
{
    public sealed class MatchDetector : IMatchDetector
    {
        private const byte MIN_MATCH_LENGTH = 3;

        private readonly byte _width;
        private readonly byte _height;
        private readonly bool[] _matchFlags;
        private readonly byte[] _hRunLengths;
        private readonly byte[] _vRunLengths;

        public MatchDetector(byte width, byte height)
        {
            _width = width;
            _height = height;
            int total = width * height;
            _matchFlags = new bool[total];
            _hRunLengths = new byte[total];
            _vRunLengths = new byte[total];
        }

        public bool TryFindMatches(BoardModel board, List<GridCoord> resultBuffer)
        {
            return TryFindMatches(board, resultBuffer, null, default);
        }

        public bool TryFindMatches(
            BoardModel board,
            List<GridCoord> resultBuffer,
            List<SpecialTileCreation> creationsBuffer,
            GridCoord playerActionCoord)
        {
            resultBuffer.Clear();
            creationsBuffer?.Clear();
            Array.Clear(_matchFlags, 0, _matchFlags.Length);
            Array.Clear(_hRunLengths, 0, _hRunLengths.Length);
            Array.Clear(_vRunLengths, 0, _vRunLengths.Length);

            ScanHorizontal(board);
            ScanVertical(board);
            ScanSquares(board, creationsBuffer, playerActionCoord);

            if (creationsBuffer != null)
            {
                DetectRunsAndSpecialTiles(board, creationsBuffer, playerActionCoord);
            }

            for (byte y = 0; y < _height; y++)
            {
                for (byte x = 0; x < _width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    if (_matchFlags[board.ToIndex(coord)])
                    {
                        resultBuffer.Add(coord);
                    }
                }
            }

            return resultBuffer.Count > 0;
        }

        private void ScanHorizontal(BoardModel board)
        {
            for (byte y = 0; y < _height; y++)
            {
                byte runStart = 0;
                byte runLength = 0;
                TileType runType = TileType.None;

                for (byte x = 0; x < _width; x++)
                {
                    bool matchable = TryGetMatchableType(board, new GridCoord(x, y), out TileType type);

                    if (matchable && type == runType)
                    {
                        runLength++;
                        continue;
                    }

                    FlagHorizontalRun(board, y, runStart, runLength);
                    runType = type;
                    runStart = x;
                    runLength = matchable ? (byte)1 : (byte)0;
                }

                FlagHorizontalRun(board, y, runStart, runLength);
            }
        }

        private void ScanVertical(BoardModel board)
        {
            for (byte x = 0; x < _width; x++)
            {
                byte runStart = 0;
                byte runLength = 0;
                TileType runType = TileType.None;

                for (byte y = 0; y < _height; y++)
                {
                    bool matchable = TryGetMatchableType(board, new GridCoord(x, y), out TileType type);

                    if (matchable && type == runType)
                    {
                        runLength++;
                        continue;
                    }

                    FlagVerticalRun(board, x, runStart, runLength);
                    runType = type;
                    runStart = y;
                    runLength = matchable ? (byte)1 : (byte)0;
                }

                FlagVerticalRun(board, x, runStart, runLength);
            }
        }

        private void FlagHorizontalRun(BoardModel board, byte y, byte runStartX, byte runLength)
        {
            if (runLength < MIN_MATCH_LENGTH)
            {
                return;
            }

            for (byte x = runStartX; x < runStartX + runLength; x++)
            {
                int index = board.ToIndex(new GridCoord(x, y));
                _matchFlags[index] = true;
                _hRunLengths[index] = runLength;
            }
        }

        private void FlagVerticalRun(BoardModel board, byte x, byte runStartY, byte runLength)
        {
            if (runLength < MIN_MATCH_LENGTH)
            {
                return;
            }

            for (byte y = runStartY; y < runStartY + runLength; y++)
            {
                int index = board.ToIndex(new GridCoord(x, y));
                _matchFlags[index] = true;
                _vRunLengths[index] = runLength;
            }
        }

        private void ScanSquares(
            BoardModel board,
            List<SpecialTileCreation> creationsBuffer,
            GridCoord playerActionCoord)
        {
            for (byte y = 0; y < _height - 1; y++)
            {
                for (byte x = 0; x < _width - 1; x++)
                {
                    GridCoord c00 = new GridCoord(x, y);
                    GridCoord c10 = new GridCoord((byte)(x + 1), y);
                    GridCoord c01 = new GridCoord(x, (byte)(y + 1));
                    GridCoord c11 = new GridCoord((byte)(x + 1), (byte)(y + 1));

                    if (!TryGetMatchableType(board, c00, out TileType t00) ||
                        !TryGetMatchableType(board, c10, out TileType t10) ||
                        !TryGetMatchableType(board, c01, out TileType t01) ||
                        !TryGetMatchableType(board, c11, out TileType t11))
                    {
                        continue;
                    }

                    if (t00 == t10 && t00 == t01 && t00 == t11)
                    {
                        _matchFlags[board.ToIndex(c00)] = true;
                        _matchFlags[board.ToIndex(c10)] = true;
                        _matchFlags[board.ToIndex(c01)] = true;
                        _matchFlags[board.ToIndex(c11)] = true;

                        if (creationsBuffer != null)
                        {
                            GridCoord creationCoord = (playerActionCoord.Equals(c10) || playerActionCoord.Equals(c01) || playerActionCoord.Equals(c11))
                                ? playerActionCoord
                                : c00;

                            creationsBuffer.Add(new SpecialTileCreation(creationCoord, TileType.Propeller));
                        }
                    }
                }
            }
        }

        private void DetectRunsAndSpecialTiles(
            BoardModel board,
            List<SpecialTileCreation> creationsBuffer,
            GridCoord playerActionCoord)
        {
            int firstBomb = creationsBuffer.Count;

            for (byte y = 0; y < _height; y++)
            {
                for (byte x = 0; x < _width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    int index = board.ToIndex(coord);

                    if (_hRunLengths[index] >= MIN_MATCH_LENGTH && _vRunLengths[index] >= MIN_MATCH_LENGTH)
                    {
                        creationsBuffer.Add(new SpecialTileCreation(coord, TileType.Bomb));
                    }
                }
            }

            int lastBomb = creationsBuffer.Count;
            for (int i = firstBomb; i < lastBomb; i++)
            {
                ConsumeCrossingRuns(board, creationsBuffer[i].Coord);
            }

            for (byte y = 0; y < _height; y++)
            {
                for (byte x = 0; x < _width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    int index = board.ToIndex(coord);

                    byte hLen = _hRunLengths[index];
                    byte vLen = _vRunLengths[index];

                    if (hLen >= 5 && (playerActionCoord.Equals(coord) || x == 0 || _hRunLengths[board.ToIndex(new GridCoord((byte)(x - 1), y))] < 5))
                    {
                        creationsBuffer.Add(new SpecialTileCreation(playerActionCoord.Y == y ? playerActionCoord : coord, TileType.ColorBomb));
                        continue;
                    }

                    if (vLen >= 5 && (playerActionCoord.Equals(coord) || y == 0 || _vRunLengths[board.ToIndex(new GridCoord(x, (byte)(y - 1)))] < 5))
                    {
                        creationsBuffer.Add(new SpecialTileCreation(playerActionCoord.X == x ? playerActionCoord : coord, TileType.ColorBomb));
                        continue;
                    }

                    if (hLen == 4 && (playerActionCoord.Equals(coord) || x == 0 || _hRunLengths[board.ToIndex(new GridCoord((byte)(x - 1), y))] < 4))
                    {
                        creationsBuffer.Add(new SpecialTileCreation(playerActionCoord.Y == y ? playerActionCoord : coord, TileType.VerticalRocket));
                        continue;
                    }

                    if (vLen == 4 && (playerActionCoord.Equals(coord) || y == 0 || _vRunLengths[board.ToIndex(new GridCoord(x, (byte)(y - 1)))] < 4))
                    {
                        creationsBuffer.Add(new SpecialTileCreation(playerActionCoord.X == x ? playerActionCoord : coord, TileType.HorizontalRocket));
                    }
                }
            }
        }

        /// <summary>
        /// Zeroing only the crossing cell would leave the arms of an L or T with their run
        /// length, so a long arm would spawn a rocket or color bomb next to the bomb.
        /// </summary>
        private void ConsumeCrossingRuns(BoardModel board, GridCoord center)
        {
            TileType type = board.GetCell(center).Type;

            for (int x = center.X; x >= 0; x--)
            {
                if (!ClearRunCell(board, (byte)x, center.Y, type, _hRunLengths))
                {
                    break;
                }
            }

            for (int x = center.X + 1; x < _width; x++)
            {
                if (!ClearRunCell(board, (byte)x, center.Y, type, _hRunLengths))
                {
                    break;
                }
            }

            for (int y = center.Y; y >= 0; y--)
            {
                if (!ClearRunCell(board, center.X, (byte)y, type, _vRunLengths))
                {
                    break;
                }
            }

            for (int y = center.Y + 1; y < _height; y++)
            {
                if (!ClearRunCell(board, center.X, (byte)y, type, _vRunLengths))
                {
                    break;
                }
            }
        }

        private static bool ClearRunCell(BoardModel board, byte x, byte y, TileType type, byte[] runLengths)
        {
            GridCoord coord = new GridCoord(x, y);
            int index = board.ToIndex(coord);

            if (runLengths[index] == 0 || board.GetCell(coord).Type != type)
            {
                return false;
            }

            runLengths[index] = 0;
            return true;
        }

        private static bool TryGetMatchableType(BoardModel board, GridCoord coord, out TileType type)
        {
            ref GridCell cell = ref board.GetCell(coord);

            if (!cell.IsPlayable || cell.IsLocked || cell.Type == TileType.None || !TileTypeLibrary.IsStandardGem(cell.Type))
            {
                type = TileType.None;
                return false;
            }

            type = cell.Type;
            return true;
        }
    }
}
