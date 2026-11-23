using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View.HUD
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _limitValueText;
        [SerializeField] private Button _settingsButton;

        private void Awake()
        {
            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(HandleSettingsClicked);
            }
        }

        private void OnDestroy()
        {
            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(HandleSettingsClicked);
            }
        }

        private void HandleSettingsClicked()
        {
            UiSignals.RaiseOpenSettings();
        }

        private void OnEnable()
        {
            LevelSignals.OnScoreChanged += HandleScoreChanged;
            LevelSignals.OnMovesChanged += HandleMovesChanged;
            LevelSignals.OnTimeChanged += HandleTimeChanged;
        }

        private void OnDisable()
        {
            LevelSignals.OnScoreChanged -= HandleScoreChanged;
            LevelSignals.OnMovesChanged -= HandleMovesChanged;
            LevelSignals.OnTimeChanged -= HandleTimeChanged;
        }

        private void HandleScoreChanged(int score)
        {
            _scoreText.SetText("{0}", score);
        }

        private void HandleMovesChanged(byte remainingMoves)
        {
            _limitValueText.SetText("{0}", remainingMoves);
        }

        private void HandleTimeChanged(int remainingSeconds)
        {
            _limitValueText.SetText("{0}", remainingSeconds);
        }
    }
}
