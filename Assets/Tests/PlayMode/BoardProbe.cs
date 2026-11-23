using System.Reflection;
using Core.Data.Tiles;
using Core.StateMachine;
using Gameplay.Board;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// Reads the controller's private runtime state so tests can check the board without adding test-only API to production code.
    /// </summary>
    public sealed class BoardProbe
    {
        private readonly BoardController _controller;

        public BoardProbe(BoardController controller)
        {
            _controller = controller;
        }

        public BoardModel Model => Get<BoardModel>("_boardModel");
        public GameStateMachine StateMachine => Get<GameStateMachine>("_stateMachine");
        public GemView[] Views => Get<GemView[]>("_views");
        public LevelLimitService Limit => Get<LevelLimitService>("_limitService");

        public bool IsIdle => StateMachine != null && StateMachine.CurrentState != null && StateMachine.CurrentState == Get<IGameState>("_idleState");

        public string StateName => StateMachine?.CurrentState != null ? StateMachine.CurrentState.GetType().Name : "none";

        public void EnterReshuffle()
        {
            StateMachine.ChangeState(Get<IGameState>("_reshuffleState"));
        }

        public void EnterMatch()
        {
            StateMachine.ChangeState(Get<IGameState>("_matchState"));
        }

        public int CountEmptyPlayableCells()
        {
            int empty = 0;
            BoardModel model = Model;
            for (byte y = 0; y < model.Height; y++)
            {
                for (byte x = 0; x < model.Width; x++)
                {
                    ref GridCell cell = ref model.GetCell(new GridCoord(x, y));
                    if (cell.IsPlayable && cell.Type == TileType.None)
                    {
                        empty++;
                    }
                }
            }

            return empty;
        }

        public int CountCellsMissingViews()
        {
            int missing = 0;
            BoardModel model = Model;
            GemView[] views = Views;
            for (byte y = 0; y < model.Height; y++)
            {
                for (byte x = 0; x < model.Width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    ref GridCell cell = ref model.GetCell(coord);
                    if (cell.IsPlayable && cell.Type != TileType.None && views[model.ToIndex(coord)] == null)
                    {
                        missing++;
                    }
                }
            }

            return missing;
        }

        public TileType[] Snapshot()
        {
            BoardModel model = Model;
            TileType[] types = new TileType[model.Width * model.Height];
            for (byte y = 0; y < model.Height; y++)
            {
                for (byte x = 0; x < model.Width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    types[model.ToIndex(coord)] = model.GetCell(coord).Type;
                }
            }

            return types;
        }

        private T Get<T>(string field) where T : class
        {
            FieldInfo info = typeof(BoardController).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            return info.GetValue(_controller) as T;
        }
    }
}
