using Signals;
using UnityEngine;
using UnityEngine.UI;

namespace View.Popups
{
    public sealed class LeaveLevelPopupView : PopupView
    {
        [Header("Buttons")]
        [SerializeField] private Button _stayButton;
        [SerializeField] private Button _leaveButton;

        protected override void Awake()
        {
            base.Awake();

            if (_stayButton != null)
            {
                _stayButton.onClick.AddListener(Close);
            }

            if (_leaveButton != null)
            {
                _leaveButton.onClick.AddListener(HandleLeaveClicked);
            }
        }

        private void OnEnable()
        {
            UiSignals.OnLeaveLevelConfirmRequested += Open;
        }

        private void OnDisable()
        {
            UiSignals.OnLeaveLevelConfirmRequested -= Open;
        }

        private void HandleLeaveClicked()
        {
            Close();
            UiSignals.RaiseReturnToMenu();
        }
    }
}
