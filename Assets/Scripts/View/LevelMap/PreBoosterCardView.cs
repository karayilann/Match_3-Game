using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View.LevelMap
{
    public sealed class PreBoosterCardView : MonoBehaviour
    {
        private const string SELECTED_STATE_TEXT = "SELECTED";
        private const string AVAILABLE_STATE_TEXT = "Tap to select";
        private const string EMPTY_STATE_TEXT = "Out of stock";
        private const float PUNCH_DURATION = 0.2f;

        private static readonly Color EMPTY_TINT = new Color(0.55f, 0.55f, 0.55f, 1f);
        private static readonly Color COUNT_BADGE_COLOR = new Color(1f, 0.3f, 0.3f, 1f);
        private static readonly Vector3 PUNCH_STRENGTH = new Vector3(0.12f, 0.12f, 0f);

        [SerializeField] private Button _button;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _countBadgeImage;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private TMP_Text _stateText;
        [SerializeField] private GameObject _checkBadge;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _selectedSprite;

        public Button Button => _button;

        /// <summary>
        /// A selected booster stays clickable at zero stock so the player can always undo the choice;
        /// only an unselected empty booster is locked.
        /// </summary>
        public void Bind(int count, bool isSelected)
        {
            bool isEmpty = count <= 0 && !isSelected;

            _backgroundImage.sprite = isSelected ? _selectedSprite : _normalSprite;
            _backgroundImage.color = isEmpty ? EMPTY_TINT : Color.white;
            _iconImage.color = isEmpty ? EMPTY_TINT : Color.white;
            _countBadgeImage.color = isEmpty ? EMPTY_TINT : COUNT_BADGE_COLOR;
            _countText.SetText("{0:0}", count);
            _stateText.text = isSelected ? SELECTED_STATE_TEXT : isEmpty ? EMPTY_STATE_TEXT : AVAILABLE_STATE_TEXT;
            _checkBadge.SetActive(isSelected);
            _button.interactable = !isEmpty;
        }

        public void Punch()
        {
            Transform cardTransform = transform;
            Tween.CompleteAll(cardTransform);
            Tween.PunchScale(cardTransform, PUNCH_STRENGTH, PUNCH_DURATION);
        }
    }
}
