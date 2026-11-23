using Signals;
using UnityEngine;
using UnityEngine.UI;
using View.Popups;

namespace View.Settings
{
    public sealed class SettingsPopupView : PopupView
    {
        [Header("Buttons")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _sfxToggleButton;
        [SerializeField] private Image _sfxCheckmark;
        [SerializeField] private Button _musicToggleButton;
        [SerializeField] private Image _musicCheckmark;
        [SerializeField] private Button _hapticsToggleButton;
        [SerializeField] private Image _hapticsCheckmark;
        [SerializeField] private Button _homeButton;

        private bool _sfxEnabled = true;
        private bool _musicEnabled = true;
        private bool _hapticsEnabled = true;

        protected override void Awake()
        {
            base.Awake();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }

            if (_sfxToggleButton != null)
            {
                _sfxToggleButton.onClick.AddListener(ToggleSfx);
            }

            if (_musicToggleButton != null)
            {
                _musicToggleButton.onClick.AddListener(ToggleMusic);
            }

            if (_hapticsToggleButton != null)
            {
                _hapticsToggleButton.onClick.AddListener(ToggleHaptics);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.AddListener(HandleHomeClicked);
            }

            UpdateVisuals();
        }

        private void OnEnable()
        {
            UiSignals.OnOpenSettings += Open;
            UiSignals.OnSettingsLoaded += HandleSettingsLoaded;
        }

        private void OnDisable()
        {
            UiSignals.OnOpenSettings -= Open;
            UiSignals.OnSettingsLoaded -= HandleSettingsLoaded;
        }

        private void HandleSettingsLoaded(bool isSfxEnabled, bool isMusicEnabled, bool isHapticsEnabled)
        {
            _sfxEnabled = isSfxEnabled;
            _musicEnabled = isMusicEnabled;
            _hapticsEnabled = isHapticsEnabled;
            UpdateVisuals();
        }

        public override void Open()
        {
            base.Open();
            UpdateVisuals();
        }

        private void ToggleSfx()
        {
            _sfxEnabled = !_sfxEnabled;
            UiSignals.RaiseSfxToggled(_sfxEnabled);
            UpdateVisuals();
        }

        private void ToggleMusic()
        {
            _musicEnabled = !_musicEnabled;
            UiSignals.RaiseMusicToggled(_musicEnabled);
            UpdateVisuals();
        }

        private void ToggleHaptics()
        {
            _hapticsEnabled = !_hapticsEnabled;
            UiSignals.RaiseHapticsToggled(_hapticsEnabled);
            UpdateVisuals();
        }

        private void HandleHomeClicked()
        {
            Close();
            UiSignals.RaiseLeaveLevelRequested();
        }

        private void UpdateVisuals()
        {
            if (_sfxCheckmark != null)
            {
                _sfxCheckmark.gameObject.SetActive(_sfxEnabled);
            }

            if (_musicCheckmark != null)
            {
                _musicCheckmark.gameObject.SetActive(_musicEnabled);
            }

            if (_hapticsCheckmark != null)
            {
                _hapticsCheckmark.gameObject.SetActive(_hapticsEnabled);
            }
        }
    }
}
