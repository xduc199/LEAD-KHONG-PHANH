using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    public static MissionManager Instance { get; private set; }


    // =========================================================
    // SETTINGS
    // =========================================================

    private const int DailyMissionCount = 6;

    private const float SaveDelay = 0.75f;

    // Phải khớp tên scene Gameplay thực tế.
    private const string GameplaySceneName = "GameplayScene";


    // =========================================================
    // MISSION TYPES
    // =========================================================

    public enum MissionType
    {
        CollectCoins,
        TravelDistance,
        CompleteRuns,
        EarnExp,
        ReachLevel
    }


    // =========================================================
    // MISSION TEMPLATE
    // =========================================================

    [Serializable]
    private class MissionTemplate
    {
        public string id;
        public string title;
        public string description;

        public MissionType type;

        public int target;

        public int expReward;

        public int goldReward;


        public MissionTemplate(
            string id,
            string title,
            string description,
            MissionType type,
            int target,
            int expReward,
            int goldReward)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.type = type;
            this.target = target;
            this.expReward = expReward;
            this.goldReward = goldReward;
        }
    }


    // =========================================================
    // RUNTIME MISSION
    // =========================================================

    [Serializable]
    public class Mission
    {
        [Header("Identity")]
        public string id;

        public string title;

        [TextArea(1, 3)]
        public string description;


        [Header("Objective")]
        public MissionType type;

        public int target;


        [Header("Reward")]
        public int expReward;

        public int goldReward;


        [HideInInspector]
        public int progress;

        [HideInInspector]
        public bool claimed;


        public bool IsCompleted
        {
            get
            {
                return progress >= target;
            }
        }


        public int CurrentProgress
        {
            get
            {
                return Mathf.Clamp(
                    progress,
                    0,
                    target
                );
            }
        }
    }


    // =========================================================
    // RUNTIME DATA
    // =========================================================

    private readonly List<MissionTemplate>
        missionPool =
        new List<MissionTemplate>();


    private readonly List<Mission>
        missions =
        new List<Mission>();


    // =========================================================
    // DAILY COUNTERS
    // =========================================================

    private int dailyCoins;

    private int dailyDistance;

    private int dailyRuns;

    private int dailyExp;


    // =========================================================
    // CURRENT RUN TRACKING
    // =========================================================

    // Các biến này CHỈ có ý nghĩa trong GameplayScene.
    // Tuyệt đối không dùng chúng ở MainMenu.
    private int lastRunCoins;

    private int lastRunDistance;


    // =========================================================
    // GAMEPLAY STATE
    // =========================================================

    private bool isGameplaySceneActive;


    // =========================================================
    // SYSTEM REFERENCES
    // =========================================================

    private GameManager connectedGameManager;

    private PlayerProgression connectedPlayerProgression;


    // =========================================================
    // FIRESTORE
    // =========================================================

    private FirestorePlayerDataManager firestoreManager;

    private bool accountInitialized;

    private bool missionDataDirty;

    private bool saveInProgress;

    private bool saveAgainAfterCurrentSave;

    private Coroutine delayedSaveCoroutine;


    // Claim lock: prevents double-click / duplicate reward
    // while EXP + Gold are being persisted.
    private bool claimInProgress;

    private string claimMissionId;


    // =========================================================
    // EVENTS
    // =========================================================

    public Action<Mission>
        OnMissionProgressChanged;

    public Action<Mission>
        OnMissionCompleted;

    public Action<Mission>
        OnMissionClaimed;

    public Action
        OnDailyMissionsGenerated;

    /// <summary>
    /// Fired once when every daily mission is claimed.
    /// MissionManager does not know anything about Lucky Wheel.
    /// A separate bridge can subscribe to this event later.
    /// </summary>
    public Action
        OnAllDailyMissionsClaimed;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (
            Instance != null &&
            Instance != this
        )
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded +=
            HandleSceneLoaded;

        SceneManager.sceneUnloaded +=
            HandleSceneUnloaded;

        BuildMissionPool();

        // MissionManager khởi tạo theo scene hiện tại.
        isGameplaySceneActive =
            SceneManager.GetActiveScene().name ==
            GameplaySceneName;

        Debug.Log(
            $"[MissionManager] Mission Pool: " +
            $"{missionPool.Count} nhiệm vụ."
        );

        Debug.Log(
            $"[MissionManager] Initial Scene = " +
            $"{SceneManager.GetActiveScene().name} | " +
            $"GameplayActive = {isGameplaySceneActive}"
        );
    }


    private void Start()
    {
        StartCoroutine(
            InitializeManagerNextFrame()
        );
    }


    private void OnApplicationPause(
        bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveMissionDataImmediately();
        }
    }


    private void OnApplicationQuit()
    {
        SaveMissionDataImmediately();
    }


    private void OnDestroy()
    {
        if (delayedSaveCoroutine != null)
        {
            StopCoroutine(
                delayedSaveCoroutine
            );

            delayedSaveCoroutine = null;
        }

        SceneManager.sceneLoaded -=
            HandleSceneLoaded;

        SceneManager.sceneUnloaded -=
            HandleSceneUnloaded;

        UnsubscribeFromSystems();

        UnsubscribeFirestore();

        if (Instance == this)
        {
            Instance = null;
        }
    }


    // =========================================================
    // INITIALIZE
    // =========================================================

    private IEnumerator InitializeManagerNextFrame()
    {
        yield return null;

        TryBindFirestore();

        TryBindSystems();

        if (
            firestoreManager != null &&
            firestoreManager.HasPlayerData
        )
        {
            InitializeForCurrentAccount();
        }
    }


    // =========================================================
    // FIRESTORE BIND
    // =========================================================

    private void TryBindFirestore()
    {
        FirestorePlayerDataManager current =
            FirestorePlayerDataManager.Instance;


        if (current == firestoreManager)
        {
            return;
        }


        if (firestoreManager != null)
        {
            UnsubscribeFirestore();
        }


        firestoreManager =
            current;


        if (firestoreManager != null)
        {
            firestoreManager.OnPlayerDataLoaded +=
                HandlePlayerDataLoaded;

            firestoreManager.OnPlayerDataCleared +=
                HandlePlayerDataCleared;


            Debug.Log(
                "[MissionManager] Đã Bind FirestorePlayerDataManager."
            );
        }
    }


    private void UnsubscribeFirestore()
    {
        if (firestoreManager == null)
            return;


        firestoreManager.OnPlayerDataLoaded -=
            HandlePlayerDataLoaded;

        firestoreManager.OnPlayerDataCleared -=
            HandlePlayerDataCleared;
    }


    // =========================================================
    // PLAYER LOADED
    // =========================================================

    private void HandlePlayerDataLoaded(
        PlayerData data)
    {
        if (data == null)
        {
            ClearRuntimeData();
            return;
        }


        InitializeForCurrentAccount();
    }


    // =========================================================
    // PLAYER CLEARED
    // =========================================================

    private void HandlePlayerDataCleared()
    {
        Debug.Log(
            "[MissionManager] Account cleared → " +
            "Clear Mission runtime."
        );


        ClearRuntimeData();
    }


    // =========================================================
    // INITIALIZE ACCOUNT
    // =========================================================

    private void InitializeForCurrentAccount()
    {
        if (
            firestoreManager == null ||
            !firestoreManager.HasPlayerData ||
            firestoreManager.CurrentPlayerData == null
        )
        {
            return;
        }


        PlayerData playerData =
            firestoreManager.CurrentPlayerData;


        if (string.IsNullOrEmpty(playerData.uid))
        {
            Debug.LogError(
                "[MissionManager] PlayerData UID rỗng."
            );

            return;
        }


        accountInitialized = false;

        missionDataDirty = false;

        saveAgainAfterCurrentSave = false;


        LoadOrGenerateDailyMissions();


        accountInitialized = true;


        SyncPreviousExpReference();


        TryBindSystems();


        CheckProgressionMissions();


        Debug.Log(
            "[MissionManager] Mission initialized " +
            $"cho UID: {playerData.uid}"
        );


        LogCurrentDailyMissions();
    }


    // =========================================================
    // SYNC PREVIOUS EXP
    // =========================================================

    private void SyncPreviousExpReference()
    {
        if (
            firestoreManager == null ||
            firestoreManager.CurrentPlayerData == null
        )
        {
            return;
        }


        PlayerData data =
            firestoreManager.CurrentPlayerData;


        int currentTotalExp =
            GetCurrentTotalExp();


        // Nếu Firestore đang dùng giá trị cũ 0
        // nhưng account thực tế đã có EXP,
        // không tính toàn bộ EXP cũ thành EXP hôm nay.
        if (
            data.missionPreviousTotalExp == 0 &&
            currentTotalExp > 0 &&
            data.missionDailyExp == 0
        )
        {
            data.missionPreviousTotalExp =
                currentTotalExp;


            missionDataDirty = true;
        }
    }


    // =========================================================
    // CLEAR RUNTIME
    // =========================================================

    private void ClearRuntimeData()
    {
        if (delayedSaveCoroutine != null)
        {
            StopCoroutine(
                delayedSaveCoroutine
            );

            delayedSaveCoroutine = null;
        }


        missions.Clear();


        dailyCoins = 0;

        dailyDistance = 0;

        dailyRuns = 0;

        dailyExp = 0;


        lastRunCoins = 0;

        lastRunDistance = 0;


        accountInitialized = false;

        missionDataDirty = false;

        saveInProgress = false;

        saveAgainAfterCurrentSave = false;

        claimInProgress = false;

        claimMissionId = null;


        isGameplaySceneActive = false;


        UnsubscribeFromSystems();


        connectedGameManager = null;

        connectedPlayerProgression = null;


        OnDailyMissionsGenerated?.Invoke();


        Debug.Log(
            "[MissionManager] Runtime Mission đã clear."
        );
    }


    // =========================================================
    // SCENE LOADED
    // =========================================================

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        bool gameplay =
            scene.name ==
            GameplaySceneName;


        isGameplaySceneActive =
            gameplay;


        Debug.Log(
            $"[MissionManager] Scene Loaded → " +
            $"{scene.name} | " +
            $"Gameplay = {gameplay}"
        );


        if (!gameplay)
        {
            // MainMenu / Garage / Login / scene khác:
            //
            // KHÔNG được giữ GameManager run-state.
            //
            // Daily mission data vẫn giữ nguyên.
            DisconnectGameplaySystems();

            return;
        }


        // Gameplay vừa load.
        // Chờ 1 frame để GameManager / Player / PlayerProgression
        // hoàn tất Awake/Start.
        StartCoroutine(
            BindSystemsNextFrame()
        );
    }


    // =========================================================
    // SCENE UNLOADED
    // =========================================================

    private void HandleSceneUnloaded(
        Scene scene)
    {
        if (
            scene.name ==
            GameplaySceneName
        )
        {
            Debug.Log(
                "[MissionManager] GameplayScene unloaded → " +
                "Disconnect Gameplay systems."
            );


            DisconnectGameplaySystems();
        }
    }


    // =========================================================
    // DISCONNECT GAMEPLAY SYSTEMS
    // =========================================================

    private void DisconnectGameplaySystems()
    {
        // -----------------------------------------------------
        // GAME MANAGER
        // -----------------------------------------------------

        UnsubscribeGameManager();

        connectedGameManager = null;


        // -----------------------------------------------------
        // CURRENT RUN BASELINE
        // -----------------------------------------------------

        lastRunCoins = 0;

        lastRunDistance = 0;


        // -----------------------------------------------------
        // PLAYER PROGRESSION
        // -----------------------------------------------------
        //
        // PlayerProgression là persistent progression.
        //
        // Không bắt buộc phải unsubscribe ở MainMenu.
        // Tuy nhiên để tránh duplicate binding khi scene
        // thay đổi và PlayerProgression được recreate,
        // TryBindSystems() sẽ resolve lại reference.
        //
        // Ta giữ progression subscription nếu object vẫn sống.
        //
        // -----------------------------------------------------

        Debug.Log(
            "[MissionManager] Gameplay binding disconnected. " +
            "Daily counters được giữ nguyên."
        );
    }


    // =========================================================
    // BIND SYSTEMS NEXT FRAME
    // =========================================================

    private IEnumerator BindSystemsNextFrame()
    {
        yield return null;

        TryBindSystems();
    }


    // =========================================================
    // BIND SYSTEMS
    // =========================================================

    private void TryBindSystems()
    {
        if (!accountInitialized)
        {
            return;
        }


        // =====================================================
        // CHECK ACTIVE SCENE
        // =====================================================

        Scene activeScene =
            SceneManager.GetActiveScene();


        bool gameplay =
            activeScene.name ==
            GameplaySceneName;


        isGameplaySceneActive =
            gameplay;


        // =====================================================
        // GAME MANAGER
        // =====================================================
        //
        // GameManager là RUN-SCOPED.
        //
        // Chỉ bind khi GameplayScene active.
        //
        // Đây là điểm quan trọng nhất của fix.
        // =====================================================

        if (gameplay)
        {
            GameManager currentGameManager =
                GameManager.Instance;


            if (
                currentGameManager !=
                connectedGameManager
            )
            {
                if (connectedGameManager != null)
                {
                    UnsubscribeGameManager();
                }


                connectedGameManager =
                    currentGameManager;


                if (connectedGameManager != null)
                {
                    SubscribeGameManager();

                    InitializeCurrentRunCounters();


                    Debug.Log(
                        "[MissionManager] Đã Bind GameManager " +
                        "trong Gameplay."
                    );
                }
            }
        }
        else
        {
            // Không bao giờ bind GameManager ở MainMenu.
            if (connectedGameManager != null)
            {
                UnsubscribeGameManager();

                connectedGameManager = null;
            }


            lastRunCoins = 0;

            lastRunDistance = 0;
        }


        // =====================================================
        // PLAYER PROGRESSION
        // =====================================================
        //
        // PlayerProgression là account progression.
        // Có thể tồn tại ở MainMenu.
        // =====================================================

        PlayerProgression currentProgression =
            FindFirstObjectByType<PlayerProgression>();


        if (
            currentProgression !=
            connectedPlayerProgression
        )
        {
            if (connectedPlayerProgression != null)
            {
                UnsubscribePlayerProgression();
            }


            connectedPlayerProgression =
                currentProgression;


            if (connectedPlayerProgression != null)
            {
                SubscribePlayerProgression();

                SyncPreviousExpReference();

                CheckProgressionMissions();


                Debug.Log(
                    "[MissionManager] Đã Bind PlayerProgression."
                );
            }
        }
    }


    // =========================================================
    // GAME MANAGER
    // =========================================================

    private void SubscribeGameManager()
    {
        if (connectedGameManager == null)
            return;


        connectedGameManager.OnCoinChanged +=
            HandleCoinChanged;


        connectedGameManager.OnScoreChanged +=
            HandleScoreChanged;


        connectedGameManager.OnGameOver +=
            HandleGameOver;
    }


    private void UnsubscribeGameManager()
    {
        if (connectedGameManager == null)
            return;


        connectedGameManager.OnCoinChanged -=
            HandleCoinChanged;


        connectedGameManager.OnScoreChanged -=
            HandleScoreChanged;


        connectedGameManager.OnGameOver -=
            HandleGameOver;
    }


    // =========================================================
    // PLAYER PROGRESSION
    // =========================================================

    private void SubscribePlayerProgression()
    {
        if (connectedPlayerProgression == null)
            return;


        connectedPlayerProgression.OnExpChanged +=
            HandleExpChanged;


        connectedPlayerProgression.OnLevelUp +=
            HandleLevelUp;
    }


    private void UnsubscribePlayerProgression()
    {
        if (connectedPlayerProgression == null)
            return;


        connectedPlayerProgression.OnExpChanged -=
            HandleExpChanged;


        connectedPlayerProgression.OnLevelUp -=
            HandleLevelUp;
    }


    // =========================================================
    // UNSUBSCRIBE
    // =========================================================

    private void UnsubscribeFromSystems()
    {
        UnsubscribeGameManager();

        UnsubscribePlayerProgression();


        connectedGameManager = null;

        connectedPlayerProgression = null;


        lastRunCoins = 0;

        lastRunDistance = 0;

        isGameplaySceneActive = false;
    }


    // =========================================================
    // BUILD MISSION POOL
    // =========================================================

    private void BuildMissionPool()
    {
        missionPool.Clear();

        // =====================================================
        // COINS
        // =====================================================

        missionPool.Add(
            new MissionTemplate(
                "coin_30",
                "Gom Vàng",
                "Thu thập 30 đồng vàng.",
                MissionType.CollectCoins,
                30,
                30,
                100
            )
        );

        missionPool.Add(
            new MissionTemplate(
                "coin_50",
                "Hốt Bạc",
                "Thu thập 50 đồng vàng.",
                MissionType.CollectCoins,
                50,
                50,
                150
            )
        );

        missionPool.Add(
            new MissionTemplate(
                "coin_100",
                "Đại Gia Đường Phố",
                "Thu thập 100 đồng vàng.",
                MissionType.CollectCoins,
                100,
                100,
                300
            )
        );

        missionPool.Add(
            new MissionTemplate(
                "coin_200",
                "Máy In Tiền",
                "Thu thập 200 đồng vàng.",
                MissionType.CollectCoins,
                200,
                150,
                500
            )
        );

        // =====================================================
        // DISTANCE
        // =====================================================

        missionPool.Add(
            new MissionTemplate(
                "distance_500",
                "Chạy Nóng Máy",
                "Chạy được 500m.",
                MissionType.TravelDistance,
                500,
                40,
                100
            )
        );

        missionPool.Add(
            new MissionTemplate(
                "distance_1000",
                "Không Phanh",
                "Chạy được 1000m.",
                MissionType.TravelDistance,
                1000,
                80,
                200
            )
        );

        missionPool.Add(
            new MissionTemplate(
                "distance_2000",
                "Quái Xế",
                "Chạy được 2000m.",
                MissionType.TravelDistance,
                2000,
                150,
                400
            )
        );

        missionPool.Add(
            new MissionTemplate(
                "distance_5000",
                "Lead Bất Tử",
                "Chạy được 5000m.",
                MissionType.TravelDistance,
                5000,
                250,
                750
            )
        );

        // =====================================================
        // RUNS
        // =====================================================

        missionPool.Add(
            new MissionTemplate(
                "runs_2",
                "Khởi Động",
                "Hoàn thành 2 lượt chơi.",
                MissionType.CompleteRuns,
                2,
                50,
                150
            )
        );

        missionPool.Add(
            new MissionTemplate(
                "runs_3",
                "Tay Lái",
                "Hoàn thành 3 lượt chơi.",
                MissionType.CompleteRuns,
                3,
                75,
                250
            )
        );

        missionPool.Add(
            new MissionTemplate(
                "runs_5",
                "Quái Xế Chính Hiệu",
                "Hoàn thành 5 lượt chơi.",
                MissionType.CompleteRuns,
                5,
                120,
                400
            )
        );

        // =====================================================
        // EXP
        // =====================================================

        missionPool.Add(
            new MissionTemplate(
                "exp_100",
                "Tích Lũy Kinh Nghiệm",
                "Kiếm 100 EXP trong ngày.",
                MissionType.EarnExp,
                100,
                50,
                100
            )
        );

        missionPool.Add(
            new MissionTemplate(
                "exp_250",
                "Lên Tay",
                "Kiếm 250 EXP trong ngày.",
                MissionType.EarnExp,
                250,
                100,
                250
            )
        );

        missionPool.Add(
            new MissionTemplate(
                "exp_500",
                "Kinh Nghiệm Đầy Mình",
                "Kiếm 500 EXP trong ngày.",
                MissionType.EarnExp,
                500,
                200,
                500
            )
        );

        // =====================================================
        // LEVEL
        // =====================================================

        missionPool.Add(
            new MissionTemplate(
                "level_3",
                "Tay Lái Mới",
                "Đạt Level 3.",
                MissionType.ReachLevel,
                3,
                75,
                150
            )
        );

        missionPool.Add(
            new MissionTemplate(
                "level_5",
                "Tay Lái Cứng",
                "Đạt Level 5.",
                MissionType.ReachLevel,
                5,
                150,
                350
            )
        );

        missionPool.Add(
            new MissionTemplate(
                "level_10",
                "Trùm Đường Phố",
                "Đạt Level 10.",
                MissionType.ReachLevel,
                10,
                300,
                1000
            )
        );
    }


    // =========================================================
    // DATE
    // =========================================================

    private string GetTodayKey()
    {
        return DateTime.Now.ToString(
            "yyyyMMdd"
        );
    }


    private int GetTodaySeed()
    {
        DateTime now =
            DateTime.Now;


        return
            now.Year * 10000 +
            now.Month * 100 +
            now.Day;
    }


    // =========================================================
    // LOAD / GENERATE
    // =========================================================

    private void LoadOrGenerateDailyMissions()
    {
        if (
            firestoreManager == null ||
            firestoreManager.CurrentPlayerData == null
        )
        {
            return;
        }


        PlayerData data =
            firestoreManager.CurrentPlayerData;


        string today =
            GetTodayKey();


        string savedDate =
            data.missionDate;


        Debug.Log(
            $"[MissionManager] Today: {today} | " +
            $"Firestore MissionDate: {savedDate}"
        );


        // =====================================================
        // NEW DAY
        // =====================================================

        if (savedDate != today)
        {
            ResetDailyCounters();

            GenerateDailyMissions(
                today
            );

            return;
        }


        // =====================================================
        // SAME DAY
        // =====================================================

        LoadDailyCounters();

        LoadDailyMissions();


        if (
            missions.Count !=
            DailyMissionCount
        )
        {
            Debug.LogWarning(
                "[MissionManager] Mission data không hợp lệ. " +
                "Generate lại."
            );


            GenerateDailyMissions(
                today
            );


            return;
        }


        Debug.Log(
            "[MissionManager] Đã load Daily Mission từ Firestore."
        );
    }


    // =========================================================
    // GENERATE DAILY
    // =========================================================

    private void GenerateDailyMissions(
        string dateKey)
    {
        missions.Clear();


        if (
            firestoreManager == null ||
            firestoreManager.CurrentPlayerData == null
        )
        {
            return;
        }


        if (
            missionPool.Count <
            DailyMissionCount
        )
        {
            Debug.LogError(
                "[MissionManager] Mission Pool không đủ."
            );

            return;
        }


        System.Random random =
            new System.Random(
                GetTodaySeed()
            );


        List<int> availableIndexes =
            new List<int>();


        for (
            int i = 0;
            i < missionPool.Count;
            i++)
        {
            availableIndexes.Add(i);
        }


        List<string> selectedTemplateIds =
            new List<string>();


        while (
            selectedTemplateIds.Count <
            DailyMissionCount
        )
        {
            int randomPosition =
                random.Next(
                    0,
                    availableIndexes.Count
                );


            int poolIndex =
                availableIndexes[
                    randomPosition
                ];


            availableIndexes.RemoveAt(
                randomPosition
            );


            MissionTemplate template =
                missionPool[
                    poolIndex
                ];


            if (template == null)
                continue;


            selectedTemplateIds.Add(
                template.id
            );


            Mission mission =
                CreateRuntimeMission(
                    template,
                    dateKey
                );


            missions.Add(
                mission
            );
        }


        PlayerData data =
            firestoreManager.CurrentPlayerData;


        data.missionDate =
            dateKey;


        data.missionSelection =
            new List<string>(
                selectedTemplateIds
            );


        data.missionStates =
            new List<MissionSaveData>();


        data.missionPreviousTotalExp =
            GetCurrentTotalExp();


        SaveRuntimeCountersToPlayerData();

        RebuildMissionProgressFromCounters();


        missionDataDirty = true;


        Debug.Log(
            $"[MissionManager] Đã tạo " +
            $"{missions.Count} Daily Mission cho {dateKey}."
        );


        LogCurrentDailyMissions();


        OnDailyMissionsGenerated?.Invoke();


        SaveMissionDataImmediately();
    }


    // =========================================================
    // CREATE RUNTIME MISSION
    // =========================================================

    private Mission CreateRuntimeMission(
        MissionTemplate template,
        string dateKey)
    {
        Mission mission =
            new Mission();


        mission.id =
            dateKey +
            "_" +
            template.id;


        mission.title =
            template.title;


        mission.description =
            template.description;


        mission.type =
            template.type;


        mission.target =
            Mathf.Max(
                1,
                template.target
            );


        mission.expReward =
            Mathf.Max(
                0,
                template.expReward
            );


        mission.goldReward =
            Mathf.Max(
                0,
                template.goldReward
            );


        mission.progress = 0;

        mission.claimed = false;


        return mission;
    }


    // =========================================================
    // LOAD DAILY MISSIONS
    // =========================================================

    private void LoadDailyMissions()
    {
        missions.Clear();


        if (
            firestoreManager == null ||
            firestoreManager.CurrentPlayerData == null
        )
        {
            return;
        }


        PlayerData data =
            firestoreManager.CurrentPlayerData;


        List<string> selectedIds =
            data.missionSelection;


        if (
            selectedIds == null ||
            selectedIds.Count == 0
        )
        {
            Debug.LogWarning(
                "[MissionManager] Firestore không có Mission Selection."
            );


            GenerateDailyMissions(
                GetTodayKey()
            );


            return;
        }


        string today =
            GetTodayKey();


        for (
            int i = 0;
            i < selectedIds.Count;
            i++)
        {
            string templateId =
                selectedIds[i];


            MissionTemplate template =
                FindTemplate(
                    templateId
                );


            if (template == null)
            {
                Debug.LogWarning(
                    "[MissionManager] Không tìm thấy Template: " +
                    templateId
                );


                continue;
            }


            Mission mission =
                CreateRuntimeMission(
                    template,
                    today
                );


            MissionSaveData saved =
                FindSavedMissionState(
                    mission.id
                );


            if (saved != null)
            {
                mission.progress =
                    Mathf.Clamp(
                        saved.progress,
                        0,
                        mission.target
                    );


                mission.claimed =
                    saved.claimed;
            }


            missions.Add(
                mission
            );
        }


        // Nếu Firestore có selection nhưng thiếu state,
        // progress vẫn được phục hồi từ daily counters.
        RebuildMissionProgressFromCounters(
            false
        );
    }


    // =========================================================
    // FIND TEMPLATE
    // =========================================================

    private MissionTemplate FindTemplate(
        string templateId)
    {
        for (
            int i = 0;
            i < missionPool.Count;
            i++)
        {
            MissionTemplate template =
                missionPool[i];


            if (template == null)
                continue;


            if (
                template.id ==
                templateId
            )
            {
                return template;
            }
        }


        return null;
    }


    // =========================================================
    // FIND SAVED STATE
    // =========================================================

    private MissionSaveData FindSavedMissionState(
        string missionId)
    {
        if (
            firestoreManager == null ||
            firestoreManager.CurrentPlayerData == null
        )
        {
            return null;
        }


        List<MissionSaveData> states =
            firestoreManager.CurrentPlayerData
                .missionStates;


        if (states == null)
            return null;


        for (
            int i = 0;
            i < states.Count;
            i++)
        {
            MissionSaveData state =
                states[i];


            if (state == null)
                continue;


            if (
                state.id ==
                missionId
            )
            {
                return state;
            }
        }


        return null;
    }


    // =========================================================
    // REBUILD PROGRESS
    // =========================================================

    private void RebuildMissionProgressFromCounters(
        bool overwrite = true)
    {
        for (
            int i = 0;
            i < missions.Count;
            i++)
        {
            Mission mission =
                missions[i];


            if (mission == null)
                continue;


            int calculatedProgress =
                GetCalculatedMissionProgress(
                    mission
                );


            if (overwrite)
            {
                mission.progress =
                    Mathf.Clamp(
                        calculatedProgress,
                        0,
                        mission.target
                    );
            }
            else
            {
                // Chỉ bổ sung progress nếu state chưa có.
                if (mission.progress <= 0)
                {
                    mission.progress =
                        Mathf.Clamp(
                            calculatedProgress,
                            0,
                            mission.target
                        );
                }
            }
        }
    }


    private int GetCalculatedMissionProgress(
        Mission mission)
    {
        if (mission == null)
            return 0;


        switch (mission.type)
        {
            case MissionType.CollectCoins:
                return dailyCoins;


            case MissionType.TravelDistance:
                return dailyDistance;


            case MissionType.CompleteRuns:
                return dailyRuns;


            case MissionType.EarnExp:
                return dailyExp;


            case MissionType.ReachLevel:
                if (
                    connectedPlayerProgression != null
                )
                {
                    return connectedPlayerProgression.Level;
                }


                if (
                    firestoreManager != null &&
                    firestoreManager.CurrentPlayerData != null
                )
                {
                    return firestoreManager
                        .CurrentPlayerData
                        .level;
                }


                return 0;
        }


        return 0;
    }


    // =========================================================
    // DAILY COUNTERS RESET
    // =========================================================

    private void ResetDailyCounters()
    {
        dailyCoins = 0;

        dailyDistance = 0;

        dailyRuns = 0;

        dailyExp = 0;


        if (
            firestoreManager != null &&
            firestoreManager.CurrentPlayerData != null
        )
        {
            PlayerData data =
                firestoreManager.CurrentPlayerData;


            data.missionDailyCoins = 0;

            data.missionDailyDistance = 0;

            data.missionDailyRuns = 0;

            data.missionDailyExp = 0;


            data.missionStates =
                new List<MissionSaveData>();


            data.missionSelection =
                new List<string>();


            data.missionPreviousTotalExp =
                GetCurrentTotalExp();
        }
    }


    // =========================================================
    // LOAD DAILY COUNTERS
    // =========================================================

    private void LoadDailyCounters()
    {
        if (
            firestoreManager == null ||
            firestoreManager.CurrentPlayerData == null
        )
        {
            return;
        }


        PlayerData data =
            firestoreManager.CurrentPlayerData;


        dailyCoins =
            Mathf.Max(
                0,
                data.missionDailyCoins
            );


        dailyDistance =
            Mathf.Max(
                0,
                data.missionDailyDistance
            );


        dailyRuns =
            Mathf.Max(
                0,
                data.missionDailyRuns
            );


        dailyExp =
            Mathf.Max(
                0,
                data.missionDailyExp
            );


        Debug.Log(
            "[MissionManager] Daily Counters → " +
            $"Coins: {dailyCoins} | " +
            $"Distance: {dailyDistance} | " +
            $"Runs: {dailyRuns} | " +
            $"EXP: {dailyExp}"
        );
    }


    // =========================================================
    // SAVE COUNTERS TO PLAYER DATA
    // =========================================================

    private void SaveRuntimeCountersToPlayerData()
    {
        if (
            firestoreManager == null ||
            firestoreManager.CurrentPlayerData == null
        )
        {
            return;
        }


        PlayerData data =
            firestoreManager.CurrentPlayerData;


        data.missionDailyCoins =
            Mathf.Max(
                0,
                dailyCoins
            );


        data.missionDailyDistance =
            Mathf.Max(
                0,
                dailyDistance
            );


        data.missionDailyRuns =
            Mathf.Max(
                0,
                dailyRuns
            );


        data.missionDailyExp =
            Mathf.Max(
                0,
                dailyExp
            );
    }


    // =========================================================
    // SAVE STATES TO PLAYER DATA
    // =========================================================

    private void SaveMissionStatesToPlayerData()
    {
        if (
            firestoreManager == null ||
            firestoreManager.CurrentPlayerData == null
        )
        {
            return;
        }


        PlayerData data =
            firestoreManager.CurrentPlayerData;


        List<MissionSaveData> states =
            new List<MissionSaveData>();


        for (
            int i = 0;
            i < missions.Count;
            i++
        )
        {
            Mission mission =
                missions[i];


            if (mission == null)
                continue;


            states.Add(
                new MissionSaveData(
                    mission.id,
                    mission.progress,
                    mission.claimed
                )
            );
        }


        data.missionStates =
            states;
    }


    // =========================================================
    // SYNC ALL MISSION DATA
    // =========================================================

    private void SyncMissionDataToPlayerData()
    {
        if (
            firestoreManager == null ||
            firestoreManager.CurrentPlayerData == null
        )
        {
            return;
        }


        SaveRuntimeCountersToPlayerData();

        SaveMissionStatesToPlayerData();


        firestoreManager.CurrentPlayerData
            .missionDate =
            GetTodayKey();


        List<string> selection =
            new List<string>();


        for (
            int i = 0;
            i < missions.Count;
            i++
        )
        {
            Mission mission =
                missions[i];


            if (mission == null)
                continue;


            string templateId =
                ExtractTemplateId(
                    mission.id
                );


            if (!string.IsNullOrEmpty(templateId))
            {
                selection.Add(
                    templateId
                );
            }
        }


        if (selection.Count > 0)
        {
            firestoreManager.CurrentPlayerData
                .missionSelection =
                selection;
        }
    }


    // =========================================================
    // EXTRACT TEMPLATE ID
    // =========================================================

    private string ExtractTemplateId(
        string missionId)
    {
        if (string.IsNullOrEmpty(missionId))
            return string.Empty;


        int separator =
            missionId.IndexOf("_");


        if (separator < 0)
            return missionId;


        return missionId.Substring(
            separator + 1
        );
    }


    // =========================================================
    // MARK DIRTY
    // =========================================================

    private void MarkMissionDataDirty(
        bool immediate = false)
    {
        if (!accountInitialized)
            return;


        missionDataDirty = true;


        SyncMissionDataToPlayerData();


        if (saveInProgress)
        {
            saveAgainAfterCurrentSave = true;
            return;
        }


        if (immediate)
        {
            ScheduleImmediateSave();
            return;
        }


        ScheduleDelayedSave();
    }


    // =========================================================
    // SCHEDULE DELAYED SAVE
    // =========================================================

    private void ScheduleDelayedSave()
    {
        if (delayedSaveCoroutine != null)
        {
            StopCoroutine(
                delayedSaveCoroutine
            );
        }


        delayedSaveCoroutine =
            StartCoroutine(
                DelayedSaveMissionData()
            );
    }


    private IEnumerator DelayedSaveMissionData()
    {
        yield return new WaitForSecondsRealtime(
            SaveDelay
        );


        delayedSaveCoroutine = null;


        if (!missionDataDirty)
            yield break;


        SaveMissionDataToFirestore();
    }


    // =========================================================
    // SCHEDULE IMMEDIATE SAVE
    // =========================================================

    private void ScheduleImmediateSave()
    {
        if (delayedSaveCoroutine != null)
        {
            StopCoroutine(
                delayedSaveCoroutine
            );

            delayedSaveCoroutine = null;
        }


        StartCoroutine(
            SaveMissionDataNextFrame()
        );
    }


    private IEnumerator SaveMissionDataNextFrame()
    {
        yield return null;


        if (!missionDataDirty)
            yield break;


        SaveMissionDataToFirestore();
    }


    // =========================================================
    // PUBLIC INTERNAL SAVE
    // =========================================================

    private void SaveMissionDataImmediately()
    {
        if (!accountInitialized)
            return;


        missionDataDirty = true;


        SyncMissionDataToPlayerData();


        if (saveInProgress)
        {
            saveAgainAfterCurrentSave = true;
            return;
        }


        SaveMissionDataToFirestore();
    }


    // =========================================================
    // FIRESTORE SAVE
    // =========================================================

    private void SaveMissionDataToFirestore()
    {
        if (saveInProgress)
        {
            saveAgainAfterCurrentSave = true;
            return;
        }


        if (
            firestoreManager == null ||
            !firestoreManager.HasPlayerData
        )
        {
            return;
        }


        SyncMissionDataToPlayerData();


        SaveMissionDataAsync();
    }


    private async void SaveMissionDataAsync()
    {
        if (saveInProgress)
        {
            saveAgainAfterCurrentSave = true;
            return;
        }


        if (
            firestoreManager == null ||
            !firestoreManager.HasPlayerData ||
            firestoreManager.CurrentPlayerData == null
        )
        {
            return;
        }


        saveInProgress = true;

        saveAgainAfterCurrentSave = false;


        string uid =
            firestoreManager.CurrentPlayerData.uid;


        bool success = false;


        try
        {
            success =
                await firestoreManager
                    .SaveMissionDataAsync();
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[MissionManager] Mission save exception: {exception}"
            );
        }


        saveInProgress = false;


        bool needsAnotherSave =
            saveAgainAfterCurrentSave;


        saveAgainAfterCurrentSave = false;


        if (success)
        {
            Debug.Log(
                $"[MissionManager] Mission saved → UID: {uid}"
            );


            if (!needsAnotherSave)
            {
                missionDataDirty = false;
            }
        }
        else
        {
            Debug.LogWarning(
                "[MissionManager] Mission save thất bại."
            );
        }


        if (needsAnotherSave)
        {
            missionDataDirty = true;


            Debug.Log(
                "[MissionManager] Phát hiện thay đổi mới " +
                "trong lúc đang save → tiếp tục save."
            );


            ScheduleDelayedSave();
        }
    }


    // =========================================================
    // CURRENT RUN
    // =========================================================

    private void InitializeCurrentRunCounters()
    {
        if (
            !isGameplaySceneActive ||
            connectedGameManager == null
        )
        {
            lastRunCoins = 0;

            lastRunDistance = 0;

            return;
        }


        lastRunCoins =
            Mathf.Max(
                0,
                connectedGameManager.CoinCount
            );


        lastRunDistance =
            Mathf.Max(
                0,
                connectedGameManager.ScoreInt
            );


        Debug.Log(
            "[MissionManager] Current Run Counters → " +
            $"Coins: {lastRunCoins} | " +
            $"Distance: {lastRunDistance}"
        );
    }


    // =========================================================
    // COIN EVENT
    // =========================================================

    private void HandleCoinChanged(
        int coinCount)
    {
        // =====================================================
        // HARD GAMEPLAY GUARD
        // =====================================================

        if (
            !accountInitialized ||
            !isGameplaySceneActive ||
            connectedGameManager == null
        )
        {
            return;
        }


        // GameManager mới cũng phải đang active run.
        if (!connectedGameManager.IsRunActive)
        {
            return;
        }


        int currentCoins =
            Mathf.Max(
                0,
                coinCount
            );


        int delta =
            currentCoins -
            lastRunCoins;


        if (delta > 0)
        {
            dailyCoins += delta;


            UpdateMissionProgress(
                MissionType.CollectCoins,
                dailyCoins
            );


            SaveRuntimeCountersToPlayerData();


            MarkMissionDataDirty();
        }


        lastRunCoins =
            currentCoins;
    }


    // =========================================================
    // SCORE EVENT
    // =========================================================

    private void HandleScoreChanged(
        int score)
    {
        // =====================================================
        // HARD GAMEPLAY GUARD
        // =====================================================

        if (
            !accountInitialized ||
            !isGameplaySceneActive ||
            connectedGameManager == null
        )
        {
            return;
        }


        if (!connectedGameManager.IsRunActive)
        {
            return;
        }


        int currentDistance =
            Mathf.Max(
                0,
                score
            );


        int delta =
            currentDistance -
            lastRunDistance;


        if (delta > 0)
        {
            dailyDistance += delta;


            UpdateMissionProgress(
                MissionType.TravelDistance,
                dailyDistance
            );


            SaveRuntimeCountersToPlayerData();


            MarkMissionDataDirty();
        }


        if (
            currentDistance >=
            lastRunDistance
        )
        {
            lastRunDistance =
                currentDistance;
        }
        else
        {
            // Score reset trong cùng GameplayScene:
            // bắt đầu run mới.
            lastRunDistance = 0;
        }
    }


    // =========================================================
    // GAME OVER
    // =========================================================

    private void HandleGameOver()
    {
        // GameOver chỉ được tính là CompleteRuns
        // nếu GameManager đang ở Gameplay và run thực sự active.
        if (
            !accountInitialized ||
            !isGameplaySceneActive ||
            connectedGameManager == null ||
            !connectedGameManager.IsRunActive
        )
        {
            return;
        }


        dailyRuns++;


        UpdateMissionProgress(
            MissionType.CompleteRuns,
            dailyRuns
        );


        CheckProgressionMissions();


        SaveRuntimeCountersToPlayerData();


        MarkMissionDataDirty(
            true
        );
    }


    // =========================================================
    // EXP EVENT
    // =========================================================

    private void HandleExpChanged(
        int currentLevelExp,
        int expRequiredForNextLevel)
    {
        if (!accountInitialized)
            return;


        PlayerProgression progression =
            connectedPlayerProgression;


        if (progression == null)
            return;


        int currentTotalExp =
            Mathf.Max(
                0,
                progression.TotalExp
            );


        int previousKnownTotalExp =
            GetPreviousTotalExp(
                currentTotalExp
            );


        int delta =
            currentTotalExp -
            previousKnownTotalExp;


        // Account load / reset có thể làm TotalExp giảm.
        // Không tính âm thành EXP trong ngày.
        if (delta > 0)
        {
            dailyExp += delta;


            UpdateMissionProgress(
                MissionType.EarnExp,
                dailyExp
            );


            SaveRuntimeCountersToPlayerData();


            MarkMissionDataDirty();
        }


        if (
            firestoreManager != null &&
            firestoreManager.CurrentPlayerData != null
        )
        {
            firestoreManager.CurrentPlayerData
                .missionPreviousTotalExp =
                currentTotalExp;
        }


        CheckProgressionMissions();
    }


    // =========================================================
    // PREVIOUS EXP
    // =========================================================

    private int GetPreviousTotalExp(
        int fallback)
    {
        if (
            firestoreManager == null ||
            firestoreManager.CurrentPlayerData == null
        )
        {
            return fallback;
        }


        return Mathf.Max(
            0,
            firestoreManager.CurrentPlayerData
                .missionPreviousTotalExp
        );
    }


    // =========================================================
    // CURRENT TOTAL EXP
    // =========================================================

    private int GetCurrentTotalExp()
    {
        if (
            connectedPlayerProgression != null
        )
        {
            return Mathf.Max(
                0,
                connectedPlayerProgression.TotalExp
            );
        }


        if (
            firestoreManager != null &&
            firestoreManager.CurrentPlayerData != null
        )
        {
            return Mathf.Max(
                0,
                firestoreManager.CurrentPlayerData.exp
            );
        }


        return 0;
    }


    // =========================================================
    // LEVEL UP
    // =========================================================

    private void HandleLevelUp(
        int newLevel)
    {
        if (!accountInitialized)
            return;


        UpdateMissionProgress(
            MissionType.ReachLevel,
            newLevel
        );


        MarkMissionDataDirty();
    }


    // =========================================================
    // PROGRESSION CHECK
    // =========================================================

    private void CheckProgressionMissions()
    {
        if (
            connectedPlayerProgression == null
        )
        {
            return;
        }


        UpdateMissionProgress(
            MissionType.ReachLevel,
            connectedPlayerProgression.Level
        );
    }


    // =========================================================
    // UPDATE MISSION
    // =========================================================

    private void UpdateMissionProgress(
        MissionType type,
        int value)
    {
        if (
            !accountInitialized ||
            missions == null
        )
        {
            return;
        }


        bool changed = false;


        for (
            int i = 0;
            i < missions.Count;
            i++
        )
        {
            Mission mission =
                missions[i];


            if (mission == null)
                continue;


            if (mission.type != type)
                continue;


            if (mission.claimed)
                continue;


            if (mission.IsCompleted)
                continue;


            int oldProgress =
                mission.progress;


            mission.progress =
                Mathf.Clamp(
                    value,
                    0,
                    mission.target
                );


            if (
                mission.progress !=
                oldProgress
            )
            {
                changed = true;


                OnMissionProgressChanged?.Invoke(
                    mission
                );


                Debug.Log(
                    $"[MissionManager] Progress → " +
                    $"{mission.title}: " +
                    $"{oldProgress}/" +
                    $"{mission.target} → " +
                    $"{mission.progress}/" +
                    $"{mission.target}"
                );
            }


            if (
                mission.IsCompleted &&
                oldProgress <
                mission.target
            )
            {
                OnMissionCompleted?.Invoke(
                    mission
                );
            }
        }


        if (changed)
        {
            SaveMissionStatesToPlayerData();

            MarkMissionDataDirty();
        }
    }


    // =========================================================
    // DAILY COMPLETION
    // =========================================================

    public bool AreAllDailyMissionsClaimed
    {
        get
        {
            if (!accountInitialized)
                return false;


            if (
                missions == null ||
                missions.Count != DailyMissionCount
            )
            {
                return false;
            }


            for (
                int i = 0;
                i < missions.Count;
                i++
            )
            {
                Mission mission =
                    missions[i];


                if (
                    mission == null ||
                    !mission.claimed
                )
                {
                    return false;
                }
            }


            return true;
        }
    }


    private void CheckAllDailyMissionsClaimed()
    {
        if (!AreAllDailyMissionsClaimed)
            return;


        Debug.Log(
            "[MissionManager] ALL DAILY MISSIONS CLAIMED."
        );


        OnAllDailyMissionsClaimed?.Invoke();
    }


    // =========================================================
    // GET MISSION
    // =========================================================

    public Mission GetMission(
        string missionId)
    {
        if (missions == null)
            return null;


        for (
            int i = 0;
            i < missions.Count;
            i++
        )
        {
            Mission mission =
                missions[i];


            if (mission == null)
                continue;


            if (
                mission.id ==
                missionId
            )
            {
                return mission;
            }
        }


        return null;
    }


    // =========================================================
    // GET ALL MISSIONS
    // =========================================================

    public List<Mission> GetMissions()
    {
        return missions;
    }


    // =========================================================
    // CLAIM MISSION
    // =========================================================

    public bool ClaimMission(
        string missionId)
    {
        if (!accountInitialized)
        {
            Debug.LogWarning(
                "[MissionManager] Account chưa được initialize."
            );

            return false;
        }


        if (claimInProgress)
        {
            Debug.LogWarning(
                "[MissionManager] Claim đang được xử lý. " +
                "Bỏ qua request trùng."
            );

            return false;
        }


        Mission mission =
            GetMission(
                missionId
            );


        if (mission == null)
        {
            Debug.LogWarning(
                "[MissionManager] Không tìm thấy Mission: " +
                missionId
            );

            return false;
        }


        if (mission.claimed)
        {
            Debug.LogWarning(
                "[MissionManager] Mission đã nhận thưởng: " +
                mission.title
            );

            return false;
        }


        if (!mission.IsCompleted)
        {
            Debug.LogWarning(
                "[MissionManager] Mission chưa hoàn thành: " +
                $"{mission.title} " +
                $"({mission.progress}/{mission.target})"
            );

            return false;
        }


        if (
            firestoreManager == null ||
            !firestoreManager.HasPlayerData ||
            firestoreManager.CurrentPlayerData == null
        )
        {
            Debug.LogError(
                "[MissionManager] Không có PlayerData."
            );

            return false;
        }


        PlayerProgression progression =
            connectedPlayerProgression;


        if (progression == null)
        {
            Debug.LogError(
                "[MissionManager] Không tìm thấy PlayerProgression."
            );

            return false;
        }


        claimInProgress = true;

        claimMissionId = missionId;


        ClaimMissionAsync(
            mission,
            progression
        );


        return true;
    }


    private async void ClaimMissionAsync(
        Mission mission,
        PlayerProgression progression)
    {
        bool rewardSuccess = false;


        try
        {
            if (
                mission == null ||
                progression == null ||
                firestoreManager == null ||
                firestoreManager.CurrentPlayerData == null
            )
            {
                return;
            }


            // =================================================
            // GIVE EXP
            // =================================================

            if (mission.expReward > 0)
            {
                await progression.AddExpAsync(
                    mission.expReward
                );
            }


            // =================================================
            // GIVE GOLD
            // =================================================

            if (mission.goldReward > 0)
            {
                bool goldSuccess =
                    await firestoreManager.AddCoinsAsync(
                        mission.goldReward
                    );


                if (!goldSuccess)
                {
                    Debug.LogError(
                        "[MissionManager] Không thể lưu Gold reward."
                    );

                    return;
                }
            }


            rewardSuccess = true;


            // =================================================
            // COMMIT CLAIMED
            // =================================================

            mission.claimed = true;


            SyncMissionDataToPlayerData();


            MarkMissionDataDirty(
                true
            );


            OnMissionClaimed?.Invoke(
                mission
            );


            CheckAllDailyMissionsClaimed();


            if (Time.timeScale <= 0f)
            {
                Time.timeScale = 1f;
            }


            Debug.Log(
                "[MissionManager] MISSION CLAIMED\n" +
                $"Title: {mission.title}\n" +
                $"Reward: +{mission.expReward} EXP | " +
                $"+{mission.goldReward} GOLD\n" +
                $"Claimed: {mission.claimed}"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[MissionManager] Claim Mission exception: " +
                exception
            );
        }
        finally
        {
            if (!rewardSuccess)
            {
                Debug.LogWarning(
                    "[MissionManager] Claim thất bại → " +
                    "mission chưa commit claimed: " +
                    $"{mission?.title}"
                );
            }


            claimInProgress = false;

            claimMissionId = null;
        }
    }


    // =========================================================
    // DEBUG LOG
    // =========================================================

    private void LogCurrentDailyMissions()
    {
        Debug.Log(
            "========== DAILY MISSIONS =========="
        );


        for (
            int i = 0;
            i < missions.Count;
            i++
        )
        {
            Mission mission =
                missions[i];


            if (mission == null)
                continue;


            Debug.Log(
                $"[{i + 1}] " +
                $"{mission.title} | " +
                $"Type: {mission.type} | " +
                $"Progress: {mission.progress}/" +
                $"{mission.target} | " +
                $"Reward: +{mission.expReward} EXP | " +
                $"+{mission.goldReward} GOLD | " +
                $"Claimed: {mission.claimed}"
            );
        }


        Debug.Log(
            "===================================="
        );
    }


    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("DEBUG Show Daily Missions")]
    private void DebugShowDailyMissions()
    {
        LogCurrentDailyMissions();
    }


    [ContextMenu("DEBUG Show Daily Counters")]
    private void DebugShowDailyCounters()
    {
        Debug.Log(
            "[MissionManager] DAILY COUNTERS\n" +
            $"Coins: {dailyCoins}\n" +
            $"Distance: {dailyDistance}\n" +
            $"Runs: {dailyRuns}\n" +
            $"EXP: {dailyExp}"
        );
    }


    // =========================================================
    // TEST RESET
    // =========================================================

    [ContextMenu("TEST Reset Daily Missions")]
    private void TestResetDailyMissions()
    {
        if (
            firestoreManager == null ||
            firestoreManager.CurrentPlayerData == null
        )
        {
            Debug.LogWarning(
                "[MissionManager TEST] " +
                "Chưa có account."
            );

            return;
        }


        ResetDailyCounters();


        GenerateDailyMissions(
            GetTodayKey()
        );


        MarkMissionDataDirty(
            true
        );


        Debug.Log(
            "[MissionManager TEST] " +
            "Đã reset Daily Mission trên account hiện tại."
        );
    }
}