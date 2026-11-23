using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core.Data.Obstacles;
using Core.Data.Tiles;
using Gameplay.Board;

namespace Gameplay.Obstacles
{
    public sealed class ObstacleService
    {
        private readonly byte _width;
        private readonly byte _height;
        private readonly ObstacleDatabase _database;
        private readonly bool[] _damagedFlags;
        private readonly List<GridCoord> _destroyedBuffer;
        private readonly List<GridCoord> _damagedBuffer;

        public List<GridCoord> DamagedBuffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _damagedBuffer;
        }

        public ObstacleService(byte width, byte height, ObstacleDatabase database)
        {
            int totalCells = width * height;
            _width = width;
            _height = height;
            _database = database;
            _damagedFlags = new bool[totalCells];
            _destroyedBuffer = new List<GridCoord>(totalCells);
            _damagedBuffer = new List<GridCoord>(totalCells);
        }

        public List<GridCoord> ResolveDamage(BoardModel board, List<GridCoord> matchedCoords)
        {
            _destroyedBuffer.Clear();
            _damagedBuffer.Clear();
            Array.Clear(_damagedFlags, 0, _damagedFlags.Length);

            ExtractSpecialDamageTargets(board, matchedCoords);

            for (int i = 0; i < matchedCoords.Count; i++)
            {
                GridCoord coord = matchedCoords[i];

                TryDamageNeighbor(board, coord.X - 1, coord.Y);
                TryDamageNeighbor(board, coord.X + 1, coord.Y);
                TryDamageNeighbor(board, coord.X, coord.Y - 1);
                TryDamageNeighbor(board, coord.X, coord.Y + 1);
            }

            return _destroyedBuffer;
        }

        private void TryDamageNeighbor(BoardModel board, int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height)
            {
                return;
            }

            GridCoord neighbor = new GridCoord((byte)x, (byte)y);
            ref GridCell cell = ref board.GetCell(neighbor);

            if (!cell.IsLocked)
            {
                return;
            }

            ObstacleDefinition definition = _database.GetDefinitionByTileType(cell.Type);
            if (!definition.CanTakeAdjacentDamage)
            {
                return;
            }

            ApplyDamage(board, neighbor, ref cell);
        }

        /// <summary>
        /// Special tile blasts put obstacle cells into the matched list as plain tiles, which would wipe
        /// them regardless of durability. Every locked cell is pulled out here so the blast becomes one
        /// durability hit, and a locked cell can never reach the clearing step with its flag still set.
        /// </summary>
        private void ExtractSpecialDamageTargets(BoardModel board, List<GridCoord> matchedCoords)
        {
            for (int i = matchedCoords.Count - 1; i >= 0; i--)
            {
                GridCoord coord = matchedCoords[i];
                ref GridCell cell = ref board.GetCell(coord);

                if (!cell.IsLocked)
                {
                    continue;
                }

                matchedCoords.RemoveAt(i);
                ApplyDamage(board, coord, ref cell);
            }
        }

        private void ApplyDamage(BoardModel board, GridCoord coord, ref GridCell cell)
        {
            int index = board.ToIndex(coord);

            if (_damagedFlags[index])
            {
                return;
            }

            _damagedFlags[index] = true;
            cell.Durability--;

            if (cell.Durability <= 0)
            {
                cell.Flags &= ~CellFlags.Locked;
                _destroyedBuffer.Add(coord);
                return;
            }

            _damagedBuffer.Add(coord);
        }

        public bool DamageCell(BoardModel board, GridCoord coord)
        {
            ref GridCell cell = ref board.GetCell(coord);
            if (!cell.IsLocked)
            {
                return false;
            }

            cell.Durability--;
            if (cell.Durability <= 0)
            {
                cell.Flags &= ~CellFlags.Locked;
                return true;
            }

            return false;
        }
    }
}
