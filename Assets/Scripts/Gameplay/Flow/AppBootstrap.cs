using UnityEngine;

namespace Gameplay.Flow
{
    /// <summary>
    /// Mobile defaults to 30 fps without an explicit target, so the frame rate is fixed before the first scene loads.
    /// On a 90 Hz panel a 60 target is not a divisor of the refresh rate and locks presentation to 45 fps, while a 90
    /// target is missed by mid-range GPUs and alternates between one and two refreshes per frame. Asking the window
    /// for a 60 Hz display mode makes the 60 target line up with the panel. The Medium quality level enables vsync,
    /// and a device capture showed a flat 33 ms frame with the panel already at 60 Hz, so the swap interval was most
    /// likely fixed against the 90 Hz mode seen at startup. Vsync is disabled so the explicit target alone paces frames.
    /// </summary>
    public static class AppBootstrap
    {
        private const int TARGET_FRAME_RATE = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyRuntimeSettings()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TARGET_FRAME_RATE;

#if UNITY_ANDROID && !UNITY_EDITOR
            RequestDisplayRefreshRate();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void RequestDisplayRefreshRate()
        {
            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    using (AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow"))
                    using (AndroidJavaObject attributes = window.Call<AndroidJavaObject>("getAttributes"))
                    {
                        attributes.Set("preferredRefreshRate", (float)TARGET_FRAME_RATE);
                        window.Call("setAttributes", attributes);
                    }
                }));
            }
            catch
            {
                Application.targetFrameRate = TARGET_FRAME_RATE;
            }
        }
#endif
    }
}
