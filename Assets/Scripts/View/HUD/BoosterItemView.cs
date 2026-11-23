using Core.Data.Boosters;
using PrimeTween;
using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View.HUD
{
    public sealed class BoosterItemView : MonoBehaviour
    {
        [SerializeField] private BoosterType _boosterType;
        [SerializeField] private Button _button;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _glowImage;
        [SerializeField] private TMP_Text _countText;

        [Header("Frame Animation")]
        [SerializeField] private Sprite[] _animationFrames;
        [SerializeField] private float _framesPerSecond = 8f;

        private Tween _animationTween;

        public BoosterType Type => _boosterType;

        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleButtonClicked);
            }

            if (_glowImage != null)
            {
                _glowImage.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            StartFrameAnimation();
        }

        private void OnDisable()
        {
            StopFrameAnimation();
        }

        private void OnDestroy()
        {
            StopFrameAnimation();

            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleButtonClicked);
            }
        }

        public void SetAnimationFrames(Sprite[] frames, float fps = 8f)
        {
            _animationFrames = frames;
            _framesPerSecond = fps;
            if (isActiveAndEnabled)
            {
                StartFrameAnimation();
            }
        }

        private void StartFrameAnimation()
        {
            StopFrameAnimation();

            if (_animationFrames == null || _animationFrames.Length <= 1 || _iconImage == null)
            {
                return;
            }

            float duration = _animationFrames.Length / Mathf.Max(1f, _framesPerSecond);
            _animationTween = Tween.Custom(
                this,
                0f,
                1f,
                duration,
                static (target, val) =>
                {
                    if (target != null && target._iconImage != null && target._animationFrames != null && target._animationFrames.Length > 0)
                    {
                        int index = Mathf.Clamp((int)(val * target._animationFrames.Length), 0, target._animationFrames.Length - 1);
                        target._iconImage.sprite = target._animationFrames[index];
                    }
                },
                ease: Ease.Linear,
                cycles: -1);
        }

        private void StopFrameAnimation()
        {
            if (_animationTween.isAlive)
            {
                _animationTween.Stop();
            }
        }

        public void SetCount(int count)
        {
            if (_countText != null)
            {
                if (count > 0)
                {
                    _countText.SetText("{0:0}", count);
                }
                else
                {
                    _countText.SetText("+");
                }
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (_glowImage != null)
            {
                _glowImage.gameObject.SetActive(isSelected);

                if (isSelected)
                {
                    Tween.CompleteAll(transform);
                    Tween.PunchScale(transform, new Vector3(0.18f, 0.18f, 0f), 0.25f);
                }
            }
        }

        private void HandleButtonClicked()
        {
            EconomySignals.RaiseBoosterButtonClicked(_boosterType);
        }
    }
}
