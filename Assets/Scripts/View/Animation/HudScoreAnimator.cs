using PrimeTween;
using Signals;
using UnityEngine;
using UnityEngine.UI;

namespace View.Animation
{
    public sealed class HudScoreAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform _scoreTarget;
        [SerializeField] private Graphic _scoreGraphic;
        [SerializeField] private Vector3 _punchScaleStrength = new Vector3(0.3f, 0.3f, 0f);
        [SerializeField] private Vector3 _punchRotationStrength = new Vector3(0f, 0f, 7f);
        [SerializeField] private Color _highlightColor = new Color(1f, 0.85f, 0.24f, 1f);
        [SerializeField] private float _duration = 0.28f;
        [SerializeField] private int _frequency = 6;

        private Color _baseColor;
        private int _lastScore;
        private bool _isInitialized;

        private void Awake()
        {
            _baseColor = _scoreGraphic.color;
        }

        private void OnEnable()
        {
            LevelSignals.OnScoreChanged += HandleScoreChanged;
        }

        private void OnDisable()
        {
            LevelSignals.OnScoreChanged -= HandleScoreChanged;
            Tween.CompleteAll(_scoreTarget);
            Tween.CompleteAll(_scoreGraphic);
        }

        /// <summary>
        /// Combines scale punch, local Z-rotation tilt, and a gold-to-base color flash on score increase while skipping the initial zero broadcast.
        /// </summary>
        private void HandleScoreChanged(int score)
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                _lastScore = score;
                return;
            }

            if (score == _lastScore)
            {
                return;
            }

            _lastScore = score;

            Tween.CompleteAll(_scoreTarget);
            Tween.CompleteAll(_scoreGraphic);

            Tween.PunchScale(_scoreTarget, _punchScaleStrength, _duration, _frequency);
            Tween.PunchLocalRotation(_scoreTarget, _punchRotationStrength, _duration, _frequency);

            _scoreGraphic.color = _highlightColor;
            Tween.Color(_scoreGraphic, _baseColor, _duration);
        }
    }
}
