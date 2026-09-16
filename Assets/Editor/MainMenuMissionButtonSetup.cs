#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class MainMenuMissionButtonSetup
{
    private static readonly Color Gold =
        new Color32(255, 203, 45, 255);

    private static readonly Color White =
        new Color32(245, 247, 250, 255);

    private static readonly Color Dark =
        new Color32(25, 30, 38, 245);

    private static readonly Color GoldHover =
        new Color32(255, 220, 80, 255);

    [MenuItem("LEAD KHONG PHANH/UI/Create Main Menu Mission Button")]
    public static void CreateMissionButton()
    {
        //=========================================================
        // FIND MAIN MENU CANVAS
        //=========================================================

        GameObject canvas =
            GameObject.Find("MainMenuCanvas");

        if (canvas == null)
        {
            Debug.LogError(
                "[Mission Button] Không tìm thấy MainMenuCanvas."
            );

            return;
        }


        //=========================================================
        // FIND SAFE AREA
        //=========================================================

        Transform safeArea =
            canvas.transform.Find("SafeArea");

        if (safeArea == null)
        {
            Debug.LogError(
                "[Mission Button] Không tìm thấy SafeArea."
            );

            return;
        }


        //=========================================================
        // FIND MAIN MENU CONTROLLER
        //=========================================================

        MainMenuController controller =
            Object.FindFirstObjectByType<MainMenuController>();

        if (controller == null)
        {
            Debug.LogError(
                "[Mission Button] Không tìm thấy MainMenuController " +
                "trong Scene."
            );

            return;
        }


        //=========================================================
        // CHECK OLD BUTTON
        //=========================================================

        Transform oldButton =
            safeArea.Find("MissionButton");

        if (oldButton != null)
        {
            Selection.activeGameObject =
                oldButton.gameObject;

            Debug.Log(
                "[Mission Button] MissionButton đã tồn tại."
            );

            return;
        }


        //=========================================================
        // CREATE BUTTON
        //=========================================================

        GameObject buttonObject =
            new GameObject(
                "MissionButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );


        buttonObject.transform.SetParent(
            safeArea,
            false
        );


        RectTransform rect =
            buttonObject.GetComponent<RectTransform>();


        //=========================================================
        // POSITION
        //=========================================================

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
            new Vector2(
                0f,
                -80f
            );

        rect.sizeDelta =
            new Vector2(
                330f,
                72f
            );


        //=========================================================
        // BACKGROUND
        //=========================================================

        Image image =
            buttonObject.GetComponent<Image>();


        Sprite roundedSprite =
            FindRoundedRect();


        if (roundedSprite != null)
        {
            image.sprite =
                roundedSprite;

            image.type =
                Image.Type.Sliced;
        }


        image.color =
            Gold;


        //=========================================================
        // BUTTON
        //=========================================================

        Button button =
            buttonObject.GetComponent<Button>();


        ColorBlock colors =
            button.colors;


        colors.normalColor =
            Gold;

        colors.highlightedColor =
            GoldHover;

        colors.pressedColor =
            new Color32(
                220,
                170,
                25,
                255
            );

        colors.selectedColor =
            GoldHover;

        colors.disabledColor =
            new Color32(
                100,
                100,
                100,
                120
            );

        colors.fadeDuration =
            0.08f;


        button.colors =
            colors;


        //=========================================================
        // SHADOW
        //=========================================================

        Shadow shadow =
            buttonObject.AddComponent<Shadow>();


        shadow.effectColor =
            new Color32(
                0,
                0,
                0,
                100
            );

        shadow.effectDistance =
            new Vector2(
                0f,
                -3f
            );


        //=========================================================
        // TEXT
        //=========================================================

        GameObject textObject =
            new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );


        textObject.transform.SetParent(
            buttonObject.transform,
            false
        );


        RectTransform textRect =
            textObject.GetComponent<RectTransform>();


        textRect.anchorMin =
            Vector2.zero;

        textRect.anchorMax =
            Vector2.one;

        textRect.offsetMin =
            Vector2.zero;

        textRect.offsetMax =
            Vector2.zero;


        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();


        text.text =
            "NHIỆM VỤ";

        text.fontSize =
            24f;

        text.fontStyle =
            FontStyles.Bold;

        text.color =
            Dark;

        text.alignment =
            TextAlignmentOptions.Center;

        text.textWrappingMode =
            TextWrappingModes.NoWrap;

        text.raycastTarget =
            false;


        //=========================================================
        // ICON
        //=========================================================

        GameObject iconObject =
            new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );


        iconObject.transform.SetParent(
            buttonObject.transform,
            false
        );


        RectTransform iconRect =
            iconObject.GetComponent<RectTransform>();


        iconRect.anchorMin =
            new Vector2(
                0f,
                0.5f
            );

        iconRect.anchorMax =
            new Vector2(
                0f,
                0.5f
            );

        iconRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        iconRect.anchoredPosition =
            new Vector2(
                45f,
                0f
            );

        iconRect.sizeDelta =
            new Vector2(
                42f,
                42f
            );


        TextMeshProUGUI icon =
            iconObject.GetComponent<TextMeshProUGUI>();


        icon.text =
            "★";

        icon.fontSize =
            24f;

        icon.fontStyle =
            FontStyles.Bold;

        icon.color =
            Dark;

        icon.alignment =
            TextAlignmentOptions.Center;

        icon.textWrappingMode =
            TextWrappingModes.NoWrap;

        icon.raycastTarget =
            false;


        //=========================================================
        // MOVE TEXT SLIGHTLY RIGHT
        //=========================================================

        textRect.offsetMin =
            new Vector2(
                55f,
                0f
            );

        textRect.offsetMax =
            new Vector2(
                -10f,
                0f
            );


        //=========================================================
        // AUTO CONNECT ONCLICK
        //=========================================================

        button.onClick.RemoveAllListeners();


        UnityEventTools.AddPersistentListener(
            button.onClick,
            controller.OpenMission
        );


        //=========================================================
        // SAVE
        //=========================================================

        EditorUtility.SetDirty(
            buttonObject
        );

        EditorUtility.SetDirty(
            controller
        );


        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );


        Selection.activeGameObject =
            buttonObject;


        Debug.Log(
            "[Mission Button] Đã tạo MissionButton " +
            "và tự động nối OpenMission()."
        );
    }


    //=============================================================
    // FIND ROUNDED RECT
    //=============================================================

    private static Sprite FindRoundedRect()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "rounded_rect t:Sprite"
            );


        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );


            Sprite sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    path
                );


            if (sprite != null)
            {
                return sprite;
            }
        }


        return null;
    }
}

#endif