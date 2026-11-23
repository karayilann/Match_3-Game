using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core.Data.Goals;
using Core.Data.Tiles;
using Signals;
using UnityEngine;

namespace Gameplay.Goals
{
    public sealed class GoalService
    {
        private readonly LevelGoalEntry[] _goals;
        private readonly byte[] _currentCounts;
        private bool _isAllCompleted;

        public int GoalCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _goals.Length;
        }

        public bool IsAllCompleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isAllCompleted;
        }

        public GoalService(List<LevelGoalEntry> goals)
        {
            int count = goals != null ? goals.Count : 0;
            _goals = new LevelGoalEntry[count];
            _currentCounts = new byte[count];

            for (int i = 0; i < count; i++)
            {
                _goals[i] = goals[i];
            }

            _isAllCompleted = count == 0;
        }

        public byte GetCurrentCount(int index) => _currentCounts[index];

        public void RestoreGoalCounts(byte[] counts)
        {
            if (counts == null)
            {
                return;
            }

            int count = Mathf.Min(counts.Length, _goals.Length);
            bool allComplete = true;

            for (int i = 0; i < count; i++)
            {
                _currentCounts[i] = counts[i];
                LevelGoalEntry entry = _goals[i];
                if (_currentCounts[i] < entry.TargetCount)
                {
                    allComplete = false;
                }
                LevelSignals.RaiseGoalUpdated((byte)i, entry.TargetType, _currentCounts[i], entry.TargetCount);
            }

            _isAllCompleted = allComplete;
            if (_isAllCompleted && count > 0)
            {
                LevelSignals.RaiseAllGoalsCompleted();
            }
        }

        public void BroadcastInitialState()
        {
            int count = _goals.Length;
            LevelSignals.RaiseGoalsInitialized((byte)count);

            for (int i = 0; i < count; i++)
            {
                LevelGoalEntry entry = _goals[i];
                LevelSignals.RaiseGoalUpdated((byte)i, entry.TargetType, _currentCounts[i], entry.TargetCount);
            }
        }

        /// <summary>
        /// Updates matching goal counters without allocations and triggers completion once all targets reach their quota.
        /// </summary>
        public void OnTileCleared(TileType clearedType, GridCoord coord)
        {
            Advance(clearedType, coord, GoalKind.Clear);
        }

        /// <summary>
        /// Creation goals count tiles the player made, so clearing a special tile never advances them.
        /// </summary>
        public void OnTileCreated(TileType createdType, GridCoord coord)
        {
            Advance(createdType, coord, GoalKind.Create);
        }

        public int GetRemaining(TileType type, GoalKind kind)
        {
            int remaining = 0;

            for (int i = 0; i < _goals.Length; i++)
            {
                LevelGoalEntry entry = _goals[i];
                if (entry.TargetType == type && entry.Kind == kind && _currentCounts[i] < entry.TargetCount)
                {
                    remaining += entry.TargetCount - _currentCounts[i];
                }
            }

            return remaining;
        }

        private void Advance(TileType type, GridCoord coord, GoalKind kind)
        {
            if (type == TileType.None || _isAllCompleted)
            {
                return;
            }

            TileType clearedType = type;
            int count = _goals.Length;
            bool anyUpdated = false;

            for (int i = 0; i < count; i++)
            {
                LevelGoalEntry entry = _goals[i];
                if (entry.TargetType != clearedType || entry.Kind != kind)
                {
                    continue;
                }

                byte current = _currentCounts[i];
                if (current >= entry.TargetCount)
                {
                    continue;
                }

                current++;
                _currentCounts[i] = current;
                anyUpdated = true;

                BoardSignals.RaiseGoalTileCollected(coord, clearedType);
                LevelSignals.RaiseGoalUpdated((byte)i, entry.TargetType, current, entry.TargetCount);
            }

            if (!anyUpdated)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (_currentCounts[i] < _goals[i].TargetCount)
                {
                    return;
                }
            }

            _isAllCompleted = true;
            LevelSignals.RaiseAllGoalsCompleted();
        }

        public void OnTileCleared(TileType clearedType)
        {
            OnTileCleared(clearedType, default);
        }
    }
}
