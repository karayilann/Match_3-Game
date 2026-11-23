using TMPro;
using UnityEngine;
using UnityEngine.UI;
using View.Popups;

namespace View.MainMenu
{
    public sealed class InfoModalPopupView : PopupView
    {
        [Header("Content")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private Button _closeButton;

        public string Title => _titleText != null ? _titleText.text : string.Empty;

        protected override void Awake()
        {
            base.Awake();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }
        }

        public void Open(string title, string description)
        {
            UpdateContent(title, description);
            Open();
        }

        public void UpdateContent(string title, string description)
        {
            if (_titleText != null)
            {
                _titleText.text = title;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = description;
            }
        }
    }
}
