namespace Core.Data.Obstacles
{
    [System.Serializable]
    public struct ObstaclePlacementEntry
    {
        public byte X;
        public byte Y;
        public ObstacleType Type;

        public ObstaclePlacementEntry(byte x, byte y, ObstacleType type)
        {
            X = x;
            Y = y;
            Type = type;
        }
    }
}
