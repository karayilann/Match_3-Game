using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Debug = Utils.Debug;

namespace Editor
{
    /// <summary>
    /// Keeps every text inside the visible area of the panel that holds it. Texts are clamped
    /// to the panel rect minus a padding that clears rounded frames, so a bad rect from scene
    /// setup can no longer push text past the border. Texts whose center sits outside their
    /// panel are treated as intentional captions and left alone.
    /// </summary>
    [InitializeOnLoad]
    public static class UiTextFitValidator
    {
        private const float MIN_PADDING = 10f;
        private const float MAX_PADDING = 48f;
        private const float PADDING_RATIO = 0.06f;
        private const float TOLERANCE = 1f;
        private const float MIN_PANEL_ALPHA = 0.1f;

        static UiTextFitValidator()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        [MenuItem("Match-3/UI/Fix Text Fit")]
        public static void FixTextFit()
        {
            int fixedCount = Run(true, true, out string report);
            if (fixedCount > 0)
            {
                EditorSceneManager.MarkAllScenesDirty();
            }

            EditorUtility.DisplayDialog("Text Fit", fixedCount + " text(s) adjusted.\n\n" + report, "OK");
        }

        [MenuItem("Match-3/UI/Validate Text Fit")]
        public static void ValidateTextFit()
        {
            int problems = Run(false, true, out string report);
            EditorUtility.DisplayDialog("Text Fit", problems == 0 ? "All texts fit their panels." : problems + " problem(s):\n\n" + report, "OK");
        }

        private static void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            int problems = Run(false, false, out string report);
            if (problems > 0)
            {
                Debug.LogWarning("Text fit: " + problems + " problem(s). Run Match-3/UI/Fix Text Fit.\n" + report);
            }
        }

        private static int Run(bool apply, bool includePrefabs, out string report)
        {
            StringBuilder log = new StringBuilder();
            int count = 0;

            foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (!text.gameObject.scene.IsValid() || EditorUtility.IsPersistent(text))
                {
                    continue;
                }

                count += ProcessText(text, apply, string.Empty, log);
            }

            string[] prefabGuids = includePrefabs ? AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }) : System.Array.Empty<string>();
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                int prefabCount = 0;

                try
                {
                    foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        prefabCount += ProcessText(text, apply, path + ": ", log);
                    }

                    if (apply && prefabCount > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                count += prefabCount;
            }

            report = log.ToString();
            return count;
        }

        private static int ProcessText(TMP_Text text, bool apply, string prefix, StringBuilder log)
        {
            if (text.transform.parent == null || text.name.Contains("SubMesh"))
            {
                return 0;
            }

            Image panel = FindPanel(text.transform);
            if (panel == null)
            {
                return 0;
            }

            RectTransform panelRect = (RectTransform)panel.transform;
            RectTransform textRect = (RectTransform)text.transform;
            Rect safe = GetSafeRect(panelRect);

            if (!safe.Contains(GetCenterInPanelSpace(panelRect, textRect)))
            {
                return 0;
            }

            int count = 0;

            if (TryClamp(panelRect, textRect, safe, apply))
            {
                count++;
                log.AppendLine(prefix + Describe(text) + " clamped to " + panel.name);
            }

            if (apply && EnsureFitSettings(text))
            {
                count++;
            }

            if (!FitsAtMinSize(text))
            {
                count++;
                log.AppendLine(prefix + Describe(text) + " does not fit even at min size, shorten text or enlarge " + panel.name);
            }

            return count;
        }

        private static Image FindPanel(Transform start)
        {
            for (Transform current = start.parent; current != null; current = current.parent)
            {
                Image image = current.GetComponent<Image>();
                if (image != null && image.sprite != null && image.color.a > MIN_PANEL_ALPHA)
                {
                    return image;
                }
            }

            return null;
        }

        private static Rect GetSafeRect(RectTransform panelRect)
        {
            Rect rect = panelRect.rect;
            float padding = Mathf.Clamp(Mathf.Min(rect.width, rect.height) * PADDING_RATIO, MIN_PADDING, MAX_PADDING);
            return new Rect(rect.xMin + padding, rect.yMin + padding, rect.width - padding * 2f, rect.height - padding * 2f);
        }

        private static Vector2 GetCenterInPanelSpace(RectTransform panelRect, RectTransform textRect)
        {
            Vector3 world = textRect.TransformPoint(textRect.rect.center);
            return panelRect.InverseTransformPoint(world);
        }

        private static bool TryClamp(RectTransform panelRect, RectTransform textRect, Rect safe, bool apply)
        {
            Vector3[] corners = new Vector3[4];
            textRect.GetWorldCorners(corners);

            Vector2 min = panelRect.InverseTransformPoint(corners[0]);
            Vector2 max = panelRect.InverseTransformPoint(corners[2]);

            RectTransform parentRect = (RectTransform)textRect.parent;
            Vector3[] parentCorners = new Vector3[4];
            parentRect.GetWorldCorners(parentCorners);
            Vector2 parentMin = panelRect.InverseTransformPoint(parentCorners[0]);
            Vector2 parentMax = panelRect.InverseTransformPoint(parentCorners[2]);

            Vector2 limitMin = new Vector2(Mathf.Max(safe.xMin, parentMin.x), Mathf.Max(safe.yMin, parentMin.y));
            Vector2 limitMax = new Vector2(Mathf.Min(safe.xMax, parentMax.x), Mathf.Min(safe.yMax, parentMax.y));

            Vector2 newMin = new Vector2(Mathf.Max(min.x, limitMin.x), Mathf.Max(min.y, limitMin.y));
            Vector2 newMax = new Vector2(Mathf.Min(max.x, limitMax.x), Mathf.Min(max.y, limitMax.y));

            bool changed = newMin.x > min.x + TOLERANCE || newMin.y > min.y + TOLERANCE
                || newMax.x < max.x - TOLERANCE || newMax.y < max.y - TOLERANCE;

            if (!changed || !apply)
            {
                return changed;
            }

            Vector2 minShift = ToParentDelta(panelRect, textRect, newMin, min);
            Vector2 maxShift = ToParentDelta(panelRect, textRect, newMax, max);

            Undo.RecordObject(textRect, "Fix Text Fit");
            textRect.offsetMin += minShift;
            textRect.offsetMax += maxShift;
            EditorUtility.SetDirty(textRect);
            return true;
        }

        private static Vector2 ToParentDelta(RectTransform panelRect, RectTransform textRect, Vector2 target, Vector2 current)
        {
            Transform parent = textRect.parent;
            Vector2 targetInParent = parent.InverseTransformPoint(panelRect.TransformPoint(target));
            Vector2 currentInParent = parent.InverseTransformPoint(panelRect.TransformPoint(current));
            return targetInParent - currentInParent;
        }

        private static bool EnsureFitSettings(TMP_Text text)
        {
            if (text.enableAutoSizing && text.textWrappingMode == TextWrappingModes.Normal)
            {
                return false;
            }

            Undo.RecordObject(text, "Fix Text Fit");
            float max = text.enableAutoSizing ? text.fontSizeMax : text.fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMax = max;
            text.fontSizeMin = Mathf.Max(12f, Mathf.Round(max * 0.55f));
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            EditorUtility.SetDirty(text);
            return true;
        }

        private static bool FitsAtMinSize(TMP_Text text)
        {
            if (!text.enableAutoSizing)
            {
                return true;
            }

            bool wasAuto = text.enableAutoSizing;
            float size = text.fontSize;
            text.enableAutoSizing = false;
            text.fontSize = text.fontSizeMin;

            Rect rect = ((RectTransform)text.transform).rect;
            Vector2 preferred = text.GetPreferredValues(text.text, rect.width, 0f);
            bool fits = preferred.y <= rect.height + TOLERANCE;

            text.fontSize = size;
            text.enableAutoSizing = wasAuto;
            return fits;
        }

        private static string Describe(TMP_Text text)
        {
            return text.transform.parent.name + "/" + text.name;
        }
    }
}
