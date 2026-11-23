using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using View.Popups;

namespace View.MainMenu
{
    public sealed class ShopPopupView : PopupView
    {
        [Header("Buttons")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _buyPack1Button;
        [SerializeField] private Button _buyPack2Button;
        [SerializeField] private Button _buyPack3Button;

        protected override void Awake()
        {
            base.Awake();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }

            if (_buyPack1Button != null)
            {
                _buyPack1Button.onClick.AddListener(() => AddCoins(1000));
            }

            if (_buyPack2Button != null)
            {
                _buyPack2Button.onClick.AddListener(() => AddCoins(5000));
            }

            if (_buyPack3Button != null)
            {
                _buyPack3Button.onClick.AddListener(() => AddBundle(10000, 3));
            }
        }

        private void OnEnable()
        {
            UiSignals.OnOpenShop += Open;
        }

        private void OnDisable()
        {
            UiSignals.OnOpenShop -= Open;
        }

        private void AddCoins(int amount)
        {
            EconomySignals.RaiseCoinPackPurchaseRequested(amount);
            Close();
        }

        private void AddBundle(int coins, int boosterCount)
        {
            EconomySignals.RaiseBundlePurchaseRequested(coins, boosterCount);
            Close();
        }
    }
}
