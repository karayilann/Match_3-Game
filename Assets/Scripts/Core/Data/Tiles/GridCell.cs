using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Core.Data.Tiles
{
    /// <summary>
    /// Pack = 1 forces the struct to be exactly 2 bytes (1 byte TileType + 1 byte CellFlags) without any CPU memory padding.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GridCell
    {
        public TileType Type;
        public CellFlags Flags;
        public byte Durability;

        public bool IsPlayable 
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Flags & CellFlags.NonPlayable) == 0;
        }

        public bool IsLocked 
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Flags & CellFlags.Locked) != 0;
        }
    }
}