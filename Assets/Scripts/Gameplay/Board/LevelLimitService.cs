using System.Runtime.CompilerServices;
using Core.Data.Level;
using Signals;
using UnityEngine;

namespace Gameplay.Board
{
    public sealed class LevelLimitService
    {
        private const byte LOW_MOVES_THRESHOLD = 5;
        private const float SECONDS_PER_EXTRA_MOVE = 3f;

        public readonly LevelLimitType LimitType;

        private byte _remainingMoves;
        private float _remainingTime;
        private int _lastReportedWholeSecond;
        private bool _isLimitReachedNotified;

        public byte RemainingMoves
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _remainingMoves;
        }

        public float RemainingTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _remainingTime;
        }

        public bool HasRemainingLimit
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => LimitType == LevelLimitType.Moves ? _remainingMoves > 0 : _remainingTime > 0f;
        }

        public LevelLimitService(LevelData levelData)
        {
            LimitType = levelData.LimitType;
            _remainingMoves = levelData.MoveLimit;
            _remainingTime = levelData.TimeLimitSeconds;
            _lastReportedWholeSecond = Mathf.CeilToInt(_remainingTime);
        }

        public void BroadcastInitialState()
        {
            if (LimitType == LevelLimitType.Moves)
            {
                LevelSignals.RaiseMovesChanged(_remainingMoves);
                if (_remainingMoves <= LOW_MOVES_THRESHOLD && _remainingMoves > 0)
                {
                    LevelSignals.RaiseLowMovesWarning(_remainingMoves);
                }
                return;
            }

            LevelSignals.RaiseTimeChanged(_lastReportedWholeSecond);
        }

        /// <summary>
        /// The win finale burns the remaining moves after the level is already won, so it passes
        /// suppressWarning to keep the red low-moves border from flashing over the celebration.
        /// </summary>
        public void ConsumeMove(bool suppressWarning = false)
        {
            if (LimitType != LevelLimitType.Moves || _remainingMoves == 0)
            {
                return;
            }

            _remainingMoves--;
            LevelSignals.RaiseMovesChanged(_remainingMoves);

            if (suppressWarning)
            {
                return;
            }

            if (_remainingMoves <= LOW_MOVES_THRESHOLD && _remainingMoves > 0)
            {
                LevelSignals.RaiseLowMovesWarning(_remainingMoves);
            }
            else if (_remainingMoves == 0)
            {
                LevelSignals.RaiseLowMovesWarningEnded();
            }
        }

        public void AddMoves(byte count)
        {
            _remainingMoves = (byte)Mathf.Min(_remainingMoves + count, byte.MaxValue);
            _isLimitReachedNotified = false;
            LevelSignals.RaiseMovesChanged(_remainingMoves);

            if (_remainingMoves <= LOW_MOVES_THRESHOLD && _remainingMoves > 0)
            {
                LevelSignals.RaiseLowMovesWarning(_remainingMoves);
            }
            else
            {
                LevelSignals.RaiseLowMovesWarningEnded();
            }
        }

        /// <summary>
        /// The continue offer is phrased in moves, so a timed level converts the same reward into seconds.
        /// </summary>
        public void ApplyContinueBonus(byte extraMoves)
        {
            if (LimitType == LevelLimitType.Moves)
            {
                AddMoves(extraMoves);
                return;
            }

            AddTime(extraMoves * SECONDS_PER_EXTRA_MOVE);
        }

        public void AddTime(float seconds)
        {
            _remainingTime += seconds;
            _isLimitReachedNotified = false;
            _lastReportedWholeSecond = Mathf.CeilToInt(_remainingTime);
            LevelSignals.RaiseTimeChanged(_lastReportedWholeSecond);
        }

        public void RestoreState(byte remainingMoves, float remainingTime)
        {
            _remainingMoves = remainingMoves;
            _remainingTime = remainingTime;
            _lastReportedWholeSecond = Mathf.CeilToInt(_remainingTime);
            _isLimitReachedNotified = false;
            BroadcastInitialState();
        }

        /// <summary>
        /// Emits OnTimeChanged only when the ceiling whole-second value changes so HUD text is not rebuilt every frame.
        /// Returns true only on the frame the time runs out, so the caller can lock input exactly once.
        /// </summary>
        public bool TickTime(float deltaTime)
        {
            if (LimitType != LevelLimitType.Time || _remainingTime <= 0f)
            {
                return false;
            }

            _remainingTime -= deltaTime;
            if (_remainingTime < 0f)
            {
                _remainingTime = 0f;
            }

            int currentWholeSecond = Mathf.CeilToInt(_remainingTime);
            if (currentWholeSecond != _lastReportedWholeSecond)
            {
                _lastReportedWholeSecond = currentWholeSecond;
                LevelSignals.RaiseTimeChanged(currentWholeSecond);
            }

            return _remainingTime <= 0f;
        }

        public void NotifyIfLimitReached()
        {
            if (_isLimitReachedNotified || HasRemainingLimit)
            {
                return;
            }

            _isLimitReachedNotified = true;
            LevelSignals.RaiseLowMovesWarningEnded();
            LevelSignals.RaiseLevelLimitReached();
        }
    }
}
