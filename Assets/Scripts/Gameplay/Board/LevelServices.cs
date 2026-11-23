using Core.Data.Obstacles;
using Gameplay.Goals;
using Gameplay.Obstacles;
using Gameplay.Scoring;

namespace Gameplay.Board
{
    public sealed class LevelServices
    {
        public readonly LevelLimitService Limit;
        public readonly ScoreService Score;
        public readonly GoalService Goal;
        public readonly ObstacleService ObstacleService;
        public readonly ObstacleDatabase ObstacleDatabase;

        public LevelServices(LevelLimitService limit, ScoreService score, GoalService goal, ObstacleService obstacleService, ObstacleDatabase obstacleDatabase)
        {
            Limit = limit;
            Score = score;
            Goal = goal;
            ObstacleService = obstacleService;
            ObstacleDatabase = obstacleDatabase;
        }
    }
}
