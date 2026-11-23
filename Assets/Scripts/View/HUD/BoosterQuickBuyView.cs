using Core.Data.Boosters;
using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View.HUD
{
    /// <summary>
    /// In-level purchase dialog for an empty booster. Built in code under the host's canvas so it needs
    /// no scene wiring. It only shows the cached balance and raises OnQuickBuyRequested, the inventory
    /// spends the coins and grants the booster.
    /// </summary>
    public sealed class BoosterQuickBuyView : MonoBehaviour
    {
        private static readonly Color DIM_COLOR = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color PANEL_COLOR = new Color(0.1f, 0.2f, 0.38f, 1f);
        private static readonly Color BUY_COLOR = new Color(0.95f, 0.7f, 0.15f, 1f);
        private static readonly Color CANCEL_COLOR = new Color(0.35f, 0.4f, 0.5f, 1f);

        [SerializeField] private RectTransform _parent;
        [SerializeField] private int _price = 100;

        private GameObject _root;
        private TMP_Text _titleText;
        private TMP_Text _messageText;
        private BoosterType _pending;
        private int _coins;

        private void Awake()
        {
            Build();
        }

        private void OnEnable()
        {
            EconomySignals.OnBoosterPurchaseRequested += HandlePurchaseRequested;
            EconomySignals.OnCoinsChanged += HandleCoinsChanged;
            _root.SetActive(false);
        }

        private void OnDisable()
        {
            EconomySignals.OnBoosterPurchaseRequested -= HandlePurchaseRequested;
            EconomySignals.OnCoinsChanged -= HandleCoinsChanged;
            _root.SetActive(false);
        }

        private void HandleCoinsChanged(int coins)
        {
            _coins = coins;
        }

        private void HandlePurchaseRequested(BoosterType type)
        {
            _pending = type;
            _titleText.SetText("Out of " + type);
            UpdateMessage();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        private void UpdateMessage()
        {
            int coins = _coins;
            if (coins >= _price)
            {
                _messageText.SetText("Buy one for {0} coins?\nYou have {1} coins.", (float)_price, (float)coins);
            }
            else
            {
                _messageText.SetText("Not enough coins.\nYou need {0}, you have {1}.", (float)_price, (float)coins);
            }
        }

        private void HandleBuyClicked()
        {
            _root.SetActive(false);
            EconomySignals.RaiseQuickBuyRequested(_pending, _price);
        }

        private void HandleCancelClicked()
        {
            _root.SetActive(false);
        }

        private void Build()
        {
            RectTransform host = _parent != null ? _parent : (RectTransform)transform;

            _root = new GameObject("BoosterQuickBuy", typeof(RectTransform), typeof(Image));
            RectTransform rootRect = (RectTransform)_root.transform;
            rootRect.SetParent(host, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            _root.GetComponent<Image>().color = DIM_COLOR;

            RectTransform panel = CreateImage("Panel", rootRect, PANEL_COLOR, new Vector2(760f, 460f), Vector2.zero);
            _titleText = CreateText("Title", panel, 52f, new Vector2(0f, 150f), new Vector2(700f, 90f));
            _messageText = CreateText("Message", panel, 38f, new Vector2(0f, 20f), new Vector2(700f, 150f));

            CreateButton("Buy", panel, "Buy", BUY_COLOR, new Vector2(-170f, -150f), HandleBuyClicked);
            CreateButton("Cancel", panel, "Close", CANCEL_COLOR, new Vector2(170f, -150f), HandleCancelClicked);
        }

        private static RectTransform CreateImage(string objectName, RectTransform parent, Color color, Vector2 size, Vector2 position)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private static TMP_Text CreateText(string objectName, RectTransform parent, float fontSize, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateButton(string objectName, RectTransform parent, string label, Color color, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = CreateImage(objectName, parent, color, new Vector2(300f, 110f), position);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(onClick);

            TMP_Text text = CreateText("Label", rect, 44f, Vector2.zero, new Vector2(280f, 100f));
            text.SetText(label);
        }
    }
}
