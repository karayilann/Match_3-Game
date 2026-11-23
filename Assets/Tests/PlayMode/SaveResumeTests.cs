using System.Collections;
using System.Reflection;
using Core.Data.Tiles;
using Gameplay.Board;
using Gameplay.Persistence;
using NUnit.Framework;
using Signals;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public sealed class SaveResumeTests
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
        public IEnumerator ResumeFromUnsettledSaveRefillsGaps()
        {
            UiSignals.RaiseStartLevel(LEVEL);
            yield return WaitIdle("start");

            BoardModel model = _probe.Model;
            GridCoord gap = new GridCoord(0, (byte)(model.Height - 1));
            Assert.IsTrue(model.GetCell(gap).IsPlayable && !model.GetCell(gap).IsLocked, "top-left cell must be a plain tile");

            model.GetCell(gap).Type = TileType.None;
            InvokeController("OnApplicationQuit");
            Assert.IsTrue(SaveLoadService.HasSavedGame(LEVEL), "idle board must save");

            UiSignals.RaiseStartLevel(LEVEL);
            yield return WaitIdle("resume");

            Assert.AreNotEqual(TileType.None, _probe.Model.GetCell(gap).Type, "gap from the saved board must be refilled");
            Assert.IsNotNull(_probe.Views[_probe.Model.ToIndex(gap)], "refilled cell must have a view");
        }

        [UnityTest]
        public IEnumerator PauseDuringAnimationDoesNotSave()
        {
            UiSignals.RaiseStartLevel(LEVEL);
            yield return WaitIdle("start");
            SaveLoadService.ClearSavedGame(LEVEL);

            BoardModel model = _probe.Model;
            GridCoord from = default;
            GridCoord to = default;
            bool found = false;
            for (byte y = 0; y < model.Height && !found; y++)
            {
                for (byte x = 0; x + 1 < model.Width && !found; x++)
                {
                    GridCoord a = new GridCoord(x, y);
                    GridCoord b = new GridCoord((byte)(x + 1), y);
                    if (CanSwap(model, a, b))
                    {
                        from = a;
                        to = b;
                        found = true;
                    }
                }
            }

            Assert.IsTrue(found, "no swappable pair");
            BoardSignals.RaiseTileSwiped(from, to);
            Assert.IsFalse(_probe.IsIdle, "swap must leave idle");

            InvokeController("OnApplicationQuit");
            Assert.IsFalse(SaveLoadService.HasSavedGame(LEVEL), "mid-animation board must not be saved");
        }

        private static bool CanSwap(BoardModel model, GridCoord a, GridCoord b)
        {
            ref GridCell ca = ref model.GetCell(a);
            ref GridCell cb = ref model.GetCell(b);
            return ca.IsPlayable && cb.IsPlayable && !ca.IsLocked && !cb.IsLocked && ca.Type != cb.Type;
        }

        private void InvokeController(string method)
        {
            typeof(BoardController).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_controller, null);
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
    }
}
