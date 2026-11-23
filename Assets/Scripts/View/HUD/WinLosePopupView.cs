using PrimeTween;
using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace View.HUD
{
    public sealed class WinLosePopupView : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private GameObject _popupRoot;
        [SerializeField] private RectTransform _dialogContainer;

        [Header("Ribbon & Titles")]
        [SerializeField] private Image _ribbonImage;
        [SerializeField] private Sprite _victoryRibbon;
        [SerializeField] private Sprite _defeatRibbon;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _scoreText;

        [Header("Stars")]
        [SerializeField] private Image[] _stars;
        [SerializeField] private Color _activeStarColor = Color.white;
        [SerializeField] private Color _inactiveStarColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        [Header("Buttons")]
        [SerializeField] private Button _primaryButton;
        [SerializeField] private Button _continueWithMovesButton;
        [SerializeField] private Button _mapButton;
        [SerializeField] private TextMeshProUGUI _primaryButtonText;

        private Image[] _starBackgrounds;
        private Sequence _starSequence;
        private Tween _popInTween;
        private bool _backgroundsInitialized;
        private int _currentScore;

        private void Awake()
        {
            if (_popupRoot != null)
            {
                _popupRoot.SetActive(false);
            }

            if (_primaryButton != null)
            {
                _primaryButton.onClick.AddListener(HandlePrimaryButtonClicked);
            }

            if (_continueWithMovesButton != null)
            {
                _continueWithMovesButton.onClick.AddListener(HandleContinueClicked);
            }

            if (_mapButton != null)
            {
                _mapButton.onClick.AddListener(HandleMapButtonClicked);
            }

            InitializeStarBackgrounds();
        }

        private void InitializeStarBackgrounds()
        {
            if (_backgroundsInitialized || _stars == null || _stars.Length == 0)
            {
                return;
            }

            _backgroundsInitialized = true;
            _starBackgrounds = new Image[_stars.Length];

            for (int i = 0; i < _stars.Length; i++)
            {
                Image star = _stars[i];
                if (star == null)
                {
                    continue;
                }

                GameObject bgObj = new GameObject(star.gameObject.name + "_Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                RectTransform starRect = star.rectTransform;

                bgRect.SetParent(starRect.parent, false);
                bgRect.SetSiblingIndex(starRect.GetSiblingIndex());
                bgRect.anchorMin = starRect.anchorMin;
                bgRect.anchorMax = starRect.anchorMax;
                bgRect.pivot = starRect.pivot;
                bgRect.anchoredPosition = starRect.anchoredPosition;
                bgRect.sizeDelta = starRect.sizeDelta;
                bgRect.localRotation = starRect.localRotation;
                bgRect.localScale = Vector3.one;

                Image bgImage = bgObj.GetComponent<Image>();
                bgImage.sprite = star.sprite;
                bgImage.color = _inactiveStarColor;
                bgImage.raycastTarget = false;

                _starBackgrounds[i] = bgImage;
            }
        }

        private void OnEnable()
        {
            LevelSignals.OnLevelWon += ShowVictory;
            LevelSignals.OnLevelLimitReached += ShowDefeat;
            LevelSignals.OnScoreChanged += HandleScoreChanged;
        }

        private void OnDisable()
        {
            LevelSignals.OnLevelWon -= ShowVictory;
            LevelSignals.OnLevelLimitReached -= ShowDefeat;
            LevelSignals.OnScoreChanged -= HandleScoreChanged;

            if (_starSequence.isAlive)
            {
                _starSequence.Stop();
            }

            if (_popInTween.isAlive)
            {
                _popInTween.Stop();
            }
        }

        private void OnDestroy()
        {
            if (_starSequence.isAlive)
            {
                _starSequence.Stop();
            }

            if (_popInTween.isAlive)
            {
                _popInTween.Stop();
            }
        }

        private void HandleScoreChanged(int score)
        {
            _currentScore = score;
            if (_popupRoot != null && _popupRoot.activeSelf && _scoreText != null)
            {
                _scoreText.SetText("{0}", _currentScore);
            }
        }

        public void ShowVictory(int earnedStars)
        {
            if (_popupRoot == null)
            {
                return;
            }

            _popupRoot.SetActive(true);

            if (_ribbonImage != null && _victoryRibbon != null)
            {
                _ribbonImage.sprite = _victoryRibbon;
            }

            if (_titleText != null)
            {
                _titleText.text = "LEVEL COMPLETE!";
            }

            if (_primaryButtonText != null)
            {
                _primaryButtonText.text = "NEXT";
            }

            if (_continueWithMovesButton != null)
            {
                _continueWithMovesButton.gameObject.SetActive(false);
            }

            if (_scoreText != null)
            {
                _scoreText.SetText("{0}", _currentScore);
            }

            AnimatePopIn();
            SetStars(earnedStars, true);
        }

        public void ShowVictory()
        {
            ShowVictory(3);
        }

        public void ShowDefeat()
        {
            if (_popupRoot == null)
            {
                return;
            }

            _popupRoot.SetActive(true);

            if (_ribbonImage != null && _defeatRibbon != null)
            {
                _ribbonImage.sprite = _defeatRibbon;
            }

            if (_titleText != null)
            {
                _titleText.text = "OUT OF MOVES!";
            }

            if (_scoreText != null)
            {
                _scoreText.SetText("{0}", _currentScore);
            }

            if (_primaryButtonText != null)
            {
                _primaryButtonText.text = "TRY AGAIN";
            }

            if (_continueWithMovesButton != null)
            {
                _continueWithMovesButton.gameObject.SetActive(true);
            }

            AnimatePopIn();
            SetStars(0, false);
        }

        private void SetStars(int earnedStars, bool animate = true)
        {
            if (_starSequence.isAlive)
            {
                _starSequence.Stop();
            }

            if (_stars == null || _stars.Length == 0)
            {
                return;
            }

            if (_starBackgrounds == null)
            {
                InitializeStarBackgrounds();
            }

            if (!animate || earnedStars <= 0)
            {
                for (int i = 0; i < _stars.Length; i++)
                {
                    if (_stars[i] != null)
                    {
                        bool isActive = i < earnedStars;
                        _stars[i].color = _activeStarColor;
                        _stars[i].rectTransform.localScale = isActive ? Vector3.one : Vector3.zero;
                        _stars[i].rectTransform.localEulerAngles = Vector3.zero;
                    }
                }
                return;
            }

            for (int i = 0; i < _stars.Length; i++)
            {
                if (_stars[i] != null)
                {
                    _stars[i].color = _activeStarColor;
                    _stars[i].rectTransform.localScale = Vector3.zero;
                    _stars[i].rectTransform.localEulerAngles = Vector3.zero;
                }
            }

            _starSequence = Sequence.Create();
            float baseDelay = 0.35f;

            for (int i = 0; i < earnedStars && i < _stars.Length; i++)
            {
                int starIndex = i;
                Image star = _stars[starIndex];
                if (star == null)
                {
                    continue;
                }

                RectTransform starRect = star.rectTransform;
                float starStartTime = baseDelay + (starIndex * 0.18f);

                _starSequence.InsertCallback(starStartTime, () =>
                {
                    LevelSignals.RaiseStarAwarded(starIndex);
                });

                _starSequence.Insert(starStartTime, Tween.Scale(starRect, Vector3.zero, Vector3.one, 0.34f, Ease.OutBack));

                if (starIndex == 0)
                {
                    _starSequence.Insert(starStartTime, Tween.PunchLocalRotation(starRect, new Vector3(0, 0, -14f), 0.34f));
                }
                else if (starIndex == 1)
                {
                    _starSequence.Insert(starStartTime, Tween.PunchLocalPosition(starRect, new Vector3(0f, 18f, 0f), 0.34f));
                    _starSequence.Insert(starStartTime + 0.34f, Tween.PunchScale(starRect, new Vector3(0.2f, 0.2f, 0f), 0.25f));
                }
                else if (starIndex == 2)
                {
                    _starSequence.Insert(starStartTime, Tween.PunchLocalRotation(starRect, new Vector3(0, 0, 14f), 0.34f));
                }
            }
        }

        private void AnimatePopIn()
        {
            if (_popInTween.isAlive)
            {
                _popInTween.Stop();
            }

            if (_dialogContainer != null)
            {
                _dialogContainer.localScale = Vector3.zero;
                _popInTween = Tween.Scale(_dialogContainer, Vector3.one, 0.35f, Ease.OutBack);
            }
        }

        private void HandlePrimaryButtonClicked()
        {
            if (_starSequence.isAlive)
            {
                _starSequence.Stop();
            }
            if (_popInTween.isAlive)
            {
                _popInTween.Stop();
            }
            if (_popupRoot != null)
            {
                _popupRoot.SetActive(false);
            }
            UiSignals.RaiseReturnToMenu();
        }

        private void HandleMapButtonClicked()
        {
            if (_starSequence.isAlive)
            {
                _starSequence.Stop();
            }
            if (_popInTween.isAlive)
            {
                _popInTween.Stop();
            }
            if (_popupRoot != null)
            {
                _popupRoot.SetActive(false);
            }
            UiSignals.RaiseReturnToMenu();
        }

        private void HandleContinueClicked()
        {
            if (_starSequence.isAlive)
            {
                _starSequence.Stop();
            }
            if (_popInTween.isAlive)
            {
                _popInTween.Stop();
            }
            if (_popupRoot != null)
            {
                _popupRoot.SetActive(false);
            }

            LevelSignals.RaiseContinueWithMoves(5);
        }
    }
}
