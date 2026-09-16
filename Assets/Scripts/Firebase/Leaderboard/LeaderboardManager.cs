using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private const string LeaderboardCollection = "leaderboard";
    private const int DefaultLimit = 100;

    [Header("Leaderboard")]
    [SerializeField, Min(1)]
    private int maxEntries = DefaultLimit;

    [Header("Auth Wait")]
    [SerializeField, Min(1)]
    private int authWaitAttempts = 100;

    [SerializeField, Min(10)]
    private int authWaitDelayMilliseconds = 100;

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    private readonly List<LeaderboardEntry> entries =
        new List<LeaderboardEntry>(DefaultLimit);

    private bool isLoading;
    private bool hasLoaded;

    public IReadOnlyList<LeaderboardEntry> Entries => entries;
    public bool IsLoading => isLoading;
    public bool HasLoaded => hasLoaded;
    public int EntryCount => entries.Count;

    public event Action OnLeaderboardLoaded;
    public event Action<string> OnLeaderboardLoadFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
    }

    // ============================================================
    // LOAD LEADERBOARD
    // ============================================================

    public async Task<bool> LoadLeaderboardAsync()
    {
        if (isLoading)
        {
            return false;
        }

        isLoading = true;

        try
        {
            InitializeFirebase();

            // ----------------------------------------------------
            // WAIT FOR FIREBASE AUTH SESSION
            // ----------------------------------------------------

            bool authReady =
                await WaitForAuthenticatedUserAsync();

            if (!authReady)
            {
                const string error =
                    "Firebase Auth chưa đăng nhập.";

                Debug.LogError(
                    "[LeaderboardManager] " + error
                );

                hasLoaded = false;

                OnLeaderboardLoadFailed?.Invoke(error);

                return false;
            }

            if (db == null)
            {
                const string error =
                    "FirebaseFirestore chưa được khởi tạo.";

                Debug.LogError(
                    "[LeaderboardManager] " + error
                );

                hasLoaded = false;

                OnLeaderboardLoadFailed?.Invoke(error);

                return false;
            }

            FirebaseUser currentUser =
                auth.CurrentUser;

            Debug.Log(
                "[LeaderboardManager] " +
                "Auth ready. UID = " +
                currentUser.UserId
            );

            // ----------------------------------------------------
            // FIRESTORE QUERY
            // ----------------------------------------------------

            Query query =
                db
                    .Collection(LeaderboardCollection)
                    .OrderByDescending("bestScore")
                    .Limit(Mathf.Max(1, maxEntries));

            QuerySnapshot snapshot =
                await query.GetSnapshotAsync();

            // ----------------------------------------------------
            // REBUILD CACHE
            // ----------------------------------------------------

            entries.Clear();

            foreach (DocumentSnapshot document
                     in snapshot.Documents)
            {
                if (!document.Exists)
                {
                    continue;
                }

                if (!TryCreateEntry(
                        document,
                        out LeaderboardEntry entry))
                {
                    continue;
                }

                entries.Add(entry);
            }

            hasLoaded = true;

            Debug.Log(
                "[LeaderboardManager] Loaded " +
                entries.Count +
                " leaderboard entries."
            );

            OnLeaderboardLoaded?.Invoke();

            return true;
        }
        catch (Exception exception)
        {
            hasLoaded = false;

            Debug.LogError(
                "[LeaderboardManager] " +
                "Không thể tải leaderboard: " +
                exception
            );

            OnLeaderboardLoadFailed?.Invoke(
                exception.Message
            );

            return false;
        }
        finally
        {
            isLoading = false;
        }
    }

    // ============================================================
    // WAIT AUTH
    // ============================================================

    private async Task<bool> WaitForAuthenticatedUserAsync()
    {
        if (auth == null)
        {
            auth = FirebaseAuth.DefaultInstance;
        }

        if (auth == null)
        {
            return false;
        }

        for (int i = 0; i < authWaitAttempts; i++)
        {
            FirebaseUser currentUser =
                auth.CurrentUser;

            if (currentUser != null)
            {
                return true;
            }

            await Task.Delay(
                authWaitDelayMilliseconds
            );
        }

        return false;
    }

    // ============================================================
    // CREATE ENTRY
    // ============================================================

    private static bool TryCreateEntry(
        DocumentSnapshot document,
        out LeaderboardEntry entry)
    {
        entry = null;

        Dictionary<string, object> data =
            document.ToDictionary();

        string uid = document.Id;

        if (data.TryGetValue(
                "uid",
                out object uidValue))
        {
            string storedUid =
                uidValue as string;

            if (!string.IsNullOrEmpty(storedUid))
            {
                uid = storedUid;
            }
        }

        string displayName = "Player";

        if (data.TryGetValue(
                "displayName",
                out object displayNameValue))
        {
            string storedDisplayName =
                displayNameValue as string;

            if (!string.IsNullOrEmpty(
                    storedDisplayName))
            {
                displayName =
                    storedDisplayName;
            }
        }

        int bestScore = 0;

        if (data.TryGetValue(
                "bestScore",
                out object scoreValue))
        {
            if (scoreValue is long longScore)
            {
                bestScore =
                    ClampScoreToInt(longScore);
            }
            else if (scoreValue is int intScore)
            {
                bestScore =
                    Mathf.Max(0, intScore);
            }
            else if (scoreValue is double doubleScore)
            {
                bestScore =
                    ClampScoreToInt(
                        (long)doubleScore
                    );
            }
        }

        entry =
            new LeaderboardEntry(
                uid,
                displayName,
                bestScore
            );

        return true;
    }

    // ============================================================
    // SCORE SAFETY
    // ============================================================

    private static int ClampScoreToInt(
        long value)
    {
        if (value <= 0)
        {
            return 0;
        }

        if (value >= int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)value;
    }

    // ============================================================
    // ACCESS
    // ============================================================

    public LeaderboardEntry GetEntry(
        int index)
    {
        if ((uint)index >=
            (uint)entries.Count)
        {
            return null;
        }

        return entries[index];
    }

    public bool TryGetEntry(
        int index,
        out LeaderboardEntry entry)
    {
        if ((uint)index >=
            (uint)entries.Count)
        {
            entry = null;
            return false;
        }

        entry =
            entries[index];

        return true;
    }

    public void ClearLeaderboard()
    {
        entries.Clear();
        hasLoaded = false;
    }

    // ============================================================
    // DESTROY
    // ============================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}