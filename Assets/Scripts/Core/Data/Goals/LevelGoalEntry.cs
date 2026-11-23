using System;
using System.Runtime.InteropServices;
using Core.Data.Tiles;

namespace Core.Data.Goals
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LevelGoalEntry
    {
        public TileType TargetType;
        public byte TargetCount;
        public GoalKind Kind;

        public LevelGoalEntry(TileType targetType, byte targetCount)
        {
            TargetType = targetType;
            TargetCount = targetCount;
            Kind = GoalKind.Clear;
        }

        public LevelGoalEntry(TileType targetType, byte targetCount, GoalKind kind)
        {
            TargetType = targetType;
            TargetCount = targetCount;
            Kind = kind;
        }
    }
}
