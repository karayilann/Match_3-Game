using PrimeTween;
using Signals;
using UnityEngine;
using UnityEngine.UI;

namespace View.HUD
{
    public sealed class HudScoreProgressBarView : MonoBehaviour
    {
        [Header("Slider")]
        [SerializeField] private Slider _slider;

        [Header("Stars")]
        [SerializeField] private Image[] _stars;
        [SerializeField] private Color _activeStarColor = new Color(1f, 0.85f, 0.1f, 1f);
        [SerializeField] private Color _inactiveStarColor = new Color(0.35f, 0.35f, 0.45f, 0.7f);

        [Header("Score Thresholds")]
        [SerializeField] private int _targetScore1Star = 500;
        [SerializeField] private int _targetScore2Star = 1200;
        [SerializeField] private int _targetScore3Star = 2200;

        private int _achievedStars = 0;

        public int AchievedStars => _achievedStars;

        private void Awake()
        {
            ResetVisuals();
        }

        private void OnEnable()
        {
            ResetVisuals();
            LevelSignals.OnScoreChanged += HandleScoreChanged;
            LevelSignals.OnScoreTargetsConfigured += SetTargetScores;
        }

        private void OnDisable()
        {
            LevelSignals.OnScoreChanged -= HandleScoreChanged;
            LevelSignals.OnScoreTargetsConfigured -= SetTargetScores;
        }

        public void ResetVisuals()
        {
            _achievedStars = 0;
            if (_slider != null)
            {
                Tween.StopAll(_slider);
                _slider.minValue = 0f;
                _slider.maxValue = _targetScore3Star;
                _slider.value = 0f;
            }

            if (_stars != null)
            {
                for (int i = 0; i < _stars.Length; i++)
                {
                    if (_stars[i] != null)
                    {
                        Tween.StopAll(_stars[i].transform);
                        _stars[i].color = _inactiveStarColor;
                        _stars[i].transform.localScale = Vector3.one;
                    }
                }
            }
        }

        public void SetTargetScores(int star1, int star2, int star3)
        {
            _targetScore1Star = star1;
            _targetScore2Star = star2;
            _targetScore3Star = star3;
            ResetVisuals();
        }

        private void HandleScoreChanged(int currentScore)
        {
            if (_slider != null)
            {
                float target = Mathf.Clamp(currentScore, 0, _targetScore3Star);
                Tween.Custom(_slider, _slider.value, target, duration: 0.25f,
                    onValueChange: static (slider, val) => slider.value = val,
                    ease: Ease.OutQuad);
            }

            int newStars = 0;
            if (currentScore >= _targetScore3Star)
            {
                newStars = 3;
            }
            else if (currentScore >= _targetScore2Star)
            {
                newStars = 2;
            }
            else if (currentScore >= _targetScore1Star)
            {
                newStars = 1;
            }

            if (newStars > _achievedStars)
            {
                for (int i = _achievedStars; i < newStars; i++)
                {
                    AnimateStarActivation(i);
                }
                _achievedStars = newStars;
            }
        }

        private void AnimateStarActivation(int starIndex)
        {
            if (_stars == null || starIndex < 0 || starIndex >= _stars.Length)
            {
                return;
            }

            Image star = _stars[starIndex];
            if (star == null)
            {
                return;
            }

            star.color = _activeStarColor;
            star.transform.localScale = Vector3.one * 1.5f;
            Tween.Scale(star.transform, Vector3.one, 0.4f, Ease.OutBounce);
        }
    }
}
