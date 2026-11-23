using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Editor
{
    /// <summary>
    /// Single source of truth for UI colors. Roles are resolved by object name or by
    /// the nearest legacy color, so re-running the tool keeps the whole scene consistent.
    /// </summary>
    public static class UiPaletteApplier
    {
        private static readonly Color PANEL_DARK = new Color32(0x1F, 0x2E, 0x52, 0xEB);
        private static readonly Color PANEL_DEEP = new Color32(0x14, 0x1C, 0x33, 0xF0);
        private static readonly Color BACKDROP = new Color32(0x00, 0x00, 0x00, 0xCC);
        private static readonly Color ACCENT_BLUE = new Color32(0x33, 0x66, 0xB2, 0xFF);
        private static readonly Color SUCCESS_GREEN = new Color32(0x2E, 0xD3, 0x6A, 0xFF);
        private static readonly Color TITLE_GOLD = new Color32(0xFF, 0xD9, 0x3B, 0xFF);
        private static readonly Color DARK_TEXT = new Color32(0x4A, 0x2A, 0x10, 0xFF);

        private static readonly string[] PANEL_DARK_NAMES =
        {
            "MovesPanel", "ScorePanel", "Pack1", "Pack2", "Pack3", "Header", "BottomNav",
            "GoalSlot_0", "GoalSlot_1", "GoalSlot_2", "GoalSlot_3",
            "SES EFEKTLERİ_Row", "MÜZİK_Row", "TİTREŞİM_Row"
        };

        private static readonly string[] PANEL_DEEP_NAMES = { "GoalsContainer", "ProgressBarBg", "LivesWidget", "CoinsWidget" };
        private static readonly string[] ACCENT_BLUE_NAMES = { "ToggleBtn", "Badge" };
        private static readonly string[] SUCCESS_NAMES = { "CheckBadge", "ProgressBarFill" };
        private static readonly string[] DARK_TEXT_NAMES = { "GoalDescriptionText", "DescText", "RewardText" };

        [MenuItem("Match-3/UI/Apply Palette")]
        public static void Apply()
        {
            int changed = 0;

            foreach (Image image in Resources.FindObjectsOfTypeAll<Image>())
            {
                if (!image.gameObject.scene.IsValid() || image.transform.parent == null)
                {
                    continue;
                }

                if (TryResolveImageColor(image, out Color color) && image.color != color)
                {
                    Undo.RecordObject(image, "Apply UI Palette");
                    image.color = color;
                    EditorUtility.SetDirty(image);
                    changed++;
                }
            }

            foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (!text.gameObject.scene.IsValid() || text.transform.parent == null)
                {
                    continue;
                }

                if (TryResolveTextColor(text, out Color color) && text.color != color)
                {
                    Undo.RecordObject(text, "Apply UI Palette");
                    text.color = color;
                    EditorUtility.SetDirty(text);
                    changed++;
                }
            }

            if (changed > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            }
        }

        private static bool TryResolveImageColor(Image image, out Color color)
        {
            string name = image.name;

            if (name == "Backdrop" || name == "WinLosePopup")
            {
                color = BACKDROP;
                return true;
            }

            if (name == "Fill" && image.transform.parent.name == "Fill Area")
            {
                color = TITLE_GOLD;
                return true;
            }

            if (name == "Background" && image.transform.parent.name == "Slider")
            {
                color = PANEL_DEEP;
                return true;
            }

            if (System.Array.IndexOf(PANEL_DARK_NAMES, name) >= 0)
            {
                color = PANEL_DARK;
                return true;
            }

            if (System.Array.IndexOf(PANEL_DEEP_NAMES, name) >= 0)
            {
                color = PANEL_DEEP;
                return true;
            }

            if (System.Array.IndexOf(ACCENT_BLUE_NAMES, name) >= 0)
            {
                color = ACCENT_BLUE;
                return true;
            }

            if (System.Array.IndexOf(SUCCESS_NAMES, name) >= 0)
            {
                color = SUCCESS_GREEN;
                return true;
            }

            color = default;
            return false;
        }

        private static bool TryResolveTextColor(TMP_Text text, out Color color)
        {
            if (System.Array.IndexOf(DARK_TEXT_NAMES, text.name) >= 0)
            {
                color = DARK_TEXT;
                return true;
            }

            bool isDialogTitle = (text.name == "Title" || text.name == "TitleText")
                && (text.transform.parent.name == "DialogContainer" || text.transform.parent.name == "Header");
            if (isDialogTitle)
            {
                color = DARK_TEXT;
                return true;
            }

            Color c = text.color;
            bool isYellow = c.r > 0.95f && c.g > 0.78f && c.g < 0.97f && c.b > 0.1f && c.b < 0.55f;
            if (isYellow)
            {
                color = TITLE_GOLD;
                return true;
            }

            color = default;
            return false;
        }
    }
}
