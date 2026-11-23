using Core.Data.Tiles;

namespace Gameplay.Matching
{
    public struct SpecialTileCreation
    {
        public GridCoord Coord;
        public TileType CreatedType;

        public SpecialTileCreation(GridCoord coord, TileType createdType)
        {
            Coord = coord;
            CreatedType = createdType;
        }
    }
}
