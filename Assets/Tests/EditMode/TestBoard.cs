using Core.Data.Tiles;
using Gameplay.Board;

namespace Tests
{
    /// <summary>
    /// Builds boards from text rows, top row first, so a test reads like the screen.
    /// R B G Y P W are gems, '.' is empty, '#' is non-playable, 'X' is a locked box.
    /// </summary>
    public static class TestBoard
    {
        public static BoardModel Parse(params string[] rowsTopFirst)
        {
            byte height = (byte)rowsTopFirst.Length;
            byte width = (byte)rowsTopFirst[0].Length;
            BoardModel board = new BoardModel(width, height);

            for (int row = 0; row < height; row++)
            {
                byte y = (byte)(height - 1 - row);
                for (byte x = 0; x < width; x++)
                {
                    ref GridCell cell = ref board.GetCell(new GridCoord(x, y));
                    Apply(ref cell, rowsTopFirst[row][x]);
                }
            }

            return board;
        }

        public static GridCoord C(int x, int y) => new GridCoord((byte)x, (byte)y);

        private static void Apply(ref GridCell cell, char symbol)
        {
            switch (symbol)
            {
                case 'R': cell.Type = TileType.Red; break;
                case 'B': cell.Type = TileType.Blue; break;
                case 'G': cell.Type = TileType.Green; break;
                case 'Y': cell.Type = TileType.Yellow; break;
                case 'P': cell.Type = TileType.Purple; break;
                case 'W': cell.Type = TileType.White; break;
                case '#': cell.Flags |= CellFlags.NonPlayable; break;
                case 'X':
                    cell.Type = TileType.Box;
                    cell.Flags |= CellFlags.Locked;
                    cell.Durability = 2;
                    break;
            }
        }
    }
}
