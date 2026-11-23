using PrimeTween;
using Signals;
using UnityEngine;

namespace View.Animation
{
    public sealed class HudMovesAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform _movesTarget;
        [SerializeField] private Vector3 _punchStrength = new Vector3(0.35f, 0.35f, 0f);
        [SerializeField] private float _punchDuration = 0.25f;
        [SerializeField] private int _punchFrequency = 6;

        private byte _lastMoves;
        private bool _isInitialized;

        private void OnEnable()
        {
            LevelSignals.OnMovesChanged += HandleMovesChanged;
        }

        private void OnDisable()
        {
            LevelSignals.OnMovesChanged -= HandleMovesChanged;
            Tween.CompleteAll(_movesTarget);
        }

        /// <summary>
        /// Skips the initial level-load broadcast and completes any active tween before punching scale so rapid move updates never drift the base scale.
        /// </summary>
        private void HandleMovesChanged(byte remainingMoves)
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                _lastMoves = remainingMoves;
                return;
            }

            if (remainingMoves == _lastMoves)
            {
                return;
            }

            _lastMoves = remainingMoves;
            Tween.CompleteAll(_movesTarget);
            Tween.PunchScale(_movesTarget, _punchStrength, _punchDuration, _punchFrequency);
        }
    }
}
