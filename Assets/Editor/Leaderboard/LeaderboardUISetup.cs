#if UNITY_EDITOR

using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class LeaderboardUISetup
{
    // ============================================================
    // PATHS
    // ============================================================

    private const string PrefabFolder =
        "Assets/Prefabs/UI/Leaderboard";

    private const string RowPrefabPath =
        PrefabFolder + "/LeaderboardRow.prefab";

    // ============================================================
    // COLORS
    // ============================================================

    private static readonly Color OverlayColor =
        Hex("#05070A", 0.94f);

    private static readonly Color PanelColor =
        Hex("#10151A", 1f);

    private static readonly Color SurfaceColor =
        Hex("#171E24", 1f);

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

    private static readonly Color MutedTextColor =
        Hex("#7D8A90", 1f);

    private static readonly Color BlackColor =
        Hex("#020304", 1f);

    // ============================================================
    // 01 - CREATE ROW PREFAB
    // ============================================================

    [MenuItem(
        "LEAD KHÔNG PHANH/UI/Leaderboard/01 - Create Row Prefab"
    )]
    public static void CreateLeaderboardRowPrefab()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/UI");
        EnsureFolder(PrefabFolder);

        GameObject oldPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                RowPrefabPath
            );

        if (oldPrefab != null)
        {
            AssetDatabase.DeleteAsset(
                RowPrefabPath
            );

            AssetDatabase.Refresh();
        }

        // ========================================================
        // ROOT
        // ========================================================

        GameObject row =
            new GameObject(
                "LeaderboardRow",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(LeaderboardRowUI)
            );

        RectTransform rowRect =
            row.GetComponent<RectTransform>();

        rowRect.anchorMin =
            new Vector2(0f, 0f);

        rowRect.anchorMax =
            new Vector2(1f, 0f);

        rowRect.pivot =
            new Vector2(0.5f, 0.5f);

        rowRect.sizeDelta =
            new Vector2(0f, 64f);

        rowRect.anchoredPosition =
            Vector2.zero;

        rowRect.localScale =
            Vector3.one;

        // ========================================================
        // IMAGE
        // ========================================================

        Image rowImage =
            row.GetComponent<Image>();

        rowImage.color =
            SurfaceColor;

        rowImage.raycastTarget =
            false;

        // ========================================================
        // LAYOUT
        // ========================================================

        LayoutElement layout =
            row.GetComponent<LayoutElement>();

        layout.minWidth =
            0f;

        layout.minHeight =
            64f;

        layout.preferredWidth =
            -1f;

        layout.preferredHeight =
            64f;

        layout.flexibleWidth =
            0f;

        layout.flexibleHeight =
            0f;

        // ========================================================
        // ACCENT
        // ========================================================

        GameObject accent =
            CreateImage(
                "Accent",
                row.transform,
                GoldColor
            );

        SetLeftStretch(
            accent.GetComponent<RectTransform>(),
            0f,
            0f,
            4f
        );

        // ========================================================
        // RANK
        // ========================================================

        GameObject rankObject =
            CreateTMP(
                "Rank",
                row.transform,
                "#1",
                22f,
                FontStyles.Bold,
                GoldColor
            );

        RectTransform rankRect =
            rankObject.GetComponent<RectTransform>();

        rankRect.anchorMin =
            new Vector2(0f, 0f);

        rankRect.anchorMax =
            new Vector2(0f, 1f);

        rankRect.pivot =
            new Vector2(0f, 0.5f);

        rankRect.sizeDelta =
            new Vector2(100f, 0f);

        rankRect.anchoredPosition =
            new Vector2(12f, 0f);

        TMP_Text rankText =
            rankObject.GetComponent<TMP_Text>();

        rankText.alignment =
            TextAlignmentOptions.Center;

        rankText.textWrappingMode =
            TextWrappingModes.NoWrap;

        rankText.overflowMode =
            TextOverflowModes.Overflow;

        // ========================================================
        // NAME
        // ========================================================

        GameObject nameObject =
            CreateTMP(
                "Name",
                row.transform,
                "PLAYER",
                21f,
                FontStyles.Bold,
                WhiteColor
            );

        RectTransform nameRect =
            nameObject.GetComponent<RectTransform>();

        nameRect.anchorMin =
            new Vector2(0f, 0f);

        nameRect.anchorMax =
            new Vector2(1f, 1f);

        nameRect.pivot =
            new Vector2(0f, 0.5f);

        nameRect.offsetMin =
            new Vector2(115f, 0f);

        nameRect.offsetMax =
            new Vector2(-210f, 0f);

        TMP_Text nameText =
            nameObject.GetComponent<TMP_Text>();

        nameText.alignment =
            TextAlignmentOptions.MidlineLeft;

        nameText.textWrappingMode =
            TextWrappingModes.NoWrap;

        nameText.overflowMode =
            TextOverflowModes.Ellipsis;

        // ========================================================
        // SCORE
        // ========================================================

        GameObject scoreObject =
            CreateTMP(
                "Score",
                row.transform,
                "0",
                21f,
                FontStyles.Bold,
                CyanColor
            );

        RectTransform scoreRect =
            scoreObject.GetComponent<RectTransform>();

        scoreRect.anchorMin =
            new Vector2(1f, 0f);

        scoreRect.anchorMax =
            new Vector2(1f, 1f);

        scoreRect.pivot =
            new Vector2(1f, 0.5f);

        scoreRect.sizeDelta =
            new Vector2(180f, 0f);

        scoreRect.anchoredPosition =
            new Vector2(-24f, 0f);

        TMP_Text scoreText =
            scoreObject.GetComponent<TMP_Text>();

        scoreText.alignment =
            TextAlignmentOptions.MidlineRight;

        scoreText.textWrappingMode =
            TextWrappingModes.NoWrap;

        scoreText.overflowMode =
            TextOverflowModes.Overflow;

        // ========================================================
        // WIRE ROW
        // ========================================================

        LeaderboardRowUI rowUI =
            row.GetComponent<LeaderboardRowUI>();

        SerializedObject serializedRow =
            new SerializedObject(rowUI);

        SetObjectReference(
            serializedRow,
            "rankText",
            rankText
        );

        SetObjectReference(
            serializedRow,
            "nameText",
            nameText
        );

        SetObjectReference(
            serializedRow,
            "scoreText",
            scoreText
        );

        serializedRow.ApplyModifiedPropertiesWithoutUndo();

        // ========================================================
        // SAVE PREFAB
        // ========================================================

        GameObject prefab =
            PrefabUtility.SaveAsPrefabAsset(
                row,
                RowPrefabPath
            );

        if (prefab == null)
        {
            Debug.LogError(
                "[LeaderboardUISetup] " +
                "Không thể tạo LeaderboardRow.prefab."
            );

            Object.DestroyImmediate(row);

            return;
        }

        Object.DestroyImmediate(row);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        LeaderboardRowUI prefabComponent =
            prefab.GetComponent<LeaderboardRowUI>();

        if (prefabComponent == null)
        {
            Debug.LogError(
                "[LeaderboardUISetup] " +
                "LeaderboardRow.prefab không có " +
                "LeaderboardRowUI component."
            );

            return;
        }

        Selection.activeObject =
            prefab;

        EditorGUIUtility.PingObject(
            prefab
        );

        Debug.Log(
            "[LeaderboardUISetup] " +
            "SUCCESS: LeaderboardRow.prefab đã được tạo."
        );
    }

    // ============================================================
    // 02 - CREATE LEADERBOARD UI
    // ============================================================

    [MenuItem(
        "LEAD KHÔNG PHANH/UI/Leaderboard/02 - Create Leaderboard UI"
    )]
    public static void CreateLeaderboardUI()
    {
        EnsureEventSystem();

        // ========================================================
        // REMOVE OLD CANVAS
        // ========================================================

        GameObject oldCanvas =
            GameObject.Find(
                "LeaderboardCanvas"
            );

        if (oldCanvas != null)
        {
            Object.DestroyImmediate(
                oldCanvas
            );
        }

        // ========================================================
        // CANVAS
        // ========================================================

        GameObject canvasObject =
            new GameObject(
                "LeaderboardCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder =
            110;

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

        // ========================================================
        // SAFE AREA
        // ========================================================

        GameObject safeArea =
            CreateUIObject(
                "SafeArea",
                canvasObject.transform
            );

        SetStretch(
            safeArea.GetComponent<RectTransform>(),
            0f,
            0f,
            0f,
            0f
        );

        // ========================================================
        // PANEL
        // ========================================================

        GameObject leaderboardPanel =
            CreateImage(
                "LeaderboardPanel",
                safeArea.transform,
                OverlayColor
            );

        SetStretch(
            leaderboardPanel.GetComponent<RectTransform>(),
            0f,
            0f,
            0f,
            0f
        );

        // ========================================================
        // CONTENT
        // ========================================================

        GameObject content =
            CreateUIObject(
                "Content",
                leaderboardPanel.transform
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

        // ========================================================
        // SHADOW
        // ========================================================

        GameObject shadow =
            CreateImage(
                "PanelShadow",
                content.transform,
                new Color(
                    0f,
                    0f,
                    0f,
                    0.65f
                )
            );

        SetRect(
            shadow.GetComponent<RectTransform>(),
            14f,
            -14f,
            1120f,
            900f
        );

        // ========================================================
        // PANEL SURFACE
        // ========================================================

        GameObject panelSurface =
            CreateImage(
                "PanelSurface",
                content.transform,
                PanelColor
            );

        SetRect(
            panelSurface.GetComponent<RectTransform>(),
            0f,
            0f,
            1120f,
            900f
        );

        // ========================================================
        // BORDER
        // ========================================================

        GameObject border =
            CreateImage(
                "Border",
                content.transform,
                GoldDarkColor
            );

        SetRect(
            border.GetComponent<RectTransform>(),
            0f,
            0f,
            1120f,
            900f
        );

        GameObject borderInner =
            CreateImage(
                "BorderInner",
                border.transform,
                PanelColor
            );

        SetStretch(
            borderInner.GetComponent<RectTransform>(),
            2f,
            2f,
            2f,
            2f
        );

        // ========================================================
        // TOP GOLD
        // ========================================================

        GameObject topGold =
            CreateImage(
                "TopGoldLine",
                content.transform,
                GoldColor
            );

        SetTopStretch(
            topGold.GetComponent<RectTransform>(),
            0f,
            0f,
            5f
        );

        // ========================================================
        // HEADER
        // ========================================================

        GameObject header =
            CreateUIObject(
                "Header",
                content.transform
            );

        SetTopStretch(
            header.GetComponent<RectTransform>(),
            30f,
            30f,
            90f
        );

        GameObject headerAccent =
            CreateImage(
                "HeaderAccent",
                header.transform,
                GoldColor
            );

        SetLeftStretch(
            headerAccent.GetComponent<RectTransform>(),
            0f,
            0f,
            5f
        );

        // ========================================================
        // TITLE
        // ========================================================

        GameObject title =
            CreateTMP(
                "Title",
                header.transform,
                "BẢNG XẾP HẠNG",
                40f,
                FontStyles.Bold,
                WhiteColor
            );

        SetLeftStretch(
            title.GetComponent<RectTransform>(),
            24f,
            0f,
            500f
        );

        title.GetComponent<TMP_Text>().alignment =
            TextAlignmentOptions.MidlineLeft;

        // ========================================================
        // SUBTITLE
        // ========================================================

        GameObject subtitle =
            CreateTMP(
                "Subtitle",
                header.transform,
                "NHỮNG TAY LÁI KHÔNG PHANH",
                14f,
                FontStyles.Bold,
                MutedTextColor
            );

        RectTransform subtitleRect =
            subtitle.GetComponent<RectTransform>();

        subtitleRect.anchorMin =
            new Vector2(0f, 0f);

        subtitleRect.anchorMax =
            new Vector2(0f, 0f);

        subtitleRect.pivot =
            new Vector2(0f, 0f);

        subtitleRect.sizeDelta =
            new Vector2(500f, 25f);

        subtitleRect.anchoredPosition =
            new Vector2(26f, 8f);

        subtitle.GetComponent<TMP_Text>().alignment =
            TextAlignmentOptions.Left;

        // ========================================================
        // CLOSE BUTTON
        // ========================================================

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
            new Vector2(-6f, 0f);

        GameObject closeAccent =
            CreateImage(
                "Accent",
                closeButton.transform,
                RedColor
            );

        SetLeftStretch(
            closeAccent.GetComponent<RectTransform>(),
            0f,
            0f,
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
            0f,
            0f,
            0f,
            0f
        );

        closeText.GetComponent<TMP_Text>().alignment =
            TextAlignmentOptions.Center;

        // ========================================================
        // COLUMN HEADER
        // ========================================================

        GameObject columnHeader =
            CreateUIObject(
                "ColumnHeader",
                content.transform
            );

        SetTopStretch(
            columnHeader.GetComponent<RectTransform>(),
            140f,
            36f,
            45f
        );

        CreateColumnText(
            "Rank",
            columnHeader.transform,
            "HẠNG",
            0f,
            0.18f,
            TextAlignmentOptions.MidlineLeft
        );

        CreateColumnText(
            "Player",
            columnHeader.transform,
            "NGƯỜI CHƠI",
            0.18f,
            0.70f,
            TextAlignmentOptions.MidlineLeft
        );

        CreateColumnText(
            "Score",
            columnHeader.transform,
            "ĐIỂM",
            0.70f,
            1f,
            TextAlignmentOptions.MidlineRight
        );

        // ========================================================
        // SCROLL VIEW
        // ========================================================

        GameObject scrollView =
            CreateUIObject(
                "ScrollView",
                content.transform
            );

        SetTopStretch(
            scrollView.GetComponent<RectTransform>(),
            195f,
            36f,
            535f
        );

        Image scrollBackground =
            scrollView.AddComponent<Image>();

        scrollBackground.color =
            new Color(
                1f,
                1f,
                1f,
                0.025f
            );

        scrollBackground.raycastTarget =
            false;

        ScrollRect scrollRectComponent =
            scrollView.AddComponent<ScrollRect>();

        scrollRectComponent.horizontal =
            false;

        scrollRectComponent.vertical =
            true;

        scrollRectComponent.movementType =
            ScrollRect.MovementType.Clamped;

        scrollRectComponent.scrollSensitivity =
            35f;

        scrollRectComponent.inertia =
            true;

        scrollRectComponent.decelerationRate =
            0.135f;

        // ========================================================
        // VIEWPORT
        // ========================================================

        GameObject viewport =
            CreateUIObject(
                "Viewport",
                scrollView.transform
            );

        RectTransform viewportRect =
            viewport.GetComponent<RectTransform>();

        SetStretch(
            viewportRect,
            0f,
            0f,
            0f,
            0f
        );

        Image viewportImage =
            viewport.AddComponent<Image>();

        viewportImage.color =
            new Color(
                0f,
                0f,
                0f,
                0f
            );

        viewportImage.raycastTarget =
            true;

        RectMask2D rectMask =
            viewport.AddComponent<RectMask2D>();

        scrollRectComponent.viewport =
            viewportRect;

        // ========================================================
        // CONTENT ROOT
        // ========================================================

        GameObject contentRoot =
            CreateUIObject(
                "ContentRoot",
                viewport.transform
            );

        RectTransform contentRootRect =
            contentRoot.GetComponent<RectTransform>();

        contentRootRect.anchorMin =
            new Vector2(0f, 1f);

        contentRootRect.anchorMax =
            new Vector2(1f, 1f);

        contentRootRect.pivot =
            new Vector2(0.5f, 1f);

        contentRootRect.anchoredPosition =
            Vector2.zero;

        contentRootRect.sizeDelta =
            new Vector2(0f, 0f);

        contentRootRect.localScale =
            Vector3.one;

        contentRootRect.localRotation =
            Quaternion.identity;

        // ========================================================
        // VERTICAL LAYOUT
        // ========================================================

        VerticalLayoutGroup verticalLayout =
            contentRoot.AddComponent<
                VerticalLayoutGroup
            >();

        verticalLayout.spacing =
            6f;

        verticalLayout.padding =
            new RectOffset(
                0,
                0,
                0,
                0
            );

        verticalLayout.childAlignment =
            TextAnchor.UpperCenter;

        verticalLayout.childControlWidth =
            true;

        verticalLayout.childControlHeight =
            true;

        verticalLayout.childForceExpandWidth =
            true;

        verticalLayout.childForceExpandHeight =
            false;

        // ========================================================
        // CONTENT SIZE
        // ========================================================

        ContentSizeFitter contentFitter =
            contentRoot.AddComponent<
                ContentSizeFitter
            >();

        contentFitter.horizontalFit =
            ContentSizeFitter.FitMode.Unconstrained;

        contentFitter.verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        scrollRectComponent.content =
            contentRootRect;

        // ========================================================
        // STATUS
        // ========================================================

        GameObject statusTextObject =
            CreateTMP(
                "StatusText",
                content.transform,
                "",
                22f,
                FontStyles.Bold,
                MutedTextColor
            );

        RectTransform statusRect =
            statusTextObject.GetComponent<RectTransform>();

        statusRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        statusRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        statusRect.pivot =
            new Vector2(0.5f, 0.5f);

        statusRect.sizeDelta =
            new Vector2(700f, 60f);

        statusRect.anchoredPosition =
            Vector2.zero;

        statusTextObject.GetComponent<TMP_Text>()
            .alignment =
            TextAlignmentOptions.Center;

        // ========================================================
        // EMPTY STATE
        // ========================================================

        GameObject emptyState =
            CreateTMP(
                "EmptyState",
                content.transform,
                "CHƯA CÓ DỮ LIỆU BẢNG XẾP HẠNG",
                22f,
                FontStyles.Bold,
                MutedTextColor
            );

        RectTransform emptyRect =
            emptyState.GetComponent<RectTransform>();

        emptyRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        emptyRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        emptyRect.pivot =
            new Vector2(0.5f, 0.5f);

        emptyRect.sizeDelta =
            new Vector2(700f, 60f);

        emptyRect.anchoredPosition =
            new Vector2(0f, -50f);

        emptyState.GetComponent<TMP_Text>()
            .alignment =
            TextAlignmentOptions.Center;

        // ========================================================
        // MY RANK PANEL
        // ========================================================

        GameObject myRankPanel =
            CreateImage(
                "MyRankPanel",
                content.transform,
                SurfaceColor
            );

        RectTransform myRankRect =
            myRankPanel.GetComponent<RectTransform>();

        myRankRect.anchorMin =
            new Vector2(0f, 0f);

        myRankRect.anchorMax =
            new Vector2(1f, 0f);

        myRankRect.pivot =
            new Vector2(0.5f, 0f);

        myRankRect.offsetMin =
            new Vector2(36f, 34f);

        myRankRect.offsetMax =
            new Vector2(-36f, 124f);

        // ========================================================
        // MY RANK ACCENT
        // ========================================================

        GameObject myRankAccent =
            CreateImage(
                "Accent",
                myRankPanel.transform,
                GoldColor
            );

        SetLeftStretch(
            myRankAccent.GetComponent<RectTransform>(),
            0f,
            0f,
            5f
        );

        // ========================================================
        // MY RANK
        // ========================================================

        GameObject myRankTextObject =
            CreateTMP(
                "MyRankText",
                myRankPanel.transform,
                "#--",
                23f,
                FontStyles.Bold,
                GoldColor
            );

        SetLeftStretch(
            myRankTextObject.GetComponent<RectTransform>(),
            25f,
            0f,
            120f
        );

        myRankTextObject.GetComponent<TMP_Text>()
            .alignment =
            TextAlignmentOptions.MidlineLeft;

        // ========================================================
        // MY NAME
        // ========================================================

        GameObject myNameTextObject =
            CreateTMP(
                "MyNameText",
                myRankPanel.transform,
                "PLAYER",
                22f,
                FontStyles.Bold,
                WhiteColor
            );

        RectTransform myNameRect =
            myNameTextObject.GetComponent<RectTransform>();

        myNameRect.anchorMin =
            new Vector2(0f, 0f);

        myNameRect.anchorMax =
            new Vector2(0.65f, 1f);

        myNameRect.offsetMin =
            new Vector2(145f, 0f);

        myNameRect.offsetMax =
            Vector2.zero;

        myNameTextObject.GetComponent<TMP_Text>()
            .alignment =
            TextAlignmentOptions.MidlineLeft;

        // ========================================================
        // MY SCORE
        // ========================================================

        GameObject myScoreTextObject =
            CreateTMP(
                "MyScoreText",
                myRankPanel.transform,
                "0",
                23f,
                FontStyles.Bold,
                CyanColor
            );

        RectTransform myScoreRect =
            myScoreTextObject.GetComponent<RectTransform>();

        myScoreRect.anchorMin =
            new Vector2(0.65f, 0f);

        myScoreRect.anchorMax =
            new Vector2(1f, 1f);

        myScoreRect.offsetMin =
            Vector2.zero;

        myScoreRect.offsetMax =
            new Vector2(-25f, 0f);

        myScoreTextObject.GetComponent<TMP_Text>()
            .alignment =
            TextAlignmentOptions.MidlineRight;

        // ========================================================
        // ADD LEADERBOARD UI
        // ========================================================

        LeaderboardUI leaderboardUI =
            leaderboardPanel.AddComponent<
                LeaderboardUI
            >();

        // ========================================================
        // LOAD ROW PREFAB
        // ========================================================

        GameObject rowPrefabAsset =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                RowPrefabPath
            );

        LeaderboardRowUI rowPrefabComponent =
            null;

        if (rowPrefabAsset != null)
        {
            rowPrefabComponent =
                rowPrefabAsset.GetComponent<
                    LeaderboardRowUI
                >();
        }

        if (rowPrefabComponent == null)
        {
            Debug.LogError(
                "[LeaderboardUISetup] " +
                "Không tìm thấy LeaderboardRowUI " +
                "trên LeaderboardRow.prefab."
            );
        }

        // ========================================================
        // WIRE LEADERBOARD UI
        // ========================================================

        SerializedObject serializedUI =
            new SerializedObject(
                leaderboardUI
            );

        SetObjectReference(
            serializedUI,
            "leaderboardPanel",
            leaderboardPanel
        );

        SetObjectReference(
            serializedUI,
            "closeButton",
            closeButton.GetComponent<Button>()
        );

        SetObjectReference(
            serializedUI,
            "titleText",
            title.GetComponent<TMP_Text>()
        );

        SetObjectReference(
            serializedUI,
            "statusText",
            statusTextObject.GetComponent<TMP_Text>()
        );

        SetObjectReference(
            serializedUI,
            "contentRoot",
            contentRootRect
        );

        // ========================================================
        // IMPORTANT:
        // LeaderboardUI.rowPrefab là LeaderboardRowUI,
        // KHÔNG phải GameObject.
        // ========================================================

        SetObjectReference(
            serializedUI,
            "rowPrefab",
            rowPrefabComponent
        );

        SetObjectReference(
            serializedUI,
            "myRankPanel",
            myRankPanel
        );

        SetObjectReference(
            serializedUI,
            "myRankText",
            myRankTextObject.GetComponent<TMP_Text>()
        );

        SetObjectReference(
            serializedUI,
            "myNameText",
            myNameTextObject.GetComponent<TMP_Text>()
        );

        SetObjectReference(
            serializedUI,
            "myScoreText",
            myScoreTextObject.GetComponent<TMP_Text>()
        );

        SetObjectReference(
            serializedUI,
            "emptyState",
            emptyState
        );

        serializedUI.ApplyModifiedPropertiesWithoutUndo();

        // ========================================================
        // INITIAL STATE
        // ========================================================

        leaderboardPanel.SetActive(false);

        contentRoot.SetActive(true);

        emptyState.SetActive(false);

        myRankPanel.SetActive(false);

        // ========================================================
        // EDITOR DIRTY
        // ========================================================

        EditorUtility.SetDirty(
            leaderboardUI
        );

        EditorUtility.SetDirty(
            canvasObject
        );

        EditorSceneManager.MarkSceneDirty(
            canvasObject.scene
        );

        // ========================================================
        // SAVE
        // ========================================================

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject =
            canvasObject;

        EditorGUIUtility.PingObject(
            canvasObject
        );

        Debug.Log(
            "[LeaderboardUISetup] " +
            "SUCCESS: Leaderboard UI created."
        );

        Debug.Log(
            "[LeaderboardUISetup] " +
            "rowPrefab = " +
            (
                rowPrefabComponent != null
                    ? rowPrefabComponent.name
                    : "NULL"
            )
        );

        Debug.Log(
            "[LeaderboardUISetup] " +
            "contentRoot = " +
            contentRootRect.name
        );

        Debug.Log(
            "[LeaderboardUISetup] " +
            "Scene đã được đánh dấu Dirty. " +
            "Nhấn Ctrl+S để lưu."
        );
    }

    // ============================================================
    // COLUMN TEXT
    // ============================================================

    private static void CreateColumnText(
        string objectName,
        Transform parent,
        string text,
        float minX,
        float maxX,
        TextAlignmentOptions alignment
    )
    {
        GameObject obj =
            CreateTMP(
                objectName,
                parent,
                text,
                15f,
                FontStyles.Bold,
                MutedTextColor
            );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(minX, 0f);

        rect.anchorMax =
            new Vector2(maxX, 1f);

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        obj.GetComponent<TMP_Text>()
            .alignment =
            alignment;
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
    // CREATE IMAGE
    // ============================================================

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

        image.color =
            color;

        image.raycastTarget =
            false;

        return obj;
    }

    // ============================================================
    // CREATE BUTTON
    // ============================================================

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

        image.color =
            color;

        image.raycastTarget =
            true;

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

        colors.disabledColor =
            new Color(
                color.r,
                color.g,
                color.b,
                0.45f
            );

        button.colors =
            colors;

        button.transition =
            Selectable.Transition.ColorTint;

        return obj;
    }

    // ============================================================
    // CREATE TMP
    // ============================================================

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

        tmp.text =
            text;

        tmp.fontSize =
            fontSize;

        tmp.fontStyle =
            style;

        tmp.color =
            color;

        tmp.alignment =
            TextAlignmentOptions.Left;

        tmp.textWrappingMode =
            TextWrappingModes.NoWrap;

        tmp.overflowMode =
            TextOverflowModes.Ellipsis;

        tmp.raycastTarget =
            false;

        return obj;
    }

    // ============================================================
    // RECT - STRETCH
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
            new Vector2(
                left,
                bottom
            );

        rect.offsetMax =
            new Vector2(
                -right,
                -top
            );
    }

    // ============================================================
    // RECT - CENTER
    // ============================================================

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
            new Vector2(
                width,
                height
            );

        rect.anchoredPosition =
            new Vector2(
                x,
                y
            );
    }

    // ============================================================
    // RECT - TOP
    // ============================================================

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

    // ============================================================
    // RECT - LEFT
    // ============================================================

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
                "[LeaderboardUISetup] " +
                "Không tìm thấy SerializedProperty: " +
                propertyName
            );

            return;
        }

        if (property.propertyType !=
            SerializedPropertyType.ObjectReference)
        {
            Debug.LogError(
                "[LeaderboardUISetup] " +
                "Property không phải ObjectReference: " +
                propertyName
            );

            return;
        }

        property.objectReferenceValue =
            value;
    }

    // ============================================================
    // EVENT SYSTEM
    // ============================================================

    private static void EnsureEventSystem()
    {
        EventSystem existing =
            Object.FindFirstObjectByType<EventSystem>();

        if (existing != null)
        {
            return;
        }

        GameObject eventSystem =
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule)
            );

        eventSystem.transform.SetAsLastSibling();

        Debug.Log(
            "[LeaderboardUISetup] " +
            "EventSystem đã được tạo."
        );
    }

    // ============================================================
    // FOLDER
    // ============================================================

    private static void EnsureFolder(
        string folderPath
    )
    {
        folderPath =
            folderPath.Replace(
                "\\",
                "/"
            );

        if (AssetDatabase.IsValidFolder(
                folderPath))
        {
            return;
        }

        string parent =
            Path.GetDirectoryName(
                folderPath
            )?.Replace(
                "\\",
                "/"
            );

        string folderName =
            Path.GetFileName(
                folderPath
            );

        if (string.IsNullOrEmpty(parent) ||
            string.IsNullOrEmpty(folderName))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(
                parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(
            parent,
            folderName
        );
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
            color.a =
                alpha;

            return color;
        }

        return Color.white;
    }
}

#endif