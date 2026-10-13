using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InventoryItemCard : MonoBehaviour
{
    // =========================================================
    // CHILD NAMES
    // =========================================================

    private const string BackgroundName = "Background";
    private const string PreviewName = "Preview";
    private const string ItemNameName = "ItemName";
    private const string ItemNumberName = "ItemNumber";
    private const string StateName = "State";
    private const string ButtonName = "Button";


    // =========================================================
    // UI REFERENCES
    // =========================================================

    private Image background;
    private Image preview;

    private TMP_Text itemName;
    private TMP_Text itemNumber;
    private TMP_Text stateText;

    /*
     * QUAN TRỌNG:
     *
     * Button chính LUÔN nằm trên ROOT của InventoryItemCard.
     *
     * Không dùng Button child làm hitbox chính nữa.
     */
    private Button button;
    private Image buttonGraphic;

    private bool referencesCached;
    private bool referencesValid;
    private bool buttonBound;


    // =========================================================
    // RUNTIME IDENTITY
    // =========================================================

    private int itemId = -1;

    private InventoryUI.InventoryCategory category =
        InventoryUI.InventoryCategory.Outfit;

    private InventoryUI owner;


    // =========================================================
    // COLORS
    // =========================================================

    private static readonly Color NormalColor =
        new Color(
            0.055f,
            0.065f,
            0.078f,
            1f);

    private static readonly Color SelectedColor =
        new Color(
            0.12f,
            0.16f,
            0.20f,
            1f);

    private static readonly Color PreviewColor =
        new Color(
            0.075f,
            0.085f,
            0.10f,
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

    private static readonly Color EquippedColor =
        new Color(
            1f,
            0.78f,
            0.20f,
            1f);

    private static readonly Color TransparentButtonColor =
        new Color(
            1f,
            1f,
            1f,
            0f);


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        EnsureUIReferences();
        BindButton();
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        referencesCached = false;
        referencesValid = false;
        buttonBound = false;

        EnsureUIReferences();
    }

#endif


    // =========================================================
    // INITIALIZE
    // =========================================================

    public void Initialize(
        InventoryUI inventoryUI)
    {
        owner =
            inventoryUI;

        EnsureUIReferences();
        BindButton();

        if (!referencesValid)
        {
            Debug.LogError(
                "[InventoryItemCard] " +
                "Initialize failed because UI references are invalid.",
                this);

            return;
        }

        PrepareButtonForClick();

        Clear();
    }


    // =========================================================
    // ENSURE UI REFERENCES
    // =========================================================

    private void EnsureUIReferences()
    {
        if (
            referencesCached &&
            referencesValid
        )
        {
            PrepareExistingGraphicsForRaycastSafety();

            if (button != null)
            {
                PrepareButtonForClick();
            }

            return;
        }

        referencesCached = true;


        // =====================================================
        // BACKGROUND
        // =====================================================

        background =
            FindImage(
                BackgroundName);

        if (background == null)
        {
            background =
                CreateImageChild(
                    BackgroundName);

            ConfigureBackground(
                background);
        }


        // =====================================================
        // PREVIEW
        // =====================================================

        preview =
            FindImage(
                PreviewName);

        if (preview == null)
        {
            preview =
                CreateImageChild(
                    PreviewName);

            ConfigurePreview(
                preview);
        }


        // =====================================================
        // ITEM NAME
        // =====================================================

        itemName =
            FindTMP(
                ItemNameName);

        if (itemName == null)
        {
            itemName =
                CreateTextChild(
                    ItemNameName);

            ConfigureItemName(
                itemName);
        }


        // =====================================================
        // ITEM NUMBER
        // =====================================================

        itemNumber =
            FindTMP(
                ItemNumberName);

        if (itemNumber == null)
        {
            itemNumber =
                CreateTextChild(
                    ItemNumberName);

            ConfigureItemNumber(
                itemNumber);
        }


        // =====================================================
        // STATE
        // =====================================================

        stateText =
            FindTMP(
                StateName);

        if (stateText == null)
        {
            stateText =
                CreateTextChild(
                    StateName);

            ConfigureState(
                stateText);
        }


        // =====================================================
        // BUTTON
        // =====================================================

        ResolveRootButton();


        // =====================================================
        // VALIDATE
        // =====================================================

        referencesValid =
            background != null &&
            preview != null &&
            itemName != null &&
            itemNumber != null &&
            stateText != null &&
            button != null;


        if (!referencesValid)
        {
            Debug.LogError(
                "[InventoryItemCard] " +
                "Cannot resolve all UI references.",
                this);

            return;
        }


        // =====================================================
        // RAYCAST SAFETY
        // =====================================================

        PrepareExistingGraphicsForRaycastSafety();

        PrepareButtonForClick();
    }


    // =========================================================
    // RESOLVE ROOT BUTTON
    // =========================================================

    private void ResolveRootButton()
    {
        /*
         * BUTTON CONTRACT:
         *
         * InventoryItemCard
         * ├── Button      <-- hitbox duy nhất
         * ├── Background
         * ├── Preview
         * ├── ItemName
         * ├── ItemNumber
         * └── State
         *
         * Không sử dụng Button child nữa.
         */

        button =
            GetComponent<Button>();


        if (button == null)
        {
            button =
                gameObject.AddComponent<Button>();
        }


        buttonGraphic =
            GetComponent<Image>();


        if (buttonGraphic == null)
        {
            buttonGraphic =
                gameObject.AddComponent<Image>();
        }


        /*
         * Root Image chỉ dùng làm hitbox.
         *
         * Nó trong suốt nên không phá visual.
         */
        buttonGraphic.color =
            TransparentButtonColor;

        buttonGraphic.raycastTarget =
            true;


        /*
         * Nếu prefab cũ còn Button child,
         * vô hiệu hóa chúng để tránh nhiều hitbox.
         */
        DisableChildButtons();
    }


    // =========================================================
    // DISABLE CHILD BUTTONS
    // =========================================================

    private void DisableChildButtons()
    {
        Button[] childButtons =
            GetComponentsInChildren<Button>(
                true);


        if (childButtons == null)
        {
            return;
        }


        int count =
            childButtons.Length;


        for (
            int i = 0;
            i < count;
            i++
        )
        {
            Button childButton =
                childButtons[i];


            if (childButton == null)
            {
                continue;
            }


            /*
             * Button trên ROOT được giữ lại.
             */
            if (childButton == button)
            {
                continue;
            }


            childButton.interactable =
                false;

            childButton.enabled =
                false;


            Image childGraphic =
                childButton.GetComponent<Image>();


            if (childGraphic != null)
            {
                childGraphic.raycastTarget =
                    false;
            }
        }
    }


    // =========================================================
    // RAYCAST SAFETY
    // =========================================================

    private void PrepareExistingGraphicsForRaycastSafety()
    {
        /*
         * Chỉ ROOT BUTTON được nhận pointer event.
         *
         * Background / Preview / TMP không được chặn raycast.
         */

        if (background != null)
        {
            background.raycastTarget =
                false;
        }


        if (preview != null)
        {
            preview.raycastTarget =
                false;
        }


        if (itemName != null)
        {
            itemName.raycastTarget =
                false;
        }


        if (itemNumber != null)
        {
            itemNumber.raycastTarget =
                false;
        }


        if (stateText != null)
        {
            stateText.raycastTarget =
                false;
        }
    }


    // =========================================================
    // BUTTON BIND
    // =========================================================

    private void BindButton()
    {
        if (button == null)
        {
            return;
        }


        PrepareButtonForClick();


        /*
         * Mỗi instance tự bind OnClick của chính nó.
         *
         * Card 1 -> Card 1.OnClick()
         * Card 2 -> Card 2.OnClick()
         * Card 3 -> Card 3.OnClick()
         *
         * Không dùng index.
         * Không dùng static callback.
         * Không dùng card cố định.
         */

        button.onClick.RemoveListener(
            OnClick);

        button.onClick.AddListener(
            OnClick);

        buttonBound =
            true;
    }


    // =========================================================
    // PREPARE BUTTON
    // =========================================================

    private void PrepareButtonForClick()
    {
        if (button == null)
        {
            return;
        }


        button.enabled =
            true;

        button.interactable =
            true;


        if (buttonGraphic == null)
        {
            buttonGraphic =
                GetComponent<Image>();
        }


        if (buttonGraphic == null)
        {
            buttonGraphic =
                gameObject.AddComponent<Image>();
        }


        buttonGraphic.raycastTarget =
            true;

        buttonGraphic.color =
            TransparentButtonColor;


        button.targetGraphic =
            buttonGraphic;


        RectTransform buttonRect =
            transform as RectTransform;


        if (buttonRect != null)
        {
            /*
             * Button là ROOT nên nó tự có kích thước
             * của chính Card.
             *
             * Không stretch child Button.
             * Không SetAsLastSibling().
             */
            buttonRect.localScale =
                Vector3.one;
        }


        DisableChildButtons();

        PrepareExistingGraphicsForRaycastSafety();
    }


    // =========================================================
    // CLICK
    // =========================================================

    public void OnClick()
    {
        /*
         * Đây là public API.
         *
         * Có thể:
         *
         * 1. Runtime bind tự động.
         * 2. Inspector Button -> OnClick -> InventoryItemCard.OnClick().
         *
         * Tuyệt đối không lấy ID theo index.
         */

        if (
            itemId <
            GameItemDatabase.MinItemId
        )
        {
            Debug.LogWarning(
                "[InventoryItemCard] " +
                "Click ignored: ItemId chưa được Bind.\n" +
                "Card = " +
                gameObject.name,
                this);

            return;
        }


        if (
            itemId >
            GameItemDatabase.MaxItemId
        )
        {
            Debug.LogWarning(
                "[InventoryItemCard] " +
                "Click ignored: ItemId invalid.\n" +
                "ItemId = " +
                itemId,
                this);

            return;
        }


        if (owner == null)
        {
            Debug.LogError(
                "[InventoryItemCard] " +
                "Owner InventoryUI == NULL.\n" +
                "Card = " +
                gameObject.name +
                "\n" +
                "ItemId = " +
                itemId,
                this);

            return;
        }


        Debug.Log(
            "[InventoryItemCard] CLICK | " +
            "Card=" +
            gameObject.name +
            " | ItemId=" +
            itemId +
            " | Name=" +
            (
                itemName != null
                    ? itemName.text
                    : "NULL"
            ),
            this);


        owner.SelectItem(
            itemId,
            category);
    }


    // =========================================================
    // CLEAR
    // =========================================================

    public void Clear()
    {
        itemId =
            -1;

        category =
            InventoryUI.InventoryCategory.Outfit;


        if (background != null)
        {
            background.color =
                NormalColor;
        }


        if (preview != null)
        {
            preview.sprite =
                null;

            preview.color =
                PreviewColor;
        }


        if (itemName != null)
        {
            itemName.text =
                string.Empty;
        }


        if (itemNumber != null)
        {
            itemNumber.text =
                string.Empty;
        }


        if (stateText != null)
        {
            stateText.text =
                string.Empty;
        }


        if (button != null)
        {
            button.enabled =
                true;

            button.interactable =
                true;
        }
    }


    // =========================================================
    // BIND ITEM
    // =========================================================

    public void Bind(
        int id,
        string name,
        Sprite icon,
        InventoryUI.InventoryCategory itemCategory,
        bool equipped)
    {
        EnsureUIReferences();


        if (!referencesValid)
        {
            Debug.LogError(
                "[InventoryItemCard] " +
                "Cannot Bind because UI references are invalid.",
                this);

            return;
        }


        // =====================================================
        // VALID ID
        // =====================================================

        if (
            id <
            GameItemDatabase.MinItemId ||
            id >
            GameItemDatabase.MaxItemId
        )
        {
            Debug.LogError(
                "[InventoryItemCard] " +
                "Invalid ItemId = " +
                id,
                this);

            Clear();

            return;
        }


        // =====================================================
        // AUTHORITATIVE IDENTITY
        // =====================================================

        itemId =
            id;

        category =
            itemCategory;


        // =====================================================
        // BUTTON
        // =====================================================

        ResolveRootButton();

        BindButton();

        PrepareButtonForClick();


        button.interactable =
            true;


        // =====================================================
        // BACKGROUND
        // =====================================================

        if (background != null)
        {
            background.color =
                equipped
                    ? EquippedColor
                    : NormalColor;
        }


        // =====================================================
        // ICON
        // =====================================================

        if (preview != null)
        {
            preview.sprite =
                icon;

            preview.preserveAspect =
                true;

            preview.color =
                icon != null
                    ? Color.white
                    : PreviewColor;

            preview.raycastTarget =
                false;
        }


        // =====================================================
        // NUMBER
        // =====================================================

        if (itemNumber != null)
        {
            itemNumber.text =
                "#" +
                id.ToString("000");

            itemNumber.color =
                MutedColor;

            itemNumber.raycastTarget =
                false;
        }


        // =====================================================
        // NAME
        // =====================================================

        if (itemName != null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                itemName.text =
                    "ITEM " +
                    id.ToString("000");
            }
            else
            {
                itemName.text =
                    name.Trim();
            }

            itemName.color =
                WhiteColor;

            itemName.raycastTarget =
                false;
        }


        // =====================================================
        // STATE
        // =====================================================

        if (stateText != null)
        {
            if (equipped)
            {
                stateText.text =
                    "ĐANG TRANG BỊ";

                stateText.color =
                    EquippedColor;
            }
            else
            {
                stateText.text =
                    "ĐÃ SỞ HỮU";

                stateText.color =
                    OwnedColor;
            }

            stateText.raycastTarget =
                false;
        }


        // =====================================================
        // FINAL RAYCAST STATE
        // =====================================================

        if (background != null)
        {
            background.raycastTarget =
                false;
        }

        if (preview != null)
        {
            preview.raycastTarget =
                false;
        }

        if (buttonGraphic != null)
        {
            buttonGraphic.raycastTarget =
                true;
        }
    }


    // =========================================================
    // SELECTED
    // =========================================================

    public void SetSelected(
        bool selected)
    {
        EnsureUIReferences();


        if (background == null)
        {
            return;
        }


        if (selected)
        {
            background.color =
                SelectedColor;
        }
        else
        {
            background.color =
                NormalColor;
        }
    }


    // =========================================================
    // PUBLIC IDENTITY
    // =========================================================

    public int ItemId =>
        itemId;


    public InventoryUI.InventoryCategory Category =>
        category;


    // =========================================================
    // UI FIND HELPERS
    // =========================================================

    private Image FindImage(
        string childName)
    {
        Transform child =
            transform.Find(
                childName);


        if (child == null)
        {
            return null;
        }


        return child.GetComponent<Image>();
    }


    private TMP_Text FindTMP(
        string childName)
    {
        Transform child =
            transform.Find(
                childName);


        if (child == null)
        {
            return null;
        }


        return child.GetComponent<TMP_Text>();
    }


    private Image CreateImageChild(
        string childName)
    {
        GameObject child =
            new GameObject(
                childName,
                typeof(RectTransform),
                typeof(Image));


        child.transform.SetParent(
            transform,
            false);


        return child.GetComponent<Image>();
    }


    private TMP_Text CreateTextChild(
        string childName)
    {
        GameObject child =
            new GameObject(
                childName,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));


        child.transform.SetParent(
            transform,
            false);


        return child.GetComponent<TMP_Text>();
    }


    // =========================================================
    // UI CONFIG
    // =========================================================

    private void ConfigureBackground(
        Image image)
    {
        if (image == null)
        {
            return;
        }


        image.color =
            NormalColor;

        image.raycastTarget =
            false;


        RectTransform rect =
            image.rectTransform;


        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;


        rect.SetAsFirstSibling();
    }


    private void ConfigurePreview(
        Image image)
    {
        if (image == null)
        {
            return;
        }


        image.color =
            PreviewColor;

        image.preserveAspect =
            true;

        image.raycastTarget =
            false;


        RectTransform rect =
            image.rectTransform;


        rect.anchorMin =
            new Vector2(
                0.10f,
                0.28f);

        rect.anchorMax =
            new Vector2(
                0.90f,
                0.88f);

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }


    private void ConfigureItemName(
        TMP_Text text)
    {
        if (text == null)
        {
            return;
        }


        text.text =
            string.Empty;

        text.color =
            WhiteColor;

        text.alignment =
            TextAlignmentOptions.Center;

        text.raycastTarget =
            false;


        RectTransform rect =
            text.rectTransform;


        rect.anchorMin =
            new Vector2(
                0.05f,
                0.03f);

        rect.anchorMax =
            new Vector2(
                0.95f,
                0.25f);

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }


    private void ConfigureItemNumber(
        TMP_Text text)
    {
        if (text == null)
        {
            return;
        }


        text.text =
            string.Empty;

        text.color =
            MutedColor;

        text.alignment =
            TextAlignmentOptions.TopLeft;

        text.raycastTarget =
            false;


        RectTransform rect =
            text.rectTransform;


        rect.anchorMin =
            new Vector2(
                0.04f,
                0.90f);

        rect.anchorMax =
            new Vector2(
                0.40f,
                0.99f);

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }


    private void ConfigureState(
        TMP_Text text)
    {
        if (text == null)
        {
            return;
        }


        text.text =
            string.Empty;

        text.color =
            OwnedColor;

        text.alignment =
            TextAlignmentOptions.Center;

        text.raycastTarget =
            false;


        RectTransform rect =
            text.rectTransform;


        rect.anchorMin =
            new Vector2(
                0.05f,
                0.25f);

        rect.anchorMax =
            new Vector2(
                0.95f,
                0.34f);

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }
}