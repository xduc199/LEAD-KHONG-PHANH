using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ShopItemCard : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES
    // ============================================================

    private Image background;
    private Image preview;

    private TMP_Text itemName;
    private TMP_Text priceText;
    private TMP_Text itemNumber;
    private TMP_Text stateText;

    private Button clickButton;


    // ============================================================
    // RUNTIME
    // ============================================================

    private MainMenuShopUI owner;

    private int itemId = -1;
    private int price;

    private MainMenuShopUI.CurrencyType currency;

    private bool selected;
    private bool owned;


    // ============================================================
    // PUBLIC
    // ============================================================

    public int ItemId
    {
        get { return itemId; }
    }

    public bool IsOwned
    {
        get { return owned; }
    }


    // ============================================================
    // COLORS
    // ============================================================

    private static readonly Color NormalColor =
        new Color(
            0.055f,
            0.065f,
            0.078f,
            1f);

    private static readonly Color SelectedColor =
        new Color(
            0.02f,
            0.20f,
            0.25f,
            1f);

    private static readonly Color NormalPreviewColor =
        new Color(
            0.075f,
            0.085f,
            0.10f,
            1f);

    private static readonly Color SelectedPreviewColor =
        new Color(
            0.02f,
            0.82f,
            1f,
            1f);

    private static readonly Color WhiteColor =
        new Color(
            0.95f,
            0.96f,
            0.98f,
            1f);

    private static readonly Color MutedColor =
        new Color(
            0.58f,
            0.62f,
            0.68f,
            1f);

    private static readonly Color OwnedColor =
        new Color(
            0.25f,
            1f,
            0.55f,
            1f);


    // ============================================================
    // LIFECYCLE
    // ============================================================

    private void Awake()
    {
        CacheReferences();
        BindClickButton();
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        CacheReferences();
    }

#endif


    // ============================================================
    // CACHE REFERENCES
    // ============================================================

    private void CacheReferences()
    {
        background =
            FindImage(
                "Background",
                background);

        preview =
            FindImage(
                "Preview",
                preview);

        itemName =
            FindTMP(
                "ItemName",
                itemName);

        priceText =
            FindTMP(
                "Price",
                priceText);

        itemNumber =
            FindTMP(
                "ItemNumber",
                itemNumber);

        stateText =
            FindTMP(
                "State",
                stateText);

        clickButton =
            FindButton(
                "Button",
                clickButton);


        // --------------------------------------------------------
        // BACKGROUND FALLBACK
        // --------------------------------------------------------

        if (background == null)
        {
            background =
                GetComponent<Image>();
        }


        // --------------------------------------------------------
        // PREVIEW FALLBACK
        // --------------------------------------------------------

        if (preview == null)
        {
            Image[] images =
                GetComponentsInChildren<Image>(true);

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];

                if (image == null)
                    continue;

                if (image == background)
                    continue;

                if (image.gameObject.name == "Preview")
                {
                    preview = image;
                    break;
                }
            }
        }


        // --------------------------------------------------------
        // TMP FALLBACK
        // --------------------------------------------------------

        TMP_Text[] texts =
            GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text == null)
                continue;

            string textName =
                text.gameObject.name;

            if (
                itemName == null &&
                textName == "ItemName")
            {
                itemName = text;
                continue;
            }

            if (
                priceText == null &&
                textName == "Price")
            {
                priceText = text;
                continue;
            }

            if (
                itemNumber == null &&
                textName == "ItemNumber")
            {
                itemNumber = text;
                continue;
            }

            if (
                stateText == null &&
                textName == "State")
            {
                stateText = text;
            }
        }


        // --------------------------------------------------------
        // BUTTON FALLBACK
        // --------------------------------------------------------

        if (clickButton == null)
        {
            clickButton =
                GetComponent<Button>();
        }

        if (clickButton == null)
        {
            clickButton =
                GetComponentInChildren<Button>(true);
        }
    }


    // ============================================================
    // FIND IMAGE
    // ============================================================

    private Image FindImage(
        string objectName,
        Image current)
    {
        if (current != null)
            return current;

        Transform child =
            FindChildRecursive(
                transform,
                objectName);

        if (child == null)
            return null;

        return child.GetComponent<Image>();
    }


    // ============================================================
    // FIND TMP
    // ============================================================

    private TMP_Text FindTMP(
        string objectName,
        TMP_Text current)
    {
        if (current != null)
            return current;

        Transform child =
            FindChildRecursive(
                transform,
                objectName);

        if (child == null)
            return null;

        return child.GetComponent<TMP_Text>();
    }


    // ============================================================
    // FIND BUTTON
    // ============================================================

    private Button FindButton(
        string objectName,
        Button current)
    {
        if (current != null)
            return current;

        Transform child =
            FindChildRecursive(
                transform,
                objectName);

        if (child == null)
            return null;

        return child.GetComponent<Button>();
    }


    // ============================================================
    // RECURSIVE SEARCH
    // ============================================================

    private static Transform FindChildRecursive(
        Transform root,
        string targetName)
    {
        if (root == null)
            return null;

        int childCount =
            root.childCount;

        for (int i = 0; i < childCount; i++)
        {
            Transform child =
                root.GetChild(i);

            if (child.name == targetName)
                return child;

            Transform result =
                FindChildRecursive(
                    child,
                    targetName);

            if (result != null)
                return result;
        }

        return null;
    }


    // ============================================================
    // BUTTON BIND
    // ============================================================

    private void BindClickButton()
    {
        if (clickButton == null)
        {
            Debug.LogWarning(
                "[ShopItemCard] Button không tồn tại.",
                this);

            return;
        }

        clickButton.onClick.RemoveListener(
            OnClick);

        clickButton.onClick.AddListener(
            OnClick);
    }


    // ============================================================
    // BIND ITEM
    // ============================================================

    public void Bind(
        MainMenuShopUI shop,
        int id,
        string name,
        int itemPrice,
        MainMenuShopUI.CurrencyType itemCurrency,
        Color currencyColor)
    {
        CacheReferences();

        owner = shop;

        itemId = id;

        price =
            Mathf.Max(
                0,
                itemPrice);

        currency =
            itemCurrency;

        selected = false;

        owned =
            InventoryOwnership.IsOwned(
                itemId);

        gameObject.SetActive(true);


        // --------------------------------------------------------
        // NAME
        // --------------------------------------------------------

        if (itemName != null)
        {
            itemName.text =
                string.IsNullOrEmpty(name)
                    ? "ITEM " + id.ToString("000")
                    : name;

            itemName.color =
                WhiteColor;
        }


        // --------------------------------------------------------
        // ITEM NUMBER
        // --------------------------------------------------------

        if (itemNumber != null)
        {
            itemNumber.text =
                "#" +
                id.ToString("000");

            itemNumber.color =
                MutedColor;
        }


        // --------------------------------------------------------
        // PRICE
        // --------------------------------------------------------

        RefreshPriceVisual(
            currencyColor);

        // --------------------------------------------------------
        // STATE
        // --------------------------------------------------------

        RefreshStateVisual();

        // --------------------------------------------------------
        // VISUAL
        // --------------------------------------------------------

        RefreshVisual();
    }


    // ============================================================
    // PRICE VISUAL
    // ============================================================

    private void RefreshPriceVisual(
        Color currencyColor)
    {
        if (priceText == null)
            return;

        if (owned)
        {
            priceText.text =
                "ĐÃ SỞ HỮU";

            priceText.color =
                OwnedColor;

            return;
        }

        priceText.text =
            FormatPrice(
                price,
                currency);

        priceText.color =
            currencyColor;
    }


    // ============================================================
    // HIDE
    // ============================================================

    public void Hide()
    {
        owner = null;

        itemId = -1;

        price = 0;

        selected = false;

        owned = false;

        if (itemName != null)
            itemName.text = string.Empty;

        if (priceText != null)
            priceText.text = string.Empty;

        if (itemNumber != null)
            itemNumber.text = string.Empty;

        if (stateText != null)
            stateText.text = string.Empty;

        gameObject.SetActive(false);
    }


    // ============================================================
    // CLICK
    // ============================================================

    public void OnClick()
    {
        if (owner == null)
            return;

        if (itemId < 0)
            return;

        owner.SelectItemFromCard(
            this);
    }


    // ============================================================
    // OWNERSHIP
    // ============================================================

    public void RefreshOwnership()
    {
        if (itemId < 0)
            return;

        owned =
            InventoryOwnership.IsOwned(
                itemId);

        RefreshStateVisual();

        RefreshPriceVisual(
            GetCurrencyColor(
                currency));
    }


    // ============================================================
    // CURRENCY COLOR
    // ============================================================

    private static Color GetCurrencyColor(
        MainMenuShopUI.CurrencyType type)
    {
        if (
            type ==
            MainMenuShopUI.CurrencyType.Gem)
        {
            return new Color(
                0.3f,
                0.9f,
                1f,
                1f);
        }

        return new Color(
            1f,
            0.75f,
            0.1f,
            1f);
    }


    // ============================================================
    // SELECTION
    // ============================================================

    public void SetSelected(
        bool value)
    {
        if (selected == value)
            return;

        selected = value;

        RefreshVisual();
    }


    // ============================================================
    // VISUAL
    // ============================================================

    private void RefreshVisual()
    {
        if (background != null)
        {
            background.color =
                selected
                    ? SelectedColor
                    : NormalColor;
        }

        if (preview != null)
        {
            preview.color =
                selected
                    ? SelectedPreviewColor
                    : NormalPreviewColor;
        }
    }


    // ============================================================
    // STATE
    // ============================================================

    private void RefreshStateVisual()
    {
        if (stateText == null)
            return;

        if (owned)
        {
            stateText.text =
                "ĐÃ SỞ HỮU";

            stateText.color =
                OwnedColor;
        }
        else
        {
            stateText.text =
                string.Empty;
        }
    }


    // ============================================================
    // FORMAT PRICE
    // ============================================================

    private static string FormatPrice(
        int value,
        MainMenuShopUI.CurrencyType type)
    {
        if (
            type ==
            MainMenuShopUI.CurrencyType.Gem)
        {
            return
                "♦ " +
                value.ToString("N0") +
                " GEM";
        }

        return
            "● " +
            value.ToString("N0") +
            " GOLD";
    }
}