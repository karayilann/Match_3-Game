using System;
using Core.Data.Tiles;
using UnityEngine;

namespace Signals
{
    public static class LevelSignals
    {
        public static event Action<int> OnScoreChanged;
        public static event Action<int, int, int> OnScoreTargetsConfigured;
        public static event Action<int> OnStarAwarded;
        public static event Action<byte> OnMovesChanged;
        public static event Action<int> OnTimeChanged;
        public static event Action<byte> OnLowMovesWarning;
        public static event Action OnLowMovesWarningEnded;
        public static event Action OnLevelLimitReached;
        public static event Action<byte> OnGoalsInitialized;
        public static event Action<byte, TileType, byte, byte> OnGoalUpdated;
        public static event Action OnAllGoalsCompleted;
        public static event Action OnWinFinaleStarted;
        public static event Action OnFinaleSkipRequested;
        public static event Action<int> OnLevelWon;
        public static event Action<byte> OnContinueWithMoves;

        public static void RaiseScoreChanged(int score)
        {
            OnScoreChanged?.Invoke(score);
        }

        public static void RaiseScoreTargetsConfigured(int oneStarScore, int twoStarScore, int threeStarScore)
        {
            OnScoreTargetsConfigured?.Invoke(oneStarScore, twoStarScore, threeStarScore);
        }

        public static void RaiseStarAwarded(int starIndex)
        {
            OnStarAwarded?.Invoke(starIndex);
        }

        public static void RaiseMovesChanged(byte remainingMoves)
        {
            OnMovesChanged?.Invoke(remainingMoves);
        }

        public static void RaiseTimeChanged(int remainingSeconds)
        {
            OnTimeChanged?.Invoke(remainingSeconds);
        }

        public static void RaiseLowMovesWarning(byte remainingMoves)
        {
            OnLowMovesWarning?.Invoke(remainingMoves);
        }

        public static void RaiseLowMovesWarningEnded()
        {
            OnLowMovesWarningEnded?.Invoke();
        }

        public static void RaiseLevelLimitReached()
        {
            OnLevelLimitReached?.Invoke();
        }

        public static void RaiseGoalsInitialized(byte goalCount)
        {
            OnGoalsInitialized?.Invoke(goalCount);
        }

        public static void RaiseGoalUpdated(byte goalIndex, TileType targetType, byte currentCount, byte targetCount)
        {
            OnGoalUpdated?.Invoke(goalIndex, targetType, currentCount, targetCount);
        }

        public static void RaiseAllGoalsCompleted()
        {
            OnAllGoalsCompleted?.Invoke();
        }

        public static void RaiseWinFinaleStarted()
        {
            OnWinFinaleStarted?.Invoke();
        }

        public static void RaiseFinaleSkipRequested()
        {
            OnFinaleSkipRequested?.Invoke();
        }

        public static void RaiseLevelWon(int earnedStars)
        {
            OnLevelWon?.Invoke(earnedStars);
        }

        public static void RaiseContinueWithMoves(byte extraMoves)
        {
            OnContinueWithMoves?.Invoke(extraMoves);
        }

        /// <summary>
        /// Enter Play Mode runs without a domain reload, so static subscribers would survive into the next
        /// session and fire on destroyed objects. Clearing them before the first scene load prevents that.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            OnScoreChanged = null;
            OnScoreTargetsConfigured = null;
            OnStarAwarded = null;
            OnMovesChanged = null;
            OnTimeChanged = null;
            OnLowMovesWarning = null;
            OnLowMovesWarningEnded = null;
            OnLevelLimitReached = null;
            OnGoalsInitialized = null;
            OnGoalUpdated = null;
            OnAllGoalsCompleted = null;
            OnWinFinaleStarted = null;
            OnFinaleSkipRequested = null;
            OnLevelWon = null;
            OnContinueWithMoves = null;
        }
    }
}
