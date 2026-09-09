using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

public static class GameplayHUDVisualSetup
{
    //=============================================================
    // COLORS
    //=============================================================

    private static readonly Color White =
        new Color32(245, 247, 250, 255);

    private static readonly Color Gold =
        new Color32(255, 203, 45, 255);

    private static readonly Color Secondary =
        new Color32(190, 195, 205, 255);

    private static readonly Color PauseBackground =
        new Color32(25, 30, 38, 210);

    private static readonly Color PauseHighlight =
        new Color32(255, 255, 255, 32);

    private static readonly Color PauseShadow =
        new Color32(0, 0, 0, 85);


    //=============================================================
    // ASSET
    //=============================================================

    private const string RoundedRectName = "rounded_rect";


    //=============================================================
    // G-02.1
    //=============================================================

    [MenuItem("LEAD KHONG PHANH/UI/Apply G-02.1 Typography")]
    public static void ApplyTypography()
    {
        GameObject canvas = GameObject.Find("GameplayCanvas");

        if (canvas == null)
        {
            Debug.LogError(
                "[HUD Setup] Không tìm thấy GameplayCanvas."
            );
            return;
        }

        bool anyFound = false;


        //=========================================================
        // SCORE
        //=========================================================

        TMP_Text scoreLabel =
            Find<TMP_Text>("ScoreBlock/Label");

        TMP_Text scoreValue =
            Find<TMP_Text>("ScoreBlock/Value");

        if (scoreLabel != null)
        {
            StyleText(
                scoreLabel,
                18,
                White,
                FontStyles.Bold,
                TextAlignmentOptions.Left
            );

            anyFound = true;
        }

        if (scoreValue != null)
        {
            StyleText(
                scoreValue,
                54,
                White,
                FontStyles.Bold,
                TextAlignmentOptions.Left
            );

            anyFound = true;
        }


        //=========================================================
        // COIN
        //=========================================================

        TMP_Text coinValue =
            Find<TMP_Text>("RightInfo/CoinBlock/Value");

        Image coinIcon =
            Find<Image>("RightInfo/CoinBlock/Icon");

        if (coinValue != null)
        {
            StyleText(
                coinValue,
                30,
                Gold,
                FontStyles.Bold,
                TextAlignmentOptions.Right
            );

            anyFound = true;
        }

        if (coinIcon != null)
        {
            SetSize(
                coinIcon.rectTransform,
                28f,
                28f
            );

            anyFound = true;
        }


        //=========================================================
        // DISTANCE
        //=========================================================

        TMP_Text distanceLabel =
            Find<TMP_Text>("RightInfo/DistanceBlock/Label");

        TMP_Text distanceValue =
            Find<TMP_Text>("RightInfo/DistanceBlock/Value");

        if (distanceLabel != null)
        {
            StyleText(
                distanceLabel,
                16,
                White,
                FontStyles.Bold,
                TextAlignmentOptions.Right
            );

            anyFound = true;
        }

        if (distanceValue != null)
        {
            StyleText(
                distanceValue,
                24,
                Gold,
                FontStyles.Bold,
                TextAlignmentOptions.Right
            );

            anyFound = true;
        }


        //=========================================================
        // SPEED
        //=========================================================

        TMP_Text speedValue =
            Find<TMP_Text>("SpeedHUD/SpeedValue");

        TMP_Text speedUnit =
            Find<TMP_Text>("SpeedHUD/Unit");

        Image speedAccent =
            Find<Image>("SpeedHUD/Accent");

        if (speedValue != null)
        {
            StyleText(
                speedValue,
                72,
                White,
                FontStyles.Bold,
                TextAlignmentOptions.Right
            );

            anyFound = true;
        }

        if (speedUnit != null)
        {
            StyleText(
                speedUnit,
                17,
                Secondary,
                FontStyles.Bold,
                TextAlignmentOptions.Right
            );

            anyFound = true;
        }

        if (speedAccent != null)
        {
            SetSize(
                speedAccent.rectTransform,
                90f,
                4f
            );

            speedAccent.color = Gold;

            EditorUtility.SetDirty(speedAccent);

            anyFound = true;
        }


        //=========================================================
        // PAUSE
        //=========================================================

        Button pauseButton =
            Find<Button>("TopHUD/PauseButton");

        if (pauseButton != null)
        {
            SetupPauseButton(
                pauseButton
            );

            anyFound = true;
        }


        //=========================================================
        // PAUSE TEXT
        //=========================================================

        TMP_Text pauseText =
            Find<TMP_Text>(
                "TopHUD/PauseButton/Text (TMP)"
            );

        if (pauseText != null)
        {
            StyleText(
                pauseText,
                22,
                White,
                FontStyles.Bold,
                TextAlignmentOptions.Center
            );

            anyFound = true;
        }


        //=========================================================
        // SAVE
        //=========================================================

        MarkSceneDirty();

        Debug.Log(
            anyFound
                ? "[HUD Setup] G-02.1 Typography đã áp dụng."
                : "[HUD Setup] Không tìm thấy object nào."
        );
    }


    //=============================================================
    // G-02.2
    //=============================================================

    [MenuItem("LEAD KHONG PHANH/UI/Apply G-02.2 Plastic Surface")]
    public static void ApplyPlasticSurface()
    {
        GameObject canvas =
            GameObject.Find("GameplayCanvas");

        if (canvas == null)
        {
            Debug.LogError(
                "[HUD Plastic] Không tìm thấy GameplayCanvas."
            );

            return;
        }


        //=========================================================
        // FIND ROUNDED RECT
        //=========================================================

        Sprite roundedSprite =
            PrepareRoundedRectSprite();

        if (roundedSprite == null)
        {
            Debug.LogError(
                "[HUD Plastic] Không tìm thấy rounded_rect."
            );

            Debug.Log(
                "[HUD Plastic] Đặt file tại:\n" +
                "Assets/UI/Sprites/rounded_rect.png"
            );

            return;
        }


        //=========================================================
        // FIND PAUSE
        //=========================================================

        Button pauseButton =
            Find<Button>("TopHUD/PauseButton");

        if (pauseButton == null)
        {
            Debug.LogError(
                "[HUD Plastic] Không tìm thấy PauseButton."
            );

            return;
        }


        //=========================================================
        // PAUSE IMAGE
        //=========================================================

        Image pauseImage =
            pauseButton.GetComponent<Image>();

        if (pauseImage == null)
        {
            pauseImage =
                pauseButton.gameObject.AddComponent<Image>();
        }

        pauseImage.sprite =
            roundedSprite;

        pauseImage.type =
            Image.Type.Sliced;

        pauseImage.color =
            PauseBackground;

        pauseImage.raycastTarget =
            true;

        EditorUtility.SetDirty(
            pauseImage
        );


        //=========================================================
        // PAUSE RECT
        //=========================================================

        RectTransform pauseRect =
            pauseButton.GetComponent<RectTransform>();

        if (pauseRect != null)
        {
            pauseRect.anchorMin =
                new Vector2(1f, 1f);

            pauseRect.anchorMax =
                new Vector2(1f, 1f);

            pauseRect.pivot =
                new Vector2(1f, 1f);

            pauseRect.anchoredPosition =
                new Vector2(-10f, -10f);

            SetSize(
                pauseRect,
                64f,
                64f
            );
        }


        //=========================================================
        // SHADOW
        //=========================================================

        Shadow shadow =
            pauseButton.GetComponent<Shadow>();

        if (shadow == null)
        {
            shadow =
                pauseButton.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor =
            PauseShadow;

        shadow.effectDistance =
            new Vector2(0f, -3f);

        shadow.useGraphicAlpha =
            true;

        EditorUtility.SetDirty(
            shadow
        );


        //=========================================================
        // HIGHLIGHT
        //=========================================================

        CreateOrUpdatePauseHighlight(
            pauseButton,
            roundedSprite
        );


        //=========================================================
        // PAUSE TEXT
        //=========================================================

        TMP_Text pauseText =
            Find<TMP_Text>(
                "TopHUD/PauseButton/Text (TMP)"
            );

        if (pauseText != null)
        {
            StyleText(
                pauseText,
                22,
                White,
                FontStyles.Bold,
                TextAlignmentOptions.Center
            );

            RectTransform textRect =
                pauseText.rectTransform;

            textRect.anchorMin =
                Vector2.zero;

            textRect.anchorMax =
                Vector2.one;

            textRect.offsetMin =
                Vector2.zero;

            textRect.offsetMax =
                Vector2.zero;

            EditorUtility.SetDirty(
                textRect
            );
        }


        //=========================================================
        // SAVE
        //=========================================================

        MarkSceneDirty();

        Debug.Log(
            "[HUD Plastic] G-02.2 Plastic Surface đã áp dụng thành công."
        );
    }


    //=============================================================
    // PAUSE BUTTON
    //=============================================================

    private static void SetupPauseButton(
        Button pauseButton)
    {
        RectTransform rt =
            pauseButton.GetComponent<RectTransform>();

        if (rt != null)
        {
            rt.anchorMin =
                new Vector2(1f, 1f);

            rt.anchorMax =
                new Vector2(1f, 1f);

            rt.pivot =
                new Vector2(1f, 1f);

            rt.anchoredPosition =
                new Vector2(-10f, -10f);

            SetSize(
                rt,
                64f,
                64f
            );
        }

        Image image =
            pauseButton.GetComponent<Image>();

        if (image != null)
        {
            image.color =
                PauseBackground;

            EditorUtility.SetDirty(
                image
            );
        }
    }


    //=============================================================
    // ROUNDED RECT IMPORT
    //=============================================================

    private static Sprite PrepareRoundedRectSprite()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                RoundedRectName + " t:Texture2D"
            );

        if (guids == null ||
            guids.Length == 0)
        {
            return null;
        }


        string assetPath = null;


        //=========================================================
        // FIND EXACT NAME
        //=========================================================

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            string fileName =
                System.IO.Path.GetFileNameWithoutExtension(
                    path
                );

            if (fileName.ToLower() ==
                RoundedRectName.ToLower())
            {
                assetPath = path;
                break;
            }
        }


        if (string.IsNullOrEmpty(assetPath))
        {
            assetPath =
                AssetDatabase.GUIDToAssetPath(
                    guids[0]
                );
        }


        //=========================================================
        // IMPORTER
        //=========================================================

        TextureImporter importer =
            AssetImporter.GetAtPath(
                assetPath
            ) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError(
                "[HUD Plastic] Không lấy được TextureImporter."
            );

            return null;
        }


        importer.textureType =
            TextureImporterType.Sprite;

        importer.spriteImportMode =
            SpriteImportMode.Single;


        //=========================================================
        // SETTINGS
        //=========================================================

        TextureImporterSettings settings =
            new TextureImporterSettings();

        importer.ReadTextureSettings(
            settings
        );


        // Full Rect
        settings.spriteMeshType =
            SpriteMeshType.FullRect;


        // Border:
        // Vector4 = Left, Bottom, Right, Top
        settings.spriteBorder =
            new Vector4(
                42f,
                42f,
                42f,
                42f
            );


        importer.SetTextureSettings(
            settings
        );


        EditorUtility.SetDirty(
            importer
        );

        importer.SaveAndReimport();


        //=========================================================
        // LOAD SPRITE
        //=========================================================

        Sprite sprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(
                assetPath
            );

        if (sprite == null)
        {
            Debug.LogError(
                "[HUD Plastic] Không load được Sprite."
            );
        }

        return sprite;
    }


    //=============================================================
    // HIGHLIGHT
    //=============================================================

    private static void CreateOrUpdatePauseHighlight(
        Button pauseButton,
        Sprite roundedSprite)
    {
        Transform existing =
            pauseButton.transform.Find(
                "PlasticHighlight"
            );

        GameObject highlightObject;


        //=========================================================
        // CREATE
        //=========================================================

        if (existing == null)
        {
            highlightObject =
                new GameObject(
                    "PlasticHighlight",
                    typeof(RectTransform),
                    typeof(Image)
                );

            highlightObject.transform.SetParent(
                pauseButton.transform,
                false
            );
        }
        else
        {
            highlightObject =
                existing.gameObject;
        }


        //=========================================================
        // RECT
        //=========================================================

        RectTransform rect =
            highlightObject.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.pivot =
            new Vector2(0.5f, 1f);

        rect.anchoredPosition =
            Vector2.zero;

        rect.sizeDelta =
            new Vector2(
                -10f,
                8f
            );


        //=========================================================
        // IMAGE
        //=========================================================

        Image image =
            highlightObject.GetComponent<Image>();

        image.sprite =
            roundedSprite;

        image.type =
            Image.Type.Sliced;

        image.color =
            PauseHighlight;

        image.raycastTarget =
            false;


        // Đưa highlight xuống dưới Text
        highlightObject.transform.SetAsFirstSibling();


        EditorUtility.SetDirty(
            highlightObject
        );

        EditorUtility.SetDirty(
            rect
        );

        EditorUtility.SetDirty(
            image
        );
    }

        //=============================================================
    // G-02.3
    // PLASTIC DEPTH
    //=============================================================

    [MenuItem("LEAD KHONG PHANH/UI/Apply G-02.3 Plastic Depth")]
    public static void ApplyPlasticDepth()
    {
        GameObject canvas =
            GameObject.Find("GameplayCanvas");

        if (canvas == null)
        {
            Debug.LogError(
                "[HUD Plastic] Không tìm thấy GameplayCanvas."
            );

            return;
        }

        int appliedCount = 0;


        //=========================================================
        // SCORE
        //=========================================================

        TMP_Text scoreLabel =
            Find<TMP_Text>("ScoreBlock/Label");

        TMP_Text scoreValue =
            Find<TMP_Text>("ScoreBlock/Value");

        if (scoreLabel != null)
        {
            SetupTextShadow(
                scoreLabel,
                0.45f
            );

            appliedCount++;
        }

        if (scoreValue != null)
        {
            SetupTextShadow(
                scoreValue,
                0.75f
            );

            appliedCount++;
        }


        //=========================================================
        // COIN
        //=========================================================

        TMP_Text coinValue =
            Find<TMP_Text>(
                "RightInfo/CoinBlock/Value"
            );

        Image coinIcon =
            Find<Image>(
                "RightInfo/CoinBlock/Icon"
            );

        if (coinValue != null)
        {
            SetupTextShadow(
                coinValue,
                0.7f
            );

            appliedCount++;
        }

        if (coinIcon != null)
        {
            SetupGraphicShadow(
                coinIcon,
                0.65f
            );

            appliedCount++;
        }


        //=========================================================
        // DISTANCE
        //=========================================================

        TMP_Text distanceLabel =
            Find<TMP_Text>(
                "RightInfo/DistanceBlock/Label"
            );

        TMP_Text distanceValue =
            Find<TMP_Text>(
                "RightInfo/DistanceBlock/Value"
            );

        if (distanceLabel != null)
        {
            SetupTextShadow(
                distanceLabel,
                0.4f
            );

            appliedCount++;
        }

        if (distanceValue != null)
        {
            SetupTextShadow(
                distanceValue,
                0.6f
            );

            appliedCount++;
        }


        //=========================================================
        // SPEED
        //=========================================================

        TMP_Text speedValue =
            Find<TMP_Text>(
                "SpeedHUD/SpeedValue"
            );

        TMP_Text speedUnit =
            Find<TMP_Text>(
                "SpeedHUD/Unit"
            );

        if (speedValue != null)
        {
            SetupTextShadow(
                speedValue,
                0.85f
            );

            appliedCount++;
        }

        if (speedUnit != null)
        {
            SetupTextShadow(
                speedUnit,
                0.45f
            );

            appliedCount++;
        }


        //=========================================================
        // SPEED ACCENT
        //=========================================================

        Image speedAccent =
            Find<Image>(
                "SpeedHUD/Accent"
            );

        if (speedAccent != null)
        {
            SetupGraphicShadow(
                speedAccent,
                0.25f
            );

            appliedCount++;
        }


        //=========================================================
        // SAVE
        //=========================================================

        MarkSceneDirty();

        Debug.Log(
            "[HUD Plastic] G-02.3 Plastic Depth đã áp dụng. " +
            "Elements: " + appliedCount
        );
    }


    //=============================================================
    // TEXT SHADOW
    //=============================================================

    private static void SetupTextShadow(
        TMP_Text text,
        float alpha)
    {
        Shadow shadow =
            text.GetComponent<Shadow>();

        if (shadow == null)
        {
            shadow =
                text.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor =
            new Color(
                0f,
                0f,
                0f,
                alpha
            );

        shadow.effectDistance =
            new Vector2(
                1.5f,
                -1.5f
            );

        shadow.useGraphicAlpha =
            true;

        shadow.enabled =
            true;

        EditorUtility.SetDirty(
            shadow
        );
    }


    //=============================================================
    // GRAPHIC SHADOW
    //=============================================================

    private static void SetupGraphicShadow(
        Graphic graphic,
        float alpha)
    {
        Shadow shadow =
            graphic.GetComponent<Shadow>();

        if (shadow == null)
        {
            shadow =
                graphic.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor =
            new Color(
                0f,
                0f,
                0f,
                alpha
            );

        shadow.effectDistance =
            new Vector2(
                1.5f,
                -1.5f
            );

        shadow.useGraphicAlpha =
            true;

        shadow.enabled =
            true;

        EditorUtility.SetDirty(
            shadow
        );
    }

    //=============================================================
// G-02.4
// COIN + SCORE VISUAL GROUPING
//=============================================================

[MenuItem("LEAD KHONG PHANH/UI/Apply G-02.4 HUD Grouping")]
public static void ApplyHUDGrouping()
{
    GameObject canvas =
        GameObject.Find("GameplayCanvas");

    if (canvas == null)
    {
        Debug.LogError(
            "[HUD Grouping] Không tìm thấy GameplayCanvas."
        );

        return;
    }

    Sprite roundedSprite =
        PrepareRoundedRectSprite();

    if (roundedSprite == null)
    {
        Debug.LogError(
            "[HUD Grouping] Không tìm thấy rounded_rect."
        );

        return;
    }


    //=========================================================
    // SCORE
    //=========================================================

    Transform scoreBlock =
        FindTransform("ScoreBlock");

    if (scoreBlock != null)
    {
        CreateOrUpdateHUDSurface(
            scoreBlock,
            roundedSprite,
            "ScorePlasticSurface",
            new Color32(20, 24, 31, 115),
            14f
        );
    }


    //=========================================================
    // COIN
    //=========================================================

    Transform coinBlock =
        FindTransform(
            "RightInfo/CoinBlock"
        );

    if (coinBlock != null)
    {
        CreateOrUpdateHUDSurface(
            coinBlock,
            roundedSprite,
            "CoinPlasticSurface",
            new Color32(20, 24, 31, 105),
            10f
        );
    }


    //=========================================================
    // DISTANCE
    //=========================================================

    Transform distanceBlock =
        FindTransform(
            "RightInfo/DistanceBlock"
        );

    if (distanceBlock != null)
    {
        CreateOrUpdateHUDSurface(
            distanceBlock,
            roundedSprite,
            "DistancePlasticSurface",
            new Color32(20, 24, 31, 85),
            10f
        );
    }


    //=========================================================
    // COIN GOLD ACCENT
    //=========================================================

    if (coinBlock != null)
    {
        CreateGoldAccent(
            coinBlock,
            "CoinGoldAccent",
            3f
        );
    }


    //=========================================================
    // SCORE GOLD ACCENT
    //=========================================================

    if (scoreBlock != null)
    {
        CreateGoldAccent(
            scoreBlock,
            "ScoreGoldAccent",
            3f
        );
    }


    //=========================================================
    // SAVE
    //=========================================================

    MarkSceneDirty();

    Debug.Log(
        "[HUD Grouping] G-02.4 HUD Grouping đã áp dụng."
    );
}


//=============================================================
// FIND TRANSFORM
//=============================================================

private static Transform FindTransform(
    string relativePath)
{
    GameObject canvas =
        GameObject.Find("GameplayCanvas");

    if (canvas == null)
        return null;

    return canvas.transform.Find(
        "SafeArea/" + relativePath
    );
}


//=============================================================
// CREATE HUD SURFACE
//=============================================================

private static void CreateOrUpdateHUDSurface(
    Transform target,
    Sprite roundedSprite,
    string objectName,
    Color surfaceColor,
    float padding)
{
    Transform existing =
        target.Find(objectName);

    GameObject surfaceObject;


    //=========================================================
    // CREATE
    //=========================================================

    if (existing == null)
    {
        surfaceObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image)
            );

        surfaceObject.transform.SetParent(
            target,
            false
        );
    }
    else
    {
        surfaceObject =
            existing.gameObject;
    }


    //=========================================================
    // RECT
    //=========================================================

    RectTransform rect =
        surfaceObject.GetComponent<RectTransform>();

    rect.anchorMin =
        Vector2.zero;

    rect.anchorMax =
        Vector2.one;

    rect.pivot =
        new Vector2(
            0.5f,
            0.5f
        );

    rect.offsetMin =
        new Vector2(
            -padding,
            -padding
        );

    rect.offsetMax =
        new Vector2(
            padding,
            padding
        );


    //=========================================================
    // IMAGE
    //=========================================================

    Image image =
        surfaceObject.GetComponent<Image>();

    image.sprite =
        roundedSprite;

    image.type =
        Image.Type.Sliced;

    image.color =
        surfaceColor;

    image.raycastTarget =
        false;


    //=========================================================
    // SHADOW
    //=========================================================

    Shadow shadow =
        surfaceObject.GetComponent<Shadow>();

    if (shadow == null)
    {
        shadow =
            surfaceObject.AddComponent<Shadow>();
    }

    shadow.effectColor =
        new Color32(
            0,
            0,
            0,
            55
        );

    shadow.effectDistance =
        new Vector2(
            0f,
            -2f
        );

    shadow.useGraphicAlpha =
        true;


    //=========================================================
    // LAYER
    //=========================================================

    surfaceObject.transform.SetAsFirstSibling();


    EditorUtility.SetDirty(
        surfaceObject
    );

    EditorUtility.SetDirty(
        rect
    );

    EditorUtility.SetDirty(
        image
    );

    EditorUtility.SetDirty(
        shadow
    );
}


//=============================================================
// GOLD ACCENT
//=============================================================

private static void CreateGoldAccent(
    Transform target,
    string objectName,
    float height)
{
    Transform existing =
        target.Find(objectName);

    GameObject accentObject;


    //=========================================================
    // CREATE
    //=========================================================

    if (existing == null)
    {
        accentObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image)
            );

        accentObject.transform.SetParent(
            target,
            false
        );
    }
    else
    {
        accentObject =
            existing.gameObject;
    }


    //=========================================================
    // RECT
    //=========================================================

    RectTransform rect =
        accentObject.GetComponent<RectTransform>();

    rect.anchorMin =
        new Vector2(
            0f,
            0f
        );

    rect.anchorMax =
        new Vector2(
            0f,
            0f
        );

    rect.pivot =
        new Vector2(
            0f,
            0f
        );

    rect.anchoredPosition =
        new Vector2(
            0f,
            -3f
        );

    rect.sizeDelta =
        new Vector2(
            34f,
            height
        );


    //=========================================================
    // IMAGE
    //=========================================================

    Image image =
        accentObject.GetComponent<Image>();

    image.color =
        new Color32(
            255,
            203,
            45,
            220
        );

    image.raycastTarget =
        false;


    //=========================================================
    // ORDER
    //=========================================================

    accentObject.transform.SetAsLastSibling();


    EditorUtility.SetDirty(
        accentObject
    );

    EditorUtility.SetDirty(
        rect
    );

    EditorUtility.SetDirty(
        image
    );
}

    //=============================================================
    // FIND COMPONENT
    //=============================================================

    private static T Find<T>(
        string relativePath)
        where T : Component
    {
        GameObject canvas =
            GameObject.Find(
                "GameplayCanvas"
            );

        if (canvas == null)
            return null;


        Transform t =
            canvas.transform.Find(
                "SafeArea/" + relativePath
            );


        if (t == null)
        {
            Debug.LogWarning(
                "[HUD Setup] Không tìm thấy: " +
                "GameplayCanvas/SafeArea/" +
                relativePath
            );

            return null;
        }


        return t.GetComponent<T>();
    }


    //=============================================================
    // TEXT STYLE
    //=============================================================

    private static void StyleText(
        TMP_Text text,
        float size,
        Color color,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        text.fontSize =
            size;

        text.color =
            color;

        text.fontStyle =
            style;

        text.alignment =
            alignment;

        text.enableAutoSizing =
            false;

        text.extraPadding =
            true;

        EditorUtility.SetDirty(
            text
        );
    }


    //=============================================================
    // SIZE
    //=============================================================

    private static void SetSize(
        RectTransform rt,
        float width,
        float height)
    {
        rt.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            width
        );

        rt.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            height
        );

        EditorUtility.SetDirty(
            rt
        );
    }


    //=============================================================
    // SAVE SCENE
    //=============================================================

    private static void MarkSceneDirty()
    {
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );
    }
}