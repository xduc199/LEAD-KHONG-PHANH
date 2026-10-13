#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class LuckyWheelPanelSetup
{
    private const string CanvasName = "LuckyWheelCanvas";

    // =====================================================================
    // ENTRY POINT
    // =====================================================================

    [MenuItem("LEAD KHONG PHANH/Lucky Wheel/Create UI Skeleton")]
    public static void Create()
    {
        Canvas canvas = FindOrCreateCanvas();

        GameObject root = GetOrCreate(
            "LuckyWheelUIController",
            canvas.transform);

        LuckyWheelUIController controller =
            root.GetComponent<LuckyWheelUIController>();

        if (controller == null)
            controller = root.AddComponent<LuckyWheelUIController>();

        // -------------------------------------------------------------
        // Main open button
        // -------------------------------------------------------------

        CreateLuckyWheelOpenButton(canvas.transform);

        // -------------------------------------------------------------
        // Lucky Wheel menu
        // -------------------------------------------------------------

        CreateLuckyWheelMenu(root.transform);

        // -------------------------------------------------------------
        // Daily
        // -------------------------------------------------------------

        CreateDailyPanel(root.transform);

        // -------------------------------------------------------------
        // VIP
        // -------------------------------------------------------------

        CreateVipPanel(root.transform);

        Selection.activeGameObject = root;

        EditorUtility.SetDirty(root);

        EditorUtility.DisplayDialog(
            "Lucky Wheel",
            "Đã tạo/cập nhật Lucky Wheel UI.\n\n" +
            "Có thêm nút LUCKY WHEEL để mở menu Daily / VIP.",
            "OK");
    }

    // =====================================================================
    // CANVAS
    // =====================================================================

    private static Canvas FindOrCreateCanvas()
    {
        GameObject existing = GameObject.Find(CanvasName);

        if (existing != null)
        {
            Canvas existingCanvas =
                existing.GetComponent<Canvas>();

            if (existingCanvas != null)
                return existingCanvas;
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
            new Vector2(1080f, 1920f);

        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    // =====================================================================
    // MAIN OPEN BUTTON
    // =====================================================================

    private static void CreateLuckyWheelOpenButton(
        Transform parent)
    {
        GameObject buttonObject = GetOrCreate(
            "LuckyWheelOpenButton",
            parent,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));

        RectTransform rect =
            buttonObject.GetComponent<RectTransform>();

        // Top-right
        rect.anchorMin =
            new Vector2(1f, 1f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.pivot =
            new Vector2(1f, 1f);

        rect.anchoredPosition =
            new Vector2(-35f, -35f);

        rect.sizeDelta =
            new Vector2(260f, 90f);

        Image image =
            buttonObject.GetComponent<Image>();

        image.sprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");

        image.type =
            Image.Type.Sliced;

        Button button =
            buttonObject.GetComponent<Button>();

        button.transition =
            Selectable.Transition.ColorTint;

        CreateOrUpdateButtonLabel(
            buttonObject.transform,
            "🎡 LUCKY WHEEL",
            34);

        // -------------------------------------------------------------
        // Hook open action through UnityEvent
        // -------------------------------------------------------------

        button.onClick.RemoveAllListeners();

        GameObject controllerObject =
            GameObject.Find("LuckyWheelUIController");

        if (controllerObject != null)
        {
            LuckyWheelUIController controller =
                controllerObject.GetComponent<LuckyWheelUIController>();

            if (controller != null)
            {
                UnityEngine.Events.UnityAction action =
                    controller.OpenLuckyWheelMenu;

                button.onClick.AddListener(action);
            }
        }
    }

    // =====================================================================
    // LUCKY WHEEL MENU
    // =====================================================================

    private static void CreateLuckyWheelMenu(
        Transform parent)
    {
        GameObject menu =
            GetOrCreate(
                "LuckyWheelMenuPanel",
                parent,
                typeof(RectTransform),
                typeof(Image));

        RectTransform rect =
            menu.GetComponent<RectTransform>();

        Stretch(rect);

        Image background =
            menu.GetComponent<Image>();

        background.sprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");

        background.type =
            Image.Type.Sliced;

        background.color =
            new Color(0f, 0f, 0f, 0.92f);

        // -------------------------------------------------------------
        // Title
        // -------------------------------------------------------------

        GameObject title =
            CreateText(
                "LuckyWheelMenuTitle",
                menu.transform,
                "🎡 LUCKY WHEEL",
                58);

        RectTransform titleRect =
            title.GetComponent<RectTransform>();

        titleRect.anchorMin =
            new Vector2(0.5f, 1f);

        titleRect.anchorMax =
            new Vector2(0.5f, 1f);

        titleRect.pivot =
            new Vector2(0.5f, 1f);

        titleRect.anchoredPosition =
            new Vector2(0f, -180f);

        titleRect.sizeDelta =
            new Vector2(800f, 100f);

        // -------------------------------------------------------------
        // Daily button
        // -------------------------------------------------------------

        GameObject dailyButton =
            CreateMenuButton(
                "DailyOpenButton",
                menu.transform,
                "🎁 DAILY WHEEL",
                0f,
                80f);

        // -------------------------------------------------------------
        // VIP button
        // -------------------------------------------------------------

        GameObject vipButton =
            CreateMenuButton(
                "VIPOpenButton",
                menu.transform,
                "💎 VIP WHEEL",
                0f,
                -80f);

        // -------------------------------------------------------------
        // Close button
        // -------------------------------------------------------------

        GameObject closeButton =
            CreateMenuButton(
                "LuckyWheelMenuCloseButton",
                menu.transform,
                "CLOSE",
                0f,
                -260f);

        // -------------------------------------------------------------
        // Button events
        // -------------------------------------------------------------

        GameObject controllerObject =
            GameObject.Find("LuckyWheelUIController");

        if (controllerObject != null)
        {
            LuckyWheelUIController controller =
                controllerObject.GetComponent<LuckyWheelUIController>();

            if (controller != null)
            {
                Button daily =
                    dailyButton.GetComponent<Button>();

                Button vip =
                    vipButton.GetComponent<Button>();

                Button close =
                    closeButton.GetComponent<Button>();

                daily.onClick.RemoveAllListeners();
                vip.onClick.RemoveAllListeners();
                close.onClick.RemoveAllListeners();

                daily.onClick.AddListener(
                    controller.OpenDaily);

                vip.onClick.AddListener(
                    controller.OpenVip);

                close.onClick.AddListener(
                    controller.CloseLuckyWheelMenu);
            }
        }

        // Menu phải tồn tại nhưng mặc định đóng.
        menu.SetActive(false);
    }

    // =====================================================================
    // MENU BUTTON
    // =====================================================================

    private static GameObject CreateMenuButton(
        string objectName,
        Transform parent,
        string label,
        float x,
        float y)
    {
        GameObject obj =
            GetOrCreate(
                objectName,
                parent,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.anchoredPosition =
            new Vector2(x, y);

        rect.sizeDelta =
            new Vector2(520f, 100f);

        Image image =
            obj.GetComponent<Image>();

        image.sprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");

        image.type =
            Image.Type.Sliced;

        Button button =
            obj.GetComponent<Button>();

        button.transition =
            Selectable.Transition.ColorTint;

        CreateOrUpdateButtonLabel(
            obj.transform,
            label,
            32);

        return obj;
    }

    // =====================================================================
    // DAILY PANEL
    // =====================================================================

    private static void CreateDailyPanel(
        Transform parent)
    {
        GameObject panel =
            GetOrCreate(
                "DailyWheelPanel",
                parent,
                typeof(RectTransform),
                typeof(Image));

        RectTransform rect =
            panel.GetComponent<RectTransform>();

        Stretch(rect);

        Image background =
            panel.GetComponent<Image>();

        background.sprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");

        background.type =
            Image.Type.Sliced;

        background.color =
            new Color(0.04f, 0.04f, 0.04f, 0.98f);

        // -------------------------------------------------------------
        // Title
        // -------------------------------------------------------------

        CreateText(
            "DailyTitleText",
            panel.transform,
            "🎁 DAILY LUCKY WHEEL",
            48);

        // -------------------------------------------------------------
        // Ticket
        // -------------------------------------------------------------

        CreateText(
            "DailyTicketText",
            panel.transform,
            "Lượt quay: 0",
            32);

        // -------------------------------------------------------------
        // Reward
        // -------------------------------------------------------------

        CreateText(
            "DailyRewardText",
            panel.transform,
            "🎁 Chưa quay",
            30);

        // -------------------------------------------------------------
        // Wheel
        // -------------------------------------------------------------

        CreateWheel(
            "DailyWheel",
            panel.transform,
            12);

        // -------------------------------------------------------------
        // Pointer
        // -------------------------------------------------------------

        CreatePointer(
            "DailyPointer",
            panel.transform);

        // -------------------------------------------------------------
        // Center
        // -------------------------------------------------------------

        CreateText(
            "DailyCenterText",
            panel.transform,
            "DAILY",
            28);

        // -------------------------------------------------------------
        // Buttons
        // -------------------------------------------------------------

        CreateButton(
            "DailySpinButton",
            panel.transform,
            "SPIN");

        CreateButton(
            "DailyCloseButton",
            panel.transform,
            "CLOSE");
    }

    // =====================================================================
    // VIP PANEL
    // =====================================================================

    private static void CreateVipPanel(
        Transform parent)
    {
        GameObject panel =
            GetOrCreate(
                "VIPWheelPanel",
                parent,
                typeof(RectTransform),
                typeof(Image));

        RectTransform rect =
            panel.GetComponent<RectTransform>();

        Stretch(rect);

        Image background =
            panel.GetComponent<Image>();

        background.sprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");

        background.type =
            Image.Type.Sliced;

        background.color =
            new Color(0.04f, 0.04f, 0.08f, 0.98f);

        // -------------------------------------------------------------
        // Title
        // -------------------------------------------------------------

        CreateText(
            "VIPTitleText",
            panel.transform,
            "💎 VIP LUCKY WHEEL",
            48);

        // -------------------------------------------------------------
        // Gem
        // -------------------------------------------------------------

        CreateText(
            "VIPGemText",
            panel.transform,
            "💎 0 Gem",
            32);

        // -------------------------------------------------------------
        // Reward
        // -------------------------------------------------------------

        CreateText(
            "VIPRewardText",
            panel.transform,
            "🎁 Chưa quay",
            30);

        // -------------------------------------------------------------
        // Wheel
        // -------------------------------------------------------------

        CreateWheel(
            "VIPWheel",
            panel.transform,
            15);

        // -------------------------------------------------------------
        // Pointer
        // -------------------------------------------------------------

        CreatePointer(
            "VIPPointer",
            panel.transform);

        // -------------------------------------------------------------
        // Center
        // -------------------------------------------------------------

        CreateText(
            "VIPCenterText",
            panel.transform,
            "VIP",
            28);

        // -------------------------------------------------------------
        // Buttons
        // -------------------------------------------------------------

        CreateButton(
            "VIPSpinButton",
            panel.transform,
            "SPIN - 500 GEM");

        CreateButton(
            "VIPCloseButton",
            panel.transform,
            "CLOSE");
    }

    // =====================================================================
    // WHEEL
    // =====================================================================

    private static GameObject CreateWheel(
        string objectName,
        Transform parent,
        int sectorCount)
    {
        GameObject wheel =
            GetOrCreate(
                objectName,
                parent,
                typeof(RectTransform));

        RectTransform wheelRect =
            wheel.GetComponent<RectTransform>();

        wheelRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        wheelRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        wheelRect.pivot =
            new Vector2(0.5f, 0.5f);

        wheelRect.anchoredPosition =
            new Vector2(0f, 80f);

        wheelRect.sizeDelta =
            new Vector2(700f, 700f);

        // -------------------------------------------------------------
        // Remove obsolete generated sectors.
        // -------------------------------------------------------------

        for (int i = wheel.transform.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                wheel.transform.GetChild(i);

            if (child.name.StartsWith("Sector_"))
                Object.DestroyImmediate(
                    child.gameObject);
        }

        // -------------------------------------------------------------
        // Create sectors
        // -------------------------------------------------------------

        for (int i = 0;
             i < sectorCount;
             i++)
        {
            CreateWheelSector(
                wheel.transform,
                i,
                sectorCount);
        }

        return wheel;
    }

    // =====================================================================
    // WHEEL SECTOR
    // =====================================================================

    private static void CreateWheelSector(
        Transform parent,
        int index,
        int sectorCount)
    {
        GameObject sector =
            new GameObject(
                "Sector_" + index,
                typeof(RectTransform),
                typeof(Image));

        sector.transform.SetParent(
            parent,
            false);

        RectTransform rect =
            sector.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.sizeDelta =
            new Vector2(320f, 320f);

        rect.anchoredPosition =
            Vector2.zero;

        float sectorAngle =
            360f / sectorCount;

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                -index * sectorAngle);

        Image image =
            sector.GetComponent<Image>();

        image.sprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");

        image.type =
            Image.Type.Filled;

        image.fillMethod =
            Image.FillMethod.Radial360;

        image.fillOrigin = 0;

        image.fillClockwise = true;

        image.fillAmount =
            1f / sectorCount;

        image.raycastTarget = false;

        // -------------------------------------------------------------
        // Label
        // -------------------------------------------------------------

        GameObject label =
            CreateText(
                "Label",
                sector.transform,
                (index + 1).ToString(),
                20);

        RectTransform labelRect =
            label.GetComponent<RectTransform>();

        labelRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        labelRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        labelRect.pivot =
            new Vector2(0.5f, 0.5f);

        labelRect.anchoredPosition =
            new Vector2(0f, 115f);

        labelRect.sizeDelta =
            new Vector2(180f, 60f);

        TMP_Text text =
            label.GetComponent<TMP_Text>();

        text.raycastTarget = false;
    }

    // =====================================================================
    // POINTER
    // =====================================================================

    private static GameObject CreatePointer(
        string objectName,
        Transform parent)
    {
        GameObject pointer =
            GetOrCreate(
                objectName,
                parent,
                typeof(RectTransform),
                typeof(Image));

        RectTransform rect =
            pointer.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0f);

        rect.anchoredPosition =
            new Vector2(0f, 420f);

        rect.sizeDelta =
            new Vector2(55f, 90f);

        Image image =
            pointer.GetComponent<Image>();

        image.sprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");

        image.type =
            Image.Type.Simple;

        image.raycastTarget = false;

        return pointer;
    }

    // =====================================================================
    // TEXT
    // =====================================================================

    private static GameObject CreateText(
        string objectName,
        Transform parent,
        string value,
        float fontSize)
    {
        GameObject obj =
            GetOrCreate(
                objectName,
                parent,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

        TMP_Text text =
            obj.GetComponent<TMP_Text>();

        text.text = value;
        text.fontSize = fontSize;
        text.alignment =
            TextAlignmentOptions.Center;

        text.raycastTarget = false;

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        if (rect.sizeDelta == Vector2.zero)
            rect.sizeDelta =
                new Vector2(700f, 80f);

        return obj;
    }

    // =====================================================================
    // BUTTON
    // =====================================================================

    private static GameObject CreateButton(
        string objectName,
        Transform parent,
        string label)
    {
        GameObject obj =
            GetOrCreate(
                objectName,
                parent,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0f);

        rect.anchorMax =
            new Vector2(0.5f, 0f);

        rect.pivot =
            new Vector2(0.5f, 0f);

        if (objectName.Contains("Close"))
        {
            rect.anchoredPosition =
                new Vector2(0f, 70f);

            rect.sizeDelta =
                new Vector2(300f, 80f);
        }
        else
        {
            rect.anchoredPosition =
                new Vector2(0f, 180f);

            rect.sizeDelta =
                new Vector2(400f, 100f);
        }

        Image image =
            obj.GetComponent<Image>();

        image.sprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");

        image.type =
            Image.Type.Sliced;

        Button button =
            obj.GetComponent<Button>();

        button.transition =
            Selectable.Transition.ColorTint;

        CreateOrUpdateButtonLabel(
            obj.transform,
            label,
            30);

        return obj;
    }

    // =====================================================================
    // BUTTON LABEL
    // =====================================================================

    private static void CreateOrUpdateButtonLabel(
        Transform parent,
        string value,
        float fontSize)
    {
        Transform existing =
            parent.Find("Label");

        GameObject labelObject;

        if (existing == null)
        {
            labelObject =
                new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));

            labelObject.transform.SetParent(
                parent,
                false);
        }
        else
        {
            labelObject =
                existing.gameObject;
        }

        RectTransform rect =
            labelObject.GetComponent<RectTransform>();

        Stretch(rect);

        TMP_Text text =
            labelObject.GetComponent<TMP_Text>();

        text.text = value;
        text.fontSize = fontSize;
        text.alignment =
            TextAlignmentOptions.Center;

        text.raycastTarget = false;
    }

    // =====================================================================
    // GET OR CREATE
    // =====================================================================

    private static GameObject GetOrCreate(
        string objectName,
        Transform parent,
        params System.Type[] components)
    {
        Transform existing =
            parent.Find(objectName);

        if (existing != null)
            return existing.gameObject;

        GameObject obj =
            new GameObject(
                objectName,
                components);

        obj.transform.SetParent(
            parent,
            false);

        return obj;
    }

    // =====================================================================
    // STRETCH
    // =====================================================================

    private static void Stretch(
        RectTransform rect)
    {
        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }
}

#endif