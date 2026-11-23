namespace Gameplay.Persistence
{
    public interface IKeyValueStore
    {
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        string GetString(string key, string defaultValue);
        void SetString(string key, string value);
        void Save();
    }
}
