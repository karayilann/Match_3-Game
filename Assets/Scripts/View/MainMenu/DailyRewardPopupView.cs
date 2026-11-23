using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using View.Popups;

namespace View.MainMenu
{
    public sealed class DailyRewardPopupView : PopupView
    {
        private const int REWARD_COINS = 500;
        private const int REWARD_HAMMERS = 1;

        [Header("UI Elements")]
        [SerializeField] private Button _claimButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TextMeshProUGUI _rewardCoinsText;
        [SerializeField] private TextMeshProUGUI _rewardBoosterText;

        protected override void Awake()
        {
            base.Awake();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }

            if (_claimButton != null)
            {
                _claimButton.onClick.AddListener(ClaimReward);
            }
        }

        private void OnEnable()
        {
            UiSignals.OnOpenDailyReward += Open;
        }

        private void OnDisable()
        {
            UiSignals.OnOpenDailyReward -= Open;
        }

        public override void Open()
        {
            if (_rewardCoinsText != null)
            {
                _rewardCoinsText.text = "+500 Coins";
            }

            if (_rewardBoosterText != null)
            {
                _rewardBoosterText.text = "+1 Royal Hammer";
            }

            base.Open();
        }

        private void ClaimReward()
        {
            EconomySignals.RaiseDailyRewardClaimRequested(REWARD_COINS, REWARD_HAMMERS);
            Close();
        }
    }
}
