using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

namespace View.HUD
{
    public sealed class ReshuffleBannerView : MonoBehaviour
    {
        [SerializeField] private RectTransform _bannerRoot;
        [SerializeField] private TMP_Text _bannerText;

        public RectTransform BannerRoot
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _bannerRoot;
        }

        private void Awake()
        {
            if (_bannerRoot != null)
            {
                _bannerRoot.gameObject.SetActive(false);
            }
        }

        public void SetText(string message)
        {
            if (_bannerText != null)
            {
                _bannerText.SetText(message);
            }
        }
    }
}

