using System.Collections.Generic;
using Gameplay.Progress;
using NUnit.Framework;
using Signals;

namespace Tests
{
    public sealed class ProgressServiceTests
    {
        private const int LEVEL_COUNT = 5;

        private InMemoryKeyValueStore _store;

        [SetUp]
        public void SetUp()
        {
            _store = new InMemoryKeyValueStore();
        }

        [TearDown]
        public void TearDown()
        {
            UiSignals.Reset();
        }

        [Test]
        public void FreshProgress_UnlocksOnlyTheFirstLevel()
        {
            ProgressService progress = new ProgressService(_store, LEVEL_COUNT);

            Assert.AreEqual(1, progress.UnlockedLevel);
            Assert.AreEqual(0, progress.GetStars(1));
        }

        [Test]
        public void RecordWin_UnlocksNextLevelAndPersists()
        {
            ProgressService progress = new ProgressService(_store, LEVEL_COUNT);

            progress.RecordWin(1, 2);

            ProgressService reloaded = new ProgressService(_store, LEVEL_COUNT);
            Assert.AreEqual(2, reloaded.UnlockedLevel);
            Assert.AreEqual(2, reloaded.GetStars(1));
        }

        [Test]
        public void RecordWin_KeepsTheBestStars()
        {
            ProgressService progress = new ProgressService(_store, LEVEL_COUNT);

            progress.RecordWin(1, 3);
            progress.RecordWin(1, 1);

            Assert.AreEqual(3, progress.GetStars(1));
        }

        [Test]
        public void RecordWin_OnLastLevel_DoesNotUnlockPastTheEnd()
        {
            ProgressService progress = new ProgressService(_store, LEVEL_COUNT);

            for (int level = 1; level <= LEVEL_COUNT; level++)
            {
                progress.RecordWin(level, 1);
            }

            Assert.AreEqual(LEVEL_COUNT, progress.UnlockedLevel);
        }

        [Test]
        public void RecordWin_BroadcastsUpdatedProgress()
        {
            ProgressService progress = new ProgressService(_store, LEVEL_COUNT);
            int unlocked = 0;
            IReadOnlyList<byte> stars = null;
            UiSignals.OnProgressChanged += (level, starsByLevel) =>
            {
                unlocked = level;
                stars = starsByLevel;
            };

            progress.RecordWin(2, 3);

            Assert.AreEqual(3, unlocked);
            Assert.AreEqual(3, stars[2]);
        }
    }
}
