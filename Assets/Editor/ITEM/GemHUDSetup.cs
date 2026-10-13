#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GemHUDSetup
{
    private const string CanvasName = "GemHUDCanvas";
    private const string PanelName = "GemPanel";
    private const string IconName = "GemIcon";
    private const string TextName = "GemText";


    //==============================================================
    // MENU
    //==============================================================

    [MenuItem("LEAD KHONG PHANH/UI/Create GEM HUD")]
    public static void CreateGemHUD()
    {
        Canvas canvas = FindOrCreateCanvas();

        Transform existingPanel =
            canvas.transform.Find(PanelName);

        GameObject panelObject;

        if (existingPanel != null)
        {
            panelObject = existingPanel.gameObject;
        }
        else
        {
            panelObject = CreatePanel(canvas.transform);
        }


        GemHUD gemHUD =
            panelObject.GetComponent<GemHUD>();

        if (gemHUD == null)
        {
            gemHUD =
                panelObject.AddComponent<GemHUD>();
        }


        Transform iconTransform =
            panelObject.transform.Find(IconName);

        GameObject iconObject;

        if (iconTransform != null)
        {
            iconObject = iconTransform.gameObject;
        }
        else
        {
            iconObject =
                CreateIcon(panelObject.transform);
        }


        Transform textTransform =
            panelObject.transform.Find(TextName);

        GameObject textObject;

        if (textTransform != null)
        {
            textObject = textTransform.gameObject;
        }
        else
        {
            textObject =
                CreateText(panelObject.transform);
        }


        TMP_Text gemText =
            textObject.GetComponent<TMP_Text>();


        //==========================================================
        // GEM HUD REFERENCE
        //==========================================================

        SerializedObject serializedHUD =
            new SerializedObject(gemHUD);

        SerializedProperty textProperty =
            serializedHUD.FindProperty("gemText");

        if (textProperty != null)
        {
            textProperty.objectReferenceValue =
                gemText;
        }

        serializedHUD.ApplyModifiedProperties();


        //==========================================================
        // SELECT
        //==========================================================

        Selection.activeGameObject =
            panelObject;

        EditorUtility.SetDirty(panelObject);

        Debug.Log(
            "[GemHUDSetup] GEM HUD đã được tạo."
        );
    }


    //==============================================================
    // CANVAS
    //==============================================================

    private static Canvas FindOrCreateCanvas()
    {
        GameObject existing =
            GameObject.Find(CanvasName);

        if (existing != null)
        {
            Canvas existingCanvas =
                existing.GetComponent<Canvas>();

            if (existingCanvas != null)
                return existingCanvas;
        }


        GameObject canvasObject =
            new GameObject(
                CanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );


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

        scaler.matchWidthOrHeight =
            0.5f;


        Undo.RegisterCreatedObjectUndo(
            canvasObject,
            "Create GEM HUD Canvas"
        );

        return canvas;
    }


    //==============================================================
    // PANEL
    //==============================================================

    private static GameObject CreatePanel(
        Transform parent)
    {
        GameObject panel =
            new GameObject(
                PanelName,
                typeof(RectTransform),
                typeof(Image),
                typeof(GemHUD)
            );

        Undo.RegisterCreatedObjectUndo(
            panel,
            "Create GEM Panel"
        );

        panel.transform.SetParent(
            parent,
            false
        );


        RectTransform rect =
            panel.GetComponent<RectTransform>();


        //==========================================================
        // POSITION
        //==========================================================

        rect.anchorMin =
            new Vector2(0.5f, 1f);

        rect.anchorMax =
            new Vector2(0.5f, 1f);

        rect.pivot =
            new Vector2(0.5f, 1f);

        rect.anchoredPosition =
            new Vector2(0f, -35f);

        rect.sizeDelta =
            new Vector2(170f, 52f);


        //==========================================================
        // BACKGROUND
        //==========================================================

        Image image =
            panel.GetComponent<Image>();

        image.color =
            new Color(
                0.02f,
                0.02f,
                0.02f,
                0.82f
            );


        return panel;
    }


    //==============================================================
    // ICON
    //==============================================================

    private static GameObject CreateIcon(
        Transform parent)
    {
        GameObject icon =
            new GameObject(
                IconName,
                typeof(RectTransform),
                typeof(Image)
            );

        Undo.RegisterCreatedObjectUndo(
            icon,
            "Create GEM Icon"
        );

        icon.transform.SetParent(
            parent,
            false
        );


        RectTransform rect =
            icon.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 0.5f);

        rect.anchorMax =
            new Vector2(0f, 0.5f);

        rect.pivot =
            new Vector2(0f, 0.5f);

        rect.anchoredPosition =
            new Vector2(12f, 0f);

        rect.sizeDelta =
            new Vector2(34f, 34f);


        Image image =
            icon.GetComponent<Image>();


        // Không gán sprite mặc định.
        // User có thể kéo icon GEM vào Image sau.

        image.color =
            Color.white;


        return icon;
    }


    //==============================================================
    // TEXT
    //==============================================================

    private static GameObject CreateText(
        Transform parent)
    {
        GameObject textObject =
            new GameObject(
                TextName,
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        Undo.RegisterCreatedObjectUndo(
            textObject,
            "Create GEM Text"
        );

        textObject.transform.SetParent(
            parent,
            false
        );


        RectTransform rect =
            textObject.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 0f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.offsetMin =
            new Vector2(55f, 0f);

        rect.offsetMax =
            new Vector2(-10f, 0f);


        TMP_Text text =
            textObject.GetComponent<TMP_Text>();

        text.text =
            "0";

        text.fontSize =
            26f;

        text.fontStyle =
            FontStyles.Bold;

        text.alignment =
            TextAlignmentOptions.MidlineLeft;

        text.textWrappingMode =
        TextWrappingModes.NoWrap;

        text.overflowMode =
            TextOverflowModes.Overflow;


        return textObject;
    }
}

#endif
