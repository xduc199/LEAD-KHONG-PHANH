
#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameOverUISetup
{
    // =====================================================================
    // NAMES
    // =====================================================================

    private const string CanvasName = "GameOverCanvas";
    private const string RootName = "GameOverUI";

    private const string BackdropName = "NenToi";
    private const string RevivePanelName = "KhuVucHoiSinh";
    private const string GameOverPanelName = "GameOverPanel";

    // =====================================================================
    // COLORS
    // =====================================================================

    // ---------------------------------------------------------------------
    // BACKGROUND
    // ---------------------------------------------------------------------

    private static readonly Color BackgroundColor =
        new Color(
            0.002f,
            0.004f,
            0.008f,
            0.965f
        );

    // ---------------------------------------------------------------------
    // PANELS
    // ---------------------------------------------------------------------

    private static readonly Color PanelColor =
        new Color(
            0.014f,
            0.025f,
            0.042f,
            0.995f
        );

    private static readonly Color PanelBorderColor =
        new Color(
            0.075f,
            0.190f,
            0.245f,
            1f
        );

    // ---------------------------------------------------------------------
    // CARDS
    // ---------------------------------------------------------------------

    private static readonly Color CardColor =
        new Color(
            0.026f,
            0.050f,
            0.075f,
            1f
        );

    private static readonly Color CardBorderColor =
        new Color(
            0.080f,
            0.180f,
            0.230f,
            1f
        );
    private static readonly Color BorderColor =
    new Color(0.060f, 0.160f, 0.210f, 1f);
    // ---------------------------------------------------------------------
    // ACCENT COLORS
    // ---------------------------------------------------------------------

    private static readonly Color Cyan =
        new Color(
            0.050f,
            0.850f,
            1.000f,
            1f
        );

    private static readonly Color Teal =
        new Color(
            0.050f,
            0.950f,
            0.760f,
            1f
        );

    private static readonly Color Gem =
        new Color(
            0.760f,
            0.500f,
            1.000f,
            1f
        );

    private static readonly Color Gold =
        new Color(
            1.000f,
            0.800f,
            0.180f,
            1f
        );

    private static readonly Color Danger =
        new Color(
            1.000f,
            0.320f,
            0.320f,
            1f
        );

    // ---------------------------------------------------------------------
    // BUTTON COLORS
    // ---------------------------------------------------------------------

    private static readonly Color ButtonNormal =
        new Color(
            0.035f,
            0.080f,
            0.115f,
            1f
        );

    private static readonly Color ButtonDisabled =
        new Color(
            0.075f,
            0.090f,
            0.105f,
            1f
        );

    // ---------------------------------------------------------------------
    // TEXT COLORS
    // ---------------------------------------------------------------------

    private static readonly Color White =
        new Color(
            0.960f,
            0.985f,
            1.000f,
            1f
        );

    private static readonly Color SoftText =
        new Color(
            0.720f,
            0.805f,
            0.875f,
            1f
        );

    private static readonly Color MutedText =
        new Color(
            0.520f,
            0.620f,
            0.700f,
            1f
        );

    // =====================================================================
    // MENU
    // =====================================================================

    [MenuItem(
        "LEAD KHONG PHANH/Game Over/Tạo lại giao diện Game Over"
    )]
    private static void CreateGameOverUI()
    {
        Scene scene =
            SceneManager.GetActiveScene();

        if (!scene.isLoaded)
        {
            Debug.LogError(
                "[GameOverUISetup] Không có Scene đang mở."
            );

            return;
        }

        bool confirm =
            EditorUtility.DisplayDialog(
                "TẠO LẠI GAME OVER",
                "GameOverCanvas hiện tại sẽ bị xóa và tạo lại hoàn toàn.\n\n" +
                "Kiến trúc mới:\n" +
                "GameOverUI\n" +
                "├── NenToi\n" +
                "├── KhuVucHoiSinh\n" +
                "└── GameOverPanel",
                "TẠO LẠI",
                "HỦY"
            );

        if (!confirm)
            return;

        DestroyOldHierarchy();

        Canvas canvas =
            CreateCanvas();

        GameObject root =
            CreateRoot(
                canvas.transform
            );

        GameObject backdrop =
            CreateBackdrop(
                root.transform
            );

        GameObject revivePanel =
            CreateRevivePanel(
                root.transform
            );

        GameObject gameOverPanel =
            CreateGameOverPanel(
                root.transform
            );

        GameOverUIController controller =
            root.AddComponent<GameOverUIController>();

        SerializedObject serialized =
            new SerializedObject(controller);

        // ================================================================
        // GAME OVER REFERENCES
        // ================================================================

        SetReference(
            serialized,
            "gameOverPanel",
            gameOverPanel
        );

        SetReference(
            serialized,
            "gameOverCanvasGroup",
            gameOverPanel.GetComponent<CanvasGroup>()
        );

        SetReference(
            serialized,
            "gameOverPanelRect",
            gameOverPanel.GetComponent<RectTransform>()
        );

        SetReference(
            serialized,
            "scoreValue",
            FindText(
                gameOverPanel,
                "GiaTriDiem"
            )
        );

        SetReference(
            serialized,
            "highScoreValue",
            FindText(
                gameOverPanel,
                "GiaTriKyLuc"
            )
        );

        SetReference(
            serialized,
            "coinValue",
            FindText(
                gameOverPanel,
                "GiaTriXu"
            )
        );

        SetReference(
            serialized,
            "expRewardValue",
            FindText(
                gameOverPanel,
                "GiaTriEXP"
            )
        );

        SetReference(
            serialized,
            "levelValue",
            FindText(
                gameOverPanel,
                "GiaTriCapDo"
            )
        );

        SetReference(
            serialized,
            "levelUpValue",
            FindText(
                gameOverPanel,
                "GiaTriLenCap"
            )
        );

        SetReference(
            serialized,
            "retryButton",
            FindButton(
                gameOverPanel,
                "NutThuLai"
            )
        );

        SetReference(
            serialized,
            "mainMenuButton",
            FindButton(
                gameOverPanel,
                "NutManHinhChinh"
            )
        );

        // ================================================================
        // REVIVE REFERENCES
        // ================================================================

        SetReference(
            serialized,
            "revivePanel",
            revivePanel
        );

        SetReference(
            serialized,
            "reviveCanvasGroup",
            revivePanel.GetComponent<CanvasGroup>()
        );

        SetReference(
            serialized,
            "revivePanelRect",
            revivePanel.GetComponent<RectTransform>()
        );

        SetReference(
            serialized,
            "reviveButton",
            FindButton(
                revivePanel,
                "NutHoiSinh"
            )
        );

        SetReference(
            serialized,
            "reviveCostValue",
            FindText(
                revivePanel,
                "GiaHoiSinh"
            )
        );

        SetReference(
            serialized,
            "reviveGemValue",
            FindText(
                revivePanel,
                "NgocHienCo"
            )
        );

        SetReference(
            serialized,
            "reviveStatusValue",
            FindText(
                revivePanel,
                "TrangThaiHoiSinh"
            )
        );

        SetReference(
            serialized,
            "reviveTimerValue",
            FindText(
                revivePanel,
                "GiaTriThoiGian"
            )
        );

        SetReference(
            serialized,
            "reviveTimerFill",
            FindImage(
                revivePanel,
                "ThanhThoiGianDay"
            )
        );

        // ================================================================
        // BACKDROP
        // ================================================================

        SetReference(
            serialized,
            "backdrop",
            backdrop
        );

        SetReference(
            serialized,
            "backdropCanvasGroup",
            backdrop.GetComponent<CanvasGroup>()
        );

        serialized.ApplyModifiedPropertiesWithoutUndo();

        // ================================================================
        // INITIAL STATE
        // ================================================================

        backdrop.SetActive(false);
        revivePanel.SetActive(false);
        gameOverPanel.SetActive(false);

        // ================================================================
        // EVENT SYSTEM
        // ================================================================

        EnsureEventSystem();

        // ================================================================
        // SAVE
        // ================================================================

        EditorUtility.SetDirty(
            controller
        );

        EditorUtility.SetDirty(
            root
        );

        Selection.activeGameObject =
            root;

        EditorGUIUtility.PingObject(
            root
        );

        Debug.Log(
            "[GameOverUISetup] GAME OVER UI ĐÃ ĐƯỢC TẠO LẠI.\n" +
            "=================================================\n" +
            "GameOverUI\n" +
            "├── NenToi\n" +
            "├── KhuVucHoiSinh\n" +
            "└── GameOverPanel\n" +
            "=================================================\n" +
            "HIGH CONTRAST UI ĐÃ ĐƯỢC ÁP DỤNG.\n" +
            "================================================="
        );
    }

    // =====================================================================
    // DESTROY
    // =====================================================================

    private static void DestroyOldHierarchy()
    {
        GameObject canvas =
            GameObject.Find(
                CanvasName
            );

        if (canvas != null)
        {
            Undo.DestroyObjectImmediate(
                canvas
            );
        }

        GameObject root =
            GameObject.Find(
                RootName
            );

        if (root != null)
        {
            Undo.DestroyObjectImmediate(
                root
            );
        }
    }

    // =====================================================================
    // CANVAS
    // =====================================================================

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject =
            new GameObject(
                CanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

        Undo.RegisterCreatedObjectUndo(
            canvasObject,
            "Create GameOver Canvas"
        );

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder =
            100;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(
                1920f,
                1080f
            );

        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        scaler.matchWidthOrHeight =
            0.5f;

        return canvas;
    }

    // =====================================================================
    // ROOT
    // =====================================================================

    private static GameObject CreateRoot(
        Transform parent
    )
    {
        GameObject root =
            new GameObject(
                RootName,
                typeof(RectTransform)
            );

        Undo.RegisterCreatedObjectUndo(
            root,
            "Create GameOver Root"
        );

        RectTransform rect =
            root.GetComponent<RectTransform>();

        rect.SetParent(
            parent,
            false
        );

        StretchFullScreen(
            rect
        );

        return root;
    }

    // =====================================================================
    // BACKDROP
    // =====================================================================

    private static GameObject CreateBackdrop(
        Transform parent
    )
    {
        GameObject objectRoot =
            CreateImageObject(
                BackdropName,
                parent,
                BackgroundColor
            );

        RectTransform rect =
            objectRoot.GetComponent<RectTransform>();

        StretchFullScreen(
            rect
        );

        CanvasGroup group =
            objectRoot.AddComponent<CanvasGroup>();

        group.alpha =
            0f;

        group.blocksRaycasts =
            true;

        group.interactable =
            true;

        return objectRoot;
    }

    // =====================================================================
    // REVIVE PANEL
    // =====================================================================

    private static GameObject CreateRevivePanel(
        Transform parent
    )
    {
        GameObject panel =
            CreatePanel(
                RevivePanelName,
                parent,
                new Vector2(
                    820f,
                    470f
                ),
                PanelColor
            );

        RectTransform rect =
            panel.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            Vector2.zero;

        AddOutline(
            panel,
            PanelBorderColor,
            2f
        );

        CanvasGroup group =
            panel.AddComponent<CanvasGroup>();

        group.alpha =
            0f;

        group.blocksRaycasts =
            true;

        group.interactable =
            true;

        // ---------------------------------------------------------------
        // TITLE
        // ---------------------------------------------------------------

        CreateText(
            "TieuDe",
            panel.transform,
            "✦ HỒI SINH ✦",
            42,
            White,
            TextAlignmentOptions.Center,
            new Vector2(
                720f,
                70f
            ),
            new Vector2(
                0f,
                155f
            )
        );

        CreateText(
            "MoTa",
            panel.transform,
            "TIẾP TỤC CUỘC ĐUA BẰNG GEM",
            20,
            SoftText,
            TextAlignmentOptions.Center,
            new Vector2(
                700f,
                45f
            ),
            new Vector2(
                0f,
                105f
            )
        );

        // ---------------------------------------------------------------
        // GEM CARD
        // ---------------------------------------------------------------

        GameObject gemCard =
            CreateCard(
                "TheGem",
                panel.transform,
                new Vector2(
                    680f,
                    82f
                ),
                new Vector2(
                    0f,
                    35f
                ),
                CardColor
            );

        CreateText(
            "Nhan",
            gemCard.transform,
            "CHI PHÍ HỒI SINH",
            17,
            SoftText,
            TextAlignmentOptions.Left,
            new Vector2(
                300f,
                50f
            ),
            new Vector2(
                -135f,
                0f
            )
        );

        CreateText(
            "GiaHoiSinh",
            gemCard.transform,
            "10 GEM",
            22,
            Gem,
            TextAlignmentOptions.Right,
            new Vector2(
                180f,
                50f
            ),
            new Vector2(
                90f,
                0f
            )
        );

        CreateText(
            "NgocHienCo",
            gemCard.transform,
            "0",
            22,
            White,
            TextAlignmentOptions.Right,
            new Vector2(
                160f,
                50f
            ),
            new Vector2(
                285f,
                0f
            )
        );

        // ---------------------------------------------------------------
        // TIMER
        // ---------------------------------------------------------------

        CreateText(
            "GiaTriThoiGian",
            panel.transform,
            "3.0",
            38,
            Gold,
            TextAlignmentOptions.Center,
            new Vector2(
                180f,
                55f
            ),
            new Vector2(
                0f,
                -35f
            )
        );

        CreateText(
            "NhanThoiGian",
            panel.transform,
            "GIÂY CÒN LẠI",
            15,
            SoftText,
            TextAlignmentOptions.Center,
            new Vector2(
                220f,
                35f
            ),
            new Vector2(
                0f,
                -72f
            )
        );

        // ---------------------------------------------------------------
        // TIMER BAR
        // ---------------------------------------------------------------

        GameObject timerBackground =
            CreateImageObject(
                "ThanhThoiGian",
                panel.transform,
                new Color(
                    0.045f,
                    0.090f,
                    0.115f,
                    1f
                )
            );

        RectTransform timerRect =
            timerBackground.GetComponent<RectTransform>();

        timerRect.sizeDelta =
            new Vector2(
                660f,
                14f
            );

        timerRect.anchoredPosition =
            new Vector2(
                0f,
                -105f
            );

        AddOutline(
            timerBackground,
            BorderColor,
            1f
        );

        GameObject fill =
            CreateImageObject(
                "ThanhThoiGianDay",
                timerBackground.transform,
                Teal
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
                1f,
                1f
            );

        fillRect.offsetMin =
            Vector2.zero;

        fillRect.offsetMax =
            Vector2.zero;

        Image fillImage =
            fill.GetComponent<Image>();

        fillImage.type =
            Image.Type.Filled;

        fillImage.fillMethod =
            Image.FillMethod.Horizontal;

        fillImage.fillOrigin =
            0;

        fillImage.fillAmount =
            1f;

        // ---------------------------------------------------------------
        // STATUS
        // ---------------------------------------------------------------

        CreateText(
            "TrangThaiHoiSinh",
            panel.transform,
            "SẴN SÀNG HỒI SINH",
            17,
            Teal,
            TextAlignmentOptions.Center,
            new Vector2(
                650f,
                35f
            ),
            new Vector2(
                0f,
                -135f
            )
        );

        // ---------------------------------------------------------------
        // REVIVE BUTTON
        // ---------------------------------------------------------------

        GameObject reviveButton =
            CreateButton(
                "NutHoiSinh",
                panel.transform,
                "HỒI SINH",
                22,
                Teal,
                new Vector2(
                    330f,
                    64f
                ),
                new Vector2(
                    0f,
                    -205f
                )
            );

        Button button =
            reviveButton.GetComponent<Button>();

        button.interactable =
            false;

        // ---------------------------------------------------------------
        // WARNING
        // ---------------------------------------------------------------

        CreateText(
            "CanhBao",
            panel.transform,
            "HỒI SINH SẼ TIÊU TỐN GEM",
            13,
            MutedText,
            TextAlignmentOptions.Center,
            new Vector2(
                600f,
                30f
            ),
            new Vector2(
                0f,
                -250f
            )
        );

        return panel;
    }

    // =====================================================================
    // GAME OVER PANEL
    // =====================================================================

    private static GameObject CreateGameOverPanel(
        Transform parent
    )
    {
        GameObject panel =
            CreatePanel(
                GameOverPanelName,
                parent,
                new Vector2(
                    900f,
                    690f
                ),
                PanelColor
            );

        RectTransform rect =
            panel.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            Vector2.zero;

        AddOutline(
            panel,
            PanelBorderColor,
            2f
        );

        CanvasGroup group =
            panel.AddComponent<CanvasGroup>();

        group.alpha =
            0f;

        group.blocksRaycasts =
            true;

        group.interactable =
            true;

        // ---------------------------------------------------------------
        // TITLE
        // ---------------------------------------------------------------

        CreateText(
            "TieuDe",
            panel.transform,
            "KẾT THÚC VÁN CHƠI",
            44,
            White,
            TextAlignmentOptions.Center,
            new Vector2(
                780f,
                70f
            ),
            new Vector2(
                0f,
                275f
            )
        );

        CreateText(
            "MoTa",
            panel.transform,
            "CUỘC ĐUA ĐÃ KẾT THÚC",
            18,
            SoftText,
            TextAlignmentOptions.Center,
            new Vector2(
                700f,
                40f
            ),
            new Vector2(
                0f,
                225f
            )
        );

        // ---------------------------------------------------------------
        // RESULT CARD
        // ---------------------------------------------------------------

        GameObject result =
            CreateCard(
                "KetQua",
                panel.transform,
                new Vector2(
                    780f,
                    205f
                ),
                new Vector2(
                    0f,
                    90f
                ),
                CardColor
            );

        CreateResultRow(
            result.transform,
            "ĐIỂM",
            "GiaTriDiem",
            "0",
            Cyan,
            65f
        );

        CreateResultRow(
            result.transform,
            "KỶ LỤC",
            "GiaTriKyLuc",
            "0",
            Gold,
            0f
        );

        CreateResultRow(
            result.transform,
            "XU",
            "GiaTriXu",
            "0",
            Teal,
            -65f
        );

        // ---------------------------------------------------------------
        // EXP CARD
        // ---------------------------------------------------------------

        GameObject exp =
            CreateCard(
                "KinhNghiem",
                panel.transform,
                new Vector2(
                    780f,
                    115f
                ),
                new Vector2(
                    0f,
                    -75f
                ),
                CardColor
            );

        CreateText(
            "NhanEXP",
            exp.transform,
            "KINH NGHIỆM",
            16,
            SoftText,
            TextAlignmentOptions.Left,
            new Vector2(
                220f,
                40f
            ),
            new Vector2(
                -260f,
                0f
            )
        );

        CreateText(
            "GiaTriEXP",
            exp.transform,
            "+0 EXP",
            24,
            Cyan,
            TextAlignmentOptions.Center,
            new Vector2(
                220f,
                45f
            ),
            new Vector2(
                0f,
                0f
            )
        );

        CreateText(
            "GiaTriCapDo",
            exp.transform,
            "CẤP 1",
            18,
            White,
            TextAlignmentOptions.Right,
            new Vector2(
                180f,
                40f
            ),
            new Vector2(
                250f,
                0f
            )
        );

        CreateText(
            "GiaTriLenCap",
            exp.transform,
            "LÊN CẤP!",
            16,
            Gold,
            TextAlignmentOptions.Center,
            new Vector2(
                200f,
                35f
            ),
            new Vector2(
                0f,
                -32f
            )
        );

        // ---------------------------------------------------------------
        // RETRY BUTTON
        // ---------------------------------------------------------------

        GameObject retry =
            CreateButton(
                "NutThuLai",
                panel.transform,
                "CHƠI LẠI",
                21,
                Cyan,
                new Vector2(
                    350f,
                    68f
                ),
                new Vector2(
                    -190f,
                    -215f
                )
            );

        retry.GetComponent<Button>()
            .interactable =
            false;

        // ---------------------------------------------------------------
        // MAIN MENU BUTTON
        // ---------------------------------------------------------------

        GameObject mainMenu =
            CreateButton(
                "NutManHinhChinh",
                panel.transform,
                "MÀN HÌNH CHÍNH",
                21,
                White,
                new Vector2(
                    350f,
                    68f
                ),
                new Vector2(
                    190f,
                    -215f
                )
            );

        mainMenu.GetComponent<Button>()
            .interactable =
            false;

        // ---------------------------------------------------------------
        // FOOTER
        // ---------------------------------------------------------------

        CreateText(
            "Footer",
            panel.transform,
            "SẴN SÀNG CHO MỘT CUỘC ĐUA MỚI?",
            13,
            MutedText,
            TextAlignmentOptions.Center,
            new Vector2(
                700f,
                30f
            ),
            new Vector2(
                0f,
                -285f
            )
        );

        return panel;
    }

    // =====================================================================
    // RESULT ROW
    // =====================================================================

    private static void CreateResultRow(
        Transform parent,
        string label,
        string valueName,
        string value,
        Color valueColor,
        float y
    )
    {
        CreateText(
            "Nhan_" + valueName,
            parent,
            label,
            17,
            SoftText,
            TextAlignmentOptions.Left,
            new Vector2(
                300f,
                45f
            ),
            new Vector2(
                -180f,
                y
            )
        );

        CreateText(
            valueName,
            parent,
            value,
            25,
            valueColor,
            TextAlignmentOptions.Right,
            new Vector2(
                250f,
                45f
            ),
            new Vector2(
                180f,
                y
            )
        );
    }

    // =====================================================================
    // PANEL
    // =====================================================================

    private static GameObject CreatePanel(
        string name,
        Transform parent,
        Vector2 size,
        Color color
    )
    {
        GameObject panel =
            CreateImageObject(
                name,
                parent,
                color
            );

        RectTransform rect =
            panel.GetComponent<RectTransform>();

        rect.sizeDelta =
            size;

        return panel;
    }

    // =====================================================================
    // CARD
    // =====================================================================

    private static GameObject CreateCard(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 position,
        Color color
    )
    {
        GameObject card =
            CreateImageObject(
                name,
                parent,
                color
            );

        RectTransform rect =
            card.GetComponent<RectTransform>();

        rect.sizeDelta =
            size;

        rect.anchoredPosition =
            position;

        AddOutline(
            card,
            CardBorderColor,
            1f
        );

        return card;
    }

    // =====================================================================
    // IMAGE
    // =====================================================================

    private static GameObject CreateImageObject(
        string name,
        Transform parent,
        Color color
    )
    {
        GameObject objectRoot =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image)
            );

        objectRoot.transform.SetParent(
            parent,
            false
        );

        Image image =
            objectRoot.GetComponent<Image>();

        image.color =
            color;

        return objectRoot;
    }

    // =====================================================================
    // OUTLINE
    // =====================================================================

    private static void AddOutline(
        GameObject target,
        Color color,
        float thickness
    )
    {
        Outline outline =
            target.GetComponent<Outline>();

        if (outline == null)
        {
            outline =
                target.AddComponent<Outline>();
        }

        outline.effectColor =
            color;

        outline.effectDistance =
            new Vector2(
                thickness,
                thickness
            );

        outline.useGraphicAlpha =
            true;
    }

    // =====================================================================
    // TEXT
    // =====================================================================

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string content,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 size,
        Vector2 position
    )
    {
        GameObject objectRoot =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        objectRoot.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            objectRoot.GetComponent<RectTransform>();

        rect.sizeDelta =
            size;

        rect.anchoredPosition =
            position;

        TMP_Text text =
            objectRoot.GetComponent<TMP_Text>();

        text.text =
            content;

        text.fontSize =
            fontSize;

        text.color =
            color;

        text.alignment =
            alignment;

        text.textWrappingMode =
            TextWrappingModes.NoWrap;

        text.overflowMode =
            TextOverflowModes.Ellipsis;

        // Không để text chặn raycast.
        text.raycastTarget =
            false;

        return text;
    }

    // =====================================================================
    // BUTTON
    // =====================================================================

    private static GameObject CreateButton(
        string name,
        Transform parent,
        string label,
        float fontSize,
        Color accentColor,
        Vector2 size,
        Vector2 position
    )
    {
        GameObject objectRoot =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(Outline)
            );

        objectRoot.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            objectRoot.GetComponent<RectTransform>();

        rect.sizeDelta =
            size;

        rect.anchoredPosition =
            position;

        // ================================================================
        // BACKGROUND
        // ================================================================

        Image image =
            objectRoot.GetComponent<Image>();

        image.color =
            ButtonNormal;

        // ================================================================
        // OUTLINE
        // ================================================================

        Outline outline =
            objectRoot.GetComponent<Outline>();

        outline.effectColor =
            new Color(
                accentColor.r,
                accentColor.g,
                accentColor.b,
                0.95f
            );

        outline.effectDistance =
            new Vector2(
                2f,
                2f
            );

        outline.useGraphicAlpha =
            true;

        // ================================================================
        // BUTTON COLOR STATES
        // ================================================================

        Button button =
            objectRoot.GetComponent<Button>();

        ColorBlock colors =
            button.colors;

        // ---------------------------------------------------------------
        // NORMAL
        // ---------------------------------------------------------------

        colors.normalColor =
            ButtonNormal;

        // ---------------------------------------------------------------
        // HIGHLIGHTED
        // ---------------------------------------------------------------

        colors.highlightedColor =
            new Color(
                accentColor.r * 0.34f + 0.06f,
                accentColor.g * 0.34f + 0.06f,
                accentColor.b * 0.34f + 0.06f,
                1f
            );

        // ---------------------------------------------------------------
        // PRESSED
        // ---------------------------------------------------------------

        colors.pressedColor =
            new Color(
                accentColor.r * 0.62f,
                accentColor.g * 0.62f,
                accentColor.b * 0.62f,
                1f
            );

        // ---------------------------------------------------------------
        // SELECTED
        // ---------------------------------------------------------------

        colors.selectedColor =
            colors.highlightedColor;

        // ---------------------------------------------------------------
        // DISABLED
        // ---------------------------------------------------------------

        colors.disabledColor =
            ButtonDisabled;

        colors.fadeDuration =
            0.08f;

        button.colors =
            colors;

        // ================================================================
        // LABEL
        // ================================================================

        TMP_Text text =
            CreateText(
                "Label",
                objectRoot.transform,
                label,
                fontSize,
                White,
                TextAlignmentOptions.Center,
                new Vector2(
                    size.x - 20f,
                    size.y - 10f
                ),
                Vector2.zero
            );

        text.color =
            White;

        text.fontStyle =
            FontStyles.Bold;

        text.fontWeight =
            FontWeight.Bold;

        text.raycastTarget =
            false;

        return objectRoot;
    }

    // =====================================================================
    // FIND TEXT
    // =====================================================================

    private static TMP_Text FindText(
        GameObject root,
        string objectName
    )
    {
        Transform target =
            FindChild(
                root.transform,
                objectName
            );

        if (target == null)
            return null;

        return target.GetComponent<TMP_Text>();
    }

    // =====================================================================
    // FIND IMAGE
    // =====================================================================

    private static Image FindImage(
        GameObject root,
        string objectName
    )
    {
        Transform target =
            FindChild(
                root.transform,
                objectName
            );

        if (target == null)
            return null;

        return target.GetComponent<Image>();
    }

    // =====================================================================
    // FIND BUTTON
    // =====================================================================

    private static Button FindButton(
        GameObject root,
        string objectName
    )
    {
        Transform target =
            FindChild(
                root.transform,
                objectName
            );

        if (target == null)
            return null;

        return target.GetComponent<Button>();
    }

    // =====================================================================
    // FIND CHILD
    // =====================================================================

    private static Transform FindChild(
        Transform parent,
        string objectName
    )
    {
        int count =
            parent.childCount;

        for (int i = 0; i < count; i++)
        {
            Transform child =
                parent.GetChild(i);

            if (child.name ==
                objectName)
            {
                return child;
            }

            Transform result =
                FindChild(
                    child,
                    objectName
                );

            if (result != null)
                return result;
        }

        return null;
    }

    // =====================================================================
    // SERIALIZED REFERENCE
    // =====================================================================

    private static void SetReference(
        SerializedObject serialized,
        string propertyName,
        Object value
    )
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName
            );

        if (property == null)
        {
            Debug.LogWarning(
                "[GameOverUISetup] Không tìm thấy field: " +
                propertyName
            );

            return;
        }

        property.objectReferenceValue =
            value;
    }

    // =====================================================================
    // FULL SCREEN
    // =====================================================================

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
    }

    // =====================================================================
    // EVENT SYSTEM
    // =====================================================================

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem =
            Object.FindFirstObjectByType<EventSystem>();

        if (eventSystem != null)
            return;

        GameObject eventObject =
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule)
            );

        Undo.RegisterCreatedObjectUndo(
            eventObject,
            "Create EventSystem"
        );
    }
}

#endif
