using PrimeTween;
using Signals;
using UnityEngine;
using View.HUD;

namespace View.Animation
{
    public sealed class HudReshuffleAnimator : MonoBehaviour
    {
        private static readonly Vector3 HIDDEN_SCALE = Vector3.zero;
        private static readonly Vector3 VISIBLE_SCALE = Vector3.one;

        [SerializeField] private ReshuffleBannerView _bannerView;
        [SerializeField] private float _enterDuration = 0.22f;
        [SerializeField] private float _holdDuration = 0.65f;
        [SerializeField] private float _exitDuration = 0.18f;

        private Sequence _bannerSequence;

        private void OnEnable()
        {
            BoardSignals.OnBoardReshuffled += HandleBoardReshuffled;
        }

        private void OnDisable()
        {
            BoardSignals.OnBoardReshuffled -= HandleBoardReshuffled;

            CompleteBannerSequence();
        }

        private void HandleBoardReshuffled()
        {
            if (_bannerView == null || _bannerView.BannerRoot == null)
            {
                return;
            }

            RectTransform root = _bannerView.BannerRoot;
            CompleteBannerSequence();

            root.gameObject.SetActive(true);
            root.localScale = HIDDEN_SCALE;

            _bannerSequence = Sequence.Create()
                .Chain(Tween.Scale(root, VISIBLE_SCALE, _enterDuration, Ease.OutBack))
                .ChainDelay(_holdDuration)
                .Chain(Tween.Scale(root, HIDDEN_SCALE, _exitDuration, Ease.InBack))
                .OnComplete(root.gameObject, static target => target.SetActive(false));
        }

        /// <summary>
        /// The banner tweens are nested in a Sequence, so they must be completed through the Sequence handle;
        /// completing them by target is rejected by PrimeTween.
        /// </summary>
        private void CompleteBannerSequence()
        {
            if (_bannerSequence.isAlive)
            {
                _bannerSequence.Complete();
            }
        }
    }
}

