using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View.LevelMap
{
    public sealed class LevelGoalCardView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private TMP_Text _labelText;

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        public void Bind(Sprite iconSprite, byte targetCount, string label)
        {
            _iconImage.sprite = iconSprite;
            _countText.SetText("{0:0}", targetCount);
            _labelText.text = label;
        }
    }
}
