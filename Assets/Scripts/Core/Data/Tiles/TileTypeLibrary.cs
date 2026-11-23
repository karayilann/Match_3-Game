using System.Runtime.CompilerServices;

namespace Core.Data.Tiles
{
    public static class TileTypeLibrary
    {
        public static readonly TileType[] PLAYABLE_TYPES =
        {
            TileType.Red,
            TileType.Blue,
            TileType.Green,
            TileType.Yellow,
            TileType.Purple,
            TileType.White
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSpecialTile(TileType type)
        {
            return type >= TileType.HorizontalRocket && type <= TileType.Propeller;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsStandardGem(TileType type)
        {
            return type >= TileType.Red && type <= TileType.White;
        }
    }
}
