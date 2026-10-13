#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MainMenuShopUISetup
{
    // ============================================================
    // CONSTANTS
    // ============================================================

    private const int CATEGORY_COUNT = 4;

    private const int CARD_COUNT = 20;


    private const string SHOP_PANEL_NAME =
        "ShopPanel";

    private const string SHOP_WINDOW_NAME =
        "ShopWindow";

    private const string HEADER_NAME =
        "Header";

    private const string CATEGORY_BAR_NAME =
        "CategoryBar";

    private const string CONTENT_NAME =
        "Content";

    private const string ITEM_VIEWPORT_NAME =
        "ItemViewport";

    private const string ITEM_GRID_NAME =
        "ItemGrid";

    private const string SELECTED_PANEL_NAME =
        "SelectedPanel";

    private const string PAGINATION_NAME =
        "Pagination";

    private const string CURRENCY_BAR_NAME =
        "CurrencyBar";


    // ============================================================
    // COLORS
    // ============================================================

    private static readonly Color ShopBackgroundColor =
        new Color(
            0f,
            0f,
            0f,
            0.72f);

    private static readonly Color WindowColor =
        new Color(
            0.025f,
            0.03f,
            0.038f,
            0.98f);

    private static readonly Color HeaderColor =
        new Color(
            0.04f,
            0.05f,
            0.06f,
            1f);

    private static readonly Color CategoryColor =
        new Color(
            0.08f,
            0.09f,
            0.105f,
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

    private static readonly Color GoldColor =
        new Color(
            1f,
            0.75f,
            0.1f,
            1f);

    private static readonly Color GemColor =
        new Color(
            0.3f,
            0.9f,
            1f,
            1f);


    // ============================================================
    // MENU
    // ============================================================

    [MenuItem(
        "LEAD KHONG PHANH/Shop/Setup Main Menu Shop",
        false,
        2000)]
    private static void SetupShop()
    {
        MainMenuShopUI shop =
            Object.FindFirstObjectByType<MainMenuShopUI>();

        if (shop == null)
        {
            Debug.LogError(
                "[MainMenuShopUISetup] " +
                "Không tìm thấy MainMenuShopUI trong Scene.");

            return;
        }


        Undo.RegisterFullObjectHierarchyUndo(
            shop.gameObject,
            "Setup Main Menu Shop UI");


        // ========================================================
        // DATABASE
        // ========================================================

        GameItemDatabase database =
            FindDatabase();


        if (database == null)
        {
            Debug.LogError(
                "[MainMenuShopUISetup] " +
                "Không tìm thấy GameItemDatabase.");

            return;
        }


        // ========================================================
        // EVENT SYSTEM
        // ========================================================

        EnsureEventSystem();


        // ========================================================
        // SHOP PANEL
        // ========================================================

        GameObject shopPanel =
            GetOrCreateChild(
                shop.transform,
                SHOP_PANEL_NAME);


        ConfigureShopPanel(
            shopPanel);


        // ========================================================
        // SHOP WINDOW
        // ========================================================

        GameObject shopWindow =
            GetOrCreateChild(
                shopPanel.transform,
                SHOP_WINDOW_NAME);


        ConfigureShopWindow(
            shopWindow);


        // ========================================================
        // HEADER
        // ========================================================

        GameObject header =
            GetOrCreateChild(
                shopWindow.transform,
                HEADER_NAME);


        SetupHeader(
            header);


        // ========================================================
        // CATEGORY BAR
        // ========================================================

        GameObject categoryBar =
            GetOrCreateChild(
                shopWindow.transform,
                CATEGORY_BAR_NAME);


        SetupCategoryBar(
            categoryBar);


        // ========================================================
        // CONTENT
        // ========================================================

        GameObject content =
            GetOrCreateChild(
                shopWindow.transform,
                CONTENT_NAME);


        SetupContent(
            content);


        // ========================================================
        // ITEM VIEWPORT
        // ========================================================

        GameObject itemViewport =
            GetOrCreateChild(
                content.transform,
                ITEM_VIEWPORT_NAME);


        GameObject itemGrid =
            GetOrCreateChild(
                itemViewport.transform,
                ITEM_GRID_NAME);


        SetupItemViewport(
            itemViewport,
            itemGrid);


        // ========================================================
        // SELECTED PANEL
        // ========================================================

        GameObject selectedPanel =
            GetOrCreateChild(
                content.transform,
                SELECTED_PANEL_NAME);


        SetupSelectedPanel(
            selectedPanel);


        // ========================================================
        // PAGINATION
        // ========================================================

        GameObject pagination =
            GetOrCreateChild(
                content.transform,
                PAGINATION_NAME);


        SetupPagination(
            pagination);


        // ========================================================
        // CURRENCY BAR
        // ========================================================

        GameObject currencyBar =
            GetOrCreateChild(
                shopWindow.transform,
                CURRENCY_BAR_NAME);


        SetupCurrencyBar(
            currencyBar);


        // ========================================================
        // CARDS
        // ========================================================

        ShopItemCard[] cards =
            CreateItemCards(
                itemGrid.transform);


        // ========================================================
        // REFERENCES
        // ========================================================

        AssignShopReferences(
            shop,
            database,
            shopPanel,
            categoryBar,
            itemViewport,
            itemGrid,
            selectedPanel,
            pagination,
            currencyBar,
            cards);


        // ========================================================
        // CARD COMPONENTS
        // ========================================================

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
                continue;

            EditorUtility.SetDirty(
                cards[i]);
        }


        // ========================================================
        // FINALIZE
        // ========================================================

        EditorUtility.SetDirty(
            shop);


        EditorSceneManager.MarkSceneDirty(
            shop.gameObject.scene);


        AssetDatabase.SaveAssets();


        if (
            shop.gameObject.scene.IsValid() &&
            shop.gameObject.scene.isLoaded)
        {
            EditorSceneManager.SaveScene(
                shop.gameObject.scene);
        }


        Debug.Log(
            "[MainMenuShopUISetup] " +
            "SHOP UI SETUP HOÀN TẤT.\n\n" +
            "Database: ASSIGNED\n" +
            "Categories: 4\n" +
            "Reusable Cards: 20\n" +
            "Pagination: ENABLED\n" +
            "Category source: GameItemData.Category\n" +
            "ItemId source: GameItemData.ItemId\n" +
            "Ownership source: InventoryOwnership\n" +
            "Prefab dependency for listing: NONE");
    }


    // ============================================================
    // FIND DATABASE
    // ============================================================

    private static GameItemDatabase FindDatabase()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:GameItemDatabase");


        for (int i = 0; i < guids.Length; i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]);


            GameItemDatabase database =
                AssetDatabase.LoadAssetAtPath<GameItemDatabase>(
                    path);


            if (database != null)
                return database;
        }


        return null;
    }


    // ============================================================
    // EVENT SYSTEM
    // ============================================================

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem =
            Object.FindFirstObjectByType<EventSystem>();


        if (eventSystem != null)
            return;


        GameObject eventSystemObject =
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));


        Undo.RegisterCreatedObjectUndo(
            eventSystemObject,
            "Create EventSystem");
    }


    // ============================================================
    // SHOP PANEL
    // ============================================================

    private static void ConfigureShopPanel(
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
            ShopBackgroundColor;


        panel.transform.SetAsLastSibling();
    }


    // ============================================================
    // SHOP WINDOW
    // ============================================================

    private static void ConfigureShopWindow(
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
    }


    // ============================================================
    // HEADER
    // ============================================================

    private static void SetupHeader(
        GameObject header)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                header);


        rect.anchorMin =
            new Vector2(
                0f,
                0.87f);

        rect.anchorMax =
            new Vector2(
                1f,
                1f);

        rect.offsetMin =
            new Vector2(
                16f,
                0f);

        rect.offsetMax =
            new Vector2(
                -16f,
                -8f);


        Image image =
            GetOrAdd<Image>(
                header);


        image.color =
            HeaderColor;


        GameObject title =
            GetOrCreateChild(
                header.transform,
                "Title");


        SetupText(
            title,
            "SHOP",
            30,
            TextAlignmentOptions.Left,
            WhiteColor);


        RectTransform titleRect =
            title.GetComponent<RectTransform>();


        titleRect.anchorMin =
            new Vector2(
                0f,
                0f);

        titleRect.anchorMax =
            new Vector2(
                0.45f,
                1f);

        titleRect.offsetMin =
            new Vector2(
                20f,
                0f);

        titleRect.offsetMax =
            Vector2.zero;
    }


    // ============================================================
    // CATEGORY BAR
    // ============================================================

    private static void SetupCategoryBar(
        GameObject categoryBar)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                categoryBar);


        rect.anchorMin =
            new Vector2(
                0f,
                0.79f);

        rect.anchorMax =
            new Vector2(
                1f,
                0.87f);

        rect.offsetMin =
            new Vector2(
                16f,
                0f);

        rect.offsetMax =
            new Vector2(
                -16f,
                0f);


        HorizontalLayoutGroup layout =
            GetOrAdd<HorizontalLayoutGroup>(
                categoryBar);


        layout.spacing =
            10f;

        layout.childForceExpandWidth =
            true;

        layout.childForceExpandHeight =
            true;

        layout.childControlWidth =
            true;

        layout.childControlHeight =
            true;


        string[] names =
        {
            "XE",
            "TRANG PHỤC",
            "PHỤ KIỆN",
            "COSMETIC"
        };


        for (int i = 0; i < CATEGORY_COUNT; i++)
        {
            GameObject button =
                GetOrCreateChild(
                    categoryBar.transform,
                    "CategoryButton_" + i);


            SetupButton(
                button,
                CategoryColor);


            GameObject text =
                GetOrCreateChild(
                    button.transform,
                    "Text");


            SetupText(
                text,
                names[i],
                18,
                TextAlignmentOptions.Center,
                WhiteColor);


            RectTransform textRect =
                text.GetComponent<RectTransform>();


            textRect.anchorMin =
                Vector2.zero;

            textRect.anchorMax =
                Vector2.one;

            textRect.offsetMin =
                Vector2.zero;

            textRect.offsetMax =
                Vector2.zero;
        }
    }


    // ============================================================
    // CONTENT
    // ============================================================

    private static void SetupContent(
        GameObject content)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                content);


        rect.anchorMin =
            new Vector2(
                0f,
                0.12f);

        rect.anchorMax =
            new Vector2(
                1f,
                0.79f);

        rect.offsetMin =
            new Vector2(
                16f,
                0f);

        rect.offsetMax =
            new Vector2(
                -16f,
                0f);
    }


    // ============================================================
    // ITEM VIEWPORT
    // ============================================================

    private static void SetupItemViewport(
        GameObject viewport,
        GameObject grid)
    {
        RectTransform viewportRect =
            GetOrAdd<RectTransform>(
                viewport);


        viewportRect.anchorMin =
            new Vector2(
                0f,
                0f);

        viewportRect.anchorMax =
            new Vector2(
                0.67f,
                1f);

        viewportRect.offsetMin =
            Vector2.zero;

        viewportRect.offsetMax =
            Vector2.zero;


        Image viewportImage =
            GetOrAdd<Image>(
                viewport);


        viewportImage.color =
            new Color(
                0f,
                0f,
                0f,
                0.08f);


        Mask mask =
            GetOrAdd<Mask>(
                viewport);


        mask.showMaskGraphic =
            false;


        ScrollRect scrollRect =
            GetOrAdd<ScrollRect>(
                viewport);


        scrollRect.horizontal =
            false;

        scrollRect.vertical =
            true;

        scrollRect.movementType =
            ScrollRect.MovementType.Clamped;

        scrollRect.scrollSensitivity =
            35f;


        RectTransform gridRect =
            GetOrAdd<RectTransform>(
                grid);


        gridRect.anchorMin =
            new Vector2(
                0f,
                1f);

        gridRect.anchorMax =
            new Vector2(
                1f,
                1f);

        gridRect.pivot =
            new Vector2(
                0.5f,
                1f);

        gridRect.anchoredPosition =
            Vector2.zero;

        gridRect.sizeDelta =
            new Vector2(
                0f,
                0f);


        GridLayoutGroup gridLayout =
            GetOrAdd<GridLayoutGroup>(
                grid);


        gridLayout.cellSize =
            new Vector2(
                175f,
                150f);

        gridLayout.spacing =
            new Vector2(
                10f,
                10f);

        gridLayout.padding =
            new RectOffset(
                10,
                10,
                10,
                10);

        gridLayout.constraint =
            GridLayoutGroup.Constraint.FixedColumnCount;

        gridLayout.constraintCount =
            4;

        gridLayout.childAlignment =
            TextAnchor.UpperCenter;


        ContentSizeFitter fitter =
            GetOrAdd<ContentSizeFitter>(
                grid);


        fitter.horizontalFit =
            ContentSizeFitter.FitMode.Unconstrained;

        fitter.verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;


        scrollRect.viewport =
            viewportRect;

        scrollRect.content =
            gridRect;
    }


    // ============================================================
    // SELECTED PANEL
    // ============================================================

    private static void SetupSelectedPanel(
        GameObject panel)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                panel);


        rect.anchorMin =
            new Vector2(
                0.69f,
                0f);

        rect.anchorMax =
            new Vector2(
                1f,
                1f);

        rect.offsetMin =
            new Vector2(
                10f,
                0f);

        rect.offsetMax =
            Vector2.zero;


        Image image =
            GetOrAdd<Image>(
                panel);


        image.color =
            new Color(
                0.045f,
                0.055f,
                0.065f,
                1f);


        // --------------------------------------------------------
        // PREVIEW
        // --------------------------------------------------------

        GameObject preview =
            GetOrCreateChild(
                panel.transform,
                "SelectedPreview");


        SetupImage(
            preview,
            PreviewColor);


        RectTransform previewRect =
            preview.GetComponent<RectTransform>();


        previewRect.anchorMin =
            new Vector2(
                0.08f,
                0.52f);

        previewRect.anchorMax =
            new Vector2(
                0.92f,
                0.94f);

        previewRect.offsetMin =
            Vector2.zero;

        previewRect.offsetMax =
            Vector2.zero;


        // --------------------------------------------------------
        // NAME
        // --------------------------------------------------------

        GameObject name =
            GetOrCreateChild(
                panel.transform,
                "SelectedName");


        SetupText(
            name,
            string.Empty,
            24,
            TextAlignmentOptions.Center,
            WhiteColor);


        SetRect(
            name,
            new Vector2(
                0.05f,
                0.43f),
            new Vector2(
                0.95f,
                0.52f));


        // --------------------------------------------------------
        // DESCRIPTION
        // --------------------------------------------------------

        GameObject description =
            GetOrCreateChild(
                panel.transform,
                "SelectedDescription");


        SetupText(
            description,
            string.Empty,
            14,
            TextAlignmentOptions.TopLeft,
            MutedColor);


        TMP_Text descriptionText =
            description.GetComponent<TMP_Text>();


        descriptionText.textWrappingMode = TMPro.TextWrappingModes.Normal;


        SetRect(
            description,
            new Vector2(
                0.08f,
                0.25f),
            new Vector2(
                0.92f,
                0.42f));


        // --------------------------------------------------------
        // CATEGORY
        // --------------------------------------------------------

        GameObject category =
            GetOrCreateChild(
                panel.transform,
                "SelectedCategory");


        SetupText(
            category,
            string.Empty,
            14,
            TextAlignmentOptions.Center,
            MutedColor);


        SetRect(
            category,
            new Vector2(
                0.05f,
                0.18f),
            new Vector2(
                0.95f,
                0.25f));


        // --------------------------------------------------------
        // PRICE
        // --------------------------------------------------------

        GameObject price =
            GetOrCreateChild(
                panel.transform,
                "SelectedPrice");


        SetupText(
            price,
            string.Empty,
            20,
            TextAlignmentOptions.Center,
            GoldColor);


        SetRect(
            price,
            new Vector2(
                0.05f,
                0.10f),
            new Vector2(
                0.95f,
                0.18f));


        // --------------------------------------------------------
        // BUY BUTTON
        // --------------------------------------------------------

        GameObject buy =
            GetOrCreateChild(
                panel.transform,
                "BuyButton");


        SetupButton(
            buy,
            GoldColor);


        SetRect(
            buy,
            new Vector2(
                0.12f,
                0.02f),
            new Vector2(
                0.88f,
                0.09f));


        GameObject buyText =
            GetOrCreateChild(
                buy.transform,
                "Text");


        SetupText(
            buyText,
            "MUA NGAY",
            17,
            TextAlignmentOptions.Center,
            Color.black);


        SetFullRect(
            buyText);
    }


    // ============================================================
    // PAGINATION
    // ============================================================

    private static void SetupPagination(
        GameObject pagination)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                pagination);


        rect.anchorMin =
            new Vector2(
                0f,
                0f);

        rect.anchorMax =
            new Vector2(
                0.67f,
                0.10f);

        rect.offsetMin =
            new Vector2(
                16f,
                0f);

        rect.offsetMax =
            new Vector2(
                -16f,
                0f);


        // --------------------------------------------------------
        // PREVIOUS
        // --------------------------------------------------------

        GameObject previous =
            GetOrCreateChild(
                pagination.transform,
                "PreviousPageButton");


        SetupButton(
            previous,
            CategoryColor);


        SetRect(
            previous,
            new Vector2(
                0f,
                0f),
            new Vector2(
                0.22f,
                1f));


        GameObject previousText =
            GetOrCreateChild(
                previous.transform,
                "Text");


        SetupText(
            previousText,
            "‹",
            30,
            TextAlignmentOptions.Center,
            WhiteColor);


        SetFullRect(
            previousText);


        // --------------------------------------------------------
        // PAGE TEXT
        // --------------------------------------------------------

        GameObject page =
            GetOrCreateChild(
                pagination.transform,
                "PageText");


        SetupText(
            page,
            "1 / 1",
            18,
            TextAlignmentOptions.Center,
            WhiteColor);


        SetRect(
            page,
            new Vector2(
                0.22f,
                0f),
            new Vector2(
                0.78f,
                1f));


        // --------------------------------------------------------
        // NEXT
        // --------------------------------------------------------

        GameObject next =
            GetOrCreateChild(
                pagination.transform,
                "NextPageButton");


        SetupButton(
            next,
            CategoryColor);


        SetRect(
            next,
            new Vector2(
                0.78f,
                0f),
            new Vector2(
                1f,
                1f));


        GameObject nextText =
            GetOrCreateChild(
                next.transform,
                "Text");


        SetupText(
            nextText,
            "›",
            30,
            TextAlignmentOptions.Center,
            WhiteColor);


        SetFullRect(
            nextText);
    }


    // ============================================================
    // CURRENCY BAR
    // ============================================================

    private static void SetupCurrencyBar(
        GameObject currencyBar)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                currencyBar);


        rect.anchorMin =
            new Vector2(
                0.58f,
                0.885f);

        rect.anchorMax =
            new Vector2(
                0.98f,
                0.98f);

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;


        HorizontalLayoutGroup layout =
            GetOrAdd<HorizontalLayoutGroup>(
                currencyBar);


        layout.spacing =
            25f;

        layout.childAlignment =
            TextAnchor.MiddleRight;

        layout.childForceExpandWidth =
            false;

        layout.childForceExpandHeight =
            true;


        GameObject gold =
            GetOrCreateChild(
                currencyBar.transform,
                "GoldAmount");


        SetupText(
            gold,
            "0 GOLD",
            18,
            TextAlignmentOptions.Center,
            GoldColor);


        GameObject gem =
            GetOrCreateChild(
                currencyBar.transform,
                "GemAmount");


        SetupText(
            gem,
            "0 GEM",
            18,
            TextAlignmentOptions.Center,
            GemColor);
    }


    // ============================================================
    // CREATE ITEM CARDS
    // ============================================================

    private static ShopItemCard[] CreateItemCards(
        Transform grid)
    {
        ShopItemCard[] cards =
            new ShopItemCard[CARD_COUNT];


        for (int i = 0; i < CARD_COUNT; i++)
        {
            string cardName =
                "ShopItemCard_" +
                i.ToString("00");


            GameObject card =
                GetOrCreateChild(
                    grid,
                    cardName);


            ConfigureCard(
                card);


            ShopItemCard component =
                GetOrAdd<ShopItemCard>(
                    card);


            cards[i] =
                component;
        }


        return cards;
    }


    // ============================================================
    // CONFIGURE CARD
    // ============================================================

    private static void ConfigureCard(
        GameObject card)
    {
        Image background =
            GetOrAdd<Image>(
                card);


        background.color =
            CardColor;


        Button button =
            GetOrAdd<Button>(
                card);


        ConfigureButtonColors(
            button,
            CardColor,
            new Color(
                0.02f,
                0.2f,
                0.25f,
                1f));


        // --------------------------------------------------------
        // BACKGROUND
        // --------------------------------------------------------

        GameObject backgroundObject =
            GetOrCreateChild(
                card.transform,
                "Background");


        SetupImage(
            backgroundObject,
            CardColor);


        SetFullRect(
            backgroundObject);


        // --------------------------------------------------------
        // PREVIEW
        // --------------------------------------------------------

        GameObject preview =
            GetOrCreateChild(
                card.transform,
                "Preview");


        SetupImage(
            preview,
            PreviewColor);


        SetRect(
            preview,
            new Vector2(
                0.08f,
                0.32f),
            new Vector2(
                0.92f,
                0.88f));


        // --------------------------------------------------------
        // ITEM NUMBER
        // --------------------------------------------------------

        GameObject itemNumber =
            GetOrCreateChild(
                card.transform,
                "ItemNumber");


        SetupText(
            itemNumber,
            "#000",
            12,
            TextAlignmentOptions.Left,
            MutedColor);


        SetRect(
            itemNumber,
            new Vector2(
                0.07f,
                0.90f),
            new Vector2(
                0.93f,
                0.99f));


        // --------------------------------------------------------
        // ITEM NAME
        // --------------------------------------------------------

        GameObject itemName =
            GetOrCreateChild(
                card.transform,
                "ItemName");


        SetupText(
            itemName,
            "ITEM",
            15,
            TextAlignmentOptions.Center,
            WhiteColor);


        TMP_Text itemNameText =
            itemName.GetComponent<TMP_Text>();


        itemNameText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;


        SetRect(
            itemName,
            new Vector2(
                0.05f,
                0.20f),
            new Vector2(
                0.95f,
                0.31f));


        // --------------------------------------------------------
        // PRICE
        // --------------------------------------------------------

        GameObject price =
            GetOrCreateChild(
                card.transform,
                "Price");


        SetupText(
            price,
            string.Empty,
            13,
            TextAlignmentOptions.Center,
            GoldColor);


        SetRect(
            price,
            new Vector2(
                0.05f,
                0.08f),
            new Vector2(
                0.95f,
                0.19f));


        // --------------------------------------------------------
        // STATE
        // --------------------------------------------------------

        GameObject state =
            GetOrCreateChild(
                card.transform,
                "State");


        SetupText(
            state,
            string.Empty,
            11,
            TextAlignmentOptions.Center,
            new Color(
                0.25f,
                1f,
                0.55f,
                1f));


        SetRect(
            state,
            new Vector2(
                0.05f,
                0.01f),
            new Vector2(
                0.95f,
                0.09f));


        // --------------------------------------------------------
        // CLICK BUTTON
        // --------------------------------------------------------

        GameObject clickButton =
            GetOrCreateChild(
                card.transform,
                "Button");


        Button click =
            GetOrAdd<Button>(
                clickButton);


        click.targetGraphic =
            background;


        SetFullRect(
            clickButton);
    }


    // ============================================================
    // ASSIGN REFERENCES
    // ============================================================

    private static void AssignShopReferences(
        MainMenuShopUI shop,
        GameItemDatabase database,
        GameObject shopPanel,
        GameObject categoryBar,
        GameObject itemViewport,
        GameObject itemGrid,
        GameObject selectedPanel,
        GameObject pagination,
        GameObject currencyBar,
        ShopItemCard[] cards)
    {
        SerializedObject serialized =
            new SerializedObject(
                shop);


        // --------------------------------------------------------
        // DATABASE
        // --------------------------------------------------------

        SetObjectReference(
            serialized,
            "itemDatabase",
            database);


        // --------------------------------------------------------
        // SHOP PANEL
        // --------------------------------------------------------

        SetObjectReference(
            serialized,
            "shopPanel",
            shopPanel);


        // --------------------------------------------------------
        // CATEGORY BUTTONS
        // --------------------------------------------------------

        Button[] categoryButtons =
            GetCategoryButtons(
                categoryBar.transform);


        TMP_Text[] categoryTexts =
            GetCategoryTexts(
                categoryBar.transform);


        SetObjectArray(
            serialized,
            "categoryButtons",
            categoryButtons);


        SetObjectArray(
            serialized,
            "categoryButtonTexts",
            categoryTexts);


        // --------------------------------------------------------
        // ITEM GRID
        // --------------------------------------------------------

        SetObjectReference(
            serialized,
            "itemGrid",
            itemGrid.transform);


        SetObjectArray(
            serialized,
            "itemCards",
            cards);


        ScrollRect scrollRect =
            itemViewport.GetComponent<ScrollRect>();


        SetObjectReference(
            serialized,
            "itemScrollRect",
            scrollRect);


        // --------------------------------------------------------
        // PAGINATION
        // --------------------------------------------------------

        Button previous =
            FindButton(
                pagination,
                "PreviousPageButton");


        Button next =
            FindButton(
                pagination,
                "NextPageButton");


        TMP_Text pageText =
            FindText(
                pagination,
                "PageText");


        SetObjectReference(
            serialized,
            "previousPageButton",
            previous);


        SetObjectReference(
            serialized,
            "nextPageButton",
            next);


        SetObjectReference(
            serialized,
            "pageText",
            pageText);


        // --------------------------------------------------------
        // SELECTED ITEM
        // --------------------------------------------------------

        GameObject selectedPreview =
            FindDirectChild(
                selectedPanel.transform,
                "SelectedPreview");


        GameObject selectedName =
            FindDirectChild(
                selectedPanel.transform,
                "SelectedName");


        GameObject selectedDescription =
            FindDirectChild(
                selectedPanel.transform,
                "SelectedDescription");


        GameObject selectedPrice =
            FindDirectChild(
                selectedPanel.transform,
                "SelectedPrice");


        GameObject selectedCategory =
            FindDirectChild(
                selectedPanel.transform,
                "SelectedCategory");


        Button buyButton =
            FindButton(
                selectedPanel,
                "BuyButton");


        TMP_Text buyButtonText =
            FindText(
                FindDirectChild(
                    selectedPanel.transform,
                    "BuyButton"),
                "Text");


        SetObjectReference(
            serialized,
            "selectedPreview",
            selectedPreview != null
                ? selectedPreview.GetComponent<Image>()
                : null);


        SetObjectReference(
            serialized,
            "selectedNameText",
            selectedName != null
                ? selectedName.GetComponent<TMP_Text>()
                : null);


        SetObjectReference(
            serialized,
            "selectedDescriptionText",
            selectedDescription != null
                ? selectedDescription.GetComponent<TMP_Text>()
                : null);


        SetObjectReference(
            serialized,
            "selectedPriceText",
            selectedPrice != null
                ? selectedPrice.GetComponent<TMP_Text>()
                : null);


        SetObjectReference(
            serialized,
            "selectedCategoryText",
            selectedCategory != null
                ? selectedCategory.GetComponent<TMP_Text>()
                : null);


        SetObjectReference(
            serialized,
            "buyButton",
            buyButton);


        SetObjectReference(
            serialized,
            "buyButtonText",
            buyButtonText);


        // --------------------------------------------------------
        // CURRENCY
        // --------------------------------------------------------

        GameObject gold =
            FindDirectChild(
                currencyBar.transform,
                "GoldAmount");


        GameObject gem =
            FindDirectChild(
                currencyBar.transform,
                "GemAmount");


        SetObjectReference(
            serialized,
            "goldAmountText",
            gold != null
                ? gold.GetComponent<TMP_Text>()
                : null);


        SetObjectReference(
            serialized,
            "gemAmountText",
            gem != null
                ? gem.GetComponent<TMP_Text>()
                : null);


        // --------------------------------------------------------
        // APPLY
        // --------------------------------------------------------

        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(
            shop);
    }


    // ============================================================
    // CATEGORY BUTTON ARRAY
    // ============================================================

    private static Button[] GetCategoryButtons(
        Transform categoryBar)
    {
        Button[] result =
            new Button[CATEGORY_COUNT];


        for (int i = 0; i < CATEGORY_COUNT; i++)
        {
            GameObject button =
                FindDirectChild(
                    categoryBar,
                    "CategoryButton_" + i);


            if (button == null)
                continue;


            result[i] =
                button.GetComponent<Button>();
        }


        return result;
    }


    // ============================================================
    // CATEGORY TEXT ARRAY
    // ============================================================

    private static TMP_Text[] GetCategoryTexts(
        Transform categoryBar)
    {
        TMP_Text[] result =
            new TMP_Text[CATEGORY_COUNT];


        for (int i = 0; i < CATEGORY_COUNT; i++)
        {
            GameObject button =
                FindDirectChild(
                    categoryBar,
                    "CategoryButton_" + i);


            if (button == null)
                continue;


            result[i] =
                FindText(
                    button,
                    "Text");
        }


        return result;
    }


    // ============================================================
    // SERIALIZED OBJECT REFERENCE
    // ============================================================

    private static void SetObjectReference(
        SerializedObject serializedObject,
        string propertyName,
        Object value)
    {
        SerializedProperty property =
            serializedObject.FindProperty(
                propertyName);


        if (property == null)
        {
            Debug.LogWarning(
                "[MainMenuShopUISetup] " +
                "Không tìm thấy field: " +
                propertyName);

            return;
        }


        if (
            property.propertyType !=
            SerializedPropertyType.ObjectReference)
        {
            Debug.LogWarning(
                "[MainMenuShopUISetup] " +
                "Field không phải ObjectReference: " +
                propertyName);

            return;
        }


        property.objectReferenceValue =
            value;
    }


    // ============================================================
    // SERIALIZED ARRAY
    // ============================================================

    private static void SetObjectArray(
        SerializedObject serializedObject,
        string propertyName,
        Object[] values)
    {
        SerializedProperty property =
            serializedObject.FindProperty(
                propertyName);


        if (property == null)
        {
            Debug.LogWarning(
                "[MainMenuShopUISetup] " +
                "Không tìm thấy array: " +
                propertyName);

            return;
        }


        if (!property.isArray)
        {
            Debug.LogWarning(
                "[MainMenuShopUISetup] " +
                "Field không phải array: " +
                propertyName);

            return;
        }


        property.arraySize =
            values.Length;


        for (int i = 0; i < values.Length; i++)
        {
            SerializedProperty element =
                property.GetArrayElementAtIndex(i);


            element.objectReferenceValue =
                values[i];
        }
    }


    // ============================================================
    // CREATE UI OBJECT
    // ============================================================

    private static GameObject CreateUIObject(
        string name,
        Transform parent)
    {
        GameObject go =
            new GameObject(
                name,
                typeof(RectTransform));


        Undo.RegisterCreatedObjectUndo(
            go,
            "Create " + name);


        go.transform.SetParent(
            parent,
            false);


        go.transform.localScale =
            Vector3.one;


        return go;
    }


    // ============================================================
    // GET OR CREATE
    // ============================================================

    private static GameObject GetOrCreateChild(
        Transform parent,
        string name)
    {
        GameObject existing =
            FindDirectChild(
                parent,
                name);


        if (existing != null)
            return existing;


        return CreateUIObject(
            name,
            parent);
    }


    // ============================================================
    // FIND DIRECT CHILD
    // ============================================================

    private static GameObject FindDirectChild(
        Transform parent,
        string name)
    {
        if (parent == null)
            return null;


        int count =
            parent.childCount;


        for (int i = 0; i < count; i++)
        {
            Transform child =
                parent.GetChild(i);


            if (child.name == name)
                return child.gameObject;
        }


        return null;
    }


    // ============================================================
    // FIND BUTTON
    // ============================================================

    private static Button FindButton(
        GameObject parent,
        string name)
    {
        if (parent == null)
            return null;


        GameObject child =
            FindDirectChild(
                parent.transform,
                name);


        if (child == null)
            return null;


        return child.GetComponent<Button>();
    }


    // ============================================================
    // FIND TEXT
    // ============================================================

    private static TMP_Text FindText(
        GameObject parent,
        string name)
    {
        if (parent == null)
            return null;


        GameObject child =
            FindDirectChild(
                parent.transform,
                name);


        if (child == null)
            return null;


        return child.GetComponent<TMP_Text>();
    }


    // ============================================================
    // GET OR ADD COMPONENT
    // ============================================================

    private static T GetOrAdd<T>(
        GameObject go)
        where T : Component
    {
        T component =
            go.GetComponent<T>();


        if (component != null)
            return component;


        return Undo.AddComponent<T>(
            go);
    }


    // ============================================================
    // SET RECT
    // ============================================================

    private static void SetRect(
        GameObject go,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        RectTransform rect =
            GetOrAdd<RectTransform>(
                go);


        rect.anchorMin =
            anchorMin;

        rect.anchorMax =
            anchorMax;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }


    // ============================================================
    // FULL RECT
    // ============================================================

    private static void SetFullRect(
        GameObject go)
    {
        SetRect(
            go,
            Vector2.zero,
            Vector2.one);
    }


    // ============================================================
    // IMAGE
    // ============================================================

    private static void SetupImage(
        GameObject go,
        Color color)
    {
        Image image =
            GetOrAdd<Image>(
                go);


        image.color =
            color;


        image.raycastTarget =
            false;
    }


    // ============================================================
    // BUTTON
    // ============================================================

    private static void SetupButton(
        GameObject go,
        Color color)
    {
        Image image =
            GetOrAdd<Image>(
                go);


        image.color =
            color;


        Button button =
            GetOrAdd<Button>(
                go);


        button.targetGraphic =
            image;


        ConfigureButtonColors(
            button,
            color,
            Color.Lerp(
                color,
                Color.black,
                0.2f));
    }


    // ============================================================
    // BUTTON COLORS
    // ============================================================

    private static void ConfigureButtonColors(
        Button button,
        Color normal,
        Color pressed)
    {
        ColorBlock colors =
            button.colors;


        colors.normalColor =
            normal;


        colors.highlightedColor =
            Color.Lerp(
                normal,
                Color.white,
                0.12f);


        colors.pressedColor =
            pressed;


        colors.selectedColor =
            Color.Lerp(
                normal,
                Color.white,
                0.08f);


        colors.disabledColor =
            new Color(
                normal.r,
                normal.g,
                normal.b,
                0.45f);


        colors.colorMultiplier =
            1f;


        colors.fadeDuration =
            0.08f;


        button.colors =
            colors;


        button.transition =
            Selectable.Transition.ColorTint;
    }


    // ============================================================
    // TEXT
    // ============================================================

    private static void SetupText(
        GameObject go,
        string value,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color)
    {
        TMP_Text text =
            GetOrAdd<TextMeshProUGUI>(
                go);


        text.text =
            value;


        text.fontSize =
            fontSize;


        text.color =
            color;


        text.alignment =
            alignment;


        text.textWrappingMode = TMPro.TextWrappingModes.Normal;


        text.raycastTarget =
            false;


        RectTransform rect =
            text.rectTransform;


        rect.localScale =
            Vector3.one;
    }
}

#endif