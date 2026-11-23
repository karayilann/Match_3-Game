#if DEVELOPMENT_BUILD && CRASH_TEST_PANEL && !UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace ErrorHandling.Debugging
{
    /// <summary>
    /// Panel for checking that crash reports reach the backend. It is compiled only when DEVELOPMENT_BUILD and the
    /// CRASH_TEST_PANEL scripting define are both set, because its IMGUI allocates every frame and would pollute
    /// profiler captures. It builds itself so no scene or prefab has to carry it.
    /// </summary>
    public sealed class CrashTestPanel : MonoBehaviour
    {
        private const float REFERENCE_DPI = 160f;
        private const float BUTTON_WIDTH = 220f;
        private const float BUTTON_HEIGHT = 56f;
        private const float MARGIN = 8f;

        private bool _isExpanded;
        private float _guiScale = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            GameObject host = new GameObject("CrashTestPanel");
            host.AddComponent<CrashTestPanel>();
            DontDestroyOnLoad(host);
        }

        private void Awake()
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : REFERENCE_DPI;
            _guiScale = dpi / REFERENCE_DPI;
        }

        private void OnGUI()
        {
            GUI.matrix = Matrix4x4.Scale(new Vector3(_guiScale, _guiScale, 1f));

            float y = MARGIN;
            if (GUI.Button(new Rect(MARGIN, y, BUTTON_WIDTH, BUTTON_HEIGHT), _isExpanded ? "Hide Crash Test" : "Crash Test"))
            {
                _isExpanded = !_isExpanded;
            }

            if (!_isExpanded)
            {
                return;
            }

            y += BUTTON_HEIGHT + MARGIN;
            if (GUI.Button(new Rect(MARGIN, y, BUTTON_WIDTH, BUTTON_HEIGHT), "Non-fatal exception"))
            {
                ErrorReporter.RecordException(new InvalidOperationException("Crash test: non-fatal exception"));
            }

            y += BUTTON_HEIGHT + MARGIN;
            if (GUI.Button(new Rect(MARGIN, y, BUTTON_WIDTH, BUTTON_HEIGHT), "Unhandled exception"))
            {
                throw new InvalidOperationException("Crash test: unhandled exception");
            }

            y += BUTTON_HEIGHT + MARGIN;
            if (GUI.Button(new Rect(MARGIN, y, BUTTON_WIDTH, BUTTON_HEIGHT), "Native crash"))
            {
                UnityEngine.Diagnostics.Utils.ForceCrash(ForcedCrashCategory.AccessViolation);
            }
        }
    }
}
#endif
