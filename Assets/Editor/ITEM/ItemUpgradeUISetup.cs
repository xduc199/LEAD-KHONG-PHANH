#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ItemUpgradeUISetup
{
    private const string CanvasName = "ItemUpgradeCanvas";

    [MenuItem("Tools/Lead Khong Phanh/UI/Create Item Upgrade UI")]
    public static void CreateItemUpgradeUI()
    {
        ItemUpgradeUI existing =
            Object.FindFirstObjectByType<ItemUpgradeUI>(
                FindObjectsInactive.Include);

        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;

            Debug.LogWarning(
                "[ItemUpgradeUISetup] ItemUpgradeUI đã tồn tại. " +
                "Xóa UI cũ trước khi chạy Setup lại.");

            return;
        }

        EnsureEventSystem();

        GameObject canvasObject = CreateCanvas();

        GameObject rootObject = CreatePanel(
            "ItemUpgradeUI",
            canvasObject.transform,
            new Color(0.025f, 0.03f, 0.045f, 1f));

        StretchFull(rootObject.GetComponent<RectTransform>());

        ItemUpgradeUI controller =
            rootObject.AddComponent<ItemUpgradeUI>();

        // =====================================================
        // DIM BACKGROUND
        // =====================================================

        GameObject dimBackground = CreatePanel(
            "DimBackground",
            rootObject.transform,
            new Color(0f, 0f, 0f, 0.72f));

        StretchFull(dimBackground.GetComponent<RectTransform>());

        // =====================================================
        // OPEN BUTTON
        // =====================================================

        GameObject openButton = CreateButton(
            "ItemUpgradeOpenButton",
            rootObject.transform,
            "NÂNG CẤP VẬT PHẨM");

        RectTransform openRect =
            openButton.GetComponent<RectTransform>();

        SetAnchors(
            openRect,
            0.035f,
            0.035f,
            0.245f,
            0.105f);

        // =====================================================
        // WINDOW
        // =====================================================

        GameObject window = CreatePanel(
            "ItemUpgradeWindow",
            rootObject.transform,
            new Color(0.045f, 0.055f, 0.075f, 0.99f));

        RectTransform windowRect =
            window.GetComponent<RectTransform>();

        SetAnchors(
            windowRect,
            0.07f,
            0.06f,
            0.93f,
            0.94f);

        AddOutline(window, 3f);

        // =====================================================
        // HEADER
        // =====================================================

        CreateHeader(window.transform);

        // =====================================================
        // ITEM TABS
        // =====================================================

        CreateItemTabs(window.transform);

        // =====================================================
        // LEVEL PROGRESS
        // =====================================================

        CreateLevelProgress(window.transform);

        // =====================================================
        // STATS
        // =====================================================

        CreateStatsArea(window.transform);

        // =====================================================
        // BOTTOM
        // =====================================================

        CreateBottomArea(window.transform);

        // =====================================================
        // REFERENCES
        // =====================================================

        AssignReferences(
            controller,
            rootObject);

        // =====================================================
        // INITIAL STATE
        // =====================================================

        window.SetActive(false);

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(rootObject.scene);

        Selection.activeGameObject = rootObject;

        Debug.Log(
            "[ItemUpgradeUISetup] Item Upgrade UI đã được tạo thành công.");
    }

    // =========================================================
    // CANVAS
    // =========================================================

    private static GameObject CreateCanvas()
    {
        GameObject existing = GameObject.Find(CanvasName);

        if (existing != null &&
            existing.GetComponent<Canvas>() != null)
        {
            return existing;
        }

        GameObject canvasObject = new GameObject(
            CanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f, 1080f);

        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        scaler.matchWidthOrHeight = 0.5f;

        Undo.RegisterCreatedObjectUndo(
            canvasObject,
            "Create Item Upgrade Canvas");

        return canvasObject;
    }

    // =========================================================
    // EVENT SYSTEM
    // =========================================================

    private static void EnsureEventSystem()
    {
        EventSystem existing =
            Object.FindFirstObjectByType<EventSystem>(
                FindObjectsInactive.Include);

        if (existing != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));

        Undo.RegisterCreatedObjectUndo(
            eventSystem,
            "Create Item Upgrade EventSystem");
    }

    // =========================================================
    // HEADER
    // =========================================================

    private static void CreateHeader(Transform parent)
    {
        GameObject header = CreatePanel(
            "Header",
            parent,
            new Color(0.07f, 0.085f, 0.11f, 1f));

        SetAnchors(
            header.GetComponent<RectTransform>(),
            0.035f,
            0.84f,
            0.965f,
            0.97f);

        CreateText(
            "ItemName",
            header.transform,
            "NÂNG CẤP VẬT PHẨM",
            38,
            TextAlignmentOptions.Left,
            new Vector2(0.025f, 0.1f),
            new Vector2(0.52f, 0.9f));

        CreateText(
            "CurrentLevel",
            header.transform,
            "CẤP 1 / 20",
            27,
            TextAlignmentOptions.Center,
            new Vector2(0.52f, 0.1f),
            new Vector2(0.74f, 0.9f));

        CreateText(
            "Coins",
            header.transform,
            "0 VÀNG",
            27,
            TextAlignmentOptions.Right,
            new Vector2(0.74f, 0.1f),
            new Vector2(0.965f, 0.9f));

        // Close button
        GameObject closeButton = CreateButton(
            "CloseButton",
            header.transform,
            "X");

        RectTransform closeRect =
            closeButton.GetComponent<RectTransform>();

        SetAnchors(
            closeRect,
            0.925f,
            0.12f,
            0.985f,
            0.88f);

        TextMeshProUGUI closeText =
            closeButton
                .GetComponentInChildren<TextMeshProUGUI>();

        if (closeText != null)
        {
            closeText.fontSize = 28;
        }
    }

    // =========================================================
    // ITEM TABS
    // =========================================================

    private static void CreateItemTabs(Transform parent)
    {
        GameObject tabs = CreatePanel(
            "ItemTabs",
            parent,
            new Color(0.035f, 0.045f, 0.065f, 1f));

        SetAnchors(
            tabs.GetComponent<RectTransform>(),
            0.035f,
            0.725f,
            0.965f,
            0.825f);

        HorizontalLayoutGroup layout =
            tabs.AddComponent<HorizontalLayoutGroup>();

        layout.spacing = 14f;

        layout.padding =
            new RectOffset(12, 12, 10, 10);

        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        CreateButton(
            "GumButton",
            tabs.transform,
            "GUM");

        CreateButton(
            "PhotonButton",
            tabs.transform,
            "PHOTON");

        CreateButton(
            "ShieldButton",
            tabs.transform,
            "SHIELD");

        CreateButton(
            "MagnetButton",
            tabs.transform,
            "MAGNET");
    }

    // =========================================================
    // LEVEL PROGRESS
    // =========================================================

    private static void CreateLevelProgress(Transform parent)
    {
        GameObject progress =
            CreatePanel(
                "LevelProgress",
                parent,
                new Color(0.035f, 0.045f, 0.065f, 1f));

        SetAnchors(
            progress.GetComponent<RectTransform>(),
            0.035f,
            0.665f,
            0.965f,
            0.715f);

        CreateText(
            "LevelProgressText",
            progress.transform,
            "TIẾN ĐỘ  CẤP 1 / 20",
            20,
            TextAlignmentOptions.Left,
            new Vector2(0.015f, 0f),
            new Vector2(0.30f, 1f));

        GameObject sliderObject =
            new GameObject(
                "LevelProgressSlider",
                typeof(RectTransform),
                typeof(Slider));

        sliderObject.transform.SetParent(
            progress.transform,
            false);

        RectTransform sliderRect =
            sliderObject.GetComponent<RectTransform>();

        SetAnchors(
            sliderRect,
            0.32f,
            0.25f,
            0.985f,
            0.75f);

        Slider slider =
            sliderObject.GetComponent<Slider>();

        slider.minValue = 0f;

        // =====================================================
        // MAX LEVEL = 20
        // =====================================================

        slider.maxValue = 20f;
        slider.value = 1f;

        slider.wholeNumbers = false;

        CreateSliderVisuals(slider);
    }

    // =========================================================
    // SLIDER VISUAL
    // =========================================================

    private static void CreateSliderVisuals(Slider slider)
    {
        GameObject background =
            new GameObject(
                "Background",
                typeof(RectTransform),
                typeof(Image));

        background.transform.SetParent(
            slider.transform,
            false);

        RectTransform backgroundRect =
            background.GetComponent<RectTransform>();

        StretchFull(backgroundRect);

        Image backgroundImage =
            background.GetComponent<Image>();

        backgroundImage.color =
            new Color(0.12f, 0.14f, 0.18f, 1f);

        GameObject fillArea =
            new GameObject(
                "Fill Area",
                typeof(RectTransform));

        fillArea.transform.SetParent(
            slider.transform,
            false);

        RectTransform fillAreaRect =
            fillArea.GetComponent<RectTransform>();

        StretchFull(fillAreaRect);

        fillAreaRect.offsetMin =
            new Vector2(4f, 4f);

        fillAreaRect.offsetMax =
            new Vector2(-4f, -4f);

        GameObject fill =
            new GameObject(
                "Fill",
                typeof(RectTransform),
                typeof(Image));

        fill.transform.SetParent(
            fillArea.transform,
            false);

        RectTransform fillRect =
            fill.GetComponent<RectTransform>();

        StretchFull(fillRect);

        Image fillImage =
            fill.GetComponent<Image>();

        fillImage.color =
            new Color(0.95f, 0.68f, 0.12f, 1f);

        slider.fillRect = fillRect;

        slider.handleRect = null;

        slider.direction =
            Slider.Direction.LeftToRight;
    }

    // =========================================================
    // STATS AREA
    // =========================================================

    private static void CreateStatsArea(Transform parent)
    {
        GameObject stats =
            CreatePanel(
                "StatsArea",
                parent,
                new Color(0f, 0f, 0f, 0f));

        SetAnchors(
            stats.GetComponent<RectTransform>(),
            0.035f,
            0.245f,
            0.965f,
            0.65f);

        CreateCurrentPanel(stats.transform);
        CreateNextPanel(stats.transform);
    }

    // =========================================================
    // CURRENT PANEL
    // =========================================================

    private static void CreateCurrentPanel(Transform parent)
    {
        GameObject panel =
            CreatePanel(
                "CurrentPanel",
                parent,
                new Color(0.065f, 0.075f, 0.10f, 1f));

        SetAnchors(
            panel.GetComponent<RectTransform>(),
            0f,
            0f,
            0.485f,
            1f);

        AddOutline(panel, 2f);

        CreateText(
            "CurrentTitle",
            panel.transform,
            "HIỆN TẠI",
            28,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.78f),
            new Vector2(0.95f, 0.96f));

        CreateText(
            "CurrentStat1",
            panel.transform,
            "-",
            24,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.51f),
            new Vector2(0.95f, 0.70f));

        CreateText(
            "CurrentStat2",
            panel.transform,
            "-",
            24,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.28f),
            new Vector2(0.95f, 0.47f));

        CreateText(
            "CurrentStat3",
            panel.transform,
            "-",
            24,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.05f),
            new Vector2(0.95f, 0.24f));
    }

    // =========================================================
    // NEXT PANEL
    // =========================================================

    private static void CreateNextPanel(Transform parent)
    {
        GameObject panel =
            CreatePanel(
                "NextPanel",
                parent,
                new Color(0.08f, 0.09f, 0.12f, 1f));

        SetAnchors(
            panel.GetComponent<RectTransform>(),
            0.515f,
            0f,
            1f,
            1f);

        AddOutline(panel, 2f);

        CreateText(
            "NextTitle",
            panel.transform,
            "CẤP TIẾP THEO",
            28,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.78f),
            new Vector2(0.95f, 0.96f));

        CreateText(
            "NextLevel",
            panel.transform,
            "CẤP 2",
            25,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.62f),
            new Vector2(0.95f, 0.78f));

        CreateText(
            "NextStat1",
            panel.transform,
            "-",
            24,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.39f),
            new Vector2(0.95f, 0.58f));

        CreateText(
            "NextStat2",
            panel.transform,
            "-",
            24,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.17f),
            new Vector2(0.95f, 0.36f));

        CreateText(
            "NextStat3",
            panel.transform,
            "-",
            24,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.01f),
            new Vector2(0.95f, 0.16f));
    }

    // =========================================================
    // BOTTOM
    // =========================================================

    private static void CreateBottomArea(Transform parent)
    {
        GameObject bottom =
            CreatePanel(
                "Bottom",
                parent,
                new Color(0.035f, 0.045f, 0.065f, 1f));

        SetAnchors(
            bottom.GetComponent<RectTransform>(),
            0.035f,
            0.035f,
            0.965f,
            0.21f);

        CreateText(
            "UpgradeStatus",
            bottom.transform,
            "SẴN SÀNG NÂNG CẤP",
            21,
            TextAlignmentOptions.Left,
            new Vector2(0.025f, 0.60f),
            new Vector2(0.53f, 0.90f));

        CreateText(
            "UpgradeCost",
            bottom.transform,
            "GIÁ NÂNG CẤP: 0 VÀNG",
            24,
            TextAlignmentOptions.Left,
            new Vector2(0.025f, 0.12f),
            new Vector2(0.50f, 0.55f));

        GameObject upgradeButton =
            CreateButton(
                "UpgradeButton",
                bottom.transform,
                "NÂNG CẤP");

        SetAnchors(
            upgradeButton.GetComponent<RectTransform>(),
            0.68f,
            0.15f,
            0.97f,
            0.85f);
    }

    // =========================================================
    // MAX LEVEL PANEL
    // =========================================================

    private static GameObject CreateMaxLevelPanel(GameObject root)
    {
        Transform bottom =
            FindTransform(root, "Bottom");

        if (bottom == null)
        {
            Debug.LogError(
                "[ItemUpgradeUISetup] Không tìm thấy Bottom.");

            return null;
        }

        GameObject maxPanel =
            CreatePanel(
                "MaxLevelPanel",
                bottom,
                new Color(0.12f, 0.09f, 0.035f, 0.98f));

        SetAnchors(
            maxPanel.GetComponent<RectTransform>(),
            0.50f,
            0.10f,
            0.97f,
            0.90f);

        CreateText(
            "Text",
            maxPanel.transform,
            "★ ĐÃ ĐẠT CẤP TỐI ĐA ★",
            23,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.one);

        return maxPanel;
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private static void AssignReferences(
        ItemUpgradeUI controller,
        GameObject root)
    {
        SerializedObject serialized =
            new SerializedObject(controller);

        // =====================================================
        // WINDOW
        // =====================================================

        AssignObject(
            serialized,
            "windowPanel",
            FindTransform(
                root,
                "ItemUpgradeWindow")?.gameObject);

        AssignObject(
            serialized,
            "openButton",
            FindComponent<Button>(
                root,
                "ItemUpgradeOpenButton"));

        AssignObject(
            serialized,
            "closeButton",
            FindComponent<Button>(
                root,
                "CloseButton"));

        // =====================================================
        // ITEM BUTTONS
        // =====================================================

        AssignObject(
            serialized,
            "gumButton",
            FindComponent<Button>(
                root,
                "GumButton"));

        AssignObject(
            serialized,
            "photonButton",
            FindComponent<Button>(
                root,
                "PhotonButton"));

        AssignObject(
            serialized,
            "shieldButton",
            FindComponent<Button>(
                root,
                "ShieldButton"));

        AssignObject(
            serialized,
            "magnetButton",
            FindComponent<Button>(
                root,
                "MagnetButton"));

        // =====================================================
        // HEADER
        // =====================================================

        AssignObject(
            serialized,
            "itemNameText",
            FindComponent<TMP_Text>(
                root,
                "ItemName"));

        AssignObject(
            serialized,
            "currentLevelText",
            FindComponent<TMP_Text>(
                root,
                "CurrentLevel"));

        AssignObject(
            serialized,
            "coinsText",
            FindComponent<TMP_Text>(
                root,
                "Coins"));

        // =====================================================
        // PROGRESS
        // =====================================================

        AssignObject(
            serialized,
            "levelProgressText",
            FindComponent<TMP_Text>(
                root,
                "LevelProgressText"));

        AssignObject(
            serialized,
            "levelProgressSlider",
            FindComponent<Slider>(
                root,
                "LevelProgressSlider"));

        // =====================================================
        // CURRENT
        // =====================================================

        AssignObject(
            serialized,
            "currentTitleText",
            FindComponent<TMP_Text>(
                root,
                "CurrentTitle"));

        AssignObject(
            serialized,
            "currentStat1Text",
            FindComponent<TMP_Text>(
                root,
                "CurrentStat1"));

        AssignObject(
            serialized,
            "currentStat2Text",
            FindComponent<TMP_Text>(
                root,
                "CurrentStat2"));

        AssignObject(
            serialized,
            "currentStat3Text",
            FindComponent<TMP_Text>(
                root,
                "CurrentStat3"));

        // =====================================================
        // NEXT
        // =====================================================

        AssignObject(
            serialized,
            "nextTitleText",
            FindComponent<TMP_Text>(
                root,
                "NextTitle"));

        AssignObject(
            serialized,
            "nextLevelText",
            FindComponent<TMP_Text>(
                root,
                "NextLevel"));

        AssignObject(
            serialized,
            "nextStat1Text",
            FindComponent<TMP_Text>(
                root,
                "NextStat1"));

        AssignObject(
            serialized,
            "nextStat2Text",
            FindComponent<TMP_Text>(
                root,
                "NextStat2"));

        AssignObject(
            serialized,
            "nextStat3Text",
            FindComponent<TMP_Text>(
                root,
                "NextStat3"));

        // =====================================================
        // UPGRADE
        // =====================================================

        AssignObject(
            serialized,
            "upgradeButton",
            FindComponent<Button>(
                root,
                "UpgradeButton"));

        AssignObject(
            serialized,
            "upgradeCostText",
            FindComponent<TMP_Text>(
                root,
                "UpgradeCost"));

        AssignObject(
            serialized,
            "upgradeStatusText",
            FindComponent<TMP_Text>(
                root,
                "UpgradeStatus"));

        // =====================================================
        // PANELS
        // =====================================================

        AssignObject(
            serialized,
            "nextLevelPanel",
            FindTransform(
                root,
                "NextPanel")?.gameObject);

        GameObject maxPanel =
            CreateMaxLevelPanel(root);

        AssignObject(
            serialized,
            "maxLevelPanel",
            maxPanel);

        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(controller);
    }

    // =========================================================
    // CREATE PANEL
    // =========================================================

    private static GameObject CreatePanel(
        string objectName,
        Transform parent,
        Color color)
    {
        GameObject objectRoot =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image));

        objectRoot.transform.SetParent(
            parent,
            false);

        Image image =
            objectRoot.GetComponent<Image>();

        image.color = color;

        return objectRoot;
    }

    // =========================================================
    // CREATE BUTTON
    // =========================================================

    private static GameObject CreateButton(
        string objectName,
        Transform parent,
        string label)
    {
        GameObject buttonObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));

        buttonObject.transform.SetParent(
            parent,
            false);

        Image image =
            buttonObject.GetComponent<Image>();

        image.color =
            new Color(0.12f, 0.14f, 0.18f, 1f);

        Button button =
            buttonObject.GetComponent<Button>();

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            new Color(0.12f, 0.14f, 0.18f, 1f);

        colors.highlightedColor =
            new Color(0.20f, 0.24f, 0.30f, 1f);

        colors.pressedColor =
            new Color(0.08f, 0.10f, 0.13f, 1f);

        colors.selectedColor =
            new Color(0.20f, 0.24f, 0.30f, 1f);

        button.colors = colors;

        GameObject textObject =
            CreateText(
                "Text",
                buttonObject.transform,
                label,
                22,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one);

        StretchFull(
            textObject.GetComponent<RectTransform>());

        return buttonObject;
    }

    // =========================================================
    // CREATE TEXT
    // =========================================================

    private static GameObject CreateText(
        string objectName,
        Transform parent,
        string text,
        float fontSize,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject textObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

        textObject.transform.SetParent(
            parent,
            false);

        RectTransform rect =
            textObject.GetComponent<RectTransform>();

        SetAnchors(
            rect,
            anchorMin.x,
            anchorMin.y,
            anchorMax.x,
            anchorMax.y);

        TextMeshProUGUI tmp =
            textObject.GetComponent<TextMeshProUGUI>();

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;

        tmp.textWrappingMode =
            TextWrappingModes.NoWrap;

        tmp.overflowMode =
            TextOverflowModes.Ellipsis;

        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font =
                TMP_Settings.defaultFontAsset;
        }

        return textObject;
    }

    // =========================================================
    // OUTLINE
    // =========================================================

    private static void AddOutline(
        GameObject target,
        float distance)
    {
        Outline outline =
            target.AddComponent<Outline>();

        outline.effectDistance =
            new Vector2(distance, -distance);

        outline.useGraphicAlpha = true;
    }

    // =========================================================
    // RECT HELPERS
    // =========================================================

    private static void StretchFull(
        RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;

        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(
        RectTransform rect,
        float minX,
        float minY,
        float maxX,
        float maxY)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin =
            new Vector2(minX, minY);

        rect.anchorMax =
            new Vector2(maxX, maxY);

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }

    // =========================================================
    // FIND COMPONENT
    // =========================================================

    private static T FindComponent<T>(
        GameObject root,
        string childName)
        where T : Component
    {
        Transform child =
            FindTransform(
                root,
                childName);

        if (child == null)
        {
            Debug.LogError(
                "[ItemUpgradeUISetup] Không tìm thấy: " +
                childName);

            return null;
        }

        T component =
            child.GetComponent<T>();

        if (component == null)
        {
            Debug.LogError(
                "[ItemUpgradeUISetup] " +
                childName +
                " thiếu component " +
                typeof(T).Name);
        }

        return component;
    }

    // =========================================================
    // FIND TRANSFORM
    // =========================================================

    private static Transform FindTransform(
        GameObject root,
        string childName)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] transforms =
            root.GetComponentsInChildren<Transform>(
                true);

        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == childName)
            {
                return transforms[i];
            }
        }

        return null;
    }

    // =========================================================
    // SERIALIZED ASSIGN
    // =========================================================

    private static void AssignObject(
        SerializedObject serialized,
        string propertyName,
        Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName);

        if (property == null)
        {
            Debug.LogError(
                "[ItemUpgradeUISetup] " +
                "Không tìm thấy field: " +
                propertyName);

            return;
        }

        property.objectReferenceValue =
            value;
    }
}

#endif