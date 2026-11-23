using System;
using System.Runtime.InteropServices;

namespace Core.Data.Tiles
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GridCoord : IEquatable<GridCoord>
    {
        public byte X;
        public byte Y;

        public GridCoord(byte x, byte y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridCoord other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);
        public override int GetHashCode() => (X << 8) | Y;
    }
}
