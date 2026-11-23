using Gameplay.Clock;
using Gameplay.Persistence;
using Signals;

namespace Gameplay.Lives
{
    /// <summary>
    /// Owns the life count for the whole app: regeneration ticks, the countdown signal and charging a life.
    /// The menu flow only asks whether a level may start and when a life is spent, so lives rules stay in one place.
    /// </summary>
    public sealed class LivesController
    {
        private readonly IKeyValueStore _store;
        private readonly ITimeProvider _clock;
        private readonly LivesService _lives;
        private int _lastTimerSeconds = -1;

        public LivesController(IKeyValueStore store, ITimeProvider clock)
        {
            _store = store;
            _clock = clock;
            _lives = LivesService.Load(store, clock.UtcNowSeconds);
        }

        public void Tick()
        {
            long now = _clock.UtcNowSeconds;
            if (_lives.Refresh(now))
            {
                _lives.Save(_store);
                Publish();
            }

            int seconds = _lives.SecondsToNextLife(now);
            if (seconds != _lastTimerSeconds)
            {
                _lastTimerSeconds = seconds;
                EconomySignals.RaiseLivesTimerChanged(seconds);
            }
        }

        public void Publish()
        {
            _lastTimerSeconds = -1;
            EconomySignals.RaiseLivesChanged(_lives.Lives, LivesService.MAX_LIVES);
        }

        /// <summary>
        /// Raises OnNoLivesLeft when the level cannot start, so the caller only has to stop.
        /// </summary>
        public bool TryStartLevel()
        {
            if (_lives.Refresh(_clock.UtcNowSeconds))
            {
                _lives.Save(_store);
                Publish();
            }

            if (_lives.Lives > 0)
            {
                return true;
            }

            EconomySignals.RaiseNoLivesLeft();
            return false;
        }

        public void ChargeLife()
        {
            if (_lives.TryConsume(_clock.UtcNowSeconds))
            {
                _lives.Save(_store);
                Publish();
            }
        }
    }
}
