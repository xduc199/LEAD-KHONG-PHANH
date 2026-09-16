using System;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerProgression : MonoBehaviour
{
    //==============================================================
    // LEVEL SETTINGS
    //==============================================================

    [Header("Level Settings")]
    [SerializeField] private int startingLevel = 1;

    [Tooltip("EXP cần để đi từ Level 1 lên Level 2.")]
    [SerializeField] private int baseExpRequired = 100;

    [Tooltip("Mỗi Level tiếp theo cần nhiều EXP hơn Level trước.")]
    [SerializeField] private float expGrowthMultiplier = 1.25f;


    //==============================================================
    // RUNTIME DATA
    //==============================================================

    private int level;
    private int totalExp;

    private bool isInitialized;


    //==============================================================
    // PUBLIC DATA
    //==============================================================

    public int Level => level;

    public int TotalExp => totalExp;

    public bool IsInitialized => isInitialized;


    /// <summary>
    /// EXP đã tích lũy trong Level hiện tại.
    /// </summary>
    public int CurrentLevelExp
    {
        get
        {
            int expBeforeCurrentLevel = 0;

            for (int i = 1; i < level; i++)
            {
                expBeforeCurrentLevel +=
                    GetExpRequiredForLevel(i);
            }

            return Mathf.Max(
                0,
                totalExp - expBeforeCurrentLevel
            );
        }
    }


    /// <summary>
    /// EXP cần thêm để lên Level tiếp theo.
    /// </summary>
    public int ExpRequiredForNextLevel
    {
        get
        {
            return GetExpRequiredForLevel(level);
        }
    }


    //==============================================================
    // EVENTS
    //==============================================================

    /// <summary>
    /// currentLevelExp, expRequiredForNextLevel
    /// </summary>
    public Action<int, int> OnExpChanged;


    /// <summary>
    /// Gọi khi Player vừa lên Level mới.
    /// Chỉ được gọi sau khi EXP đã save thành công.
    /// </summary>
    public Action<int> OnLevelUp;


    //==============================================================
    // UNITY
    //==============================================================

    private void Awake()
    {
        isInitialized = false;

        SubscribeToFirestore();
    }


    private void OnDestroy()
    {
        UnsubscribeFromFirestore();
    }


    //==============================================================
    // FIRESTORE CONNECTION
    //==============================================================

    private void SubscribeToFirestore()
    {
        if (FirestorePlayerDataManager.Instance == null)
        {
            Debug.LogWarning(
                "[PlayerProgression] " +
                "FirestorePlayerDataManager chưa tồn tại."
            );

            return;
        }

        FirestorePlayerDataManager.Instance
            .OnPlayerDataLoaded +=
            HandlePlayerDataLoaded;

        FirestorePlayerDataManager.Instance
            .OnPlayerDataCleared +=
            HandlePlayerDataCleared;


        //==========================================================
        // ACCOUNT ĐÃ LOAD TRƯỚC KHI PLAYERPROGRESSION ĐƯỢC TẠO
        //==========================================================

        if (
            FirestorePlayerDataManager.Instance.HasPlayerData
        )
        {
            HandlePlayerDataLoaded(
                FirestorePlayerDataManager.Instance
                    .CurrentPlayerData
            );
        }
    }


    private void UnsubscribeFromFirestore()
    {
        if (FirestorePlayerDataManager.Instance == null)
            return;

        FirestorePlayerDataManager.Instance
            .OnPlayerDataLoaded -=
            HandlePlayerDataLoaded;

        FirestorePlayerDataManager.Instance
            .OnPlayerDataCleared -=
            HandlePlayerDataCleared;
    }


    //==============================================================
    // PLAYER DATA LOADED
    //==============================================================

    private void HandlePlayerDataLoaded(
        PlayerData data
    )
    {
        if (data == null)
        {
            Debug.LogError(
                "[PlayerProgression] " +
                "PlayerData = null."
            );

            return;
        }

        level =
            Mathf.Max(
                startingLevel,
                data.level
            );

        totalExp =
            Mathf.Max(
                0,
                data.exp
            );


        //==========================================================
        // REBUILD LEVEL FROM TOTAL EXP
        //==========================================================

        RebuildLevelFromTotalExp();


        isInitialized = true;


        //==========================================================
        // UPDATE LOCAL PLAYER DATA
        //==========================================================

        data.level = level;
        data.exp = totalExp;


        //==========================================================
        // UI EVENT
        //==========================================================

        OnExpChanged?.Invoke(
            CurrentLevelExp,
            ExpRequiredForNextLevel
        );


        Debug.Log(
            $"[PlayerProgression] " +
            $"Account loaded → " +
            $"Level: {level} | " +
            $"Total EXP: {totalExp} | " +
            $"Current EXP: " +
            $"{CurrentLevelExp}/{ExpRequiredForNextLevel}"
        );
    }


    //==============================================================
    // PLAYER DATA CLEARED
    //==============================================================

    private void HandlePlayerDataCleared()
    {
        isInitialized = false;

        level =
            startingLevel;

        totalExp = 0;


        OnExpChanged?.Invoke(
            CurrentLevelExp,
            ExpRequiredForNextLevel
        );


        Debug.Log(
            "[PlayerProgression] " +
            "Account cleared."
        );
    }


    //==============================================================
    // ADD EXP ASYNC
    //==============================================================

    /// <summary>
    /// Cộng EXP và chờ Firestore save hoàn tất.
    ///
    /// Đây là API chính dành cho các hệ thống cần đảm bảo
    /// EXP đã save xong trước khi tiếp tục.
    /// </summary>
    public async Task<bool> AddExpAsync(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }


        if (!isInitialized)
        {
            Debug.LogWarning(
                "[PlayerProgression] " +
                "Chưa load PlayerData. " +
                "Không thể Add EXP."
            );

            return false;
        }


        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (firestore == null)
        {
            Debug.LogError(
                "[PlayerProgression] " +
                "FirestorePlayerDataManager không tồn tại."
            );

            return false;
        }


        if (!firestore.HasPlayerData)
        {
            Debug.LogWarning(
                "[PlayerProgression] " +
                "Không có PlayerData hiện tại."
            );

            return false;
        }


        //==========================================================
        // LƯU LEVEL CŨ
        //==========================================================

        int oldLevel = level;


        //==========================================================
        // ADD EXP RUNTIME
        //==========================================================

        totalExp += amount;


        //==========================================================
        // CHECK LEVEL UP
        //==========================================================

        while (
            totalExp >=
            GetTotalExpRequiredForLevel(
                level + 1
            )
        )
        {
            level++;
        }


        //==========================================================
        // UPDATE FIRESTORE CACHE
        //==========================================================

        PlayerData data =
            firestore.CurrentPlayerData;


        if (data == null)
        {
            Debug.LogError(
                "[PlayerProgression] " +
                "CurrentPlayerData = null."
            );

            return false;
        }


        data.exp =
            totalExp;

        data.level =
            level;


        //==========================================================
        // SAVE FIRESTORE
        //==========================================================

        bool saved =
            await firestore.SetProgressionAsync(
                level,
                totalExp
            );


        if (!saved)
        {
            Debug.LogError(
                "[PlayerProgression] " +
                "Không thể save EXP lên Firestore."
            );

            return false;
        }


        //==========================================================
        // LEVEL UP EVENT
        //
        // QUAN TRỌNG:
        // Chỉ invoke SAU KHI Firestore save thành công.
        //
        // Điều này tránh MissionManager nhận OnLevelUp rồi
        // tự tạo một mission save trong lúc EXP save vẫn đang chạy.
        //==========================================================

        if (level > oldLevel)
        {
            for (
                int unlockedLevel = oldLevel + 1;
                unlockedLevel <= level;
                unlockedLevel++
            )
            {
                OnLevelUp?.Invoke(unlockedLevel);

                Debug.Log(
                    $"[PlayerProgression] " +
                    $"LEVEL UP → " +
                    $"{unlockedLevel - 1} → " +
                    $"{unlockedLevel}"
                );
            }
        }


        //==========================================================
        // EXP EVENT
        //==========================================================

        OnExpChanged?.Invoke(
            CurrentLevelExp,
            ExpRequiredForNextLevel
        );


        //==========================================================
        // LOG
        //==========================================================

        Debug.Log(
            $"[PlayerProgression] " +
            $"+{amount} EXP → " +
            $"Level {level} | " +
            $"Total EXP {totalExp}"
        );


        return true;
    }


    //==============================================================
    // ADD EXP LEGACY WRAPPER
    //==============================================================

    /// <summary>
    /// API cũ để không làm hỏng các script hiện tại.
    ///
    /// Các hệ thống cần đảm bảo save hoàn tất nên dùng:
    /// await AddExpAsync(...)
    /// </summary>
    public async void AddExp(int amount)
    {
        await AddExpAsync(amount);
    }


    //==============================================================
    // EXP REQUIRED FOR ONE LEVEL
    //==============================================================

    public int GetExpRequiredForLevel(
        int targetLevel
    )
    {
        if (targetLevel <= 0)
            return baseExpRequired;


        float required =
            baseExpRequired *
            Mathf.Pow(
                expGrowthMultiplier,
                targetLevel - 1
            );


        return Mathf.Max(
            1,
            Mathf.RoundToInt(required)
        );
    }


    //==============================================================
    // TOTAL EXP REQUIRED TO REACH LEVEL
    //==============================================================

    public int GetTotalExpRequiredForLevel(
        int targetLevel
    )
    {
        if (targetLevel <= 1)
            return 0;


        int totalRequired = 0;


        for (int i = 1; i < targetLevel; i++)
        {
            totalRequired +=
                GetExpRequiredForLevel(i);
        }


        return totalRequired;
    }


    //==============================================================
    // REBUILD LEVEL
    //==============================================================

    private void RebuildLevelFromTotalExp()
    {
        level =
            Mathf.Max(
                startingLevel,
                level
            );


        while (
            totalExp >=
            GetTotalExpRequiredForLevel(
                level + 1
            )
        )
        {
            level++;
        }
    }


    //==============================================================
    // REFRESH FROM FIRESTORE CACHE
    //==============================================================

    public void RefreshFromPlayerData()
    {
        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (
            firestore == null ||
            !firestore.HasPlayerData
        )
        {
            return;
        }


        HandlePlayerDataLoaded(
            firestore.CurrentPlayerData
        );
    }


    //==============================================================
    // RESET
    //==============================================================

    public async void ResetProgress()
    {
        if (!isInitialized)
        {
            Debug.LogWarning(
                "[PlayerProgression] " +
                "Chưa initialize."
            );

            return;
        }


        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        if (
            firestore == null ||
            !firestore.HasPlayerData
        )
        {
            Debug.LogError(
                "[PlayerProgression] " +
                "Không có PlayerData."
            );

            return;
        }


        level =
            startingLevel;

        totalExp = 0;


        PlayerData data =
            firestore.CurrentPlayerData;


        if (data != null)
        {
            data.level = level;
            data.exp = totalExp;
        }


        bool saved =
            await firestore.SetProgressionAsync(
                level,
                totalExp
            );


        if (!saved)
        {
            Debug.LogError(
                "[PlayerProgression] " +
                "Reset Progress thất bại."
            );

            return;
        }


        OnExpChanged?.Invoke(
            CurrentLevelExp,
            ExpRequiredForNextLevel
        );


        Debug.Log(
            "[PlayerProgression] " +
            "Progress reset."
        );
    }


    //==============================================================
    // TEST
    //==============================================================

    [ContextMenu("TEST Add 50 EXP")]
    private void TestAddExp()
    {
        AddExp(50);
    }


    [ContextMenu("TEST Refresh From Firestore")]
    private void TestRefreshFromFirestore()
    {
        RefreshFromPlayerData();


        Debug.Log(
            $"[PlayerProgression TEST] " +
            $"Level: {Level} | " +
            $"Total EXP: {TotalExp} | " +
            $"Current EXP: " +
            $"{CurrentLevelExp}/{ExpRequiredForNextLevel}"
        );
    }
}