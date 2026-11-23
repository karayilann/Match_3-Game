using System.Collections.Generic;
using Gameplay.Persistence;

namespace Tests
{
    public sealed class InMemoryKeyValueStore : IKeyValueStore
    {
        private readonly Dictionary<string, int> _values = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

        public int SaveCount { get; private set; }

        public int GetInt(string key, int defaultValue)
        {
            return _values.TryGetValue(key, out int value) ? value : defaultValue;
        }

        public void SetInt(string key, int value)
        {
            _values[key] = value;
        }

        public string GetString(string key, string defaultValue)
        {
            return _strings.TryGetValue(key, out string value) ? value : defaultValue;
        }

        public void SetString(string key, string value)
        {
            _strings[key] = value;
        }

        public void Save()
        {
            SaveCount++;
        }
    }
}
