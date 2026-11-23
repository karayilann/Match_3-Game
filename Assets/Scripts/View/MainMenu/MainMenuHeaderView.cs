using PrimeTween;
using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View.MainMenu
{
    public sealed class MainMenuHeaderView : MonoBehaviour
    {
        [Header("Widgets & Buttons")]
        [SerializeField] private Button _coinsButton;
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private Button _livesButton;
        [SerializeField] private TextMeshProUGUI _livesText;
        [SerializeField] private Button _settingsButton;

        [Header("Popups Reference")]
        [SerializeField] private ShopPopupView _shopPopup;
        [SerializeField] private InfoModalPopupView _infoModalPopup;

        private int _coins = -1;
        private int _lives = 5;
        private int _maxLives = 5;
        private int _secondsToNextLife;
        private bool _isShowingLivesModal;

        private void Awake()
        {
            if (_coinsButton != null)
            {
                _coinsButton.onClick.AddListener(HandleCoinsClicked);
            }

            if (_livesButton != null)
            {
                _livesButton.onClick.AddListener(HandleLivesClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(HandleSettingsClicked);
            }

            RefreshVisuals();
        }

        private void OnEnable()
        {
            EconomySignals.OnCoinsChanged += HandleCoinsChanged;
            EconomySignals.OnLivesChanged += HandleLivesChanged;
            EconomySignals.OnLivesTimerChanged += HandleLivesTimerChanged;
            EconomySignals.OnNoLivesLeft += HandleLivesClicked;

            if (_infoModalPopup != null)
            {
                _infoModalPopup.OnClosed += HandleInfoModalClosed;
            }

            RefreshVisuals();
        }

        private void OnDisable()
        {
            EconomySignals.OnCoinsChanged -= HandleCoinsChanged;
            EconomySignals.OnLivesChanged -= HandleLivesChanged;
            EconomySignals.OnLivesTimerChanged -= HandleLivesTimerChanged;
            EconomySignals.OnNoLivesLeft -= HandleLivesClicked;

            if (_infoModalPopup != null)
            {
                _infoModalPopup.OnClosed -= HandleInfoModalClosed;
            }

            _isShowingLivesModal = false;
        }

        public void RefreshVisuals()
        {
            if (_coinsText != null && _coins >= 0)
            {
                _coinsText.text = _coins.ToString("N0");
            }

            if (_livesText != null)
            {
                _livesText.SetText("{0}", _lives);
            }
        }

        private void HandleCoinsChanged(int newCoins)
        {
            bool isRealChange = _coins >= 0 && newCoins != _coins;
            if (newCoins == _coins)
            {
                return;
            }

            _coins = newCoins;
            if (_coinsText == null)
            {
                return;
            }

            _coinsText.text = newCoins.ToString("N0");
            if (isRealChange)
            {
                _coinsText.transform.localScale = Vector3.one * 1.25f;
                Tween.Scale(_coinsText.transform, Vector3.one, 0.3f, Ease.OutBack);
            }
        }

        private void HandleLivesTimerChanged(int seconds)
        {
            _secondsToNextLife = seconds;
            UpdateLivesModalIfOpen();
        }

        private void HandleLivesChanged(int currentLives, int maxLives)
        {
            _lives = currentLives;
            _maxLives = maxLives;

            if (_livesText != null)
            {
                _livesText.SetText("{0}", currentLives);
            }

            UpdateLivesModalIfOpen();
        }

        private void HandleCoinsClicked()
        {
            if (_shopPopup != null)
            {
                _shopPopup.Open();
                return;
            }

            UiSignals.RaiseOpenShop();
        }

        private void HandleLivesClicked()
        {
            if (_infoModalPopup != null)
            {
                _isShowingLivesModal = true;
                _infoModalPopup.Open(BuildLivesTitle(), BuildLivesDescription());
            }
        }

        private void HandleInfoModalClosed()
        {
            _isShowingLivesModal = false;
        }

        private string BuildLivesTitle()
        {
            return "LIVES: " + _lives + "/" + _maxLives;
        }

        private string BuildLivesDescription()
        {
            string status = _lives >= _maxLives
                ? "Your lives are full!"
                : (_lives == 0 ? "Out of lives! " : string.Empty) + "Next life: " + (_secondsToNextLife / 60).ToString("00") + ":" + (_secondsToNextLife % 60).ToString("00");

            return status + "\n\nLosing a level costs 1 life. A life is restored every 30 minutes.";
        }

        private void UpdateLivesModalIfOpen()
        {
            if (!_isShowingLivesModal || _infoModalPopup == null)
            {
                return;
            }

            if (!_infoModalPopup.IsOpen || !_infoModalPopup.Title.StartsWith("CANLAR"))
            {
                _isShowingLivesModal = false;
                return;
            }

            _infoModalPopup.UpdateContent(BuildLivesTitle(), BuildLivesDescription());
        }

        private void HandleSettingsClicked()
        {
            UiSignals.RaiseOpenSettings();
        }
    }
}
