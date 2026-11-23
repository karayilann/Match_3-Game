using System.Collections;
using Core.Data.Level;
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

namespace Tests.PlayMode
{
    public sealed class BoardFlowTests
    {
        private const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";
        private const float SETTLE_TIMEOUT_SECONDS = 25f;
        private const float TEST_TIME_SCALE = 4f;
        private const int MAX_LEVEL_ID = 20;
        private const float BANNER_SETTLE_SECONDS = 1.3f;

        private BoardProbe _probe;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ClearSavedGames();
            Time.timeScale = TEST_TIME_SCALE;

#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneInPlayMode(SCENE_PATH, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync(0);
#endif
            yield return null;

            BoardController controller = Object.FindAnyObjectByType<BoardController>(FindObjectsInactive.Include);
            Assert.IsNotNull(controller, "BoardController missing from the scene");
            _probe = new BoardProbe(controller);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            ClearSavedGames();
        }

        private static void ClearSavedGames()
        {
            PlayerPrefs.DeleteKey("Lives_Count");
            PlayerPrefs.DeleteKey("Lives_RegenStart");

            for (int id = 0; id <= MAX_LEVEL_ID; id++)
            {
                SaveLoadService.ClearSavedGame(id);
            }
        }

        private IEnumerator StartLevelAndSettle(int level)
        {
            UiSignals.RaiseStartLevel(level);
            yield return WaitUntilIdle();
        }

        private IEnumerator WaitUntilIdle()
        {
            float deadline = Time.realtimeSinceStartup + SETTLE_TIMEOUT_SECONDS;
            yield return null;

            while (!_probe.IsIdle && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_probe.IsIdle, "board never returned to Idle, stuck in " + _probe.StateName);
        }

        private static bool TryFindSwap(BoardModel board, bool wantMatch, out GridCoord from, out GridCoord to)
        {
            MatchDetector detector = new MatchDetector(board.Width, board.Height);
            System.Collections.Generic.List<GridCoord> buffer = new System.Collections.Generic.List<GridCoord>();

            for (byte y = 0; y < board.Height; y++)
            {
                for (byte x = 0; x < board.Width; x++)
                {
                    GridCoord a = new GridCoord(x, y);
                    GridCoord b = new GridCoord((byte)(x + 1), y);
                    if (x + 1 < board.Width && TryEvaluateSwap(board, detector, buffer, a, b, out bool hasMatch) && hasMatch == wantMatch)
                    {
                        from = a;
                        to = b;
                        return true;
                    }
                }
            }

            from = default;
            to = default;
            return false;
        }

        private static bool TryEvaluateSwap(BoardModel board, MatchDetector detector, System.Collections.Generic.List<GridCoord> buffer, GridCoord a, GridCoord b, out bool hasMatch)
        {
            ref GridCell cellA = ref board.GetCell(a);
            ref GridCell cellB = ref board.GetCell(b);

            bool usable = cellA.IsPlayable && cellB.IsPlayable && !cellA.IsLocked && !cellB.IsLocked
                && TileTypeLibrary.IsStandardGem(cellA.Type) && TileTypeLibrary.IsStandardGem(cellB.Type)
                && cellA.Type != cellB.Type;
            if (!usable)
            {
                hasMatch = false;
                return false;
            }

            (cellA.Type, cellB.Type) = (cellB.Type, cellA.Type);
            hasMatch = detector.TryFindMatches(board, buffer);
            (cellA.Type, cellB.Type) = (cellB.Type, cellA.Type);
            return true;
        }

        [UnityTest]
        public IEnumerator StartBoardIsCleanForEveryLevel([NUnit.Framework.Range(1, 15)] int level)
        {
            yield return StartLevelAndSettle(level);

            BoardModel board = _probe.Model;
            MatchDetector detector = new MatchDetector(board.Width, board.Height);
            System.Collections.Generic.List<GridCoord> buffer = new System.Collections.Generic.List<GridCoord>();
            PossibleMoveDetector moves = new PossibleMoveDetector(board.Width, board.Height, detector);

            Assert.IsFalse(detector.TryFindMatches(board, buffer), "settled board still holds a match");
            Assert.AreEqual(0, _probe.CountEmptyPlayableCells(), "empty playable cells remain");
            Assert.AreEqual(0, _probe.CountCellsMissingViews(), "cells without a view remain");
            Assert.IsTrue(moves.HasPossibleMove(board), "no possible move on a settled board");
        }

        [UnityTest]
        public IEnumerator ValidSwapConsumesOneMoveAndBoardSettlesFull()
        {
            yield return StartLevelAndSettle(1);
            Assert.AreEqual(LevelLimitType.Moves, _probe.Limit.LimitType);

            Assert.IsTrue(TryFindSwap(_probe.Model, true, out GridCoord from, out GridCoord to), "no matching swap on the board");
            byte movesBefore = _probe.Limit.RemainingMoves;

            BoardSignals.RaiseTileSwiped(from, to);
            yield return WaitUntilIdle();

            Assert.AreEqual(movesBefore - 1, _probe.Limit.RemainingMoves);
            Assert.AreEqual(0, _probe.CountEmptyPlayableCells());
            Assert.AreEqual(0, _probe.CountCellsMissingViews());
        }

        [UnityTest]
        public IEnumerator IngredientLevelStartsWithIngredientsOnBoard([NUnit.Framework.Values(11, 13, 15)] int level)
        {
            yield return StartLevelAndSettle(level);

            int onBoard = Gameplay.Ingredients.IngredientService.CountOnBoard(_probe.Model);
            Assert.GreaterOrEqual(onBoard, 1, "no ingredient was placed");
            Assert.LessOrEqual(onBoard, 2);
        }

        [UnityTest]
        public IEnumerator IngredientAtBottomIsCollectedAndBoardRefills()
        {
            yield return StartLevelAndSettle(1);

            BoardModel board = _probe.Model;
            GridCoord exit = new GridCoord(0, 0);
            board.GetCell(exit).Type = TileType.Ingredient;
            _probe.EnterMatch();
            yield return WaitUntilIdle();

            Assert.AreEqual(0, Gameplay.Ingredients.IngredientService.CountOnBoard(board), "ingredient stayed on the board");
            Assert.AreEqual(0, _probe.CountEmptyPlayableCells());
            Assert.AreEqual(0, _probe.CountCellsMissingViews());
        }

        [UnityTest]
        public IEnumerator InvalidSwapIsRevertedAndFree()
        {
            yield return StartLevelAndSettle(1);

            Assert.IsTrue(TryFindSwap(_probe.Model, false, out GridCoord from, out GridCoord to), "no non-matching swap on the board");
            byte movesBefore = _probe.Limit.RemainingMoves;
            TileType[] before = _probe.Snapshot();

            BoardSignals.RaiseTileSwiped(from, to);
            yield return WaitUntilIdle();

            Assert.AreEqual(movesBefore, _probe.Limit.RemainingMoves);
            CollectionAssert.AreEqual(before, _probe.Snapshot());
        }

        [UnityTest]
        public IEnumerator SwapInvolvingLockedCellIsCancelled()
        {
            yield return StartLevelAndSettle(2);

            BoardModel board = _probe.Model;
            GridCoord locked = default;
            GridCoord neighbor = default;
            bool found = false;

            for (byte y = 0; y < board.Height && !found; y++)
            {
                for (byte x = 0; x + 1 < board.Width && !found; x++)
                {
                    GridCoord a = new GridCoord(x, y);
                    GridCoord b = new GridCoord((byte)(x + 1), y);
                    if (board.GetCell(a).IsLocked && board.GetCell(b).IsPlayable && !board.GetCell(b).IsLocked)
                    {
                        locked = a;
                        neighbor = b;
                        found = true;
                    }
                }
            }

            Assume.That(found, "level 2 has no locked cell next to a free cell");

            byte movesBefore = _probe.Limit.RemainingMoves;
            TileType[] before = _probe.Snapshot();

            BoardSignals.RaiseTileSwiped(locked, neighbor);
            yield return WaitUntilIdle();

            Assert.AreEqual(movesBefore, _probe.Limit.RemainingMoves);
            CollectionAssert.AreEqual(before, _probe.Snapshot());
        }

        [UnityTest]
        public IEnumerator ReshuffleAlwaysLeavesPlayableBoard([NUnit.Framework.Range(1, 15)] int level)
        {
            yield return StartLevelAndSettle(level);

            BoardModel board = _probe.Model;
            MatchDetector detector = new MatchDetector(board.Width, board.Height);
            System.Collections.Generic.List<GridCoord> buffer = new System.Collections.Generic.List<GridCoord>();
            PossibleMoveDetector moves = new PossibleMoveDetector(board.Width, board.Height, detector);

            for (int i = 0; i < 5; i++)
            {
                _probe.EnterReshuffle();
                yield return WaitUntilIdle();
                yield return new WaitForSecondsRealtime(BANNER_SETTLE_SECONDS);

                Assert.IsFalse(detector.TryFindMatches(board, buffer), "reshuffle left a match, round " + i);
                Assert.IsTrue(moves.HasPossibleMove(board), "reshuffle left no move, round " + i);
            }
        }

        [UnityTest]
        public IEnumerator BackToBackReshufflesLogNoErrors()
        {
            yield return StartLevelAndSettle(1);

            for (int i = 0; i < 3; i++)
            {
                _probe.EnterReshuffle();
                yield return WaitUntilIdle();
            }
        }

        [UnityTest]
        public IEnumerator RepeatedStartsAlwaysSettle([NUnit.Framework.Values(4, 9)] int level)
        {
            for (int i = 0; i < 6; i++)
            {
                UiSignals.RaiseReturnToMenu();
                yield return null;
                yield return StartLevelAndSettle(level);
            }
        }

        [UnityTest]
        public IEnumerator LoadingALevelMidCascadeLeavesTheNewBoardIdle()
        {
            yield return StartLevelAndSettle(1);

            Assert.IsTrue(TryFindSwap(_probe.Model, true, out GridCoord from, out GridCoord to));
            BoardSignals.RaiseTileSwiped(from, to);
            yield return new WaitForSecondsRealtime(0.1f);

            UiSignals.RaiseReturnToMenu();
            UiSignals.RaiseStartLevel(2);
            yield return WaitUntilIdle();
            yield return new WaitForSecondsRealtime(3f);

            Assert.IsTrue(_probe.IsIdle, "state machine drifted to " + _probe.StateName + " after a level change mid cascade");
            Assert.AreEqual(0, _probe.CountEmptyPlayableCells());
        }

        [UnityTest]
        public IEnumerator LevelCanBeRestartedAfterReturningToMenu()
        {
            yield return StartLevelAndSettle(1);

            UiSignals.RaiseReturnToMenu();
            yield return null;
            BoardSignals.RaiseTileSwiped(new GridCoord(1, 1), new GridCoord(2, 1));
            yield return null;

            yield return StartLevelAndSettle(3);

            Assert.AreEqual(0, _probe.CountEmptyPlayableCells());
            Assert.AreEqual(0, _probe.CountCellsMissingViews());
        }

        [UnityTest]
        public IEnumerator ReturningToMenuMidLevelDeductsLifeAndClearsSave()
        {
            yield return StartLevelAndSettle(1);

            int livesBefore = PlayerPrefs.GetInt("Lives_Count", 5);

            UiSignals.RaiseReturnToMenu();
            yield return null;

            int livesAfter = PlayerPrefs.GetInt("Lives_Count", 5);
            Assert.AreEqual(livesBefore - 1, livesAfter, "Abandoning level to menu should deduct 1 life");

            string key = "Save_Level_1";
            Assert.IsFalse(PlayerPrefs.HasKey(key), "Saved game should be cleared after returning to menu");
        }
    }
}
