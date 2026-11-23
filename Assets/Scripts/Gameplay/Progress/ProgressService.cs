using Gameplay.Persistence;
using Signals;
using UnityEngine;

namespace Gameplay.Progress
{
    /// <summary>
    /// Single owner of map progress: the highest unlocked level and the best stars per level. Stars are cached in a
    /// byte array indexed by level number so the map can be refreshed from the broadcast without reading storage.
    /// </summary>
    public sealed class ProgressService
    {
        private const string UNLOCKED_LEVEL_KEY = "Player_UnlockedLevel";
        private const string STARS_KEY_PREFIX = "Level_";
        private const string STARS_KEY_SUFFIX = "_Stars";
        private const int FIRST_LEVEL = 1;
        private const byte MAX_STARS = 3;

        private readonly IKeyValueStore _store;
        private readonly int _levelCount;
        private readonly byte[] _stars;
        private readonly string[] _starKeys;
        private int _unlockedLevel;

        public int UnlockedLevel => _unlockedLevel;

        public ProgressService(IKeyValueStore store, int levelCount)
        {
            _store = store;
            _levelCount = Mathf.Max(FIRST_LEVEL, levelCount);
            _stars = new byte[_levelCount + 1];
            _starKeys = new string[_levelCount + 1];

            for (int level = FIRST_LEVEL; level <= _levelCount; level++)
            {
                _starKeys[level] = STARS_KEY_PREFIX + level + STARS_KEY_SUFFIX;
                _stars[level] = (byte)Mathf.Clamp(store.GetInt(_starKeys[level], 0), 0, MAX_STARS);
            }

            _unlockedLevel = Mathf.Clamp(store.GetInt(UNLOCKED_LEVEL_KEY, FIRST_LEVEL), FIRST_LEVEL, _levelCount);
        }

        public byte GetStars(int level)
        {
            return level >= FIRST_LEVEL && level <= _levelCount ? _stars[level] : (byte)0;
        }

        public void Broadcast()
        {
            UiSignals.RaiseProgressChanged(_unlockedLevel, _stars);
        }

        /// <summary>
        /// Keeps the best result, so replaying a level with fewer stars never lowers the map, and unlocks the next
        /// level only up to the last one that exists.
        /// </summary>
        public void RecordWin(int level, int earnedStars)
        {
            if (level < FIRST_LEVEL || level > _levelCount)
            {
                return;
            }

            byte stars = (byte)Mathf.Clamp(earnedStars, 0, MAX_STARS);
            if (stars > _stars[level])
            {
                _stars[level] = stars;
                _store.SetInt(_starKeys[level], stars);
            }

            int nextLevel = Mathf.Min(level + 1, _levelCount);
            if (nextLevel > _unlockedLevel)
            {
                _unlockedLevel = nextLevel;
                _store.SetInt(UNLOCKED_LEVEL_KEY, nextLevel);
            }

            _store.Save();
            Broadcast();
        }
    }
}
