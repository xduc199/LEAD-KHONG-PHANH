#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BootSetup
{
    private const string BootSystemName = "BootSystem";
    private const string BootCanvasName = "BootUI";

    private const string BackgroundName = "Background";
    private const string LogoName = "Logo";
    private const string SkipButtonName = "SkipButton";


    //=============================================================
    // MENU
    //=============================================================

    [MenuItem("Tools/Lead Khong Phanh/Boot/Setup Boot Scene")]
    private static void SetupBootScene()
    {
        Debug.Log(
            "[BootSetup] ========================================"
        );

        Debug.Log(
            "[BootSetup] Starting Boot Scene setup..."
        );


        //=========================================================
        // 1. REMOVE OLD BOOT UI
        //=========================================================

        RemoveOldBootObjects();


        //=========================================================
        // 2. CREATE BOOT SYSTEM
        //=========================================================

        GameObject bootSystem =
            CreateBootSystem();


        //=========================================================
        // 3. CREATE BOOT CANVAS
        //=========================================================

        GameObject bootCanvas =
            CreateBootCanvas();


        //=========================================================
        // 4. CREATE BACKGROUND
        //=========================================================

        GameObject background =
            CreateBackground(
                bootCanvas.transform
            );


        //=========================================================
        // 5. CREATE LOGO
        //=========================================================

        GameObject logo =
            CreateLogo(
                bootCanvas.transform
            );


        //=========================================================
        // 6. CREATE SKIP BUTTON
        //=========================================================

        GameObject skipButton =
            CreateSkipButton(
                bootCanvas.transform
            );


        //=========================================================
        // 7. ADD BOOT CONTROLLER
        //=========================================================

        BootController controller =
            bootSystem.GetComponent<BootController>();

        if (controller == null)
        {
            controller =
                bootSystem.AddComponent<BootController>();
        }


        //=========================================================
        // 8. WIRE REFERENCES
        //=========================================================

        WireBootController(
            controller,
            bootCanvas,
            logo,
            skipButton
        );


        //=========================================================
        // 9. SELECT BOOT SYSTEM
        //=========================================================

        Selection.activeGameObject =
            bootSystem;

        EditorGUIUtility.PingObject(
            bootSystem
        );


        //=========================================================
        // 10. SAVE
        //=========================================================

        EditorUtility.SetDirty(
            bootSystem
        );

        EditorUtility.SetDirty(
            bootCanvas
        );

        AssetDatabase.SaveAssets();


        Debug.Log(
            "[BootSetup] Boot Scene setup completed."
        );

        Debug.Log(
            "[BootSetup] ========================================"
        );

        EditorUtility.DisplayDialog(
            "Boot Setup",
            "Đã dựng lại Boot Scene.\n\n" +
            "BootSystem\n" +
            "BootUI\n" +
            "Background\n" +
            "Logo\n" +
            "SkipButton\n\n" +
            "Boot UI sử dụng Screen Space - Overlay.",
            "OK"
        );
    }


    //=============================================================
    // REMOVE OLD OBJECTS
    //=============================================================

    private static void RemoveOldBootObjects()
    {
        GameObject oldBootSystem =
            GameObject.Find(
                BootSystemName
            );

        if (oldBootSystem != null)
        {
            Object.DestroyImmediate(
                oldBootSystem
            );

            Debug.Log(
                "[BootSetup] Removed old BootSystem."
            );
        }


        GameObject oldCanvas =
            GameObject.Find(
                BootCanvasName
            );

        if (oldCanvas != null)
        {
            Object.DestroyImmediate(
                oldCanvas
            );

            Debug.Log(
                "[BootSetup] Removed old BootUI."
            );
        }
    }


    //=============================================================
    // CREATE BOOT SYSTEM
    //=============================================================

    private static GameObject CreateBootSystem()
    {
        GameObject bootSystem =
            new GameObject(
                BootSystemName
            );

        Undo.RegisterCreatedObjectUndo(
            bootSystem,
            "Create BootSystem"
        );

        return bootSystem;
    }


    //=============================================================
    // CREATE CANVAS
    //=============================================================

    private static GameObject CreateBootCanvas()
    {
        GameObject canvasObject =
            new GameObject(
                BootCanvasName
            );

        Undo.RegisterCreatedObjectUndo(
            canvasObject,
            "Create BootUI"
        );


        Canvas canvas =
            canvasObject.AddComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder =
            10000;


        CanvasScaler scaler =
            canvasObject.AddComponent<CanvasScaler>();

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


        canvasObject.AddComponent<
            GraphicRaycaster
        >();


        return canvasObject;
    }


    //=============================================================
    // CREATE BACKGROUND
    //=============================================================

    private static GameObject CreateBackground(
        Transform parent
    )
    {
        GameObject background =
            new GameObject(
                BackgroundName
            );

        Undo.RegisterCreatedObjectUndo(
            background,
            "Create Boot Background"
        );

        background.transform.SetParent(
            parent,
            false
        );


        RectTransform rect =
            background.AddComponent<
                RectTransform
            >();

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;


        Image image =
            background.AddComponent<Image>();

        // Màu nền đen hoàn toàn.
        image.color =
            new Color(
                0f,
                0f,
                0f,
                1f
            );

        image.raycastTarget =
            true;


        return background;
    }


    //=============================================================
    // CREATE LOGO
    //=============================================================

    private static GameObject CreateLogo(
        Transform parent
    )
    {
        GameObject logo =
            new GameObject(
                LogoName
            );

        Undo.RegisterCreatedObjectUndo(
            logo,
            "Create Boot Logo"
        );

        logo.transform.SetParent(
            parent,
            false
        );


        RectTransform rect =
            logo.AddComponent<
                RectTransform
            >();

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

        rect.sizeDelta =
            new Vector2(
                700f,
                400f
            );

        rect.localScale =
            Vector3.one;


        Image image =
            logo.AddComponent<Image>();

        image.color =
            Color.white;

        image.raycastTarget =
            false;


        return logo;
    }


    //=============================================================
    // CREATE SKIP BUTTON
    //=============================================================

    private static GameObject CreateSkipButton(
        Transform parent
    )
    {
        GameObject skipButton =
            new GameObject(
                SkipButtonName
            );

        Undo.RegisterCreatedObjectUndo(
            skipButton,
            "Create Skip Button"
        );

        skipButton.transform.SetParent(
            parent,
            false
        );


        RectTransform rect =
            skipButton.AddComponent<
                RectTransform
            >();

        rect.anchorMin =
            new Vector2(
                1f,
                0f
            );

        rect.anchorMax =
            new Vector2(
                1f,
                0f
            );

        rect.pivot =
            new Vector2(
                1f,
                0f
            );

        rect.anchoredPosition =
            new Vector2(
                -60f,
                50f
            );

        rect.sizeDelta =
            new Vector2(
                180f,
                60f
            );


        Image image =
            skipButton.AddComponent<Image>();

        image.color =
            new Color(
                1f,
                1f,
                1f,
                0.15f
            );


        Button button =
            skipButton.AddComponent<Button>();

        button.targetGraphic =
            image;


        // Không thêm TextMeshPro tự động.
        // Button vẫn có thể gọi BootController.SkipIntro().
        return skipButton;
    }


    //=============================================================
    // WIRE CONTROLLER
    //=============================================================

    private static void WireBootController(
        BootController controller,
        GameObject bootCanvas,
        GameObject logo,
        GameObject skipButton
    )
    {
        SerializedObject serializedObject =
            new SerializedObject(
                controller
            );

        SerializedProperty introCanvasGroup =
            serializedObject.FindProperty(
                "introCanvasGroup"
            );

        SerializedProperty logoTransform =
            serializedObject.FindProperty(
                "logoTransform"
            );

        SerializedProperty skipButtonProperty =
            serializedObject.FindProperty(
                "skipButton"
            );


        CanvasGroup canvasGroup =
            bootCanvas.GetComponent<
                CanvasGroup
            >();

        if (canvasGroup == null)
        {
            canvasGroup =
                bootCanvas.AddComponent<
                    CanvasGroup
                >();
        }


        if (introCanvasGroup != null)
        {
            introCanvasGroup.objectReferenceValue =
                canvasGroup;
        }


        if (logoTransform != null)
        {
            logoTransform.objectReferenceValue =
                logo.GetComponent<
                    RectTransform
                >();
        }


        if (skipButtonProperty != null)
        {
            skipButtonProperty.objectReferenceValue =
                skipButton;
        }


        serializedObject.ApplyModifiedPropertiesWithoutUndo();


        Debug.Log(
            "[BootSetup] BootController references wired."
        );
    }
}

#endif