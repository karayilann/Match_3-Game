using System;
using System.Collections.Generic;
using Core.Data.Obstacles;
using Core.Data.Tiles;
using Core.StateMachine;
using Gameplay.Animation;
using Gameplay.Board;
using Gameplay.Flow;
using Gameplay.Ingredients;
using Gameplay.Matching;
using Signals;
using UnityEngine;
using View;

namespace Gameplay.States
{
    public sealed class MatchState : IGameState
    {
        private const float PULSE_DURATION = SpecialTileChoreographer.CLEAR_DURATION;
        private const float SPAWNED_TILE_PULSE_SCALE = 1.25f;
        private const float CREATED_SPECIAL_PULSE_SCALE = 1.35f;
        private const int CREATIONS_CAPACITY = 8;

        private readonly BoardData _board;
        private readonly BoardPresentation _presentation;
        private readonly LevelServices _services;
        private readonly BoardStates _states;
        private readonly IPossibleMoveDetector _possibleMoveDetector;
        private readonly SpecialTileChoreographer _choreographer;
        private readonly WinFinaleController _finale;
        private readonly List<GridCoord> _combinedClearBuffer;
        private readonly List<SpecialTileCreation> _creationsBuffer;
        private readonly List<GridCoord> _chainBuffer;
        private readonly List<GridCoord> _ingredientBuffer;
        private readonly bool[] _isInMatchBuffer;
        private readonly bool[] _isCreationCell;
        private readonly Action _onMatchComplete;
        private readonly Action _onCelebrationComplete;
        private readonly Action _onFinaleSpawnComplete;
        private byte _cascadeStep;
        private bool _isComboSwap;
        private bool _isColorBombCombo;
        private GridCoord _comboOrigin;
        public List<SpecialTileCreation> CreationsBuffer => _creationsBuffer;

        public MatchState(
            BoardData board,
            BoardPresentation presentation,
            LevelServices services,
            BoardStates states,
            IPossibleMoveDetector possibleMoveDetector,
            SpecialTileChoreographer choreographer,
            WinFinaleController finale)
        {
            _board = board;
            _presentation = presentation;
            _services = services;
            _states = states;
            _possibleMoveDetector = possibleMoveDetector;
            _choreographer = choreographer;
            _finale = finale;
            int capacity = board.Model.Width * board.Model.Height;
            _combinedClearBuffer = new List<GridCoord>(capacity);
            _creationsBuffer = new List<SpecialTileCreation>(CREATIONS_CAPACITY);
            _chainBuffer = new List<GridCoord>(capacity);
            _ingredientBuffer = new List<GridCoord>(board.Model.Width);
            _isInMatchBuffer = new bool[capacity];
            _isCreationCell = new bool[capacity];
            _onMatchComplete = OnMatchAnimationComplete;
            _onCelebrationComplete = Enter;
            _onFinaleSpawnComplete = OnFinaleSpawnComplete;
        }

        public void NotifyComboExecuted(GridCoord origin, bool isColorBombCombo)
        {
            _isComboSwap = true;
            _comboOrigin = origin;
            _isColorBombCombo = isColorBombCombo;
        }

        public void Enter()
        {
            if (_finale.IsPending)
            {
                _finale.Advance(_onCelebrationComplete, _onFinaleSpawnComplete);
                return;
            }

            ResolveBoard();
        }

        public void Execute() { }

        public void Exit() { }

        /// <summary>
        /// Split from Enter so the win finale can feed its own MatchBuffer without
        /// re-entering the finale branch, which would burn all remaining moves in one frame.
        /// The detonation itself runs through ResolveChainReactions, so the special is not executed twice.
        /// </summary>
        private void ResolveBoard()
        {
            if (_board.MatchBuffer.Count == 0)
            {
                _board.MatchDetector.TryFindMatches(_board.Model, _board.MatchBuffer, _creationsBuffer, default);
            }

            IngredientService.CollectExitCoords(_board.Model, _ingredientBuffer);

            if (_board.MatchBuffer.Count == 0 && _ingredientBuffer.Count == 0)
            {
                OnBoardSettled();
                return;
            }

            ResolveChainReactions();

            List<GridCoord> destroyedBoxes = _services.ObstacleService.ResolveDamage(_board.Model, _board.MatchBuffer);
            List<GridCoord> damagedBoxes = _services.ObstacleService.DamagedBuffer;

            _combinedClearBuffer.Clear();
            _combinedClearBuffer.AddRange(_board.MatchBuffer);

            ApplySpecialTileCreations();

            for (int i = 0; i < _ingredientBuffer.Count; i++)
            {
                _combinedClearBuffer.Add(_ingredientBuffer[i]);
            }

            for (int i = 0; i < destroyedBoxes.Count; i++)
            {
                ResolveDestroyedObstacle(destroyedBoxes[i]);
            }

            _presentation.Animator.StartBatch(_onMatchComplete);

            for (int i = 0; i < damagedBoxes.Count; i++)
            {
                GridCoord damagedCoord = damagedBoxes[i];
                ref GridCell damagedCell = ref _board.Model.GetCell(damagedCoord);
                ObstacleDefinition definition = _services.ObstacleDatabase.GetDefinitionByTileType(damagedCell.Type);
                GemView damagedView = _board.Views[_board.Model.ToIndex(damagedCoord)];

                damagedView.SetSprite(definition.GetSpriteForDurability(damagedCell.Durability));
                _presentation.Animator.QueueShake(damagedView, BoardAnimator.OBSTACLE_SHAKE_AMPLITUDE, PULSE_DURATION);
            }

            for (int i = 0; i < _combinedClearBuffer.Count; i++)
            {
                _choreographer.QueueClear(_combinedClearBuffer[i]);
            }
        }

        private void OnBoardSettled()
        {
            BoardSignals.RaiseBoardSettled();
            _cascadeStep = 0;

            if (_services.Goal.IsAllCompleted)
            {
                _finale.NotifyLevelWonOnce();
                return;
            }

            if (!_services.Limit.HasRemainingLimit)
            {
                _services.Limit.NotifyIfLimitReached();
                return;
            }

            bool hasMove = _possibleMoveDetector.HasPossibleMove(_board.Model);
            _states.Machine.ChangeState(hasMove ? _states.Idle : _states.Reshuffle);
        }

        private void ResolveDestroyedObstacle(GridCoord coord)
        {
            ref GridCell cell = ref _board.Model.GetCell(coord);
            ObstacleDefinition def = _services.ObstacleDatabase.GetDefinitionByTileType(cell.Type);

            if (!def.SpawnsTileOnDestroy)
            {
                _combinedClearBuffer.Add(coord);

                if (def.Type == ObstacleType.Rock)
                {
                    _choreographer.SpawnRockDebris(coord);
                }
                return;
            }

            TileType oldType = cell.Type;
            TileType newType = TileTypeLibrary.PLAYABLE_TYPES[UnityEngine.Random.Range(0, _board.ActiveColorCount)];
            cell.Type = newType;
            cell.Durability = 0;

            GemView view = _board.Views[_board.Model.ToIndex(coord)];
            view.SetSprite(_presentation.SpriteDatabase.GetSprite(newType));
            _presentation.Animator.QueuePulse(view, SPAWNED_TILE_PULSE_SCALE, PULSE_DURATION);

            _services.Goal.OnTileCleared(oldType, coord);
            BoardSignals.RaiseTileMatched(coord);
            _choreographer.SpawnParticle(coord);
        }

        /// <summary>
        /// The buffer grows while it is walked, so a special hit by an earlier blast is
        /// detonated in turn. Each coord is queued once (tracked per cell), so every special fires at most once.
        /// Ingredients are stripped at the end: they may only leave through the bottom row.
        /// </summary>
        private void ResolveChainReactions()
        {
            List<GridCoord> buffer = _board.MatchBuffer;
            BoardModel model = _board.Model;
            _choreographer.BeginResolve();

            if (_isComboSwap)
            {
                _isComboSwap = false;
                _choreographer.ScheduleCombo(_comboOrigin, _isColorBombCombo, buffer);
            }

            for (int i = 0; i < buffer.Count; i++)
            {
                _isInMatchBuffer[model.ToIndex(buffer[i])] = true;
            }

            for (int i = 0; i < buffer.Count; i++)
            {
                GridCoord coord = buffer[i];
                ref GridCell cell = ref model.GetCell(coord);

                if (!TileTypeLibrary.IsSpecialTile(cell.Type))
                {
                    continue;
                }

                TileType specialType = cell.Type;
                _chainBuffer.Clear();
                SpecialTileService.ExecuteSingle(model, coord, specialType, _chainBuffer);
                _choreographer.ScheduleBlast(coord, specialType, _chainBuffer);
                BoardSignals.RaiseSpecialTileDetonated(specialType);

                for (int j = 0; j < _chainBuffer.Count; j++)
                {
                    GridCoord extra = _chainBuffer[j];
                    int extraIndex = model.ToIndex(extra);
                    if (!_isInMatchBuffer[extraIndex])
                    {
                        _isInMatchBuffer[extraIndex] = true;
                        buffer.Add(extra);
                    }
                }
            }

            int write = 0;
            for (int i = 0; i < buffer.Count; i++)
            {
                GridCoord coord = buffer[i];
                _isInMatchBuffer[model.ToIndex(coord)] = false;

                if (model.GetCell(coord).Type != TileType.Ingredient)
                {
                    buffer[write++] = coord;
                }
            }

            buffer.RemoveRange(write, buffer.Count - write);
        }

        private void ApplySpecialTileCreations()
        {
            if (_creationsBuffer.Count == 0)
            {
                return;
            }

            BoardModel model = _board.Model;

            for (int i = 0; i < _creationsBuffer.Count; i++)
            {
                SpecialTileCreation creation = _creationsBuffer[i];
                ref GridCell cell = ref model.GetCell(creation.Coord);
                cell.Type = creation.CreatedType;
                cell.Durability = 0;
                _services.Goal.OnTileCreated(creation.CreatedType, creation.Coord);

                int index = model.ToIndex(creation.Coord);
                GemView view = _board.Views[index];
                if (view != null)
                {
                    view.SetSprite(_presentation.SpriteDatabase.GetSprite(creation.CreatedType));
                    _presentation.Animator.QueuePulse(view, CREATED_SPECIAL_PULSE_SCALE, PULSE_DURATION);
                }

                _isCreationCell[index] = true;
            }

            int write = 0;
            for (int i = 0; i < _combinedClearBuffer.Count; i++)
            {
                GridCoord coord = _combinedClearBuffer[i];
                if (!_isCreationCell[model.ToIndex(coord)])
                {
                    _combinedClearBuffer[write++] = coord;
                }
            }

            _combinedClearBuffer.RemoveRange(write, _combinedClearBuffer.Count - write);

            for (int i = 0; i < _creationsBuffer.Count; i++)
            {
                _isCreationCell[model.ToIndex(_creationsBuffer[i].Coord)] = false;
            }

            _creationsBuffer.Clear();
        }

        private void OnFinaleSpawnComplete()
        {
            _board.MatchBuffer.Clear();
            _finale.CollectSpecialTiles(_board.MatchBuffer);
            ResolveBoard();
        }

        private void OnMatchAnimationComplete()
        {
            _choreographer.ReleaseProjectiles();
            ClearMatchedTiles();
            _board.MatchBuffer.Clear();
            _states.Machine.ChangeState(_states.Fall);
        }

        private void ClearMatchedTiles()
        {
            int clearedCount = _combinedClearBuffer.Count;

            for (int i = 0; i < clearedCount; i++)
            {
                GridCoord coord = _combinedClearBuffer[i];
                int index = _board.Model.ToIndex(coord);

                if (_board.Views[index] != null)
                {
                    _presentation.GemPool.Release(_board.Views[index]);
                    _board.Views[index] = null;
                }

                ref GridCell cell = ref _board.Model.GetCell(coord);
                TileType clearedType = cell.Type;
                cell.Type = TileType.None;
                cell.Durability = 0;

                _services.Goal.OnTileCleared(clearedType, coord);
                BoardSignals.RaiseTileMatched(coord);
            }

            if (clearedCount > 0)
            {
                _cascadeStep++;
                Vector3 center = Vector3.zero;
                for (int i = 0; i < clearedCount; i++)
                {
                    center += _board.Converter.ToWorldPosition(_combinedClearBuffer[i]);
                }

                center *= 1f / clearedCount;
                BoardSignals.RaiseMatchScored(center, clearedCount * _services.Score.PointsPerTile, _cascadeStep, (byte)Mathf.Min(clearedCount, 255));
            }

            _services.Score.AddScoreForClearedTiles(clearedCount);
        }
    }
}
