using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using View.MainMenu;

namespace Editor
{
    public static class InventoryPopupBuilder
    {
        private static readonly Color32 TITLE_BROWN = new Color32(0x4A, 0x2A, 0x10, 0xFF);
        private static readonly Color32 DISC_NAVY = new Color32(0x1B, 0x2A, 0x4E, 0xFF);
        private static readonly Color32 COUNT_GOLD = new Color32(0xFF, 0xD9, 0x3B, 0xFF);

        [MenuItem("Match-3/UI/Build Inventory Popup")]
        public static void Build()
        {
            var popupCanvasGo = GameObject.Find("PopupCanvas");
            if (popupCanvasGo == null)
            {
                Debug.LogError("PopupCanvas not found in scene!");
                return;
            }

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var windowSprite = LoadSprite("Assets/GemHunterMatch/Images/UI/SPrite_UI_Popup_rounded.png");
            var panelSprite = LoadSprite("Assets/GemHunterMatch/Images/UI/SPrite_UI_Panel_rounded.png");
            var circleFrameSprite = LoadSprite("Assets/GemHunterMatch/Images/UI/Sprite_UI_Avatar_frame.png");
            var buttonSprite = LoadSprite("Assets/GemHunterMatch/Images/UI/Sprite_Button_green.png");
            var closeSprite = LoadSprite("Assets/Sprites/UI/Icon_Close_Red.png");
            var hammerSprite = LoadSprite("Assets/Sprites/UI/Icon_Hammer.png");
            var gloveSprite = LoadSprite("Assets/Sprites/UI/Icon_Glove_Flat.png");
            var shuffleSprite = LoadSprite("Assets/Sprites/UI/Icon_Shuffle_Flat.png");

            var existing = popupCanvasGo.transform.Find("InventoryPopup");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            var hostGo = new GameObject("InventoryPopup", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(hostGo, "Create InventoryPopup");
            hostGo.transform.SetParent(popupCanvasGo.transform, false);
            Stretch(hostGo.GetComponent<RectTransform>());

            var popupView = hostGo.AddComponent<InventoryPopupView>();

            var backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdropGo.transform.SetParent(hostGo.transform, false);
            Stretch(backdropGo.GetComponent<RectTransform>());
            var backdropImage = backdropGo.GetComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.75f);
            backdropImage.raycastTarget = true;

            var dialogGo = new GameObject("DialogContainer", typeof(RectTransform), typeof(Image));
            dialogGo.transform.SetParent(backdropGo.transform, false);
            var dialogRt = dialogGo.GetComponent<RectTransform>();
            dialogRt.sizeDelta = new Vector2(850f, 720f);
            dialogRt.anchoredPosition = Vector2.zero;
            var dialogImage = dialogGo.GetComponent<Image>();
            dialogImage.sprite = windowSprite;
            dialogImage.type = Image.Type.Sliced;

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(dialogGo.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -82.5f);
            titleRt.sizeDelta = new Vector2(620f, 75f);
            var titleText = titleGo.GetComponent<TextMeshProUGUI>();
            titleText.font = fontAsset;
            titleText.fontSize = 42f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = TITLE_BROWN;
            titleText.text = "GÜÇLENDİRİCİLER";

            var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(dialogGo.transform, false);
            var closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = closeRt.anchorMax = Vector2.one;
            closeRt.anchoredPosition = new Vector2(-40f, -40f);
            closeRt.sizeDelta = new Vector2(80f, 80f);
            var closeImage = closeGo.GetComponent<Image>();
            closeImage.sprite = closeSprite;
            closeImage.preserveAspect = true;
            var closeButton = closeGo.GetComponent<Button>();

            var hammerCountText = CreateBoosterCard("HammerCard", dialogGo.transform, -260f, hammerSprite,
                fontAsset, panelSprite, circleFrameSprite);
            var gloveCountText = CreateBoosterCard("GloveCard", dialogGo.transform, 0f, gloveSprite,
                fontAsset, panelSprite, circleFrameSprite);
            var shuffleCountText = CreateBoosterCard("ShuffleCard", dialogGo.transform, 260f, shuffleSprite,
                fontAsset, panelSprite, circleFrameSprite);

            var shopGo = new GameObject("ShopButton", typeof(RectTransform), typeof(Image), typeof(Button));
            shopGo.transform.SetParent(dialogGo.transform, false);
            var shopRt = shopGo.GetComponent<RectTransform>();
            shopRt.anchorMin = shopRt.anchorMax = new Vector2(0.5f, 0f);
            shopRt.anchoredPosition = new Vector2(0f, 95f);
            shopRt.sizeDelta = new Vector2(360f, 100f);
            var shopImage = shopGo.GetComponent<Image>();
            shopImage.sprite = buttonSprite;
            shopImage.type = Image.Type.Sliced;
            var shopButton = shopGo.GetComponent<Button>();

            var shopTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            shopTextGo.transform.SetParent(shopGo.transform, false);
            var shopTextRt = shopTextGo.GetComponent<RectTransform>();
            Stretch(shopTextRt);
            shopTextRt.offsetMin = new Vector2(10f, 10f);
            shopTextRt.offsetMax = new Vector2(-10f, -10f);
            var shopText = shopTextGo.GetComponent<TextMeshProUGUI>();
            shopText.font = fontAsset;
            shopText.fontSize = 28f;
            shopText.fontStyle = FontStyles.Bold;
            shopText.alignment = TextAlignmentOptions.Center;
            shopText.color = Color.white;
            shopText.text = "DAHA FAZLA AL";

            var shopPopupView = popupCanvasGo.GetComponentInChildren<ShopPopupView>(true);

            SetPrivateField(popupView, "_root", backdropGo);
            SetPrivateField(popupView, "_dialogContainer", dialogRt);
            SetPrivateField(popupView, "_closeButton", closeButton);
            SetPrivateField(popupView, "_shopButton", shopButton);
            SetPrivateField(popupView, "_shopPopup", shopPopupView);
            SetPrivateField(popupView, "_hammerCountText", hammerCountText);
            SetPrivateField(popupView, "_gloveCountText", gloveCountText);
            SetPrivateField(popupView, "_shuffleCountText", shuffleCountText);

            var navBarView = Object.FindAnyObjectByType<MenuBottomNavBarView>();
            if (navBarView != null)
            {
                SetPrivateField(navBarView, "_inventoryPopup", popupView);
                EditorUtility.SetDirty(navBarView);
            }

            backdropGo.SetActive(false);

            EditorUtility.SetDirty(hostGo);
            EditorUtility.SetDirty(popupCanvasGo);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("<color=green>InventoryPopup successfully built and wired!</color>");
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI CreateBoosterCard(string name, Transform parent, float x, Sprite icon,
            TMP_FontAsset font, Sprite panelSprite, Sprite circleFrameSprite)
        {
            var cardGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(parent, false);
            var cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.anchoredPosition = new Vector2(x, 10f);
            cardRt.sizeDelta = new Vector2(230f, 290f);
            var cardImage = cardGo.GetComponent<Image>();
            cardImage.sprite = panelSprite;
            cardImage.type = Image.Type.Sliced;

            var frameGo = new GameObject("IconFrame", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(cardGo.transform, false);
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchoredPosition = new Vector2(0f, 40f);
            frameRt.sizeDelta = new Vector2(160f, 160f);
            var frameImage = frameGo.GetComponent<Image>();
            frameImage.sprite = circleFrameSprite;
            frameImage.preserveAspect = true;

            var discGo = new GameObject("IconDisc", typeof(RectTransform), typeof(Image));
            discGo.transform.SetParent(frameGo.transform, false);
            var discRt = discGo.GetComponent<RectTransform>();
            discRt.anchoredPosition = Vector2.zero;
            discRt.sizeDelta = new Vector2(142f, 142f);
            var discImage = discGo.GetComponent<Image>();
            discImage.sprite = circleFrameSprite;
            discImage.color = DISC_NAVY;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(frameGo.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(100f, 100f);
            var iconImage = iconGo.GetComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;

            var countGo = new GameObject("CountText", typeof(RectTransform), typeof(TextMeshProUGUI));
            countGo.transform.SetParent(cardGo.transform, false);
            var countRt = countGo.GetComponent<RectTransform>();
            countRt.anchoredPosition = new Vector2(0f, -88f);
            countRt.sizeDelta = new Vector2(190f, 60f);
            var countText = countGo.GetComponent<TextMeshProUGUI>();
            countText.font = font;
            countText.fontSize = 44f;
            countText.fontStyle = FontStyles.Bold;
            countText.alignment = TextAlignmentOptions.Center;
            countText.color = COUNT_GOLD;
            countText.text = "x3";

            return countText;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null)
            {
                return;
            }

            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
            }
        }
    }
}
