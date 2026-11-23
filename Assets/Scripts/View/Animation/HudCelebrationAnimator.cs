using Signals;
using UnityEngine;

namespace View.Animation
{
    public sealed class HudCelebrationAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform _bannerRoot;
        [SerializeField] private ParticleSystem _confetti;

        private void Awake()
        {
            if (_bannerRoot != null)
            {
                _bannerRoot.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            LevelSignals.OnWinFinaleStarted += HandleWinFinaleStarted;
        }

        private void OnDisable()
        {
            LevelSignals.OnWinFinaleStarted -= HandleWinFinaleStarted;
        }

        private void HandleWinFinaleStarted()
        {
            PlayConfetti();
        }

        /// <summary>
        /// The emitter spans the visible top edge of the camera, which varies with the fitted ortho size.
        /// </summary>
        private void PlayConfetti()
        {
            if (_confetti == null)
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                float halfHeight = cam.orthographicSize;
                float halfWidth = halfHeight * cam.aspect;
                Transform confettiTransform = _confetti.transform;
                Vector3 camPosition = cam.transform.position;
                confettiTransform.position = new Vector3(camPosition.x, camPosition.y + halfHeight, 0f);

                ParticleSystem.ShapeModule shape = _confetti.shape;
                shape.scale = new Vector3(halfWidth * 2f, 1f, 1f);
            }

            _confetti.Play(true);
        }
    }
}
