using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterProfileUI : MonoBehaviour
{
    public static CharacterProfileUI Instance { get; private set; }

    [Header("Main References")]
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private Button closeButton;

    [Header("Character")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text levelLabel;

    [Header("EXP")]
    [SerializeField] private TMP_Text expValueText;
    [SerializeField] private Image expFill;

    [Header("Currency")]
    [SerializeField] private TMP_Text coinValueText;
    [SerializeField] private TMP_Text bestValueText;

    [Header("Stats")]
    [SerializeField] private TMP_Text runsValueText;
    [SerializeField] private TMP_Text distanceValueText;

    [Header("Mini Profile - Main Menu")]
    [SerializeField] private Button avatarButton;
    [SerializeField] private TMP_Text miniPlayerNameText;
    [SerializeField] private TMP_Text miniLevelText;
    [SerializeField] private TMP_Text miniExpText;
    [SerializeField] private Image miniExpFill;
    [SerializeField] private TMP_Text miniCoinText;

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

    private PlayerProgression playerProgression;
    private GameManager gameManager;

    // ============================================================
    // FIRESTORE EVENT SOURCE
    // ============================================================

    private FirestorePlayerDataManager firestoreManager;

    private Coroutine refreshCoroutine;

    private int lastLevel = -1;
    private int lastCurrentExp = -1;
    private int lastRequiredExp = -1;
    private int lastTotalCoins = -1;
    private int lastBestScore = -1;

    private string lastDisplayName = string.Empty;

    private float refreshTimer;

    private const float RefreshInterval = 0.1f;

    // ============================================================
    // AWAKE
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

        if (characterPanel != null)
            characterPanel.SetActive(false);
    }

    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        FindReferences();
        FindDataSystems();
        BindFirestoreEvents();

        RefreshMiniProfileImmediate();
        RefreshProfile(true);

        if (characterPanel != null)
            characterPanel.SetActive(false);

        StartRefreshCoroutine();
    }

    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        refreshTimer -= Time.unscaledDeltaTime;

        if (refreshTimer > 0f)
            return;

        refreshTimer = RefreshInterval;

        FindDataSystems();
        BindFirestoreEvents();

        if (!MiniReferencesReady())
        {
            FindMiniProfileReferences();
        }

        RefreshProfileIfDataChanged();
    }

    // ============================================================
    // ENABLE
    // ============================================================

    private void OnEnable()
    {
        FindReferences();
        FindDataSystems();
        BindFirestoreEvents();

        RefreshMiniProfileImmediate();
        RefreshProfile(true);

        StartRefreshCoroutine();
    }

    // ============================================================
    // DISABLE
    // ============================================================

    private void OnDisable()
    {
        UnbindFirestoreEvents();

        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }
    }

    // ============================================================
    // DESTROY
    // ============================================================

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

        // Đã bind đúng manager rồi.
        if (firestoreManager == manager)
            return;

        // Nếu trước đó đang bind manager cũ thì tháo ra trước.
        UnbindFirestoreEvents();

        firestoreManager = manager;

        firestoreManager.OnPlayerDataLoaded += HandlePlayerDataLoaded;
        firestoreManager.OnPlayerDataSaved += HandlePlayerDataSaved;
        firestoreManager.OnPlayerDataCleared += HandlePlayerDataCleared;
    }

    private void UnbindFirestoreEvents()
    {
        if (firestoreManager == null)
            return;

        firestoreManager.OnPlayerDataLoaded -= HandlePlayerDataLoaded;
        firestoreManager.OnPlayerDataSaved -= HandlePlayerDataSaved;
        firestoreManager.OnPlayerDataCleared -= HandlePlayerDataCleared;

        firestoreManager = null;
    }

    // ============================================================
    // FIRESTORE - PLAYER DATA LOADED
    // ============================================================

    private void HandlePlayerDataLoaded(PlayerData data)
    {
        // Firebase vừa load tài khoản.
        // Refresh ngay lập tức, không cần bấm Avatar.
        FindReferences();
        FindDataSystems();

        RefreshMiniProfileImmediate();
        RefreshProfile(true);
    }

    // ============================================================
    // FIRESTORE - PLAYER DATA SAVED
    // ============================================================

    private void HandlePlayerDataSaved(PlayerData data)
    {
        // Coin / EXP / Level / BestScore vừa được ghi thành công.
        // Đồng bộ Mini Profile ngay.
        FindReferences();
        FindDataSystems();

        RefreshMiniProfileImmediate();
        RefreshProfile(true);
    }

    // ============================================================
    // FIRESTORE - PLAYER DATA CLEARED
    // ============================================================

    private void HandlePlayerDataCleared()
    {
        // Logout / đổi tài khoản.
        // Xóa cache cũ để tài khoản mới không kế thừa UI cũ.
        lastLevel = -1;
        lastCurrentExp = -1;
        lastRequiredExp = -1;
        lastTotalCoins = -1;
        lastBestScore = -1;
        lastDisplayName = string.Empty;

        RefreshMiniProfileImmediate();
        RefreshProfile(true);
    }

    // ============================================================
    // START REFRESH COROUTINE
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

    // ============================================================
    // DATA CHANGED
    // ============================================================

    private void RefreshProfileIfDataChanged()
    {
        int level;
        int currentExp;
        int requiredExp;
        int totalCoins;
        int bestScore;
        string displayName;

        GetProfileData(
            out level,
            out currentExp,
            out requiredExp,
            out totalCoins,
            out bestScore,
            out displayName
        );

        bool changed =
            level != lastLevel ||
            currentExp != lastCurrentExp ||
            requiredExp != lastRequiredExp ||
            totalCoins != lastTotalCoins ||
            bestScore != lastBestScore ||
            displayName != lastDisplayName;

        if (!changed)
            return;

        RefreshProfile(true);
    }

    // ============================================================
    // REFRESH AFTER INITIALIZATION
    // ============================================================

    private IEnumerator RefreshProfileAfterInitialization()
    {
        const int maxRetry = 20;

        for (int i = 0; i < maxRetry; i++)
        {
            yield return null;

            FindReferences();
            FindDataSystems();
            BindFirestoreEvents();

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

        if (characterPanel == null)
        {
            Transform panelTransform =
                FindChildRecursive(
                    searchRoot,
                    PanelName
                );

            if (panelTransform != null)
                characterPanel =
                    panelTransform.gameObject;
        }

        if (closeButton == null)
        {
            Transform buttonTransform =
                FindChildRecursive(
                    searchRoot,
                    CloseButtonName
                );

            if (buttonTransform != null)
                closeButton =
                    buttonTransform.GetComponent<Button>();
        }

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

        if (expFill == null)
        {
            Transform fillTransform =
                FindChildRecursive(
                    searchRoot,
                    ExpFillName
                );

            if (fillTransform != null)
            {
                expFill =
                    fillTransform.GetComponent<Image>();
            }
        }

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

        if (closeButton == null && characterPanel != null)
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

        FindMiniProfileReferences();
    }

    // ============================================================
    // MINI PROFILE
    // ============================================================

    private void FindMiniProfileReferences()
    {
        if (MiniReferencesReady())
            return;

        Transform miniRoot = null;

        miniRoot =
            FindChildRecursive(
                transform,
                MiniPanelName
            );

        if (miniRoot == null)
        {
            Transform[] allTransforms =
                FindObjectsByType<Transform>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            for (int i = 0; i < allTransforms.Length; i++)
            {
                Transform t = allTransforms[i];

                if (t != null &&
                    t.name == MiniPanelName)
                {
                    miniRoot = t;
                    break;
                }
            }
        }

        if (miniRoot == null)
            return;

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

        if (miniExpFill == null)
        {
            Transform fillTransform =
                FindChildRecursive(
                    miniRoot,
                    MiniExpFillName
                );

            if (fillTransform != null)
            {
                miniExpFill =
                    fillTransform.GetComponent<Image>();
            }
        }

        miniCoinText =
            FindTMP(
                miniCoinText,
                MiniCoinName,
                miniRoot
            );

        if (avatarButton != null)
        {
            avatarButton.onClick.RemoveListener(Open);
            avatarButton.onClick.AddListener(Open);
        }
    }

    // ============================================================
    // MINI REFERENCES READY
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
    // MINI PROFILE IMMEDIATE REFRESH
    // ============================================================

    private void RefreshMiniProfileImmediate()
    {
        FindDataSystems();

        if (!MiniReferencesReady())
        {
            FindMiniProfileReferences();
        }

        if (!MiniReferencesReady())
            return;

        int level;
        int currentExp;
        int requiredExp;
        int totalCoins;
        int bestScore;
        string displayName;

        GetProfileData(
            out level,
            out currentExp,
            out requiredExp,
            out totalCoins,
            out bestScore,
            out displayName
        );

        float progress = 0f;

        if (requiredExp > 0)
        {
            progress =
                (float)currentExp /
                requiredExp;
        }

        progress =
            Mathf.Clamp01(progress);

        if (miniPlayerNameText != null)
        {
            miniPlayerNameText.text =
                displayName;
        }

        if (miniLevelText != null)
        {
            miniLevelText.text =
                $"LV.{level}";
        }

        if (miniExpText != null)
        {
            miniExpText.text =
                $"{currentExp} / {requiredExp} EXP";
        }

        if (miniExpFill != null)
        {
            miniExpFill.fillAmount =
                progress;
        }

        if (miniCoinText != null)
        {
            miniCoinText.text =
                totalCoins.ToString("N0");
        }
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

        return target.GetComponent<TMP_Text>();
    }

    // ============================================================
    // FIND DATA SYSTEMS
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
        return
            FirestorePlayerDataManager.Instance != null &&
            FirestorePlayerDataManager.Instance.HasPlayerData &&
            FirestorePlayerDataManager.Instance.CurrentPlayerData != null;
    }

    private PlayerData GetFirestorePlayerData()
    {
        if (!HasFirestorePlayerData())
            return null;

        return
            FirestorePlayerDataManager.Instance
                .CurrentPlayerData;
    }

    // ============================================================
    // GET PROFILE DATA
    // ============================================================

    private void GetProfileData(
        out int level,
        out int currentExp,
        out int requiredExp,
        out int totalCoins,
        out int bestScore,
        out string displayName
    )
    {
        level = 1;
        currentExp = 0;
        requiredExp = 100;
        totalCoins = 0;
        bestScore = 0;
        displayName = "NINJA LEAD";

        PlayerData cloudData =
            GetFirestorePlayerData();

        if (cloudData != null)
        {
            level =
                Mathf.Max(
                    1,
                    cloudData.level
                );

            currentExp =
                Mathf.Max(
                    0,
                    cloudData.exp
                );

            requiredExp =
                GetRequiredExpForLevel(
                    level
                );

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

            if (!string.IsNullOrWhiteSpace(
                    cloudData.displayName))
            {
                displayName =
                    cloudData.displayName;
            }

            return;
        }

        if (playerProgression != null)
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
        }

        totalCoins =
            GetLocalTotalCoins();

        bestScore =
            GetLocalBestScore();
    }

    // ============================================================
    // EXP REQUIRED
    // ============================================================

    private int GetRequiredExpForLevel(int level)
    {
        if (level <= 1)
            return 100;

        return 100;
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
        {
            Debug.LogError(
                "[CharacterProfileUI] " +
                "Không tìm thấy CharacterPanel."
            );

            return;
        }

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
        {
            Debug.LogError(
                "[CharacterProfileUI] " +
                "Không tìm thấy CharacterPanel."
            );

            return;
        }

        if (characterPanel.activeSelf)
            Close();
        else
            Open();
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

        int level;
        int currentExp;
        int requiredExp;
        int totalCoins;
        int bestScore;
        string displayName;

        GetProfileData(
            out level,
            out currentExp,
            out requiredExp,
            out totalCoins,
            out bestScore,
            out displayName
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
        // EXP
        // ========================================================

        float progress = 0f;

        if (requiredExp > 0)
        {
            progress =
                (float)currentExp /
                requiredExp;
        }

        progress =
            Mathf.Clamp01(progress);

        if (expValueText != null)
        {
            expValueText.text =
                $"{currentExp} / {requiredExp} EXP";
        }

        if (expFill != null)
        {
            expFill.fillAmount =
                progress;
        }

        if (miniExpText != null)
        {
            miniExpText.text =
                $"{currentExp} / {requiredExp} EXP";
        }

        if (miniExpFill != null)
        {
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
            runsValueText.text = "—";
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

        lastDisplayName =
            displayName;
    }

    // ============================================================
    // LOCAL COINS
    // ============================================================

    private int GetLocalTotalCoins()
    {
        if (gameManager != null)
        {
            return gameManager.GetTotalCoins();
        }

        return PlayerPrefs.GetInt(
            "TotalCoins",
            0
        );
    }

    // ============================================================
    // LOCAL BEST SCORE
    // ============================================================

    private int GetLocalBestScore()
    {
        if (gameManager != null)
        {
            return gameManager.GetHighScoreInt();
        }

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
            return
                "Tên tuổi của bạn đã trở thành huyền thoại trên mọi cung đường.";

        if (level >= 20)
            return
                "Không còn gì có thể cản nổi Ninja Lead trên xa lộ.";

        if (level >= 15)
            return
                "Một quái xế thực thụ. Giao thông chỉ còn là chuyện nhỏ.";

        if (level >= 10)
            return
                "Kỹ năng đã đủ để biến những cung đường hỗn loạn thành sân chơi.";

        if (level >= 5)
            return
                "Tay lái ngày càng cứng. Nhưng đường phố vẫn còn rất nhiều thử thách.";

        return
            "Một Ninja Lead mới vào nghề. Hãy sống sót và tăng tốc!";
    }

    // ============================================================
    // STATE
    // ============================================================

    public bool IsOpen()
    {
        return characterPanel != null &&
               characterPanel.activeSelf;
    }

    // ============================================================
    // DEBUG
    // ============================================================

    [ContextMenu("DEBUG References")]
    private void DebugReferences()
    {
        FindReferences();
        FindDataSystems();
        BindFirestoreEvents();

        PlayerData cloudData =
            GetFirestorePlayerData();

        int level;
        int currentExp;
        int requiredExp;
        int totalCoins;
        int bestScore;
        string displayName;

        GetProfileData(
            out level,
            out currentExp,
            out requiredExp,
            out totalCoins,
            out bestScore,
            out displayName
        );

        Debug.Log(
            "[CharacterProfileUI]\n" +

            $"CharacterProfileUI Object Active: " +
            $"{gameObject.activeInHierarchy}\n" +

            $"PlayerProgression: " +
            $"{(playerProgression != null ? "FOUND" : "NULL")}\n" +

            $"GameManager: " +
            $"{(gameManager != null ? "FOUND" : "NULL")}\n" +

            $"Firestore Manager: " +
            $"{(FirestorePlayerDataManager.Instance != null ? "FOUND" : "NULL")}\n" +

            $"Firestore Events Bound: " +
            $"{(firestoreManager != null ? "YES" : "NO")}\n" +

            $"Firestore PlayerData: " +
            $"{(cloudData != null ? "FOUND" : "NULL")}\n" +

            $"Display Name: " +
            $"{displayName}\n" +

            $"MiniPlayerName: " +
            $"{(miniPlayerNameText != null ? "FOUND" : "NULL")}\n" +

            $"MiniLevel: " +
            $"{(miniLevelText != null ? "FOUND" : "NULL")}\n" +

            $"MiniExp: " +
            $"{(miniExpText != null ? "FOUND" : "NULL")}\n" +

            $"MiniExpFill: " +
            $"{(miniExpFill != null ? "FOUND" : "NULL")}\n" +

            $"MiniCoin: " +
            $"{(miniCoinText != null ? "FOUND" : "NULL")}\n" +

            $"Level: " +
            $"{level}\n" +

            $"Current EXP: " +
            $"{currentExp}\n" +

            $"Required EXP: " +
            $"{requiredExp}\n" +

            $"Total Coins: " +
            $"{totalCoins}\n" +

            $"Best Score: " +
            $"{bestScore}"
        );
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

        for (int i = 0; i < parent.childCount; i++)
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