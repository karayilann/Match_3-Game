using System;
using System.Collections.Generic;
using Core.Data.Tiles;
using Gameplay.Board;
using Signals;
using UnityEngine;
using View;

namespace Gameplay.Flow
{
    /// <summary>
    /// Drives the end of a won level: a celebration pause, then every remaining move becomes a special tile
    /// that MatchState detonates through its normal resolve. MatchState stays the only owner of state changes,
    /// this class only decides what the finale shows and when to hand control back.
    /// </summary>
    public sealed class WinFinaleController
    {
        private const float CELEBRATION_DURATION = 1.6f;
        private const float SPAWN_DURATION = 0.6f;
        private const float SPAWN_PULSE_SCALE = 1.5f;
        private const int BONUS_TILES_PER_MOVE = 50;
        private const float ROCKET_CHANCE = 0.5f;
        private const int RANDOM_PICK_ATTEMPTS = 20;
        private const int MIN_STARS = 1;
        private const int MAX_STARS = 3;

        private readonly BoardData _board;
        private readonly BoardPresentation _presentation;
        private readonly LevelServices _services;
        private bool _isCelebrationShown;
        private bool _isLevelWonNotified;

        public WinFinaleController(BoardData board, BoardPresentation presentation, LevelServices services)
        {
            _board = board;
            _presentation = presentation;
            _services = services;
        }

        public bool IsPending => _services.Goal.IsAllCompleted
            && (_services.Limit.RemainingMoves > 0 || CollectSpecialTiles(null));

        /// <summary>
        /// The first call only plays the celebration and re-enters through onCelebrationComplete. Later calls place
        /// one special per remaining move and continue with onSpawnComplete once they are visible on the board.
        /// </summary>
        public void Advance(Action onCelebrationComplete, Action onSpawnComplete)
        {
            if (!_isCelebrationShown)
            {
                _isCelebrationShown = true;
                LevelSignals.RaiseWinFinaleStarted();
                _presentation.Animator.QueueDelay(CELEBRATION_DURATION);
                _presentation.Animator.StartBatch(onCelebrationComplete);
                return;
            }

            if (PlaceSpecialsForRemainingMoves())
            {
                _presentation.Animator.StartBatch(onSpawnComplete);
                return;
            }

            onSpawnComplete.Invoke();
        }

        public void NotifyLevelWonOnce()
        {
            if (_isLevelWonNotified)
            {
                return;
            }

            _isLevelWonNotified = true;
            int earnedStars = Mathf.Clamp(_services.Score.CurrentStars, MIN_STARS, MAX_STARS);
            LevelSignals.RaiseLevelWon(earnedStars);
        }

        /// <summary>
        /// Passing null only answers whether any special tile exists, without filling a buffer.
        /// </summary>
        public bool CollectSpecialTiles(List<GridCoord> buffer)
        {
            BoardModel model = _board.Model;
            byte width = model.Width;
            byte height = model.Height;
            bool found = false;

            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    ref GridCell cell = ref model.GetCell(coord);
                    if (!cell.IsPlayable || cell.IsLocked || !TileTypeLibrary.IsSpecialTile(cell.Type))
                    {
                        continue;
                    }

                    if (buffer == null)
                    {
                        return true;
                    }

                    buffer.Add(coord);
                    found = true;
                }
            }

            return found;
        }

        private bool PlaceSpecialsForRemainingMoves()
        {
            bool placedAny = false;

            while (_services.Limit.RemainingMoves > 0)
            {
                if (!TryFindRandomPlayableGem(out GridCoord coord))
                {
                    break;
                }

                _services.Limit.ConsumeMove(true);
                _services.Score.AddScoreForClearedTiles(BONUS_TILES_PER_MOVE);

                ref GridCell cell = ref _board.Model.GetCell(coord);
                TileType special = UnityEngine.Random.value > ROCKET_CHANCE ? TileType.HorizontalRocket : TileType.Bomb;
                cell.Type = special;

                GemView view = _board.Views[_board.Model.ToIndex(coord)];
                if (view != null)
                {
                    view.SetSprite(_presentation.SpriteDatabase.GetSprite(special));
                    _presentation.Animator.QueuePulse(view, SPAWN_PULSE_SCALE, SPAWN_DURATION);
                }

                placedAny = true;
            }

            return placedAny;
        }

        private bool TryFindRandomPlayableGem(out GridCoord result)
        {
            BoardModel model = _board.Model;
            byte width = model.Width;
            byte height = model.Height;

            for (int attempt = 0; attempt < RANDOM_PICK_ATTEMPTS; attempt++)
            {
                GridCoord c = new GridCoord((byte)UnityEngine.Random.Range(0, width), (byte)UnityEngine.Random.Range(0, height));
                if (IsPlayableGem(model, c))
                {
                    result = c;
                    return true;
                }
            }

            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    GridCoord c = new GridCoord(x, y);
                    if (IsPlayableGem(model, c))
                    {
                        result = c;
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        private static bool IsPlayableGem(BoardModel model, GridCoord coord)
        {
            ref GridCell cell = ref model.GetCell(coord);
            return cell.IsPlayable && !cell.IsLocked && TileTypeLibrary.IsStandardGem(cell.Type);
        }
    }
}
