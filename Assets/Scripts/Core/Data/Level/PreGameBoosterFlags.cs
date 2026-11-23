using System;

namespace Core.Data.Level
{
    [Flags]
    public enum PreGameBoosterFlags : byte
    {
        None = 0,
        RocketBomb = 1 << 0,
        ColorBomb = 1 << 1,
        ExtraMoves = 1 << 2
    }
}
