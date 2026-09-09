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

    public int CoinCount => coinCount;

    public bool IsGameOver => isGameOver;


    //==============================================================
    // EVENTS
    //==============================================================

    /// <summary>
    /// Gọi khi Score thay đổi sang số nguyên mới.
    /// </summary>
    public System.Action<int> OnScoreChanged;


    /// <summary>
    /// Gọi khi Coin thay đổi.
    /// </summary>
    public System.Action<int> OnCoinChanged;


    /// <summary>
    /// Gọi một lần khi Game Over.
    /// </summary>
    public System.Action OnGameOver;


    //==============================================================
    // PHOTON COMPATIBILITY
    //==============================================================

    /*
     * PhotonController cũ vẫn gọi:
     *
     * GameManager.Instance.UpdatePhotonTimerUI(...)
     * GameManager.Instance.HidePhotonStatusUI()
     *
     * Không xóa 2 API này vì sẽ làm PhotonController lỗi compile.
     *
     * UI Photon mới sẽ được điều khiển bởi hệ thống HUD riêng.
     * Vì vậy 2 hàm này hiện không còn điều khiển UI cũ.
     */

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

        /*
         * Chỉ gửi event khi Score thực sự
         * chuyển sang một số nguyên mới.
         */

        if (currentScore != previousScore)
        {
            OnScoreChanged?.Invoke(currentScore);
        }
    }


    //==============================================================
    // ADD COIN
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
    // COMPATIBILITY API
    //==============================================================

    public void UpdatePhotonTimerUI(float timeLeft)
    {
        /*
         * Giữ API để PhotonController cũ không lỗi.
         *
         * Không còn cập nhật Text UI cũ.
         *
         * HUD Photon mới sẽ được nối riêng với
         * PhotonController ở bước tiếp theo.
         */

        photonTimeRemaining =
            Mathf.Max(0f, timeLeft);

        photonUIActive =
            photonTimeRemaining > 0f;
    }


    //==============================================================
    // HIDE PHOTON STATUS UI
    // COMPATIBILITY API
    //==============================================================

    public void HidePhotonStatusUI()
    {
        /*
         * Giữ API tương thích với PhotonController.
         *
         * Không thao tác UI cũ.
         */

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
        // HIGH SCORE
        //==========================================================

        float highScore =
            PlayerPrefs.GetFloat(
                "HighScore",
                0f
            );

        if (score > highScore)
        {
            highScore = score;

            PlayerPrefs.SetFloat(
                "HighScore",
                highScore
            );
        }


        //==========================================================
        // TOTAL COINS
        //==========================================================

        int totalCoins =
            PlayerPrefs.GetInt(
                "TotalCoins",
                0
            );

        totalCoins += coinCount;

        PlayerPrefs.SetInt(
            "TotalCoins",
            totalCoins
        );


        //==========================================================
        // SAVE
        //==========================================================

        PlayerPrefs.Save();


        //==========================================================
        // GAME OVER EVENT
        //==========================================================

        OnGameOver?.Invoke();
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
        return PlayerPrefs.GetFloat(
            "HighScore",
            0f
        );
    }


    public int GetHighScoreInt()
    {
        return Mathf.FloorToInt(
            GetHighScore()
        );
    }


    //==============================================================
    // TOTAL COINS
    //==============================================================

    public int GetTotalCoins()
    {
        return PlayerPrefs.GetInt(
            "TotalCoins",
            0
        );
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
    // OPTIONAL RESET
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
    }
}