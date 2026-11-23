using System;
using Gameplay.Persistence;
using UnityEngine;

namespace Gameplay.Lives
{
    /// <summary>
    /// Timestamp based regeneration: only the moment the first missing life started refilling is
    /// stored, so lives keep regenerating while the app is closed without any background timer.
    /// </summary>
    public sealed class LivesService
    {
        public const int MAX_LIVES = 5;
        public const int REGEN_SECONDS = 1800;

        private const string LIVES_KEY = "Lives_Count";
        private const string REGEN_START_KEY = "Lives_RegenStart";

        private int _lives;
        private long _regenStart;

        public int Lives => _lives;
        public bool IsFull => _lives >= MAX_LIVES;

        public LivesService(int lives, long regenStart)
        {
            _lives = Mathf.Clamp(lives, 0, MAX_LIVES);
            _regenStart = regenStart;
        }

        public static LivesService Load(IKeyValueStore store, long now)
        {
            int lives = store.GetInt(LIVES_KEY, MAX_LIVES);
            long.TryParse(store.GetString(REGEN_START_KEY, "0"), out long regenStart);
            LivesService service = new LivesService(lives, regenStart);
            service.Refresh(now);
            return service;
        }

        public void Save(IKeyValueStore store)
        {
            store.SetInt(LIVES_KEY, _lives);
            store.SetString(REGEN_START_KEY, _regenStart.ToString());
            store.Save();
        }

        /// <returns>True when the life count changed.</returns>
        public bool Refresh(long now)
        {
            if (_lives >= MAX_LIVES)
            {
                _regenStart = 0;
                return false;
            }

            long elapsed = now - _regenStart;
            if (elapsed < REGEN_SECONDS)
            {
                return false;
            }

            int gained = (int)(elapsed / REGEN_SECONDS);
            _lives = Math.Min(MAX_LIVES, _lives + gained);
            _regenStart = _lives >= MAX_LIVES ? 0 : _regenStart + (long)gained * REGEN_SECONDS;
            return true;
        }

        public bool TryConsume(long now)
        {
            Refresh(now);
            if (_lives <= 0)
            {
                return false;
            }

            if (_lives >= MAX_LIVES)
            {
                _regenStart = now;
            }

            _lives--;
            return true;
        }

        public void Refill()
        {
            _lives = MAX_LIVES;
            _regenStart = 0;
        }

        public int SecondsToNextLife(long now)
        {
            if (_lives >= MAX_LIVES)
            {
                return 0;
            }

            long remaining = _regenStart + REGEN_SECONDS - now;
            return (int)Math.Max(0, remaining);
        }
    }
}
