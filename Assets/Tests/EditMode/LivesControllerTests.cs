using Gameplay.Clock;
using Gameplay.Lives;
using NUnit.Framework;
using Signals;

namespace Tests
{
    public sealed class LivesControllerTests
    {
        private const long START = 1_000_000;

        private sealed class FixedTimeProvider : ITimeProvider
        {
            public long Now = START;

            public long UtcNowSeconds => Now;
        }

        private InMemoryKeyValueStore _store;
        private FixedTimeProvider _clock;

        [SetUp]
        public void SetUp()
        {
            _store = new InMemoryKeyValueStore();
            _clock = new FixedTimeProvider();
        }

        [TearDown]
        public void TearDown()
        {
            EconomySignals.Reset();
        }

        [Test]
        public void ChargeLife_PersistsAcrossControllers()
        {
            LivesController lives = new LivesController(_store, _clock);
            int published = -1;
            EconomySignals.OnLivesChanged += (count, max) => published = count;

            lives.ChargeLife();

            Assert.AreEqual(LivesService.MAX_LIVES - 1, published);
            new LivesController(_store, _clock).Publish();
            Assert.AreEqual(LivesService.MAX_LIVES - 1, published);
        }

        [Test]
        public void TryStartLevel_WithoutLives_RaisesNoLivesLeft()
        {
            LivesController lives = new LivesController(_store, _clock);
            for (int i = 0; i < LivesService.MAX_LIVES; i++)
            {
                lives.ChargeLife();
            }

            int noLivesRaised = 0;
            EconomySignals.OnNoLivesLeft += () => noLivesRaised++;

            Assert.IsFalse(lives.TryStartLevel());
            Assert.AreEqual(1, noLivesRaised);
        }

        [Test]
        public void TryStartLevel_AfterRegeneration_Allows()
        {
            LivesController lives = new LivesController(_store, _clock);
            for (int i = 0; i < LivesService.MAX_LIVES; i++)
            {
                lives.ChargeLife();
            }

            _clock.Now += LivesService.REGEN_SECONDS;

            Assert.IsTrue(lives.TryStartLevel());
        }

        [Test]
        public void Tick_RaisesTimerOnlyWhenSecondsChange()
        {
            LivesController lives = new LivesController(_store, _clock);
            lives.ChargeLife();
            int timerRaised = 0;
            EconomySignals.OnLivesTimerChanged += seconds => timerRaised++;

            lives.Tick();
            lives.Tick();
            _clock.Now += 1;
            lives.Tick();

            Assert.AreEqual(2, timerRaised);
        }
    }
}
