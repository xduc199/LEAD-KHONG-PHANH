#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MainMenuInventoryUISetup
{
    // =====================================================================
    // MENU
    // =====================================================================

    private const string MenuPath =
        "Tools/LEAD KHONG PHANH/Setup Main Menu Inventory UI";

    // =====================================================================
    // ROOT
    // =====================================================================

    private const string INVENTORY_PANEL_NAME =
        "InventoryPanel";

    private const string INVENTORY_WINDOW_NAME =
        "InventoryWindow";

    // =====================================================================
    // STRUCTURE
    // =====================================================================

    private const string HEADER_NAME =
        "Header";

    private const string CATEGORY_BAR_NAME =
        "CategoryBar";

    private const string CONTENT_NAME =
        "Content";

    private const string SCROLL_VIEW_NAME =
        "ScrollView";

    private const string VIEWPORT_NAME =
        "Viewport";

    private const string ITEM_GRID_NAME =
        "ItemGrid";

    private const string SELECTED_ITEM_PANEL_NAME =
        "SelectedItemPanel";

    private const string EMPTY_STATE_NAME =
        "EmptyState";

    // =====================================================================
    // HEADER
    // =====================================================================

    private const string HEADER_TITLE_NAME =
        "Title";

    private const string CATEGORY_TITLE_NAME =
        "CategoryTitle";

    private const string CLOSE_BUTTON_NAME =
        "CloseButton";

    // =====================================================================
    // CATEGORY
    // =====================================================================

    private const string OUTFIT_BUTTON_NAME =
        "OutfitButton";

    private const string ACCESSORY_BUTTON_NAME =
        "AccessoryButton";

    private const string COSMETIC_BUTTON_NAME =
        "CosmeticButton";

    // =====================================================================
    // SELECTED ITEM
    // =====================================================================

    private const string SELECTED_ITEM_NAME =
        "SelectedItemName";

    private const string SELECTED_ITEM_DESCRIPTION =
        "SelectedItemDescription";

    private const string SELECTED_ITEM_STATE =
        "SelectedItemState";

    private const string EQUIP_BUTTON_NAME =
        "EquipButton";

    private const string EQUIP_BUTTON_TEXT_NAME =
        "EquipButtonText";

    // =====================================================================
    // EMPTY STATE
    // =====================================================================

    private const string EMPTY_STATE_TEXT_NAME =
        "Text";

    // =====================================================================
    // CARD
    // =====================================================================

    private const string CARD_NAME_PREFIX =
        "InventoryItemCard_";

    private const string CARD_BACKGROUND_NAME =
        "Background";

    private const string CARD_PREVIEW_NAME =
        "Preview";

    private const string CARD_ITEM_NAME =
        "ItemName";

    private const string CARD_ITEM_NUMBER_NAME =
        "ItemNumber";

    private const string CARD_STATE_NAME =
        "State";

    // =====================================================================
    // ITEM CONTRACT
    // =====================================================================

    private const int ITEM_CARD_COUNT = 20;

    private const int GRID_COLUMNS = 4;

    // =====================================================================
    // CARD SIZE
    // =====================================================================

    private const float CARD_WIDTH =
        185f;

    private const float CARD_HEIGHT =
        235f;

    private const float CARD_SPACING_X =
        12f;

    private const float CARD_SPACING_Y =
        12f;

    // =====================================================================
    // WINDOW
    // =====================================================================

    private const float HEADER_HEIGHT =
        72f;

    private const float CATEGORY_HEIGHT =
        58f;

    private const float SELECTED_PANEL_HEIGHT =
        154f;

    // =====================================================================
    // COLORS
    // =====================================================================

    private static readonly Color OverlayColor =
        new Color(
            0f,
            0f,
            0f,
            0.72f);

    private static readonly Color WindowColor =
        new Color(
            0.025f,
            0.035f,
            0.045f,
            0.98f);

    private static readonly Color HeaderColor =
        new Color(
            0.035f,
            0.045f,
            0.06f,
            1f);

    private static readonly Color CategoryBarColor =
        new Color(
            0.025f,
            0.035f,
            0.045f,
            1f);

    private static readonly Color CardColor =
        new Color(
            0.055f,
            0.065f,
            0.078f,
            1f);

    private static readonly Color PreviewColor =
        new Color(
            0.075f,
            0.085f,
            0.10f,
            1f);

    private static readonly Color SelectedColor =
        new Color(
            0.12f,
            0.16f,
            0.20f,
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

    // =====================================================================
    // ENTRY
    // =====================================================================

    [MenuItem(MenuPath)]
    private static void SetupInventoryUI()
    {
        // -------------------------------------------------------------
        // INVENTORY COMPONENT
        // -------------------------------------------------------------

        InventoryUI inventory =
            FindInventoryUI();

        if (inventory == null)
        {
            Debug.LogError(
                "[MainMenuInventoryUISetup] " +
                "Không tìm thấy InventoryUI trong Scene.");
            return;
        }

        // -------------------------------------------------------------
        // PANEL
        // -------------------------------------------------------------

        GameObject inventoryPanel =
            inventory.gameObject;

        inventoryPanel.name =
            INVENTORY_PANEL_NAME;

        ConfigurePanel(
            inventoryPanel);

        // -------------------------------------------------------------
        // WINDOW
        // -------------------------------------------------------------

        GameObject inventoryWindow =
            GetOrCreateChild(
                inventoryPanel.transform,
                INVENTORY_WINDOW_NAME);

        ConfigureWindow(
            inventoryWindow);

        // -------------------------------------------------------------
        // STRUCTURE
        // -------------------------------------------------------------

        GameObject header =
            GetOrCreateChild(
                inventoryWindow.transform,
                HEADER_NAME);

        GameObject categoryBar =
            GetOrCreateChild(
                inventoryWindow.transform,
                CATEGORY_BAR_NAME);

        GameObject content =
            GetOrCreateChild(
                inventoryWindow.transform,
                CONTENT_NAME);

        GameObject selectedPanel =
            GetOrCreateChild(
                inventoryWindow.transform,
                SELECTED_ITEM_PANEL_NAME);

        GameObject emptyState =
            GetOrCreateChild(
                content.transform,
                EMPTY_STATE_NAME);

        // -------------------------------------------------------------
        // HEADER
        // -------------------------------------------------------------

        SetupHeader(
            header);

        // -------------------------------------------------------------
        // CATEGORY
        // -------------------------------------------------------------

        SetupCategoryBar(
            categoryBar);

        // -------------------------------------------------------------
        // CONTENT
        // -------------------------------------------------------------

        SetupContent(
            content);

        // -------------------------------------------------------------
        // GRID
        // -------------------------------------------------------------

        GameObject scrollView =
            GetOrCreateChild(
                content.transform,
                SCROLL_VIEW_NAME);

        SetupScrollView(
            scrollView);

        GameObject viewport =
            GetOrCreateChild(
                scrollView.transform,
                VIEWPORT_NAME);

        SetupViewport(
            viewport);

        GameObject itemGrid =
            GetOrCreateChild(
                viewport.transform,
                ITEM_GRID_NAME);

        SetupItemGrid(
            itemGrid);

        // -------------------------------------------------------------
        // CARDS
        // -------------------------------------------------------------

        EnsureCardCount(
            itemGrid);

        InventoryItemCard[] cards =
            itemGrid.GetComponentsInChildren<
                InventoryItemCard>(
                    true);

        for (
            int i = 0;
            i < cards.Length;
            i++)
        {
            if (cards[i] == null)
                continue;

            SetupItemCard(
                cards[i].gameObject,
                i);
        }

        // -------------------------------------------------------------
        // EMPTY STATE
        // -------------------------------------------------------------

        SetupEmptyState(
            emptyState);

        // -------------------------------------------------------------
        // SELECTED ITEM
        // -------------------------------------------------------------

        SetupSelectedItemPanel(
            selectedPanel);

        // -------------------------------------------------------------
        // REFERENCES
        // -------------------------------------------------------------

        AssignInventoryReferences(
            inventory,
            inventoryPanel,
            inventoryWindow,
            categoryBar,
            scrollView,
            itemGrid,
            selectedPanel,
            emptyState);

        // -------------------------------------------------------------
        // INITIAL STATE
        // -------------------------------------------------------------

        inventoryPanel.SetActive(
            false);

        inventoryWindow.SetActive(
            true);

        emptyState.SetActive(
            false);

        selectedPanel.SetActive(
            true);

        // -------------------------------------------------------------
        // SAVE
        // -------------------------------------------------------------

        EditorUtility.SetDirty(
            inventory);

        EditorSceneManager.MarkSceneDirty(
            inventory.gameObject.scene);

        AssetDatabase.SaveAssets();

        EditorSceneManager.SaveScene(
            inventory.gameObject.scene);

        Debug.Log(
            "[MainMenuInventoryUISetup] " +
            "==================================================");

        Debug.Log(
            "[MainMenuInventoryUISetup] " +
            "INVENTORY UI SETUP HOÀN TẤT");

        Debug.Log(
            "[MainMenuInventoryUISetup] " +
            "Cards = " +
            cards.Length);

        Debug.Log(
            "[MainMenuInventoryUISetup] " +
            "Card Contract:");

        Debug.Log(
            "[MainMenuInventoryUISetup] " +
            "Background / Preview / ItemName / " +
            "ItemNumber / State / Button");

        Debug.Log(
            "[MainMenuInventoryUISetup] " +
            "3D Preview = CHƯA TRIỂN KHAI");

        Debug.Log(
            "[MainMenuInventoryUISetup] " +
            "==================================================");
    }

    // =====================================================================
    // FIND INVENTORY
    // =====================================================================

    private static InventoryUI FindInventoryUI()
    {
        InventoryUI inventory =
            Object.FindFirstObjectByType<
                InventoryUI>(
                FindObjectsInactive.Include);

        return inventory;
    }

    // =====================================================================
    // PANEL
    // =====================================================================

    private static void ConfigurePanel(
        GameObject panel)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                panel);

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        Image image =
            GetOrAdd<Image>(
                panel);

        image.color =
            OverlayColor;

        panel.transform.SetAsLastSibling();
    }

    // =====================================================================
    // WINDOW
    // =====================================================================

    private static void ConfigureWindow(
        GameObject window)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                window);

        rect.anchorMin =
            new Vector2(
                0.055f,
                0.045f);

        rect.anchorMax =
            new Vector2(
                0.945f,
                0.955f);

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        Image image =
            GetOrAdd<Image>(
                window);

        image.color =
            WindowColor;

        AddOrConfigureOutline(
            window,
            new Color(
                1f,
                1f,
                1f,
                0.10f),
            2f);
    }

    // =====================================================================
    // HEADER
    // =====================================================================

    private static void SetupHeader(
        GameObject header)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                header);

        rect.anchorMin =
            new Vector2(
                0f,
                0.875f);

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            new Vector2(
                14f,
                0f);

        rect.offsetMax =
            new Vector2(
                -14f,
                -8f);

        Image image =
            GetOrAdd<Image>(
                header);

        image.color =
            HeaderColor;

        // -------------------------------------------------------------
        // TITLE
        // -------------------------------------------------------------

        GameObject titleObject =
            GetOrCreateChild(
                header.transform,
                HEADER_TITLE_NAME);

        RectTransform titleRect =
            GetOrAdd<RectTransform>(
                titleObject);

        titleRect.anchorMin =
            new Vector2(
                0f,
                0f);

        titleRect.anchorMax =
            new Vector2(
                0.35f,
                1f);

        titleRect.offsetMin =
            new Vector2(
                18f,
                0f);

        titleRect.offsetMax =
            Vector2.zero;

        TMP_Text title =
            GetOrAdd<TextMeshProUGUI>(
                titleObject);

        title.text =
            "INVENTORY";

        title.fontSize =
            27f;

        title.fontStyle =
            FontStyles.Bold;

        title.color =
            WhiteColor;

        title.alignment =
            TextAlignmentOptions.MidlineLeft;

        title.raycastTarget =
            false;

        // -------------------------------------------------------------
        // CATEGORY TITLE
        // -------------------------------------------------------------

        GameObject categoryTitleObject =
            GetOrCreateChild(
                header.transform,
                CATEGORY_TITLE_NAME);

        RectTransform categoryRect =
            GetOrAdd<RectTransform>(
                categoryTitleObject);

        categoryRect.anchorMin =
            new Vector2(
                0.35f,
                0f);

        categoryRect.anchorMax =
            new Vector2(
                0.90f,
                1f);

        categoryRect.offsetMin =
            Vector2.zero;

        categoryRect.offsetMax =
            Vector2.zero;

        TMP_Text categoryTitle =
            GetOrAdd<TextMeshProUGUI>(
                categoryTitleObject);

        categoryTitle.text =
            "OUTFIT";

        categoryTitle.fontSize =
            18f;

        categoryTitle.fontStyle =
            FontStyles.Bold;

        categoryTitle.color =
            MutedColor;

        categoryTitle.alignment =
            TextAlignmentOptions.Center;

        categoryTitle.raycastTarget =
            false;

        // -------------------------------------------------------------
        // CLOSE
        // -------------------------------------------------------------

        GameObject close =
            GetOrCreateChild(
                header.transform,
                CLOSE_BUTTON_NAME);

        ConfigureCloseButton(
            close);
    }

    // =====================================================================
    // CLOSE BUTTON
    // =====================================================================

    private static void ConfigureCloseButton(
        GameObject buttonObject)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                buttonObject);

        rect.anchorMin =
            new Vector2(
                0.94f,
                0.16f);

        rect.anchorMax =
            new Vector2(
                1f,
                0.84f);

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        Image image =
            GetOrAdd<Image>(
                buttonObject);

        image.color =
            new Color(
                0.16f,
                0.17f,
                0.19f,
                1f);

        Button button =
            GetOrAdd<Button>(
                buttonObject);

        ConfigureButtonColors(
            button);

        GameObject textObject =
            GetOrCreateChild(
                buttonObject.transform,
                "Text");

        RectTransform textRect =
            GetOrAdd<RectTransform>(
                textObject);

        textRect.anchorMin =
            Vector2.zero;

        textRect.anchorMax =
            Vector2.one;

        textRect.offsetMin =
            Vector2.zero;

        textRect.offsetMax =
            Vector2.zero;

        TMP_Text text =
            GetOrAdd<TextMeshProUGUI>(
                textObject);

        text.text =
            "×";

        text.fontSize =
            28f;

        text.fontStyle =
            FontStyles.Bold;

        text.color =
            WhiteColor;

        text.alignment =
            TextAlignmentOptions.Center;

        text.raycastTarget =
            false;
    }

    // =====================================================================
    // CATEGORY BAR
    // =====================================================================

    private static void SetupCategoryBar(
        GameObject categoryBar)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                categoryBar);

        rect.anchorMin =
            new Vector2(
                0.02f,
                0.775f);

        rect.anchorMax =
            new Vector2(
                0.98f,
                0.855f);

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        Image image =
            GetOrAdd<Image>(
                categoryBar);

        image.color =
            CategoryBarColor;

        HorizontalLayoutGroup layout =
            GetOrAdd<HorizontalLayoutGroup>(
                categoryBar);

        layout.spacing =
            8f;

        layout.padding =
            new RectOffset(
                8,
                8,
                6,
                6);

        layout.childAlignment =
            TextAnchor.MiddleCenter;

        layout.childControlWidth =
            true;

        layout.childControlHeight =
            true;

        layout.childForceExpandWidth =
            true;

        layout.childForceExpandHeight =
            true;

        SetupCategoryButton(
            categoryBar.transform,
            OUTFIT_BUTTON_NAME,
            "OUTFIT");

        SetupCategoryButton(
            categoryBar.transform,
            ACCESSORY_BUTTON_NAME,
            "ACCESSORY");

        SetupCategoryButton(
            categoryBar.transform,
            COSMETIC_BUTTON_NAME,
            "COSMETIC");
    }

    // =====================================================================
    // CATEGORY BUTTON
    // =====================================================================

    private static void SetupCategoryButton(
        Transform parent,
        string objectName,
        string label)
    {
        GameObject buttonObject =
            GetOrCreateChild(
                parent,
                objectName);

        LayoutElement layout =
            GetOrAdd<LayoutElement>(
                buttonObject);

        layout.minHeight =
            40f;

        layout.preferredHeight =
            40f;

        Image image =
            GetOrAdd<Image>(
                buttonObject);

        image.color =
            CardColor;

        Button button =
            GetOrAdd<Button>(
                buttonObject);

        ConfigureButtonColors(
            button);

        GameObject textObject =
            GetOrCreateChild(
                buttonObject.transform,
                "Text");

        RectTransform textRect =
            GetOrAdd<RectTransform>(
                textObject);

        textRect.anchorMin =
            Vector2.zero;

        textRect.anchorMax =
            Vector2.one;

        textRect.offsetMin =
            Vector2.zero;

        textRect.offsetMax =
            Vector2.zero;

        TMP_Text text =
            GetOrAdd<TextMeshProUGUI>(
                textObject);

        text.text =
            label;

        text.fontSize =
            12f;

        text.fontStyle =
            FontStyles.Bold;

        text.color =
            WhiteColor;

        text.alignment =
            TextAlignmentOptions.Center;

        text.raycastTarget =
            false;
    }

    // =====================================================================
    // CONTENT
    // =====================================================================

    private static void SetupContent(
        GameObject content)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                content);

        rect.anchorMin =
            new Vector2(
                0f,
                0.16f);

        rect.anchorMax =
            new Vector2(
                1f,
                0.77f);

        rect.offsetMin =
            new Vector2(
                12f,
                0f);

        rect.offsetMax =
            new Vector2(
                -12f,
                0f);
    }

    // =====================================================================
    // SCROLL VIEW
    // =====================================================================

    private static void SetupScrollView(
        GameObject scrollView)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                scrollView);

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        Image image =
            GetOrAdd<Image>(
                scrollView);

        image.color =
            new Color(
                0f,
                0f,
                0f,
                0.001f);

        ScrollRect scroll =
            GetOrAdd<ScrollRect>(
                scrollView);

        scroll.horizontal =
            false;

        scroll.vertical =
            true;

        scroll.movementType =
            ScrollRect.MovementType.Clamped;

        scroll.inertia =
            true;

        scroll.scrollSensitivity =
            35f;

        scroll.viewport =
            null;

        scroll.content =
            null;
    }

    // =====================================================================
    // VIEWPORT
    // =====================================================================

    private static void SetupViewport(
        GameObject viewport)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                viewport);

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        Image image =
            GetOrAdd<Image>(
                viewport);

        image.color =
            new Color(
                0f,
                0f,
                0f,
                0.001f);

        Mask mask =
            GetOrAdd<Mask>(
                viewport);

        mask.showMaskGraphic =
            false;
    }

    // =====================================================================
    // ITEM GRID
    // =====================================================================

    private static void SetupItemGrid(
    GameObject grid)
{
    if (grid == null)
    {
        Debug.LogError(
            "[MainMenuInventoryUISetup] SetupItemGrid failed: grid is null.");
        return;
    }

    // =========================================================
    // RECT TRANSFORM
    // =========================================================

    RectTransform rect =
        GetOrAdd<RectTransform>(
            grid);

    rect.anchorMin =
        new Vector2(
            0f,
            1f);

    rect.anchorMax =
        new Vector2(
            1f,
            1f);

    rect.pivot =
        new Vector2(
            0.5f,
            1f);

    rect.offsetMin =
        Vector2.zero;

    rect.offsetMax =
        Vector2.zero;

    // =========================================================
    // GRID LAYOUT
    // =========================================================

    GridLayoutGroup layout =
        GetOrAdd<GridLayoutGroup>(
            grid);

    layout.cellSize =
        new Vector2(
            CARD_WIDTH,
            CARD_HEIGHT);

    layout.spacing =
        new Vector2(
            CARD_SPACING_X,
            CARD_SPACING_Y);

    layout.padding =
        new RectOffset(
            10,
            10,
            10,
            10);

    layout.constraint =
        GridLayoutGroup.Constraint.FixedColumnCount;

    layout.constraintCount =
        GRID_COLUMNS;

    layout.childAlignment =
        TextAnchor.UpperCenter;

    // =========================================================
    // CONTENT SIZE FITTER
    // =========================================================

    ContentSizeFitter fitter =
        GetOrAdd<ContentSizeFitter>(
            grid);

    fitter.horizontalFit =
        ContentSizeFitter.FitMode.Unconstrained;

    fitter.verticalFit =
        ContentSizeFitter.FitMode.PreferredSize;

    // =========================================================
    // SCROLL RECT
    // =========================================================

    ScrollRect scroll =
        grid.GetComponentInParent<ScrollRect>();

    if (scroll != null)
    {
        RectTransform viewportRect = null;

        Transform parent =
            grid.transform.parent;

        if (parent != null)
        {
            viewportRect =
                parent.GetComponent<RectTransform>();
        }

        if (viewportRect != null)
        {
            scroll.viewport =
                viewportRect;
        }
        else
        {
            Debug.LogWarning(
                "[MainMenuInventoryUISetup] " +
                "Viewport RectTransform not found for ItemGrid.",
                grid);
        }

        scroll.content =
            rect;
    }
    else
    {
        Debug.LogWarning(
            "[MainMenuInventoryUISetup] " +
            "ScrollRect not found above ItemGrid.",
            grid);
    }
}
    // =====================================================================
    // CARD COUNT
    // =====================================================================

    private static void EnsureCardCount(
        GameObject grid)
    {
        // -------------------------------------------------------------
        // CREATE / REPAIR 20 CARDS
        // -------------------------------------------------------------

        for (
            int i = 0;
            i < ITEM_CARD_COUNT;
            i++)
        {
            string name =
                CARD_NAME_PREFIX +
                i.ToString("00");

            GameObject card =
                FindDirectChild(
                    grid.transform,
                    name);

            if (card == null)
            {
                card =
                    CreateUIObject(
                        name,
                        grid.transform);
            }

            SetupItemCard(
                card,
                i);
        }

        // -------------------------------------------------------------
        // REMOVE OLD EXTRA CARDS
        // -------------------------------------------------------------

        for (
            int i =
                grid.transform.childCount - 1;
            i >= 0;
            i--)
        {
            Transform child =
                grid.transform.GetChild(i);

            if (child == null)
                continue;

            if (!child.name.StartsWith(
                    CARD_NAME_PREFIX))
                continue;

            string suffix =
                child.name.Substring(
                    CARD_NAME_PREFIX.Length);

            if (!int.TryParse(
                    suffix,
                    out int index))
                continue;

            if (index >= ITEM_CARD_COUNT)
            {
                Undo.DestroyObjectImmediate(
                    child.gameObject);
            }
        }
    }

    // =====================================================================
    // ITEM CARD
    // =====================================================================

    private static void SetupItemCard(
        GameObject card,
        int index)
    {
        if (card == null)
            return;

        card.name =
            CARD_NAME_PREFIX +
            index.ToString("00");

        RectTransform rect =
            GetOrAdd<RectTransform>(
                card);

        rect.localScale =
            Vector3.one;

        Image background =
            GetOrAdd<Image>(
                card);

        background.color =
            CardColor;

        background.raycastTarget =
            true;

        AddOrConfigureOutline(
            card,
            new Color(
                1f,
                1f,
                1f,
                0.06f),
            1f);

        Button button =
            GetOrAdd<Button>(
                card);

        ConfigureButtonColors(
            button);

        LayoutElement layout =
            GetOrAdd<LayoutElement>(
                card);

        layout.minWidth =
            CARD_WIDTH;

        layout.preferredWidth =
            CARD_WIDTH;

        layout.minHeight =
            CARD_HEIGHT;

        layout.preferredHeight =
            CARD_HEIGHT;

        InventoryItemCard itemCard =
            GetOrAdd<InventoryItemCard>(
                card);

        // -------------------------------------------------------------
        // REQUIRED CHILDREN
        //
        // DO NOT ADD PreviewFrame AS A REQUIRED
        // REFERENCE FOR InventoryItemCard.
        // -------------------------------------------------------------

        GameObject backgroundObject =
            GetOrCreateChild(
                card.transform,
                CARD_BACKGROUND_NAME);

        RectTransform backgroundRect =
            GetOrAdd<RectTransform>(
                backgroundObject);

        backgroundRect.anchorMin =
            Vector2.zero;

        backgroundRect.anchorMax =
            Vector2.one;

        backgroundRect.offsetMin =
            Vector2.zero;

        backgroundRect.offsetMax =
            Vector2.zero;

        Image childBackground =
            GetOrAdd<Image>(
                backgroundObject);

        childBackground.color =
            CardColor;

        childBackground.raycastTarget =
            false;

        // -------------------------------------------------------------
        // PREVIEW
        // -------------------------------------------------------------

        GameObject previewObject =
            GetOrCreateChild(
                card.transform,
                CARD_PREVIEW_NAME);

        RectTransform previewRect =
            GetOrAdd<RectTransform>(
                previewObject);

        previewRect.anchorMin =
            new Vector2(
                0.08f,
                0.30f);

        previewRect.anchorMax =
            new Vector2(
                0.92f,
                0.88f);

        previewRect.offsetMin =
            Vector2.zero;

        previewRect.offsetMax =
            Vector2.zero;

        Image preview =
            GetOrAdd<Image>(
                previewObject);

        preview.color =
            PreviewColor;

        preview.preserveAspect =
            true;

        preview.raycastTarget =
            false;

        // -------------------------------------------------------------
        // ITEM NUMBER
        // -------------------------------------------------------------

        GameObject numberObject =
            GetOrCreateChild(
                card.transform,
                CARD_ITEM_NUMBER_NAME);

        RectTransform numberRect =
            GetOrAdd<RectTransform>(
                numberObject);

        numberRect.anchorMin =
            new Vector2(
                0.06f,
                0.91f);

        numberRect.anchorMax =
            new Vector2(
                0.94f,
                0.99f);

        numberRect.offsetMin =
            Vector2.zero;

        numberRect.offsetMax =
            Vector2.zero;

        TMP_Text number =
            GetOrAdd<TextMeshProUGUI>(
                numberObject);

        number.text =
            "#000";

        number.fontSize =
            9f;

        number.fontStyle =
            FontStyles.Bold;

        number.color =
            MutedColor;

        number.alignment =
            TextAlignmentOptions.MidlineLeft;

        number.raycastTarget =
            false;

        // -------------------------------------------------------------
        // ITEM NAME
        // -------------------------------------------------------------

        GameObject nameObject =
            GetOrCreateChild(
                card.transform,
                CARD_ITEM_NAME);

        RectTransform nameRect =
            GetOrAdd<RectTransform>(
                nameObject);

        nameRect.anchorMin =
            new Vector2(
                0.06f,
                0.16f);

        nameRect.anchorMax =
            new Vector2(
                0.94f,
                0.28f);

        nameRect.offsetMin =
            Vector2.zero;

        nameRect.offsetMax =
            Vector2.zero;

        TMP_Text nameText =
            GetOrAdd<TextMeshProUGUI>(
                nameObject);

        nameText.text =
            "ITEM";

        nameText.fontSize =
            13f;

        nameText.fontStyle =
            FontStyles.Bold;

        nameText.color =
            WhiteColor;

        nameText.alignment =
            TextAlignmentOptions.Center;

        nameText.overflowMode =
            TextOverflowModes.Ellipsis;

        nameText.raycastTarget =
            false;

        // -------------------------------------------------------------
        // STATE
        // -------------------------------------------------------------

        GameObject stateObject =
            GetOrCreateChild(
                card.transform,
                CARD_STATE_NAME);

        RectTransform stateRect =
            GetOrAdd<RectTransform>(
                stateObject);

        stateRect.anchorMin =
            new Vector2(
                0.06f,
                0.055f);

        stateRect.anchorMax =
            new Vector2(
                0.94f,
                0.14f);

        stateRect.offsetMin =
            Vector2.zero;

        stateRect.offsetMax =
            Vector2.zero;

        TMP_Text state =
            GetOrAdd<TextMeshProUGUI>(
                stateObject);

        state.text =
            "ĐÃ SỞ HỮU";

        state.fontSize =
            8f;

        state.fontStyle =
            FontStyles.Bold;

        state.color =
            OwnedColor;

        state.alignment =
            TextAlignmentOptions.Center;

        state.raycastTarget =
            false;

        // -------------------------------------------------------------
        // BUTTON
        // -------------------------------------------------------------

        GameObject buttonObject =
            GetOrCreateChild(
                card.transform,
                "Button");

        RectTransform buttonRect =
            GetOrAdd<RectTransform>(
                buttonObject);

        buttonRect.anchorMin =
            Vector2.zero;

        buttonRect.anchorMax =
            Vector2.one;

        buttonRect.offsetMin =
            Vector2.zero;

        buttonRect.offsetMax =
            Vector2.zero;

        Image buttonImage =
            GetOrAdd<Image>(
                buttonObject);

        buttonImage.color =
            new Color(
                1f,
                1f,
                1f,
                0.001f);

        buttonImage.raycastTarget =
            true;

        Button childButton =
            GetOrAdd<Button>(
                buttonObject);

        ConfigureButtonColors(
            childButton);

        // -------------------------------------------------------------
        // KEEP CARD BUTTON ABOVE VISUAL CHILDREN
        // -------------------------------------------------------------

        buttonObject.transform.SetAsLastSibling();

        // -------------------------------------------------------------
        // REMOVE UNWANTED COMPONENTS FROM CHILD BUTTON
        // -------------------------------------------------------------

        // Do not remove Image because Button needs a Graphic.
        // Keep the setup deterministic.

        _ = itemCard;
    }

    // =====================================================================
    // EMPTY STATE
    // =====================================================================

    private static void SetupEmptyState(
        GameObject emptyState)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                emptyState);

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        Image image =
            GetOrAdd<Image>(
                emptyState);

        image.color =
            new Color(
                0f,
                0f,
                0f,
                0.001f);

        GameObject textObject =
            GetOrCreateChild(
                emptyState.transform,
                EMPTY_STATE_TEXT_NAME);

        RectTransform textRect =
            GetOrAdd<RectTransform>(
                textObject);

        textRect.anchorMin =
            new Vector2(
                0.2f,
                0.35f);

        textRect.anchorMax =
            new Vector2(
                0.8f,
                0.65f);

        textRect.offsetMin =
            Vector2.zero;

        textRect.offsetMax =
            Vector2.zero;

        TMP_Text text =
            GetOrAdd<TextMeshProUGUI>(
                textObject);

        text.text =
            "CHƯA CÓ ITEM";

        text.fontSize =
            16f;

        text.fontStyle =
            FontStyles.Bold;

        text.color =
            MutedColor;

        text.alignment =
            TextAlignmentOptions.Center;

        text.raycastTarget =
            false;
    }

    // =====================================================================
    // SELECTED ITEM PANEL
    // =====================================================================

    private static void SetupSelectedItemPanel(
        GameObject panel)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                panel);

        rect.anchorMin =
            new Vector2(
                0f,
                0.015f);

        rect.anchorMax =
            new Vector2(
                1f,
                0.15f);

        rect.offsetMin =
            new Vector2(
                12f,
                0f);

        rect.offsetMax =
            new Vector2(
                -12f,
                0f);

        Image image =
            GetOrAdd<Image>(
                panel);

        image.color =
            new Color(
                0.035f,
                0.045f,
                0.055f,
                1f);

        AddOrConfigureOutline(
            panel,
            new Color(
                1f,
                1f,
                1f,
                0.06f),
            1f);

        // -------------------------------------------------------------
        // NAME
        // -------------------------------------------------------------

        GameObject nameObject =
            GetOrCreateChild(
                panel.transform,
                SELECTED_ITEM_NAME);

        RectTransform nameRect =
            GetOrAdd<RectTransform>(
                nameObject);

        nameRect.anchorMin =
            new Vector2(
                0.03f,
                0.55f);

        nameRect.anchorMax =
            new Vector2(
                0.55f,
                0.90f);

        nameRect.offsetMin =
            Vector2.zero;

        nameRect.offsetMax =
            Vector2.zero;

        TMP_Text name =
            GetOrAdd<TextMeshProUGUI>(
                nameObject);

        name.text =
            "CHƯA CHỌN ITEM";

        name.fontSize =
            18f;

        name.fontStyle =
            FontStyles.Bold;

        name.color =
            WhiteColor;

        name.alignment =
            TextAlignmentOptions.MidlineLeft;

        name.raycastTarget =
            false;

        // -------------------------------------------------------------
        // DESCRIPTION
        // -------------------------------------------------------------

        GameObject descriptionObject =
            GetOrCreateChild(
                panel.transform,
                SELECTED_ITEM_DESCRIPTION);

        RectTransform descriptionRect =
            GetOrAdd<RectTransform>(
                descriptionObject);

        descriptionRect.anchorMin =
            new Vector2(
                0.03f,
                0.15f);

        descriptionRect.anchorMax =
            new Vector2(
                0.55f,
                0.52f);

        descriptionRect.offsetMin =
            Vector2.zero;

        descriptionRect.offsetMax =
            Vector2.zero;

        TMP_Text description =
            GetOrAdd<TextMeshProUGUI>(
                descriptionObject);

        description.text =
            string.Empty;

        description.fontSize =
            11f;

        description.color =
            MutedColor;

        description.alignment =
            TextAlignmentOptions.TopLeft;

        description.overflowMode =
            TextOverflowModes.Ellipsis;

        description.raycastTarget =
            false;

        // -------------------------------------------------------------
        // STATE
        // -------------------------------------------------------------

        GameObject stateObject =
            GetOrCreateChild(
                panel.transform,
                SELECTED_ITEM_STATE);

        RectTransform stateRect =
            GetOrAdd<RectTransform>(
                stateObject);

        stateRect.anchorMin =
            new Vector2(
                0.58f,
                0.56f);

        stateRect.anchorMax =
            new Vector2(
                0.78f,
                0.88f);

        stateRect.offsetMin =
            Vector2.zero;

        stateRect.offsetMax =
            Vector2.zero;

        TMP_Text state =
            GetOrAdd<TextMeshProUGUI>(
                stateObject);

        state.text =
            string.Empty;

        state.fontSize =
            11f;

        state.fontStyle =
            FontStyles.Bold;

        state.color =
            EquippedColor;

        state.alignment =
            TextAlignmentOptions.Center;

        state.raycastTarget =
            false;

        // -------------------------------------------------------------
        // EQUIP BUTTON
        // -------------------------------------------------------------

        GameObject equipButton =
            GetOrCreateChild(
                panel.transform,
                EQUIP_BUTTON_NAME);

        RectTransform equipRect =
            GetOrAdd<RectTransform>(
                equipButton);

        equipRect.anchorMin =
            new Vector2(
                0.80f,
                0.25f);

        equipRect.anchorMax =
            new Vector2(
                0.97f,
                0.78f);

        equipRect.offsetMin =
            Vector2.zero;

        equipRect.offsetMax =
            Vector2.zero;

        Image equipImage =
            GetOrAdd<Image>(
                equipButton);

        equipImage.color =
            new Color(
                0.10f,
                0.30f,
                0.18f,
                1f);

        Button button =
            GetOrAdd<Button>(
                equipButton);

        ConfigureButtonColors(
            button);

        GameObject equipTextObject =
            GetOrCreateChild(
                equipButton.transform,
                EQUIP_BUTTON_TEXT_NAME);

        RectTransform equipTextRect =
            GetOrAdd<RectTransform>(
                equipTextObject);

        equipTextRect.anchorMin =
            Vector2.zero;

        equipTextRect.anchorMax =
            Vector2.one;

        equipTextRect.offsetMin =
            Vector2.zero;

        equipTextRect.offsetMax =
            Vector2.zero;

        TMP_Text equipText =
            GetOrAdd<TextMeshProUGUI>(
                equipTextObject);

        equipText.text =
            "TRANG BỊ";

        equipText.fontSize =
            12f;

        equipText.fontStyle =
            FontStyles.Bold;

        equipText.color =
            WhiteColor;

        equipText.alignment =
            TextAlignmentOptions.Center;

        equipText.raycastTarget =
            false;
    }

    // =====================================================================
    // REFERENCES
    // =====================================================================

    private static void AssignInventoryReferences(
        InventoryUI inventory,
        GameObject inventoryPanel,
        GameObject inventoryWindow,
        GameObject categoryBar,
        GameObject scrollView,
        GameObject itemGrid,
        GameObject selectedPanel,
        GameObject emptyState)
    {
        if (inventory == null)
            return;

        SerializedObject so =
            new SerializedObject(
                inventory);

        so.Update();

        // -------------------------------------------------------------
        // PANEL
        // -------------------------------------------------------------

        AssignObject(
            so,
            "panelRoot",
            inventoryPanel);

        // -------------------------------------------------------------
        // DATABASE
        // -------------------------------------------------------------

        GameItemDatabase database =
            FindGameItemDatabaseAsset();

        AssignObject(
            so,
            "itemDatabase",
            database);

        // -------------------------------------------------------------
        // EQUIPMENT MANAGER
        // -------------------------------------------------------------

        EquipmentManager equipmentManager =
            FindSceneComponent<
                EquipmentManager>();

        AssignObject(
            so,
            "equipmentManager",
            equipmentManager);

        // -------------------------------------------------------------
        // CATEGORY
        // -------------------------------------------------------------

        Transform categoryTransform =
            categoryBar != null
                ? categoryBar.transform
                : null;

        if (categoryTransform != null)
        {
            Button outfitButton =
                FindButton(
                    categoryBar,
                    OUTFIT_BUTTON_NAME);

            Button accessoryButton =
                FindButton(
                    categoryBar,
                    ACCESSORY_BUTTON_NAME);

            Button cosmeticButton =
                FindButton(
                    categoryBar,
                    COSMETIC_BUTTON_NAME);

            AssignObject(
                so,
                "outfitButton",
                outfitButton);

            AssignObject(
                so,
                "accessoryButton",
                accessoryButton);

            AssignObject(
                so,
                "cosmeticButton",
                cosmeticButton);

            AssignButtonText(
                so,
                "outfitButtonText",
                outfitButton);

            AssignButtonText(
                so,
                "accessoryButtonText",
                accessoryButton);

            AssignButtonText(
                so,
                "cosmeticButtonText",
                cosmeticButton);
        }

        // -------------------------------------------------------------
        // HEADER
        // -------------------------------------------------------------

        Transform header =
            FindDirectChildTransform(
                inventoryWindow.transform,
                HEADER_NAME);

        if (header != null)
        {
            TMP_Text categoryTitle =
                FindTextRecursive(
                    header,
                    CATEGORY_TITLE_NAME);

            Button closeButton =
                FindButton(
                    header.gameObject,
                    CLOSE_BUTTON_NAME);

            AssignObject(
                so,
                "categoryTitle",
                categoryTitle);

            AssignObject(
                so,
                "closeButton",
                closeButton);
        }

        // -------------------------------------------------------------
        // GRID
        // -------------------------------------------------------------

        AssignObject(
            so,
            "itemGrid",
            itemGrid);

        InventoryItemCard[] cards =
            itemGrid != null
                ? itemGrid.GetComponentsInChildren<
                    InventoryItemCard>(
                    true)
                : null;

        if (cards == null)
        {
            cards =
                new InventoryItemCard[0];
        }

        if (cards.Length >
            ITEM_CARD_COUNT)
        {
            InventoryItemCard[] trimmed =
                new InventoryItemCard[
                    ITEM_CARD_COUNT];

            for (
                int i = 0;
                i < ITEM_CARD_COUNT;
                i++)
            {
                trimmed[i] =
                    cards[i];
            }

            cards =
                trimmed;
        }

        AssignObjectArray(
            so,
            "itemCards",
            cards);

        // -------------------------------------------------------------
        // SCROLL RECT
        // -------------------------------------------------------------

        ScrollRect scrollRect =
            scrollView != null
                ? scrollView.GetComponent<
                    ScrollRect>()
                : null;

        AssignObject(
            so,
            "itemScrollRect",
            scrollRect);

        // -------------------------------------------------------------
        // EMPTY STATE
        // -------------------------------------------------------------

        AssignObject(
            so,
            "emptyState",
            emptyState);

        TMP_Text emptyStateText =
            emptyState != null
                ? FindTextRecursive(
                    emptyState.transform,
                    EMPTY_STATE_TEXT_NAME)
                : null;

        AssignObject(
            so,
            "emptyStateText",
            emptyStateText);

        // -------------------------------------------------------------
        // SELECTED ITEM
        // -------------------------------------------------------------

        if (selectedPanel != null)
        {
            TMP_Text selectedName =
                FindTextRecursive(
                    selectedPanel.transform,
                    SELECTED_ITEM_NAME);

            TMP_Text selectedDescription =
                FindTextRecursive(
                    selectedPanel.transform,
                    SELECTED_ITEM_DESCRIPTION);

            TMP_Text selectedState =
                FindTextRecursive(
                    selectedPanel.transform,
                    SELECTED_ITEM_STATE);

            Button equipButton =
                FindButton(
                    selectedPanel,
                    EQUIP_BUTTON_NAME);

            TMP_Text equipButtonText =
                FindTextRecursive(
                    selectedPanel.transform,
                    EQUIP_BUTTON_TEXT_NAME);

            AssignObject(
                so,
                "selectedItemName",
                selectedName);

            AssignObject(
                so,
                "selectedItemDescription",
                selectedDescription);

            AssignObject(
                so,
                "selectedItemState",
                selectedState);

            AssignObject(
                so,
                "equipButton",
                equipButton);

            AssignObject(
                so,
                "equipButtonText",
                equipButtonText);
        }

        // -------------------------------------------------------------
        // APPLY
        // -------------------------------------------------------------

        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log(
            "[MainMenuInventoryUISetup] " +
            "References assigned.");

        Debug.Log(
            "[MainMenuInventoryUISetup] " +
            "InventoryItemCard count = " +
            cards.Length);
    }

    // =====================================================================
    // DATABASE
    // =====================================================================

    private static GameItemDatabase
        FindGameItemDatabaseAsset()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:GameItemDatabase");

        if (guids == null ||
            guids.Length == 0)
        {
            Debug.LogWarning(
                "[MainMenuInventoryUISetup] " +
                "Không tìm thấy GameItemDatabase.");

            return null;
        }

        for (
            int i = 0;
            i < guids.Length;
            i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]);

            GameItemDatabase database =
                AssetDatabase.LoadAssetAtPath<
                    GameItemDatabase>(
                    path);

            if (database != null)
                return database;
        }

        return null;
    }

    // =====================================================================
    // FIND SCENE COMPONENT
    // =====================================================================

    private static T FindSceneComponent<T>()
        where T : Component
    {
        return Object.FindFirstObjectByType<T>(
            FindObjectsInactive.Include);
    }

    // =====================================================================
    // CREATE UI OBJECT
    // =====================================================================

    private static GameObject CreateUIObject(
        string objectName,
        Transform parent)
    {
        GameObject objectInstance =
            new GameObject(
                objectName,
                typeof(RectTransform));

        Undo.RegisterCreatedObjectUndo(
            objectInstance,
            "Create Inventory UI Object");

        RectTransform rect =
            objectInstance.GetComponent<
                RectTransform>();

        rect.SetParent(
            parent,
            false);

        rect.localScale =
            Vector3.one;

        rect.localRotation =
            Quaternion.identity;

        rect.anchoredPosition =
            Vector2.zero;

        return objectInstance;
    }

    // =====================================================================
    // GET OR CREATE CHILD
    // =====================================================================

    private static GameObject GetOrCreateChild(
        Transform parent,
        string childName)
    {
        if (parent == null)
            return null;

        Transform existing =
            FindDirectChildTransform(
                parent,
                childName);

        if (existing != null)
            return existing.gameObject;

        return CreateUIObject(
            childName,
            parent);
    }

    // =====================================================================
    // GET OR ADD COMPONENT
    // =====================================================================

    private static T GetOrAdd<T>(
        GameObject target)
        where T : Component
    {
        if (target == null)
            return null;

        T component =
            target.GetComponent<T>();

        if (component != null)
            return component;

        component =
            Undo.AddComponent<T>(
                target);

        return component;
    }

    // =====================================================================
    // FIND DIRECT CHILD
    // =====================================================================

    private static GameObject FindDirectChild(
        Transform parent,
        string childName)
    {
        Transform child =
            FindDirectChildTransform(
                parent,
                childName);

        return child != null
            ? child.gameObject
            : null;
    }

    private static Transform
        FindDirectChildTransform(
            Transform parent,
            string childName)
    {
        if (parent == null)
            return null;

        int childCount =
            parent.childCount;

        for (
            int i = 0;
            i < childCount;
            i++)
        {
            Transform child =
                parent.GetChild(i);

            if (child.name ==
                childName)
            {
                return child;
            }
        }

        return null;
    }

    // =====================================================================
    // FIND RECURSIVE
    // =====================================================================

    private static Transform FindChildRecursive(
        Transform root,
        string childName)
    {
        if (root == null)
            return null;

        if (root.name ==
            childName)
        {
            return root;
        }

        int childCount =
            root.childCount;

        for (
            int i = 0;
            i < childCount;
            i++)
        {
            Transform result =
                FindChildRecursive(
                    root.GetChild(i),
                    childName);

            if (result != null)
                return result;
        }

        return null;
    }

    // =====================================================================
    // FIND TEXT
    // =====================================================================

    private static TMP_Text FindTextRecursive(
        Transform root,
        string childName)
    {
        Transform target =
            FindChildRecursive(
                root,
                childName);

        if (target == null)
            return null;

        return target.GetComponent<
            TMP_Text>();
    }

    // =====================================================================
    // FIND BUTTON
    // =====================================================================

    private static Button FindButton(
        GameObject root,
        string childName)
    {
        if (root == null)
            return null;

        Transform target =
            FindChildRecursive(
                root.transform,
                childName);

        if (target == null)
            return null;

        return target.GetComponent<
            Button>();
    }

    // =====================================================================
    // BUTTON COLORS
    // =====================================================================

    private static void ConfigureButtonColors(
        Button button)
    {
        if (button == null)
            return;

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            new Color(
                1f,
                1f,
                1f,
                1f);

        colors.highlightedColor =
            new Color(
                1f,
                1f,
                1f,
                1f);

        colors.pressedColor =
            new Color(
                0.82f,
                0.82f,
                0.82f,
                1f);

        colors.selectedColor =
            new Color(
                1f,
                1f,
                1f,
                1f);

        colors.disabledColor =
            new Color(
                0.35f,
                0.35f,
                0.35f,
                0.65f);

        colors.colorMultiplier =
            1f;

        colors.fadeDuration =
            0.06f;

        button.colors =
            colors;

        button.transition =
            Selectable.Transition.ColorTint;
    }

    // =====================================================================
    // OUTLINE
    // =====================================================================

    private static void AddOrConfigureOutline(
        GameObject target,
        Color color,
        float distance)
    {
        if (target == null)
            return;

        Outline outline =
            GetOrAdd<Outline>(
                target);

        outline.effectColor =
            color;

        outline.effectDistance =
            new Vector2(
                distance,
                distance);

        outline.useGraphicAlpha =
            true;
    }

    // =====================================================================
    // SERIALIZED OBJECT
    // =====================================================================

    private static void AssignObject(
        SerializedObject serializedObject,
        string propertyName,
        Object value)
    {
        if (serializedObject == null)
            return;

        SerializedProperty property =
            serializedObject.FindProperty(
                propertyName);

        if (property == null)
        {
            Debug.LogWarning(
                "[MainMenuInventoryUISetup] " +
                "SerializedProperty không tồn tại: " +
                propertyName);

            return;
        }

        property.objectReferenceValue =
            value;
    }

    // =====================================================================
    // SERIALIZED ARRAY
    // =====================================================================

    private static void AssignObjectArray(
        SerializedObject serializedObject,
        string propertyName,
        Object[] values)
    {
        if (serializedObject == null)
            return;

        SerializedProperty property =
            serializedObject.FindProperty(
                propertyName);

        if (property == null)
        {
            Debug.LogWarning(
                "[MainMenuInventoryUISetup] " +
                "Array Property không tồn tại: " +
                propertyName);

            return;
        }

        if (!property.isArray)
        {
            Debug.LogWarning(
                "[MainMenuInventoryUISetup] " +
                propertyName +
                " không phải Array.");

            return;
        }

        int count =
            values != null
                ? values.Length
                : 0;

        property.arraySize =
            count;

        for (
            int i = 0;
            i < count;
            i++)
        {
            property
                .GetArrayElementAtIndex(i)
                .objectReferenceValue =
                    values[i];
        }
    }

    // =====================================================================
    // BUTTON TEXT
    // =====================================================================

    private static void AssignButtonText(
        SerializedObject serializedObject,
        string propertyName,
        Button button)
    {
        if (button == null)
        {
            AssignObject(
                serializedObject,
                propertyName,
                null);

            return;
        }

        TMP_Text text =
            FindTextRecursive(
                button.transform,
                "Text");

        AssignObject(
            serializedObject,
            propertyName,
            text);
    }
}

#endif