using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour
{
    //=====================================================================
    // SINGLETON
    //=====================================================================

    public static GameManager Instance { get; private set; }


    //=====================================================================
    // SCENE
    //=====================================================================

    private const string GameplaySceneName = "GameplayScene";

    private bool runActive;


    //=====================================================================
    // PLAYER
    //=====================================================================

    [Header("Player")]
    [SerializeField]
    private Transform playerTransform;


    //=====================================================================
    // RUN DATA
    //=====================================================================

    private float score;
    private int coinCount;

    private bool isGameOver;

    // Player đã chết nhưng Run vẫn chưa kết thúc.
    private bool isDeathPending;

    // Số lần đã sử dụng Revive trong Run hiện tại.
    private int reviveCount;

    // Bảo vệ không cho Final Game Over chạy nhiều lần.
    private bool runFinalized;


    //=====================================================================
    // REVIVE SETTINGS
    //=====================================================================

    [Header("Revive")]

    [Tooltip(
        "Giá GEM cơ bản của lần Revive đầu tiên. " +
        "Ví dụ: 10 -> Revive #1 = 10 GEM."
    )]
    [Min(1)]
    [SerializeField]
    private int reviveBaseGemCost = 10;


    [Tooltip(
        "Số GEM tăng thêm sau mỗi lần Revive. " +
        "Ví dụ: 10 -> 10, 20, 30, 40..."
    )]
    [Min(0)]
    [SerializeField]
    private int reviveGemCostIncrease = 10;


    [Tooltip(
        "Thời gian người chơi được quyết định Revive sau khi chết."
    )]
    [Min(0f)]
    [SerializeField]
    private float reviveDecisionDuration = 3f;


    // Thời gian Revive còn lại.
    private float reviveDecisionTimer;


    //=====================================================================
    // PUBLIC RUN STATE
    //=====================================================================

    public float Score
    {
        get
        {
            return score;
        }
    }


    public int ScoreInt
    {
        get
        {
            return Mathf.FloorToInt(score);
        }
    }


    public int CoinCount
    {
        get
        {
            return coinCount;
        }
    }


    public bool IsGameOver
    {
        get
        {
            return isGameOver;
        }
    }


    public bool IsDeathPending
    {
        get
        {
            return isDeathPending;
        }
    }


    /// <summary>
    /// True nếu Gameplay Run hiện tại đang hoạt động.
    ///
    /// MainMenu = false.
    /// Gameplay = true.
    /// </summary>
    public bool IsRunActive
    {
        get
        {
            return runActive;
        }
    }


    /// <summary>
    /// Player runtime hiện tại.
    ///
    /// Chỉ có giá trị khi đang Gameplay.
    /// </summary>
    public Transform PlayerTransform
    {
        get
        {
            return playerTransform;
        }
    }


    /// <summary>
    /// True nếu Player runtime hiện tại tồn tại.
    /// </summary>
    public bool HasPlayer
    {
        get
        {
            return playerTransform != null;
        }
    }


    /// <summary>
    /// True nếu scene hiện tại là GameplayScene.
    /// </summary>
    public bool IsGameplayScene
    {
        get
        {
            return SceneManager.GetActiveScene().name == GameplaySceneName;
        }
    }


    /// <summary>
    /// True nếu Player đã sử dụng ít nhất một lượt Revive
    /// trong Run hiện tại.
    ///
    /// Property này được giữ lại để tương thích với UI/script cũ.
    ///
    /// Không dùng property này để quyết định có được Revive hay không.
    /// Hãy dùng CanRevive.
    /// </summary>
    public bool HasUsedRevive
    {
        get
        {
            return reviveCount > 0;
        }
    }


    /// <summary>
    /// Số lần Revive đã sử dụng trong Run hiện tại.
    /// </summary>
    public int ReviveCount
    {
        get
        {
            return reviveCount;
        }
    }


    /// <summary>
    /// Giá GEM của lần Revive tiếp theo.
    /// </summary>
    public int ReviveGemCost
    {
        get
        {
            return GetReviveGemCost();
        }
    }


    /// <summary>
    /// Giá GEM cơ bản.
    /// </summary>
    public int ReviveBaseGemCost
    {
        get
        {
            return GetReviveBaseGemCost();
        }
    }


    /// <summary>
    /// Số GEM tăng thêm sau mỗi lần Revive.
    /// </summary>
    public int ReviveGemCostIncrease
    {
        get
        {
            return GetReviveGemCostIncrease();
        }
    }


    /// <summary>
    /// Thời gian còn lại để người chơi quyết định Revive.
    /// </summary>
    public float ReviveDecisionTimeRemaining
    {
        get
        {
            return Mathf.Max(
                0f,
                reviveDecisionTimer
            );
        }
    }


    /// <summary>
    /// Tổng thời gian cho phép quyết định Revive.
    /// </summary>
    public float ReviveDecisionDuration
    {
        get
        {
            return Mathf.Max(
                0f,
                reviveDecisionDuration
            );
        }
    }


    /// <summary>
    /// Kiểm tra toàn bộ điều kiện để Revive.
    ///
    /// Revive là VÔ HẠN theo số lần.
    ///
    /// Điều kiện:
    /// - Run đang active.
    /// - Run chưa kết thúc.
    /// - Player đang Death Pending.
    /// - Timer chưa hết.
    /// - Giá GEM hợp lệ.
    /// - Player đủ GEM.
    /// </summary>
    public bool CanRevive
    {
        get
        {
            if (!runActive)
                return false;

            int cost =
                GetReviveGemCost();

            return
                !isGameOver &&
                isDeathPending &&
                !runFinalized &&
                cost > 0 &&
                ReviveDecisionTimeRemaining > 0f &&
                CanAffordGem(cost);
        }
    }


    //=====================================================================
    // EVENTS
    //=====================================================================

    public Action<int> OnScoreChanged;

    public Action<int> OnCoinChanged;

    public Action<int> OnGoldChanged;

    public Action<int> OnGemChanged;

    // Chỉ gọi khi Run thực sự kết thúc.
    public Action OnGameOver;

    // Gọi khi Player chết nhưng Run chưa kết thúc.
    public Action OnDeathPending;

    // Gọi sau khi Revive thành công.
    public Action OnRevive;


    //=====================================================================
    // PHOTON UI STATE
    //=====================================================================

    private float photonTimeRemaining;

    private bool photonUIActive;


    public float PhotonTimeRemaining
    {
        get
        {
            return photonTimeRemaining;
        }
    }


    public bool IsPhotonUIActive
    {
        get
        {
            return photonUIActive;
        }
    }


    //=====================================================================
    // UNITY
    //=====================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;

        DontDestroyOnLoad(gameObject);


        SceneManager.sceneLoaded += HandleSceneLoaded;

        SceneManager.sceneUnloaded += HandleSceneUnloaded;


        // GameManager có thể được tạo ngay tại MainMenu.
        // Vì vậy KHÔNG được tự động bắt đầu Gameplay ở Awake.
        runActive = false;

        playerTransform = null;

        ResetRunData();


        Debug.Log(
            "[GameManager] Persistent instance initialized."
        );
    }


    private void Start()
    {
        Time.timeScale = 1f;

        Scene activeScene =
            SceneManager.GetActiveScene();


        if (activeScene.name != GameplaySceneName)
        {
            StopRuntimeRunWithoutSaving();
        }


#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            "[GameManager] Start | " +
            $"Scene = {activeScene.name} | " +
            $"RunActive = {runActive}"
        );
#endif
    }


    private void OnDestroy()
    {
        if (Instance != this)
            return;


        SceneManager.sceneLoaded -= HandleSceneLoaded;

        SceneManager.sceneUnloaded -= HandleSceneUnloaded;


        Instance = null;
    }


    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        UpdateReviveDebugCheat();
#endif

        //=============================================================
        // CRITICAL:
        //
        // GameManager là Persistent.
        // Nhưng RUN chỉ tồn tại trong Gameplay.
        //
        // Vì vậy MainMenu / Garage / Shop / Inventory:
        // Update() thoát ngay tại đây.
        //=============================================================

        if (!runActive)
            return;


        if (isGameOver)
            return;


        //=============================================================
        // SAFETY:
        // Nếu somehow scene đã rời Gameplay nhưng runActive
        // chưa được reset bởi lifecycle callback.
        //=============================================================

        Scene activeScene =
            SceneManager.GetActiveScene();


        if (activeScene.name != GameplaySceneName)
        {
            StopRuntimeRunWithoutSaving();
            return;
        }


        //=============================================================
        // PLAYER SAFETY
        //=============================================================

        if (playerTransform == null)
        {
            // Không tự Finalize Game Over.
            //
            // Có thể Player đang trong quá trình:
            // - Scene unload
            // - Respawn
            // - Revive
            //
            // Chỉ dừng việc update score.
            return;
        }


        //=============================================================
        // DEATH PENDING
        //=============================================================

        if (isDeathPending)
        {
            UpdateDeathPendingTimer();

            return;
        }


        //=============================================================
        // NORMAL RUN
        //=============================================================

        UpdateScore();
    }


    //=====================================================================
    // SCENE LIFECYCLE
    //=====================================================================

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode
    )
    {
        if (scene.name == GameplaySceneName)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(
                "[GameManager] GameplayScene loaded. " +
                "Waiting for GameManagerPlayerBinder.BeginNewRun()."
            );
#endif

            return;
        }


        //=============================================================
        // MAIN MENU / GARAGE / SHOP / OTHER SCENES
        //=============================================================

        StopRuntimeRunWithoutSaving();


#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            "[GameManager] Scene loaded: " +
            scene.name +
            " → Gameplay Run disabled."
        );
#endif
    }


    private void HandleSceneUnloaded(
        Scene scene
    )
    {
        if (scene.name != GameplaySceneName)
            return;


        //=============================================================
        // Gameplay vừa bị unload.
        //
        // Không Save Game Over ở đây.
        // Đây chỉ là cleanup lifecycle.
        //=============================================================

        StopRuntimeRunWithoutSaving();


#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            "[GameManager] GameplayScene unloaded → " +
            "Runtime Run cleared."
        );
#endif
    }


    //=====================================================================
    // REVIVE COST
    //=====================================================================

    /// <summary>
    /// Giá GEM cơ bản của Revive #1.
    /// </summary>
    private int GetReviveBaseGemCost()
    {
        return Mathf.Max(
            0,
            reviveBaseGemCost
        );
    }


    /// <summary>
    /// Mức tăng giá sau mỗi lần Revive.
    /// </summary>
    private int GetReviveGemCostIncrease()
    {
        return Mathf.Max(
            0,
            reviveGemCostIncrease
        );
    }


    /// <summary>
    /// Tính giá của lần Revive tiếp theo.
    ///
    /// Cost =
    /// BaseCost +
    /// (ReviveCount × CostIncrease)
    ///
    /// Ví dụ:
    ///
    /// Count = 0 -> 10
    /// Count = 1 -> 20
    /// Count = 2 -> 30
    /// Count = 3 -> 40
    /// </summary>
    private int GetReviveGemCost()
    {
        int baseCost =
            GetReviveBaseGemCost();

        int increase =
            GetReviveGemCostIncrease();


        long calculatedCost =
            (long)baseCost +
            ((long)reviveCount * increase);


        if (calculatedCost <= 0L)
            return 0;


        if (calculatedCost >= int.MaxValue)
            return int.MaxValue;


        return (int)calculatedCost;
    }


    //=====================================================================
    // REVIVE DECISION TIMER
    //=====================================================================

    private void UpdateDeathPendingTimer()
    {
        if (!runActive)
            return;

        if (!isDeathPending)
            return;

        if (runFinalized)
            return;


        if (reviveDecisionTimer <= 0f)
        {
            reviveDecisionTimer = 0f;

            FinalizeGameOver();

            return;
        }


        reviveDecisionTimer -=
            Time.unscaledDeltaTime;


        if (reviveDecisionTimer > 0f)
            return;


        reviveDecisionTimer = 0f;

        FinalizeGameOver();
    }


    //=====================================================================
    // SCORE
    //=====================================================================

    private void UpdateScore()
    {
        if (!runActive)
            return;

        if (playerTransform == null)
            return;


        int previousScore =
            Mathf.FloorToInt(score);


        score =
            playerTransform.position.z;


        int currentScore =
            Mathf.FloorToInt(score);


        if (currentScore == previousScore)
            return;


        OnScoreChanged?.Invoke(
            currentScore
        );
    }


    //=====================================================================
    // COIN
    //=====================================================================

    public void AddCoin(
        int amount
    )
    {
        if (!runActive)
            return;

        if (isGameOver)
            return;

        if (isDeathPending)
            return;

        if (amount <= 0)
            return;


        coinCount += amount;


        OnCoinChanged?.Invoke(
            coinCount
        );
    }


    //=====================================================================
    // PHOTON
    //=====================================================================

    public void UpdatePhotonTimerUI(
        float timeLeft
    )
    {
        if (!runActive)
        {
            photonTimeRemaining = 0f;
            photonUIActive = false;
            return;
        }


        photonTimeRemaining =
            Mathf.Max(
                0f,
                timeLeft
            );

        photonUIActive =
            photonTimeRemaining > 0f;
    }


    public void HidePhotonStatusUI()
    {
        photonTimeRemaining = 0f;

        photonUIActive = false;
    }


    //=====================================================================
    // PLAYER BINDING
    //=====================================================================

    /// <summary>
    /// Bind Player runtime vào GameManager.
    ///
    /// Chỉ Gameplay Run mới được phép bind.
    /// </summary>
    public void SetPlayerTransform(
        Transform target
    )
    {
        if (!runActive)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(
                "[GameManager] SetPlayerTransform ignored. " +
                "Gameplay Run chưa active."
            );
#endif
            return;
        }


        playerTransform = target;


#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (target != null)
        {
            Debug.Log(
                "[GameManager] Player bound → " +
                target.gameObject.name
            );
        }
#endif
    }


    /// <summary>
    /// Xóa Player runtime reference.
    /// </summary>
    public void ClearPlayerReference()
    {
        playerTransform = null;


#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            "[GameManager] Player reference cleared."
        );
#endif
    }


    //=====================================================================
    // DEATH PENDING
    //=====================================================================

    /// <summary>
    /// Đánh dấu Player đã chết nhưng Run chưa kết thúc.
    ///
    /// Không lưu Best Score.
    /// Không lưu Run Coin.
    /// Không gọi OnGameOver.
    /// </summary>
    public bool BeginDeathPending()
    {
        if (!runActive)
            return false;

        if (isGameOver)
            return false;

        if (runFinalized)
            return false;

        if (isDeathPending)
            return false;


        isDeathPending = true;


        reviveDecisionTimer =
            Mathf.Max(
                0f,
                reviveDecisionDuration
            );


        OnDeathPending?.Invoke();


        Debug.Log(
            "[GameManager] PLAYER DEATH PENDING | " +
            $"Score = {ScoreInt} | " +
            $"Run Coins = {coinCount} | " +
            $"ReviveCount = {reviveCount} | " +
            $"NextCost = {GetReviveGemCost()} GEM | " +
            $"CanRevive = {CanRevive} | " +
            $"Timer = {reviveDecisionTimer:0.00}s"
        );


        if (reviveDecisionTimer <= 0f)
        {
            FinalizeGameOver();
        }


        return true;
    }


    //=====================================================================
    // TRY REVIVE
    //=====================================================================

    public bool TryRevive()
    {
        if (!runActive)
            return false;

        if (isGameOver)
            return false;

        if (!isDeathPending)
            return false;

        if (runFinalized)
            return false;


        int cost =
            GetReviveGemCost();


        if (cost <= 0)
        {
            Debug.Log(
                "[GameManager] REVIVE FAILED | " +
                "Invalid Revive GEM cost."
            );

            return false;
        }


        if (reviveDecisionTimer <= 0f)
        {
            reviveDecisionTimer = 0f;

            FinalizeGameOver();


            Debug.Log(
                "[GameManager] REVIVE FAILED | " +
                "Revive timer expired."
            );


            return false;
        }


        int currentGem =
            GetGem();


        if (currentGem < cost)
        {
            Debug.Log(
                "[GameManager] REVIVE FAILED | " +
                $"Required = {cost} | " +
                $"Current = {currentGem} | " +
                $"ReviveCount = {reviveCount}"
            );

            return false;
        }


        bool spent =
            SpendGem(cost);


        if (!spent)
        {
            Debug.LogWarning(
                "[GameManager] REVIVE FAILED | " +
                "SpendGem returned false."
            );

            return false;
        }


        reviveCount++;


        isDeathPending = false;

        reviveDecisionTimer = 0f;


        OnRevive?.Invoke();


        Debug.Log(
            "[GameManager] REVIVE SUCCESS | " +
            $"Revive #{reviveCount} | " +
            $"Cost = {cost} GEM | " +
            $"Remaining GEM = {GetGem()} | " +
            $"Next Revive Cost = {GetReviveGemCost()} GEM | " +
            $"Score preserved = {ScoreInt} | " +
            $"Run Coins preserved = {coinCount}"
        );


        return true;
    }


    //=====================================================================
    // FINAL GAME OVER
    //=====================================================================

    /// <summary>
    /// Kết thúc Run hoàn toàn.
    ///
    /// Đây là nơi duy nhất:
    /// - khóa Run
    /// - lưu Best Score
    /// - lưu Run Coin thành Gold
    /// - phát OnGameOver
    ///
    /// Không được gọi khi chỉ rời Gameplay về MainMenu.
    /// </summary>
    public void FinalizeGameOver()
    {
        if (!runActive)
            return;

        if (runFinalized)
            return;


        runFinalized = true;

        isDeathPending = false;

        reviveDecisionTimer = 0f;

        isGameOver = true;


        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (
            firestore != null &&
            firestore.HasPlayerData &&
            firestore.CurrentPlayerData != null
        )
        {
            int runScore =
                Mathf.Max(
                    0,
                    ScoreInt
                );


            _ = SaveBestScoreAsync(
                firestore,
                runScore
            );


            if (coinCount > 0)
            {
                _ = SaveRunCoinsAsync(
                    firestore,
                    coinCount
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[GameManager] Không có PlayerData Firestore " +
                "khi Final Game Over. " +
                "Không thể lưu High Score/Coin."
            );
        }


        OnGoldChanged?.Invoke(
            GetGold()
        );

        OnGemChanged?.Invoke(
            GetGem()
        );


        OnGameOver?.Invoke();


        Debug.Log(
            "[GameManager] FINAL GAME OVER | " +
            $"Score = {ScoreInt} | " +
            $"Run Coins = {coinCount} | " +
            $"Revives Used = {reviveCount}"
        );
    }


    //=====================================================================
    // LEGACY GAME OVER API
    //=====================================================================

    public void GameOver()
    {
        FinalizeGameOver();
    }


    //=====================================================================
    // SAVE BEST SCORE
    //=====================================================================

    private async Task SaveBestScoreAsync(
        FirestorePlayerDataManager firestore,
        int runScore
    )
    {
        if (firestore == null)
            return;

        if (!firestore.HasPlayerData)
            return;

        if (firestore.CurrentPlayerData == null)
            return;


        int previousBestScore =
            Mathf.Max(
                0,
                firestore.CurrentPlayerData.bestScore
            );


        bool isNewRecord =
            runScore > previousBestScore;


        bool success =
            await firestore.TrySetBestScoreAsync(
                runScore
            );


        if (!success)
        {
            Debug.LogError(
                "[GameManager] " +
                "Không thể lưu Best Score."
            );

            return;
        }


        int currentBestScore =
            firestore.CurrentPlayerData != null
                ? firestore.CurrentPlayerData.bestScore
                : 0;


        Debug.Log(
            "[GameManager] " +
            $"Best Score Firestore = {currentBestScore}"
        );


        if (!isNewRecord)
            return;


        bool leaderboardSuccess =
            await firestore.SaveLeaderboardBestScoreAsync(
                currentBestScore
            );


        if (leaderboardSuccess)
        {
            Debug.Log(
                "[GameManager] NEW RECORD → " +
                $"Leaderboard updated: {currentBestScore}"
            );
        }
        else
        {
            Debug.LogError(
                "[GameManager] Best Score đã lưu nhưng " +
                "không thể cập nhật Leaderboard."
            );
        }
    }


    //=====================================================================
    // SAVE RUN COINS
    //=====================================================================

    private async Task SaveRunCoinsAsync(
        FirestorePlayerDataManager firestore,
        int runCoins
    )
    {
        if (firestore == null)
            return;

        if (runCoins <= 0)
            return;


        bool success =
            await firestore.AddCoinsAsync(
                runCoins
            );


        if (!success)
            return;


        int gold =
            firestore.CurrentPlayerData != null
                ? firestore.CurrentPlayerData.coins
                : 0;


        OnGoldChanged?.Invoke(
            gold
        );


        Debug.Log(
            "[GameManager] " +
            $"Run Coins +{runCoins} → " +
            $"Firestore Gold = {gold}"
        );
    }


    //=====================================================================
    // RESTART
    //=====================================================================

    public void RestartGame()
    {
        Time.timeScale = 1f;


        //=============================================================
        // Kết thúc runtime session hiện tại.
        //
        // KHÔNG save Game Over.
        // Restart sẽ tạo một Run mới thông qua Binder.
        //=============================================================

        StopRuntimeRunWithoutSaving();


        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }


    //=====================================================================
    // HIGH SCORE
    //=====================================================================

    public float GetHighScore()
    {
        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (firestore == null)
            return 0f;

        if (!firestore.HasPlayerData)
            return 0f;

        if (firestore.CurrentPlayerData == null)
            return 0f;


        return Mathf.Max(
            0,
            firestore.CurrentPlayerData.bestScore
        );
    }


    public int GetHighScoreInt()
    {
        return Mathf.FloorToInt(
            GetHighScore()
        );
    }


    //=====================================================================
    // GOLD
    //=====================================================================

    public int GetGold()
    {
        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (firestore == null)
            return 0;

        if (!firestore.HasPlayerData)
            return 0;

        if (firestore.CurrentPlayerData == null)
            return 0;


        return Mathf.Max(
            0,
            firestore.CurrentPlayerData.coins
        );
    }


    public void AddGold(
        int amount
    )
    {
        if (amount <= 0)
            return;


        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (
            firestore == null ||
            !firestore.HasPlayerData ||
            firestore.CurrentPlayerData == null
        )
        {
            Debug.LogWarning(
                "[GameManager] Không có PlayerData. " +
                "Không thể AddGold."
            );

            return;
        }


        _ = AddGoldAsync(
            firestore,
            amount
        );
    }


    private async Task AddGoldAsync(
        FirestorePlayerDataManager firestore,
        int amount
    )
    {
        bool success =
            await firestore.AddCoinsAsync(
                amount
            );


        if (!success)
            return;


        int gold =
            firestore.CurrentPlayerData != null
                ? firestore.CurrentPlayerData.coins
                : 0;


        OnGoldChanged?.Invoke(
            gold
        );
    }


    public bool CanAffordGold(
        int amount
    )
    {
        if (amount < 0)
            return false;


        return GetGold() >= amount;
    }


    public bool SpendGold(
        int amount
    )
    {
        if (amount <= 0)
            return false;


        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (
            firestore == null ||
            !firestore.HasPlayerData ||
            firestore.CurrentPlayerData == null
        )
        {
            Debug.LogWarning(
                "[GameManager] Không có PlayerData. " +
                "Không thể SpendGold."
            );

            return false;
        }


        int currentGold =
            firestore.CurrentPlayerData.coins;


        if (currentGold < amount)
            return false;


        int newGold =
            currentGold - amount;


        firestore.CurrentPlayerData.coins =
            newGold;


        _ = SaveSpentGoldAsync(
            firestore
        );


        OnGoldChanged?.Invoke(
            newGold
        );


        return true;
    }


    private async Task SaveSpentGoldAsync(
        FirestorePlayerDataManager firestore
    )
    {
        bool success =
            await firestore.SetCoinsAsync(
                firestore.CurrentPlayerData.coins
            );


        if (!success)
        {
            Debug.LogError(
                "[GameManager] " +
                "Không thể lưu Gold sau khi SpendGold."
            );
        }
    }


    //=====================================================================
    // GEM
    //=====================================================================

    public int GetGem()
    {
        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (firestore == null)
            return 0;

        if (!firestore.HasPlayerData)
            return 0;

        if (firestore.CurrentPlayerData == null)
            return 0;


        return Mathf.Max(
            0,
            firestore.CurrentPlayerData.gems
        );
    }


    public void AddGem(
        int amount
    )
    {
        if (amount <= 0)
            return;


        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (
            firestore == null ||
            !firestore.HasPlayerData
        )
        {
            Debug.LogWarning(
                "[GameManager] Không có PlayerData. " +
                "Không thể AddGem."
            );

            return;
        }


        _ = AddGemAsync(
            firestore,
            amount
        );
    }


    private async Task AddGemAsync(
        FirestorePlayerDataManager firestore,
        int amount
    )
    {
        bool success =
            await firestore.AddGemsAsync(
                amount
            );


        if (!success)
            return;


        int gems =
            firestore.CurrentPlayerData != null
                ? firestore.CurrentPlayerData.gems
                : 0;


        OnGemChanged?.Invoke(
            gems
        );
    }


    public bool CanAffordGem(
        int amount
    )
    {
        if (amount < 0)
            return false;


        return GetGem() >= amount;
    }


    public bool SpendGem(
        int amount
    )
    {
        if (amount <= 0)
            return false;


        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (
            firestore == null ||
            !firestore.HasPlayerData ||
            firestore.CurrentPlayerData == null
        )
        {
            Debug.LogWarning(
                "[GameManager] Không có PlayerData. " +
                "Không thể SpendGem."
            );

            return false;
        }


        int currentGem =
            firestore.CurrentPlayerData.gems;


        if (currentGem < amount)
            return false;


        int newGem =
            currentGem - amount;


        firestore.CurrentPlayerData.gems =
            newGem;


        _ = SaveSpentGemAsync(
            firestore
        );


        OnGemChanged?.Invoke(
            newGem
        );


        return true;
    }


    private async Task SaveSpentGemAsync(
        FirestorePlayerDataManager firestore
    )
    {
        bool success =
            await firestore.SetGemsAsync(
                firestore.CurrentPlayerData.gems
            );


        if (!success)
        {
            Debug.LogError(
                "[GameManager] " +
                "Không thể lưu GEM sau khi SpendGem."
            );
        }
    }


    //=====================================================================
    // RUN DATA
    //=====================================================================

    public int GetTotalCoins()
    {
        return GetGold();
    }


    /// <summary>
    /// Bắt đầu Gameplay Run mới.
    ///
    /// Đây là điểm duy nhất mở một runtime run.
    /// </summary>
    public void BeginNewRun()
    {
        Time.timeScale = 1f;


        //=============================================================
        // SAFETY
        //=============================================================

        Scene activeScene =
            SceneManager.GetActiveScene();


        if (activeScene.name != GameplaySceneName)
        {
            Debug.LogWarning(
                "[GameManager] BeginNewRun() bị gọi ngoài " +
                "GameplayScene. Request bị bỏ qua."
            );

            return;
        }


        //=============================================================
        // RESET RUN
        //=============================================================

        ResetRunData();


        //=============================================================
        // ACTIVE
        //=============================================================

        runActive = true;


        //=============================================================
        // LIFETIME RUN COUNTER
        //=============================================================

        IncrementLifetimeRun();


        Debug.Log(
            "[GameManager] NEW GAMEPLAY RUN STARTED."
        );
    }


    /// <summary>
    /// Kết thúc runtime Gameplay mà KHÔNG coi đó là Game Over.
    ///
    /// Dùng khi:
    /// - Gameplay -> MainMenu
    /// - Gameplay -> Garage
    /// - Restart Scene
    /// - Scene bị unload
    ///
    /// Không:
    /// - lưu Best Score
    /// - lưu Run Coin
    /// - gọi OnGameOver
    /// - tăng TotalRuns
    /// </summary>
    public void EndGameplayRun()
    {
        StopRuntimeRunWithoutSaving();
    }


    /// <summary>
    /// Cleanup runtime state.
    ///
    /// Đây là method nội bộ dùng cho lifecycle.
    /// </summary>
    private void StopRuntimeRunWithoutSaving()
    {
        bool wasActive =
            runActive ||
            playerTransform != null ||
            score != 0f ||
            coinCount != 0 ||
            reviveCount != 0 ||
            isDeathPending ||
            isGameOver;


        runActive = false;


        //=============================================================
        // PLAYER
        //=============================================================

        playerTransform = null;


        //=============================================================
        // RUN STATE
        //=============================================================

        score = 0f;

        coinCount = 0;

        isGameOver = false;

        isDeathPending = false;

        reviveCount = 0;

        runFinalized = false;

        reviveDecisionTimer = 0f;


        //=============================================================
        // PHOTON
        //=============================================================

        photonTimeRemaining = 0f;

        photonUIActive = false;


        //=============================================================
        // TIME
        //=============================================================

        Time.timeScale = 1f;


        //=============================================================
        // UI
        //=============================================================

        OnScoreChanged?.Invoke(0);

        OnCoinChanged?.Invoke(0);

        OnGoldChanged?.Invoke(GetGold());

        OnGemChanged?.Invoke(GetGem());


        if (!wasActive)
            return;


#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            "[GameManager] RUNTIME GAMEPLAY ENDED | " +
            "Player reference cleared | " +
            "Score reset | " +
            "Run Coins reset | " +
            "Revive state reset | " +
            "Photon UI reset."
        );
#endif
    }


    /// <summary>
    /// Reset dữ liệu Run.
    ///
    /// KHÔNG tăng TotalRuns.
    /// KHÔNG save Firestore.
    /// </summary>
    public void ResetRunData()
    {
        score = 0f;

        coinCount = 0;

        isGameOver = false;

        isDeathPending = false;

        reviveCount = 0;

        runFinalized = false;

        reviveDecisionTimer = 0f;

        photonTimeRemaining = 0f;

        photonUIActive = false;


        OnScoreChanged?.Invoke(0);

        OnCoinChanged?.Invoke(0);

        OnGoldChanged?.Invoke(
            GetGold()
        );

        OnGemChanged?.Invoke(
            GetGem()
        );
    }


    //=====================================================================
    // LIFETIME RUN COUNTER
    //=====================================================================

    private void IncrementLifetimeRun()
    {
        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (firestore == null)
        {
            Debug.LogWarning(
                "[GameManager] TotalRuns chưa được tăng: " +
                "FirestorePlayerDataManager chưa tồn tại."
            );

            return;
        }


        if (!firestore.HasPlayerData)
        {
            Debug.LogWarning(
                "[GameManager] TotalRuns chưa được tăng: " +
                "PlayerData chưa sẵn sàng."
            );

            return;
        }


        if (firestore.CurrentPlayerData == null)
        {
            Debug.LogWarning(
                "[GameManager] TotalRuns chưa được tăng: " +
                "CurrentPlayerData == null."
            );

            return;
        }


        _ = IncrementLifetimeRunAsync(
            firestore
        );
    }


    private async Task IncrementLifetimeRunAsync(
        FirestorePlayerDataManager firestore
    )
    {
        if (firestore == null)
            return;


        bool success =
            await firestore.IncrementTotalRunsAsync();


        if (!success)
        {
            Debug.LogError(
                "[GameManager] " +
                "Không thể tăng Total Runs trên Firestore."
            );

            return;
        }


        int totalRuns =
            firestore.CurrentPlayerData != null
                ? Mathf.Max(
                    0,
                    firestore.CurrentPlayerData.totalRuns
                )
                : 0;


        Debug.Log(
            "[GameManager] NEW RUN | " +
            $"Lifetime TotalRuns = {totalRuns}"
        );
    }


    //=====================================================================
    // GEM DEBUG
    //=====================================================================

    public void SaveGemDebugValue(
        int amount
    )
    {
        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (
            firestore == null ||
            !firestore.HasPlayerData ||
            firestore.CurrentPlayerData == null
        )
        {
            Debug.LogWarning(
                "[GameManager] Không có PlayerData. " +
                "Không thể Set GEM."
            );

            return;
        }


        amount =
            Mathf.Max(
                0,
                amount
            );


        _ = SaveGemDebugValueAsync(
            firestore,
            amount
        );
    }


    private async Task SaveGemDebugValueAsync(
        FirestorePlayerDataManager firestore,
        int amount
    )
    {
        bool success =
            await firestore.SetGemsAsync(
                amount
            );


        if (!success)
        {
            Debug.LogError(
                "[GameManager] " +
                "Set GEM Firestore thất bại."
            );

            return;
        }


        OnGemChanged?.Invoke(
            amount
        );


        Debug.Log(
            $"[GameManager] GEM saved successfully = {amount}"
        );
    }


    //=====================================================================
    // DEBUG REVIVE CHEAT
    //=====================================================================

#if UNITY_EDITOR || DEVELOPMENT_BUILD

    [Header("DEBUG - Revive")]
    [SerializeField]
    private bool enableReviveDebugCheat = true;

    [SerializeField]
    private KeyCode reviveDebugKey = KeyCode.R;


    private void UpdateReviveDebugCheat()
    {
        if (!enableReviveDebugCheat)
            return;


        if (!runActive)
            return;


        if (!Input.GetKeyDown(reviveDebugKey))
            return;


        //=============================================================
        // CHƯA DEATH PENDING
        //=============================================================

        if (!isDeathPending && !isGameOver)
        {
            bool deathStarted =
                BeginDeathPending();


            Debug.Log(
                "[GameManager][DEBUG] " +
                $"Death Pending = {deathStarted} | " +
                $"ReviveCount = {reviveCount} | " +
                $"NextCost = {GetReviveGemCost()} GEM | " +
                $"GEM = {GetGem()} | " +
                $"Timer = {ReviveDecisionTimeRemaining:0.00}s"
            );


            return;
        }


        //=============================================================
        // ĐANG DEATH PENDING
        //=============================================================

        if (isDeathPending)
        {
            bool revived =
                TryRevive();


            Debug.Log(
                "[GameManager][DEBUG] " +
                $"TryRevive = {revived} | " +
                $"ReviveCount = {reviveCount} | " +
                $"NextCost = {GetReviveGemCost()} GEM | " +
                $"GEM = {GetGem()} | " +
                $"Score = {ScoreInt} | " +
                $"Run Coins = {coinCount} | " +
                $"Timer = {ReviveDecisionTimeRemaining:0.00}s"
            );
        }
    }

#endif
}