#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayItemHUDSetup
{
    // ============================================================
    // ITEM COLORS
    // ============================================================

    // GUM = NEON GREEN
    private static readonly Color GumColor =
        new Color(0.10f, 1.00f, 0.25f, 1.00f);

    // PHOTON = NEON YELLOW
    private static readonly Color PhotonColor =
        new Color(1.00f, 0.88f, 0.00f, 1.00f);

    // SHIELD = NEON BLUE
    private static readonly Color ShieldColor =
        new Color(0.08f, 0.55f, 1.00f, 1.00f);

    // MAGNET = NEON RED
    private static readonly Color MagnetColor =
        new Color(1.00f, 0.10f, 0.14f, 1.00f);


    // ============================================================
    // UI COLORS
    // ============================================================

    private static readonly Color PanelColor =
        new Color(
            0.018f,
            0.022f,
            0.030f,
            0.95f
        );

    // WHITE / GRAY
    private static readonly Color BarBackgroundColor =
        new Color(
            0.68f,
            0.70f,
            0.74f,
            1.00f
        );


    // ============================================================
    // SETUP
    // ============================================================

    [MenuItem(
        "Tools/Lead Khong Phanh/Setup Gameplay Item HUD"
    )]
    public static void Setup()
    {
        Canvas canvas =
            FindOrCreateCanvas();

        // --------------------------------------------------------
        // DELETE OLD HUD
        // --------------------------------------------------------

        Transform old =
            canvas.transform.Find(
                "GameplayItemHUD"
            );

        if (old != null)
        {
            Object.DestroyImmediate(
                old.gameObject
            );
        }


        // ========================================================
        // ROOT
        // ========================================================

        GameObject root =
            CreateUIObject(
                "GameplayItemHUD",
                canvas.transform
            );

        RectTransform rootRect =
            root.GetComponent<RectTransform>();

        rootRect.anchorMin =
            new Vector2(0f, 0f);

        rootRect.anchorMax =
            new Vector2(0f, 0f);

        rootRect.pivot =
            new Vector2(0f, 0f);

        rootRect.anchoredPosition =
            new Vector2(28f, 30f);

        rootRect.sizeDelta =
            new Vector2(330f, 430f);


        // ========================================================
        // CONTAINER
        // ========================================================

        GameObject container =
            CreateUIObject(
                "ItemContainer",
                root.transform
            );

        RectTransform containerRect =
            container.GetComponent<RectTransform>();

        containerRect.anchorMin =
            new Vector2(0f, 0f);

        containerRect.anchorMax =
            new Vector2(0f, 0f);

        containerRect.pivot =
            new Vector2(0f, 0f);

        containerRect.anchoredPosition =
            Vector2.zero;

        containerRect.sizeDelta =
            new Vector2(330f, 420f);


        VerticalLayoutGroup layout =
            container.AddComponent<
                VerticalLayoutGroup
            >();

        layout.spacing = 8f;

        layout.reverseArrangement =
            false;

        layout.childAlignment =
            TextAnchor.LowerLeft;

        layout.childControlWidth =
            true;

        layout.childControlHeight =
            true;

        layout.childForceExpandWidth =
            false;

        layout.childForceExpandHeight =
            false;


        // ========================================================
        // SLOTS
        // ========================================================

        GameObject gum =
            CreateSlot(
                "GumSlot",
                "GUM",
                GumColor,
                container.transform
            );

        GameObject photon =
            CreateSlot(
                "PhotonSlot",
                "PHOTON",
                PhotonColor,
                container.transform
            );

        GameObject shield =
            CreateSlot(
                "ShieldSlot",
                "SHIELD",
                ShieldColor,
                container.transform
            );

        GameObject magnet =
            CreateSlot(
                "MagnetSlot",
                "MAGNET",
                MagnetColor,
                container.transform
            );


        // ========================================================
        // HUD
        // ========================================================

        GameplayItemHUD hud =
            root.AddComponent<
                GameplayItemHUD
            >();

        SerializedObject so =
            new SerializedObject(hud);

        so.FindProperty(
            "gumSlot"
        ).objectReferenceValue = gum;

        so.FindProperty(
            "photonSlot"
        ).objectReferenceValue = photon;

        so.FindProperty(
            "shieldSlot"
        ).objectReferenceValue = shield;

        so.FindProperty(
            "magnetSlot"
        ).objectReferenceValue = magnet;

        so.ApplyModifiedPropertiesWithoutUndo();


        Selection.activeGameObject =
            root;

        EditorUtility.SetDirty(root);

        Debug.Log(
            "[GameplayItemHUDSetup] " +
            "Gameplay Item HUD rebuilt successfully."
        );
    }


    // ============================================================
    // CREATE SLOT
    // ============================================================

    private static GameObject CreateSlot(
        string objectName,
        string label,
        Color accentColor,
        Transform parent
    )
    {
        // ========================================================
        // SLOT
        // ========================================================

        GameObject slot =
            CreateUIObject(
                objectName,
                parent
            );

        RectTransform slotRect =
            slot.GetComponent<RectTransform>();

        slotRect.sizeDelta =
            new Vector2(
                330f,
                76f
            );


        LayoutElement layoutElement =
            slot.AddComponent<
                LayoutElement
            >();

        layoutElement.minWidth =
            330f;

        layoutElement.preferredWidth =
            330f;

        layoutElement.minHeight =
            76f;

        layoutElement.preferredHeight =
            76f;


        // ========================================================
        // PANEL
        // ========================================================

        Image panel =
            slot.AddComponent<Image>();

        panel.color =
            PanelColor;

        panel.raycastTarget =
            false;


        // ========================================================
        // ACCENT
        // ========================================================

        GameObject accent =
            CreateUIObject(
                "Accent",
                slot.transform
            );

        RectTransform accentRect =
            accent.GetComponent<RectTransform>();

        accentRect.anchorMin =
            new Vector2(0f, 0f);

        accentRect.anchorMax =
            new Vector2(0f, 1f);

        accentRect.pivot =
            new Vector2(0f, 0.5f);

        accentRect.anchoredPosition =
            Vector2.zero;

        accentRect.sizeDelta =
            new Vector2(5f, 0f);


        Image accentImage =
            accent.AddComponent<Image>();

        accentImage.color =
            accentColor;

        accentImage.raycastTarget =
            false;


        // ========================================================
        // ICON BACKGROUND
        // ========================================================

        GameObject iconBackground =
            CreateUIObject(
                "IconBackground",
                slot.transform
            );

        RectTransform iconBGRect =
            iconBackground.GetComponent<RectTransform>();

        iconBGRect.anchorMin =
            new Vector2(0f, 0.5f);

        iconBGRect.anchorMax =
            new Vector2(0f, 0.5f);

        iconBGRect.pivot =
            new Vector2(0f, 0.5f);

        iconBGRect.anchoredPosition =
            new Vector2(
                17f,
                0f
            );

        iconBGRect.sizeDelta =
            new Vector2(
                50f,
                50f
            );


        Image iconBGImage =
            iconBackground.AddComponent<Image>();

        iconBGImage.color =
            new Color(
                accentColor.r,
                accentColor.g,
                accentColor.b,
                0.16f
            );

        iconBGImage.raycastTarget =
            false;


        Outline iconOutline =
            iconBackground.AddComponent<Outline>();

        iconOutline.effectColor =
            new Color(
                accentColor.r,
                accentColor.g,
                accentColor.b,
                0.70f
            );

        iconOutline.effectDistance =
            new Vector2(
                1.5f,
                1.5f
            );

        iconOutline.useGraphicAlpha =
            false;


        // ========================================================
        // ICON - IMAGE
        // ========================================================

        GameObject icon =
            CreateUIObject(
                "Icon",
                iconBackground.transform
            );

        RectTransform iconRect =
            icon.GetComponent<RectTransform>();

        iconRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        iconRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        iconRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        iconRect.anchoredPosition =
            Vector2.zero;

        iconRect.sizeDelta =
            new Vector2(
                36f,
                36f
            );


        // ========================================================
        // IMAGE COMPONENT
        // ========================================================

        Image iconImage =
            icon.AddComponent<Image>();

        iconImage.sprite =
            null;

        iconImage.type =
            Image.Type.Simple;

        iconImage.preserveAspect =
            true;

        iconImage.raycastTarget =
            false;

        /*
         * ========================================================
         * ICON COLOR
         * ========================================================
         *
         * Để WHITE để Sprite giữ nguyên màu gốc.
         *
         * Sau này bạn chỉ cần kéo Sprite vào:
         *
         * GumSlot
         *   -> IconBackground
         *      -> Icon
         *
         * Inspector:
         * Image -> Source Image
         */

        iconImage.color =
            Color.white;


        // ========================================================
        // NAME
        // ========================================================

        GameObject nameObject =
            CreateUIObject(
                "Name",
                slot.transform
            );

        RectTransform nameRect =
            nameObject.GetComponent<RectTransform>();

        nameRect.anchorMin =
            new Vector2(
                0f,
                0.5f
            );

        nameRect.anchorMax =
            new Vector2(
                0f,
                0.5f
            );

        nameRect.pivot =
            new Vector2(
                0f,
                0.5f
            );

        nameRect.anchoredPosition =
            new Vector2(
                82f,
                13f
            );

        nameRect.sizeDelta =
            new Vector2(
                130f,
                23f
            );


        TMP_Text nameText =
            nameObject.AddComponent<
                TextMeshProUGUI
            >();

        nameText.text =
            label;

        nameText.fontSize =
            15f;

        nameText.fontStyle =
            FontStyles.Bold;

        nameText.alignment =
            TextAlignmentOptions.Left;

        nameText.color =
            Color.white;

        nameText.textWrappingMode =
            TextWrappingModes.NoWrap;

        nameText.raycastTarget =
            false;


        // ========================================================
        // TIME
        // ========================================================

        GameObject timeObject =
            CreateUIObject(
                "Time",
                slot.transform
            );

        RectTransform timeRect =
            timeObject.GetComponent<RectTransform>();

        timeRect.anchorMin =
            new Vector2(
                1f,
                0.5f
            );

        timeRect.anchorMax =
            new Vector2(
                1f,
                0.5f
            );

        timeRect.pivot =
            new Vector2(
                1f,
                0.5f
            );

        timeRect.anchoredPosition =
            new Vector2(
                -14f,
                13f
            );

        timeRect.sizeDelta =
            new Vector2(
                55f,
                23f
            );


        TMP_Text timeText =
            timeObject.AddComponent<
                TextMeshProUGUI
            >();

        timeText.text =
            "0.0s";

        timeText.fontSize =
            13f;

        timeText.fontStyle =
            FontStyles.Bold;

        timeText.alignment =
            TextAlignmentOptions.Right;

        timeText.color =
            Color.white;

        timeText.textWrappingMode =
            TextWrappingModes.NoWrap;

        timeText.raycastTarget =
            false;


        // ========================================================
        // BAR BACKGROUND
        // ========================================================

        GameObject bar =
            CreateUIObject(
                "BarBackground",
                slot.transform
            );

        RectTransform barRect =
            bar.GetComponent<RectTransform>();

        barRect.anchorMin =
            new Vector2(
                0f,
                0f
            );

        barRect.anchorMax =
            new Vector2(
                1f,
                0f
            );

        barRect.pivot =
            new Vector2(
                0.5f,
                0f
            );

        barRect.offsetMin =
            new Vector2(
                82f,
                7f
            );

        barRect.offsetMax =
            new Vector2(
                -14f,
                21f
            );


        Image barImage =
            bar.AddComponent<Image>();

        // ========================================================
        // BAR BACKGROUND = WHITE / GRAY
        // ========================================================

        barImage.color =
            BarBackgroundColor;

        barImage.raycastTarget =
            false;


        // ========================================================
        // BAR OUTLINE
        // ========================================================

        Outline barOutline =
            bar.AddComponent<Outline>();

        barOutline.effectColor =
            new Color(
                1f,
                1f,
                1f,
                0.75f
            );

        barOutline.effectDistance =
            new Vector2(
                1f,
                1f
            );

        barOutline.useGraphicAlpha =
            false;


        // ========================================================
        // FILL
        // ========================================================

        GameObject fill =
            CreateUIObject(
                "Fill",
                bar.transform
            );

        RectTransform fillRect =
            fill.GetComponent<RectTransform>();

        fillRect.anchorMin =
            new Vector2(
                0f,
                0f
            );

        fillRect.anchorMax =
            new Vector2(
                0f,
                1f
            );

        fillRect.pivot =
            new Vector2(
                0f,
                0.5f
            );

        fillRect.anchoredPosition =
            Vector2.zero;

        fillRect.sizeDelta =
            Vector2.zero;


        // ========================================================
        // FILL IMAGE
        // ========================================================

        Image fillImage =
            fill.AddComponent<Image>();

        /*
         * QUAN TRỌNG:
         *
         * Fill dùng CHÍNH XÁC accentColor.
         *
         * Không Lerp.
         * Không trắng hóa.
         * Không đổi màu runtime ở Setup.
         */

        fillImage.color =
            accentColor;

        fillImage.type =
            Image.Type.Simple;

        fillImage.preserveAspect =
            false;

        fillImage.raycastTarget =
            false;


        // ========================================================
        // FILL GLOW
        // ========================================================

        Shadow fillGlow =
            fill.AddComponent<Shadow>();

        fillGlow.effectColor =
            new Color(
                accentColor.r,
                accentColor.g,
                accentColor.b,
                0.35f
            );

        fillGlow.effectDistance =
            new Vector2(
                3f,
                0f
            );

        fillGlow.useGraphicAlpha =
            false;


        // ========================================================
        // ORDER
        // ========================================================

        fill.transform.SetAsLastSibling();


        return slot;
    }


    // ============================================================
    // CREATE UI OBJECT
    // ============================================================

    private static GameObject CreateUIObject(
        string objectName,
        Transform parent
    )
    {
        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform)
            );

        obj.transform.SetParent(
            parent,
            false
        );

        return obj;
    }


    // ============================================================
    // CANVAS
    // ============================================================

    private static Canvas FindOrCreateCanvas()
    {
        Canvas canvas =
            Object.FindFirstObjectByType<Canvas>();

        if (canvas != null)
            return canvas;


        GameObject canvasObject =
            new GameObject(
                "GameplayCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );


        canvas =
            canvasObject.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;


        CanvasScaler scaler =
            canvasObject.GetComponent<
                CanvasScaler
            >();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode
                .ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(
                1920f,
                1080f
            );

        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode
                .MatchWidthOrHeight;

        scaler.matchWidthOrHeight =
            0.5f;


        return canvas;
    }
}

#endif