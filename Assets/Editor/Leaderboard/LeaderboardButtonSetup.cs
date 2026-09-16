#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class LeaderboardButtonSetup
{
    private const string ButtonName = "LeaderboardButton";

    private static readonly Color GoldColor =
        Hex("#FFC928");

    private static readonly Color GoldDarkColor =
        Hex("#A66F00");

    private static readonly Color SurfaceColor =
        Hex("#151C21");

    private static readonly Color WhiteColor =
        Hex("#F4F7F8");

    private static readonly Color BlackColor =
        Hex("#05070A");

    // ============================================================
    // MENU
    // ============================================================

    [MenuItem(
        "LEAD KHÔNG PHANH/UI/Leaderboard/03 - Create Leaderboard Icon"
    )]
    public static void CreateLeaderboardIcon()
    {
        // --------------------------------------------------------
        // FIND CANVAS
        // --------------------------------------------------------

        Canvas canvas =
            Object.FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogError(
                "[LeaderboardButtonSetup] " +
                "Không tìm thấy Canvas trong Scene hiện tại."
            );

            return;
        }

        // --------------------------------------------------------
        // FIND EXISTING BUTTON
        // --------------------------------------------------------

        Transform existing =
            canvas.transform.Find(ButtonName);

        if (existing != null)
        {
            Object.DestroyImmediate(
                existing.gameObject
            );
        }

        // --------------------------------------------------------
        // CREATE BUTTON
        // --------------------------------------------------------

        GameObject buttonObject =
            new GameObject(
                ButtonName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );

        buttonObject.transform.SetParent(
            canvas.transform,
            false
        );

        RectTransform buttonRect =
            buttonObject.GetComponent<RectTransform>();

        buttonRect.anchorMin =
            new Vector2(1f, 1f);

        buttonRect.anchorMax =
            new Vector2(1f, 1f);

        buttonRect.pivot =
            new Vector2(1f, 1f);

        buttonRect.sizeDelta =
            new Vector2(78f, 78f);

        buttonRect.anchoredPosition =
            new Vector2(-45f, -45f);

        // --------------------------------------------------------
        // BACKGROUND
        // --------------------------------------------------------

        Image buttonImage =
            buttonObject.GetComponent<Image>();

        buttonImage.color =
            SurfaceColor;

        buttonImage.raycastTarget =
            true;

        // --------------------------------------------------------
        // BUTTON COLORS
        // --------------------------------------------------------

        Button button =
            buttonObject.GetComponent<Button>();

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            SurfaceColor;

        colors.highlightedColor =
            Color.Lerp(
                SurfaceColor,
                GoldColor,
                0.18f
            );

        colors.pressedColor =
            Color.Lerp(
                SurfaceColor,
                BlackColor,
                0.25f
            );

        colors.selectedColor =
            colors.highlightedColor;

        colors.disabledColor =
            new Color(
                0.15f,
                0.15f,
                0.15f,
                0.5f
            );

        colors.colorMultiplier =
            1f;

        colors.fadeDuration =
            0.08f;

        button.colors =
            colors;

        // --------------------------------------------------------
        // GOLD BORDER
        // --------------------------------------------------------

        GameObject border =
            new GameObject(
                "Border",
                typeof(RectTransform),
                typeof(Image)
            );

        border.transform.SetParent(
            buttonObject.transform,
            false
        );

        RectTransform borderRect =
            border.GetComponent<RectTransform>();

        borderRect.anchorMin =
            Vector2.zero;

        borderRect.anchorMax =
            Vector2.one;

        borderRect.offsetMin =
            Vector2.zero;

        borderRect.offsetMax =
            Vector2.zero;

        Image borderImage =
            border.GetComponent<Image>();

        borderImage.color =
            GoldDarkColor;

        borderImage.raycastTarget =
            false;

        // --------------------------------------------------------
        // INNER
        // --------------------------------------------------------

        GameObject inner =
            new GameObject(
                "Inner",
                typeof(RectTransform),
                typeof(Image)
            );

        inner.transform.SetParent(
            border.transform,
            false
        );

        RectTransform innerRect =
            inner.GetComponent<RectTransform>();

        innerRect.anchorMin =
            Vector2.zero;

        innerRect.anchorMax =
            Vector2.one;

        innerRect.offsetMin =
            new Vector2(2f, 2f);

        innerRect.offsetMax =
            new Vector2(-2f, -2f);

        Image innerImage =
            inner.GetComponent<Image>();

        innerImage.color =
            SurfaceColor;

        innerImage.raycastTarget =
            false;

        // --------------------------------------------------------
        // TROPHY ICON
        // --------------------------------------------------------

        GameObject icon =
            new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        icon.transform.SetParent(
            buttonObject.transform,
            false
        );

        RectTransform iconRect =
            icon.GetComponent<RectTransform>();

        iconRect.anchorMin =
            Vector2.zero;

        iconRect.anchorMax =
            Vector2.one;

        iconRect.offsetMin =
            new Vector2(4f, 4f);

        iconRect.offsetMax =
            new Vector2(-4f, -4f);

        TextMeshProUGUI iconText =
            icon.GetComponent<TextMeshProUGUI>();

        iconText.text =
            "🏆";

        iconText.fontSize =
            37f;

        iconText.alignment =
            TextAlignmentOptions.Center;

        iconText.color =
            GoldColor;

        iconText.raycastTarget =
            false;

        iconText.textWrappingMode =
            TextWrappingModes.NoWrap;

        // --------------------------------------------------------
        // LABEL
        // --------------------------------------------------------

        GameObject label =
            new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        label.transform.SetParent(
            buttonObject.transform,
            false
        );

        RectTransform labelRect =
            label.GetComponent<RectTransform>();

        labelRect.anchorMin =
            new Vector2(0f, 0f);

        labelRect.anchorMax =
            new Vector2(1f, 0f);

        labelRect.pivot =
            new Vector2(0.5f, 1f);

        labelRect.sizeDelta =
            new Vector2(150f, 26f);

        labelRect.anchoredPosition =
            new Vector2(0f, -4f);

        TextMeshProUGUI labelText =
            label.GetComponent<TextMeshProUGUI>();

        labelText.text =
            "BXH";

        labelText.fontSize =
            12f;

        labelText.fontStyle =
            FontStyles.Bold;

        labelText.alignment =
            TextAlignmentOptions.Center;

        labelText.color =
            WhiteColor;

        labelText.raycastTarget =
            false;

        labelText.textWrappingMode =
            TextWrappingModes.NoWrap;

        // --------------------------------------------------------
        // FIND LEADERBOARD UI
        // --------------------------------------------------------

        LeaderboardUI leaderboardUI =
            Object.FindFirstObjectByType<LeaderboardUI>();

        if (leaderboardUI == null)
        {
            Debug.LogWarning(
                "[LeaderboardButtonSetup] " +
                "Không tìm thấy LeaderboardUI trong Scene.\n" +
                "Hãy chạy trước:\n" +
                "LEAD KHÔNG PHANH > UI > Leaderboard > " +
                "02 - Create Leaderboard UI"
            );
        }
        else
        {
            // ----------------------------------------------------
            // ADD PERSISTENT OPEN LISTENER
            // ----------------------------------------------------

            UnityEventTools.AddPersistentListener(
                button.onClick,
                leaderboardUI.Open
            );

            EditorUtility.SetDirty(
                button
            );
        }

        // --------------------------------------------------------
        // PUT BUTTON ON TOP
        // --------------------------------------------------------

        buttonObject.transform.SetAsLastSibling();

        // --------------------------------------------------------
        // SAVE
        // --------------------------------------------------------

        EditorUtility.SetDirty(
            buttonObject
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject =
            buttonObject;

        EditorGUIUtility.PingObject(
            buttonObject
        );

        Debug.Log(
            "[LeaderboardButtonSetup] " +
            "SUCCESS: Leaderboard icon đã được tạo."
        );
    }

    // ============================================================
    // COLOR
    // ============================================================

    private static Color Hex(
        string hex
    )
    {
        if (ColorUtility.TryParseHtmlString(
                hex,
                out Color color))
        {
            return color;
        }

        return Color.white;
    }
}

#endif