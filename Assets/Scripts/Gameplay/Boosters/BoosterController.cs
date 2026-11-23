using System;
using Core.Data.Boosters;
using Core.Data.Obstacles;
using Core.Data.Tiles;
using Gameplay.Animation;
using Gameplay.Board;
using Gameplay.Input;
using Gameplay.Matching;
using Gameplay.States;
using Signals;

namespace Gameplay.Boosters
{
    /// <summary>
    /// Turns board taps and swipes into booster actions. Dependencies are rebuilt for every level, so it is bound once per level
    /// while the signal subscriptions follow the owner's enable state.
    /// </summary>
    public sealed class BoosterController
    {
        private const float OBSTACLE_SHAKE_DURATION = 0.25f;

        private readonly BoosterService _boosterService;
        private readonly Action<BoosterType> _onBoosterButtonClicked;
        private readonly Action<GridCoord> _onCellTapped;
        private readonly Action<GridCoord, GridCoord> _onTileSwiped;
        private BoardData _board;
        private BoardPresentation _presentation;
        private LevelServices _services;
        private BoardStates _states;
        private BoardInputHandler _input;

        public BoosterController(BoosterService boosterService)
        {
            _boosterService = boosterService;
            _onBoosterButtonClicked = HandleBoosterButtonClicked;
            _onCellTapped = HandleCellTapped;
            _onTileSwiped = HandleTileSwiped;
        }

        public void Bind(BoardData board, BoardPresentation presentation, LevelServices services, BoardStates states, BoardInputHandler input)
        {
            _board = board;
            _presentation = presentation;
            _services = services;
            _states = states;
            _input = input;
        }

        public void Subscribe()
        {
            EconomySignals.OnBoosterButtonClicked += _onBoosterButtonClicked;
            BoardSignals.OnCellTapped += _onCellTapped;
            BoardSignals.OnTileSwiped += _onTileSwiped;
        }

        public void Unsubscribe()
        {
            EconomySignals.OnBoosterButtonClicked -= _onBoosterButtonClicked;
            BoardSignals.OnCellTapped -= _onCellTapped;
            BoardSignals.OnTileSwiped -= _onTileSwiped;
        }

        private void HandleBoosterButtonClicked(BoosterType type)
        {
            if (_board == null || _states.Machine.CurrentState != _states.Idle)
            {
                return;
            }

            if (type == BoosterType.Shuffle)
            {
                if (_boosterService.GetCount(BoosterType.Shuffle) <= 0)
                {
                    EconomySignals.RaiseBoosterPurchaseRequested(type);
                    return;
                }

                if (_boosterService.TryConsumeBooster(BoosterType.Shuffle))
                {
                    _states.Machine.ChangeState(_states.Reshuffle);
                }
                return;
            }

            _boosterService.SelectBooster(type);
        }

        private void HandleCellTapped(GridCoord coord)
        {
            if (_board == null || _states.Machine.CurrentState != _states.Idle)
            {
                return;
            }

            BoardModel model = _board.Model;
            if (!model.IsValidCoord(coord))
            {
                return;
            }

            ref GridCell cell = ref model.GetCell(coord);
            if (!cell.IsPlayable || cell.Type == TileType.None)
            {
                return;
            }

            if (_boosterService.ActiveBooster == BoosterType.Hammer)
            {
                if (cell.Type == TileType.Ingredient)
                {
                    return;
                }

                if (_boosterService.TryConsumeBooster(BoosterType.Hammer))
                {
                    ApplyHammerToCell(coord);
                }
                return;
            }

            if (TileTypeLibrary.IsSpecialTile(cell.Type))
            {
                _input.SetInputEnabled(false);
                _board.MatchBuffer.Clear();
                _board.MatchBuffer.Add(coord);
                _states.Machine.ChangeState(_states.Match);
            }
        }

        private void ApplyHammerToCell(GridCoord coord)
        {
            BoardModel model = _board.Model;
            ref GridCell cell = ref model.GetCell(coord);
            _input.SetInputEnabled(false);
            EconomySignals.RaiseBoosterUsed(BoosterType.Hammer);

            int index = model.ToIndex(coord);

            if (cell.IsLocked)
            {
                if (_services.ObstacleService.DamageCell(model, coord))
                {
                    ClearCell(coord, index, ref cell);
                }
                else if (_board.Views[index] != null)
                {
                    ObstacleDefinition definition = _services.ObstacleDatabase.GetDefinitionByTileType(cell.Type);
                    _board.Views[index].SetSprite(definition.GetSpriteForDurability(cell.Durability));
                    _presentation.Animator.QueueShake(_board.Views[index], BoardAnimator.OBSTACLE_SHAKE_AMPLITUDE, OBSTACLE_SHAKE_DURATION);
                }
            }
            else
            {
                ClearCell(coord, index, ref cell);
            }

            _states.Machine.ChangeState(_states.Fall);
        }

        private void ClearCell(GridCoord coord, int index, ref GridCell cell)
        {
            if (_board.Views[index] != null)
            {
                _presentation.GemPool.Release(_board.Views[index]);
                _board.Views[index] = null;
            }

            _services.Goal.OnTileCleared(cell.Type, coord);
            _services.Score.AddScoreForClearedTiles(1);
            BoardSignals.RaiseTileMatched(coord);
            cell.Type = TileType.None;
        }

        /// <summary>
        /// The glove is consumed before SwapState runs, so a swap that SwapState would reject
        /// (obstacle or inactive cell) or that changes nothing (same type) must be filtered here,
        /// otherwise the player loses a booster for no effect.
        /// </summary>
        private bool CanGloveSwap(GridCoord coordA, GridCoord coordB)
        {
            ref GridCell cellA = ref _board.Model.GetCell(coordA);
            ref GridCell cellB = ref _board.Model.GetCell(coordB);

            if (!cellA.IsPlayable || cellA.IsLocked || !cellB.IsPlayable || cellB.IsLocked)
            {
                return false;
            }

            return cellA.Type != cellB.Type;
        }

        private void HandleTileSwiped(GridCoord coordA, GridCoord coordB)
        {
            if (_board == null)
            {
                return;
            }

            BoardModel model = _board.Model;
            if (!model.IsValidCoord(coordA) || !model.IsValidCoord(coordB))
            {
                return;
            }

            if (_board.Views[model.ToIndex(coordA)] == null || _board.Views[model.ToIndex(coordB)] == null)
            {
                return;
            }

            if (_boosterService.ActiveBooster == BoosterType.Glove)
            {
                if (!CanGloveSwap(coordA, coordB))
                {
                    return;
                }

                if (_boosterService.TryConsumeBooster(BoosterType.Glove))
                {
                    _states.Swap.SetFreeSwap(true);
                }
            }

            _states.Swap.SetSwapTargets(coordA, coordB);
            _states.Machine.ChangeState(_states.Swap);
        }
    }
}
