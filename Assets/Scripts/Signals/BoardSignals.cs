using System;
using Core.Data.Tiles;
using UnityEngine;

namespace Signals
{
    public static class BoardSignals
    {
        public static event Action<GridCoord> OnTileMatched;
        public static event Action<Vector3, int, byte, byte> OnMatchScored;
        public static event Action<TileType> OnSpecialTileDetonated;
        public static event Action OnBoardReshuffled;
        public static event Action OnBoardSettled;
        public static event Action<GridCoord> OnCellTapped;
        public static event Action<GridCoord, GridCoord> OnTileSwiped;
        public static event Action<GridCoord, TileType> OnGoalTileCollected;
        public static event Action<Vector3, TileType> OnGoalTileFlyRequested;

        public static void RaiseTileMatched(GridCoord coord)
        {
            OnTileMatched?.Invoke(coord);
        }

        public static void RaiseMatchScored(Vector3 center, int points, byte cascadeStep, byte clearedCount)
        {
            OnMatchScored?.Invoke(center, points, cascadeStep, clearedCount);
        }

        public static void RaiseSpecialTileDetonated(TileType specialType)
        {
            OnSpecialTileDetonated?.Invoke(specialType);
        }

        public static void RaiseBoardReshuffled()
        {
            OnBoardReshuffled?.Invoke();
        }

        public static void RaiseBoardSettled()
        {
            OnBoardSettled?.Invoke();
        }

        public static void RaiseCellTapped(GridCoord coord)
        {
            OnCellTapped?.Invoke(coord);
        }

        public static void RaiseTileSwiped(GridCoord from, GridCoord to)
        {
            OnTileSwiped?.Invoke(from, to);
        }

        public static void RaiseGoalTileCollected(GridCoord coord, TileType type)
        {
            OnGoalTileCollected?.Invoke(coord, type);
        }

        public static void RaiseGoalTileFlyRequested(Vector3 worldPosition, TileType type)
        {
            OnGoalTileFlyRequested?.Invoke(worldPosition, type);
        }

        /// <summary>
        /// Enter Play Mode runs without a domain reload, so static subscribers would survive into the next
        /// session and fire on destroyed objects. Clearing them before the first scene load prevents that.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            OnTileMatched = null;
            OnMatchScored = null;
            OnSpecialTileDetonated = null;
            OnBoardReshuffled = null;
            OnBoardSettled = null;
            OnCellTapped = null;
            OnTileSwiped = null;
            OnGoalTileCollected = null;
            OnGoalTileFlyRequested = null;
        }
    }
}
