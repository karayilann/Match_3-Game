using Gameplay.Settings;
using NUnit.Framework;
using Signals;

namespace Tests
{
    public sealed class SettingsServiceTests
    {
        private InMemoryKeyValueStore _store;
        private SettingsService _settings;

        [SetUp]
        public void SetUp()
        {
            _store = new InMemoryKeyValueStore();
            _settings = new SettingsService(_store);
            _settings.Subscribe();
        }

        [TearDown]
        public void TearDown()
        {
            _settings.Unsubscribe();
            UiSignals.Reset();
        }

        [Test]
        public void FreshSettings_BroadcastAllEnabled()
        {
            bool sfx = false, music = false, haptics = false;
            UiSignals.OnSettingsLoaded += (s, m, h) => { sfx = s; music = m; haptics = h; };

            _settings.Broadcast();

            Assert.IsTrue(sfx);
            Assert.IsTrue(music);
            Assert.IsTrue(haptics);
        }

        [Test]
        public void Toggle_PersistsAcrossInstances()
        {
            UiSignals.RaiseMusicToggled(false);
            UiSignals.RaiseHapticsToggled(false);

            bool sfx = false, music = true, haptics = true;
            UiSignals.OnSettingsLoaded += (s, m, h) => { sfx = s; music = m; haptics = h; };
            new SettingsService(_store).Broadcast();

            Assert.IsTrue(sfx);
            Assert.IsFalse(music);
            Assert.IsFalse(haptics);
            Assert.AreEqual(2, _store.SaveCount);
        }
    }
}
