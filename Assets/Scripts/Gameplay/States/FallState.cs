using System;
using Core.Data.Tiles;
using Core.StateMachine;
using Gameplay.Board;
using UnityEngine;
using View;

namespace Gameplay.States
{
    public sealed class FallState : IGameState
    {
        private const float FALL_DURATION = 0.22f;
        private const int MAX_GRAVITY_PASSES = 32;
        private const float POSITION_EPSILON_SQR = 0.0001f;

        private readonly BoardData _board;
        private readonly BoardPresentation _presentation;
        private readonly BoardStates _states;
        private readonly Action _onFallComplete;

        public FallState(BoardData board, BoardPresentation presentation, BoardStates states)
        {
            _board = board;
            _presentation = presentation;
            _states = states;
            _onFallComplete = OnFallAnimationComplete;
        }

        public void Enter()
        {
            _presentation.Animator.StartBatch(_onFallComplete);
            SimulateGravity();
            QueueMovementAnimations();
        }

        public void Execute() { }

        public void Exit() { }

        private void OnFallAnimationComplete()
        {
            _states.Machine.ChangeState(_states.Fill);
        }

        /// <summary>
        /// Executes iterative straight and diagonal gravity passes until the board settles into a steady state.
        /// </summary>
        private void SimulateGravity()
        {
            byte width = _board.Model.Width;
            byte height = _board.Model.Height;

            for (int pass = 0; pass < MAX_GRAVITY_PASSES; pass++)
            {
                bool movedStraight = CollapseStraightColumns(width, height);
                bool movedDiagonal = SlideDiagonalTiles(width, height);

                if (!movedStraight && !movedDiagonal)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Collapses playable unlocked tiles straight down within each column segment.
        /// </summary>
        private bool CollapseStraightColumns(byte width, byte height)
        {
            bool anyMoved = false;

            for (byte x = 0; x < width; x++)
            {
                byte writeY = 0;

                for (byte y = 0; y < height; y++)
                {
                    GridCoord readCoord = new GridCoord(x, y);
                    ref GridCell readCell = ref _board.Model.GetCell(readCoord);

                    if (readCell.IsLocked || !readCell.IsPlayable)
                    {
                        writeY = (byte)(y + 1);
                        continue;
                    }

                    if (readCell.Type == TileType.None)
                    {
                        continue;
                    }

                    if (writeY != y)
                    {
                        GridCoord writeCoord = new GridCoord(x, writeY);
                        ref GridCell writeCell = ref _board.Model.GetCell(writeCoord);

                        writeCell.Type = readCell.Type;
                        readCell.Type = TileType.None;

                        int writeIndex = _board.Model.ToIndex(writeCoord);
                        int readIndex = _board.Model.ToIndex(readCoord);

                        _board.Views[writeIndex] = _board.Views[readIndex];
                        _board.Views[readIndex] = null;

                        anyMoved = true;
                    }

                    writeY++;
                }
            }

            return anyMoved;
        }

        /// <summary>
        /// Scans bottom-up for empty cells blocked from straight fall and pulls eligible diagonal candidate tiles with right priority.
        /// </summary>
        private bool SlideDiagonalTiles(byte width, byte height)
        {
            bool anyMoved = false;

            for (byte y = 0; y < height - 1; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    GridCoord emptyCoord = new GridCoord(x, y);
                    ref GridCell emptyCell = ref _board.Model.GetCell(emptyCoord);

                    if (!emptyCell.IsPlayable || emptyCell.IsLocked || emptyCell.Type != TileType.None)
                    {
                        continue;
                    }

                    if (!IsStraightFallBlocked(x, y, height))
                    {
                        continue;
                    }

                    byte nextY = (byte)(y + 1);

                    if (x < width - 1 && TrySlideFrom(emptyCoord, new GridCoord((byte)(x + 1), nextY)))
                    {
                        anyMoved = true;
                        continue;
                    }

                    if (x > 0 && TrySlideFrom(emptyCoord, new GridCoord((byte)(x - 1), nextY)))
                    {
                        anyMoved = true;
                    }
                }
            }

            return anyMoved;
        }

        /// <summary>
        /// Determines whether straight fall into (x, y) is blocked by an obstacle or non-playable cell.
        /// </summary>
        private bool IsStraightFallBlocked(byte x, byte y, byte height)
        {
            for (byte checkY = (byte)(y + 1); checkY < height; checkY++)
            {
                GridCoord checkCoord = new GridCoord(x, checkY);
                ref GridCell checkCell = ref _board.Model.GetCell(checkCoord);

                if (checkCell.IsLocked || !checkCell.IsPlayable)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Evaluates whether candidateCoord can slide into targetCoord per diagonal fall rules.
        /// </summary>
        private bool TrySlideFrom(GridCoord targetCoord, GridCoord candidateCoord)
        {
            ref GridCell candidateCell = ref _board.Model.GetCell(candidateCoord);

            if (!candidateCell.IsPlayable || candidateCell.IsLocked || candidateCell.Type == TileType.None)
            {
                return false;
            }

            GridCoord candidateBelow = new GridCoord(candidateCoord.X, targetCoord.Y);
            ref GridCell belowCell = ref _board.Model.GetCell(candidateBelow);

            bool belowIsBlocked = !belowCell.IsPlayable || belowCell.IsLocked || belowCell.Type != TileType.None;
            if (!belowIsBlocked)
            {
                return false;
            }

            ref GridCell cornerCell = ref _board.Model.GetCell(new GridCoord(targetCoord.X, candidateCoord.Y));
            if (cornerCell.IsLocked && belowCell.IsLocked)
            {
                return false;
            }

            ref GridCell targetCell = ref _board.Model.GetCell(targetCoord);
            targetCell.Type = candidateCell.Type;
            candidateCell.Type = TileType.None;

            int targetIndex = _board.Model.ToIndex(targetCoord);
            int candidateIndex = _board.Model.ToIndex(candidateCoord);

            _board.Views[targetIndex] = _board.Views[candidateIndex];
            _board.Views[candidateIndex] = null;

            return true;
        }

        /// <summary>
        /// Compares current view world positions with target grid positions and queues smooth animations for moved tiles.
        /// </summary>
        private void QueueMovementAnimations()
        {
            byte width = _board.Model.Width;
            byte height = _board.Model.Height;

            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    int index = _board.Model.ToIndex(coord);
                    GemView view = _board.Views[index];

                    if (view == null)
                    {
                        continue;
                    }

                    Vector3 targetPosition = _board.Converter.ToWorldPosition(coord);
                    Vector3 currentPosition = view.GetPosition();

                    if ((currentPosition - targetPosition).sqrMagnitude > POSITION_EPSILON_SQR)
                    {
                        _presentation.Animator.QueueMove(view, targetPosition, FALL_DURATION);
                    }
                }
            }
        }
    }
}
