using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Core.Data.Tiles;
using Gameplay.Board;
using Gameplay.Matching;
using Gameplay.Persistence;
using NUnit.Framework;
using Signals;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using View;

namespace Tests.PlayMode
{
    public sealed class Level3FuzzTests
    {
        private const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";
        private const float SETTLE_TIMEOUT_SECONDS = 25f;
        private const int LEVEL = 3;

        private BoardProbe _probe;
        private BoardController _controller;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveLoadService.ClearSavedGame(LEVEL);
            Time.timeScale = 4f;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneInPlayMode(SCENE_PATH, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync(0);
#endif
            yield return null;
            _controller = Object.FindAnyObjectByType<BoardController>(FindObjectsInactive.Include);
            _probe = new BoardProbe(_controller);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            SaveLoadService.ClearSavedGame(LEVEL);
        }

        [UnityTest]
        public IEnumerator FuzzSwapsKeepBoardConsistent([NUnit.Framework.Range(0, 24)] int seed)
        {
            Random.InitState(seed);
            UiSignals.RaiseStartLevel(LEVEL);
            yield return WaitIdle("start");

            TileSpriteDatabase db = typeof(BoardController).GetField("_spriteDatabase", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_controller) as TileSpriteDatabase;
            StringBuilder history = new StringBuilder();
            history.AppendLine("seed " + seed);
            history.AppendLine(Dump());

            for (int move = 0; move < 12; move++)
            {
                if (!PickSwap(out GridCoord from, out GridCoord to))
                {
                    break;
                }

                history.AppendLine("move " + move + ": " + from.X + "," + from.Y + " -> " + to.X + "," + to.Y);
                BoardSignals.RaiseTileSwiped(from, to);

                float deadline = Time.realtimeSinceStartup + SETTLE_TIMEOUT_SECONDS;
                yield return null;
                while (!_probe.IsIdle && !GoalsDone() && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                if (GoalsDone())
                {
                    yield break;
                }

                history.AppendLine(Dump());
                Assert.IsTrue(_probe.IsIdle, "stuck in " + _probe.StateName + "\n" + history);

                string problem = Validate(db);
                Assert.IsNull(problem, problem + "\n" + history);
            }
        }

        [UnityTest]
        public IEnumerator FuzzHammerAndSwapsKeepBoardConsistent([NUnit.Framework.Range(0, 24)] int seed)
        {
            Random.InitState(1000 + seed);
            UiSignals.RaiseStartLevel(LEVEL);
            yield return WaitIdle("start");

            TileSpriteDatabase db = typeof(BoardController).GetField("_spriteDatabase", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_controller) as TileSpriteDatabase;
            StringBuilder history = new StringBuilder();
            history.AppendLine("seed " + seed);
            history.AppendLine(Dump());
            int hammersLeft = 2;

            for (int step = 0; step < 8; step++)
            {
                if (hammersLeft > 0 && Random.value < 0.4f)
                {
                    BoardModel b = _probe.Model;
                    GridCoord target = new GridCoord((byte)Random.Range(0, b.Width), (byte)Random.Range(0, b.Height));
                    if (!b.GetCell(target).IsPlayable)
                    {
                        continue;
                    }

                    hammersLeft--;
                    history.AppendLine("hammer " + target.X + "," + target.Y);
                    EconomySignals.RaiseBoosterButtonClicked(Core.Data.Boosters.BoosterType.Hammer);
                    BoardSignals.RaiseCellTapped(target);
                }
                else
                {
                    if (!PickSwap(out GridCoord from, out GridCoord to))
                    {
                        break;
                    }

                    history.AppendLine("swap " + from.X + "," + from.Y + " -> " + to.X + "," + to.Y);
                    BoardSignals.RaiseTileSwiped(from, to);
                }

                float deadline = Time.realtimeSinceStartup + SETTLE_TIMEOUT_SECONDS;
                yield return null;
                while (!_probe.IsIdle && !GoalsDone() && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                if (GoalsDone())
                {
                    yield break;
                }

                history.AppendLine(Dump());
                Assert.IsTrue(_probe.IsIdle, "stuck in " + _probe.StateName + "\n" + history);
                string problem = Validate(db);
                Assert.IsNull(problem, problem + "\n" + history);
            }
        }

        private bool GoalsDone()
        {
            object goals = typeof(BoardController).GetField("_goalService", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_controller);
            return ((Gameplay.Goals.GoalService)goals).IsAllCompleted;
        }

        private IEnumerator WaitIdle(string tag)
        {
            float deadline = Time.realtimeSinceStartup + SETTLE_TIMEOUT_SECONDS;
            yield return null;
            while (!_probe.IsIdle && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.IsTrue(_probe.IsIdle, tag + " stuck in " + _probe.StateName);
        }

        private bool PickSwap(out GridCoord from, out GridCoord to)
        {
            BoardModel b = _probe.Model;
            List<(GridCoord, GridCoord)> candidates = new List<(GridCoord, GridCoord)>();
            for (byte y = 0; y < b.Height; y++)
            {
                for (byte x = 0; x < b.Width; x++)
                {
                    GridCoord a = new GridCoord(x, y);
                    if (x + 1 < b.Width) candidates.Add((a, new GridCoord((byte)(x + 1), y)));
                    if (y + 1 < b.Height) candidates.Add((a, new GridCoord(x, (byte)(y + 1))));
                }
            }

            MatchDetector detector = new MatchDetector(b.Width, b.Height);
            List<GridCoord> buf = new List<GridCoord>();
            List<(GridCoord, GridCoord)> good = new List<(GridCoord, GridCoord)>();
            foreach ((GridCoord a, GridCoord c) in candidates)
            {
                ref GridCell ca = ref b.GetCell(a);
                ref GridCell cc = ref b.GetCell(c);
                if (!ca.IsPlayable || !cc.IsPlayable || ca.IsLocked || cc.IsLocked || ca.Type == cc.Type) continue;
                (ca.Type, cc.Type) = (cc.Type, ca.Type);
                bool m = detector.TryFindMatches(b, buf);
                (ca.Type, cc.Type) = (cc.Type, ca.Type);
                if (m) good.Add((a, c));
            }

            if (good.Count == 0)
            {
                from = default;
                to = default;
                return false;
            }

            (from, to) = good[Random.Range(0, good.Count)];
            return true;
        }

        private string Validate(TileSpriteDatabase db)
        {
            BoardModel b = _probe.Model;
            GemView[] views = _probe.Views;
            MatchDetector detector = new MatchDetector(b.Width, b.Height);
            List<GridCoord> buf = new List<GridCoord>();
            if (detector.TryFindMatches(b, buf)) return "settled board holds a match at " + buf[0].X + "," + buf[0].Y;

            for (byte y = 0; y < b.Height; y++)
            {
                for (byte x = 0; x < b.Width; x++)
                {
                    GridCoord c = new GridCoord(x, y);
                    ref GridCell cell = ref b.GetCell(c);
                    if (!cell.IsPlayable) continue;
                    GemView v = views[b.ToIndex(c)];
                    if (cell.Type == TileType.None) return "empty cell " + x + "," + y + " locked=" + cell.IsLocked;
                    if (v == null) return "missing view " + x + "," + y + " type=" + cell.Type;
                    if (!cell.IsLocked)
                    {
                        Sprite s = v.GetComponentInChildren<SpriteRenderer>().sprite;
                        if (s != db.GetSprite(cell.Type)) return "sprite desync " + x + "," + y + " type=" + cell.Type + " sprite=" + (s != null ? s.name : "null");
                    }
                }
            }
            return null;
        }

        private string Dump()
        {
            BoardModel b = _probe.Model;
            StringBuilder sb = new StringBuilder();
            for (int y = b.Height - 1; y >= 0; y--)
            {
                for (byte x = 0; x < b.Width; x++)
                {
                    ref GridCell cell = ref b.GetCell(new GridCoord(x, (byte)y));
                    sb.Append(cell.IsPlayable ? ((int)cell.Type).ToString("00") + (cell.IsLocked ? "L" : " ") : "-- ");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
