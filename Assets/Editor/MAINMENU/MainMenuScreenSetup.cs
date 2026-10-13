#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

using TMPro;

public static class MainMenuScreenSetup
{
    // ============================================================
    // MENU SIZE
    // ============================================================

    private const float CanvasWidth = 1920f;
    private const float CanvasHeight = 1080f;

    private const float ButtonWidth = 360f;
    private const float ButtonHeight = 72f;
    private const float ButtonGap = 18f;

    // ============================================================
    // COLORS
    // ============================================================

    private static readonly Color BackgroundColor =
        new Color(0.025f, 0.03f, 0.04f, 1f);

    private static readonly Color MenuButtonColor =
        new Color(0.10f, 0.12f, 0.14f, 0.96f);

    private static readonly Color MenuButtonHoverColor =
        new Color(0.16f, 0.18f, 0.20f, 1f);

    private static readonly Color PlayButtonColor =
        new Color(0.92f, 0.62f, 0.04f, 1f);

    private static readonly Color White =
        Color.white;

    // ============================================================
    // MAIN ENTRY
    // ============================================================

    [MenuItem("LEAD KHONG PHANH/Setup/Main Menu")]
    public static void SetupMainMenu()
    {
        Debug.Log(
            "[MainMenuScreenSetup] ===== START SETUP ====="
        );

        // ========================================================
        // CONTROLLER
        // ========================================================

        GameObject controllerObject =
            FindOrCreateMainMenuController();

        MainMenuController controller =
            controllerObject.GetComponent<MainMenuController>();

        if (controller == null)
        {
            Debug.LogError(
                "[MainMenuScreenSetup] Không tìm thấy MainMenuController."
            );

            return;
        }

        // ========================================================
        // CANVAS
        // ========================================================

        Canvas canvas =
            FindOrCreateCanvas();

        // ========================================================
        // EVENT SYSTEM
        // ========================================================

        EventSystem eventSystem =
            FindOrCreateEventSystem();

        // ========================================================
        // MAIN MENU SCREEN
        // ========================================================

        GameObject mainMenuScreen =
            FindOrCreateObject(
                "MainMenuScreen",
                canvas.transform
            );

        RectTransform mainMenuRect =
            GetRectTransform(mainMenuScreen);

        StretchFullScreen(mainMenuRect);

        // ========================================================
        // BACKGROUND
        // ========================================================

        GameObject background =
            FindOrCreateObject(
                "Background",
                mainMenuScreen.transform
            );

        RectTransform backgroundRect =
            GetRectTransform(background);

        StretchFullScreen(backgroundRect);

        Image backgroundImage =
            GetOrAddComponent<Image>(background);

        backgroundImage.color =
            BackgroundColor;

        background.transform.SetAsFirstSibling();

        // ========================================================
        // LEFT MENU
        // ========================================================

        GameObject leftMenu =
            FindOrCreateObject(
                "LeftMenu",
                mainMenuScreen.transform
            );

        RectTransform leftMenuRect =
            GetRectTransform(leftMenu);

        leftMenuRect.anchorMin =
            new Vector2(0f, 0.5f);

        leftMenuRect.anchorMax =
            new Vector2(0f, 0.5f);

        leftMenuRect.pivot =
            new Vector2(0f, 0.5f);

        leftMenuRect.sizeDelta =
            new Vector2(
                ButtonWidth,
                5f * ButtonHeight +
                4f * ButtonGap
            );

        leftMenuRect.anchoredPosition =
            new Vector2(80f, 40f);

        // ========================================================
        // MAIN BUTTONS
        // ========================================================

        GameObject shopButton =
            CreateButton(
                "ShopButton",
                "CỬA HÀNG",
                leftMenu.transform,
                0
            );

        GameObject wardrobeButton =
            CreateButton(
                "WardrobeButton",
                "TỦ ĐỒ",
                leftMenu.transform,
                1
            );

        GameObject garageButton =
            CreateButton(
                "GarageButton",
                "GARAGE",
                leftMenu.transform,
                2
            );

        GameObject upgradeButton =
            CreateButton(
                "UpgradeButton",
                "NÂNG CẤP",
                leftMenu.transform,
                3
            );

        GameObject missionButton =
            CreateButton(
                "MissionButton",
                "NHIỆM VỤ",
                leftMenu.transform,
                4
            );

        // ========================================================
        // PLAY BUTTON
        // ========================================================

        GameObject playButton =
            CreatePlayButton(
                mainMenuScreen.transform
            );

        // ========================================================
        // PANELS
        //
        // QUAN TRỌNG:
        // Panel là CON TRỰC TIẾP của CANVAS.
        // KHÔNG được đặt bên trong MainMenuScreen.
        // ========================================================

        GameObject shopPanel =
            CreatePanel(
                "ShopPanel",
                canvas.transform,
                "CỬA HÀNG"
            );

        GameObject wardrobePanel =
            CreatePanel(
                "WardrobePanel",
                canvas.transform,
                "TỦ ĐỒ"
            );

        GameObject garagePanel =
            CreatePanel(
                "GaragePanel",
                canvas.transform,
                "GARAGE"
            );

        GameObject upgradePanel =
            CreatePanel(
                "UpgradePanel",
                canvas.transform,
                "NÂNG CẤP"
            );

        GameObject missionPanel =
            CreatePanel(
                "MissionPanel",
                canvas.transform,
                "NHIỆM VỤ"
            );

        // ========================================================
        // CLOSE BUTTONS
        // ========================================================

        GameObject closeShop =
            CreateCloseButton(
                shopPanel.transform,
                "CloseShopButton"
            );

        GameObject closeWardrobe =
            CreateCloseButton(
                wardrobePanel.transform,
                "CloseWardrobeButton"
            );

        GameObject closeGarage =
            CreateCloseButton(
                garagePanel.transform,
                "CloseGarageButton"
            );

        GameObject closeUpgrade =
            CreateCloseButton(
                upgradePanel.transform,
                "CloseUpgradeButton"
            );

        GameObject closeMission =
            CreateCloseButton(
                missionPanel.transform,
                "CloseMissionButton"
            );

        // ========================================================
        // CONTROLLER REFERENCES
        // ========================================================

        SerializedObject serializedController =
            new SerializedObject(controller);

        SetObjectReference(
            serializedController,
            "mainMenuScreen",
            mainMenuScreen
        );

        SetObjectReference(
            serializedController,
            "shopPanel",
            shopPanel
        );

        SetObjectReference(
            serializedController,
            "wardrobePanel",
            wardrobePanel
        );

        SetObjectReference(
            serializedController,
            "garagePanel",
            garagePanel
        );

        SetObjectReference(
            serializedController,
            "upgradePanel",
            upgradePanel
        );

        SetObjectReference(
            serializedController,
            "missionPanel",
            missionPanel
        );

        SetObjectReference(
            serializedController,
            "playButton",
            playButton.GetComponent<Button>()
        );

        serializedController.ApplyModifiedProperties();

        // ========================================================
        // MAIN BUTTON EVENTS
        //
        // FIX:
        // controller.OpenShop
        // KHÔNG PHẢI:
        // MainMenuController.OpenShop
        // ========================================================

        AddPersistentListener(
            shopButton.GetComponent<Button>(),
            controller,
            controller.OpenShop
        );

        AddPersistentListener(
            wardrobeButton.GetComponent<Button>(),
            controller,
            controller.OpenWardrobe
        );

        AddPersistentListener(
            garageButton.GetComponent<Button>(),
            controller,
            controller.OpenGarage
        );

        AddPersistentListener(
            upgradeButton.GetComponent<Button>(),
            controller,
            controller.OpenUpgrade
        );

        AddPersistentListener(
            missionButton.GetComponent<Button>(),
            controller,
            controller.OpenMission
        );

        // ========================================================
        // CLOSE EVENTS
        // ========================================================

        AddPersistentListener(
            closeShop.GetComponent<Button>(),
            controller,
            controller.CloseShop
        );

        AddPersistentListener(
            closeWardrobe.GetComponent<Button>(),
            controller,
            controller.CloseWardrobe
        );

        AddPersistentListener(
            closeGarage.GetComponent<Button>(),
            controller,
            controller.CloseGarage
        );

        AddPersistentListener(
            closeUpgrade.GetComponent<Button>(),
            controller,
            controller.CloseUpgrade
        );

        AddPersistentListener(
            closeMission.GetComponent<Button>(),
            controller,
            controller.CloseMission
        );

        // ========================================================
        // INITIAL STATE
        // ========================================================

        mainMenuScreen.SetActive(true);

        shopPanel.SetActive(false);
        wardrobePanel.SetActive(false);
        garagePanel.SetActive(false);
        upgradePanel.SetActive(false);
        missionPanel.SetActive(false);

        // ========================================================
        // SORTING
        // ========================================================

        // Main menu nằm dưới.
        mainMenuScreen.transform.SetAsFirstSibling();

        // Các panel nằm trên MainMenuScreen.
        shopPanel.transform.SetAsLastSibling();
        wardrobePanel.transform.SetAsLastSibling();
        garagePanel.transform.SetAsLastSibling();
        upgradePanel.transform.SetAsLastSibling();
        missionPanel.transform.SetAsLastSibling();

        // ========================================================
        // MARK DIRTY
        // ========================================================

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(canvas);
        EditorUtility.SetDirty(eventSystem);

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );

        Selection.activeGameObject =
            mainMenuScreen;

        Debug.Log(
            "[MainMenuScreenSetup] ===== SETUP COMPLETED ====="
        );
    }

    // ============================================================
    // MAIN MENU CONTROLLER
    // ============================================================

    private static GameObject FindOrCreateMainMenuController()
    {
        MainMenuController existing =
            Object.FindFirstObjectByType<MainMenuController>(
                FindObjectsInactive.Include
            );

        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject obj =
            new GameObject(
                "MainMenuController"
            );

        Undo.RegisterCreatedObjectUndo(
            obj,
            "Create MainMenuController"
        );

        obj.AddComponent<MainMenuController>();

        return obj;
    }

    // ============================================================
    // CANVAS
    // ============================================================

    private static Canvas FindOrCreateCanvas()
    {
        Canvas existing =
            Object.FindFirstObjectByType<Canvas>(
                FindObjectsInactive.Include
            );

        if (existing != null)
        {
            ConfigureCanvas(existing);
            return existing;
        }

        GameObject obj =
            new GameObject(
                "MainMenuCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

        Undo.RegisterCreatedObjectUndo(
            obj,
            "Create MainMenuCanvas"
        );

        Canvas canvas =
            obj.GetComponent<Canvas>();

        ConfigureCanvas(canvas);

        return canvas;
    }

    private static void ConfigureCanvas(
        Canvas canvas
    )
    {
        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder = 0;

        CanvasScaler scaler =
            canvas.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(
                CanvasWidth,
                CanvasHeight
            );

        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster =
            canvas.GetComponent<GraphicRaycaster>();

        raycaster.enabled = true;
    }

    // ============================================================
    // EVENT SYSTEM
    // ============================================================

    private static EventSystem FindOrCreateEventSystem()
    {
        EventSystem existing =
            Object.FindFirstObjectByType<EventSystem>(
                FindObjectsInactive.Include
            );

        if (existing != null)
        {
            EnsureInputModule(
                existing.gameObject
            );

            return existing;
        }

        GameObject obj =
            new GameObject(
                "EventSystem",
                typeof(EventSystem)
            );

        Undo.RegisterCreatedObjectUndo(
            obj,
            "Create EventSystem"
        );

        EnsureInputModule(obj);

        return obj.GetComponent<EventSystem>();
    }

    private static void EnsureInputModule(
        GameObject obj
    )
    {
        if (
            obj.GetComponent<InputSystemUIInputModule>() ==
            null
        )
        {
            obj.AddComponent<InputSystemUIInputModule>();
        }
    }

    // ============================================================
    // FIND OR CREATE OBJECT
    // ============================================================

    private static GameObject FindOrCreateObject(
        string objectName,
        Transform parent
    )
    {
        Transform existing =
            parent.Find(objectName);

        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform)
            );

        Undo.RegisterCreatedObjectUndo(
            obj,
            "Create " + objectName
        );

        obj.transform.SetParent(
            parent,
            false
        );

        return obj;
    }

    // ============================================================
    // GET OR ADD COMPONENT
    // ============================================================

    private static T GetOrAddComponent<T>(
        GameObject obj
    )
        where T : Component
    {
        T component =
            obj.GetComponent<T>();

        if (component != null)
        {
            return component;
        }

        return Undo.AddComponent<T>(obj);
    }

    // ============================================================
    // RECT TRANSFORM
    // ============================================================

    private static RectTransform GetRectTransform(
        GameObject obj
    )
    {
        RectTransform rect =
            obj.GetComponent<RectTransform>();

        if (rect != null)
        {
            return rect;
        }

        return Undo.AddComponent<RectTransform>(obj);
    }

    private static void StretchFullScreen(
        RectTransform rect
    )
    {
        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        rect.localScale =
            Vector3.one;

        rect.localRotation =
            Quaternion.identity;
    }

    // ============================================================
    // SERIALIZED REFERENCE
    // ============================================================

    private static void SetObjectReference(
        SerializedObject serializedObject,
        string propertyName,
        Object value
    )
    {
        SerializedProperty property =
            serializedObject.FindProperty(
                propertyName
            );

        if (property == null)
        {
            Debug.LogError(
                "[MainMenuScreenSetup] Missing serialized field: " +
                propertyName
            );

            return;
        }

        property.objectReferenceValue =
            value;
    }

    // ============================================================
    // MAIN MENU BUTTON
    // ============================================================

    private static GameObject CreateButton(
        string objectName,
        string text,
        Transform parent,
        int index
    )
    {
        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );

        Undo.RegisterCreatedObjectUndo(
            obj,
            "Create " + objectName
        );

        obj.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(0f, 1f);

        rect.pivot =
            new Vector2(0f, 1f);

        rect.sizeDelta =
            new Vector2(
                ButtonWidth,
                ButtonHeight
            );

        rect.anchoredPosition =
            new Vector2(
                0f,
                -index *
                (ButtonHeight + ButtonGap)
            );

        Image image =
            obj.GetComponent<Image>();

        image.color =
            MenuButtonColor;

        Button button =
            obj.GetComponent<Button>();

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            MenuButtonColor;

        colors.highlightedColor =
            MenuButtonHoverColor;

        colors.pressedColor =
            new Color(
                0.07f,
                0.08f,
                0.09f,
                1f
            );

        colors.selectedColor =
            MenuButtonHoverColor;

        colors.disabledColor =
            new Color(
                0.2f,
                0.2f,
                0.2f,
                0.5f
            );

        colors.colorMultiplier =
            1f;

        button.colors =
            colors;

        CreateButtonText(
            obj.transform,
            text,
            28f,
            FontStyles.Bold,
            White
        );

        return obj;
    }

    // ============================================================
    // PLAY BUTTON
    // ============================================================

    private static GameObject CreatePlayButton(
        Transform parent
    )
    {
        GameObject obj =
            new GameObject(
                "PlayButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );

        Undo.RegisterCreatedObjectUndo(
            obj,
            "Create PlayButton"
        );

        obj.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(1f, 0f);

        rect.anchorMax =
            new Vector2(1f, 0f);

        rect.pivot =
            new Vector2(1f, 0f);

        rect.sizeDelta =
            new Vector2(
                470f,
                105f
            );

        rect.anchoredPosition =
            new Vector2(
                -80f,
                70f
            );

        Image image =
            obj.GetComponent<Image>();

        image.color =
            PlayButtonColor;

        Button button =
            obj.GetComponent<Button>();

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            PlayButtonColor;

        colors.highlightedColor =
            new Color(
                1f,
                0.72f,
                0.12f,
                1f
            );

        colors.pressedColor =
            new Color(
                0.72f,
                0.45f,
                0.02f,
                1f
            );

        button.colors =
            colors;

        CreateButtonText(
            obj.transform,
            "CHƠI NGAY",
            38f,
            FontStyles.Bold,
            Color.black
        );

        return obj;
    }

    // ============================================================
    // PANEL
    // ============================================================

    private static GameObject CreatePanel(
        string objectName,
        Transform parent,
        string title
    )
    {
        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image)
            );

        Undo.RegisterCreatedObjectUndo(
            obj,
            "Create " + objectName
        );

        // ========================================================
        // QUAN TRỌNG:
        // parent = MainMenuCanvas
        // KHÔNG phải MainMenuScreen.
        // ========================================================

        obj.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        StretchFullScreen(rect);

        Image image =
            obj.GetComponent<Image>();

        image.color =
            new Color(
                0.025f,
                0.03f,
                0.04f,
                0.98f
            );

        CreatePanelTitle(
            obj.transform,
            title
        );

        return obj;
    }

    // ============================================================
    // CLOSE BUTTON
    // ============================================================

    private static GameObject CreateCloseButton(
        Transform parent,
        string objectName
    )
    {
        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );

        Undo.RegisterCreatedObjectUndo(
            obj,
            "Create " + objectName
        );

        obj.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(1f, 1f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.pivot =
            new Vector2(1f, 1f);

        rect.sizeDelta =
            new Vector2(
                120f,
                55f
            );

        rect.anchoredPosition =
            new Vector2(
                -40f,
                -40f
            );

        Image image =
            obj.GetComponent<Image>();

        image.color =
            new Color(
                0.15f,
                0.16f,
                0.18f,
                1f
            );

        Button button =
            obj.GetComponent<Button>();

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            new Color(
                0.15f,
                0.16f,
                0.18f,
                1f
            );

        colors.highlightedColor =
            new Color(
                0.25f,
                0.26f,
                0.28f,
                1f
            );

        button.colors =
            colors;

        CreateButtonText(
            obj.transform,
            "ĐÓNG",
            22f,
            FontStyles.Bold,
            White
        );

        return obj;
    }

    // ============================================================
    // BUTTON TEXT
    // ============================================================

    private static TMP_Text CreateButtonText(
        Transform parent,
        string text,
        float fontSize,
        FontStyles fontStyle,
        Color color
    )
    {
        GameObject obj =
            new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        Undo.RegisterCreatedObjectUndo(
            obj,
            "Create Button Text"
        );

        obj.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        StretchFullScreen(rect);

        rect.offsetMin =
            new Vector2(
                15f,
                5f
            );

        rect.offsetMax =
            new Vector2(
                -15f,
                -5f
            );

        TextMeshProUGUI tmp =
            obj.GetComponent<TextMeshProUGUI>();

        tmp.text =
            text;

        tmp.fontSize =
            fontSize;

        tmp.fontStyle =
            fontStyle;

        tmp.color =
            color;

        tmp.alignment =
            TextAlignmentOptions.Center;

        tmp.enableAutoSizing =
            false;

        tmp.raycastTarget =
            false;

        tmp.overflowMode =
            TextOverflowModes.Overflow;

        return tmp;
    }

    // ============================================================
    // PANEL TITLE
    // ============================================================

    private static void CreatePanelTitle(
        Transform parent,
        string title
    )
    {
        GameObject obj =
            new GameObject(
                "Title",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        Undo.RegisterCreatedObjectUndo(
            obj,
            "Create Panel Title"
        );

        obj.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(
                0.5f,
                1f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                1f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                1f
            );

        rect.sizeDelta =
            new Vector2(
                800f,
                90f
            );

        rect.anchoredPosition =
            new Vector2(
                0f,
                -45f
            );

        TextMeshProUGUI tmp =
            obj.GetComponent<TextMeshProUGUI>();

        tmp.text =
            title;

        tmp.fontSize =
            42f;

        tmp.fontStyle =
            FontStyles.Bold;

        tmp.color =
            White;

        tmp.alignment =
            TextAlignmentOptions.Center;

        tmp.raycastTarget =
            false;
    }

    // ============================================================
    // PERSISTENT UNITY EVENT
    // ============================================================

    private static void AddPersistentListener(
        Button button,
        MainMenuController controller,
        UnityAction action
    )
    {
        if (button == null)
        {
            Debug.LogError(
                "[MainMenuScreenSetup] Button NULL."
            );

            return;
        }

        if (controller == null)
        {
            Debug.LogError(
                "[MainMenuScreenSetup] Controller NULL."
            );

            return;
        }

        if (action == null)
        {
            Debug.LogError(
                "[MainMenuScreenSetup] UnityAction NULL."
            );

            return;
        }

        SerializedObject serializedButton =
            new SerializedObject(button);

        SerializedProperty calls =
            serializedButton.FindProperty(
                "m_OnClick.m_PersistentCalls.m_Calls"
            );

        if (calls != null)
        {
            calls.ClearArray();
        }

        serializedButton.ApplyModifiedProperties();

        UnityEventTools.AddPersistentListener(
            button.onClick,
            action
        );

        EditorUtility.SetDirty(button);
    }
}

#endif
