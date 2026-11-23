using Core.Data.Goals;
using Core.Data.Obstacles;
using Core.Data.Tiles;
using UnityEngine;

namespace Core.Data.Level
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "Match3/Level Data")]
    public sealed class LevelData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private int _levelId;
        [SerializeField] private string _levelName; // Not for display, just for internal reference
        [SerializeField] private int _levelOrder;

        [Header("Board")]
        [SerializeField] private byte _width = 8;
        [SerializeField] private byte _height = 8;
        [SerializeField] private byte _activeColorCount = 6;
        [SerializeField] private TextAsset _nonPlayableDataFile;
        [SerializeField] private GridCoord[] _nonPlayableCells;

        [Header("Limit")]
        [SerializeField] private LevelLimitType _limitType;
        [SerializeField] private byte _moveLimit = 20;
        [SerializeField] private float _timeLimitSeconds = 60f;

        [Header("Scoring")]
        [SerializeField] private int _pointsPerTile = 10;
        [SerializeField] private int _targetScore1Star = 500;
        [SerializeField] private int _targetScore2Star = 1200;
        [SerializeField] private int _targetScore3Star = 2200;

        [Header("Goals")]
        [SerializeField] private TextAsset _goalDataFile;
        [SerializeField] private LevelGoalEntry[] _goals;

        [Header("Hints")]
        [SerializeField] private bool _enableHints = true;
        [SerializeField] private float _hintDelaySeconds = 5f;

        [Header("Obstacles")]
        [SerializeField] private bool _useRandomPlacement;
        [SerializeField] private ObstacleType _randomObstacleType;
        [SerializeField] private byte _randomObstacleCount;
        [SerializeField] private TextAsset _obstacleDataFile;
        [SerializeField] private ObstaclePlacementEntry[] _obstaclePlacements;

        public byte Width => _width;
        public byte Height => _height;
        public byte ActiveColorCount => _activeColorCount;
        public TextAsset NonPlayableDataFile => _nonPlayableDataFile;
        public GridCoord[] NonPlayableCells => _nonPlayableCells;
        public int PointsPerTile => _pointsPerTile;
        public int TargetScore1Star => _targetScore1Star > 0 ? _targetScore1Star : 500;
        public int TargetScore2Star => _targetScore2Star > 0 ? _targetScore2Star : 1200;
        public int TargetScore3Star => _targetScore3Star > 0 ? _targetScore3Star : 2200;
        public TextAsset GoalDataFile => _goalDataFile;
        public LevelGoalEntry[] Goals => _goals;
        public bool EnableHints => _enableHints;
        public float HintDelaySeconds => _hintDelaySeconds;
        public bool UseRandomPlacement => _useRandomPlacement;
        public ObstacleType RandomObstacleType => _randomObstacleType;
        public byte RandomObstacleCount => _randomObstacleCount;
        public TextAsset ObstacleDataFile => _obstacleDataFile;
        public ObstaclePlacementEntry[] ObstaclePlacements => _obstaclePlacements;
        
        public int LevelId => _levelId;
        public string LevelName => _levelName;
        public int LevelOrder => _levelOrder;
        public LevelLimitType LimitType => _limitType;
        public byte MoveLimit => _moveLimit;
        public float TimeLimitSeconds => _timeLimitSeconds;
    }
}
