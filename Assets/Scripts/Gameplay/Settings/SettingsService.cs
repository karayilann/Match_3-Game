using System;
using Gameplay.Persistence;
using Signals;

namespace Gameplay.Settings
{
    /// <summary>
    /// Single owner of the sound, music and haptics switches. The audio service and the settings popup both used to
    /// read the keys on their own and only the audio service saved them; now both listen to the same signals.
    /// </summary>
    public sealed class SettingsService
    {
        private const string SFX_KEY = "Settings_SFX";
        private const string MUSIC_KEY = "Settings_Music";
        private const string HAPTICS_KEY = "Settings_Haptics";
        private const int ENABLED = 1;
        private const int DISABLED = 0;

        private readonly IKeyValueStore _store;
        private readonly Action<bool> _onSfxToggled;
        private readonly Action<bool> _onMusicToggled;
        private readonly Action<bool> _onHapticsToggled;
        private bool _isSfxEnabled;
        private bool _isMusicEnabled;
        private bool _isHapticsEnabled;

        public SettingsService(IKeyValueStore store)
        {
            _store = store;
            _isSfxEnabled = store.GetInt(SFX_KEY, ENABLED) == ENABLED;
            _isMusicEnabled = store.GetInt(MUSIC_KEY, ENABLED) == ENABLED;
            _isHapticsEnabled = store.GetInt(HAPTICS_KEY, ENABLED) == ENABLED;
            _onSfxToggled = HandleSfxToggled;
            _onMusicToggled = HandleMusicToggled;
            _onHapticsToggled = HandleHapticsToggled;
        }

        public void Subscribe()
        {
            UiSignals.OnSfxToggled += _onSfxToggled;
            UiSignals.OnMusicToggled += _onMusicToggled;
            UiSignals.OnHapticsToggled += _onHapticsToggled;
        }

        public void Unsubscribe()
        {
            UiSignals.OnSfxToggled -= _onSfxToggled;
            UiSignals.OnMusicToggled -= _onMusicToggled;
            UiSignals.OnHapticsToggled -= _onHapticsToggled;
        }

        public void Broadcast()
        {
            UiSignals.RaiseSettingsLoaded(_isSfxEnabled, _isMusicEnabled, _isHapticsEnabled);
        }

        private void HandleSfxToggled(bool isEnabled)
        {
            _isSfxEnabled = isEnabled;
            Persist(SFX_KEY, isEnabled);
        }

        private void HandleMusicToggled(bool isEnabled)
        {
            _isMusicEnabled = isEnabled;
            Persist(MUSIC_KEY, isEnabled);
        }

        private void HandleHapticsToggled(bool isEnabled)
        {
            _isHapticsEnabled = isEnabled;
            Persist(HAPTICS_KEY, isEnabled);
        }

        private void Persist(string key, bool isEnabled)
        {
            _store.SetInt(key, isEnabled ? ENABLED : DISABLED);
            _store.Save();
        }
    }
}
