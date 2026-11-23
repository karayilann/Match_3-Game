using System;
using System.Collections.Generic;
using System.Diagnostics;
using Core.Data.Tiles;
using Gameplay.Board;
using Gameplay.Matching;
using NUnit.Framework;

namespace Tests
{
    /// <summary>
    /// Backs the zero-GC claim with numbers. Allocation is measured on the current thread,
    /// so a single stray list, closure or boxed value in a hot path fails the test.
    /// Timings are Editor (Mono) numbers and the budgets are intentionally loose, they
    /// catch accidental O(n^2) regressions, not micro-differences.
    /// </summary>
    public sealed class HotPathPerformanceTests
    {
        private const int WARMUP_ITERATIONS = 50;
        private const int MEASURED_ITERATIONS = 2000;
        private const double MATCH_SCAN_BUDGET_MICROSECONDS = 200.0;
        private const double MOVE_SCAN_BUDGET_MICROSECONDS = 2000.0;

        private const string COLORS = "RBGYPW";

        private static BoardModel CreateBoard(int size)
        {
            string[] rows = new string[size];
            for (int y = 0; y < size; y++)
            {
                char[] row = new char[size];
                for (int x = 0; x < size; x++)
                {
                    row[x] = COLORS[(x * 2 + y * 3 + (x * y) % 5) % COLORS.Length];
                }
                rows[y] = new string(row);
            }
            return TestBoard.Parse(rows);
        }

        private static BoardModel CreateBoardWithMatches()
        {
            BoardModel board = CreateBoard(8);
            for (byte x = 0; x < 3; x++)
            {
                board.GetCell(new GridCoord(x, 0)).Type = TileType.Red;
            }
            return board;
        }

        [Test]
        public void MatchDetectorAllocatesNothingAfterWarmup()
        {
            BoardModel board = CreateBoardWithMatches();
            MatchDetector detector = new MatchDetector(board.Width, board.Height);
            List<GridCoord> matches = new List<GridCoord>(board.Width * board.Height);
            List<SpecialTileCreation> creations = new List<SpecialTileCreation>(8);

            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                detector.TryFindMatches(board, matches, creations, default);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < MEASURED_ITERATIONS; i++)
            {
                detector.TryFindMatches(board, matches, creations, default);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            TestContext.WriteLine($"MatchDetector 8x8: {allocated} bytes allocated over {MEASURED_ITERATIONS} scans");
            Assert.AreEqual(0, allocated);
        }

        [Test]
        public void PossibleMoveDetectorAllocatesNothingAfterWarmup()
        {
            BoardModel board = CreateBoard(8);
            MatchDetector matchDetector = new MatchDetector(board.Width, board.Height);
            PossibleMoveDetector detector = new PossibleMoveDetector(board.Width, board.Height, matchDetector);

            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                detector.HasPossibleMove(board);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < MEASURED_ITERATIONS; i++)
            {
                detector.HasPossibleMove(board);
                detector.TryFindPossibleMove(board, out GridCoord from, out GridCoord to);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            TestContext.WriteLine($"PossibleMoveDetector 8x8: {allocated} bytes allocated over {MEASURED_ITERATIONS} scans");
            Assert.AreEqual(0, allocated);
        }

        [Test]
        public void MatchDetectorStaysInsideTimeBudget()
        {
            BoardModel board = CreateBoardWithMatches();
            MatchDetector detector = new MatchDetector(board.Width, board.Height);
            List<GridCoord> matches = new List<GridCoord>(board.Width * board.Height);
            List<SpecialTileCreation> creations = new List<SpecialTileCreation>(8);

            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                detector.TryFindMatches(board, matches, creations, default);
            }

            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < MEASURED_ITERATIONS; i++)
            {
                detector.TryFindMatches(board, matches, creations, default);
            }
            watch.Stop();

            double microseconds = watch.Elapsed.TotalMilliseconds * 1000.0 / MEASURED_ITERATIONS;
            TestContext.WriteLine($"MatchDetector 8x8: {microseconds:F2} us per full-board scan (budget {MATCH_SCAN_BUDGET_MICROSECONDS} us)");
            Assert.Less(microseconds, MATCH_SCAN_BUDGET_MICROSECONDS);
        }

        [Test]
        public void PossibleMoveDetectorStaysInsideTimeBudget()
        {
            BoardModel board = CreateBoard(8);
            MatchDetector matchDetector = new MatchDetector(board.Width, board.Height);
            PossibleMoveDetector detector = new PossibleMoveDetector(board.Width, board.Height, matchDetector);

            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                detector.HasPossibleMove(board);
            }

            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < MEASURED_ITERATIONS; i++)
            {
                detector.HasPossibleMove(board);
            }
            watch.Stop();

            double microseconds = watch.Elapsed.TotalMilliseconds * 1000.0 / MEASURED_ITERATIONS;
            TestContext.WriteLine($"PossibleMoveDetector 8x8: {microseconds:F2} us per scan (budget {MOVE_SCAN_BUDGET_MICROSECONDS} us)");
            Assert.Less(microseconds, MOVE_SCAN_BUDGET_MICROSECONDS);
        }
    }
}
