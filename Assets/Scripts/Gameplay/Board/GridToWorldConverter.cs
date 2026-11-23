using System.Runtime.CompilerServices;
using UnityEngine;
using Core.Data;
using Core.Data.Tiles;

namespace Gameplay.Board
{
    public sealed class GridToWorldConverter
    {
        private readonly byte _width;
        private readonly byte _height;
        private readonly float _cellSize;
        private readonly float _inverseCellSize;
        private readonly Vector2 _origin;

        public GridToWorldConverter(byte width, byte height, float cellSize)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            _inverseCellSize = 1f / cellSize;
            _origin = new Vector2(
                -(width * cellSize) * 0.5f + cellSize * 0.5f,
                -(height * cellSize) * 0.5f + cellSize * 0.5f
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 ToWorldPosition(GridCoord coord)
        {
            return new Vector3(
                _origin.x + coord.X * _cellSize,
                _origin.y + coord.Y * _cellSize,
                0f
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidCoord(GridCoord coord)
        {
            return coord.X < _width && coord.Y < _height;
        }
        
        public bool TryToGridCoord(Vector3 worldPosition, out GridCoord coord)
        {
            float localX = worldPosition.x - _origin.x + _cellSize * 0.5f;
            float localY = worldPosition.y - _origin.y + _cellSize * 0.5f;

            int x = Mathf.FloorToInt(localX * _inverseCellSize);
            int y = Mathf.FloorToInt(localY * _inverseCellSize);

            if (x < 0 || y < 0 || x >= _width || y >= _height)
            {
                coord = default;
                return false;
            }

            coord = new GridCoord((byte)x, (byte)y);
            return true;
        }
    }
}