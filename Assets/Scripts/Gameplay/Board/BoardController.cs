using System.Collections.Generic;
using Core.Data.Goals;
using Core.Data.Level;
using Core.Data.Obstacles;
using Core.Data.Tiles;
using Core.Pooling;
using Core.StateMachine;
using Gameplay.Animation;
using Gameplay.Boosters;
using Gameplay.Economy;
using Gameplay.Flow;
using Gameplay.Goals;
using Gameplay.Hints;
using Gameplay.Ingredients;
using Gameplay.Input;
using Gameplay.Matching;
using Gameplay.Obstacles;
using Gameplay.Persistence;
using Gameplay.Scoring;
using Gameplay.Spawning;
using Gameplay.States;
using Signals;
using UnityEngine;
using View;

namespace Gameplay.Board
{
    public sealed class BoardController : MonoBehaviour
    {
        private const byte MIN_COLOR_COUNT = 3;
        private const int INITIAL_GOAL_CAPACITY = 4;
        private const float FINALE_SKIP_TIME_SCALE = 5f;
        private const int GEM_PREWARM_COUNT = 81;
        private const int PARTICLE_PREWARM_COUNT = 32;
        private const int ROCK_BREAK_PREWARM_COUNT = 8;

        [SerializeField] private GemView _gemPrefab;
        [SerializeField] private MatchParticleView _particlePrefab;
        [SerializeField] private MatchParticleView _rockBreakParticlePrefab;
        [SerializeField] private Sprite _cellBackgroundSprite;
        [SerializeField] private Transform _poolParent;
        [SerializeField] private TileSpriteDatabase _spriteDatabase;
        [SerializeField] private CameraFitter _cameraFitter;
        [SerializeField] private BoardInputHandler _input;
        [SerializeField] private BoardAnimator _animator;
        [SerializeField] private ObstacleDatabase _obstacleDatabase;
        [SerializeField] private LevelData _levelData;
        [SerializeField] private float _cellSize = 1f;

        private BoardModel _boardModel;
        private GameStateMachine _stateMachine;
        private BoardStates _states;
        private IdleState _idleState;
        private MatchState _matchState;
        private ReshuffleState _reshuffleState;
        private LevelLimitService _limitService;
        private ScoreService _scoreService;
        private GoalService _goalService;
        private HintService _hintService;
        private BoosterService _boosterService;
        private BoardData _boardData;
        private GemView[] _views;
        private PoolManager<GemView> _pool;
        private PoolManager<MatchParticleView> _particlePool;
        private PoolManager<MatchParticleView> _rockBreakParticlePool;
        private ObstacleService _obstacleService;
        private GamePersistenceController _persistence;
        private BoosterController _boosterController;
        private BoardBackgroundPlacer _backgroundPlacer;
        private PlayerInventory _inventory;

        public void Construct(PlayerInventory inventory)
        {
            _inventory = inventory;
        }

        private void Awake()
        {
            _persistence = new GamePersistenceController();
            _pool = new PoolManager<GemView>(_gemPrefab, _poolParent, GEM_PREWARM_COUNT);
            _particlePool = _particlePrefab != null
                ? new PoolManager<MatchParticleView>(_particlePrefab, _poolParent, PARTICLE_PREWARM_COUNT)
                : null;
            _rockBreakParticlePool = _rockBreakParticlePrefab != null
                ? new PoolManager<MatchParticleView>(_rockBreakParticlePrefab, _poolParent, ROCK_BREAK_PREWARM_COUNT)
                : null;

            _stateMachine = new GameStateMachine();
            _boosterService = new BoosterService(_inventory);
            _boosterController = new BoosterController(_boosterService);
            _backgroundPlacer = new BoardBackgroundPlacer(_poolParent);
        }

        public void LoadLevel(LevelData levelData, PreGameBoosterFlags preGameBoosters)
        {
            ClearBoard();
            _levelData = levelData;
            InitLevel(levelData, preGameBoosters);
        }

        public void ClearBoard()
        {
            _input.Deactivate();

            if (_views != null && _pool != null)
            {
                for (int i = 0; i < _views.Length; i++)
                {
                    if (_views[i] != null)
                    {
                        _pool.Release(_views[i]);
                        _views[i] = null;
                    }
                }
            }

            _backgroundPlacer?.Clear();
        }

        private void InitLevel(LevelData levelData, PreGameBoosterFlags preGameBoosters)
        {
            _levelData = levelData;
            byte width = _levelData.Width;
            byte height = _levelData.Height;
            int totalCells = width * height;
            byte maxColorCount = (byte)TileTypeLibrary.PLAYABLE_TYPES.Length;
            byte activeColorCount = (byte)Mathf.Clamp(_levelData.ActiveColorCount, MIN_COLOR_COUNT, maxColorCount);

            _boardModel = new BoardModel(width, height);
            GridToWorldConverter converter = new GridToWorldConverter(width, height, _cellSize);
            _views = new GemView[totalCells];
            IMatchDetector matchDetector = new MatchDetector(width, height);
            IPossibleMoveDetector possibleMoveDetector = new PossibleMoveDetector(width, height, matchDetector);
            _obstacleService = new ObstacleService(width, height, _obstacleDatabase);
            _limitService = new LevelLimitService(_levelData);
            _scoreService = new ScoreService(_levelData.PointsPerTile, _levelData.TargetScore1Star, _levelData.TargetScore2Star, _levelData.TargetScore3Star);
            List<LevelGoalEntry> goals = new List<LevelGoalEntry>(INITIAL_GOAL_CAPACITY);
            GoalLoader.LoadGoals(_levelData, goals);
            _goalService = new GoalService(goals);

            _animator.Init(totalCells);

            TileSpawner spawner = new TileSpawner(_boardModel, converter, _pool, _spriteDatabase, _obstacleDatabase, _views, activeColorCount);
            _boardData = new BoardData(_boardModel, converter, _views, matchDetector, activeColorCount);
            BoardPresentation presentation = new BoardPresentation(_animator, _pool, _particlePool, _rockBreakParticlePool, _spriteDatabase);
            LevelServices services = new LevelServices(_limitService, _scoreService, _goalService, _obstacleService, _obstacleDatabase);

            _cameraFitter.Fit(width, height, _cellSize);
            _input.Init(converter);

            _hintService = new HintService(_boardModel, possibleMoveDetector, _views, _animator, _levelData);
            _idleState = new IdleState(_input, _hintService);

            _states = new BoardStates(_stateMachine);
            _reshuffleState = new ReshuffleState(_boardData, presentation, _states, possibleMoveDetector);
            SpecialTileChoreographer choreographer = new SpecialTileChoreographer(_boardData, presentation);
            WinFinaleController finale = new WinFinaleController(_boardData, presentation, services);
            _matchState = new MatchState(_boardData, presentation, services, _states, possibleMoveDetector, choreographer, finale);
            FillState fillState = new FillState(_boardData, presentation, services, _states);
            FallState fallState = new FallState(_boardData, presentation, _states);
            SwapState swapState = new SwapState(_boardData, presentation, services, _states, _input);
            _states.Init(_idleState, swapState, _matchState, fallState, fillState, _reshuffleState);

            _boosterController.Bind(_boardData, presentation, services, _states, _input);
            _boosterService.ResetForLevel();

            BoardShapePlacer.ApplyNonPlayableCells(_boardModel, _levelData);
            _backgroundPlacer.SpawnGridBackground(_boardModel, converter, _cellBackgroundSprite, _cellSize);

            byte[] goalScratch = new byte[goals.Count];
            _persistence.Bind(_levelData, _boardModel, _limitService, _scoreService, _goalService);
            bool loaded = _persistence.TryLoad(goalScratch);

            if (loaded)
            {
                spawner.SpawnViewsFromModel();
            }
            else
            {
                spawner.SpawnBoard();
                ObstaclePlacer.PlaceObstacles(_boardData, _obstacleDatabase, _levelData);
                IngredientService.PlaceInitial(_boardModel, _views, _goalService, _spriteDatabase);

                PreGameBoosterApplier.Apply(_inventory.ConsumePreBoosters(preGameBoosters), _boardData, _spriteDatabase, _limitService);
            }

            LevelSignals.RaiseScoreTargetsConfigured(_levelData.TargetScore1Star, _levelData.TargetScore2Star, _levelData.TargetScore3Star);
            _limitService.BroadcastInitialState();
            _scoreService.BroadcastInitialState();
            _goalService.BroadcastInitialState();
            _stateMachine.ChangeState(loaded ? _states.Fall : _matchState);
        }

        private void Start()
        {
            if (_matchState != null && _stateMachine != null && _stateMachine.CurrentState == null)
            {
                _stateMachine.ChangeState(_matchState);
            }
        }

        private void Update()
        {
            if (_stateMachine != null)
            {
                _stateMachine.Tick();
            }

            if (_limitService != null && _limitService.TickTime(Time.deltaTime))
            {
                _input.SetInputEnabled(false);
            }
        }

        private void OnEnable()
        {
            _persistence?.Subscribe();
            LevelSignals.OnContinueWithMoves += HandleContinueWithMoves;
            LevelSignals.OnWinFinaleStarted += HandleWinFinaleStarted;
            LevelSignals.OnFinaleSkipRequested += HandleFinaleSkipRequested;
            BoardSignals.OnGoalTileCollected += HandleGoalTileCollected;
            _boosterController?.Subscribe();
        }

        private void OnDisable()
        {
            _persistence?.Unsubscribe();
            LevelSignals.OnContinueWithMoves -= HandleContinueWithMoves;
            LevelSignals.OnWinFinaleStarted -= HandleWinFinaleStarted;
            LevelSignals.OnFinaleSkipRequested -= HandleFinaleSkipRequested;
            BoardSignals.OnGoalTileCollected -= HandleGoalTileCollected;
            _boosterController?.Unsubscribe();
        }

        private void HandleWinFinaleStarted()
        {
            _input.SetFinaleSkipEnabled(true);
        }

        private void HandleFinaleSkipRequested()
        {
            _input.SetFinaleSkipEnabled(false);
            _animator.SetTimeScale(FINALE_SKIP_TIME_SCALE);
        }

        private void HandleGoalTileCollected(GridCoord coord, TileType type)
        {
            if (_boardData != null)
            {
                BoardSignals.RaiseGoalTileFlyRequested(_boardData.Converter.ToWorldPosition(coord), type);
            }
        }

        private void OnDestroy()
        {
            _hintService?.Dispose();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveIfIdle();
            }
        }

        private void OnApplicationQuit()
        {
            SaveIfIdle();
        }

        /// <summary>
        /// Pause and quit can arrive mid-animation, when the model holds gaps and a spent move that the
        /// views have not caught up with. Only an idle board is a consistent snapshot, otherwise the
        /// previous settled save is kept.
        /// </summary>
        private void SaveIfIdle()
        {
            if (_stateMachine != null && _stateMachine.CurrentState == _idleState)
            {
                _persistence.Save();
            }
        }

        public void ClearSavedGame()
        {
            _persistence.Clear();
        }

        private void HandleContinueWithMoves(byte extraMoves)
        {
            _limitService.ApplyContinueBonus(extraMoves);
            _input.SetInputEnabled(true);
            _stateMachine.ChangeState(_idleState);
            _persistence.Save();
        }
    }
}
