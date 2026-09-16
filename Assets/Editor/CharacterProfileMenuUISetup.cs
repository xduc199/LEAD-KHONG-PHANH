#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CharacterProfileMenuUISetup
{
    private const string CanvasName = "MainMenuCanvas";
    private const string SafeAreaName = "SafeArea";
    private const string MiniPanelName = "CharacterMiniPanel";

    // ============================================================
    // COLORS
    // ============================================================

    private static readonly Color DarkPanel =
        new Color(0.035f, 0.045f, 0.055f, 0.96f);

    private static readonly Color DarkSurface =
        new Color(0.065f, 0.080f, 0.090f, 1f);

    private static readonly Color Gold =
        new Color(1f, 0.78f, 0.16f, 1f);

    private static readonly Color GoldDark =
        new Color(0.55f, 0.38f, 0.04f, 1f);

    private static readonly Color Cyan =
        new Color(0.10f, 0.85f, 1f, 1f);

    private static readonly Color White =
        new Color(0.95f, 0.97f, 0.98f, 1f);

    private static readonly Color Gray =
        new Color(0.58f, 0.63f, 0.67f, 1f);

    private static readonly Color Black =
        new Color(0.015f, 0.02f, 0.025f, 1f);

    // ============================================================
    // MENU
    // ============================================================

    [MenuItem("LEAD KHÔNG PHANH/UI/Create Character Mini Profile")]
    public static void CreateCharacterMiniProfile()
    {
        Canvas canvas = FindCanvas();

        if (canvas == null)
        {
            Debug.LogError(
                "[CharacterProfileMenuUISetup] " +
                "Không tìm thấy MainMenuCanvas."
            );

            return;
        }

        Transform safeArea =
            FindChildRecursive(
                canvas.transform,
                SafeAreaName
            );

        if (safeArea == null)
        {
            Debug.LogError(
                "[CharacterProfileMenuUISetup] " +
                "Không tìm thấy SafeArea trong MainMenuCanvas."
            );

            return;
        }

        // --------------------------------------------------------
        // XÓA MINI PANEL CŨ
        // --------------------------------------------------------

        Transform oldMini =
            safeArea.Find(MiniPanelName);

        if (oldMini != null)
        {
            Object.DestroyImmediate(
                oldMini.gameObject
            );
        }

        // --------------------------------------------------------
        // ROOT
        // --------------------------------------------------------

        GameObject miniPanel =
            CreateUIObject(
                MiniPanelName,
                safeArea
            );

        RectTransform miniRect =
            miniPanel.GetComponent<RectTransform>();

        miniRect.anchorMin =
            new Vector2(0f, 1f);

        miniRect.anchorMax =
            new Vector2(0f, 1f);

        miniRect.pivot =
            new Vector2(0f, 1f);

        miniRect.anchoredPosition =
            new Vector2(45f, -45f);

        miniRect.sizeDelta =
            new Vector2(620f, 155f);

        // --------------------------------------------------------
        // PANEL BACKGROUND
        // --------------------------------------------------------

        Image panelImage =
            miniPanel.AddComponent<Image>();

        panelImage.color =
            DarkPanel;

        // --------------------------------------------------------
        // BORDER
        // --------------------------------------------------------

        GameObject border =
            CreateUIObject(
                "Border",
                miniPanel.transform
            );

        RectTransform borderRect =
            border.GetComponent<RectTransform>();

        Stretch(borderRect);

        Image borderImage =
            border.AddComponent<Image>();

        borderImage.color =
            GoldDark;

        border.transform.SetAsFirstSibling();

        // --------------------------------------------------------
        // INNER SURFACE
        // --------------------------------------------------------

        GameObject surface =
            CreateUIObject(
                "Surface",
                miniPanel.transform
            );

        RectTransform surfaceRect =
            surface.GetComponent<RectTransform>();

        surfaceRect.anchorMin =
            new Vector2(0f, 0f);

        surfaceRect.anchorMax =
            new Vector2(1f, 1f);

        surfaceRect.offsetMin =
            new Vector2(2f, 2f);

        surfaceRect.offsetMax =
            new Vector2(-2f, -2f);

        Image surfaceImage =
            surface.AddComponent<Image>();

        surfaceImage.color =
            DarkSurface;

        // Để surface không che các object phía trên
        surface.transform.SetAsLastSibling();

        // --------------------------------------------------------
        // GOLD TOP LINE
        // --------------------------------------------------------

        GameObject topLine =
            CreateUIObject(
                "GoldAccent",
                miniPanel.transform
            );

        RectTransform topLineRect =
            topLine.GetComponent<RectTransform>();

        topLineRect.anchorMin =
            new Vector2(0f, 1f);

        topLineRect.anchorMax =
            new Vector2(1f, 1f);

        topLineRect.pivot =
            new Vector2(0.5f, 1f);

        topLineRect.anchoredPosition =
            Vector2.zero;

        topLineRect.sizeDelta =
            new Vector2(0f, 5f);

        Image topLineImage =
            topLine.AddComponent<Image>();

        topLineImage.color =
            Gold;

        // ========================================================
        // CHARACTER AREA
        // ========================================================

        GameObject characterArea =
            CreateUIObject(
                "CharacterArea",
                miniPanel.transform
            );

        RectTransform characterRect =
            characterArea.GetComponent<RectTransform>();

        characterRect.anchorMin =
            new Vector2(0f, 0f);

        characterRect.anchorMax =
            new Vector2(0.70f, 1f);

        characterRect.offsetMin =
            new Vector2(12f, 12f);

        characterRect.offsetMax =
            new Vector2(-8f, -12f);

        // --------------------------------------------------------
        // AVATAR BUTTON
        // --------------------------------------------------------

        GameObject avatarButton =
            CreateUIObject(
                "AvatarButton",
                characterArea.transform
            );

        RectTransform avatarRect =
            avatarButton.GetComponent<RectTransform>();

        avatarRect.anchorMin =
            new Vector2(0f, 0.5f);

        avatarRect.anchorMax =
            new Vector2(0f, 0.5f);

        avatarRect.pivot =
            new Vector2(0f, 0.5f);

        avatarRect.anchoredPosition =
            new Vector2(0f, 0f);

        avatarRect.sizeDelta =
            new Vector2(115f, 115f);

        Image avatarImage =
            avatarButton.AddComponent<Image>();

        avatarImage.color =
            Black;

        Button avatarButtonComponent =
            avatarButton.AddComponent<Button>();

        ColorBlock colors =
            avatarButtonComponent.colors;

        colors.normalColor =
            Black;

        colors.highlightedColor =
            new Color(
                0.12f,
                0.14f,
                0.15f,
                1f
            );

        colors.pressedColor =
            new Color(
                0.18f,
                0.20f,
                0.21f,
                1f
            );

        colors.selectedColor =
            colors.highlightedColor;

        avatarButtonComponent.colors =
            colors;

        // --------------------------------------------------------
        // AVATAR BORDER
        // --------------------------------------------------------

        GameObject avatarBorder =
            CreateUIObject(
                "AvatarBorder",
                avatarButton.transform
            );

        RectTransform avatarBorderRect =
            avatarBorder.GetComponent<RectTransform>();

        Stretch(avatarBorderRect);

        Image avatarBorderImage =
            avatarBorder.AddComponent<Image>();

        avatarBorderImage.color =
            Gold;

        avatarBorderImage.raycastTarget =
            false;

        // --------------------------------------------------------
        // AVATAR TEXT
        // --------------------------------------------------------

        TMP_Text avatarText =
            CreateTMP(
                "Avatar",
                avatarButton.transform,
                "LEAD",
                25,
                FontStyles.Bold,
                White
            );

        RectTransform avatarTextRect =
            avatarText.rectTransform;

        Stretch(
            avatarTextRect,
            new Vector2(5f, 5f),
            new Vector2(-5f, -5f)
        );

        avatarText.alignment =
            TextAlignmentOptions.Center;

        avatarText.raycastTarget =
            false;

        // --------------------------------------------------------
        // AVATAR SUBTEXT
        // --------------------------------------------------------

        TMP_Text avatarSub =
            CreateTMP(
                "AvatarHint",
                avatarButton.transform,
                "PROFILE",
                12,
                FontStyles.Bold,
                Gold
            );

        RectTransform avatarSubRect =
            avatarSub.rectTransform;

        avatarSubRect.anchorMin =
            new Vector2(0f, 0f);

        avatarSubRect.anchorMax =
            new Vector2(1f, 0f);

        avatarSubRect.pivot =
            new Vector2(0.5f, 0f);

        avatarSubRect.anchoredPosition =
            new Vector2(0f, 8f);

        avatarSubRect.sizeDelta =
            new Vector2(0f, 22f);

        avatarSub.alignment =
            TextAlignmentOptions.Center;

        avatarSub.raycastTarget =
            false;

        // ========================================================
        // CHARACTER INFORMATION
        // ========================================================

        GameObject info =
            CreateUIObject(
                "CharacterInfo",
                characterArea.transform
            );

        RectTransform infoRect =
            info.GetComponent<RectTransform>();

        infoRect.anchorMin =
            new Vector2(0f, 0f);

        infoRect.anchorMax =
            new Vector2(1f, 1f);

        infoRect.offsetMin =
            new Vector2(135f, 0f);

        infoRect.offsetMax =
            new Vector2(-5f, 0f);

        // --------------------------------------------------------
        // PLAYER NAME
        // --------------------------------------------------------

        TMP_Text playerName =
            CreateTMP(
                "MiniPlayerName",
                info.transform,
                "NINJA LEAD",
                24,
                FontStyles.Bold,
                White
            );

        RectTransform playerNameRect =
            playerName.rectTransform;

        playerNameRect.anchorMin =
            new Vector2(0f, 1f);

        playerNameRect.anchorMax =
            new Vector2(1f, 1f);

        playerNameRect.pivot =
            new Vector2(0f, 1f);

        playerNameRect.anchoredPosition =
            Vector2.zero;

        playerNameRect.sizeDelta =
            new Vector2(0f, 34f);

        playerName.alignment =
            TextAlignmentOptions.Left;

        // --------------------------------------------------------
        // LEVEL
        // --------------------------------------------------------

        TMP_Text level =
            CreateTMP(
                "MiniLevel",
                info.transform,
                "LV.1",
                18,
                FontStyles.Bold,
                Gold
            );

        RectTransform levelRect =
            level.rectTransform;

        levelRect.anchorMin =
            new Vector2(0f, 1f);

        levelRect.anchorMax =
            new Vector2(1f, 1f);

        levelRect.pivot =
            new Vector2(0f, 1f);

        levelRect.anchoredPosition =
            new Vector2(0f, -36f);

        levelRect.sizeDelta =
            new Vector2(0f, 28f);

        level.alignment =
            TextAlignmentOptions.Left;

        // --------------------------------------------------------
        // EXP TEXT
        // --------------------------------------------------------

        TMP_Text exp =
            CreateTMP(
                "MiniExp",
                info.transform,
                "0 / 100 EXP",
                14,
                FontStyles.Bold,
                Gray
            );

        RectTransform expRect =
            exp.rectTransform;

        expRect.anchorMin =
            new Vector2(0f, 0f);

        expRect.anchorMax =
            new Vector2(1f, 0f);

        expRect.pivot =
            new Vector2(0f, 0f);

        expRect.anchoredPosition =
            new Vector2(0f, 12f);

        expRect.sizeDelta =
            new Vector2(0f, 24f);

        exp.alignment =
            TextAlignmentOptions.Left;

        // --------------------------------------------------------
        // EXP BAR BACKGROUND
        // --------------------------------------------------------

        GameObject expBar =
            CreateUIObject(
                "MiniExpBar",
                info.transform
            );

        RectTransform expBarRect =
            expBar.GetComponent<RectTransform>();

        expBarRect.anchorMin =
            new Vector2(0f, 0f);

        expBarRect.anchorMax =
            new Vector2(1f, 0f);

        expBarRect.pivot =
            new Vector2(0f, 0f);

        expBarRect.anchoredPosition =
            new Vector2(0f, 0f);

        expBarRect.sizeDelta =
            new Vector2(0f, 9f);

        Image expBarImage =
            expBar.AddComponent<Image>();

        expBarImage.color =
            new Color(
                0.025f,
                0.035f,
                0.04f,
                1f
            );

        // --------------------------------------------------------
        // EXP FILL
        // --------------------------------------------------------

        GameObject expFill =
            CreateUIObject(
                "MiniExpFill",
                expBar.transform
            );

        RectTransform expFillRect =
            expFill.GetComponent<RectTransform>();

        expFillRect.anchorMin =
            new Vector2(0f, 0f);

        expFillRect.anchorMax =
            new Vector2(0f, 1f);

        expFillRect.pivot =
            new Vector2(0f, 0.5f);

        expFillRect.anchoredPosition =
            Vector2.zero;

        expFillRect.sizeDelta =
            new Vector2(
                120f,
                0f
            );

        Image expFillImage =
            expFill.AddComponent<Image>();

        expFillImage.color =
            Cyan;

        // ========================================================
        // COIN AREA
        // ========================================================

        GameObject coinArea =
            CreateUIObject(
                "CoinPanel",
                miniPanel.transform
            );

        RectTransform coinRect =
            coinArea.GetComponent<RectTransform>();

        coinRect.anchorMin =
            new Vector2(0.70f, 0f);

        coinRect.anchorMax =
            new Vector2(1f, 1f);

        coinRect.offsetMin =
            new Vector2(8f, 12f);

        coinRect.offsetMax =
            new Vector2(-12f, -12f);

        Image coinImage =
            coinArea.AddComponent<Image>();

        coinImage.color =
            new Color(
                0.025f,
                0.032f,
                0.038f,
                1f
            );

        // --------------------------------------------------------
        // COIN BORDER
        // --------------------------------------------------------

        GameObject coinAccent =
            CreateUIObject(
                "CoinAccent",
                coinArea.transform
            );

        RectTransform coinAccentRect =
            coinAccent.GetComponent<RectTransform>();

        coinAccentRect.anchorMin =
            new Vector2(0f, 1f);

        coinAccentRect.anchorMax =
            new Vector2(1f, 1f);

        coinAccentRect.pivot =
            new Vector2(0.5f, 1f);

        coinAccentRect.anchoredPosition =
            Vector2.zero;

        coinAccentRect.sizeDelta =
            new Vector2(0f, 4f);

        Image coinAccentImage =
            coinAccent.AddComponent<Image>();

        coinAccentImage.color =
            Gold;

        // --------------------------------------------------------
        // COIN LABEL
        // --------------------------------------------------------

        TMP_Text coinLabel =
            CreateTMP(
                "CoinLabel",
                coinArea.transform,
                "🪙  COIN",
                15,
                FontStyles.Bold,
                Gold
            );

        RectTransform coinLabelRect =
            coinLabel.rectTransform;

        coinLabelRect.anchorMin =
            new Vector2(0f, 1f);

        coinLabelRect.anchorMax =
            new Vector2(1f, 1f);

        coinLabelRect.pivot =
            new Vector2(0.5f, 1f);

        coinLabelRect.anchoredPosition =
            new Vector2(0f, -14f);

        coinLabelRect.sizeDelta =
            new Vector2(0f, 26f);

        coinLabel.alignment =
            TextAlignmentOptions.Center;

        // --------------------------------------------------------
        // COIN VALUE
        // --------------------------------------------------------

        TMP_Text coinValue =
            CreateTMP(
                "MiniCoin",
                coinArea.transform,
                "0",
                28,
                FontStyles.Bold,
                White
            );

        RectTransform coinValueRect =
            coinValue.rectTransform;

        coinValueRect.anchorMin =
            new Vector2(0f, 0.5f);

        coinValueRect.anchorMax =
            new Vector2(1f, 0.5f);

        coinValueRect.pivot =
            new Vector2(0.5f, 0.5f);

        coinValueRect.anchoredPosition =
            new Vector2(0f, -8f);

        coinValueRect.sizeDelta =
            new Vector2(0f, 40f);

        coinValue.alignment =
            TextAlignmentOptions.Center;

        // ========================================================
        // SEPARATOR
        // ========================================================

        GameObject separator =
            CreateUIObject(
                "Separator",
                miniPanel.transform
            );

        RectTransform separatorRect =
            separator.GetComponent<RectTransform>();

        separatorRect.anchorMin =
            new Vector2(0.70f, 0f);

        separatorRect.anchorMax =
            new Vector2(0.70f, 1f);

        separatorRect.pivot =
            new Vector2(0.5f, 0.5f);

        separatorRect.anchoredPosition =
            Vector2.zero;

        separatorRect.sizeDelta =
            new Vector2(2f, -30f);

        Image separatorImage =
            separator.AddComponent<Image>();

        separatorImage.color =
            new Color(
                Gold.r,
                Gold.g,
                Gold.b,
                0.35f
            );

        // ========================================================
        // SIBLING ORDER
        // ========================================================

        border.transform.SetAsFirstSibling();
        surface.transform.SetAsLastSibling();

        topLine.transform.SetAsLastSibling();

        characterArea.transform.SetAsLastSibling();
        coinArea.transform.SetAsLastSibling();
        separator.transform.SetAsLastSibling();

        // ========================================================
        // BUTTON CONNECTION
        // ========================================================

        ConnectAvatarButton(
            avatarButtonComponent
        );

        // ========================================================
        // SAVE
        // ========================================================

        EditorUtility.SetDirty(
            miniPanel
        );

        Selection.activeGameObject =
            miniPanel;

        Debug.Log(
            "[CharacterProfileMenuUISetup] " +
            "Đã tạo CharacterMiniPanel thành công.\n" +
            "AvatarButton đã được nối tới " +
            "CharacterProfileUI.Open()."
        );
    }

    // ============================================================
    // CONNECT AVATAR
    // ============================================================

    private static void ConnectAvatarButton(
        Button avatarButton
    )
    {
        if (avatarButton == null)
            return;

        CharacterProfileUI profileUI =
            Object.FindFirstObjectByType<CharacterProfileUI>();

        if (profileUI == null)
        {
            Debug.LogWarning(
                "[CharacterProfileMenuUISetup] " +
                "Không tìm thấy CharacterProfileUI trong Scene.\n" +
                "Mini profile vẫn được tạo, nhưng AvatarButton " +
                "chưa được nối tự động."
            );

            return;
        }

        avatarButton.onClick.RemoveAllListeners();

        avatarButton.onClick.AddListener(
            profileUI.Open
        );

        EditorUtility.SetDirty(
            avatarButton
        );
    }

    // ============================================================
    // FIND CANVAS
    // ============================================================

    private static Canvas FindCanvas()
    {
        GameObject canvasObject =
            GameObject.Find(CanvasName);

        if (canvasObject == null)
            return null;

        return canvasObject.GetComponent<Canvas>();
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
    // CREATE TMP
    // ============================================================

    private static TMP_Text CreateTMP(
        string objectName,
        Transform parent,
        string text,
        float fontSize,
        FontStyles fontStyle,
        Color color
    )
    {
        GameObject obj =
            CreateUIObject(
                objectName,
                parent
            );

        TextMeshProUGUI tmp =
            obj.AddComponent<TextMeshProUGUI>();

        tmp.text =
            text;

        tmp.fontSize =
            fontSize;

        tmp.fontStyle =
            fontStyle;

        tmp.color =
            color;

        tmp.enableAutoSizing =
            false;

        tmp.textWrappingMode =
            TextWrappingModes.NoWrap;

        tmp.raycastTarget =
            false;

        return tmp;
    }

    // ============================================================
    // STRETCH
    // ============================================================

    private static void Stretch(
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
    }

    private static void Stretch(
        RectTransform rect,
        Vector2 offsetMin,
        Vector2 offsetMax
    )
    {
        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            offsetMin;

        rect.offsetMax =
            offsetMax;
    }

    // ============================================================
    // FIND CHILD RECURSIVE
    // ============================================================

    private static Transform FindChildRecursive(
        Transform parent,
        string targetName
    )
    {
        if (parent == null)
            return null;

        if (parent.name == targetName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child =
                parent.GetChild(i);

            Transform result =
                FindChildRecursive(
                    child,
                    targetName
                );

            if (result != null)
                return result;
        }

        return null;
    }
}

#endif