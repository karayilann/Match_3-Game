using PrimeTween;
using Signals;
using UnityEngine;

namespace View.HUD
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LowMovesWarningView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _peakAlpha = 0.9f;
        [SerializeField] private float _flashInDuration = 0.12f;
        [SerializeField] private float _flashOutDuration = 0.45f;

        private Sequence _flashSequence;

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            LevelSignals.OnLowMovesWarning += HandleLowMovesWarning;
            LevelSignals.OnLowMovesWarningEnded += HandleStopWarning;
            LevelSignals.OnLevelLimitReached += HandleStopWarning;
            LevelSignals.OnAllGoalsCompleted += HandleStopWarning;
        }

        private void OnDisable()
        {
            LevelSignals.OnLowMovesWarning -= HandleLowMovesWarning;
            LevelSignals.OnLowMovesWarningEnded -= HandleStopWarning;
            LevelSignals.OnLevelLimitReached -= HandleStopWarning;
            LevelSignals.OnAllGoalsCompleted -= HandleStopWarning;
            StopFlash();
        }

        /// <summary>
        /// Flashes the thin red warning border once when a move is consumed while moves <= 5.
        /// Zero GC and halts any running sequence to smoothly replay the flash on rapid moves.
        /// </summary>
        private void HandleLowMovesWarning(byte remainingMoves)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            if (_flashSequence.isAlive)
            {
                _flashSequence.Stop();
            }

            _flashSequence = Sequence.Create()
                .Chain(Tween.Alpha(_canvasGroup, startValue: _canvasGroup.alpha, endValue: _peakAlpha, duration: _flashInDuration, ease: Ease.OutQuad))
                .Chain(Tween.Alpha(_canvasGroup, startValue: _peakAlpha, endValue: 0f, duration: _flashOutDuration, ease: Ease.InQuad));
        }

        private void HandleStopWarning()
        {
            StopFlash();
        }

        private void StopFlash()
        {
            if (_flashSequence.isAlive)
            {
                _flashSequence.Stop();
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }
    }
}
