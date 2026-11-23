using System;
using System.Collections.Generic;
using Core.Data.Tiles;
using Gameplay.Board;
using UnityEngine;
using View;

namespace Gameplay.Animation
{
    /// <summary>
    /// Turns the instant area clears of special tiles into a travelling wave: every hit cell gets a start delay
    /// from its distance to the blast origin, and flying copies, rocket trails and electric beams are queued
    /// on the shared BoardAnimator batch so the match resolve still completes with a single callback.
    /// </summary>
    public sealed class SpecialTileChoreographer
    {
        public const float CLEAR_DURATION = 0.15f;

        private const float HIT_SHAKE_AMPLITUDE = 0.12f;
        private const float HIT_SHAKE_DURATION = 0.15f;

        private const float ROCKET_WINDUP = 0.12f;
        private const float ROCKET_STEP = 0.038f;
        private const float ROCKET_EXTRA_TRAVEL = 1.6f;
        private const float ROCKET_BASE_FLIGHT = 0.16f;
        private const float ROCKET_HIT_LEAD_CELLS = 0.2f;
        private const float ROCKET_WINDUP_PULSE_SCALE = 1.30f;
        private const float ROCKET_WINDUP_SHAKE_AMPLITUDE = 0.10f;
        private const float BACKWARD_ROTATION_DEGREES = 180f;

        private const float BOMB_STEP = 0.05f;

        private const float COLOR_BOMB_WINDUP = 0.10f;
        private const float COLOR_BOMB_STAGGER = 0.03f;
        private const float COLOR_BOMB_BEAM_DURATION = 0.38f;
        private const float COLOR_BOMB_HIT_OFFSET = 0.06f;
        private const float COLOR_BOMB_PULSE_DURATION = 0.28f;
        private const float COLOR_BOMB_SHAKE_AMPLITUDE = 0.16f;
        private const float COLOR_BOMB_TARGET_PULSE_SCALE = 1.38f;
        private const float COLOR_BOMB_ORIGIN_PULSE_SCALE = 1.45f;

        private const float PROPELLER_FLIGHT = 0.4f;
        private const float PROJECTILE_APPEAR_DURATION = 0.05f;

        private static readonly Vector3 ZERO_SCALE = Vector3.zero;
        private static readonly Vector3 PROJECTILE_SCALE = new Vector3(0.7f, 0.7f, 1f);
        private static readonly Quaternion BACKWARD_ROTATION = Quaternion.Euler(0f, 0f, BACKWARD_ROTATION_DEGREES);

        private readonly BoardData _board;
        private readonly BoardPresentation _presentation;
        private readonly float[] _cellDelay;
        private readonly bool[] _hasDelay;
        private readonly List<GemView> _projectileViews;
        private readonly Action<int> _spawnParticleCallback;
        private readonly Action<MatchParticleView> _releaseParticleCallback;
        private readonly Action<MatchParticleView> _releaseRockBreakCallback;
        private readonly int _width;

        public SpecialTileChoreographer(BoardData board, BoardPresentation presentation)
        {
            _board = board;
            _presentation = presentation;
            _width = board.Model.Width;
            int capacity = board.Model.Width * board.Model.Height;
            _cellDelay = new float[capacity];
            _hasDelay = new bool[capacity];
            _projectileViews = new List<GemView>(capacity);
            _spawnParticleCallback = SpawnParticleAtIndex;
            _releaseParticleCallback = ReleaseParticle;
            _releaseRockBreakCallback = ReleaseRockBreakParticle;
        }

        public void BeginResolve()
        {
            Array.Clear(_hasDelay, 0, _hasDelay.Length);
        }

        public void ScheduleCombo(GridCoord origin, bool isColorBombCombo, List<GridCoord> targets)
        {
            if (isColorBombCombo)
            {
                ScheduleElectricWave(origin, targets, 0f);
                return;
            }

            int count = targets.Count;
            for (int i = 0; i < count; i++)
            {
                GridCoord c = targets[i];
                int dist = Mathf.Max(Mathf.Abs(c.X - origin.X), Mathf.Abs(c.Y - origin.Y));
                SetDelay(c, dist * BOMB_STEP);
            }
        }

        /// <summary>
        /// A cell already scheduled by an earlier blast keeps its earlier time, and a chained special starts
        /// from the time it was hit so a chain reads as one wave.
        /// </summary>
        public void ScheduleBlast(GridCoord origin, TileType type, List<GridCoord> hitCells)
        {
            int originIndex = _board.Model.ToIndex(origin);
            float baseDelay = _hasDelay[originIndex] ? _cellDelay[originIndex] : 0f;

            switch (type)
            {
                case TileType.HorizontalRocket:
                case TileType.VerticalRocket:
                    ScheduleRocket(origin, type, hitCells, baseDelay);
                    break;
                case TileType.Bomb:
                    for (int i = 0; i < hitCells.Count; i++)
                    {
                        GridCoord c = hitCells[i];
                        int dist = Mathf.Max(Mathf.Abs(c.X - origin.X), Mathf.Abs(c.Y - origin.Y));
                        SetDelay(c, baseDelay + dist * BOMB_STEP);
                    }
                    break;
                case TileType.ColorBomb:
                    ScheduleElectricWave(origin, hitCells, baseDelay);
                    break;
                case TileType.Propeller:
                    SchedulePropeller(origin, hitCells, baseDelay);
                    break;
            }
        }

        public void QueueClear(GridCoord coord)
        {
            int index = _board.Model.ToIndex(coord);
            float delay = _hasDelay[index] ? _cellDelay[index] : 0f;
            GemView view = _board.Views[index];
            if (view != null)
            {
                _presentation.Animator.QueueScale(view, ZERO_SCALE, CLEAR_DURATION, delay);
            }

            if (delay > 0f)
            {
                _presentation.Animator.QueueCallback(_spawnParticleCallback, index, delay);
            }
            else
            {
                SpawnParticleAtIndex(index);
            }
        }

        public void SpawnParticle(GridCoord coord)
        {
            if (_presentation.ParticlePool == null)
            {
                return;
            }

            MatchParticleView particle = _presentation.ParticlePool.Get();
            particle.Play(_board.Converter.ToWorldPosition(coord), _releaseParticleCallback);
        }

        public void SpawnRockDebris(GridCoord coord)
        {
            if (_presentation.RockBreakParticlePool == null)
            {
                return;
            }

            MatchParticleView debris = _presentation.RockBreakParticlePool.Get();
            debris.Play(_board.Converter.ToWorldPosition(coord), _releaseRockBreakCallback);
        }

        public void ReleaseProjectiles()
        {
            for (int i = 0; i < _projectileViews.Count; i++)
            {
                _presentation.GemPool.Release(_projectileViews[i]);
            }

            _projectileViews.Clear();
        }

        private void ScheduleRocket(GridCoord origin, TileType type, List<GridCoord> hitCells, float baseDelay)
        {
            bool horizontal = type == TileType.HorizontalRocket;
            float launchDelay = baseDelay + ROCKET_WINDUP;

            GemView originView = _board.Views[_board.Model.ToIndex(origin)];
            if (originView != null)
            {
                _presentation.Animator.QueuePulse(originView, ROCKET_WINDUP_PULSE_SCALE, ROCKET_WINDUP, baseDelay);
                _presentation.Animator.QueueShake(originView, ROCKET_WINDUP_SHAKE_AMPLITUDE, ROCKET_WINDUP, baseDelay);
            }
            SetDelay(origin, launchDelay);

            for (int i = 0; i < hitCells.Count; i++)
            {
                GridCoord c = hitCells[i];
                if (c.Equals(origin))
                {
                    continue;
                }

                int dist = horizontal ? Mathf.Abs(c.X - origin.X) : Mathf.Abs(c.Y - origin.Y);
                float hitDelay = launchDelay + Mathf.Max(0f, (dist - ROCKET_HIT_LEAD_CELLS) * ROCKET_STEP);
                SetDelay(c, hitDelay);

                int cellIndex = _board.Model.ToIndex(c);
                _presentation.Animator.QueueCallback(_spawnParticleCallback, cellIndex, hitDelay);

                GemView targetView = _board.Views[cellIndex];
                if (targetView != null)
                {
                    _presentation.Animator.QueueShake(targetView, HIT_SHAKE_AMPLITUDE, HIT_SHAKE_DURATION, hitDelay);
                }
            }

            Vector3 originWorld = _board.Converter.ToWorldPosition(origin);
            byte last = horizontal ? (byte)(_board.Model.Width - 1) : (byte)(_board.Model.Height - 1);
            int negativeDist = horizontal ? origin.X : origin.Y;
            int positiveDist = last - (horizontal ? origin.X : origin.Y);

            Vector3 negativeDir = horizontal ? Vector3.left : Vector3.down;
            Vector3 positiveDir = horizontal ? Vector3.right : Vector3.up;

            Vector3 negativeTarget = originWorld + negativeDir * (negativeDist + ROCKET_EXTRA_TRAVEL);
            Vector3 positiveTarget = originWorld + positiveDir * (positiveDist + ROCKET_EXTRA_TRAVEL);

            float negativeDuration = ROCKET_BASE_FLIGHT + negativeDist * ROCKET_STEP;
            float positiveDuration = ROCKET_BASE_FLIGHT + positiveDist * ROCKET_STEP;

            SpawnProjectile(type, originWorld, negativeTarget, BACKWARD_ROTATION, negativeDuration, launchDelay);
            _presentation.Animator.QueueRocketTrail(originWorld, negativeTarget, negativeDuration, launchDelay);

            SpawnProjectile(type, originWorld, positiveTarget, Quaternion.identity, positiveDuration, launchDelay);
            _presentation.Animator.QueueRocketTrail(originWorld, positiveTarget, positiveDuration, launchDelay);
        }

        private void ScheduleElectricWave(GridCoord origin, List<GridCoord> targets, float baseDelay)
        {
            int originIndex = _board.Model.ToIndex(origin);
            Vector3 originWorld = _board.Converter.ToWorldPosition(origin);
            GemView originView = _board.Views[originIndex];

            int targetStep = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                GridCoord c = targets[i];
                if (c.Equals(origin))
                {
                    continue;
                }

                float launchDelay = baseDelay + COLOR_BOMB_WINDUP + targetStep * COLOR_BOMB_STAGGER;
                float hitDelay = launchDelay + COLOR_BOMB_HIT_OFFSET;
                float detonateDelay = launchDelay + COLOR_BOMB_BEAM_DURATION;
                targetStep++;

                SetDelay(c, detonateDelay);

                Vector3 targetWorld = _board.Converter.ToWorldPosition(c);
                _presentation.Animator.QueueElectricBeam(originWorld, targetWorld, COLOR_BOMB_BEAM_DURATION, launchDelay);

                int targetIndex = _board.Model.ToIndex(c);
                _presentation.Animator.QueueCallback(_spawnParticleCallback, targetIndex, hitDelay);

                GemView target = _board.Views[targetIndex];
                if (target != null)
                {
                    _presentation.Animator.QueuePulse(target, COLOR_BOMB_TARGET_PULSE_SCALE, COLOR_BOMB_PULSE_DURATION, hitDelay);
                    _presentation.Animator.QueueShake(target, COLOR_BOMB_SHAKE_AMPLITUDE, COLOR_BOMB_BEAM_DURATION - COLOR_BOMB_HIT_OFFSET, hitDelay);
                }
            }

            float totalWaveDuration = targetStep > 0
                ? COLOR_BOMB_WINDUP + (targetStep - 1) * COLOR_BOMB_STAGGER + COLOR_BOMB_BEAM_DURATION
                : COLOR_BOMB_WINDUP + COLOR_BOMB_BEAM_DURATION;

            if (originView != null)
            {
                _presentation.Animator.QueuePulse(originView, COLOR_BOMB_ORIGIN_PULSE_SCALE, COLOR_BOMB_PULSE_DURATION, baseDelay);
                _presentation.Animator.QueueShake(originView, COLOR_BOMB_SHAKE_AMPLITUDE, totalWaveDuration, baseDelay);
            }

            SetDelay(origin, baseDelay + totalWaveDuration);
        }

        private void SchedulePropeller(GridCoord origin, List<GridCoord> hitCells, float baseDelay)
        {
            SetDelay(origin, baseDelay);

            if (hitCells.Count < 2)
            {
                return;
            }

            GridCoord target = hitCells[hitCells.Count - 1];
            bool isAdjacent = Mathf.Abs(target.X - origin.X) + Mathf.Abs(target.Y - origin.Y) <= 1;
            if (isAdjacent)
            {
                return;
            }

            SetDelay(target, baseDelay + PROPELLER_FLIGHT);
            Vector3 fromWorld = _board.Converter.ToWorldPosition(origin);
            Vector3 toWorld = _board.Converter.ToWorldPosition(target);
            SpawnProjectile(TileType.Propeller, fromWorld, toWorld, Quaternion.identity, PROPELLER_FLIGHT, baseDelay);
        }

        private void SetDelay(GridCoord coord, float delay)
        {
            int index = _board.Model.ToIndex(coord);
            if (_hasDelay[index])
            {
                return;
            }

            _hasDelay[index] = true;
            _cellDelay[index] = delay;
        }

        private void SpawnProjectile(TileType type, Vector3 fromWorld, Vector3 toWorld, Quaternion rotation, float duration, float startDelay)
        {
            if (duration <= 0f)
            {
                return;
            }

            GemView projectile = _presentation.GemPool.Get();
            projectile.SetSprite(_presentation.SpriteDatabase.GetSprite(type));
            projectile.SetPosition(fromWorld);
            projectile.SetRotation(rotation);
            projectile.SetScale(ZERO_SCALE);
            _projectileViews.Add(projectile);

            _presentation.Animator.QueueScale(projectile, PROJECTILE_SCALE, PROJECTILE_APPEAR_DURATION, startDelay);
            _presentation.Animator.QueueMove(projectile, toWorld, duration, AnimationEase.QuadIn, startDelay);
        }

        private void SpawnParticleAtIndex(int index)
        {
            SpawnParticle(new GridCoord((byte)(index % _width), (byte)(index / _width)));
        }

        private void ReleaseParticle(MatchParticleView particle)
        {
            _presentation.ParticlePool.Release(particle);
        }

        private void ReleaseRockBreakParticle(MatchParticleView particle)
        {
            _presentation.RockBreakParticlePool.Release(particle);
        }
    }
}
