using PrimeTween;
using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View.LevelMap
{
    public sealed class LevelNodeView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _pedestalImage;
        [SerializeField] private TextMeshProUGUI _levelNumberText;
        [SerializeField] private Image[] _stars;
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private Color _activeStarColor = Color.white;
        [SerializeField] private Color _inactiveStarColor = new Color(0.25f, 0.25f, 0.25f, 0.45f);

        [Header("Sprites")]
        [SerializeField] private Sprite _normalPedestal;
        [SerializeField] private Sprite _currentPedestal;

        private int _levelIndex;
        private bool _isUnlocked;

        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
            }

            Tween.CompleteAll(transform);
        }

        public void Bind(int levelIndex, bool isUnlocked, bool isCurrent, int earnedStars)
        {
            _levelIndex = levelIndex;
            _isUnlocked = isUnlocked;

            if (_levelNumberText != null)
            {
                _levelNumberText.SetText("{0}", levelIndex);
            }

            if (_button != null)
            {
                _button.interactable = isUnlocked;
            }

            if (_pedestalImage != null)
            {
                _pedestalImage.sprite = isCurrent ? _currentPedestal : _normalPedestal;
                _pedestalImage.color = isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.85f);
            }

            if (_lockIcon != null)
            {
                _lockIcon.SetActive(!isUnlocked);
            }

            if (_stars != null)
            {
                for (int i = 0; i < _stars.Length; i++)
                {
                    if (_stars[i] != null)
                    {
                        _stars[i].gameObject.SetActive(isUnlocked);
                        _stars[i].color = i < earnedStars ? _activeStarColor : _inactiveStarColor;
                    }
                }
            }

            Tween.CompleteAll(transform);
            if (isCurrent)
            {
                transform.localScale = Vector3.one;
                Tween.Scale(transform, new Vector3(1.1f, 1.1f, 1f), 0.7f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }

        private void HandleClicked()
        {
            if (!_isUnlocked)
            {
                return;
            }

            UiSignals.RaiseLevelNodeClicked(_levelIndex);
        }
    }
}
