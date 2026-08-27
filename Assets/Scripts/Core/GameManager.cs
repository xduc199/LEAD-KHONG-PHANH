using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Reference")]
    [SerializeField] private GameObject scoreTextObject;
    [SerializeField] private GameObject coinTextObject;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject finalScoreTextObject;
    [SerializeField] private GameObject photonStatusTextObject; // Kéo Text hiển thị trạng thái Photon vào đây

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;

    private float score = 0f;
    private int coinCount = 0;
    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (photonStatusTextObject != null) photonStatusTextObject.SetActive(false);
        UpdateCoinUI();
    }

    private void Update()
    {
        if (isGameOver) return;

        if (playerTransform != null)
        {
            score = playerTransform.position.z;
            SetText(scoreTextObject, "SCORE: " + Mathf.FloorToInt(score).ToString());
        }
    }

    public void AddCoin(int amount)
    {
        coinCount += amount;
        UpdateCoinUI();
    }

    private void UpdateCoinUI()
    {
        SetText(coinTextObject, "COINS: " + coinCount.ToString());
    }

    // Cập nhật thời gian đếm ngược Photon ra UI
    public void UpdatePhotonTimerUI(float timeLeft)
    {
        if (photonStatusTextObject != null)
        {
            photonStatusTextObject.SetActive(true);
            SetText(photonStatusTextObject, "⚡ TỐC ĐỘ ÁNH SÁNG: " + Mathf.CeilToInt(timeLeft) + "s");
        }
    }

    // Ẩn UI Photon khi hết giờ
    public void HidePhotonStatusUI()
    {
        if (photonStatusTextObject != null)
        {
            photonStatusTextObject.SetActive(false);
        }
    }

    public void GameOver()
    {
        isGameOver = true;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        // --- 1. XỬ LÝ LƯU KỶ LỤC ĐIỂM (HIGH SCORE) ---
        float highScore = PlayerPrefs.GetFloat("HighScore", 0f);
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetFloat("HighScore", highScore);
        }

        // --- 2. XỬ LÝ CỘNG DỒN TỔNG VÀNG (TOTAL COINS) ---
        int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        totalCoins += coinCount;
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        
        // Lưu toàn bộ dữ liệu lại
        PlayerPrefs.Save(); 

        // --- 3. HIỂN THỊ RA MÀN HÌNH ---
        string finalMessage = 
            "SCORE: " + Mathf.FloorToInt(score).ToString() + "\n" +
            "HIGH SCORE: " + Mathf.FloorToInt(highScore).ToString() + "\n\n" +
            "COINS (RUN): " + coinCount.ToString() + "\n" +
            "TOTAL COINS: " + totalCoins.ToString();

        SetText(finalScoreTextObject, finalMessage);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void SetText(GameObject obj, string message)
    {
        if (obj == null) return;
        var tmpText = obj.GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.text = message;
            return;
        }
        var legacyText = obj.GetComponent<Text>();
        if (legacyText != null)
        {
            legacyText.text = message;
        }
    }
    
}