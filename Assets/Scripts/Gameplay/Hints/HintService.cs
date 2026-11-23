using System;
using Core.Data.Level;
using Core.Data.Tiles;
using Gameplay.Animation;
using Gameplay.Board;
using Gameplay.Matching;
using Signals;
using UnityEngine;
using View;

namespace Gameplay.Hints
{
    public sealed class HintService : IDisposable
    {
        private const float PULSE_SCALE = 1.18f;
        private const float PULSE_DURATION = 0.5f;
        private const float REPEAT_INTERVAL = 2.5f;

        private readonly BoardModel _boardModel;
        private readonly IPossibleMoveDetector _moveDetector;
        private readonly GemView[] _views;
        private readonly BoardAnimator _animator;
        private readonly bool _enabled;
        private readonly float _hintDelaySeconds;

        private float _timer;
        private float _repeatTimer;
        private bool _isHintActive;
        private GridCoord _hintFrom;
        private GridCoord _hintTo;

        public HintService(
            BoardModel boardModel,
            IPossibleMoveDetector moveDetector,
            GemView[] views,
            BoardAnimator animator,
            LevelData levelData)
        {
            _boardModel = boardModel;
            _moveDetector = moveDetector;
            _views = views;
            _animator = animator;
            _enabled = levelData != null && levelData.EnableHints;
            _hintDelaySeconds = levelData != null ? levelData.HintDelaySeconds : 5f;

            BoardSignals.OnCellTapped += HandleInteraction;
            BoardSignals.OnTileSwiped += HandleSwipeInteraction;
        }

        public void Tick(float deltaTime)
        {
            if (!_enabled)
            {
                return;
            }

            _timer += deltaTime;

            if (!_isHintActive)
            {
                if (_timer >= _hintDelaySeconds)
                {
                    if (_moveDetector.TryFindPossibleMove(_boardModel, out _hintFrom, out _hintTo))
                    {
                        _isHintActive = true;
                        _repeatTimer = 0f;
                        TriggerPulse();
                    }
                    else
                    {
                        _timer = 0f;
                    }
                }
                return;
            }

            _repeatTimer += deltaTime;
            if (_repeatTimer >= REPEAT_INTERVAL)
            {
                _repeatTimer = 0f;
                TriggerPulse();
            }
        }

        public void ClearHint()
        {
            _timer = 0f;
            _repeatTimer = 0f;

            if (!_isHintActive)
            {
                return;
            }

            _isHintActive = false;
            ResetViewScale(_hintFrom);
            ResetViewScale(_hintTo);
        }

        public void Dispose()
        {
            BoardSignals.OnCellTapped -= HandleInteraction;
            BoardSignals.OnTileSwiped -= HandleSwipeInteraction;
            ClearHint();
        }

        private void HandleInteraction(GridCoord _)
        {
            ClearHint();
        }

        private void HandleSwipeInteraction(GridCoord _, GridCoord __)
        {
            ClearHint();
        }

        private void TriggerPulse()
        {
            PulseView(_hintFrom);
            PulseView(_hintTo);
        }

        private void PulseView(GridCoord coord)
        {
            int index = _boardModel.ToIndex(coord);
            GemView view = _views[index];
            if (view != null)
            {
                _animator.CancelCommands(view);
                _animator.QueuePulse(view, PULSE_SCALE, PULSE_DURATION);
            }
        }

        private void ResetViewScale(GridCoord coord)
        {
            int index = _boardModel.ToIndex(coord);
            GemView view = _views[index];
            if (view != null)
            {
                _animator.CancelCommands(view);
                view.SetScale(Vector3.one);
            }
        }
    }
}
