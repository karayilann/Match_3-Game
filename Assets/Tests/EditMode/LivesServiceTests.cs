using Gameplay.Lives;
using NUnit.Framework;

namespace Tests
{
    public sealed class LivesServiceTests
    {
        private const long START = 1000000;

        [Test]
        public void ConsumingFromFullStartsRegenTimer()
        {
            LivesService service = new LivesService(LivesService.MAX_LIVES, 0);
            Assert.IsTrue(service.TryConsume(START));
            Assert.AreEqual(LivesService.MAX_LIVES - 1, service.Lives);
            Assert.AreEqual(LivesService.REGEN_SECONDS, service.SecondsToNextLife(START));
        }

        [Test]
        public void LifeRegeneratesAfterInterval()
        {
            LivesService service = new LivesService(LivesService.MAX_LIVES, 0);
            service.TryConsume(START);
            Assert.IsTrue(service.Refresh(START + LivesService.REGEN_SECONDS));
            Assert.IsTrue(service.IsFull);
        }

        [Test]
        public void SeveralIntervalsGiveSeveralLivesAndKeepRemainder()
        {
            LivesService service = new LivesService(0, START);
            service.Refresh(START + LivesService.REGEN_SECONDS * 2 + 100);
            Assert.AreEqual(2, service.Lives);
            Assert.AreEqual(LivesService.REGEN_SECONDS - 100, service.SecondsToNextLife(START + LivesService.REGEN_SECONDS * 2 + 100));
        }

        [Test]
        public void CannotConsumeWithZeroLives()
        {
            LivesService service = new LivesService(0, START);
            Assert.IsFalse(service.TryConsume(START + 10));
            Assert.AreEqual(0, service.Lives);
        }

        [Test]
        public void RegenCapsAtMax()
        {
            LivesService service = new LivesService(3, START);
            service.Refresh(START + LivesService.REGEN_SECONDS * 20);
            Assert.AreEqual(LivesService.MAX_LIVES, service.Lives);
            Assert.AreEqual(0, service.SecondsToNextLife(START));
        }
    }
}
