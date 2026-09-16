using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //==============================================================
    // SINGLETON
    //==============================================================

    public static GameManager Instance { get; private set; }


    //==============================================================
    // PLAYER
    //==============================================================

    [Header("Player")]
    [SerializeField]
    private Transform playerTransform;


    //==============================================================
    // GAME DATA
    //==============================================================

    private float score;
    private int coinCount;
    private bool isGameOver;


    //==============================================================
    // PUBLIC DATA
    //==============================================================

    public float Score => score;

    public int ScoreInt =>
        Mathf.FloorToInt(score);

    /// <summary>
    /// Coin nhặt được trong RUN hiện tại.
    /// Không phải Gold tài khoản.
    /// </summary>
    public int CoinCount => coinCount;

    public bool IsGameOver => isGameOver;


    //==============================================================
    // EVENTS
    //==============================================================

    public Action<int> OnScoreChanged;

    public Action<int> OnCoinChanged;

    /// <summary>
    /// Gold tài khoản hiện tại.
    /// </summary>
    public Action<int> OnGoldChanged;

    public Action OnGameOver;


    //==============================================================
    // PHOTON COMPATIBILITY
    //==============================================================

    private float photonTimeRemaining;
    private bool photonUIActive;


    //==============================================================
    // AWAKE
    //==============================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    //==============================================================
    // START
    //==============================================================

    private void Start()
    {
        Time.timeScale = 1f;

        score = 0f;
        coinCount = 0;
        isGameOver = false;

        photonTimeRemaining = 0f;
        photonUIActive = false;

        if (playerTransform != null)
        {
            score = playerTransform.position.z;
        }

        OnScoreChanged?.Invoke(ScoreInt);
        OnCoinChanged?.Invoke(coinCount);
        OnGoldChanged?.Invoke(GetGold());
    }


    //==============================================================
    // UPDATE
    //==============================================================

    private void Update()
    {
        if (isGameOver)
            return;

        UpdateScore();
    }


    //==============================================================
    // SCORE
    //==============================================================

    private void UpdateScore()
    {
        if (playerTransform == null)
            return;

        int previousScore =
            Mathf.FloorToInt(score);

        score =
            playerTransform.position.z;

        int currentScore =
            Mathf.FloorToInt(score);

        if (currentScore != previousScore)
        {
            OnScoreChanged?.Invoke(currentScore);
        }
    }


    //==============================================================
    // ADD COIN IN RUN
    //==============================================================

    public void AddCoin(int amount)
    {
        if (isGameOver)
            return;

        if (amount <= 0)
            return;

        coinCount += amount;

        OnCoinChanged?.Invoke(coinCount);
    }


    //==============================================================
    // PHOTON TIMER UI
    //==============================================================

    public void UpdatePhotonTimerUI(float timeLeft)
    {
        photonTimeRemaining =
            Mathf.Max(0f, timeLeft);

        photonUIActive =
            photonTimeRemaining > 0f;
    }


    //==============================================================
    // HIDE PHOTON STATUS UI
    //==============================================================

    public void HidePhotonStatusUI()
    {
        photonTimeRemaining = 0f;
        photonUIActive = false;
    }


    //==============================================================
    // PHOTON DATA
    //==============================================================

    public float PhotonTimeRemaining =>
        photonTimeRemaining;

    public bool IsPhotonUIActive =>
        photonUIActive;


    //==============================================================
    // GAME OVER
    //==============================================================

    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;


        //==========================================================
        // CURRENT ACCOUNT DATA
        //==========================================================

        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (firestore != null &&
            firestore.HasPlayerData &&
            firestore.CurrentPlayerData != null)
        {
            int runScore =
                Mathf.Max(
                    0,
                    ScoreInt
                );


            //======================================================
            // HIGH SCORE → FIRESTORE
            //======================================================

            _ = SaveBestScoreAsync(
                firestore,
                runScore
            );


            //======================================================
            // RUN COIN → ACCOUNT GOLD
            //======================================================

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
                "khi Game Over. Không thể lưu High Score/Coin."
            );
        }


        //==========================================================
        // GOLD EVENT
        //==========================================================

        OnGoldChanged?.Invoke(
            GetGold()
        );


        //==========================================================
        // GAME OVER EVENT
        //==========================================================

        OnGameOver?.Invoke();
    }


    //==============================================================
    // SAVE BEST SCORE + LEADERBOARD
    //==============================================================

    private async Task SaveBestScoreAsync(
        FirestorePlayerDataManager firestore,
        int runScore)
    {
        if (firestore == null)
            return;

        if (!firestore.HasPlayerData ||
            firestore.CurrentPlayerData == null)
        {
            return;
        }


        //==========================================================
        // CAPTURE BEST SCORE BEFORE MODIFYING PLAYER DATA
        //==========================================================

        int previousBestScore =
            Mathf.Max(
                0,
                firestore.CurrentPlayerData.bestScore
            );


        //==========================================================
        // CHECK NEW RECORD
        //==========================================================

        bool isNewRecord =
            runScore > previousBestScore;


        //==========================================================
        // SAVE PLAYER BEST SCORE
        //==========================================================

        bool success =
            await firestore.TrySetBestScoreAsync(
                runScore
            );


        if (!success)
        {
            Debug.LogError(
                "[GameManager] Không thể lưu Best Score."
            );

            return;
        }


        //==========================================================
        // LOG CURRENT BEST
        //==========================================================

        int currentBestScore =
            firestore.CurrentPlayerData != null
                ? firestore.CurrentPlayerData.bestScore
                : 0;


        Debug.Log(
            "[GameManager] " +
            $"Best Score Firestore = {currentBestScore}"
        );


        //==========================================================
        // LEADERBOARD
        //==========================================================

        if (!isNewRecord)
        {
            return;
        }


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


    //==============================================================
    // SAVE RUN COINS
    //==============================================================

    private async Task SaveRunCoinsAsync(
        FirestorePlayerDataManager firestore,
        int runCoins)
    {
        if (firestore == null)
            return;

        bool success =
            await firestore.AddCoinsAsync(
                runCoins
            );

        if (success)
        {
            int gold =
                firestore.CurrentPlayerData != null
                    ? firestore.CurrentPlayerData.coins
                    : 0;

            OnGoldChanged?.Invoke(gold);

            Debug.Log(
                "[GameManager] " +
                $"Run Coins +{runCoins} → " +
                $"Firestore Gold = {gold}"
            );
        }
    }


    //==============================================================
    // RESTART
    //==============================================================

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }


    //==============================================================
    // HIGH SCORE
    //==============================================================

    public float GetHighScore()
    {
        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;

        if (firestore != null &&
            firestore.HasPlayerData &&
            firestore.CurrentPlayerData != null)
        {
            return Mathf.Max(
                0,
                firestore.CurrentPlayerData.bestScore
            );
        }

        return 0f;
    }


    public int GetHighScoreInt()
    {
        return Mathf.FloorToInt(
            GetHighScore()
        );
    }


    //==============================================================
    // GOLD
    //==============================================================

    /// <summary>
    /// Lấy Gold của tài khoản Firebase hiện tại.
    /// </summary>
    public int GetGold()
    {
        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;

        if (firestore != null &&
            firestore.HasPlayerData &&
            firestore.CurrentPlayerData != null)
        {
            return Mathf.Max(
                0,
                firestore.CurrentPlayerData.coins
            );
        }

        return 0;
    }


    //==============================================================
    // ADD GOLD
    //==============================================================

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;

        if (firestore == null ||
            !firestore.HasPlayerData)
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
        int amount)
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

        OnGoldChanged?.Invoke(gold);
    }


    //==============================================================
    // CAN AFFORD GOLD
    //==============================================================

    public bool CanAffordGold(int amount)
    {
        if (amount < 0)
            return false;

        return GetGold() >= amount;
    }


    //==============================================================
    // SPEND GOLD
    //==============================================================

    public bool SpendGold(int amount)
    {
        if (amount <= 0)
            return false;

        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;

        if (firestore == null ||
            !firestore.HasPlayerData ||
            firestore.CurrentPlayerData == null)
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
        FirestorePlayerDataManager firestore)
    {
        bool success =
            await firestore.SetCoinsAsync(
                firestore.CurrentPlayerData.coins
            );

        if (!success)
        {
            Debug.LogError(
                "[GameManager] Không thể lưu Gold sau khi SpendGold."
            );
        }
    }


    //==============================================================
    // API TƯƠNG THÍCH
    //==============================================================

    public int GetTotalCoins()
    {
        return GetGold();
    }


    //==============================================================
    // PLAYER
    //==============================================================

    public void SetPlayerTransform(
        Transform target
    )
    {
        playerTransform = target;
    }


    //==============================================================
    // BEGIN NEW RUN
    //==============================================================

    public void BeginNewRun()
    {
        Time.timeScale = 1f;

        ResetRunData();
    }


    //==============================================================
    // RESET RUN
    //==============================================================

    public void ResetRunData()
    {
        score = 0f;
        coinCount = 0;
        isGameOver = false;

        photonTimeRemaining = 0f;
        photonUIActive = false;

        OnScoreChanged?.Invoke(0);
        OnCoinChanged?.Invoke(0);
        OnGoldChanged?.Invoke(GetGold());
    }
}