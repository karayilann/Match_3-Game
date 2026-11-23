using System;
using Core.Data.Tiles;

namespace View.HUD
{
    /// <summary>
    /// Counts tiles that are still flying toward a goal slot, so the HUD can hold back the
    /// counter until the icon actually lands.
    /// </summary>
    public static class GoalFlightTracker
    {
        private static readonly int[] _pending = new int[256];

        public static Action<TileType> OnArrived;

        public static int GetPending(TileType type)
        {
            return _pending[(byte)type];
        }

        public static void Add(TileType type)
        {
            _pending[(byte)type]++;
        }

        public static void Arrive(TileType type)
        {
            if (_pending[(byte)type] > 0)
            {
                _pending[(byte)type]--;
            }

            OnArrived?.Invoke(type);
        }

        public static void Reset()
        {
            Array.Clear(_pending, 0, _pending.Length);
        }
    }
}
