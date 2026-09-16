#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuSettingsUISetup
{
    // =========================================================
    // MENU
    // =========================================================

    private const string MENU_PATH =
        "Tools/LEAD KHÔNG PHANH/Setup MainMenu Settings UI";

    [MenuItem(MENU_PATH)]
    public static void Setup()
    {
        Canvas canvas = FindMainCanvas();

        if (canvas == null)
        {
            EditorUtility.DisplayDialog(
                "MainMenu Settings",
                "Không tìm thấy Canvas trong Scene hiện tại.",
                "OK");

            return;
        }

        Transform oldPanel =
            canvas.transform.Find("SettingsPanel_MainMenu");

        if (oldPanel != null)
        {
            bool deleteOld = EditorUtility.DisplayDialog(
                "MainMenu Settings",
                "SettingsPanel_MainMenu đã tồn tại.\n\n" +
                "Bạn có muốn xóa và dựng lại không?",
                "Dựng lại",
                "Hủy");

            if (!deleteOld)
                return;

            Undo.DestroyObjectImmediate(oldPanel.gameObject);
        }

        GameObject settingsPanel =
            CreateSettingsPanel(canvas.transform);

        Selection.activeGameObject =
            settingsPanel;

        EditorUtility.SetDirty(settingsPanel);

        Debug.Log(
            "[MainMenuSettingsUISetup] " +
            "Đã tạo MainMenu Settings UI thành công.");
    }

    // =========================================================
    // MAIN PANEL
    // =========================================================

    private static GameObject CreateSettingsPanel(
        Transform parent)
    {
        GameObject panel =
            CreateUIObject(
                "SettingsPanel_MainMenu",
                parent);

        RectTransform panelRect =
            panel.GetComponent<RectTransform>();

        SetStretch(panelRect);

        Image panelImage =
            panel.AddComponent<Image>();

        panelImage.color =
            new Color(
                0.025f,
                0.03f,
                0.04f,
                0.94f);

        panelImage.raycastTarget = true;

        CanvasGroup canvasGroup =
            panel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // -----------------------------------------------------
        // SETTINGS WINDOW
        // -----------------------------------------------------

        GameObject window =
            CreateUIObject(
                "SettingsWindow",
                panel.transform);

        RectTransform windowRect =
            window.GetComponent<RectTransform>();

        windowRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        windowRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        windowRect.pivot =
            new Vector2(0.5f, 0.5f);

        windowRect.sizeDelta =
            new Vector2(620f, 430f);

        windowRect.anchoredPosition =
            Vector2.zero;

        Image windowImage =
            window.AddComponent<Image>();

        windowImage.color =
            new Color(
                0.07f,
                0.08f,
                0.10f,
                1f);

        // -----------------------------------------------------
        // HEADER
        // -----------------------------------------------------

        GameObject header =
            CreateUIObject(
                "Header",
                window.transform);

        RectTransform headerRect =
            header.GetComponent<RectTransform>();

        SetAnchorTop(
            headerRect,
            0f,
            80f);

        Image headerImage =
            header.AddComponent<Image>();

        headerImage.color =
            new Color(
                0.10f,
                0.11f,
                0.13f,
                1f);

        CreateText(
            "Title",
            header.transform,
            "CÀI ĐẶT",
            30f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);

        RectTransform titleRect =
            header.transform
                .Find("Title")
                .GetComponent<RectTransform>();

        SetStretch(titleRect);

        titleRect.offsetMin =
            new Vector2(30f, 0f);

        titleRect.offsetMax =
            new Vector2(-80f, 0f);

        // -----------------------------------------------------
        // CLOSE BUTTON
        // -----------------------------------------------------

        GameObject closeButton =
            CreateButton(
                "CloseButton",
                header.transform,
                "X");

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
            new Vector2(-14f, 0f);

        SetButtonColor(
            closeButton,
            new Color(
                0.75f,
                0.12f,
                0.10f,
                1f));

        // -----------------------------------------------------
        // CONTENT
        // -----------------------------------------------------

        GameObject content =
            CreateUIObject(
                "Content",
                window.transform);

        RectTransform contentRect =
            content.GetComponent<RectTransform>();

        SetStretch(contentRect);

        contentRect.offsetMin =
            new Vector2(50f, 70f);

        contentRect.offsetMax =
            new Vector2(-50f, -95f);

        // -----------------------------------------------------
        // AUDIO SECTION
        // -----------------------------------------------------

        GameObject audioSection =
            CreateUIObject(
                "AudioSection",
                content.transform);

        RectTransform audioRect =
            audioSection.GetComponent<RectTransform>();

        audioRect.anchorMin =
            new Vector2(0f, 0.5f);

        audioRect.anchorMax =
            new Vector2(1f, 0.5f);

        audioRect.pivot =
            new Vector2(0.5f, 0.5f);

        audioRect.sizeDelta =
            new Vector2(0f, 210f);

        audioRect.anchoredPosition =
            new Vector2(0f, 10f);

        CreateText(
            "SectionTitle",
            audioSection.transform,
            "ÂM THANH",
            21f,
            FontStyles.Bold,
            new Color(
                1f,
                0.78f,
                0.15f,
                1f),
            TextAlignmentOptions.Left);

        RectTransform sectionTitleRect =
            audioSection.transform
                .Find("SectionTitle")
                .GetComponent<RectTransform>();

        sectionTitleRect.anchorMin =
            new Vector2(0f, 1f);

        sectionTitleRect.anchorMax =
            new Vector2(1f, 1f);

        sectionTitleRect.pivot =
            new Vector2(0f, 1f);

        sectionTitleRect.sizeDelta =
            new Vector2(0f, 40f);

        sectionTitleRect.anchoredPosition =
            Vector2.zero;

        // -----------------------------------------------------
        // MUSIC ROW
        // -----------------------------------------------------

        GameObject musicRow =
            CreateSettingRow(
                "MusicRow",
                audioSection.transform,
                "NHẠC");

        RectTransform musicRowRect =
            musicRow.GetComponent<RectTransform>();

        musicRowRect.anchorMin =
            new Vector2(0f, 0.5f);

        musicRowRect.anchorMax =
            new Vector2(1f, 0.5f);

        musicRowRect.pivot =
            new Vector2(0.5f, 0.5f);

        musicRowRect.sizeDelta =
            new Vector2(0f, 70f);

        musicRowRect.anchoredPosition =
            new Vector2(0f, 25f);

        GameObject musicToggle =
            CreateToggle(
                "MusicToggle",
                musicRow.transform,
                true);

        RectTransform musicToggleRect =
            musicToggle.GetComponent<RectTransform>();

        musicToggleRect.anchorMin =
            new Vector2(1f, 0.5f);

        musicToggleRect.anchorMax =
            new Vector2(1f, 0.5f);

        musicToggleRect.pivot =
            new Vector2(1f, 0.5f);

        musicToggleRect.sizeDelta =
            new Vector2(70f, 40f);

        musicToggleRect.anchoredPosition =
            new Vector2(0f, 0f);

        // -----------------------------------------------------
        // SFX ROW
        // -----------------------------------------------------

        GameObject sfxRow =
            CreateSettingRow(
                "SFXRow",
                audioSection.transform,
                "HIỆU ỨNG");

        RectTransform sfxRowRect =
            sfxRow.GetComponent<RectTransform>();

        sfxRowRect.anchorMin =
            new Vector2(0f, 0.5f);

        sfxRowRect.anchorMax =
            new Vector2(1f, 0.5f);

        sfxRowRect.pivot =
            new Vector2(0.5f, 0.5f);

        sfxRowRect.sizeDelta =
            new Vector2(0f, 70f);

        sfxRowRect.anchoredPosition =
            new Vector2(0f, -55f);

        GameObject sfxToggle =
            CreateToggle(
                "SFXToggle",
                sfxRow.transform,
                true);

        RectTransform sfxToggleRect =
            sfxToggle.GetComponent<RectTransform>();

        sfxToggleRect.anchorMin =
            new Vector2(1f, 0.5f);

        sfxToggleRect.anchorMax =
            new Vector2(1f, 0.5f);

        sfxToggleRect.pivot =
            new Vector2(1f, 0.5f);

        sfxToggleRect.sizeDelta =
            new Vector2(70f, 40f);

        sfxToggleRect.anchoredPosition =
            new Vector2(0f, 0f);

        // -----------------------------------------------------
        // BOTTOM CLOSE
        // -----------------------------------------------------

        GameObject bottomClose =
            CreateButton(
                "CloseButtonBottom",
                window.transform,
                "ĐÓNG");

        RectTransform bottomRect =
            bottomClose.GetComponent<RectTransform>();

        bottomRect.anchorMin =
            new Vector2(0.5f, 0f);

        bottomRect.anchorMax =
            new Vector2(0.5f, 0f);

        bottomRect.pivot =
            new Vector2(0.5f, 0f);

        bottomRect.sizeDelta =
            new Vector2(180f, 55f);

        bottomRect.anchoredPosition =
            new Vector2(0f, 20f);

        SetButtonColor(
            bottomClose,
            new Color(
                0.92f,
                0.68f,
                0.08f,
                1f));

        TMP_Text bottomText =
            bottomClose
                .GetComponentInChildren<TMP_Text>();

        if (bottomText != null)
        {
            bottomText.color =
                new Color(
                    0.05f,
                    0.05f,
                    0.05f,
                    1f);

            bottomText.fontStyle =
                FontStyles.Bold;
        }

        // -----------------------------------------------------
        // START HIDDEN
        // -----------------------------------------------------

        panel.SetActive(false);

        return panel;
    }

    // =========================================================
    // SETTING ROW
    // =========================================================

    private static GameObject CreateSettingRow(
        string name,
        Transform parent,
        string label)
    {
        GameObject row =
            CreateUIObject(
                name,
                parent);

        Image background =
            row.AddComponent<Image>();

        background.color =
            new Color(
                0.10f,
                0.11f,
                0.13f,
                1f);

        CreateText(
            "Label",
            row.transform,
            label,
            22f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Left);

        RectTransform labelRect =
            row.transform
                .Find("Label")
                .GetComponent<RectTransform>();

        labelRect.anchorMin =
            new Vector2(0f, 0f);

        labelRect.anchorMax =
            new Vector2(0.75f, 1f);

        labelRect.offsetMin =
            new Vector2(24f, 0f);

        labelRect.offsetMax =
            new Vector2(0f, 0f);

        return row;
    }

    // =========================================================
    // TOGGLE
    // =========================================================

    private static GameObject CreateToggle(
        string name,
        Transform parent,
        bool defaultValue)
    {
        GameObject toggleGO =
            CreateUIObject(
                name,
                parent);

        Toggle toggle =
            toggleGO.AddComponent<Toggle>();

        toggle.isOn =
            defaultValue;

        toggle.transition =
            Selectable.Transition.ColorTint;

        ColorBlock colors =
            toggle.colors;

        colors.normalColor =
            new Color(
                0.35f,
                0.35f,
                0.35f,
                1f);

        colors.highlightedColor =
            new Color(
                0.45f,
                0.45f,
                0.45f,
                1f);

        colors.pressedColor =
            new Color(
                0.25f,
                0.25f,
                0.25f,
                1f);

        colors.selectedColor =
            colors.normalColor;

        toggle.colors =
            colors;

        // -----------------------------------------------------
        // BACKGROUND
        // -----------------------------------------------------

        GameObject background =
            CreateUIObject(
                "Background",
                toggleGO.transform);

        RectTransform bgRect =
            background.GetComponent<RectTransform>();

        SetStretch(bgRect);

        Image bgImage =
            background.AddComponent<Image>();

        bgImage.color =
            new Color(
                0.18f,
                0.19f,
                0.21f,
                1f);

        // -----------------------------------------------------
        // CHECKMARK
        // -----------------------------------------------------

        GameObject checkmark =
            CreateUIObject(
                "Checkmark",
                background.transform);

        RectTransform checkRect =
            checkmark.GetComponent<RectTransform>();

        checkRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        checkRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        checkRect.pivot =
            new Vector2(0.5f, 0.5f);

        checkRect.sizeDelta =
            new Vector2(28f, 28f);

        checkRect.anchoredPosition =
            Vector2.zero;

        Image checkImage =
            checkmark.AddComponent<Image>();

        checkImage.color =
            new Color(
                1f,
                0.78f,
                0.08f,
                1f);

        toggle.graphic =
            checkImage;

        toggle.targetGraphic =
            bgImage;

        // -----------------------------------------------------
        // LABEL
        // -----------------------------------------------------

        CreateText(
            "Label",
            toggleGO.transform,
            "",
            18f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);

        RectTransform labelRect =
            toggleGO.transform
                .Find("Label")
                .GetComponent<RectTransform>();

        SetStretch(labelRect);

        return toggleGO;
    }

    // =========================================================
    // BUTTON
    // =========================================================

    private static GameObject CreateButton(
        string name,
        Transform parent,
        string text)
    {
        GameObject buttonGO =
            CreateUIObject(
                name,
                parent);

        Image image =
            buttonGO.AddComponent<Image>();

        image.color =
            new Color(
                0.20f,
                0.21f,
                0.23f,
                1f);

        Button button =
            buttonGO.AddComponent<Button>();

        button.targetGraphic =
            image;

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            image.color;

        colors.highlightedColor =
            new Color(
                0.30f,
                0.31f,
                0.33f,
                1f);

        colors.pressedColor =
            new Color(
                0.14f,
                0.15f,
                0.17f,
                1f);

        colors.selectedColor =
            colors.highlightedColor;

        button.colors =
            colors;

        CreateText(
            "Text",
            buttonGO.transform,
            text,
            18f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);

        RectTransform textRect =
            buttonGO.transform
                .Find("Text")
                .GetComponent<RectTransform>();

        SetStretch(textRect);

        return buttonGO;
    }

    // =========================================================
    // TEXT
    // =========================================================

    private static GameObject CreateText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textGO =
            CreateUIObject(
                name,
                parent);

        TextMeshProUGUI tmp =
            textGO.AddComponent<TextMeshProUGUI>();

        tmp.text =
            text;

        tmp.fontSize =
            fontSize;

        tmp.fontStyle =
            fontStyle;

        tmp.color =
            color;

        tmp.alignment =
            alignment;

        tmp.textWrappingMode =
            TextWrappingModes.NoWrap;

        tmp.overflowMode =
            TextOverflowModes.Ellipsis;

        return textGO;
    }

    // =========================================================
    // UI OBJECT
    // =========================================================

    private static GameObject CreateUIObject(
        string name,
        Transform parent)
    {
        GameObject go =
            new GameObject(
                name,
                typeof(RectTransform));

        Undo.RegisterCreatedObjectUndo(
            go,
            "Create " + name);

        go.transform.SetParent(
            parent,
            false);

        return go;
    }

    // =========================================================
    // CANVAS
    // =========================================================

    private static Canvas FindMainCanvas()
    {
        Canvas[] canvases =
            Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas =
                canvases[i];

            if (canvas == null)
                continue;

            if (canvas.renderMode ==
                RenderMode.ScreenSpaceOverlay)
            {
                return canvas;
            }
        }

        if (canvases.Length > 0)
            return canvases[0];

        return null;
    }

    // =========================================================
    // RECT HELPERS
    // =========================================================

    private static void SetStretch(
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

        rect.anchoredPosition =
            Vector2.zero;
    }

    private static void SetAnchorTop(
        RectTransform rect,
        float bottom,
        float height)
    {
        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.pivot =
            new Vector2(0.5f, 1f);

        rect.sizeDelta =
            new Vector2(0f, height);

        rect.anchoredPosition =
            new Vector2(0f, -bottom);
    }

    // =========================================================
    // BUTTON COLOR
    // =========================================================

    private static void SetButtonColor(
        GameObject buttonGO,
        Color color)
    {
        Image image =
            buttonGO.GetComponent<Image>();

        if (image != null)
            image.color = color;

        Button button =
            buttonGO.GetComponent<Button>();

        if (button == null)
            return;

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            color;

        colors.selectedColor =
            color;

        colors.highlightedColor =
            color * 1.1f;

        colors.pressedColor =
            color * 0.8f;

        button.colors =
            colors;
    }
}

#endif