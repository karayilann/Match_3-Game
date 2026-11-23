using System;
using UnityEngine;
using Core.Data;
using Core.Data.Tiles;
using Core.StateMachine;
using Gameplay.Board;
using Gameplay.Ingredients;
using View;

namespace Gameplay.States
{
    public sealed class FillState : IGameState
    {
        private const float BASE_FILL_DURATION = 0.22f;
        private const float DURATION_PER_EXTRA_CELL = 0.03f;
        private const float INGREDIENT_SPAWN_CHANCE = 0.15f;

        private readonly BoardData _board;
        private readonly BoardPresentation _presentation;
        private readonly LevelServices _services;
        private readonly BoardStates _states;
        private readonly Action _onFillComplete;
        private int _spawnedCount;

        public FillState(BoardData board, BoardPresentation presentation, LevelServices services, BoardStates states)
        {
            _board = board;
            _presentation = presentation;
            _services = services;
            _states = states;
            _onFillComplete = OnFillAnimationComplete;
        }

        public void Enter()
        {
            _spawnedCount = 0;
            _presentation.Animator.StartBatch(_onFillComplete);
            FillEmptyCells();
        }

        public void Execute() { }

        public void Exit() { }

        /// <summary>
        /// Freshly spawned tiles in neighbouring columns can become diagonal sources for cells shadowed by obstacles,
        /// so gravity must run again. Requiring a spawn this pass guarantees the Fall-Fill loop terminates.
        /// </summary>
        private void OnFillAnimationComplete()
        {
            if (_spawnedCount > 0 && HasEmptyPlayableCell())
            {
                _states.Machine.ChangeState(_states.Fall);
                return;
            }

            _states.Machine.ChangeState(_states.Match);
        }

        private bool HasEmptyPlayableCell()
        {
            byte width = _board.Model.Width;
            byte height = _board.Model.Height;

            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    ref GridCell cell = ref _board.Model.GetCell(new GridCoord(x, y));
                    if (cell.IsPlayable && !cell.IsLocked && cell.Type == TileType.None)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Stacks spawned tiles sequentially above the top row per column so multiple empty cells in the same column fall with staggered vertical spacing.
        /// </summary>
        private void FillEmptyCells()
        {
            byte width = _board.Model.Width;
            byte height = _board.Model.Height;
            byte activeColorCount = _board.ActiveColorCount;
            int missingIngredients = IngredientService.GetMissing(_board.Model, _services.Goal);

            for (byte x = 0; x < width; x++)
            {
                byte columnSpawnOffset = 0;

                for (byte y = 0; y < height; y++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    ref GridCell cell = ref _board.Model.GetCell(coord);

                    if (!cell.IsPlayable || cell.Type != TileType.None || HasObstacleAbove(x, y, height))
                    {
                        continue;
                    }

                    TileType type;
                    if (missingIngredients > 0 && UnityEngine.Random.value < INGREDIENT_SPAWN_CHANCE)
                    {
                        type = TileType.Ingredient;
                        missingIngredients--;
                    }
                    else
                    {
                        type = TileTypeLibrary.PLAYABLE_TYPES[UnityEngine.Random.Range(0, activeColorCount)];
                    }

                    cell.Type = type;

                    GridCoord spawnCoord = new GridCoord(x, (byte)(height + columnSpawnOffset));
                    Vector3 spawnPosition = _board.Converter.ToWorldPosition(spawnCoord);
                    Vector3 targetPosition = _board.Converter.ToWorldPosition(coord);

                    GemView view = _presentation.GemPool.Get();
                    view.SetPosition(spawnPosition);
                    view.SetSprite(_presentation.SpriteDatabase.GetSprite(type));

                    int index = _board.Model.ToIndex(coord);
                    _board.Views[index] = view;

                    float duration = BASE_FILL_DURATION + columnSpawnOffset * DURATION_PER_EXTRA_CELL;
                    _presentation.Animator.QueueMove(view, targetPosition, duration);

                    columnSpawnOffset++;
                    _spawnedCount++;
                }
            }
        }

        private bool HasObstacleAbove(byte x, byte y, byte height)
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
    }
}
