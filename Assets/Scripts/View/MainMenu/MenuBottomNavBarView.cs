using System.Collections.Generic;
using PrimeTween;
using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View.MainMenu
{
    public sealed class MenuBottomNavBarView : MonoBehaviour
    {
        private const string PLAY_LEVEL_FORMAT = "LEVEL {0}";

        [Header("Bottom Tabs")]
        [SerializeField] private Button _tabMapButton;
        [SerializeField] private Button _tabTeamButton;
        [SerializeField] private Button _tabInventoryButton;
        [SerializeField] private Button _tabEventsButton;
        [SerializeField] private Button _tabShopButton;

        [Header("Floating Action Buttons")]
        [SerializeField] private Button _playFloatingButton;
        [SerializeField] private TextMeshProUGUI _playButtonLevelText;
        [SerializeField] private Button _dailyGiftButton;

        [Header("Popups Reference")]
        [SerializeField] private ShopPopupView _shopPopup;
        [SerializeField] private DailyRewardPopupView _dailyRewardPopup;
        [SerializeField] private InfoModalPopupView _infoModalPopup;
        [SerializeField] private InventoryPopupView _inventoryPopup;

        private Tween _playPulseTween;
        private int _unlockedLevel = 1;

        private void Awake()
        {
            if (_tabMapButton != null)
            {
                _tabMapButton.onClick.AddListener(HandleMapTabClicked);
            }

            if (_tabTeamButton != null)
            {
                _tabTeamButton.onClick.AddListener(HandleTeamTabClicked);
            }

            if (_tabInventoryButton != null)
            {
                _tabInventoryButton.onClick.AddListener(HandleInventoryTabClicked);
            }

            if (_tabEventsButton != null)
            {
                _tabEventsButton.onClick.AddListener(HandleEventsTabClicked);
            }

            if (_tabShopButton != null)
            {
                _tabShopButton.onClick.AddListener(HandleShopTabClicked);
            }

            if (_playFloatingButton != null)
            {
                _playFloatingButton.onClick.AddListener(HandlePlayFloatingClicked);
            }

            if (_dailyGiftButton != null)
            {
                _dailyGiftButton.onClick.AddListener(HandleDailyGiftClicked);
            }
        }

        private void OnEnable()
        {
            UiSignals.OnProgressChanged += HandleProgressChanged;
        }

        private void OnDisable()
        {
            UiSignals.OnProgressChanged -= HandleProgressChanged;
        }

        private void Start()
        {
            RefreshUnlockedLevel();
            StartFloatingPlayPulse();
        }

        private void OnDestroy()
        {
            if (_playPulseTween.isAlive)
            {
                _playPulseTween.Stop();
            }
        }

        private void HandleProgressChanged(int unlockedLevel, IReadOnlyList<byte> starsByLevel)
        {
            _unlockedLevel = unlockedLevel;
            RefreshUnlockedLevel();
        }

        private void RefreshUnlockedLevel()
        {
            if (_playButtonLevelText != null)
            {
                _playButtonLevelText.SetText(PLAY_LEVEL_FORMAT, _unlockedLevel);
            }
        }

        private void StartFloatingPlayPulse()
        {
            if (_playFloatingButton == null)
            {
                return;
            }

            Transform tr = _playFloatingButton.transform;
            tr.localScale = Vector3.one;
            _playPulseTween = Tween.Scale(tr, 1.06f, 0.75f, Ease.InOutSine, -1, CycleMode.Yoyo);
        }

        private void HandlePlayFloatingClicked()
        {
            UiSignals.RaiseLevelNodeClicked(_unlockedLevel);
        }

        private void HandleDailyGiftClicked()
        {
            if (_dailyRewardPopup != null)
            {
                _dailyRewardPopup.Open();
                return;
            }

            UiSignals.RaiseOpenDailyReward();
        }

        private void HandleMapTabClicked()
        {
            // Already on map tab
        }

        private void HandleTeamTabClicked()
        {
            if (_infoModalPopup != null)
            {
                _infoModalPopup.Open("ROYAL TEAM", "Join a team, compete with friends and win 2,000 coins from the team chest every week!\n\n(Unlocks at Level 20 - Preview Active)");
            }
        }

        private void HandleInventoryTabClicked()
        {
            if (_inventoryPopup != null)
            {
                _inventoryPopup.Open();
                return;
            }

            UiSignals.RaiseOpenInventory();
        }

        private void HandleEventsTabClicked()
        {
            if (_infoModalPopup != null)
            {
                _infoModalPopup.Open("ROYAL TOURNAMENT",
                    "WEEKLY LEADERBOARD\n\n" +
                    "1. King George - 1,450 Points (Reward: 5,000 Coins)\n" +
                    "2. You (Player) - 980 Points (Reward: 2,500 Coins)\n" +
                    "3. Knight Arthur - 820 Points (Reward: 1,000 Coins)\n\n" +
                    "Complete levels to win trophies and climb the ranking!");
            }
        }

        private void HandleShopTabClicked()
        {
            if (_shopPopup != null)
            {
                _shopPopup.Open();
                return;
            }

            UiSignals.RaiseOpenShop();
        }
    }
}
