#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GameplaySettingsUISetup
{
    private const string RootName = "SettingsPanel_Gameplay";

    [MenuItem("Tools/LEAD KHÔNG PHANH/Setup Gameplay Settings UI")]
    public static void Setup()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Gameplay Settings",
                "Không tìm thấy Canvas trong Scene.",
                "OK"
            );

            return;
        }

        GameObject oldRoot = FindChild(
            canvas.transform,
            RootName
        );

        if (oldRoot != null)
        {
            Object.DestroyImmediate(oldRoot);
        }

        GameObject panel =
            CreateUIObject(
                RootName,
                canvas.transform
            );

        RectTransform panelRect =
            panel.GetComponent<RectTransform>();

        SetFullStretch(panelRect);

        Image panelImage =
            panel.AddComponent<Image>();

        panelImage.color =
            new Color(0f, 0f, 0f, 0.72f);


        // ========================================================
        // SETTINGS WINDOW
        // ========================================================

        GameObject window =
            CreateUIObject(
                "SettingsWindow",
                panel.transform
            );

        RectTransform windowRect =
            window.GetComponent<RectTransform>();

        windowRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        windowRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        windowRect.pivot =
            new Vector2(0.5f, 0.5f);

        windowRect.sizeDelta =
            new Vector2(620f, 420f);

        windowRect.anchoredPosition =
            Vector2.zero;

        Image windowImage =
            window.AddComponent<Image>();

        windowImage.color =
            new Color(0.055f, 0.055f, 0.065f, 0.98f);


        // ========================================================
        // HEADER
        // ========================================================

        GameObject header =
            CreateUIObject(
                "Header",
                window.transform
            );

        RectTransform headerRect =
            header.GetComponent<RectTransform>();

        headerRect.anchorMin =
            new Vector2(0f, 1f);

        headerRect.anchorMax =
            new Vector2(1f, 1f);

        headerRect.pivot =
            new Vector2(0.5f, 1f);

        headerRect.sizeDelta =
            new Vector2(-48f, 82f);

        headerRect.anchoredPosition =
            new Vector2(0f, -24f);


        // ========================================================
        // TITLE
        // ========================================================

        GameObject title =
            CreateText(
                "Title",
                header.transform,
                "CÀI ĐẶT"
            );

        RectTransform titleRect =
            title.GetComponent<RectTransform>();

        titleRect.anchorMin =
            new Vector2(0f, 0.5f);

        titleRect.anchorMax =
            new Vector2(0f, 0.5f);

        titleRect.pivot =
            new Vector2(0f, 0.5f);

        titleRect.sizeDelta =
            new Vector2(300f, 60f);

        titleRect.anchoredPosition =
            new Vector2(0f, 0f);

        TMP_Text titleText =
            title.GetComponent<TMP_Text>();

        titleText.fontSize = 32f;
        titleText.fontStyle =
            FontStyles.Bold;
        titleText.alignment =
            TextAlignmentOptions.Left;
        titleText.color = Color.white;


        // ========================================================
        // CLOSE BUTTON
        // ========================================================

        GameObject closeButton =
            CreateButton(
                "CloseButton",
                header.transform,
                "X"
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
            new Vector2(54f, 54f);

        closeRect.anchoredPosition =
            Vector2.zero;


        // ========================================================
        // CONTENT
        // ========================================================

        GameObject content =
            CreateUIObject(
                "Content",
                window.transform
            );

        RectTransform contentRect =
            content.GetComponent<RectTransform>();

        contentRect.anchorMin =
            new Vector2(0f, 0f);

        contentRect.anchorMax =
            new Vector2(1f, 1f);

        contentRect.offsetMin =
            new Vector2(36f, 36f);

        contentRect.offsetMax =
            new Vector2(-36f, -118f);


        // ========================================================
        // AUDIO SECTION
        // ========================================================

        GameObject audioSection =
            CreateUIObject(
                "AudioSection",
                content.transform
            );

        RectTransform audioRect =
            audioSection.GetComponent<RectTransform>();

        SetFullStretch(audioRect);


        // ========================================================
        // MUSIC ROW
        // ========================================================

        GameObject musicRow =
            CreateUIObject(
                "MusicRow",
                audioSection.transform
            );

        RectTransform musicRowRect =
            musicRow.GetComponent<RectTransform>();

        musicRowRect.anchorMin =
            new Vector2(0f, 1f);

        musicRowRect.anchorMax =
            new Vector2(1f, 1f);

        musicRowRect.pivot =
            new Vector2(0.5f, 1f);

        musicRowRect.sizeDelta =
            new Vector2(0f, 72f);

        musicRowRect.anchoredPosition =
            new Vector2(0f, -52f);


        CreateTextRow(
            musicRow.transform,
            "Label",
            "NHẠC",
            out GameObject musicToggle
        );


        // ========================================================
        // SFX ROW
        // ========================================================

        GameObject sfxRow =
            CreateUIObject(
                "SFXRow",
                audioSection.transform
            );

        RectTransform sfxRowRect =
            sfxRow.GetComponent<RectTransform>();

        sfxRowRect.anchorMin =
            new Vector2(0f, 1f);

        sfxRowRect.anchorMax =
            new Vector2(1f, 1f);

        sfxRowRect.pivot =
            new Vector2(0.5f, 1f);

        sfxRowRect.sizeDelta =
            new Vector2(0f, 72f);

        sfxRowRect.anchoredPosition =
            new Vector2(0f, -142f);


        CreateTextRow(
            sfxRow.transform,
            "Label",
            "HIỆU ỨNG",
            out GameObject sfxToggle
        );


        // ========================================================
        // DEFAULT STATE
        // ========================================================

        panel.SetActive(false);

        Toggle musicToggleComponent =
            musicToggle.GetComponent<Toggle>();

        Toggle sfxToggleComponent =
            sfxToggle.GetComponent<Toggle>();

        if (musicToggleComponent != null)
            musicToggleComponent.isOn = true;

        if (sfxToggleComponent != null)
            sfxToggleComponent.isOn = true;


        // ========================================================
        // MARK DIRTY
        // ========================================================

        EditorUtility.SetDirty(panel);

        Selection.activeGameObject = panel;

        Debug.Log(
            "[GameplaySettingsUISetup] " +
            "Đã tạo Gameplay Settings UI."
        );
    }


    // ============================================================
    // CREATE TEXT ROW
    // ============================================================

    private static void CreateTextRow(
        Transform parent,
        string labelName,
        string labelText,
        out GameObject toggleObject
    )
    {
        GameObject label =
            CreateText(
                labelName,
                parent,
                labelText
            );

        RectTransform labelRect =
            label.GetComponent<RectTransform>();

        labelRect.anchorMin =
            new Vector2(0f, 0.5f);

        labelRect.anchorMax =
            new Vector2(0f, 0.5f);

        labelRect.pivot =
            new Vector2(0f, 0.5f);

        labelRect.sizeDelta =
            new Vector2(300f, 60f);

        labelRect.anchoredPosition =
            new Vector2(0f, 0f);

        TMP_Text text =
            label.GetComponent<TMP_Text>();

        text.fontSize = 24f;
        text.fontStyle =
            FontStyles.Bold;
        text.alignment =
            TextAlignmentOptions.Left;
        text.color = Color.white;


        toggleObject =
            CreateToggle(
                labelText == "NHẠC"
                    ? "MusicToggle"
                    : "SFXToggle",
                parent
            );

        RectTransform toggleRect =
            toggleObject.GetComponent<RectTransform>();

        toggleRect.anchorMin =
            new Vector2(1f, 0.5f);

        toggleRect.anchorMax =
            new Vector2(1f, 0.5f);

        toggleRect.pivot =
            new Vector2(1f, 0.5f);

        toggleRect.sizeDelta =
            new Vector2(90f, 44f);

        toggleRect.anchoredPosition =
            Vector2.zero;
    }


    // ============================================================
    // CREATE TOGGLE
    // ============================================================

    private static GameObject CreateToggle(
        string name,
        Transform parent
    )
    {
        GameObject toggleObject =
            CreateUIObject(
                name,
                parent
            );

        Toggle toggle =
            toggleObject.AddComponent<Toggle>();

        toggle.isOn = true;

        Image background =
            toggleObject.AddComponent<Image>();

        background.color =
            new Color(
                0.18f,
                0.18f,
                0.20f,
                1f
            );

        GameObject checkmark =
            CreateUIObject(
                "Checkmark",
                toggleObject.transform
            );

        RectTransform checkRect =
            checkmark.GetComponent<RectTransform>();

        checkRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        checkRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        checkRect.sizeDelta =
            new Vector2(32f, 32f);

        Image checkImage =
            checkmark.AddComponent<Image>();

        checkImage.color =
            new Color(
                0.1f,
                0.8f,
                0.95f,
                1f
            );

        toggle.graphic = checkImage;

        return toggleObject;
    }


    // ============================================================
    // CREATE BUTTON
    // ============================================================

    private static GameObject CreateButton(
        string name,
        Transform parent,
        string text
    )
    {
        GameObject buttonObject =
            CreateUIObject(
                name,
                parent
            );

        Image image =
            buttonObject.AddComponent<Image>();

        image.color =
            new Color(
                0.12f,
                0.12f,
                0.14f,
                1f
            );

        Button button =
            buttonObject.AddComponent<Button>();

        button.targetGraphic = image;

        GameObject label =
            CreateText(
                "Text",
                buttonObject.transform,
                text
            );

        RectTransform labelRect =
            label.GetComponent<RectTransform>();

        SetFullStretch(labelRect);

        TMP_Text labelText =
            label.GetComponent<TMP_Text>();

        labelText.fontSize = 24f;
        labelText.fontStyle =
            FontStyles.Bold;
        labelText.alignment =
            TextAlignmentOptions.Center;
        labelText.color = Color.white;

        return buttonObject;
    }


    // ============================================================
    // CREATE TEXT
    // ============================================================

    private static GameObject CreateText(
        string name,
        Transform parent,
        string text
    )
    {
        GameObject obj =
            CreateUIObject(
                name,
                parent
            );

        TextMeshProUGUI tmp =
            obj.AddComponent<TextMeshProUGUI>();

        tmp.text = text;
        tmp.textWrappingMode =
            TextWrappingModes.NoWrap;

        return obj;
    }


    // ============================================================
    // CREATE UI OBJECT
    // ============================================================

    private static GameObject CreateUIObject(
        string name,
        Transform parent
    )
    {
        GameObject obj =
            new GameObject(
                name,
                typeof(RectTransform)
            );

        obj.transform.SetParent(
            parent,
            false
        );

        return obj;
    }


    // ============================================================
    // FULL STRETCH
    // ============================================================

    private static void SetFullStretch(
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

        rect.pivot =
            new Vector2(0.5f, 0.5f);
    }


    // ============================================================
    // FIND CHILD
    // ============================================================

    private static GameObject FindChild(
        Transform parent,
        string childName
    )
    {
        Transform child =
            parent.Find(childName);

        return child != null
            ? child.gameObject
            : null;
    }
}

#endif
