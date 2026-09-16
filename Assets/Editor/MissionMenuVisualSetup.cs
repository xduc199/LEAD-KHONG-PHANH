#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class MissionMenuVisualSetup
{
    //=============================================================
    // SETTINGS
    //=============================================================

    private const int InitialMissionCardCount = 3;

    //=============================================================
    // COLORS
    //=============================================================

    private static readonly Color White =
        new Color32(245, 247, 250, 255);

    private static readonly Color Secondary =
        new Color32(185, 192, 202, 255);

    private static readonly Color Gold =
        new Color32(255, 203, 45, 255);

    private static readonly Color GoldBright =
        new Color32(255, 220, 80, 255);

    private static readonly Color DarkBackground =
        new Color32(15, 19, 25, 245);

    private static readonly Color PanelBackground =
        new Color32(24, 29, 37, 245);

    private static readonly Color CardBackground =
        new Color32(31, 37, 47, 245);

    private static readonly Color ProgressBackground =
        new Color32(10, 13, 18, 220);

    private static readonly Color ProgressFill =
        new Color32(255, 203, 45, 255);

    private static readonly Color ButtonBackground =
        new Color32(255, 203, 45, 255);

    private static readonly Color ButtonText =
        new Color32(20, 23, 28, 255);

    private static readonly Color CloseBackground =
        new Color32(35, 41, 51, 235);

    private static readonly Color ShadowColor =
        new Color32(0, 0, 0, 100);

    //=============================================================
    // NAMES
    //=============================================================

    private const string CanvasName =
        "MainMenuCanvas";

    private const string SafeAreaName =
        "SafeArea";

    private const string PanelName =
        "MissionPanel";

    private const string RoundedRectName =
        "rounded_rect";

    //=============================================================
    // MENU
    //=============================================================

    [MenuItem(
        "LEAD KHONG PHANH/UI/Create Mission UI"
    )]
    public static void CreateMissionUI()
    {
        //=========================================================
        // FIND CANVAS
        //=========================================================

        GameObject canvas =
            GameObject.Find(CanvasName);

        if (canvas == null)
        {
            Debug.LogError(
                "[Mission UI] Không tìm thấy " +
                CanvasName +
                ".\nHãy tạo MainMenuCanvas trước."
            );

            return;
        }

        //=========================================================
        // FIND SAFE AREA
        //=========================================================

        Transform safeArea =
            canvas.transform.Find(SafeAreaName);

        if (safeArea == null)
        {
            Debug.LogError(
                "[Mission UI] Không tìm thấy " +
                CanvasName +
                "/" +
                SafeAreaName +
                "."
            );

            return;
        }

        //=========================================================
        // ROUNDED RECT
        //=========================================================

        Sprite roundedSprite =
            PrepareRoundedRectSprite();

        if (roundedSprite == null)
        {
            Debug.LogError(
                "[Mission UI] Không tìm thấy rounded_rect."
            );

            Debug.Log(
                "[Mission UI] Đặt file tại:\n" +
                "Assets/UI/Sprites/rounded_rect.png"
            );

            return;
        }

        //=========================================================
        // REMOVE OLD PANEL
        //=========================================================

        Transform oldPanel =
            safeArea.Find(PanelName);

        if (oldPanel != null)
        {
            if (!EditorUtility.DisplayDialog(
                "Mission UI",
                "MissionPanel đã tồn tại.\n\n" +
                "Bạn có muốn xóa và dựng lại toàn bộ Mission UI không?",
                "Dựng lại",
                "Hủy"
            ))
            {
                return;
            }

            Object.DestroyImmediate(
                oldPanel.gameObject
            );
        }

        //=========================================================
        // CREATE PANEL
        //=========================================================

        GameObject panel =
            CreateUIObject(
                PanelName,
                safeArea
            );

        RectTransform panelRect =
            panel.GetComponent<RectTransform>();

        StretchFull(panelRect);

        //=========================================================
        // PANEL BACKGROUND
        //=========================================================

        Image panelImage =
            panel.AddComponent<Image>();

        panelImage.sprite =
            roundedSprite;

        panelImage.type =
            Image.Type.Sliced;

        panelImage.color =
            DarkBackground;

        panelImage.raycastTarget =
            true;

        SetupShadow(
            panel,
            ShadowColor,
            new Vector2(0f, -5f)
        );

        //=========================================================
        // CONTENT
        //=========================================================

        GameObject content =
            CreateUIObject(
                "Content",
                panel.transform
            );

        RectTransform contentRect =
            content.GetComponent<RectTransform>();

        contentRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        contentRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        contentRect.pivot =
            new Vector2(0.5f, 0.5f);

        contentRect.anchoredPosition =
            Vector2.zero;

        contentRect.sizeDelta =
            new Vector2(
                980f,
                760f
            );

        //=========================================================
        // PANEL SURFACE
        //=========================================================

        GameObject surface =
            CreateUIObject(
                "PanelSurface",
                content.transform
            );

        RectTransform surfaceRect =
            surface.GetComponent<RectTransform>();

        StretchFull(surfaceRect);

        Image surfaceImage =
            surface.AddComponent<Image>();

        surfaceImage.sprite =
            roundedSprite;

        surfaceImage.type =
            Image.Type.Sliced;

        surfaceImage.color =
            PanelBackground;

        // QUAN TRỌNG:
        // Surface chỉ để visual, không được chặn raycast.
        surfaceImage.raycastTarget =
            false;

        surface.transform.SetAsFirstSibling();

        //=========================================================
        // HEADER
        //=========================================================

        GameObject header =
            CreateUIObject(
                "Header",
                content.transform
            );

        RectTransform headerRect =
            header.GetComponent<RectTransform>();

        headerRect.anchorMin =
            new Vector2(0f, 1f);

        headerRect.anchorMax =
            new Vector2(1f, 1f);

        headerRect.pivot =
            new Vector2(0.5f, 1f);

        headerRect.anchoredPosition =
            new Vector2(0f, -28f);

        headerRect.sizeDelta =
            new Vector2(-56f, 110f);

        //=========================================================
        // HEADER ACCENT
        //=========================================================

        CreateAccent(
            header.transform,
            "GoldAccent",
            new Vector2(150f, 5f),
            new Vector2(0f, -100f)
        );

        //=========================================================
        // TITLE
        //=========================================================

        TMP_Text title =
            CreateText(
                "Title",
                header.transform,
                "NHIỆM VỤ HÔM NAY",
                38f,
                White,
                FontStyles.Bold,
                TextAlignmentOptions.Left
            );

        RectTransform titleRect =
            title.rectTransform;

        titleRect.anchorMin =
            new Vector2(0f, 1f);

        titleRect.anchorMax =
            new Vector2(1f, 1f);

        titleRect.pivot =
            new Vector2(0f, 1f);

        titleRect.anchoredPosition =
            new Vector2(0f, -4f);

        titleRect.sizeDelta =
            new Vector2(-150f, 55f);

        //=========================================================
        // SUBTITLE
        //=========================================================

        TMP_Text subtitle =
            CreateText(
                "Subtitle",
                header.transform,
                "Hoàn thành nhiệm vụ để nhận EXP",
                17f,
                Secondary,
                FontStyles.Normal,
                TextAlignmentOptions.Left
            );

        RectTransform subtitleRect =
            subtitle.rectTransform;

        subtitleRect.anchorMin =
            new Vector2(0f, 1f);

        subtitleRect.anchorMax =
            new Vector2(1f, 1f);

        subtitleRect.pivot =
            new Vector2(0f, 1f);

        subtitleRect.anchoredPosition =
            new Vector2(0f, -57f);

        subtitleRect.sizeDelta =
            new Vector2(-150f, 32f);

        //=========================================================
        // CLOSE BUTTON
        //=========================================================

        GameObject closeButton =
            CreateButton(
                "CloseButton",
                header.transform,
                roundedSprite,
                CloseBackground,
                62f,
                62f
            );

        RectTransform closeRect =
            closeButton.GetComponent<RectTransform>();

        closeRect.anchorMin =
            new Vector2(1f, 1f);

        closeRect.anchorMax =
            new Vector2(1f, 1f);

        closeRect.pivot =
            new Vector2(1f, 1f);

        closeRect.anchoredPosition =
            Vector2.zero;

        TMP_Text closeText =
            CreateText(
                "Text",
                closeButton.transform,
                "×",
                38f,
                White,
                FontStyles.Normal,
                TextAlignmentOptions.Center
            );

        StretchFull(
            closeText.rectTransform
        );

        //=========================================================
        // MISSION LIST
        //=========================================================

        GameObject missionList =
            CreateUIObject(
                "MissionList",
                content.transform
            );

        RectTransform listRect =
            missionList.GetComponent<RectTransform>();

        listRect.anchorMin =
            new Vector2(0f, 0f);

        listRect.anchorMax =
            new Vector2(1f, 1f);

        listRect.offsetMin =
            new Vector2(32f, 75f);

        listRect.offsetMax =
            new Vector2(-32f, -128f);

        VerticalLayoutGroup listLayout =
            missionList.AddComponent<
                VerticalLayoutGroup
            >();

        listLayout.spacing =
            14f;

        listLayout.childAlignment =
            TextAnchor.UpperCenter;

        listLayout.childControlWidth =
            true;

        listLayout.childControlHeight =
            true;

        listLayout.childForceExpandWidth =
            true;

        listLayout.childForceExpandHeight =
            false;

        //=========================================================
        // CREATE INITIAL MISSION CARDS
        //=========================================================

        for (int i = 0;
             i < InitialMissionCardCount;
             i++)
        {
            CreateMissionCard(
                missionList.transform,
                roundedSprite,
                i + 1
            );
        }

        //=========================================================
        // FOOTER
        //=========================================================

        GameObject footer =
            CreateUIObject(
                "Footer",
                content.transform
            );

        RectTransform footerRect =
            footer.GetComponent<RectTransform>();

        footerRect.anchorMin =
            new Vector2(0f, 0f);

        footerRect.anchorMax =
            new Vector2(1f, 0f);

        footerRect.pivot =
            new Vector2(0.5f, 0f);

        footerRect.anchoredPosition =
            new Vector2(0f, 28f);

        footerRect.sizeDelta =
            new Vector2(-64f, 42f);

        //=========================================================
        // DAILY RESET
        //=========================================================

        TMP_Text resetText =
            CreateText(
                "DailyResetText",
                footer.transform,
                "NHIỆM VỤ SẼ ĐƯỢC LÀM MỚI MỖI NGÀY",
                14f,
                Secondary,
                FontStyles.Bold,
                TextAlignmentOptions.Center
            );

        StretchFull(
            resetText.rectTransform
        );

        //=========================================================
        // ORDER
        //=========================================================

        panel.transform.SetAsLastSibling();

        //=========================================================
        // SAVE
        //=========================================================

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );

        Selection.activeGameObject =
            panel;

        Debug.Log(
            "[Mission UI] Đã tạo Mission UI thành công.\n" +
            "- MissionPanel\n" +
            "- Header\n" +
            "- CloseButton\n" +
            "- MissionList\n" +
            "- 3 Mission Cards\n" +
            "- MissionCardUI tự động trên từng card\n" +
            "- Footer"
        );
    }

    //=============================================================
    // CREATE MISSION CARD
    //=============================================================

    private static void CreateMissionCard(
        Transform parent,
        Sprite roundedSprite,
        int index)
    {
        GameObject card =
            CreateUIObject(
                "MissionCard_" +
                index.ToString("00"),
                parent
            );

        //=========================================================
        // LAYOUT
        //=========================================================

        LayoutElement layout =
            card.AddComponent<LayoutElement>();

        layout.minHeight =
            150f;

        layout.preferredHeight =
            150f;

        layout.flexibleHeight =
            0f;

        //=========================================================
        // CARD IMAGE
        //=========================================================

        Image cardImage =
            card.AddComponent<Image>();

        cardImage.sprite =
            roundedSprite;

        cardImage.type =
            Image.Type.Sliced;

        cardImage.color =
            CardBackground;

        cardImage.raycastTarget =
            true;

        SetupShadow(
            card,
            ShadowColor,
            new Vector2(0f, -3f)
        );

        //=========================================================
        // TOP HIGHLIGHT
        //=========================================================

        CreateAccent(
            card.transform,
            "Highlight",
            new Vector2(120f, 3f),
            new Vector2(22f, -10f)
        );

        //=========================================================
        // LEFT ACCENT
        //=========================================================

        CreateVerticalAccent(
            card.transform
        );

        //=========================================================
        // ICON
        //=========================================================

        GameObject icon =
            CreateUIObject(
                "Icon",
                card.transform
            );

        RectTransform iconRect =
            icon.GetComponent<RectTransform>();

        iconRect.anchorMin =
            new Vector2(0f, 0.5f);

        iconRect.anchorMax =
            new Vector2(0f, 0.5f);

        iconRect.pivot =
            new Vector2(0.5f, 0.5f);

        iconRect.anchoredPosition =
            new Vector2(60f, 5f);

        iconRect.sizeDelta =
            new Vector2(62f, 62f);

        Image iconImage =
            icon.AddComponent<Image>();

        iconImage.color =
            new Color32(
                255,
                203,
                45,
                35
            );

        iconImage.raycastTarget =
            false;

        //=========================================================
        // ICON TEXT
        //=========================================================

        TMP_Text iconText =
            CreateText(
                "IconText",
                icon.transform,
                "★",
                27f,
                Gold,
                FontStyles.Bold,
                TextAlignmentOptions.Center
            );

        StretchFull(
            iconText.rectTransform
        );

        //=========================================================
        // TITLE
        //=========================================================

        TMP_Text title =
            CreateText(
                "Title",
                card.transform,
                "MISSION TITLE",
                22f,
                White,
                FontStyles.Bold,
                TextAlignmentOptions.Left
            );

        RectTransform titleRect =
            title.rectTransform;

        titleRect.anchorMin =
            new Vector2(0f, 1f);

        titleRect.anchorMax =
            new Vector2(1f, 1f);

        titleRect.pivot =
            new Vector2(0f, 1f);

        titleRect.offsetMin =
            new Vector2(150f, 0f);

        titleRect.offsetMax =
            new Vector2(-185f, -18f);

        //=========================================================
        // DESCRIPTION
        //=========================================================

        TMP_Text description =
            CreateText(
                "Description",
                card.transform,
                "Hoàn thành mục tiêu nhiệm vụ",
                15f,
                Secondary,
                FontStyles.Normal,
                TextAlignmentOptions.Left
            );

        RectTransform descriptionRect =
            description.rectTransform;

        descriptionRect.anchorMin =
            new Vector2(0f, 1f);

        descriptionRect.anchorMax =
            new Vector2(1f, 1f);

        descriptionRect.pivot =
            new Vector2(0f, 1f);

        descriptionRect.offsetMin =
            new Vector2(150f, 0f);

        descriptionRect.offsetMax =
            new Vector2(-185f, -52f);

        //=========================================================
        // PROGRESS BAR
        //=========================================================

        GameObject progress =
            CreateUIObject(
                "ProgressBar",
                card.transform
            );

        RectTransform progressRect =
            progress.GetComponent<RectTransform>();

        progressRect.anchorMin =
            new Vector2(0f, 0f);

        progressRect.anchorMax =
            new Vector2(1f, 0f);

        progressRect.pivot =
            new Vector2(0f, 0f);

        progressRect.anchoredPosition =
            new Vector2(150f, 23f);

        progressRect.sizeDelta =
            new Vector2(-355f, 14f);

        Image progressImage =
            progress.AddComponent<Image>();

        progressImage.sprite =
            roundedSprite;

        progressImage.type =
            Image.Type.Sliced;

        progressImage.color =
            ProgressBackground;

        progressImage.raycastTarget =
            false;

        //=========================================================
        // PROGRESS FILL
        //=========================================================

        GameObject fill =
            CreateUIObject(
                "Fill",
                progress.transform
            );

        RectTransform fillRect =
            fill.GetComponent<RectTransform>();

        fillRect.anchorMin =
            new Vector2(0f, 0f);

        fillRect.anchorMax =
            new Vector2(0.55f, 1f);

        fillRect.pivot =
            new Vector2(0f, 0.5f);

        fillRect.offsetMin =
            Vector2.zero;

        fillRect.offsetMax =
            Vector2.zero;

        Image fillImage =
            fill.AddComponent<Image>();

        fillImage.sprite =
            roundedSprite;

        fillImage.type =
            Image.Type.Sliced;

        fillImage.color =
            ProgressFill;

        fillImage.raycastTarget =
            false;

        //=========================================================
        // PROGRESS TEXT
        //=========================================================

        TMP_Text progressText =
            CreateText(
                "ProgressText",
                card.transform,
                "0 / 0",
                14f,
                White,
                FontStyles.Bold,
                TextAlignmentOptions.Right
            );

        RectTransform progressTextRect =
            progressText.rectTransform;

        progressTextRect.anchorMin =
            new Vector2(0f, 0f);

        progressTextRect.anchorMax =
            new Vector2(1f, 0f);

        progressTextRect.pivot =
            new Vector2(1f, 0f);

        progressTextRect.anchoredPosition =
            new Vector2(-185f, 19f);

        progressTextRect.sizeDelta =
            new Vector2(150f, 24f);

        //=========================================================
        // REWARD
        //=========================================================

        GameObject reward =
            CreateUIObject(
                "Reward",
                card.transform
            );

        RectTransform rewardRect =
            reward.GetComponent<RectTransform>();

        rewardRect.anchorMin =
            new Vector2(1f, 0.5f);

        rewardRect.anchorMax =
            new Vector2(1f, 0.5f);

        rewardRect.pivot =
            new Vector2(1f, 0.5f);

        rewardRect.anchoredPosition =
            new Vector2(-118f, 38f);

        rewardRect.sizeDelta =
            new Vector2(120f, 32f);

        TMP_Text rewardText =
            CreateText(
                "RewardValue",
                reward.transform,
                "+50 EXP",
                16f,
                GoldBright,
                FontStyles.Bold,
                TextAlignmentOptions.Right
            );

        StretchFull(
            rewardText.rectTransform
        );

        //=========================================================
        // CLAIM BUTTON
        //=========================================================

        GameObject claim =
            CreateButton(
                "ClaimButton",
                card.transform,
                roundedSprite,
                ButtonBackground,
                98f,
                42f
            );

        RectTransform claimRect =
            claim.GetComponent<RectTransform>();

        claimRect.anchorMin =
            new Vector2(1f, 0.5f);

        claimRect.anchorMax =
            new Vector2(1f, 0.5f);

        claimRect.pivot =
            new Vector2(1f, 0.5f);

        claimRect.anchoredPosition =
            new Vector2(-18f, -28f);

        TMP_Text claimText =
            CreateText(
                "Text",
                claim.transform,
                "NHẬN",
                15f,
                ButtonText,
                FontStyles.Bold,
                TextAlignmentOptions.Center
            );

        StretchFull(
            claimText.rectTransform
        );

        //=========================================================
        // DISABLED OVERLAY
        //=========================================================

        GameObject disabled =
            CreateUIObject(
                "DisabledOverlay",
                card.transform
            );

        RectTransform disabledRect =
            disabled.GetComponent<RectTransform>();

        StretchFull(
            disabledRect
        );

        Image disabledImage =
            disabled.AddComponent<Image>();

        disabledImage.sprite =
            roundedSprite;

        disabledImage.type =
            Image.Type.Sliced;

        disabledImage.color =
            new Color32(
                0,
                0,
                0,
                85
            );

        // Không được chặn raycast.
        disabledImage.raycastTarget =
            false;

        disabled.SetActive(false);

        //=========================================================
        // COMPLETED BADGE
        //=========================================================

        GameObject completed =
            CreateUIObject(
                "CompletedBadge",
                card.transform
            );

        RectTransform completedRect =
            completed.GetComponent<RectTransform>();

        completedRect.anchorMin =
            new Vector2(1f, 1f);

        completedRect.anchorMax =
            new Vector2(1f, 1f);

        completedRect.pivot =
            new Vector2(1f, 1f);

        completedRect.anchoredPosition =
            new Vector2(-18f, -16f);

        completedRect.sizeDelta =
            new Vector2(105f, 32f);

        Image completedImage =
            completed.AddComponent<Image>();

        completedImage.sprite =
            roundedSprite;

        completedImage.type =
            Image.Type.Sliced;

        completedImage.color =
            new Color32(
                255,
                203,
                45,
                40
            );

        // Không được chặn ClaimButton.
        completedImage.raycastTarget =
            false;

        TMP_Text completedText =
            CreateText(
                "Text",
                completed.transform,
                "HOÀN THÀNH",
                11f,
                GoldBright,
                FontStyles.Bold,
                TextAlignmentOptions.Center
            );

        StretchFull(
            completedText.rectTransform
        );

        completed.SetActive(false);

        //=========================================================
        // ADD MISSION CARD CONTROLLER
        //=========================================================

        MissionCardUI cardUI =
            card.GetComponent<MissionCardUI>();

        if (cardUI == null)
        {
            cardUI =
                card.AddComponent<MissionCardUI>();
        }

        //=========================================================
        // IMPORTANT
        //=========================================================
        //
        // Không gán Mission ID ở đây.
        //
        // MissionUIController sẽ tự lấy mission từ
        // MissionManager và gọi:
        //
        // cardUI.Setup(missionId);
        //
        // Vì vậy 3 card visual này chỉ là UI container.
        // Không có mission cố định trong Inspector.
        //
        //=========================================================

        EditorUtility.SetDirty(
            cardUI
        );
    }

    //=============================================================
    // CREATE UI OBJECT
    //=============================================================

    private static GameObject CreateUIObject(
        string objectName,
        Transform parent)
    {
        GameObject go =
            new GameObject(
                objectName,
                typeof(RectTransform)
            );

        go.transform.SetParent(
            parent,
            false
        );

        RectTransform rt =
            go.GetComponent<RectTransform>();

        rt.localScale =
            Vector3.one;

        rt.localRotation =
            Quaternion.identity;

        return go;
    }

    //=============================================================
    // CREATE TEXT
    //=============================================================

    private static TMP_Text CreateText(
        string objectName,
        Transform parent,
        string text,
        float size,
        Color color,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        GameObject go =
            CreateUIObject(
                objectName,
                parent
            );

        TMP_Text tmp =
            go.AddComponent<TextMeshProUGUI>();

        tmp.text =
            text;

        tmp.fontSize =
            size;

        tmp.color =
            color;

        tmp.fontStyle =
            style;

        tmp.alignment =
            alignment;

        tmp.enableAutoSizing =
            false;

        tmp.textWrappingMode =
            TextWrappingModes.NoWrap;

        tmp.extraPadding =
            true;

        tmp.raycastTarget =
            false;

        return tmp;
    }

    //=============================================================
    // CREATE BUTTON
    //=============================================================

    private static GameObject CreateButton(
        string objectName,
        Transform parent,
        Sprite sprite,
        Color color,
        float width,
        float height)
    {
        GameObject buttonObject =
            CreateUIObject(
                objectName,
                parent
            );

        RectTransform rect =
            buttonObject.GetComponent<RectTransform>();

        rect.sizeDelta =
            new Vector2(
                width,
                height
            );

        Image image =
            buttonObject.AddComponent<Image>();

        image.sprite =
            sprite;

        image.type =
            Image.Type.Sliced;

        image.color =
            color;

        image.raycastTarget =
            true;

        Button button =
            buttonObject.AddComponent<Button>();

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            color;

        colors.highlightedColor =
            new Color(
                Mathf.Min(
                    color.r + 0.08f,
                    1f
                ),
                Mathf.Min(
                    color.g + 0.08f,
                    1f
                ),
                Mathf.Min(
                    color.b + 0.08f,
                    1f
                ),
                color.a
            );

        colors.pressedColor =
            new Color(
                color.r * 0.85f,
                color.g * 0.85f,
                color.b * 0.85f,
                color.a
            );

        colors.selectedColor =
            colors.highlightedColor;

        colors.disabledColor =
            new Color(
                color.r,
                color.g,
                color.b,
                0.35f
            );

        colors.fadeDuration =
            0.08f;

        button.colors =
            colors;

        button.interactable =
            true;

        SetupShadow(
            buttonObject,
            new Color32(
                0,
                0,
                0,
                70
            ),
            new Vector2(
                0f,
                -2f
            )
        );

        return buttonObject;
    }

    //=============================================================
    // CREATE ACCENT
    //=============================================================

    private static void CreateAccent(
        Transform parent,
        string objectName,
        Vector2 size,
        Vector2 position)
    {
        GameObject accent =
            CreateUIObject(
                objectName,
                parent
            );

        RectTransform rect =
            accent.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(0f, 1f);

        rect.pivot =
            new Vector2(0f, 1f);

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            size;

        Image image =
            accent.AddComponent<Image>();

        image.color =
            Gold;

        image.raycastTarget =
            false;

        SetupShadow(
            accent,
            new Color32(
                255,
                203,
                45,
                60
            ),
            new Vector2(
                0f,
                -1f
            )
        );
    }

    //=============================================================
    // VERTICAL ACCENT
    //=============================================================

    private static void CreateVerticalAccent(
        Transform parent)
    {
        GameObject accent =
            CreateUIObject(
                "MissionAccent",
                parent
            );

        RectTransform rect =
            accent.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(
                0f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0f,
                0.5f
            );

        rect.anchoredPosition =
            Vector2.zero;

        rect.sizeDelta =
            new Vector2(
                6f,
                92f
            );

        Image image =
            accent.AddComponent<Image>();

        image.color =
            Gold;

        image.raycastTarget =
            false;
    }

    //=============================================================
    // SHADOW
    //=============================================================

    private static void SetupShadow(
        GameObject target,
        Color color,
        Vector2 distance)
    {
        Shadow shadow =
            target.GetComponent<Shadow>();

        if (shadow == null)
        {
            shadow =
                target.AddComponent<Shadow>();
        }

        shadow.effectColor =
            color;

        shadow.effectDistance =
            distance;

        shadow.useGraphicAlpha =
            true;

        shadow.enabled =
            true;

        EditorUtility.SetDirty(
            shadow
        );
    }

    //=============================================================
    // STRETCH FULL
    //=============================================================

    private static void StretchFull(
        RectTransform rect)
    {
        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );
    }

    //=============================================================
    // ROUNDED RECT
    //=============================================================

    private static Sprite PrepareRoundedRectSprite()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                RoundedRectName +
                " t:Texture2D"
            );

        if (guids == null ||
            guids.Length == 0)
        {
            return null;
        }

        string assetPath =
            null;

        //=========================================================
        // FIND EXACT FILE
        //=========================================================

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            string fileName =
                Path.GetFileNameWithoutExtension(
                    path
                );

            if (fileName.ToLower() ==
                RoundedRectName.ToLower())
            {
                assetPath =
                    path;

                break;
            }
        }

        if (string.IsNullOrEmpty(
            assetPath))
        {
            assetPath =
                AssetDatabase.GUIDToAssetPath(
                    guids[0]
                );
        }

        //=========================================================
        // IMPORTER
        //=========================================================

        TextureImporter importer =
            AssetImporter.GetAtPath(
                assetPath
            ) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError(
                "[Mission UI] Không lấy được TextureImporter."
            );

            return null;
        }

        importer.textureType =
            TextureImporterType.Sprite;

        importer.spriteImportMode =
            SpriteImportMode.Single;

        //=========================================================
        // SETTINGS
        //=========================================================

        TextureImporterSettings settings =
            new TextureImporterSettings();

        importer.ReadTextureSettings(
            settings
        );

        settings.spriteMeshType =
            SpriteMeshType.FullRect;

        settings.spriteBorder =
            new Vector4(
                42f,
                42f,
                42f,
                42f
            );

        importer.SetTextureSettings(
            settings
        );

        EditorUtility.SetDirty(
            importer
        );

        importer.SaveAndReimport();

        //=========================================================
        // LOAD
        //=========================================================

        Sprite sprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(
                assetPath
            );

        if (sprite == null)
        {
            Debug.LogError(
                "[Mission UI] Không load được rounded_rect."
            );
        }

        return sprite;
    }
}

#endif