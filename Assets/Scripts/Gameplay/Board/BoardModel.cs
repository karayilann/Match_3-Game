using System.Runtime.CompilerServices;
using Core.Data;
using Core.Data.Tiles;

namespace Gameplay.Board
{
    public sealed class BoardModel
    {
        public readonly byte Width;
        public readonly byte Height;

        private readonly GridCell[] _cells;

        public BoardModel(byte width, byte height)
        {
            Width = width;
            Height = height;
            _cells = new GridCell[width * height];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToIndex(GridCoord coord) => coord.Y * Width + coord.X;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidCoord(GridCoord coord) => coord.X < Width && coord.Y < Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref GridCell GetCell(GridCoord coord) => ref _cells[ToIndex(coord)];
    }
}