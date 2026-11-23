using Core.Data.Level;
using Core.Data.Tiles;
using Gameplay.Board;
using View;

namespace Gameplay.Boosters
{
    public static class PreGameBoosterApplier
    {
        private const byte EXTRA_MOVES = 3;

        public static void Apply(PreGameBoosterFlags flags, BoardData board, TileSpriteDatabase spriteDatabase, LevelLimitService limit)
        {
            if ((flags & PreGameBoosterFlags.RocketBomb) != 0)
            {
                PlaceRocketBomb(board, spriteDatabase);
            }

            if ((flags & PreGameBoosterFlags.ColorBomb) != 0)
            {
                PlaceColorBomb(board, spriteDatabase);
            }

            if ((flags & PreGameBoosterFlags.ExtraMoves) != 0)
            {
                limit.AddMoves(EXTRA_MOVES);
            }
        }

        private static void PlaceRocketBomb(BoardData board, TileSpriteDatabase spriteDatabase)
        {
            byte width = board.Model.Width;
            byte height = board.Model.Height;
            GridCoord rocketCoord = new GridCoord((byte)(width / 2 - 1), (byte)(height / 2));
            GridCoord bombCoord = new GridCoord((byte)(width / 2), (byte)(height / 2));

            if (!IsFree(board.Model, rocketCoord) || !IsFree(board.Model, bombCoord))
            {
                return;
            }

            SetSpecial(board, spriteDatabase, rocketCoord, TileType.HorizontalRocket);
            SetSpecial(board, spriteDatabase, bombCoord, TileType.Bomb);
        }

        private static void PlaceColorBomb(BoardData board, TileSpriteDatabase spriteDatabase)
        {
            byte width = board.Model.Width;
            byte height = board.Model.Height;
            GridCoord coord = new GridCoord((byte)(width / 2), (byte)(height / 2 + 1));

            if (IsFree(board.Model, coord))
            {
                SetSpecial(board, spriteDatabase, coord, TileType.ColorBomb);
            }
        }

        private static bool IsFree(BoardModel model, GridCoord coord)
        {
            return model.IsValidCoord(coord) && !model.GetCell(coord).IsLocked;
        }

        private static void SetSpecial(BoardData board, TileSpriteDatabase spriteDatabase, GridCoord coord, TileType type)
        {
            board.Model.GetCell(coord).Type = type;
            GemView view = board.Views[board.Model.ToIndex(coord)];

            if (view != null)
            {
                view.SetSprite(spriteDatabase.GetSprite(type));
            }
        }
    }
}
