using UnityEngine;
using Core.Data;
using Core.Data.Obstacles;
using Core.Data.Tiles;
using Core.Pooling;
using Gameplay.Board;
using View;

namespace Gameplay.Spawning
{
    public sealed class TileSpawner
    {
        private readonly BoardModel _boardModel;
        private readonly GridToWorldConverter _converter;
        private readonly PoolManager<GemView> _pool;
        private readonly TileSpriteDatabase _spriteDatabase;
        private readonly GemView[] _views;
        private readonly ObstacleDatabase _obstacleDatabase;
        private readonly byte _activeColorCount;

        public TileSpawner(BoardModel boardModel, GridToWorldConverter converter, PoolManager<GemView> pool, TileSpriteDatabase spriteDatabase, ObstacleDatabase obstacleDatabase, GemView[] views, byte activeColorCount)
        {
            _boardModel = boardModel;
            _converter = converter;
            _pool = pool;
            _spriteDatabase = spriteDatabase;
            _obstacleDatabase = obstacleDatabase;
            _views = views;
            _activeColorCount = activeColorCount;
        }

        /// <summary>
        /// Rebuilds views for a board restored from a save, where cell data exists but no views were created yet.
        /// </summary>
        public void SpawnViewsFromModel()
        {
            for (byte y = 0; y < _boardModel.Height; y++)
            {
                for (byte x = 0; x < _boardModel.Width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    ref GridCell cell = ref _boardModel.GetCell(coord);

                    if (!cell.IsPlayable || cell.Type == TileType.None)
                    {
                        continue;
                    }

                    GemView view = _pool.Get();
                    view.SetPosition(_converter.ToWorldPosition(coord));
                    view.SetSprite(cell.IsLocked
                        ? _obstacleDatabase.GetDefinitionByTileType(cell.Type).GetSpriteForDurability(cell.Durability)
                        : _spriteDatabase.GetSprite(cell.Type));

                    _views[_boardModel.ToIndex(coord)] = view;
                }
            }
        }

        public void SpawnBoard()
        {
            for (byte y = 0; y < _boardModel.Height; y++)
            {
                for (byte x = 0; x < _boardModel.Width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    ref GridCell cell = ref _boardModel.GetCell(coord);

                    if (!cell.IsPlayable)
                    {
                        continue;
                    }

                    TileType type = GetNonMatchingType(coord);
                    cell.Type = type;

                    GemView view = _pool.Get();
                    view.SetPosition(_converter.ToWorldPosition(coord));
                    view.SetSprite(_spriteDatabase.GetSprite(type));

                    _views[_boardModel.ToIndex(coord)] = view;
                }
            }
        }

        /// <summary>
        /// Rerolls the tile type when placing it would immediately complete a horizontal or vertical run of three, guaranteeing a match-free starting board.
        /// </summary>
        private TileType GetNonMatchingType(GridCoord coord)
        {
            TileType type;

            do
            {
                type = TileTypeLibrary.PLAYABLE_TYPES[Random.Range(0, _activeColorCount)];
            }
            while (CreatesImmediateMatch(coord, type));

            return type;
        }

        private bool CreatesImmediateMatch(GridCoord coord, TileType type)
        {
            if (coord.X >= 2)
            {
                TileType left1 = _boardModel.GetCell(new GridCoord((byte)(coord.X - 1), coord.Y)).Type;
                TileType left2 = _boardModel.GetCell(new GridCoord((byte)(coord.X - 2), coord.Y)).Type;

                if (left1 == type && left2 == type)
                {
                    return true;
                }
            }

            if (coord.Y >= 2)
            {
                TileType down1 = _boardModel.GetCell(new GridCoord(coord.X, (byte)(coord.Y - 1))).Type;
                TileType down2 = _boardModel.GetCell(new GridCoord(coord.X, (byte)(coord.Y - 2))).Type;

                if (down1 == type && down2 == type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
