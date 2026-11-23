using System.Collections.Generic;
using Core.Data.Goals;
using Core.Data.Level;
using Gameplay.Board;
using Gameplay.Clock;
using Gameplay.Economy;
using Gameplay.Goals;
using Gameplay.Lives;
using Gameplay.Persistence;
using Gameplay.Progress;
using Gameplay.Settings;
using Signals;
using UnityEngine;
using View;
using View.LevelMap;

namespace Gameplay.Flow
{
    /// <summary>
    /// App-level composition root. It runs before the other scene scripts so the shared inventory exists before
    /// BoardController.Awake builds its booster service, whether the board starts active or inactive.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MenuFlowController : MonoBehaviour
    {
        private const int PREVIEW_GOAL_CAPACITY = 4;

        [Header("Canvases & Roots")]
        [SerializeField] private GameObject _mainMenuRoot;
        [SerializeField] private GameObject _gameplayRoot;
        [SerializeField] private BoardController _boardController;

        [Header("Level Database")]
        [SerializeField] private LevelData[] _levelDatabase;

        [Header("Popups")]
        [SerializeField] private LevelStartPopupView _levelStartPopup;

        [Header("Transition")]
        [SerializeField] private ScreenTransitionView _transition;

        private readonly List<LevelGoalEntry> _previewGoals = new List<LevelGoalEntry>(PREVIEW_GOAL_CAPACITY);
        private int _activeLevel = 1;
        private LivesController _lives;
        private ProgressService _progress;
        private SettingsService _settings;
        private PlayerInventory _inventory;
        private EconomyRequestHandler _economy;
        private bool _lifePending;
        private bool _isPlayingLevel;

        private void Awake()
        {
            IKeyValueStore store = new PlayerPrefsKeyValueStore();
            _lives = new LivesController(store, new SystemTimeProvider());
            _inventory = new PlayerInventory(store);
            _progress = new ProgressService(store, _levelDatabase != null ? _levelDatabase.Length : 0);
            _settings = new SettingsService(store);
            _settings.Subscribe();
            _economy = new EconomyRequestHandler(_inventory);
            _economy.Subscribe();

            if (_boardController != null)
            {
                _boardController.Construct(_inventory);
            }

            ShowMenu();
        }

        private void Start()
        {
            _lives.Publish();
            _inventory.BroadcastAll();
            _progress.Broadcast();
            _settings.Broadcast();
            TryResumeSavedLevel();
        }

        /// <summary>
        /// A save only survives when the app was closed mid-level, so launching straight back into that board
        /// avoids a start popup whose booster choice could not be applied to an already running level.
        /// </summary>
        private void TryResumeSavedLevel()
        {
            if (_levelDatabase == null)
            {
                return;
            }

            for (int levelIndex = _levelDatabase.Length; levelIndex >= 1; levelIndex--)
            {
                LevelData data = _levelDatabase[levelIndex - 1];
                if (data != null && SaveLoadService.HasSavedGame(data.LevelId))
                {
                    StartLevel(levelIndex, PreGameBoosterFlags.None);
                    return;
                }
            }
        }

        private void ClearSavedGameOf(int levelIndex)
        {
            LevelData data = GetLevelData(levelIndex);
            if (data != null)
            {
                SaveLoadService.ClearSavedGame(data.LevelId);
            }
        }

        private void Update()
        {
            _lives.Tick();
        }

        private void OnEnable()
        {
            UiSignals.OnLevelNodeClicked += HandleLevelNodeClicked;
            UiSignals.OnStartLevel += HandleStartLevel;
            UiSignals.OnStartLevelWithBoosters += HandleStartLevelWithBoosters;
            UiSignals.OnReturnToMenu += HandleReturnToMenu;
            UiSignals.OnLeaveLevelRequested += HandleLeaveLevelRequested;
            LevelSignals.OnLevelWon += HandleLevelWon;
            LevelSignals.OnLevelLimitReached += HandleLevelLost;
            LevelSignals.OnContinueWithMoves += HandleContinueWithMoves;
        }

        private void OnDisable()
        {
            UiSignals.OnLevelNodeClicked -= HandleLevelNodeClicked;
            UiSignals.OnStartLevel -= HandleStartLevel;
            UiSignals.OnStartLevelWithBoosters -= HandleStartLevelWithBoosters;
            UiSignals.OnReturnToMenu -= HandleReturnToMenu;
            UiSignals.OnLeaveLevelRequested -= HandleLeaveLevelRequested;
            LevelSignals.OnLevelWon -= HandleLevelWon;
            LevelSignals.OnLevelLimitReached -= HandleLevelLost;
            LevelSignals.OnContinueWithMoves -= HandleContinueWithMoves;
        }

        private void OnDestroy()
        {
            _economy?.Unsubscribe();
            _settings?.Unsubscribe();
        }

        /// <summary>
        /// The life is charged when the player leaves the defeat screen, not when it appears,
        /// so the "+5 moves" continue offer does not cost a life.
        /// </summary>
        private void HandleLevelLost()
        {
            _isPlayingLevel = false;
            _lifePending = true;
        }

        private void HandleContinueWithMoves(byte moves)
        {
            _isPlayingLevel = true;
            _lifePending = false;
        }

        public void ShowMenu()
        {
            _isPlayingLevel = false;

            if (_boardController != null)
            {
                _boardController.ClearBoard();
            }

            if (_mainMenuRoot != null)
            {
                _mainMenuRoot.SetActive(true);
            }

            if (_gameplayRoot != null)
            {
                _gameplayRoot.SetActive(false);
            }

            _progress.Broadcast();
            _lives.Publish();
            _inventory.BroadcastAll();
        }

        public void StartLevel(int levelIndex, PreGameBoosterFlags boosters)
        {
            if (!_lives.TryStartLevel())
            {
                return;
            }

            _activeLevel = levelIndex;
            _isPlayingLevel = true;
            _lifePending = false;

            if (_mainMenuRoot != null)
            {
                _mainMenuRoot.SetActive(false);
            }

            if (_gameplayRoot != null)
            {
                _gameplayRoot.SetActive(true);
            }

            LevelData data = GetLevelData(levelIndex);
            if (_boardController != null && data != null)
            {
                _boardController.LoadLevel(data, boosters);
            }

            _inventory.BroadcastAll();

            PlayTransition();
        }

        private void PlayTransition()
        {
            if (_transition != null)
            {
                _transition.PlayOpen();
            }
        }

        private LevelData GetLevelData(int levelIndex)
        {
            if (_levelDatabase == null || _levelDatabase.Length == 0)
            {
                return null;
            }

            int dataIndex = Mathf.Clamp(levelIndex - 1, 0, _levelDatabase.Length - 1);
            return _levelDatabase[dataIndex];
        }

        private void HandleLevelNodeClicked(int levelIndex)
        {
            if (_levelStartPopup == null)
            {
                return;
            }

            LevelData data = GetLevelData(levelIndex);
            _previewGoals.Clear();
            if (data != null)
            {
                GoalLoader.LoadGoals(data, _previewGoals);
            }

            _levelStartPopup.Open(levelIndex, data, _previewGoals);
        }

        private void HandleStartLevel(int levelIndex)
        {
            ClearSavedGameOf(levelIndex);
            StartLevel(levelIndex, PreGameBoosterFlags.None);
        }

        private void HandleStartLevelWithBoosters(int levelIndex, PreGameBoosterFlags boosters)
        {
            ClearSavedGameOf(levelIndex);
            StartLevel(levelIndex, boosters);
        }

        private void HandleLeaveLevelRequested()
        {
            if (_isPlayingLevel)
            {
                UiSignals.RaiseLeaveLevelConfirmRequested();
                return;
            }

            HandleReturnToMenu();
        }

        private void HandleReturnToMenu()
        {
            if (_isPlayingLevel)
            {
                _isPlayingLevel = false;
                _lifePending = false;

                if (_boardController != null)
                {
                    _boardController.ClearSavedGame();
                }

                _lives.ChargeLife();
            }
            else if (_lifePending)
            {
                _lifePending = false;
                _lives.ChargeLife();
            }

            ShowMenu();
            PlayTransition();
        }

        private void HandleLevelWon(int earnedStars)
        {
            _isPlayingLevel = false;
            _lifePending = false;
            _progress.RecordWin(_activeLevel, earnedStars);
        }
    }
}
