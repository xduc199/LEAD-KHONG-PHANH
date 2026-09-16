#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class LoginPasswordToggleSetup
{
    private const string MENU_PATH =
        "Tools/LEAD KHONG PHANH/Setup Password Toggle";

    [MenuItem(MENU_PATH)]
    public static void SetupPasswordToggle()
    {
        GameObject passwordInputObject =
            GameObject.Find("PasswordInput");

        if (passwordInputObject == null)
        {
            Debug.LogError(
                "[Login UI] Không tìm thấy GameObject 'PasswordInput'."
            );

            return;
        }

        TMP_InputField passwordInput =
            passwordInputObject.GetComponent<TMP_InputField>();

        if (passwordInput == null)
        {
            Debug.LogError(
                "[Login UI] PasswordInput không có TMP_InputField."
            );

            return;
        }

        GameObject toggleObject =
            FindOrCreateToggle(passwordInputObject);

        if (toggleObject == null)
        {
            Debug.LogError(
                "[Login UI] Không thể tạo PasswordToggleButton."
            );

            return;
        }

        ConfigureToggle(toggleObject);

        CreateEyeIcon(toggleObject.transform);

        ConnectToLoginController(
            toggleObject.GetComponent<Button>()
        );

        Selection.activeGameObject =
            toggleObject;

        Debug.Log(
            "[Login UI] Password Toggle đã được setup thành công."
        );
    }

    private static GameObject FindOrCreateToggle(
        GameObject passwordInputObject
    )
    {
        Transform existing =
            passwordInputObject.transform.Find(
                "PasswordToggleButton"
            );

        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject toggleObject =
            new GameObject(
                "PasswordToggleButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );

        Undo.RegisterCreatedObjectUndo(
            toggleObject,
            "Create Password Toggle"
        );

        toggleObject.transform.SetParent(
            passwordInputObject.transform,
            false
        );

        return toggleObject;
    }

    private static void ConfigureToggle(
        GameObject toggleObject
    )
    {
        RectTransform rect =
            toggleObject.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(1f, 0.5f);

        rect.anchorMax =
            new Vector2(1f, 0.5f);

        rect.pivot =
            new Vector2(1f, 0.5f);

        rect.sizeDelta =
            new Vector2(55f, 55f);

        rect.anchoredPosition =
            new Vector2(-8f, 0f);

        Image image =
            toggleObject.GetComponent<Image>();

        image.color =
            new Color(
                1f,
                1f,
                1f,
                0f
            );

        Button button =
            toggleObject.GetComponent<Button>();

        button.transition =
            Selectable.Transition.ColorTint;

        button.targetGraphic = image;
    }

    private static void CreateEyeIcon(
        Transform parent
    )
    {
        Transform existing =
            parent.Find("EyeIcon");

        GameObject eyeObject;

        if (existing != null)
        {
            eyeObject = existing.gameObject;
        }
        else
        {
            eyeObject =
                new GameObject(
                    "EyeIcon",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI)
                );

            eyeObject.transform.SetParent(
                parent,
                false
            );

            Undo.RegisterCreatedObjectUndo(
                eyeObject,
                "Create Eye Icon"
            );
        }

        RectTransform rect =
            eyeObject.GetComponent<RectTransform>();

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        TextMeshProUGUI text =
            eyeObject.GetComponent<TextMeshProUGUI>();

        text.text = "👁";
        text.fontSize = 22f;
        text.alignment =
            TextAlignmentOptions.Center;

        text.raycastTarget = false;

        text.color = Color.white;
    }

    private static void ConnectToLoginController(
        Button toggleButton
    )
    {
        LoginUIController controller =
            Object.FindFirstObjectByType<LoginUIController>();

        if (controller == null)
        {
            Debug.LogWarning(
                "[Login UI] Không tìm thấy LoginUIController trong Scene."
            );

            return;
        }

        SerializedObject serializedObject =
            new SerializedObject(controller);

        SerializedProperty property =
            serializedObject.FindProperty(
                "passwordToggleButton"
            );

        if (property == null)
        {
            Debug.LogError(
                "[Login UI] Không tìm thấy field passwordToggleButton."
            );

            return;
        }

        property.objectReferenceValue =
            toggleButton;

        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(controller);

        Debug.Log(
            "[Login UI] Đã tự động nối PasswordToggleButton."
        );
    }
}

#endif