using Core.Data.Boosters;
using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using View.Popups;

namespace View.MainMenu
{
    public sealed class InventoryPopupView : PopupView
    {
        private const string COUNT_FORMAT = "x{0}";

        [Header("Count Badges")]
        [SerializeField] private TextMeshProUGUI _hammerCountText;
        [SerializeField] private TextMeshProUGUI _gloveCountText;
        [SerializeField] private TextMeshProUGUI _shuffleCountText;

        [Header("Buttons")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private ShopPopupView _shopPopup;

        private int _hammerCount;
        private int _gloveCount;
        private int _shuffleCount;

        protected override void Awake()
        {
            base.Awake();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }

            if (_shopButton != null)
            {
                _shopButton.onClick.AddListener(HandleShopClicked);
            }
        }

        private void OnEnable()
        {
            UiSignals.OnOpenInventory += Open;
            EconomySignals.OnBoosterCountChanged += HandleBoosterCountChanged;
        }

        private void OnDisable()
        {
            UiSignals.OnOpenInventory -= Open;
            EconomySignals.OnBoosterCountChanged -= HandleBoosterCountChanged;
        }

        private void HandleBoosterCountChanged(BoosterType type, int count)
        {
            switch (type)
            {
                case BoosterType.Hammer:
                    _hammerCount = count;
                    break;
                case BoosterType.Glove:
                    _gloveCount = count;
                    break;
                case BoosterType.Shuffle:
                    _shuffleCount = count;
                    break;
                default:
                    return;
            }

            if (IsOpen)
            {
                RefreshCounts();
            }
        }

        public override void Open()
        {
            RefreshCounts();
            base.Open();
        }

        public void RefreshCounts()
        {
            if (_hammerCountText != null)
            {
                _hammerCountText.SetText(COUNT_FORMAT, _hammerCount);
            }

            if (_gloveCountText != null)
            {
                _gloveCountText.SetText(COUNT_FORMAT, _gloveCount);
            }

            if (_shuffleCountText != null)
            {
                _shuffleCountText.SetText(COUNT_FORMAT, _shuffleCount);
            }
        }

        private void HandleShopClicked()
        {
            Close();
            if (_shopPopup != null)
            {
                _shopPopup.Open();
            }
            else
            {
                UiSignals.RaiseOpenShop();
            }
        }
    }
}
