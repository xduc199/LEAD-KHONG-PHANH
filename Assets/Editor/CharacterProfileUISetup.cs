#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public static class CharacterProfileUISetup
{
    // ============================================================
    // COLORS
    // ============================================================

    private static readonly Color OverlayColor =
        Hex("#05070A", 0.92f);

    private static readonly Color PanelColor =
        Hex("#10151A", 1f);

    private static readonly Color SurfaceColor =
        Hex("#171E24", 1f);

    private static readonly Color SurfaceDarkColor =
        Hex("#0B1014", 1f);

    private static readonly Color SurfaceLightColor =
        Hex("#202A31", 1f);

    private static readonly Color GoldColor =
        Hex("#FFC928", 1f);

    private static readonly Color GoldDarkColor =
        Hex("#A66F00", 1f);

    private static readonly Color RedColor =
        Hex("#E53935", 1f);

    private static readonly Color CyanColor =
        Hex("#22D9FF", 1f);

    private static readonly Color WhiteColor =
        Hex("#F4F7F8", 1f);

    private static readonly Color TextColor =
        Hex("#D8E0E3", 1f);

    private static readonly Color MutedTextColor =
        Hex("#7D8A90", 1f);

    private static readonly Color BlackColor =
        Hex("#020304", 1f);

    // ============================================================
    // MAIN
    // ============================================================

    [MenuItem("LEAD KHÔNG PHANH/UI/Create Character Profile")]
    public static void CreateCharacterProfile()
    {
        // --------------------------------------------------------
        // CANVAS
        // --------------------------------------------------------

        GameObject canvasObject =
            GameObject.Find("CharacterProfileCanvas");

        if (canvasObject != null)
        {
            Object.DestroyImmediate(canvasObject);
        }

        canvasObject = new GameObject(
            "CharacterProfileCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CharacterProfileUI)
        );

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder = 100;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f, 1080f);

        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystem();

        // --------------------------------------------------------
        // SAFE AREA
        // --------------------------------------------------------

        GameObject safeArea =
            CreateUIObject(
                "SafeArea",
                canvasObject.transform
            );

        SetStretch(
            safeArea.GetComponent<RectTransform>(),
            0, 0, 0, 0
        );

        // --------------------------------------------------------
        // FULL SCREEN OVERLAY
        // --------------------------------------------------------

        GameObject characterPanel =
            CreateImage(
                "CharacterPanel",
                safeArea.transform,
                OverlayColor
            );

        SetStretch(
            characterPanel.GetComponent<RectTransform>(),
            0, 0, 0, 0
        );

        // --------------------------------------------------------
        // MAIN CONTENT
        // --------------------------------------------------------

        GameObject content =
            CreateUIObject(
                "Content",
                characterPanel.transform
            );

        RectTransform contentRect =
            content.GetComponent<RectTransform>();

        contentRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        contentRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        contentRect.pivot =
            new Vector2(0.5f, 0.5f);

        contentRect.sizeDelta =
            new Vector2(1120f, 900f);

        contentRect.anchoredPosition =
            Vector2.zero;

        // --------------------------------------------------------
        // SHADOW
        // --------------------------------------------------------

        GameObject shadow =
            CreateImage(
                "PanelShadow",
                content.transform,
                new Color(0f, 0f, 0f, 0.65f)
            );

        SetRect(
            shadow.GetComponent<RectTransform>(),
            14f,
            -14f,
            1120f,
            900f
        );

        // --------------------------------------------------------
        // MAIN PANEL
        // --------------------------------------------------------

        GameObject panelSurface =
            CreateImage(
                "PanelSurface",
                content.transform,
                PanelColor
            );

        SetRect(
            panelSurface.GetComponent<RectTransform>(),
            0,
            0,
            1120f,
            900f
        );

        // --------------------------------------------------------
        // OUTER BORDER
        // --------------------------------------------------------

        GameObject border =
            CreateImage(
                "Border",
                content.transform,
                GoldDarkColor
            );

        SetRect(
            border.GetComponent<RectTransform>(),
            0,
            0,
            1120f,
            900f
        );

        // Inner panel over border
        GameObject borderInner =
            CreateImage(
                "BorderInner",
                border.transform,
                PanelColor
            );

        SetStretch(
            borderInner.GetComponent<RectTransform>(),
            2f, 2f, 2f, 2f
        );

        // --------------------------------------------------------
        // TOP GOLD LINE
        // --------------------------------------------------------

        GameObject topGold =
            CreateImage(
                "TopGoldLine",
                content.transform,
                GoldColor
            );

        SetTopStretch(
            topGold.GetComponent<RectTransform>(),
            0,
            0,
            5f
        );

        // --------------------------------------------------------
        // HEADER
        // --------------------------------------------------------

        GameObject header =
            CreateUIObject(
                "Header",
                content.transform
            );

        SetTopStretch(
            header.GetComponent<RectTransform>(),
            30f,
            88f,
            86f
        );

        // Gold accent
        GameObject headerAccent =
            CreateImage(
                "GoldAccent",
                header.transform,
                GoldColor
            );

        SetLeftStretch(
            headerAccent.GetComponent<RectTransform>(),
            0,
            0,
            5f
        );

        // Title
        GameObject title =
            CreateTMP(
                "Title",
                header.transform,
                "HỒ SƠ TAY LÁI",
                42f,
                FontStyles.Bold,
                WhiteColor
            );

        SetLeftStretch(
            title.GetComponent<RectTransform>(),
            22f,
            48f,
            320f
        );

        title.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Left;

        // Subtitle
        GameObject subtitle =
            CreateTMP(
                "Subtitle",
                header.transform,
                "THÔNG TIN CHIẾN BINH ĐƯỜNG PHỐ",
                15f,
                FontStyles.Bold,
                MutedTextColor
            );

        SetLeftStretch(
            subtitle.GetComponent<RectTransform>(),
            24f,
            15f,
            420f
        );

        subtitle.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Left;

        // Close button
        GameObject closeButton =
            CreateButton(
                "CloseButton",
                header.transform,
                SurfaceLightColor
            );

        RectTransform closeRect =
            closeButton.GetComponent<RectTransform>();

        closeRect.anchorMin =
            new Vector2(1f, 0.5f);

        closeRect.anchorMax =
            new Vector2(1f, 0.5f);

        closeRect.pivot =
            new Vector2(1f, 0.5f);

        closeRect.sizeDelta =
            new Vector2(58f, 58f);

        closeRect.anchoredPosition =
            new Vector2(-6f, 0);

        GameObject closeAccent =
            CreateImage(
                "Accent",
                closeButton.transform,
                RedColor
            );

        SetLeftStretch(
            closeAccent.GetComponent<RectTransform>(),
            0,
            0,
            4f
        );

        GameObject closeText =
            CreateTMP(
                "Text",
                closeButton.transform,
                "×",
                34f,
                FontStyles.Bold,
                WhiteColor
            );

        SetStretch(
            closeText.GetComponent<RectTransform>(),
            0, 0, 0, 0
        );

        closeText.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Center;

        // --------------------------------------------------------
        // CHARACTER SECTION
        // --------------------------------------------------------

        GameObject characterSection =
            CreateImage(
                "CharacterSection",
                content.transform,
                SurfaceDarkColor
            );

        SetLeftStretch(
            characterSection.GetComponent<RectTransform>(),
            36f,
            350f,
            390f
        );

        // --------------------------------------------------------
        // AVATAR BACKPLATE
        // --------------------------------------------------------

        GameObject avatarBackplate =
            CreateImage(
                "AvatarBackplate",
                characterSection.transform,
                BlackColor
            );

        RectTransform avatarBackRect =
            avatarBackplate.GetComponent<RectTransform>();

        avatarBackRect.anchorMin =
            new Vector2(0f, 0.5f);

        avatarBackRect.anchorMax =
            new Vector2(0f, 0.5f);

        avatarBackRect.pivot =
            new Vector2(0f, 0.5f);

        avatarBackRect.sizeDelta =
            new Vector2(270f, 320f);

        avatarBackRect.anchoredPosition =
            new Vector2(28f, 0);

        // Cyan stripe
        GameObject avatarStripe =
            CreateImage(
                "AvatarStripe",
                avatarBackplate.transform,
                CyanColor
            );

        SetTopStretch(
            avatarStripe.GetComponent<RectTransform>(),
            0,
            0,
            5f
        );

        // Avatar frame
        GameObject avatarFrame =
            CreateImage(
                "AvatarFrame",
                avatarBackplate.transform,
                GoldDarkColor
            );

        RectTransform avatarFrameRect =
            avatarFrame.GetComponent<RectTransform>();

        avatarFrameRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        avatarFrameRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        avatarFrameRect.sizeDelta =
            new Vector2(180f, 210f);

        // Avatar
        GameObject avatar =
            CreateImage(
                "Avatar",
                avatarFrame.transform,
                Hex("#252D33", 1f)
            );

        SetStretch(
            avatar.GetComponent<RectTransform>(),
            5f, 5f, 5f, 5f
        );

        // Placeholder
        GameObject avatarPlaceholder =
            CreateTMP(
                "AvatarPlaceholder",
                avatar.transform,
                "LEAD",
                30f,
                FontStyles.Bold,
                GoldColor
            );

        SetStretch(
            avatarPlaceholder.GetComponent<RectTransform>(),
            0, 0, 0, 0
        );

        avatarPlaceholder.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Center;

        // Avatar sub
        GameObject avatarSub =
            CreateTMP(
                "AvatarSub",
                avatarBackplate.transform,
                "NINJA LEAD",
                14f,
                FontStyles.Bold,
                CyanColor
            );

        RectTransform avatarSubRect =
            avatarSub.GetComponent<RectTransform>();

        avatarSubRect.anchorMin =
            new Vector2(0.5f, 0f);

        avatarSubRect.anchorMax =
            new Vector2(0.5f, 0f);

        avatarSubRect.pivot =
            new Vector2(0.5f, 0f);

        avatarSubRect.sizeDelta =
            new Vector2(220f, 30f);

        avatarSubRect.anchoredPosition =
            new Vector2(0f, 18f);

        // --------------------------------------------------------
        // LEVEL BADGE
        // --------------------------------------------------------

        GameObject levelBadge =
            CreateImage(
                "LevelBadge",
                characterSection.transform,
                GoldColor
            );

        RectTransform badgeRect =
            levelBadge.GetComponent<RectTransform>();

        badgeRect.anchorMin =
            new Vector2(0f, 0.5f);

        badgeRect.anchorMax =
            new Vector2(0f, 0.5f);

        badgeRect.pivot =
            new Vector2(0f, 0.5f);

        badgeRect.sizeDelta =
            new Vector2(90f, 90f);

        badgeRect.anchoredPosition =
            new Vector2(260f, 112f);

        GameObject levelLabel =
            CreateTMP(
                "LevelLabel",
                levelBadge.transform,
                "LV 1",
                25f,
                FontStyles.Bold,
                BlackColor
            );

        SetStretch(
            levelLabel.GetComponent<RectTransform>(),
            0, 0, 0, 0
        );

        levelLabel.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Center;

        // --------------------------------------------------------
        // CHARACTER INFO
        // --------------------------------------------------------

        GameObject characterInfo =
            CreateUIObject(
                "CharacterInfo",
                characterSection.transform
            );

        SetLeftStretch(
            characterInfo.GetComponent<RectTransform>(),
            340f,
            25f,
            310f
        );

        GameObject playerName =
            CreateTMP(
                "PlayerName",
                characterInfo.transform,
                "NINJA LEAD",
                34f,
                FontStyles.Bold,
                WhiteColor
            );

        SetTopStretch(
            playerName.GetComponent<RectTransform>(),
            0,
            0,
            50f
        );

        playerName.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Left;

        GameObject rankText =
            CreateTMP(
                "RankText",
                characterInfo.transform,
                "TÂN BINH ĐƯỜNG PHỐ",
                17f,
                FontStyles.Bold,
                GoldColor
            );

        SetTopStretch(
            rankText.GetComponent<RectTransform>(),
            58f,
            0,
            30f
        );

        rankText.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Left;

        GameObject rankLine =
            CreateImage(
                "RankLine",
                characterInfo.transform,
                GoldColor
            );

        SetTopStretch(
            rankLine.GetComponent<RectTransform>(),
            96f,
            0,
            2f
        );

        GameObject description =
            CreateTMP(
                "Description",
                characterInfo.transform,
                "Tay lái trẻ chuyên trị giao thông hỗn loạn.\n"
                + "Không thắng bằng tốc độ — thắng bằng độ liều.",
                16f,
                FontStyles.Normal,
                TextColor
            );

        SetTopStretch(
            description.GetComponent<RectTransform>(),
            112f,
            0,
            80f
        );

        description.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.TopLeft;

        GameObject status =
            CreateTMP(
                "Status",
                characterInfo.transform,
                "● ĐANG HOẠT ĐỘNG",
                14f,
                FontStyles.Bold,
                CyanColor
            );

        SetBottomStretch(
            status.GetComponent<RectTransform>(),
            0,
            0,
            30f
        );

        status.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Left;

        // --------------------------------------------------------
        // EXP SECTION
        // --------------------------------------------------------

        GameObject expSection =
            CreateImage(
                "ExpSection",
                content.transform,
                SurfaceColor
            );

        SetLeftStretch(
            expSection.GetComponent<RectTransform>(),
            406f,
            350f,
            390f
        );

        GameObject expHeader =
            CreateUIObject(
                "ExpHeader",
                expSection.transform
            );

        SetTopStretch(
            expHeader.GetComponent<RectTransform>(),
            24f,
            24f,
            48f
        );

        GameObject expLabel =
            CreateTMP(
                "ExpLabel",
                expHeader.transform,
                "KINH NGHIỆM",
                18f,
                FontStyles.Bold,
                WhiteColor
            );

        SetLeftStretch(
            expLabel.GetComponent<RectTransform>(),
            0,
            0,
            200f
        );

        expLabel.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Left;

        GameObject nextLevel =
            CreateTMP(
                "NextLevel",
                expHeader.transform,
                "LEVEL TIẾP THEO",
                13f,
                FontStyles.Bold,
                MutedTextColor
            );

        SetRightStretch(
            nextLevel.GetComponent<RectTransform>(),
            0,
            0,
            150f
        );

        nextLevel.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Right;

        GameObject expValue =
            CreateTMP(
                "ExpValue",
                expHeader.transform,
                "0 / 100 EXP",
                16f,
                FontStyles.Bold,
                GoldColor
            );

        RectTransform expValueRect =
            expValue.GetComponent<RectTransform>();

        expValueRect.anchorMin =
            new Vector2(1f, 0f);

        expValueRect.anchorMax =
            new Vector2(1f, 1f);

        expValueRect.pivot =
            new Vector2(1f, 0.5f);

        expValueRect.sizeDelta =
            new Vector2(150f, 0);

        expValueRect.anchoredPosition =
            new Vector2(0f, 0f);

        expValue.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Right;

        // EXP BAR OUTER
        GameObject expBar =
            CreateImage(
                "ExpBar",
                expSection.transform,
                SurfaceDarkColor
            );

        SetLeftStretch(
            expBar.GetComponent<RectTransform>(),
            24f,
            145f,
            28f
        );

        // EXP INNER
        GameObject inner =
            CreateImage(
                "Inner",
                expBar.transform,
                SurfaceLightColor
            );

        SetStretch(
            inner.GetComponent<RectTransform>(),
            3f, 3f, 3f, 3f
        );

        // Fill
        GameObject fill =
            CreateImage(
                "Fill",
                inner.transform,
                GoldColor
            );

        RectTransform fillRect =
            fill.GetComponent<RectTransform>();

        fillRect.anchorMin =
            new Vector2(0f, 0f);

        fillRect.anchorMax =
            new Vector2(0f, 1f);

        fillRect.pivot =
            new Vector2(0f, 0.5f);

        fillRect.sizeDelta =
            new Vector2(0f, 0f);

        fillRect.offsetMin =
            new Vector2(0f, 0f);

        fillRect.offsetMax =
            new Vector2(0f, 0f);

        // Glow
        GameObject glow =
            CreateImage(
                "Glow",
                fill.transform,
                new Color(
                    CyanColor.r,
                    CyanColor.g,
                    CyanColor.b,
                    0.45f
                )
            );

        SetTopStretch(
            glow.GetComponent<RectTransform>(),
            0,
            0,
            3f
        );

        // Hint
        GameObject expHint =
            CreateTMP(
                "ExpHint",
                expSection.transform,
                "Tích EXP để mở khóa cấp độ mới",
                13f,
                FontStyles.Normal,
                MutedTextColor
            );

        SetBottomStretch(
            expHint.GetComponent<RectTransform>(),
            24f,
            0,
            28f
        );

        expHint.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Left;

        // --------------------------------------------------------
        // CURRENCY SECTION
        // --------------------------------------------------------

        GameObject currencySection =
            CreateUIObject(
                "CurrencySection",
                content.transform
            );

        SetLeftStretch(
            currencySection.GetComponent<RectTransform>(),
            406f,
            350f,
            390f
        );

        // Coin card
        GameObject coinCard =
            CreateImage(
                "CoinCard",
                currencySection.transform,
                SurfaceDarkColor
            );

        SetLeftStretch(
            coinCard.GetComponent<RectTransform>(),
            24f,
            185f,
            140f
        );

        CreateCurrencyCard(
            coinCard,
            "COIN",
            "0",
            "◉",
            GoldColor
        );

        // Best card
        GameObject bestCard =
            CreateImage(
                "BestCard",
                currencySection.transform,
                SurfaceDarkColor
            );

        SetRightStretch(
            bestCard.GetComponent<RectTransform>(),
            24f,
            185f,
            140f
        );

        CreateCurrencyCard(
            bestCard,
            "KỶ LỤC",
            "0 M",
            "★",
            CyanColor
        );

        // --------------------------------------------------------
        // STATS SECTION
        // --------------------------------------------------------

        GameObject statsSection =
            CreateUIObject(
                "StatsSection",
                content.transform
            );

        SetLeftStretch(
            statsSection.GetComponent<RectTransform>(),
            406f,
            24f,
            390f
        );

        // Runs
        GameObject runsStat =
            CreateImage(
                "RunsStat",
                statsSection.transform,
                SurfaceDarkColor
            );

        SetLeftStretch(
            runsStat.GetComponent<RectTransform>(),
            24f,
            24f,
            115f
        );

        CreateStatCard(
            runsStat,
            "SỐ LẦN CHẠY",
            "—"
        );

        // Distance
        GameObject distanceStat =
            CreateImage(
                "DistanceStat",
                statsSection.transform,
                SurfaceDarkColor
            );

        SetRightStretch(
            distanceStat.GetComponent<RectTransform>(),
            24f,
            24f,
            115f
        );

        CreateStatCard(
            distanceStat,
            "QUÃNG ĐƯỜNG",
            "0 M"
        );

        // --------------------------------------------------------
        // FOOTER
        // --------------------------------------------------------

        GameObject footer =
            CreateUIObject(
                "Footer",
                content.transform
            );

        SetBottomStretch(
            footer.GetComponent<RectTransform>(),
            24f,
            24f,
            45f
        );

        GameObject footerLeft =
            CreateTMP(
                "FooterLeft",
                footer.transform,
                "LEAD KHÔNG PHANH",
                12f,
                FontStyles.Bold,
                GoldColor
            );

        SetLeftStretch(
            footerLeft.GetComponent<RectTransform>(),
            0,
            0,
            240f
        );

        footerLeft.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Left;

        GameObject footerRight =
            CreateTMP(
                "FooterRight",
                footer.transform,
                "RIDE HARD • STAY ALIVE",
                12f,
                FontStyles.Bold,
                MutedTextColor
            );

        SetRightStretch(
            footerRight.GetComponent<RectTransform>(),
            0,
            0,
            240f
        );

        footerRight.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Right;

        GameObject footerAccent =
            CreateImage(
                "FooterAccent",
                footer.transform,
                CyanColor
            );

        RectTransform footerAccentRect =
            footerAccent.GetComponent<RectTransform>();

        footerAccentRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        footerAccentRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        footerAccentRect.sizeDelta =
            new Vector2(180f, 2f);

        footerAccentRect.anchoredPosition =
            Vector2.zero;

        // --------------------------------------------------------
        // INITIAL STATE
        // --------------------------------------------------------

        characterPanel.SetActive(false);

        // --------------------------------------------------------
        // SAVE
        // --------------------------------------------------------

        EditorUtility.SetDirty(canvasObject);

        Selection.activeGameObject =
            canvasObject;

        Debug.Log(
            "[CharacterProfileUISetup] " +
            "Character Profile created successfully."
        );
    }

    // ============================================================
    // CURRENCY CARD
    // ============================================================

    private static void CreateCurrencyCard(
        GameObject card,
        string label,
        string value,
        string icon,
        Color accent
    )
    {
        GameObject iconObject =
            CreateImage(
                "Icon",
                card.transform,
                new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    0.12f
                )
            );

        RectTransform iconRect =
            iconObject.GetComponent<RectTransform>();

        iconRect.anchorMin =
            new Vector2(0f, 0.5f);

        iconRect.anchorMax =
            new Vector2(0f, 0.5f);

        iconRect.pivot =
            new Vector2(0f, 0.5f);

        iconRect.sizeDelta =
            new Vector2(64f, 64f);

        iconRect.anchoredPosition =
            new Vector2(20f, 0);

        GameObject iconText =
            CreateTMP(
                "IconText",
                iconObject.transform,
                icon,
                27f,
                FontStyles.Bold,
                accent
            );

        SetStretch(
            iconText.GetComponent<RectTransform>(),
            0, 0, 0, 0
        );

        iconText.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Center;

        GameObject labelObject =
            CreateTMP(
                "Label",
                card.transform,
                label,
                12f,
                FontStyles.Bold,
                MutedTextColor
            );

        SetTopStretch(
            labelObject.GetComponent<RectTransform>(),
            22f,
            0,
            25f
        );

        labelObject.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Center;

        GameObject valueObject =
            CreateTMP(
                "Value",
                card.transform,
                value,
                26f,
                FontStyles.Bold,
                WhiteColor
            );

        SetBottomStretch(
            valueObject.GetComponent<RectTransform>(),
            18f,
            0,
            42f
        );

        valueObject.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Center;
    }

    // ============================================================
    // STAT CARD
    // ============================================================

    private static void CreateStatCard(
        GameObject card,
        string label,
        string value
    )
    {
        GameObject labelObject =
            CreateTMP(
                "Label",
                card.transform,
                label,
                12f,
                FontStyles.Bold,
                MutedTextColor
            );

        SetTopStretch(
            labelObject.GetComponent<RectTransform>(),
            18f,
            0,
            25f
        );

        labelObject.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Center;

        GameObject valueObject =
            CreateTMP(
                "Value",
                card.transform,
                value,
                25f,
                FontStyles.Bold,
                WhiteColor
            );

        SetBottomStretch(
            valueObject.GetComponent<RectTransform>(),
            20f,
            0,
            42f
        );

        valueObject.GetComponent<TextMeshProUGUI>().alignment =
            TextAlignmentOptions.Center;
    }

    // ============================================================
    // UI CREATION
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

    private static GameObject CreateImage(
        string objectName,
        Transform parent,
        Color color
    )
    {
        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image)
            );

        obj.transform.SetParent(
            parent,
            false
        );

        Image image =
            obj.GetComponent<Image>();

        image.color = color;
        image.raycastTarget = false;

        return obj;
    }

    private static GameObject CreateButton(
        string objectName,
        Transform parent,
        Color color
    )
    {
        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );

        obj.transform.SetParent(
            parent,
            false
        );

        Image image =
            obj.GetComponent<Image>();

        image.color = color;
        image.raycastTarget = true;

        Button button =
            obj.GetComponent<Button>();

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            color;

        colors.highlightedColor =
            Color.Lerp(
                color,
                WhiteColor,
                0.12f
            );

        colors.pressedColor =
            Color.Lerp(
                color,
                BlackColor,
                0.15f
            );

        colors.selectedColor =
            colors.highlightedColor;

        button.colors = colors;

        return obj;
    }

    private static GameObject CreateTMP(
        string objectName,
        Transform parent,
        string text,
        float fontSize,
        FontStyles style,
        Color color
    )
    {
        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        obj.transform.SetParent(
            parent,
            false
        );

        TextMeshProUGUI tmp =
            obj.GetComponent<TextMeshProUGUI>();

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;

        tmp.alignment =
            TextAlignmentOptions.Left;

        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;

        return obj;
    }

    // ============================================================
    // RECT HELPERS
    // ============================================================

    private static void SetStretch(
        RectTransform rect,
        float left,
        float right,
        float top,
        float bottom
    )
    {
        rect.anchorMin =
            new Vector2(0f, 0f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.offsetMin =
            new Vector2(left, bottom);

        rect.offsetMax =
            new Vector2(-right, -top);
    }

    private static void SetRect(
        RectTransform rect,
        float x,
        float y,
        float width,
        float height
    )
    {
        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.sizeDelta =
            new Vector2(width, height);

        rect.anchoredPosition =
            new Vector2(x, y);
    }

    private static void SetTopStretch(
        RectTransform rect,
        float top,
        float right,
        float height
    )
    {
        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.pivot =
            new Vector2(0.5f, 1f);

        rect.sizeDelta =
            new Vector2(
                -right,
                height
            );

        rect.offsetMin =
            new Vector2(
                0f,
                -top - height
            );

        rect.offsetMax =
            new Vector2(
                -right,
                -top
            );
    }

    private static void SetBottomStretch(
        RectTransform rect,
        float bottom,
        float right,
        float height
    )
    {
        rect.anchorMin =
            new Vector2(0f, 0f);

        rect.anchorMax =
            new Vector2(1f, 0f);

        rect.pivot =
            new Vector2(0.5f, 0f);

        rect.sizeDelta =
            new Vector2(
                -right,
                height
            );

        rect.offsetMin =
            new Vector2(
                0f,
                bottom
            );

        rect.offsetMax =
            new Vector2(
                -right,
                bottom + height
            );
    }

    private static void SetLeftStretch(
        RectTransform rect,
        float left,
        float right,
        float width
    )
    {
        rect.anchorMin =
            new Vector2(0f, 0f);

        rect.anchorMax =
            new Vector2(0f, 1f);

        rect.pivot =
            new Vector2(0f, 0.5f);

        rect.sizeDelta =
            new Vector2(
                width,
                -right
            );

        rect.offsetMin =
            new Vector2(
                left,
                0f
            );

        rect.offsetMax =
            new Vector2(
                left + width,
                -right
            );
    }

    private static void SetRightStretch(
        RectTransform rect,
        float right,
        float top,
        float width
    )
    {
        rect.anchorMin =
            new Vector2(1f, 0f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.pivot =
            new Vector2(1f, 0.5f);

        rect.sizeDelta =
            new Vector2(
                width,
                -top
            );

        rect.offsetMin =
            new Vector2(
                -right - width,
                0f
            );

        rect.offsetMax =
            new Vector2(
                -right,
                -top
            );
    }

    // ============================================================
    // EVENT SYSTEM
    // ============================================================

    private static void EnsureEventSystem()
    {
        EventSystem existing =
            Object.FindFirstObjectByType<EventSystem>();

        if (existing != null)
            return;

        GameObject eventSystem =
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule)
            );

        eventSystem.transform.SetAsLastSibling();
    }

    // ============================================================
    // COLOR
    // ============================================================

    private static Color Hex(
        string hex,
        float alpha = 1f
    )
    {
        if (ColorUtility.TryParseHtmlString(
            hex,
            out Color color))
        {
            color.a = alpha;
            return color;
        }

        return Color.white;
    }
}

#endif