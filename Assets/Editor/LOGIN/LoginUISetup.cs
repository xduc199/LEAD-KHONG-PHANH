#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public static class LoginUISetup
{
    //=============================================================
    // MENU
    //=============================================================

    [MenuItem("LEAD KHONG PHANH/UI/Setup Login UI")]
    public static void SetupLoginUI()
    {
        //=========================================================
        // CANVAS
        //=========================================================

        GameObject canvasObject =
            GameObject.Find("LoginCanvas");

        if (canvasObject == null)
        {
            canvasObject = new GameObject(
                "LoginCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
        }

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f, 1080f);

        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        scaler.matchWidthOrHeight = 0.5f;


        //=========================================================
        // RAYCASTER
        //=========================================================

        GraphicRaycaster raycaster =
            canvasObject.GetComponent<GraphicRaycaster>();

        if (raycaster == null)
        {
            raycaster =
                canvasObject.AddComponent<GraphicRaycaster>();
        }

        raycaster.enabled = true;


        //=========================================================
        // EVENT SYSTEM
        //=========================================================

        EventSystem eventSystem =
            Object.FindFirstObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            GameObject eventSystemObject =
                new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule)
                );

            eventSystem =
                eventSystemObject.GetComponent<EventSystem>();
        }
        else
        {
            StandaloneInputModule inputModule =
                eventSystem.GetComponent<StandaloneInputModule>();

            if (inputModule == null)
            {
                eventSystemObjectAddInputModule(eventSystem);
            }
        }


        //=========================================================
        // BACKGROUND
        //=========================================================

        GameObject background =
            GetOrCreateUIObject(
                "Background",
                canvasObject.transform
            );

        SetFullStretch(background);

        Image backgroundImage =
            GetOrAddComponent<Image>(background);

        backgroundImage.color =
            HexColor("#080A0D");


        //=========================================================
        // DECORATIVE TOP STRIPE
        //=========================================================

        GameObject topStripe =
            GetOrCreateUIObject(
                "TopStripe",
                canvasObject.transform
            );

        SetTopStripe(topStripe);

        Image stripeImage =
            GetOrAddComponent<Image>(topStripe);

        stripeImage.color =
            HexColor("#E52B20");


        //=========================================================
        // LOGIN PANEL
        //=========================================================

        GameObject loginPanel =
            GetOrCreateUIObject(
                "LoginPanel",
                canvasObject.transform
            );

        SetCenter(
            loginPanel,
            650f,
            820f
        );

        Image panelImage =
            GetOrAddComponent<Image>(loginPanel);

        panelImage.color =
            HexColor("#171B21");


        //=========================================================
        // PANEL ACCENT
        //=========================================================

        GameObject panelAccent =
            GetOrCreateUIObject(
                "PanelAccent",
                loginPanel.transform
            );

        SetRect(
            panelAccent,
            0f,
            360f,
            560f,
            6f
        );

        Image accentImage =
            GetOrAddComponent<Image>(panelAccent);

        accentImage.color =
            HexColor("#E52B20");


        //=========================================================
        // TITLE
        //=========================================================

        GameObject title =
            GetOrCreateUIObject(
                "TitleText",
                loginPanel.transform
            );

        SetRect(
            title,
            0f,
            285f,
            580f,
            80f
        );

        TMP_Text titleText =
            GetOrAddComponent<TextMeshProUGUI>(
                title
            );

        titleText.text =
            "LEAD KHÔNG PHANH";

        titleText.fontSize = 46f;

        titleText.fontStyle =
            FontStyles.Bold;

        titleText.alignment =
            TextAlignmentOptions.Center;

        titleText.color =
            Color.white;


        //=========================================================
        // SUBTITLE
        //=========================================================

        GameObject subtitle =
            GetOrCreateUIObject(
                "SubtitleText",
                loginPanel.transform
            );

        SetRect(
            subtitle,
            0f,
            225f,
            580f,
            40f
        );

        TMP_Text subtitleText =
            GetOrAddComponent<TextMeshProUGUI>(
                subtitle
            );

        subtitleText.text =
            "ĐĂNG NHẬP ĐỂ TIẾP TỤC CUỘC CHƠI";

        subtitleText.fontSize = 18f;

        subtitleText.alignment =
            TextAlignmentOptions.Center;

        subtitleText.color =
            HexColor("#8D949E");


        //=========================================================
        // EMAIL
        //=========================================================

        GameObject emailInput =
            CreateInputField(
                "EmailInput",
                loginPanel.transform,
                "Email"
            );

        SetRect(
            emailInput,
            0f,
            125f,
            540f,
            68f
        );


        //=========================================================
        // PASSWORD
        //=========================================================

        GameObject passwordInput =
            CreateInputField(
                "PasswordInput",
                loginPanel.transform,
                "Mật khẩu"
            );

        SetRect(
            passwordInput,
            0f,
            40f,
            540f,
            68f
        );

        TMP_InputField passwordField =
            passwordInput.GetComponent<TMP_InputField>();

        passwordField.contentType =
            TMP_InputField.ContentType.Password;


        //=========================================================
        // LOGIN BUTTON
        //=========================================================

        GameObject loginButton =
            CreateButton(
                "LoginButton",
                loginPanel.transform,
                "ĐĂNG NHẬP",
                HexColor("#E52B20"),
                Color.white,
                25f
            );

        SetRect(
            loginButton,
            0f,
            -65f,
            540f,
            70f
        );


        //=========================================================
        // OR DIVIDER
        //=========================================================

        CreateDivider(
            loginPanel.transform
        );


        //=========================================================
        // GOOGLE BUTTON
        //=========================================================

        GameObject googleButton =
            CreateGoogleButton(
                loginPanel.transform
            );

        SetRect(
            googleButton,
            0f,
            -185f,
            540f,
            68f
        );


        //=========================================================
        // REGISTER
        //=========================================================

        GameObject registerText =
            GetOrCreateUIObject(
                "RegisterPrompt",
                loginPanel.transform
            );

        SetRect(
            registerText,
            0f,
            -275f,
            540f,
            45f
        );

        TMP_Text registerPrompt =
            GetOrAddComponent<TextMeshProUGUI>(
                registerText
            );

        registerPrompt.text =
            "Chưa có tài khoản?";

        registerPrompt.fontSize = 17f;

        registerPrompt.alignment =
            TextAlignmentOptions.Center;

        registerPrompt.color =
            HexColor("#858C96");


        GameObject registerButton =
            CreateTextButton(
                "RegisterButton",
                loginPanel.transform,
                "ĐĂNG KÝ"
            );

        SetRect(
            registerButton,
            0f,
            -320f,
            540f,
            45f
        );


        //=========================================================
        // STATUS
        //=========================================================

        GameObject status =
            GetOrCreateUIObject(
                "StatusText",
                loginPanel.transform
            );

        SetRect(
            status,
            0f,
            -370f,
            540f,
            35f
        );

        TMP_Text statusText =
            GetOrAddComponent<TextMeshProUGUI>(
                status
            );

        statusText.text = "";

        statusText.fontSize = 17f;

        statusText.alignment =
            TextAlignmentOptions.Center;

        statusText.color =
            HexColor("#F0B429");


        //=========================================================
        // SAVE
        //=========================================================

        EditorUtility.SetDirty(canvasObject);

        EditorUtility.SetDirty(loginPanel);

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager
                .GetActiveScene()
        );

        Selection.activeGameObject =
            canvasObject;


        Debug.Log(
            "========================================\n" +
            "[Login UI] SETUP HOÀN TẤT\n" +
            "Canvas          : OK\n" +
            "Background      : OK\n" +
            "LoginPanel      : OK\n" +
            "Title           : OK\n" +
            "Email           : OK\n" +
            "Password        : OK\n" +
            "Login Button    : OK\n" +
            "Google Button   : OK\n" +
            "Register        : OK\n" +
            "Status          : OK\n" +
            "EventSystem     : OK\n" +
            "========================================"
        );
    }


    //=============================================================
    // GOOGLE BUTTON
    //=============================================================

    private static GameObject CreateGoogleButton(
        Transform parent
    )
    {
        GameObject existing =
            parent.Find("GoogleLoginButton")?.gameObject;

        if (existing != null)
            return existing;

        GameObject buttonObject =
            new GameObject(
                "GoogleLoginButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );

        buttonObject.transform.SetParent(
            parent,
            false
        );

        Image image =
            buttonObject.GetComponent<Image>();

        image.color =
            Color.white;

        image.raycastTarget = true;


        Button button =
            buttonObject.GetComponent<Button>();

        button.interactable = true;

        button.targetGraphic = image;


        //=========================================================
        // GOOGLE "G"
        //=========================================================

        GameObject googleIcon =
            new GameObject(
                "GoogleIcon",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        googleIcon.transform.SetParent(
            buttonObject.transform,
            false
        );

        RectTransform iconRect =
            googleIcon.GetComponent<RectTransform>();

        iconRect.anchorMin =
            new Vector2(0f, 0.5f);

        iconRect.anchorMax =
            new Vector2(0f, 0.5f);

        iconRect.pivot =
            new Vector2(0f, 0.5f);

        iconRect.anchoredPosition =
            new Vector2(150f, 0f);

        iconRect.sizeDelta =
            new Vector2(45f, 50f);


        TMP_Text iconText =
            googleIcon.GetComponent<TMP_Text>();

        iconText.text = "G";

        iconText.fontSize = 28f;

        iconText.fontStyle =
            FontStyles.Bold;

        iconText.alignment =
            TextAlignmentOptions.Center;

        // Google-style colored G approximation.
        iconText.color =
            HexColor("#4285F4");


        //=========================================================
        // BUTTON LABEL
        //=========================================================

        GameObject label =
            new GameObject(
                "Text",
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
            new Vector2(0.5f, 0.5f);

        labelRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        labelRect.pivot =
            new Vector2(0.5f, 0.5f);

        labelRect.anchoredPosition =
            new Vector2(25f, 0f);

        labelRect.sizeDelta =
            new Vector2(390f, 55f);


        TMP_Text labelText =
            label.GetComponent<TMP_Text>();

        labelText.text =
            "ĐĂNG NHẬP VỚI GOOGLE";

        labelText.fontSize = 20f;

        labelText.fontStyle =
            FontStyles.Bold;

        labelText.alignment =
            TextAlignmentOptions.Center;

        labelText.color =
            HexColor("#202124");


        return buttonObject;
    }


    //=============================================================
    // DIVIDER
    //=============================================================

    private static void CreateDivider(
        Transform parent
    )
    {
        GameObject divider =
            GetOrCreateUIObject(
                "OrDivider",
                parent
            );

        SetRect(
            divider,
            0f,
            -125f,
            540f,
            40f
        );


        // LEFT LINE

        GameObject left =
            GetOrCreateUIObject(
                "LeftLine",
                divider.transform
            );

        SetRect(
            left,
            -185f,
            0f,
            145f,
            1f
        );

        Image leftImage =
            GetOrAddComponent<Image>(left);

        leftImage.color =
            HexColor("#383E47");


        // TEXT

        GameObject text =
            GetOrCreateUIObject(
                "Text",
                divider.transform
            );

        SetRect(
            text,
            0f,
            0f,
            80f,
            35f
        );

        TMP_Text dividerText =
            GetOrAddComponent<TextMeshProUGUI>(
                text
            );

        dividerText.text =
            "HOẶC";

        dividerText.fontSize = 15f;

        dividerText.fontStyle =
            FontStyles.Bold;

        dividerText.alignment =
            TextAlignmentOptions.Center;

        dividerText.color =
            HexColor("#6F7680");


        // RIGHT LINE

        GameObject right =
            GetOrCreateUIObject(
                "RightLine",
                divider.transform
            );

        SetRect(
            right,
            185f,
            0f,
            145f,
            1f
        );

        Image rightImage =
            GetOrAddComponent<Image>(right);

        rightImage.color =
            HexColor("#383E47");
    }


    //=============================================================
    // INPUT FIELD
    //=============================================================

    private static GameObject CreateInputField(
        string objectName,
        Transform parent,
        string placeholder
    )
    {
        GameObject existing =
            parent.Find(objectName)?.gameObject;

        if (existing != null)
            return existing;

        GameObject inputObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(TMP_InputField)
            );

        inputObject.transform.SetParent(
            parent,
            false
        );

        Image background =
            inputObject.GetComponent<Image>();

        background.color =
            HexColor("#0E1115");

        background.raycastTarget = true;


        // TEXT AREA

        GameObject textArea =
            new GameObject(
                "Text Area",
                typeof(RectTransform)
            );

        textArea.transform.SetParent(
            inputObject.transform,
            false
        );

        RectTransform textAreaRect =
            textArea.GetComponent<RectTransform>();

        textAreaRect.anchorMin =
            Vector2.zero;

        textAreaRect.anchorMax =
            Vector2.one;

        textAreaRect.offsetMin =
            new Vector2(24f, 6f);

        textAreaRect.offsetMax =
            new Vector2(-24f, -6f);


        // TEXT

        GameObject text =
            new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        text.transform.SetParent(
            textArea.transform,
            false
        );

        RectTransform textRect =
            text.GetComponent<RectTransform>();

        textRect.anchorMin =
            Vector2.zero;

        textRect.anchorMax =
            Vector2.one;

        textRect.offsetMin =
            Vector2.zero;

        textRect.offsetMax =
            Vector2.zero;

        TMP_Text textComponent =
            text.GetComponent<TextMeshProUGUI>();

        textComponent.fontSize = 22f;

        textComponent.color =
            Color.white;

        textComponent.alignment =
            TextAlignmentOptions.MidlineLeft;


        // PLACEHOLDER

        GameObject placeholderObject =
            new GameObject(
                "Placeholder",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        placeholderObject.transform.SetParent(
            textArea.transform,
            false
        );

        RectTransform placeholderRect =
            placeholderObject.GetComponent<RectTransform>();

        placeholderRect.anchorMin =
            Vector2.zero;

        placeholderRect.anchorMax =
            Vector2.one;

        placeholderRect.offsetMin =
            Vector2.zero;

        placeholderRect.offsetMax =
            Vector2.zero;

        TMP_Text placeholderText =
            placeholderObject.GetComponent<
                TextMeshProUGUI
            >();

        placeholderText.text =
            placeholder;

        placeholderText.fontSize = 22f;

        placeholderText.color =
            HexColor("#686F79");

        placeholderText.alignment =
            TextAlignmentOptions.MidlineLeft;


        // INPUT FIELD

        TMP_InputField inputField =
            inputObject.GetComponent<
                TMP_InputField
            >();

        inputField.textViewport =
            textAreaRect;

        inputField.textComponent =
            textComponent;

        inputField.placeholder =
            placeholderText;

        inputField.interactable = true;

        inputField.targetGraphic =
            background;

        return inputObject;
    }


    //=============================================================
    // NORMAL BUTTON
    //=============================================================

    private static GameObject CreateButton(
        string objectName,
        Transform parent,
        string label,
        Color backgroundColor,
        Color textColor,
        float fontSize
    )
    {
        GameObject existing =
            parent.Find(objectName)?.gameObject;

        if (existing != null)
            return existing;

        GameObject buttonObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );

        buttonObject.transform.SetParent(
            parent,
            false
        );

        Image image =
            buttonObject.GetComponent<Image>();

        image.color =
            backgroundColor;

        image.raycastTarget = true;


        Button button =
            buttonObject.GetComponent<Button>();

        button.interactable = true;

        button.targetGraphic = image;


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

        TMP_Text text =
            textObject.GetComponent<TMP_Text>();

        text.text =
            label;

        text.fontSize =
            fontSize;

        text.fontStyle =
            FontStyles.Bold;

        text.alignment =
            TextAlignmentOptions.Center;

        text.color =
            textColor;

        return buttonObject;
    }


    //=============================================================
    // TEXT BUTTON
    //=============================================================

    private static GameObject CreateTextButton(
        string objectName,
        Transform parent,
        string label
    )
    {
        GameObject existing =
            parent.Find(objectName)?.gameObject;

        if (existing != null)
            return existing;

        GameObject buttonObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Button)
            );

        buttonObject.transform.SetParent(
            parent,
            false
        );


        Button button =
            buttonObject.GetComponent<Button>();

        button.interactable = true;


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


        TMP_Text text =
            textObject.GetComponent<TMP_Text>();

        text.text =
            label;

        text.fontSize = 18f;

        text.fontStyle =
            FontStyles.Bold;

        text.alignment =
            TextAlignmentOptions.Center;

        text.color =
            HexColor("#E52B20");

        return buttonObject;
    }


    //=============================================================
    // HELPERS
    //=============================================================

    private static GameObject GetOrCreateUIObject(
        string objectName,
        Transform parent
    )
    {
        Transform existing =
            parent.Find(objectName);

        if (existing != null)
            return existing.gameObject;

        GameObject objectCreated =
            new GameObject(
                objectName,
                typeof(RectTransform)
            );

        objectCreated.transform.SetParent(
            parent,
            false
        );

        return objectCreated;
    }


    private static T GetOrAddComponent<T>(
        GameObject target
    )
        where T : Component
    {
        T component =
            target.GetComponent<T>();

        if (component == null)
        {
            component =
                target.AddComponent<T>();
        }

        return component;
    }


    private static void SetFullStretch(
        GameObject target
    )
    {
        RectTransform rect =
            target.GetComponent<RectTransform>();

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


    private static void SetTopStripe(
        GameObject target
    )
    {
        RectTransform rect =
            target.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.pivot =
            new Vector2(0.5f, 1f);

        rect.anchoredPosition =
            Vector2.zero;

        rect.sizeDelta =
            new Vector2(0f, 8f);
    }


    private static void SetCenter(
        GameObject target,
        float width,
        float height
    )
    {
        RectTransform rect =
            target.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.anchoredPosition =
            Vector2.zero;

        rect.sizeDelta =
            new Vector2(width, height);

        rect.localScale =
            Vector3.one;
    }


    private static void SetRect(
        GameObject target,
        float x,
        float y,
        float width,
        float height
    )
    {
        RectTransform rect =
            target.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.anchoredPosition =
            new Vector2(x, y);

        rect.sizeDelta =
            new Vector2(width, height);

        rect.localScale =
            Vector3.one;
    }


    private static Color HexColor(
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


    private static void eventSystemObjectAddInputModule(
        EventSystem eventSystem
    )
    {
        eventSystem.gameObject.AddComponent<
            StandaloneInputModule
        >();
    }
}

#endif