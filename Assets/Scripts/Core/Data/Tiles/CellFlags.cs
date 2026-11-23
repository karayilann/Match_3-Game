using System;

namespace Core.Data.Tiles
{
    [Flags]
    public enum CellFlags : byte
    {
        None        = 0,
        NonPlayable = 1 << 0,
        Locked      = 1 << 1,
        Reserved1   = 1 << 2,
        Reserved2   = 1 << 3
    }
}