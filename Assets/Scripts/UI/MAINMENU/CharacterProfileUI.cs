using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterProfileUI : MonoBehaviour
{
    public static CharacterProfileUI Instance { get; private set; }

    // ============================================================
    // MAIN REFERENCES
    // ============================================================

    [Header("Main References")]
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private Button closeButton;

    // ============================================================
    // CHARACTER
    // ============================================================

    [Header("Character")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text levelLabel;

    // ============================================================
    // EXP
    // ============================================================

    [Header("EXP")]
    [SerializeField] private TMP_Text expValueText;

    [Tooltip(
        "Image Fill của thanh EXP chính. " +
        "Nếu để trống, hệ thống tự tìm object 'Fill' bên trong CharacterPanel."
    )]
    [SerializeField] private Image expFill;

    // ============================================================
    // CURRENCY
    // ============================================================

    [Header("Currency")]
    [SerializeField] private TMP_Text coinValueText;
    [SerializeField] private TMP_Text bestValueText;

    // ============================================================
    // STATS
    // ============================================================

    [Header("Stats")]
    [SerializeField] private TMP_Text runsValueText;
    [SerializeField] private TMP_Text distanceValueText;

    // ============================================================
    // MINI PROFILE
    // ============================================================

    [Header("Mini Profile - Main Menu")]
    [SerializeField] private Button avatarButton;
    [SerializeField] private TMP_Text miniPlayerNameText;
    [SerializeField] private TMP_Text miniLevelText;
    [SerializeField] private TMP_Text miniExpText;

    [Tooltip(
        "Image Fill của thanh EXP mini. " +
        "Nếu để trống, hệ thống tự tìm object 'MiniExpFill' bên trong CharacterMiniPanel."
    )]
    [SerializeField] private Image miniExpFill;

    [SerializeField] private TMP_Text miniCoinText;

    // ============================================================
    // HIERARCHY NAMES
    // ============================================================

    private const string PanelName = "CharacterPanel";
    private const string CloseButtonName = "CloseButton";

    private const string PlayerNameName = "PlayerName";
    private const string RankTextName = "RankText";
    private const string DescriptionName = "Description";
    private const string LevelLabelName = "LevelLabel";

    private const string ExpValueName = "ExpValue";
    private const string ExpFillName = "Fill";

    private const string CoinValueName = "CoinValue";
    private const string BestValueName = "BestValue";

    private const string RunsValueName = "RunsValue";
    private const string DistanceValueName = "DistanceValue";

    private const string MiniPanelName = "CharacterMiniPanel";
    private const string AvatarButtonName = "AvatarButton";
    private const string MiniPlayerNameName = "MiniPlayerName";
    private const string MiniLevelName = "MiniLevel";
    private const string MiniExpName = "MiniExp";
    private const string MiniExpFillName = "MiniExpFill";
    private const string MiniCoinName = "MiniCoin";

    // ============================================================
    // DATA SYSTEMS
    // ============================================================

    private PlayerProgression playerProgression;
    private GameManager gameManager;
    private FirestorePlayerDataManager firestoreManager;

    private Coroutine refreshCoroutine;

    // ============================================================
    // CACHE
    // ============================================================

    private int lastLevel = -1;
    private int lastCurrentExp = -1;
    private int lastRequiredExp = -1;
    private int lastTotalCoins = -1;
    private int lastBestScore = -1;
    private int lastTotalRuns = -1;

    private string lastDisplayName = string.Empty;

    // ============================================================
    // REFRESH
    // ============================================================

    private float refreshTimer;

    private const float RefreshInterval = 0.1f;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        FindReferences();
        FindDataSystems();
        BindFirestoreEvents();
        SetupButtons();
        ConfigureExpImages();

        if (characterPanel != null)
            characterPanel.SetActive(false);
    }

    private void Start()
    {
        FindReferences();
        FindDataSystems();
        BindFirestoreEvents();
        SetupButtons();
        ConfigureExpImages();

        RefreshMiniProfileImmediate();
        RefreshProfile(true);

        if (characterPanel != null)
            characterPanel.SetActive(false);

        StartRefreshCoroutine();
    }

    private void Update()
    {
        refreshTimer -= Time.unscaledDeltaTime;

        if (refreshTimer > 0f)
            return;

        refreshTimer = RefreshInterval;

        FindDataSystems();
        BindFirestoreEvents();

        if (!MiniReferencesReady())
            FindMiniProfileReferences();

        RefreshProfileIfDataChanged();
    }

    private void OnEnable()
    {
        FindReferences();
        FindDataSystems();
        BindFirestoreEvents();
        SetupButtons();
        ConfigureExpImages();

        RefreshMiniProfileImmediate();
        RefreshProfile(true);

        StartRefreshCoroutine();
    }

    private void OnDisable()
    {
        UnbindFirestoreEvents();

        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        UnbindFirestoreEvents();

        if (Instance == this)
            Instance = null;
    }

    // ============================================================
    // FIRESTORE EVENTS
    // ============================================================

    private void BindFirestoreEvents()
    {
        FirestorePlayerDataManager manager =
            FirestorePlayerDataManager.Instance;

        if (manager == null)
            return;

        if (firestoreManager == manager)
            return;

        UnbindFirestoreEvents();

        firestoreManager = manager;

        firestoreManager.OnPlayerDataLoaded +=
            HandlePlayerDataLoaded;

        firestoreManager.OnPlayerDataSaved +=
            HandlePlayerDataSaved;

        firestoreManager.OnPlayerDataCleared +=
            HandlePlayerDataCleared;
    }

    private void UnbindFirestoreEvents()
    {
        if (firestoreManager == null)
            return;

        firestoreManager.OnPlayerDataLoaded -=
            HandlePlayerDataLoaded;

        firestoreManager.OnPlayerDataSaved -=
            HandlePlayerDataSaved;

        firestoreManager.OnPlayerDataCleared -=
            HandlePlayerDataCleared;

        firestoreManager = null;
    }

    private void HandlePlayerDataLoaded(PlayerData data)
    {
        ResetCache();

        FindReferences();
        FindDataSystems();
        ConfigureExpImages();

        RefreshMiniProfileImmediate();
        RefreshProfile(true);
    }

    private void HandlePlayerDataSaved(PlayerData data)
    {
        ResetCache();

        FindReferences();
        FindDataSystems();
        ConfigureExpImages();

        RefreshMiniProfileImmediate();
        RefreshProfile(true);
    }

    private void HandlePlayerDataCleared()
    {
        ResetCache();

        RefreshMiniProfileImmediate();
        RefreshProfile(true);
    }

    // ============================================================
    // CACHE
    // ============================================================

    private void ResetCache()
    {
        lastLevel = -1;
        lastCurrentExp = -1;
        lastRequiredExp = -1;
        lastTotalCoins = -1;
        lastBestScore = -1;
        lastTotalRuns = -1;

        lastDisplayName = string.Empty;
    }

    // ============================================================
    // REFRESH COROUTINE
    // ============================================================

    private void StartRefreshCoroutine()
    {
        if (!isActiveAndEnabled)
            return;

        if (refreshCoroutine != null)
            return;

        refreshCoroutine =
            StartCoroutine(
                RefreshProfileAfterInitialization()
            );
    }

    private IEnumerator RefreshProfileAfterInitialization()
    {
        const int maxRetry = 20;

        for (int i = 0; i < maxRetry; i++)
        {
            yield return null;

            FindReferences();
            FindDataSystems();
            BindFirestoreEvents();
            ConfigureExpImages();

            RefreshMiniProfileImmediate();
            RefreshProfile(true);

            bool dataReady =
                playerProgression != null ||
                HasFirestorePlayerData();

            bool miniUIReady =
                MiniReferencesReady();

            if (dataReady && miniUIReady)
                break;
        }

        RefreshMiniProfileImmediate();

        refreshCoroutine = null;
    }

    // ============================================================
    // FIND REFERENCES
    // ============================================================

    private void FindReferences()
    {
        Transform searchRoot = transform;

        // --------------------------------------------------------
        // CHARACTER PANEL
        // --------------------------------------------------------

        if (characterPanel == null)
        {
            Transform panelTransform =
                FindChildRecursive(
                    searchRoot,
                    PanelName
                );

            if (panelTransform != null)
            {
                characterPanel =
                    panelTransform.gameObject;
            }
        }

        // --------------------------------------------------------
        // CLOSE BUTTON
        // --------------------------------------------------------

        if (closeButton == null)
        {
            Transform buttonTransform =
                FindChildRecursive(
                    searchRoot,
                    CloseButtonName
                );

            if (buttonTransform != null)
            {
                closeButton =
                    buttonTransform.GetComponent<Button>();
            }
        }

        // --------------------------------------------------------
        // CHARACTER TEXT
        // --------------------------------------------------------

        playerNameText =
            FindTMP(
                playerNameText,
                PlayerNameName,
                searchRoot
            );

        rankText =
            FindTMP(
                rankText,
                RankTextName,
                searchRoot
            );

        descriptionText =
            FindTMP(
                descriptionText,
                DescriptionName,
                searchRoot
            );

        levelLabel =
            FindTMP(
                levelLabel,
                LevelLabelName,
                searchRoot
            );

        expValueText =
            FindTMP(
                expValueText,
                ExpValueName,
                searchRoot
            );

        // ========================================================
        // MAIN EXP FILL
        // ========================================================

        ResolveMainExpFill();

        // --------------------------------------------------------
        // CURRENCY
        // --------------------------------------------------------

        coinValueText =
            FindTMP(
                coinValueText,
                CoinValueName,
                searchRoot
            );

        bestValueText =
            FindTMP(
                bestValueText,
                BestValueName,
                searchRoot
            );

        // --------------------------------------------------------
        // STATS
        // --------------------------------------------------------

        runsValueText =
            FindTMP(
                runsValueText,
                RunsValueName,
                searchRoot
            );

        distanceValueText =
            FindTMP(
                distanceValueText,
                DistanceValueName,
                searchRoot
            );

        // --------------------------------------------------------
        // CLOSE BUTTON FALLBACK
        // --------------------------------------------------------

        if (
            closeButton == null &&
            characterPanel != null
        )
        {
            Transform buttonTransform =
                FindChildRecursive(
                    characterPanel.transform,
                    CloseButtonName
                );

            if (buttonTransform != null)
            {
                closeButton =
                    buttonTransform.GetComponent<Button>();
            }
        }

        // --------------------------------------------------------
        // MINI PROFILE
        // --------------------------------------------------------

        FindMiniProfileReferences();

        ConfigureExpImages();
    }

    // ============================================================
    // RESOLVE MAIN EXP FILL
    // ============================================================

    private void ResolveMainExpFill()
    {
        if (characterPanel == null)
            return;

        Transform fillTransform =
            FindChildRecursive(
                characterPanel.transform,
                ExpFillName
            );

        if (fillTransform == null)
        {
            Debug.LogWarning(
                "[CharacterProfileUI] " +
                "Không tìm thấy object '" +
                ExpFillName +
                "' bên trong CharacterPanel."
            );

            return;
        }

        Image foundImage =
            fillTransform.GetComponent<Image>();

        if (foundImage == null)
        {
            Debug.LogWarning(
                "[CharacterProfileUI] " +
                "Object '" +
                ExpFillName +
                "' không có Image component."
            );

            return;
        }

        // --------------------------------------------------------
        // QUAN TRỌNG:
        // Luôn ưu tiên object Fill thực sự nằm trong CharacterPanel.
        // Điều này tránh serialized reference cũ trỏ nhầm Image.
        // --------------------------------------------------------

        if (expFill != foundImage)
        {
            expFill = foundImage;

            Debug.Log(
                "[CharacterProfileUI] " +
                "Main EXP Fill đã bind: " +
                GetHierarchyPath(expFill.transform)
            );
        }
    }

    // ============================================================
    // MINI PROFILE REFERENCES
    // ============================================================

    private void FindMiniProfileReferences()
    {
        Transform miniRoot =
            FindChildRecursive(
                transform,
                MiniPanelName
            );

        if (miniRoot == null)
            return;

        // --------------------------------------------------------
        // AVATAR
        // --------------------------------------------------------

        if (avatarButton == null)
        {
            Transform avatarTransform =
                FindChildRecursive(
                    miniRoot,
                    AvatarButtonName
                );

            if (avatarTransform != null)
            {
                avatarButton =
                    avatarTransform.GetComponent<Button>();
            }
        }

        // --------------------------------------------------------
        // MINI TEXT
        // --------------------------------------------------------

        miniPlayerNameText =
            FindTMP(
                miniPlayerNameText,
                MiniPlayerNameName,
                miniRoot
            );

        miniLevelText =
            FindTMP(
                miniLevelText,
                MiniLevelName,
                miniRoot
            );

        miniExpText =
            FindTMP(
                miniExpText,
                MiniExpName,
                miniRoot
            );

        // ========================================================
        // MINI EXP FILL
        // ========================================================

        Transform fillTransform =
            FindChildRecursive(
                miniRoot,
                MiniExpFillName
            );

        if (fillTransform != null)
        {
            Image foundImage =
                fillTransform.GetComponent<Image>();

            if (foundImage != null &&
                miniExpFill != foundImage)
            {
                miniExpFill = foundImage;

                Debug.Log(
                    "[CharacterProfileUI] " +
                    "Mini EXP Fill đã bind: " +
                    GetHierarchyPath(
                        miniExpFill.transform
                    )
                );
            }
        }

        // --------------------------------------------------------
        // COIN
        // --------------------------------------------------------

        miniCoinText =
            FindTMP(
                miniCoinText,
                MiniCoinName,
                miniRoot
            );

        // --------------------------------------------------------
        // BUTTON
        // --------------------------------------------------------

        if (avatarButton != null)
        {
            avatarButton.onClick.RemoveListener(Open);
            avatarButton.onClick.AddListener(Open);
        }

        ConfigureExpImage(miniExpFill);
    }

    // ============================================================
    // MINI READY
    // ============================================================

    private bool MiniReferencesReady()
    {
        return
            miniPlayerNameText != null &&
            miniLevelText != null &&
            miniExpText != null &&
            miniExpFill != null &&
            miniCoinText != null;
    }

    // ============================================================
    // CONFIGURE EXP IMAGE
    // ============================================================

    private void ConfigureExpImages()
    {
        ConfigureExpImage(expFill);
        ConfigureExpImage(miniExpFill);
    }

    private void ConfigureExpImage(Image image)
    {
        if (image == null)
            return;

        image.type =
            Image.Type.Filled;

        image.fillMethod =
            Image.FillMethod.Horizontal;

        image.fillOrigin =
            (int)Image.OriginHorizontal.Left;

        image.fillClockwise = true;
    }

    // ============================================================
    // DATA SYSTEMS
    // ============================================================

    private void FindDataSystems()
    {
        if (playerProgression == null)
        {
            playerProgression =
                FindFirstObjectByType<PlayerProgression>();
        }

        if (gameManager == null)
        {
            gameManager =
                GameManager.Instance;

            if (gameManager == null)
            {
                gameManager =
                    FindFirstObjectByType<GameManager>();
            }
        }
    }

    // ============================================================
    // FIRESTORE DATA
    // ============================================================

    private bool HasFirestorePlayerData()
    {
        FirestorePlayerDataManager manager =
            FirestorePlayerDataManager.Instance;

        return
            manager != null &&
            manager.HasPlayerData &&
            manager.CurrentPlayerData != null;
    }

    private PlayerData GetFirestorePlayerData()
    {
        if (!HasFirestorePlayerData())
            return null;

        return
            FirestorePlayerDataManager
                .Instance
                .CurrentPlayerData;
    }

    // ============================================================
    // PROFILE DATA
    // ============================================================

    private void GetProfileData(
        out int level,
        out int currentExp,
        out int requiredExp,
        out int totalCoins,
        out int bestScore,
        out int totalRuns,
        out string displayName
    )
    {
        level = 1;
        currentExp = 0;
        requiredExp = 100;

        totalCoins = 0;
        bestScore = 0;
        totalRuns = 0;

        displayName =
            "NINJA LEAD";

        PlayerData cloudData =
            GetFirestorePlayerData();

        // ========================================================
        // CLOUD DATA
        // ========================================================

        if (cloudData != null)
        {
            totalCoins =
                Mathf.Max(
                    0,
                    cloudData.coins
                );

            bestScore =
                Mathf.Max(
                    0,
                    cloudData.bestScore
                );

            totalRuns =
                Mathf.Max(
                    0,
                    cloudData.totalRuns
                );

            if (!string.IsNullOrWhiteSpace(
                    cloudData.displayName))
            {
                displayName =
                    cloudData.displayName;
            }
        }
        else
        {
            totalCoins =
                GetLocalTotalCoins();

            bestScore =
                GetLocalBestScore();
        }

        // ========================================================
        // PLAYER PROGRESSION
        // SOURCE OF TRUTH
        // ========================================================

        if (
            playerProgression != null &&
            playerProgression.IsInitialized
        )
        {
            level =
                Mathf.Max(
                    1,
                    playerProgression.Level
                );

            currentExp =
                Mathf.Max(
                    0,
                    playerProgression.CurrentLevelExp
                );

            requiredExp =
                Mathf.Max(
                    1,
                    playerProgression.ExpRequiredForNextLevel
                );

            if (currentExp > requiredExp)
            {
                currentExp =
                    requiredExp;
            }

            return;
        }

        // ========================================================
        // FALLBACK FIRESTORE
        // ========================================================

        if (cloudData != null)
        {
            level =
                Mathf.Max(
                    1,
                    cloudData.level
                );

            int totalExp =
                Mathf.Max(
                    0,
                    cloudData.exp
                );

            CalculateCurrentLevelExpFallback(
                level,
                totalExp,
                out currentExp,
                out requiredExp
            );
        }
    }

    // ============================================================
    // FALLBACK EXP CALCULATION
    // ============================================================

    private void CalculateCurrentLevelExpFallback(
        int level,
        int totalExp,
        out int currentExp,
        out int requiredExp
    )
    {
        currentExp = 0;

        requiredExp =
            CalculateRequiredExpForLevel(
                level
            );

        long expBeforeCurrentLevel = 0;

        for (int i = 1;
             i < level;
             i++)
        {
            int requiredForLevel =
                CalculateRequiredExpForLevel(i);

            if (
                expBeforeCurrentLevel >
                long.MaxValue -
                requiredForLevel
            )
            {
                expBeforeCurrentLevel =
                    long.MaxValue;

                break;
            }

            expBeforeCurrentLevel +=
                requiredForLevel;
        }

        long current =
            (long)totalExp -
            expBeforeCurrentLevel;

        if (current <= 0)
        {
            currentExp = 0;
            return;
        }

        if (current >= requiredExp)
        {
            currentExp =
                requiredExp;

            return;
        }

        currentExp =
            (int)current;
    }

    // ============================================================
    // EXP FORMULA
    // ============================================================

    private int CalculateRequiredExpForLevel(
        int level
    )
    {
        const int baseExp = 100;
        const float growthMultiplier = 1.25f;

        if (level <= 1)
            return baseExp;

        float required =
            baseExp *
            Mathf.Pow(
                growthMultiplier,
                level - 1
            );

        if (
            float.IsNaN(required) ||
            float.IsInfinity(required)
        )
        {
            return int.MaxValue;
        }

        if (required >= int.MaxValue)
            return int.MaxValue;

        return Mathf.Max(
            1,
            Mathf.RoundToInt(required)
        );
    }

    // ============================================================
    // EXP PROGRESS
    // ============================================================

    private float CalculateExpProgress(
        int currentExp,
        int requiredExp
    )
    {
        if (requiredExp <= 0)
            return 0f;

        if (currentExp <= 0)
            return 0f;

        if (currentExp >= requiredExp)
            return 1f;

        float progress =
            (float)currentExp /
            (float)requiredExp;

        return Mathf.Clamp01(progress);
    }

    // ============================================================
    // MINI REFRESH
    // ============================================================

    private void RefreshMiniProfileImmediate()
    {
        FindDataSystems();

        if (!MiniReferencesReady())
            FindMiniProfileReferences();

        if (!MiniReferencesReady())
            return;

        int level;
        int currentExp;
        int requiredExp;
        int totalCoins;
        int bestScore;
        int totalRuns;
        string displayName;

        GetProfileData(
            out level,
            out currentExp,
            out requiredExp,
            out totalCoins,
            out bestScore,
            out totalRuns,
            out displayName
        );

        float progress =
            CalculateExpProgress(
                currentExp,
                requiredExp
            );

        miniPlayerNameText.text =
            displayName;

        miniLevelText.text =
            $"LV.{level}";

        miniExpText.text =
            $"{currentExp:N0} / {requiredExp:N0} EXP";

        ConfigureExpImage(miniExpFill);

        miniExpFill.fillAmount =
            progress;

        miniCoinText.text =
            totalCoins.ToString("N0");
    }

    // ============================================================
    // DATA CHANGE CHECK
    // ============================================================

    private void RefreshProfileIfDataChanged()
    {
        int level;
        int currentExp;
        int requiredExp;
        int totalCoins;
        int bestScore;
        int totalRuns;
        string displayName;

        GetProfileData(
            out level,
            out currentExp,
            out requiredExp,
            out totalCoins,
            out bestScore,
            out totalRuns,
            out displayName
        );

        bool changed =
            level != lastLevel ||
            currentExp != lastCurrentExp ||
            requiredExp != lastRequiredExp ||
            totalCoins != lastTotalCoins ||
            bestScore != lastBestScore ||
            totalRuns != lastTotalRuns ||
            displayName != lastDisplayName;

        if (!changed)
            return;

        RefreshProfile(true);
    }

    // ============================================================
    // REFRESH PROFILE
    // ============================================================

    public void RefreshProfile()
    {
        RefreshProfile(true);
    }

    private void RefreshProfile(bool force)
    {
        FindReferences();
        FindDataSystems();
        ConfigureExpImages();

        int level;
        int currentExp;
        int requiredExp;
        int totalCoins;
        int bestScore;
        int totalRuns;
        string displayName;

        GetProfileData(
            out level,
            out currentExp,
            out requiredExp,
            out totalCoins,
            out bestScore,
            out totalRuns,
            out displayName
        );

        float progress =
            CalculateExpProgress(
                currentExp,
                requiredExp
            );

        // ========================================================
        // CHARACTER
        // ========================================================

        if (playerNameText != null)
        {
            playerNameText.text =
                displayName;
        }

        if (miniPlayerNameText != null)
        {
            miniPlayerNameText.text =
                displayName;
        }

        // ========================================================
        // LEVEL
        // ========================================================

        if (levelLabel != null)
        {
            levelLabel.text =
                $"LV.{level}";
        }

        if (miniLevelText != null)
        {
            miniLevelText.text =
                $"LV.{level}";
        }

        // ========================================================
        // RANK
        // ========================================================

        if (rankText != null)
        {
            rankText.text =
                GetRankName(level);
        }

        if (descriptionText != null)
        {
            descriptionText.text =
                GetCharacterDescription(level);
        }

        // ========================================================
        // EXP TEXT
        // ========================================================

        if (expValueText != null)
        {
            expValueText.text =
                $"{currentExp:N0} / {requiredExp:N0} EXP";
        }

        if (miniExpText != null)
        {
            miniExpText.text =
                $"{currentExp:N0} / {requiredExp:N0} EXP";
        }

        // ========================================================
        // MAIN EXP BAR
        // ========================================================

        if (expFill != null)
        {
            ConfigureExpImage(expFill);

            expFill.fillAmount =
                progress;
        }

        // ========================================================
        // MINI EXP BAR
        // ========================================================

        if (miniExpFill != null)
        {
            ConfigureExpImage(miniExpFill);

            miniExpFill.fillAmount =
                progress;
        }

        // ========================================================
        // COINS
        // ========================================================

        if (coinValueText != null)
        {
            coinValueText.text =
                totalCoins.ToString("N0");
        }

        if (miniCoinText != null)
        {
            miniCoinText.text =
                totalCoins.ToString("N0");
        }

        // ========================================================
        // BEST SCORE
        // ========================================================

        if (bestValueText != null)
        {
            bestValueText.text =
                $"{bestScore:N0} m";
        }

        // ========================================================
        // DISTANCE
        // ========================================================

        if (distanceValueText != null)
        {
            distanceValueText.text =
                $"{bestScore:N0} m";
        }

        // ========================================================
        // RUNS
        // ========================================================

        if (runsValueText != null)
        {
            runsValueText.text =
                totalRuns.ToString("N0");
        }

        // ========================================================
        // CACHE
        // ========================================================

        lastLevel =
            level;

        lastCurrentExp =
            currentExp;

        lastRequiredExp =
            requiredExp;

        lastTotalCoins =
            totalCoins;

        lastBestScore =
            bestScore;

        lastTotalRuns =
            totalRuns;

        lastDisplayName =
            displayName;
    }

    // ============================================================
    // LOCAL COINS
    // ============================================================

    private int GetLocalTotalCoins()
    {
        if (gameManager != null)
            return gameManager.GetTotalCoins();

        return PlayerPrefs.GetInt(
            "TotalCoins",
            0
        );
    }

    // ============================================================
    // LOCAL BEST
    // ============================================================

    private int GetLocalBestScore()
    {
        if (gameManager != null)
            return gameManager.GetHighScoreInt();

        return Mathf.FloorToInt(
            PlayerPrefs.GetFloat(
                "HighScore",
                0f
            )
        );
    }

    // ============================================================
    // RANK
    // ============================================================

    private string GetRankName(int level)
    {
        if (level >= 30)
            return "HUYỀN THOẠI ĐƯỜNG PHỐ";

        if (level >= 20)
            return "BÁ VƯƠNG XA LỘ";

        if (level >= 15)
            return "QUÁI XẾ";

        if (level >= 10)
            return "TAY LÁI LÃO LUYỆN";

        if (level >= 5)
            return "TAY LÁI ĐƯỜNG PHỐ";

        return "TÂN BINH NINJA";
    }

    // ============================================================
    // DESCRIPTION
    // ============================================================

    private string GetCharacterDescription(int level)
    {
        if (level >= 30)
        {
            return
                "Tên tuổi của bạn đã trở thành huyền thoại trên mọi cung đường.";
        }

        if (level >= 20)
        {
            return
                "Không còn gì có thể cản nổi Ninja Lead trên xa lộ.";
        }

        if (level >= 15)
        {
            return
                "Một quái xế thực thụ. Giao thông chỉ còn là chuyện nhỏ.";
        }

        if (level >= 10)
        {
            return
                "Kỹ năng đã đủ để biến những cung đường hỗn loạn thành sân chơi.";
        }

        if (level >= 5)
        {
            return
                "Tay lái ngày càng cứng. Nhưng đường phố vẫn còn rất nhiều thử thách.";
        }

        return
            "Một Ninja Lead mới vào nghề. Hãy sống sót và tăng tốc!";
    }

    // ============================================================
    // BUTTONS
    // ============================================================

    private void SetupButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (avatarButton != null)
        {
            avatarButton.onClick.RemoveListener(Open);
            avatarButton.onClick.AddListener(Open);
        }
    }

    // ============================================================
    // OPEN
    // ============================================================

    public void Open()
    {
        FindReferences();
        FindDataSystems();
        BindFirestoreEvents();
        ConfigureExpImages();

        RefreshProfile(true);

        if (characterPanel == null)
        {
            Debug.LogError(
                "[CharacterProfileUI] " +
                "Không tìm thấy CharacterPanel."
            );

            return;
        }

        characterPanel.SetActive(true);
    }

    // ============================================================
    // CLOSE
    // ============================================================

    public void Close()
    {
        if (characterPanel == null)
            FindReferences();

        if (characterPanel == null)
            return;

        characterPanel.SetActive(false);
    }

    // ============================================================
    // TOGGLE
    // ============================================================

    public void Toggle()
    {
        if (characterPanel == null)
            FindReferences();

        if (characterPanel == null)
            return;

        if (characterPanel.activeSelf)
            Close();
        else
            Open();
    }

    // ============================================================
    // STATE
    // ============================================================

    public bool IsOpen()
    {
        return
            characterPanel != null &&
            characterPanel.activeSelf;
    }

    // ============================================================
    // DEBUG EXP BAR
    // ============================================================

    [ContextMenu("DEBUG EXP BAR")]
    private void DebugExpBar()
    {
        FindReferences();
        FindDataSystems();
        ConfigureExpImages();

        int level;
        int currentExp;
        int requiredExp;
        int totalCoins;
        int bestScore;
        int totalRuns;
        string displayName;

        GetProfileData(
            out level,
            out currentExp,
            out requiredExp,
            out totalCoins,
            out bestScore,
            out totalRuns,
            out displayName
        );

        float progress =
            CalculateExpProgress(
                currentExp,
                requiredExp
            );

        Debug.Log(
            "\n" +
            "========== CHARACTER EXP DEBUG ==========\n" +

            $"Level             : {level}\n" +
            $"Current EXP       : {currentExp}\n" +
            $"Required EXP      : {requiredExp}\n" +
            $"Progress          : {progress:F4}\n" +
            $"Percentage        : {(progress * 100f):F2}%\n" +

            "-----------------------------------------\n" +

            $"Main expFill      : " +
            $"{(expFill != null ? expFill.name : "NULL")}\n" +

            $"Main hierarchy    : " +
            $"{(expFill != null ? GetHierarchyPath(expFill.transform) : "NULL")}\n" +

            $"Main Image Type   : " +
            $"{(expFill != null ? expFill.type.ToString() : "NULL")}\n" +

            $"Main Fill Method  : " +
            $"{(expFill != null ? expFill.fillMethod.ToString() : "NULL")}\n" +

            $"Main Fill Origin  : " +
            $"{(expFill != null ? expFill.fillOrigin.ToString() : "NULL")}\n" +

            $"Main fillAmount   : " +
            $"{(expFill != null ? expFill.fillAmount.ToString("F4") : "NULL")}\n" +

            "-----------------------------------------\n" +

            $"Mini expFill      : " +
            $"{(miniExpFill != null ? miniExpFill.name : "NULL")}\n" +

            $"Mini hierarchy    : " +
            $"{(miniExpFill != null ? GetHierarchyPath(miniExpFill.transform) : "NULL")}\n" +

            $"Mini Image Type   : " +
            $"{(miniExpFill != null ? miniExpFill.type.ToString() : "NULL")}\n" +

            $"Mini Fill Method  : " +
            $"{(miniExpFill != null ? miniExpFill.fillMethod.ToString() : "NULL")}\n" +

            $"Mini Fill Origin  : " +
            $"{(miniExpFill != null ? miniExpFill.fillOrigin.ToString() : "NULL")}\n" +

            $"Mini fillAmount   : " +
            $"{(miniExpFill != null ? miniExpFill.fillAmount.ToString("F4") : "NULL")}\n" +

            "-----------------------------------------\n" +

            $"Total Runs        : {totalRuns}\n" +

            "=========================================\n"
        );
    }

    // ============================================================
    // DEBUG FORCE 0%
    // ============================================================

    [ContextMenu("DEBUG Set EXP Bar 0%")]
    private void DebugSetExpBarZero()
    {
        ForceExpBar(0f);

        Debug.Log(
            "[CharacterProfileUI] " +
            "Đã set EXP bar = 0%."
        );
    }

    // ============================================================
    // DEBUG FORCE 25%
    // ============================================================

    [ContextMenu("DEBUG Set EXP Bar 25%")]
    private void DebugSetExpBar25()
    {
        ForceExpBar(0.25f);

        Debug.Log(
            "[CharacterProfileUI] " +
            "Đã set EXP bar = 25%."
        );
    }

    // ============================================================
    // DEBUG FORCE 50%
    // ============================================================

    [ContextMenu("DEBUG Set EXP Bar 50%")]
    private void DebugSetExpBar50()
    {
        ForceExpBar(0.50f);

        Debug.Log(
            "[CharacterProfileUI] " +
            "Đã set EXP bar = 50%."
        );
    }

    // ============================================================
    // DEBUG FORCE 75%
    // ============================================================

    [ContextMenu("DEBUG Set EXP Bar 75%")]
    private void DebugSetExpBar75()
    {
        ForceExpBar(0.75f);

        Debug.Log(
            "[CharacterProfileUI] " +
            "Đã set EXP bar = 75%."
        );
    }

    // ============================================================
    // DEBUG FORCE 100%
    // ============================================================

    [ContextMenu("DEBUG Set EXP Bar 100%")]
    private void DebugSetExpBar100()
    {
        ForceExpBar(1f);

        Debug.Log(
            "[CharacterProfileUI] " +
            "Đã set EXP bar = 100%."
        );
    }

    // ============================================================
    // FORCE EXP BAR
    // ============================================================

    private void ForceExpBar(float value)
    {
        value =
            Mathf.Clamp01(value);

        FindReferences();
        ConfigureExpImages();

        if (expFill != null)
        {
            expFill.fillAmount =
                value;
        }

        if (miniExpFill != null)
        {
            miniExpFill.fillAmount =
                value;
        }

        Debug.Log(
            "[CharacterProfileUI] " +
            $"Force EXP Fill = {value * 100f:F1}%"
        );
    }

    // ============================================================
    // HIERARCHY DEBUG
    // ============================================================

    private string GetHierarchyPath(
        Transform target
    )
    {
        if (target == null)
            return "NULL";

        string path =
            target.name;

        Transform current =
            target.parent;

        int safety = 0;

        while (
            current != null &&
            safety < 100
        )
        {
            path =
                current.name +
                "/" +
                path;

            current =
                current.parent;

            safety++;
        }

        return path;
    }

    // ============================================================
    // FIND TMP
    // ============================================================

    private TMP_Text FindTMP(
        TMP_Text current,
        string targetName,
        Transform searchRoot
    )
    {
        if (current != null)
            return current;

        Transform target =
            FindChildRecursive(
                searchRoot,
                targetName
            );

        if (target == null)
            return null;

        return
            target.GetComponent<TMP_Text>();
    }

    // ============================================================
    // RECURSIVE FIND
    // ============================================================

    private Transform FindChildRecursive(
        Transform parent,
        string targetName
    )
    {
        if (parent == null)
            return null;

        if (parent.name == targetName)
            return parent;

        for (
            int i = 0;
            i < parent.childCount;
            i++
        )
        {
            Transform child =
                parent.GetChild(i);

            Transform result =
                FindChildRecursive(
                    child,
                    targetName
                );

            if (result != null)
                return result;
        }

        return null;
    }
}