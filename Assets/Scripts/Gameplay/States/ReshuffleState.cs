using System;
using System.Collections.Generic;
using Core.Data.Tiles;
using Core.StateMachine;
using Gameplay.Board;
using Gameplay.Matching;
using Signals;
using UnityEngine;
using View;

namespace Gameplay.States
{
    public sealed class ReshuffleState : IGameState
    {
        private const byte MAX_SHUFFLE_ATTEMPTS = 20;
        private const byte MAX_REPAIR_STEPS = 64;
        private const float RESHUFFLE_SHAKE_AMPLITUDE = 0.14f;
        private const float RESHUFFLE_ANIM_DURATION = 0.35f;

        private readonly BoardData _board;
        private readonly BoardPresentation _presentation;
        private readonly BoardStates _states;
        private readonly IPossibleMoveDetector _possibleMoveDetector;
        private readonly List<TileType> _scratchTypes;
        private readonly List<GridCoord> _freeCoords;
        private readonly Action _onReshuffleComplete;

        public ReshuffleState(BoardData board, BoardPresentation presentation, BoardStates states, IPossibleMoveDetector possibleMoveDetector)
        {
            _board = board;
            _presentation = presentation;
            _states = states;
            _possibleMoveDetector = possibleMoveDetector;
            int capacity = board.Model.Width * board.Model.Height;
            _scratchTypes = new List<TileType>(capacity);
            _freeCoords = new List<GridCoord>(capacity);
            _onReshuffleComplete = OnReshuffleComplete;
        }

        public void Enter()
        {
            Reshuffle();
            BoardSignals.RaiseBoardReshuffled();

            _presentation.Animator.StartBatch(_onReshuffleComplete);
            QueueReshuffleShake();
        }

        public void Execute() { }

        public void Exit() { }

        /// <summary>
        /// A board that still holds a match or has no move is handed to MatchState, which clears the match
        /// or reshuffles again, so the player never receives a stuck or already-resolved board.
        /// </summary>
        private void OnReshuffleComplete()
        {
            bool isSettled = !_board.MatchDetector.TryFindMatches(_board.Model, _board.MatchBuffer)
                && _possibleMoveDetector.HasPossibleMove(_board.Model);

            if (isSettled || _states.Match == null)
            {
                _states.Machine.ChangeState(_states.Idle);
                return;
            }

            _states.Machine.ChangeState(_states.Match);
        }

        private void QueueReshuffleShake()
        {
            BoardModel model = _board.Model;
            byte width = model.Width;
            byte height = model.Height;

            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    ref GridCell cell = ref model.GetCell(coord);
                    if (!cell.IsPlayable || cell.IsLocked)
                    {
                        continue;
                    }

                    GemView view = _board.Views[model.ToIndex(coord)];
                    if (view != null)
                    {
                        _presentation.Animator.QueueShake(view, RESHUFFLE_SHAKE_AMPLITUDE, RESHUFFLE_ANIM_DURATION);
                    }
                }
            }
        }

        /// <summary>
        /// Random shuffles are capped, then a bounded repair swaps tiles out of any remaining match. With few
        /// colors a random shuffle almost never lands match-free, so the repair is what makes the result reliable.
        /// </summary>
        private void Reshuffle()
        {
            CollectFreeCoords();

            for (byte attempt = 0; attempt < MAX_SHUFFLE_ATTEMPTS; attempt++)
            {
                ShuffleTypesInPlace();

                if (IsSettled())
                {
                    ApplySprites();
                    return;
                }
            }

            RepairMatches();
            ApplySprites();
        }

        private bool IsSettled()
        {
            return !_board.MatchDetector.TryFindMatches(_board.Model, _board.MatchBuffer)
                && _possibleMoveDetector.HasPossibleMove(_board.Model);
        }

        private void RepairMatches()
        {
            BoardModel model = _board.Model;
            int freeCount = _freeCoords.Count;

            for (byte step = 0; step < MAX_REPAIR_STEPS && freeCount > 1; step++)
            {
                if (!_board.MatchDetector.TryFindMatches(model, _board.MatchBuffer))
                {
                    return;
                }

                GridCoord matched = _board.MatchBuffer[0];
                GridCoord other = _freeCoords[UnityEngine.Random.Range(0, freeCount)];

                ref GridCell matchedCell = ref model.GetCell(matched);
                ref GridCell otherCell = ref model.GetCell(other);
                (matchedCell.Type, otherCell.Type) = (otherCell.Type, matchedCell.Type);
            }
        }

        private void CollectFreeCoords()
        {
            BoardModel model = _board.Model;
            _freeCoords.Clear();

            for (byte y = 0; y < model.Height; y++)
            {
                for (byte x = 0; x < model.Width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    ref GridCell cell = ref model.GetCell(coord);

                    if (cell.IsPlayable && !cell.IsLocked && cell.Type != TileType.Ingredient)
                    {
                        _freeCoords.Add(coord);
                    }
                }
            }
        }

        private void ShuffleTypesInPlace()
        {
            BoardModel model = _board.Model;
            int count = _freeCoords.Count;

            _scratchTypes.Clear();

            for (int i = 0; i < count; i++)
            {
                _scratchTypes.Add(model.GetCell(_freeCoords[i]).Type);
            }

            for (int i = count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (_scratchTypes[i], _scratchTypes[swapIndex]) = (_scratchTypes[swapIndex], _scratchTypes[i]);
            }

            for (int i = 0; i < count; i++)
            {
                model.GetCell(_freeCoords[i]).Type = _scratchTypes[i];
            }
        }

        private void ApplySprites()
        {
            BoardModel model = _board.Model;

            for (int i = 0; i < _freeCoords.Count; i++)
            {
                GridCoord coord = _freeCoords[i];
                GemView view = _board.Views[model.ToIndex(coord)];

                if (view != null)
                {
                    view.SetSprite(_presentation.SpriteDatabase.GetSprite(model.GetCell(coord).Type));
                }
            }
        }
    }
}
