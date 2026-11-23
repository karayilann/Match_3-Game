using System;
using Core.Data.Tiles;
using Core.StateMachine;
using Gameplay.Board;
using Gameplay.Input;
using Gameplay.Matching;
using Signals;
using View;

namespace Gameplay.States
{
    public sealed class SwapState : IGameState
    {
        private const float SWAP_DURATION = 0.15f;

        private readonly BoardData _board;
        private readonly BoardPresentation _presentation;
        private readonly LevelServices _services;
        private readonly BoardStates _states;
        private readonly BoardInputHandler _input;
        private readonly Action _onSwapComplete;
        private readonly Action _onRevertComplete;

        private GridCoord _coordA;
        private GridCoord _coordB;
        private bool _isFreeSwap;

        public SwapState(BoardData board, BoardPresentation presentation, LevelServices services, BoardStates states, BoardInputHandler input)
        {
            _board = board;
            _presentation = presentation;
            _services = services;
            _states = states;
            _input = input;
            _onSwapComplete = OnSwapAnimationComplete;
            _onRevertComplete = OnRevertAnimationComplete;
        }

        public void SetSwapTargets(GridCoord coordA, GridCoord coordB)
        {
            _coordA = coordA;
            _coordB = coordB;
        }

        public void SetFreeSwap(bool isFree)
        {
            _isFreeSwap = isFree;
        }

        public void Enter()
        {
            if ((!_isFreeSwap && !_services.Limit.HasRemainingLimit) || IsInvalidSwapTarget(_coordA) || IsInvalidSwapTarget(_coordB))
            {
                _isFreeSwap = false;
                _states.Machine.ChangeState(_states.Idle);
                return;
            }

            _input.SetInputEnabled(false);
            AnimateSwap(_onSwapComplete);
        }

        public void Execute() { }

        public void Exit() { }

        private bool IsInvalidSwapTarget(GridCoord coord)
        {
            ref GridCell cell = ref _board.Model.GetCell(coord);
            return !cell.IsPlayable || cell.IsLocked;
        }

        private void OnSwapAnimationComplete()
        {
            bool isFree = _isFreeSwap;
            _isFreeSwap = false;

            ref GridCell cellA = ref _board.Model.GetCell(_coordA);
            ref GridCell cellB = ref _board.Model.GetCell(_coordB);
            TileType typeA = cellA.Type;
            TileType typeB = cellB.Type;

            if (SpecialTileService.TryExecuteCombo(_board.Model, _coordA, _coordB, typeA, typeB, _board.MatchBuffer))
            {
                bool hasColorBomb = typeA == TileType.ColorBomb || typeB == TileType.ColorBomb;
                GridCoord comboOrigin = typeA == TileType.ColorBomb ? _coordA : _coordB;
                SyncSpecialSprites();
                _states.Match.NotifyComboExecuted(comboOrigin, hasColorBomb);
                BoardSignals.RaiseSpecialTileDetonated(typeA);
                if (!isFree)
                {
                    _services.Limit.ConsumeMove();
                }
                _states.Machine.ChangeState(_states.Match);
                return;
            }

            bool hasMatch = _board.MatchDetector.TryFindMatches(_board.Model, _board.MatchBuffer, _states.Match.CreationsBuffer, _coordB);

            if (hasMatch)
            {
                if (!isFree)
                {
                    _services.Limit.ConsumeMove();
                }
                _states.Machine.ChangeState(_states.Match);
                return;
            }

            if (isFree)
            {
                _states.Machine.ChangeState(_states.Idle);
                return;
            }

            AnimateSwap(_onRevertComplete);
        }

        /// <summary>
        /// Color bomb combos retype tiles in the model, so their sprites must follow before the clear animation.
        /// </summary>
        private void SyncSpecialSprites()
        {
            for (int i = 0; i < _board.MatchBuffer.Count; i++)
            {
                GridCoord coord = _board.MatchBuffer[i];
                TileType type = _board.Model.GetCell(coord).Type;
                if (!TileTypeLibrary.IsSpecialTile(type))
                {
                    continue;
                }

                int index = _board.Model.ToIndex(coord);
                GemView view = _board.Views[index];
                if (view != null)
                {
                    view.SetSprite(_presentation.SpriteDatabase.GetSprite(type));
                }
            }
        }

        private void OnRevertAnimationComplete()
        {
            _states.Machine.ChangeState(_states.Idle);
        }

        private void AnimateSwap(Action onComplete)
        {
            SwapTiles();

            _presentation.Animator.StartBatch(onComplete);

            int indexA = _board.Model.ToIndex(_coordA);
            int indexB = _board.Model.ToIndex(_coordB);

            _presentation.Animator.QueueMove(_board.Views[indexA], _board.Converter.ToWorldPosition(_coordA), SWAP_DURATION, Gameplay.Animation.AnimationEase.QuadOut);
            _presentation.Animator.QueueMove(_board.Views[indexB], _board.Converter.ToWorldPosition(_coordB), SWAP_DURATION, Gameplay.Animation.AnimationEase.QuadOut);
        }

        private void SwapTiles()
        {
            ref GridCell cellA = ref _board.Model.GetCell(_coordA);
            ref GridCell cellB = ref _board.Model.GetCell(_coordB);

            (cellA.Type, cellB.Type) = (cellB.Type, cellA.Type);

            int indexA = _board.Model.ToIndex(_coordA);
            int indexB = _board.Model.ToIndex(_coordB);

            (_board.Views[indexA], _board.Views[indexB]) = (_board.Views[indexB], _board.Views[indexA]);
        }
    }
}
