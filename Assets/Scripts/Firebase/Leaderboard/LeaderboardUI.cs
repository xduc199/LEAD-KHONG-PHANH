using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance { get; private set; }

    [Header("Main")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Button closeButton;

    [Header("Header")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;

    [Header("List")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private LeaderboardRowUI rowPrefab;

    [Header("My Rank")]
    [SerializeField] private GameObject myRankPanel;
    [SerializeField] private TMP_Text myRankText;
    [SerializeField] private TMP_Text myNameText;
    [SerializeField] private TMP_Text myScoreText;

    [Header("Empty State")]
    [SerializeField] private GameObject emptyState;

    private readonly List<LeaderboardRowUI> spawnedRows =
        new List<LeaderboardRowUI>(100);

    private bool isInitialized;
    private int loadRequestId;

    public bool IsOpen
    {
        get
        {
            return leaderboardPanel != null &&
                   leaderboardPanel.activeSelf;
        }
    }

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

        Initialize();
    }

    private void OnDestroy()
    {
        loadRequestId++;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ============================================================
    // INITIALIZE
    // ============================================================

    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (titleText != null)
        {
            titleText.text = "BẢNG XẾP HẠNG";
        }

        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }

        if (statusText != null)
        {
            statusText.text = string.Empty;
        }

        if (emptyState != null)
        {
            emptyState.SetActive(false);
        }

        if (myRankPanel != null)
        {
            myRankPanel.SetActive(false);
        }

        ClearRows();
    }

    // ============================================================
    // OPEN
    // ============================================================

    public void Open()
    {
        if (leaderboardPanel == null)
        {
            Debug.LogError(
                "[LeaderboardUI] " +
                "Leaderboard Panel chưa được gán."
            );

            return;
        }

        loadRequestId++;

        int requestId = loadRequestId;

        leaderboardPanel.SetActive(true);

        SetStatus("ĐANG TẢI...");

        if (emptyState != null)
        {
            emptyState.SetActive(false);
        }

        if (myRankPanel != null)
        {
            myRankPanel.SetActive(false);
        }

        ClearRows();

        _ = LoadLeaderboardAsync(requestId);
    }

    // ============================================================
    // CLOSE
    // ============================================================

    public void Close()
    {
        loadRequestId++;

        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }

        ClearRows();
    }

    // ============================================================
    // LOAD
    // ============================================================

    private async Task LoadLeaderboardAsync(
        int requestId)
    {
        LeaderboardManager manager =
            LeaderboardManager.Instance;

        if (manager == null)
        {
            SetStatus(
                "KHÔNG TÌM THẤY LEADERBOARD MANAGER"
            );

            Debug.LogError(
                "[LeaderboardUI] " +
                "Không thể tìm thấy LeaderboardManager."
            );

            return;
        }

        bool success =
            await manager.LoadLeaderboardAsync();

        if (this == null)
        {
            return;
        }

        if (requestId != loadRequestId)
        {
            return;
        }

        if (!IsOpen)
        {
            return;
        }

        if (!success)
        {
            FirebaseUser currentUser = null;

            try
            {
                currentUser =
                    FirebaseAuth
                        .DefaultInstance
                        .CurrentUser;
            }
            catch
            {
                currentUser = null;
            }

            if (currentUser == null)
            {
                SetStatus(
                    "VUI LÒNG ĐĂNG NHẬP"
                );
            }
            else
            {
                SetStatus(
                    "KHÔNG THỂ TẢI BẢNG XẾP HẠNG"
                );
            }

            return;
        }

        IReadOnlyList<LeaderboardEntry> entries =
            manager.Entries;

        Debug.Log(
            "[LeaderboardUI] " +
            "Leaderboard data received. Count = " +
            (entries != null ? entries.Count : 0)
        );

        RenderLeaderboard(entries);
    }

    // ============================================================
    // RENDER LEADERBOARD
    // ============================================================

    private void RenderLeaderboard(
        IReadOnlyList<LeaderboardEntry> entries)
    {
        ClearRows();

        if (entries == null ||
            entries.Count == 0)
        {
            SetStatus("CHƯA CÓ KỶ LỤC");

            if (emptyState != null)
            {
                emptyState.SetActive(true);
            }

            if (myRankPanel != null)
            {
                myRankPanel.SetActive(false);
            }

            return;
        }

        SetStatus(string.Empty);

        if (emptyState != null)
        {
            emptyState.SetActive(false);
        }

        // --------------------------------------------------------
        // VALIDATE PREFAB
        // --------------------------------------------------------

        if (rowPrefab == null)
        {
            SetStatus(
                "THIẾU LEADERBOARD ROW PREFAB"
            );

            Debug.LogError(
                "[LeaderboardUI] " +
                "rowPrefab == NULL."
            );

            return;
        }

        // --------------------------------------------------------
        // VALIDATE CONTENT
        // --------------------------------------------------------

        if (contentRoot == null)
        {
            SetStatus(
                "THIẾU CONTENT ROOT"
            );

            Debug.LogError(
                "[LeaderboardUI] " +
                "contentRoot == NULL."
            );

            return;
        }

        Debug.Log(
            "[LeaderboardUI] " +
            "rowPrefab = " +
            rowPrefab.name
        );

        Debug.Log(
            "[LeaderboardUI] " +
            "contentRoot = " +
            contentRoot.name
        );

        RectTransform contentRect =
            contentRoot as RectTransform;

        if (contentRect == null)
        {
            Debug.LogError(
                "[LeaderboardUI] " +
                "contentRoot không phải RectTransform."
            );

            SetStatus(
                "CONTENT ROOT KHÔNG HỢP LỆ"
            );

            return;
        }

        // --------------------------------------------------------
        // ENSURE CONTENT ACTIVE
        // --------------------------------------------------------

        if (!contentRoot.gameObject.activeSelf)
        {
            contentRoot.gameObject.SetActive(true);
        }

        int count =
            entries.Count;

        // --------------------------------------------------------
        // CREATE ROWS
        // --------------------------------------------------------

        for (int i = 0; i < count; i++)
        {
            LeaderboardEntry entry =
                entries[i];

            if (entry == null)
            {
                continue;
            }

            LeaderboardRowUI row =
                Instantiate(
                    rowPrefab,
                    contentRoot,
                    false
                );

            if (row == null)
            {
                Debug.LogError(
                    "[LeaderboardUI] " +
                    "Instantiate LeaderboardRow thất bại."
                );

                continue;
            }

            GameObject rowObject =
                row.gameObject;

            rowObject.name =
                "LeaderboardRow_" +
                (i + 1);

            rowObject.SetActive(true);

            RectTransform rowRect =
                rowObject.GetComponent<RectTransform>();

            if (rowRect != null)
            {
                rowRect.localScale =
                    Vector3.one;

                rowRect.localRotation =
                    Quaternion.identity;

                rowRect.anchoredPosition3D =
                    Vector3.zero;
            }

            row.SetData(
                i + 1,
                entry
            );

            spawnedRows.Add(row);
        }

        // --------------------------------------------------------
        // FORCE UI LAYOUT
        // --------------------------------------------------------

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            contentRect
        );

        Canvas.ForceUpdateCanvases();

        Debug.Log(
            "[LeaderboardUI] " +
            "Rendered " +
            spawnedRows.Count +
            "/" +
            entries.Count +
            " leaderboard rows."
        );

        Debug.Log(
            "[LeaderboardUI] " +
            "ContentRoot size = " +
            contentRect.rect.size
        );

        RenderMyRank(entries);
    }

    // ============================================================
    // MY RANK
    // ============================================================

    private void RenderMyRank(
        IReadOnlyList<LeaderboardEntry> entries)
    {
        FirebaseUser currentUser = null;

        try
        {
            currentUser =
                FirebaseAuth
                    .DefaultInstance
                    .CurrentUser;
        }
        catch
        {
            currentUser = null;
        }

        if (currentUser == null)
        {
            HideMyRank();
            return;
        }

        string currentUid =
            currentUser.UserId;

        if (string.IsNullOrEmpty(currentUid))
        {
            HideMyRank();
            return;
        }

        int myRank = -1;

        LeaderboardEntry myEntry = null;

        int count =
            entries.Count;

        for (int i = 0; i < count; i++)
        {
            LeaderboardEntry entry =
                entries[i];

            if (entry == null)
            {
                continue;
            }

            if (entry.uid == currentUid)
            {
                myRank =
                    i + 1;

                myEntry =
                    entry;

                break;
            }
        }

        if (myRank < 0 ||
            myEntry == null)
        {
            HideMyRank();
            return;
        }

        if (myRankPanel != null)
        {
            myRankPanel.SetActive(true);
        }

        if (myRankText != null)
        {
            myRankText.text =
                "#" + myRank;
        }

        if (myNameText != null)
        {
            myNameText.text =
                string.IsNullOrEmpty(
                    myEntry.displayName
                )
                    ? "Player"
                    : myEntry.displayName;
        }

        if (myScoreText != null)
        {
            myScoreText.text =
                myEntry.bestScore.ToString("N0");
        }
    }

    // ============================================================
    // HIDE MY RANK
    // ============================================================

    private void HideMyRank()
    {
        if (myRankPanel != null)
        {
            myRankPanel.SetActive(false);
        }
    }

    // ============================================================
    // CLEAR ROWS
    // ============================================================

    private void ClearRows()
    {
        int count =
            spawnedRows.Count;

        for (int i = 0; i < count; i++)
        {
            LeaderboardRowUI row =
                spawnedRows[i];

            if (row != null)
            {
                Destroy(row.gameObject);
            }
        }

        spawnedRows.Clear();
    }

    // ============================================================
    // STATUS
    // ============================================================

    private void SetStatus(
        string message)
    {
        if (statusText != null)
        {
            statusText.text =
                message;
        }
    }
}