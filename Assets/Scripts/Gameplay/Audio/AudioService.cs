using System;
using Core.Data.Boosters;
using Core.Data.Tiles;
using Signals;
using UnityEngine;

namespace Gameplay.Audio
{
    public sealed class AudioService : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _musicSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _swapClip;
        [SerializeField] private AudioClip _matchClip;
        [SerializeField] private AudioClip _comboClip;
        [SerializeField] private AudioClip _bombClip;
        [SerializeField] private AudioClip _rocketClip;
        [SerializeField] private AudioClip _winClip;
        [SerializeField] private AudioClip _loseClip;

        private float _currentPitch = 1.0f;
        private const float PITCH_STEP = 0.08f;
        private const float MAX_PITCH = 1.8f;

        private bool _sfxEnabled = true;
        private bool _musicEnabled = true;
        private bool _hapticsEnabled = true;
        private int _lastMatchSoundFrame = -1;
        private int _lastDetonationFrame = -1;

        private void HandleSettingsLoaded(bool isSfxEnabled, bool isMusicEnabled, bool isHapticsEnabled)
        {
            HandleSfxToggled(isSfxEnabled);
            HandleMusicToggled(isMusicEnabled);
            HandleHapticsToggled(isHapticsEnabled);

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_hapticsEnabled && !_vibratorInitialized)
            {
                InitializeVibrator();
            }
#endif
        }

        private void OnEnable()
        {
            BoardSignals.OnTileSwiped += HandleTileSwiped;
            BoardSignals.OnTileMatched += HandleTileMatched;
            BoardSignals.OnBoardSettled += HandleBoardSettled;
            LevelSignals.OnAllGoalsCompleted += HandleGoalsCompleted;
            LevelSignals.OnLevelLimitReached += HandleLevelLimitReached;
            UiSignals.OnSettingsLoaded += HandleSettingsLoaded;
            UiSignals.OnSfxToggled += HandleSfxToggled;
            UiSignals.OnMusicToggled += HandleMusicToggled;
            UiSignals.OnHapticsToggled += HandleHapticsToggled;
            BoardSignals.OnSpecialTileDetonated += HandleSpecialTileDetonated;
            EconomySignals.OnBoosterUsed += HandleBoosterUsed;
            LevelSignals.OnStarAwarded += HandleStarAwarded;
        }

        private void OnDisable()
        {
            BoardSignals.OnTileSwiped -= HandleTileSwiped;
            BoardSignals.OnTileMatched -= HandleTileMatched;
            BoardSignals.OnBoardSettled -= HandleBoardSettled;
            LevelSignals.OnAllGoalsCompleted -= HandleGoalsCompleted;
            LevelSignals.OnLevelLimitReached -= HandleLevelLimitReached;
            UiSignals.OnSettingsLoaded -= HandleSettingsLoaded;
            UiSignals.OnSfxToggled -= HandleSfxToggled;
            UiSignals.OnMusicToggled -= HandleMusicToggled;
            UiSignals.OnHapticsToggled -= HandleHapticsToggled;
            BoardSignals.OnSpecialTileDetonated -= HandleSpecialTileDetonated;
            EconomySignals.OnBoosterUsed -= HandleBoosterUsed;
            LevelSignals.OnStarAwarded -= HandleStarAwarded;
        }

        private void HandleSfxToggled(bool enabled)
        {
            _sfxEnabled = enabled;
            if (_sfxSource != null)
            {
                _sfxSource.mute = !enabled;
            }
        }

        private void HandleMusicToggled(bool enabled)
        {
            _musicEnabled = enabled;
            if (_musicSource != null)
            {
                _musicSource.mute = !enabled;
            }
        }

        private void HandleHapticsToggled(bool enabled)
        {
            _hapticsEnabled = enabled;
        }

        private void HandleTileSwiped(GridCoord arg1, GridCoord arg2)
        {
            PlayClip(_swapClip, 1.0f);
        }

        /// <summary>
        /// A large clear raises this once per frame per tile, so playing every one stacks dozens of one-shots
        /// in a single frame. One clip per frame sounds the same and keeps the pitch climbing per cascade step.
        /// </summary>
        private void HandleTileMatched(GridCoord obj)
        {
            int frame = Time.frameCount;
            if (frame == _lastMatchSoundFrame)
            {
                return;
            }

            _lastMatchSoundFrame = frame;
            PlayClip(_matchClip, _currentPitch);
            _currentPitch = Mathf.Min(_currentPitch + PITCH_STEP, MAX_PITCH);
        }

        private void HandleSpecialTileDetonated(TileType specialType)
        {
            int frame = Time.frameCount;
            if (frame == _lastDetonationFrame)
            {
                return;
            }

            _lastDetonationFrame = frame;

            bool isRocket = specialType == TileType.HorizontalRocket || specialType == TileType.VerticalRocket;
            if (isRocket && _rocketClip != null)
            {
                PlayClip(_rocketClip, 1.0f);
            }
            else
            {
                PlayClip(_bombClip != null ? _bombClip : _comboClip, 1.0f);
            }

            TriggerBoosterHaptic();
        }

        private void HandleBoosterUsed(BoosterType boosterType)
        {
            PlayClip(_bombClip != null ? _bombClip : _matchClip, 1.0f);
            TriggerBoosterHaptic();
        }

        private void TriggerBoosterHaptic()
        {
            if (!_hapticsEnabled)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private const long HAPTIC_MILLISECONDS = 80L;
        private const string VIBRATE_METHOD_SIGNATURE = "(Landroid/os/VibrationEffect;)V";

        private static AndroidJavaObject _vibrator;
        private static AndroidJavaObject _vibrationEffect;
        private static IntPtr _vibrateMethodId;
        private static jvalue[] _vibrateArguments;
        private static bool _vibratorInitialized;

        /// <summary>
        /// The reflective AndroidJavaObject.Call path builds JNI signature strings and wrapper objects on every call
        /// and cost several milliseconds per vibration. The vibrator, effect and method id are resolved once, so each
        /// vibration is a single raw JNI call. minSdk is 26, so the VibrationEffect overload always exists.
        /// </summary>
        private static void InitializeVibrator()
        {
            _vibratorInitialized = true;

            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                {
                    if (currentActivity == null)
                    {
                        return;
                    }

                    _vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    _vibrationEffect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", HAPTIC_MILLISECONDS, -1);
                }

                IntPtr vibratorClass = AndroidJNI.GetObjectClass(_vibrator.GetRawObject());
                _vibrateMethodId = AndroidJNI.GetMethodID(vibratorClass, "vibrate", VIBRATE_METHOD_SIGNATURE);
                AndroidJNI.DeleteLocalRef(vibratorClass);

                _vibrateArguments = new jvalue[1];
                _vibrateArguments[0].l = _vibrationEffect.GetRawObject();
            }
            catch
            {
                _vibrateMethodId = IntPtr.Zero;
            }
        }

        private static void VibrateAndroid()
        {
            if (!_vibratorInitialized)
            {
                InitializeVibrator();
            }

            if (_vibrateMethodId == IntPtr.Zero)
            {
                Handheld.Vibrate();
                return;
            }

            AndroidJNI.CallVoidMethod(_vibrator.GetRawObject(), _vibrateMethodId, _vibrateArguments);
        }
#endif

        private void HandleBoardSettled()
        {
            _currentPitch = 1.0f;
        }

        private void HandleStarAwarded(int starIndex)
        {
            float pitch = 1.0f + (starIndex * 0.18f);
            PlayClip(_comboClip != null ? _comboClip : _matchClip, pitch);
        }

        private void HandleGoalsCompleted()
        {
            PlayClip(_winClip, 1.0f);
        }

        private void HandleLevelLimitReached()
        {
            PlayClip(_loseClip, 1.0f);
        }

        private void PlayClip(AudioClip clip, float pitch)
        {
            if (clip == null || _sfxSource == null)
            {
                return;
            }

            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(clip);
        }
    }
}
