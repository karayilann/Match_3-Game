using System;
using UnityEngine;
using View;

namespace Gameplay.Animation
{
    public sealed class BoardAnimator : MonoBehaviour
    {
        public const float OBSTACLE_SHAKE_AMPLITUDE = 0.12f;

        [SerializeField] private Material _lineEffectMaterial;

        private TweenRunner _tweens;
        private ElectricBeamRenderer _beams;
        private RocketTrailRenderer _rocketTrails;
        private Action _onBatchComplete;
        private float _timeScale = 1f;

        /// <summary>
        /// Scales every batch animation at once so a skip speeds the whole resolve up
        /// without changing the order the commands and the completion callback run in.
        /// </summary>
        public void SetTimeScale(float timeScale)
        {
            _timeScale = timeScale;
        }

        public void Init(int capacity)
        {
            _timeScale = 1f;

            if (_tweens == null)
            {
                _tweens = new TweenRunner(capacity);
            }
            else
            {
                _tweens.Clear();
            }

            EnsureLineRenderers();
            _beams.Reset();
            _rocketTrails.Reset();
            _onBatchComplete = null;
        }

        private void OnDestroy()
        {
            _beams?.Dispose();
            _rocketTrails?.Dispose();
        }

        public void StartBatch(Action onComplete)
        {
            _onBatchComplete = onComplete;
        }

        public void QueueMove(GemView view, Vector3 targetPosition, float duration, AnimationEase ease = AnimationEase.Linear, float startDelay = 0f)
        {
            _tweens.Add(new TileAnimationCommand
            {
                View = view,
                TargetType = AnimationTargetType.Position,
                Ease = ease,
                StartValue = view.GetPosition(),
                EndValue = targetPosition,
                Duration = duration,
                Elapsed = 0f,
                StartDelay = startDelay
            });
        }

        public void QueueScale(GemView view, Vector3 targetScale, float duration, float startDelay = 0f)
        {
            _tweens.Add(new TileAnimationCommand
            {
                View = view,
                TargetType = AnimationTargetType.Scale,
                StartValue = view.GetScale(),
                EndValue = targetScale,
                Duration = duration,
                Elapsed = 0f,
                StartDelay = startDelay
            });
        }

        /// <summary>
        /// Runs a cached delegate once after the delay, so timed effects stay inside the same batch
        /// as the tile animations without allocating a closure per call.
        /// </summary>
        public void QueueCallback(Action<int> callback, int argument, float startDelay)
        {
            _tweens.Add(new TileAnimationCommand
            {
                TargetType = AnimationTargetType.Callback,
                Callback = callback,
                CallbackArg = argument,
                Duration = 0f,
                Elapsed = 0f,
                StartDelay = startDelay
            });
        }

        public void QueueShake(GemView view, float amplitude, float duration, float startDelay = 0f)
        {
            _tweens.Add(new TileAnimationCommand
            {
                View = view,
                TargetType = AnimationTargetType.Shake,
                StartValue = view.GetPosition(),
                EndValue = new Vector3(amplitude, 0f, 0f),
                Duration = duration,
                Elapsed = 0f,
                StartDelay = startDelay
            });
        }

        public void QueuePulse(GemView view, float maxScale, float duration, float startDelay = 0f)
        {
            _tweens.Add(new TileAnimationCommand
            {
                View = view,
                TargetType = AnimationTargetType.Pulse,
                StartValue = view.GetScale(),
                EndValue = new Vector3(maxScale, maxScale, 1f),
                Duration = duration,
                Elapsed = 0f,
                StartDelay = startDelay
            });
        }

        public void QueueDelay(float duration)
        {
            _tweens.Add(new TileAnimationCommand
            {
                TargetType = AnimationTargetType.Delay,
                Duration = duration,
                Elapsed = 0f
            });
        }

        public void QueueElectricBeam(Vector3 startWorldPos, Vector3 endWorldPos, float duration, float startDelay = 0f)
        {
            EnsureLineRenderers();
            _beams.Queue(startWorldPos, endWorldPos, duration, startDelay);
        }

        public void QueueRocketTrail(Vector3 startWorldPos, Vector3 endWorldPos, float duration, float startDelay = 0f)
        {
            EnsureLineRenderers();
            _rocketTrails.Queue(startWorldPos, endWorldPos, duration, startDelay);
        }

        public void CancelCommands(GemView view)
        {
            if (view == null || _tweens == null)
            {
                return;
            }

            _tweens.Cancel(view);
        }

        private void Update()
        {
            if (_tweens == null)
            {
                return;
            }

            bool hasTweens = _tweens.HasActive;
            bool hasBeams = _beams.HasActive;
            bool hasRocketTrails = _rocketTrails.HasActive;

            if (hasTweens || hasBeams || hasRocketTrails)
            {
                float deltaTime = Time.deltaTime * _timeScale;

                if (hasTweens)
                {
                    _tweens.Tick(deltaTime);
                }

                if (hasBeams)
                {
                    _beams.Tick(deltaTime);
                }

                if (hasRocketTrails)
                {
                    _rocketTrails.Tick(deltaTime);
                }

                return;
            }

            if (_onBatchComplete != null)
            {
                Action callback = _onBatchComplete;
                _onBatchComplete = null;
                callback.Invoke();
            }
        }

        private void EnsureLineRenderers()
        {
            if (_beams != null)
            {
                return;
            }

            Transform parent = transform;
            _beams = new ElectricBeamRenderer(parent, _lineEffectMaterial);
            _rocketTrails = new RocketTrailRenderer(parent, _lineEffectMaterial);
        }
    }
}
