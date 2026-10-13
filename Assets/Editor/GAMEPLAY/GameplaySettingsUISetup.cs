#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public static class GameplaySettingsUISetup
{
    private const string MENU_PATH = "Tools/LEAD KHÔNG PHANH/Setup Gameplay Settings UI";
    private const string ROOT_NAME = "SettingsPanel_Gameplay";

    private static readonly Color Overlay = new Color(0.015f, 0.02f, 0.035f, 0.82f);
    private static readonly Color Window = new Color(0.045f, 0.065f, 0.095f, 0.985f);
    private static readonly Color Header = new Color(0.055f, 0.085f, 0.125f, 1f);
    private static readonly Color Row = new Color(0.065f, 0.095f, 0.135f, 1f);
    private static readonly Color Cyan = new Color(0.10f, 0.82f, 1f, 1f);
    private static readonly Color Gold = new Color(1f, 0.68f, 0.16f, 1f);
    private static readonly Color White = new Color(0.94f, 0.97f, 1f, 1f);
    private static readonly Color Muted = new Color(0.58f, 0.67f, 0.76f, 1f);

    [MenuItem(MENU_PATH)]
    public static void Setup()
    {
        Canvas canvas = FindCanvas();

        if (canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Gameplay Settings",
                "Không tìm thấy Canvas trong Gameplay Scene.",
                "OK");
            return;
        }

        GameObject old = canvas.transform.Find(ROOT_NAME)?.gameObject;
        if (old != null)
            Object.DestroyImmediate(old);

        GameObject panel = CreatePanel(canvas.transform);

        SetupRuntimeReferences(panel);
        SetupAudioRouting();

        Selection.activeGameObject = panel;
        EditorUtility.SetDirty(panel);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log(
            "[GameplaySettingsUISetup] Setup hoàn tất: " +
            "UI + Master/Music/SFX Toggle + Mixer + AudioSource routing.");
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = CreateUIObject(ROOT_NAME, parent);
        Stretch(panel.GetComponent<RectTransform>());

        Image overlay = panel.AddComponent<Image>();
        overlay.color = Overlay;
        overlay.raycastTarget = true;

        CanvasGroup group = panel.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        GameObject window = CreateUIObject("SettingsWindow", panel.transform);
        RectTransform wr = window.GetComponent<RectTransform>();
        Center(wr, 720f, 470f);

        Image wi = window.AddComponent<Image>();
        wi.color = Window;
        AddShadow(window, new Vector2(0f, -8f), 0.35f);

        GameObject topGlow = CreateUIObject("TopAccentGlow", window.transform);
        RectTransform tg = topGlow.GetComponent<RectTransform>();
        AnchoredTop(tg, 0f, 5f);
        topGlow.AddComponent<Image>().color = Cyan;

        GameObject header = CreateUIObject("Header", window.transform);
        RectTransform hr = header.GetComponent<RectTransform>();

        SetRect(
            hr,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -82f),
            new Vector2(0f, 0f));

        header.AddComponent<Image>().color = Header;

        GameObject title = CreateText(
            "Title",
            header.transform,
            "CÀI ĐẶT",
            32f,
            FontStyles.Bold,
            White,
            TextAlignmentOptions.Left);

        SetRect(
            title.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, 0.5f),
            new Vector2(28f, 0f),
            new Vector2(380f, 0f));

        GameObject subtitle = CreateText(
            "Subtitle",
            header.transform,
            "ÂM THANH & TÙY CHỌN",
            12f,
            FontStyles.Bold,
            Muted,
            TextAlignmentOptions.Left);

        SetRect(
            subtitle.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, 0.5f),
            new Vector2(30f, -23f),
            new Vector2(380f, 0f));

        GameObject close = CreateButton(
            "CloseButton",
            header.transform,
            "×",
            28f,
            new Color(0.12f, 0.17f, 0.23f, 1f),
            White);

        SetRect(
            close.GetComponent<RectTransform>(),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-20f, 0f),
            new Vector2(48f, 48f));

        SetButtonColor(
            close,
            new Color(0.12f, 0.17f, 0.23f, 1f),
            new Color(0.16f, 0.24f, 0.31f, 1f));

        GameObject content = CreateUIObject("Content", window.transform);
        RectTransform cr = content.GetComponent<RectTransform>();

        SetRect(
            cr,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(30f, 28f),
            new Vector2(-30f, -105f));

        GameObject sectionTitle = CreateText(
            "AudioSectionTitle",
            content.transform,
            "ÂM THANH",
            14f,
            FontStyles.Bold,
            Cyan,
            TextAlignmentOptions.Left);

        SetRect(
            sectionTitle.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(2f, -4f),
            new Vector2(240f, 26f));

        GameObject sectionLine = CreateUIObject(
            "AudioSectionLine",
            content.transform);

        RectTransform slr = sectionLine.GetComponent<RectTransform>();

        SetRect(
            slr,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -35f),
            new Vector2(0f, 2f));

        sectionLine.AddComponent<Image>().color =
            new Color(Cyan.r, Cyan.g, Cyan.b, 0.25f);

        CreateAudioRow(
            content.transform,
            "MasterAudioRow",
            "ÂM THANH TỔNG",
            "Tắt toàn bộ âm thanh trong game",
            "MASTER",
            Gold,
            0);

        CreateAudioRow(
            content.transform,
            "MusicRow",
            "NHẠC",
            "Bật / tắt nhạc nền",
            "MUSIC",
            Cyan,
            1);

        CreateAudioRow(
            content.transform,
            "SFXRow",
            "HIỆU ỨNG",
            "Bật / tắt tiếng xe, coin, va chạm và UI",
            "SFX",
            new Color(0.65f, 0.46f, 1f, 1f),
            2);

        GameObject info = CreateText(
            "SharedInfo",
            content.transform,
            "Cài đặt này dùng chung với Main Menu.",
            12f,
            FontStyles.Normal,
            Muted,
            TextAlignmentOptions.Left);

        SetRect(
            info.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 6f),
            new Vector2(0f, 22f));

        GameObject bottomClose = CreateButton(
            "CloseButtonBottom",
            window.transform,
            "ĐÓNG",
            14f,
            Gold,
            new Color(0.03f, 0.04f, 0.06f, 1f));

        SetRect(
            bottomClose.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 18f),
            new Vector2(150f, 42f));

        SetButtonColor(
            bottomClose,
            Gold,
            new Color(1f, 0.78f, 0.30f, 1f));

        panel.SetActive(false);
        return panel;
    }

    private static void CreateAudioRow(
        Transform parent,
        string name,
        string title,
        string description,
        string badge,
        Color accent,
        int index)
    {
        GameObject row = CreateUIObject(name, parent);

        RectTransform rr = row.GetComponent<RectTransform>();

        SetRect(
            rr,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -(52f + index * 91f)),
            new Vector2(0f, 78f));

        Image ri = row.AddComponent<Image>();
        ri.color = Row;
        AddShadow(row, new Vector2(0f, -2f), 0.16f);

        GameObject accentBar = CreateUIObject(
            "Accent",
            row.transform);

        RectTransform ar = accentBar.GetComponent<RectTransform>();

        SetRect(
            ar,
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, 0.5f),
            Vector2.zero,
            new Vector2(4f, 0f));

        accentBar.AddComponent<Image>().color = accent;

        GameObject badgeGO = CreateUIObject(
            "Badge",
            row.transform);

        RectTransform br = badgeGO.GetComponent<RectTransform>();

        SetRect(
            br,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(22f, 0f),
            new Vector2(72f, 40f));

        badgeGO.AddComponent<Image>().color =
            new Color(accent.r, accent.g, accent.b, 0.14f);

        GameObject badgeText = CreateText(
            "BadgeText",
            badgeGO.transform,
            badge,
            10f,
            FontStyles.Bold,
            accent,
            TextAlignmentOptions.Center);

        SetFullChild(badgeText.GetComponent<RectTransform>());

        GameObject label = CreateText(
            "Label",
            row.transform,
            title,
            18f,
            FontStyles.Bold,
            White,
            TextAlignmentOptions.Left);

        SetRect(
            label.GetComponent<RectTransform>(),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(112f, 8f),
            new Vector2(330f, 28f));

        GameObject desc = CreateText(
            "Description",
            row.transform,
            description,
            11f,
            FontStyles.Normal,
            Muted,
            TextAlignmentOptions.Left);

        SetRect(
            desc.GetComponent<RectTransform>(),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(112f, -14f),
            new Vector2(390f, 22f));

        string toggleName =
            name == "MasterAudioRow"
                ? "MasterAudioToggle"
                : name == "MusicRow"
                    ? "MusicToggle"
                    : "SFXToggle";

        GameObject toggle = CreateToggle(
            toggleName,
            row.transform,
            accent);

        SetRect(
            toggle.GetComponent<RectTransform>(),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-20f, 0f),
            new Vector2(84f, 40f));
    }

    private static GameObject CreateToggle(
        string name,
        Transform parent,
        Color accent)
    {
        GameObject go = CreateUIObject(name, parent);

        Toggle toggle = go.AddComponent<Toggle>();
        toggle.isOn = true;
        toggle.toggleTransition = Toggle.ToggleTransition.Fade;

        Image background = go.AddComponent<Image>();
        background.color = new Color(0.11f, 0.15f, 0.19f, 1f);

        toggle.targetGraphic = background;

        ColorBlock cb = toggle.colors;
        cb.normalColor = new Color(0.11f, 0.15f, 0.19f, 1f);
        cb.highlightedColor = new Color(0.16f, 0.22f, 0.28f, 1f);
        cb.pressedColor = new Color(0.08f, 0.12f, 0.16f, 1f);
        cb.selectedColor = cb.highlightedColor;
        cb.disabledColor = new Color(0.08f, 0.10f, 0.12f, 1f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        toggle.colors = cb;

        GameObject check = CreateText(
            "Checkmark",
            go.transform,
            "✓",
            18f,
            FontStyles.Bold,
            accent,
            TextAlignmentOptions.Center);

        SetRect(
            check.GetComponent<RectTransform>(),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(20f, 0f),
            new Vector2(28f, 28f));

        toggle.graphic = check.GetComponent<Graphic>();

        return go;
    }

    private static GameObject CreateButton(
        string name,
        Transform parent,
        string text,
        float fontSize,
        Color bg,
        Color textColor)
    {
        GameObject go = CreateUIObject(name, parent);

        Image image = go.AddComponent<Image>();
        image.color = bg;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock cb = button.colors;
        cb.normalColor = bg;
        cb.highlightedColor = new Color(
            Mathf.Min(bg.r + 0.07f, 1f),
            Mathf.Min(bg.g + 0.07f, 1f),
            Mathf.Min(bg.b + 0.07f, 1f),
            bg.a);
        cb.pressedColor = new Color(
            bg.r * 0.82f,
            bg.g * 0.82f,
            bg.b * 0.82f,
            bg.a);
        cb.selectedColor = cb.highlightedColor;
        cb.fadeDuration = 0.08f;
        button.colors = cb;

        GameObject label = CreateText(
            "Text",
            go.transform,
            text,
            fontSize,
            FontStyles.Bold,
            textColor,
            TextAlignmentOptions.Center);

        SetFullChild(label.GetComponent<RectTransform>());

        return go;
    }

    private static GameObject CreateText(
        string name,
        Transform parent,
        string text,
        float size,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject go = CreateUIObject(name, parent);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;

        return go;
    }

    private static GameObject CreateUIObject(
        string name,
        Transform parent)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform));

        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetupRuntimeReferences(
        GameObject panel)
    {
        GameplaySettingsUI ui =
            panel.GetComponent<GameplaySettingsUI>();

        if (ui == null)
            ui = panel.AddComponent<GameplaySettingsUI>();

        SerializedObject so =
            new SerializedObject(ui);

        SetRef(so, "settingsPanel", panel);
        SetRef(so, "gameAudioMixer", FindGameAudioMixer());
        SetRef(so, "musicAudioSource", FindMusicSource());
        SetRef(so, "masterAudioToggle",
            FindToggle(panel, "MasterAudioToggle"));
        SetRef(so, "musicToggle",
            FindToggle(panel, "MusicToggle"));
        SetRef(so, "sfxToggle",
            FindToggle(panel, "SFXToggle"));
        SetRef(so, "closeButton",
            FindButton(panel, "CloseButtonBottom"));

        so.ApplyModifiedPropertiesWithoutUndo();

        Button topClose =
            FindButton(panel, "CloseButton");

        if (topClose != null)
        {
            topClose.onClick.RemoveAllListeners();

            UnityEventTools.AddPersistentListener(
                topClose.onClick,
                ui.Close);
        }

        EditorUtility.SetDirty(ui);
    }

    private static void SetupAudioRouting()
    {
        AudioMixer mixer = FindGameAudioMixer();

        if (mixer == null)
        {
            Debug.LogWarning(
                "[GameplaySettingsUISetup] Không tìm thấy GameAudioMixer.",
                null);
            return;
        }

        AudioMixerGroup musicGroup =
            FindGroup(mixer, "Music");

        AudioMixerGroup sfxGroup =
            FindGroup(mixer, "SFX");

        AudioSource music =
            FindMusicSource();

        if (music != null)
        {
            music.loop = true;
            music.playOnAwake = true;
            EditorUtility.SetDirty(music);
        }

        AudioSource[] sources =
            Object.FindObjectsByType<AudioSource>(
                FindObjectsSortMode.None);

        UnityEngine.SceneManagement.Scene activeScene =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];

            if (source == null ||
                source.gameObject.scene != activeScene)
                continue;

            if (source == music)
            {
                if (musicGroup != null)
                    source.outputAudioMixerGroup = musicGroup;
            }
            else if (sfxGroup != null)
            {
                source.outputAudioMixerGroup = sfxGroup;
            }
        }
    }

    private static AudioMixer FindGameAudioMixer()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "GameAudioMixer t:AudioMixer");

        if (guids.Length > 0)
        {
            AudioMixer mixer =
                AssetDatabase.LoadAssetAtPath<AudioMixer>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));

            if (mixer != null)
                return mixer;
        }

        string[] all =
            AssetDatabase.FindAssets("t:AudioMixer");

        for (int i = 0; i < all.Length; i++)
        {
            AudioMixer mixer =
                AssetDatabase.LoadAssetAtPath<AudioMixer>(
                    AssetDatabase.GUIDToAssetPath(all[i]));

            if (mixer != null &&
                mixer.name == "GameAudioMixer")
                return mixer;
        }

        return null;
    }

    private static AudioMixerGroup FindGroup(
        AudioMixer mixer,
        string groupName)
    {
        AudioMixerGroup[] groups =
            mixer.FindMatchingGroups(groupName);

        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null &&
                groups[i].name == groupName)
                return groups[i];
        }

        return groups.Length > 0
            ? groups[0]
            : null;
    }

    private static AudioSource FindMusicSource()
    {
        AudioSource[] sources =
            Object.FindObjectsByType<AudioSource>(
                FindObjectsSortMode.None);

        AudioSource fallback = null;

        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];

            if (source == null ||
                !source.gameObject.scene.IsValid())
                continue;

            string n =
                source.name.ToLowerInvariant();

            if (n.Contains("music") ||
                n.Contains("bgm") ||
                n.Contains("gameplay"))
                return source;

            if (fallback == null &&
                source.loop &&
                source.clip != null)
            {
                fallback = source;
            }
            else if (fallback == null &&
                     source.clip != null)
            {
                fallback = source;
            }
        }

        return fallback;
    }

    private static Toggle FindToggle(
        GameObject root,
        string name)
    {
        string row =
            name == "MasterAudioToggle"
                ? "MasterAudioRow"
                : name == "MusicToggle"
                    ? "MusicRow"
                    : "SFXRow";

        return root.transform.Find(
            "SettingsWindow/Content/" +
            row + "/" + name)?.GetComponent<Toggle>();
    }

    private static Button FindButton(
        GameObject root,
        string name)
    {
        return root.transform.Find(
                   "SettingsWindow/Header/" + name)
                   ?.GetComponent<Button>()
               ?? root.transform.Find(
                   "SettingsWindow/" + name)
                   ?.GetComponent<Button>()
               ?? root.transform.Find(
                   "SettingsWindow/Content/" + name)
                   ?.GetComponent<Button>();
    }

    private static Canvas FindCanvas()
    {
        Canvas[] canvases =
            Object.FindObjectsByType<Canvas>(
                FindObjectsSortMode.None);

        Canvas fallback = null;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];

            if (canvas == null)
                continue;

            string n =
                canvas.name.ToLowerInvariant();

            if (n.Contains("gameplay"))
                return canvas;

            if (fallback == null &&
                canvas.isRootCanvas)
            {
                fallback = canvas;
            }
        }

        return fallback;
    }

    private static void SetRef(
        SerializedObject so,
        string field,
        Object value)
    {
        SerializedProperty p =
            so.FindProperty(field);

        if (p != null)
            p.objectReferenceValue = value;
    }

    private static void Center(
        RectTransform r,
        float width,
        float height)
    {
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(
        RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        r.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetFullChild(
        RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        r.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void AnchoredTop(
        RectTransform r,
        float y,
        float height)
    {
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -y);
        r.sizeDelta = new Vector2(0f, height);
    }

    private static void SetRect(
        RectTransform r,
        Vector2 amin,
        Vector2 amax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        r.anchorMin = amin;
        r.anchorMax = amax;
        r.pivot = pivot;
        r.anchoredPosition = position;
        r.sizeDelta = size;
    }

    private static void SetButtonColor(
        GameObject go,
        Color normal,
        Color highlighted)
    {
        Button button =
            go.GetComponent<Button>();

        if (button == null)
            return;

        ColorBlock cb =
            button.colors;

        cb.normalColor = normal;
        cb.highlightedColor = highlighted;
        cb.pressedColor = new Color(
            normal.r * 0.82f,
            normal.g * 0.82f,
            normal.b * 0.82f,
            normal.a);
        cb.selectedColor = highlighted;
        cb.fadeDuration = 0.08f;

        button.colors = cb;
    }

    private static void AddShadow(
        GameObject go,
        Vector2 distance,
        float alpha)
    {
        Shadow shadow =
            go.AddComponent<Shadow>();

        shadow.effectColor =
            new Color(0f, 0f, 0f, alpha);

        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }
}

#endif
