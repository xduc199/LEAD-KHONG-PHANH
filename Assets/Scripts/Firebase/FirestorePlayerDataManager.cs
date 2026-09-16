using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;

public class FirestorePlayerDataManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    public static FirestorePlayerDataManager Instance { get; private set; }


    // =========================================================
    // CURRENT PLAYER
    // =========================================================

    public PlayerData CurrentPlayerData { get; private set; }

    public bool IsLoading { get; private set; }

    public bool HasPlayerData =>
        CurrentPlayerData != null;

    public string CurrentPlayerUID =>
        CurrentPlayerData != null
            ? CurrentPlayerData.uid
            : null;


    // =========================================================
    // FIRESTORE
    // =========================================================

    private FirebaseFirestore db;


    // =========================================================
    // EVENTS
    // =========================================================

    public Action<PlayerData> OnPlayerDataLoaded;

    public Action OnPlayerDataCleared;

    public Action<PlayerData> OnPlayerDataSaved;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        InitializeFirestore();
    }


    // =========================================================
    // FIRESTORE INITIALIZE
    // =========================================================

    private void InitializeFirestore()
    {
        try
        {
            db = FirebaseFirestore.DefaultInstance;

            if (db == null)
            {
                Debug.LogError(
                    "[Firestore] Không thể khởi tạo Firestore."
                );

                return;
            }

            Debug.Log(
                "[Firestore] Initialized successfully."
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[Firestore] Initialization failed: {exception}"
            );
        }
    }


    // =========================================================
    // CREATE / LOAD PLAYER
    // =========================================================

    public async Task<PlayerData> CreateOrLoadPlayerDataAsync(
        FirebaseUser user)
    {
        if (user == null)
        {
            Debug.LogError(
                "[Firestore] FirebaseUser = null."
            );

            return null;
        }

        if (db == null)
        {
            InitializeFirestore();
        }

        if (db == null)
        {
            return null;
        }

        IsLoading = true;

        string requestedUID = user.UserId;

        try
        {
            DocumentReference playerRef =
                db.Collection("players")
                  .Document(requestedUID);

            DocumentSnapshot snapshot =
                await playerRef.GetSnapshotAsync();


            // =================================================
            // ACCOUNT SAFETY
            // =================================================

            if (!IsSameCurrentFirebaseUser(requestedUID))
            {
                Debug.LogWarning(
                    "[Firestore] Account đã thay đổi trong lúc load. " +
                    "Bỏ qua dữ liệu cũ."
                );

                return null;
            }


            // =================================================
            // EXISTING PLAYER
            // =================================================

            if (snapshot.Exists)
            {
                Debug.Log(
                    $"[Firestore] Player data found: {requestedUID}"
                );

                PlayerData loadedData =
                    ConvertSnapshotToPlayerData(
                        snapshot,
                        requestedUID
                    );

                CurrentPlayerData =
                    loadedData;

                OnPlayerDataLoaded?.Invoke(
                    CurrentPlayerData
                );

                Debug.Log(
                    $"[Firestore] Account loaded: " +
                    $"{CurrentPlayerData.displayName} | " +
                    $"Level {CurrentPlayerData.level} | " +
                    $"EXP {CurrentPlayerData.exp} | " +
                    $"Coins {CurrentPlayerData.coins} | " +
                    $"BestScore {CurrentPlayerData.bestScore} | " +
                    $"GumLv {CurrentPlayerData.gumUpgradeLevel} | " +
                    $"PhotonLv {CurrentPlayerData.photonUpgradeLevel} | " +
                    $"ShieldLv {CurrentPlayerData.shieldUpgradeLevel} | " +
                    $"MagnetLv {CurrentPlayerData.magnetUpgradeLevel} | " +
                    $"MissionDate {CurrentPlayerData.missionDate}"
                );

                // =================================================
                // LEADERBOARD SYNC
                // =================================================

                await SyncCurrentPlayerLeaderboardAsync();

                return CurrentPlayerData;
            }


            // =================================================
            // NEW PLAYER
            // =================================================

            Debug.Log(
                $"[Firestore] Player data not found. " +
                $"Creating: {requestedUID}"
            );

            PlayerData newPlayer =
                CreateDefaultPlayerData(user);


            Dictionary<string, object> data =
                ConvertPlayerDataToDictionary(
                    newPlayer
                );


            await playerRef.SetAsync(data);


            // Account có thể đã logout trong lúc await.
            if (!IsSameCurrentFirebaseUser(requestedUID))
            {
                Debug.LogWarning(
                    "[Firestore] Account đã thay đổi sau khi tạo data. " +
                    "Không set CurrentPlayerData."
                );

                return null;
            }


            CurrentPlayerData =
                newPlayer;

            OnPlayerDataLoaded?.Invoke(
                CurrentPlayerData
            );

            Debug.Log(
                $"[Firestore] Player data created: {requestedUID}"
            );

            // =================================================
            // LEADERBOARD SYNC FOR NEW PLAYER
            // =================================================

            await SyncCurrentPlayerLeaderboardAsync();

            return CurrentPlayerData;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[Firestore] Create/Load failed: {exception}"
            );

            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }


    // =========================================================
    // CHECK CURRENT FIREBASE USER
    // =========================================================

    private bool IsSameCurrentFirebaseUser(
        string uid)
    {
        if (string.IsNullOrEmpty(uid))
            return false;

        FirebaseUser currentUser =
            FirebaseAuthManager.Instance != null
                ? FirebaseAuthManager.Instance.CurrentUser
                : null;

        if (currentUser == null)
            return false;

        return currentUser.UserId == uid;
    }


    // =========================================================
    // SAVE ALL PLAYER DATA
    // =========================================================

    public async Task<bool> SavePlayerDataAsync()
    {
        PlayerData dataToSave =
            CurrentPlayerData;

        if (dataToSave == null)
        {
            Debug.LogError(
                "[Firestore] Không có CurrentPlayerData."
            );

            return false;
        }

        FirebaseUser user =
            FirebaseAuthManager.Instance != null
                ? FirebaseAuthManager.Instance.CurrentUser
                : null;

        if (user == null)
        {
            Debug.LogError(
                "[Firestore] Không có FirebaseUser đang đăng nhập."
            );

            return false;
        }


        string uid =
            user.UserId;


        // =====================================================
        // UID SAFETY
        // =====================================================

        if (dataToSave.uid != uid)
        {
            Debug.LogError(
                "[Firestore] UID mismatch. " +
                "Không save dữ liệu của account khác."
            );

            return false;
        }


        if (db == null)
        {
            InitializeFirestore();
        }

        if (db == null)
        {
            return false;
        }


        try
        {
            DocumentReference playerRef =
                db.Collection("players")
                  .Document(uid);


            Dictionary<string, object> data =
                ConvertPlayerDataToDictionary(
                    dataToSave
                );


            await playerRef.SetAsync(
                data,
                SetOptions.MergeAll
            );


            // Không phát event cho account khác.
            if (
                CurrentPlayerData == dataToSave &&
                CurrentPlayerUID == uid
            )
            {
                OnPlayerDataSaved?.Invoke(
                    dataToSave
                );
            }


            Debug.Log(
                $"[Firestore] Player data saved: {uid}"
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[Firestore] Save failed: {exception}"
            );

            return false;
        }
    }


    // =========================================================
    // SAVE ONLY MISSION DATA
    // =========================================================

    public async Task<bool> SaveMissionDataAsync()
    {
        PlayerData dataToSave =
            CurrentPlayerData;

        if (dataToSave == null)
        {
            Debug.LogWarning(
                "[Firestore] Không có CurrentPlayerData " +
                "để save Mission."
            );

            return false;
        }


        FirebaseUser user =
            FirebaseAuthManager.Instance != null
                ? FirebaseAuthManager.Instance.CurrentUser
                : null;


        if (user == null)
        {
            Debug.LogWarning(
                "[Firestore] Không có FirebaseUser " +
                "để save Mission."
            );

            return false;
        }


        string uid =
            user.UserId;


        // =====================================================
        // UID SAFETY
        // =====================================================

        if (dataToSave.uid != uid)
        {
            Debug.LogError(
                "[Firestore] Mission save UID mismatch."
            );

            return false;
        }


        if (db == null)
        {
            InitializeFirestore();
        }

        if (db == null)
        {
            return false;
        }


        try
        {
            DocumentReference playerRef =
                db.Collection("players")
                  .Document(uid);


            Dictionary<string, object> missionData =
                ConvertMissionDataToDictionary(
                    dataToSave
                );


            // Chỉ Merge các field Mission.
            // Không đụng coins / exp / level / bike...
            await playerRef.SetAsync(
                missionData,
                SetOptions.MergeAll
            );


            Debug.Log(
                $"[Firestore] Mission data saved: {uid}"
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[Firestore] Mission save failed: {exception}"
            );

            return false;
        }
    }


    // =========================================================
    // PROGRESSION
    // =========================================================

    public async Task<bool> SetProgressionAsync(
        int level,
        int exp)
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogError(
                "[Firestore] Không có player data."
            );

            return false;
        }

        level =
            Mathf.Max(
                1,
                level
            );

        exp =
            Mathf.Max(
                0,
                exp
            );

        CurrentPlayerData.level =
            level;

        CurrentPlayerData.exp =
            exp;

        return await SavePlayerDataAsync();
    }


    // =========================================================
    // COINS
    // =========================================================

    public async Task<bool> AddCoinsAsync(
        int amount)
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogError(
                "[Firestore] Không có player data."
            );

            return false;
        }

        int newCoins =
            CurrentPlayerData.coins +
            amount;

        newCoins =
            Mathf.Max(
                0,
                newCoins
            );

        CurrentPlayerData.coins =
            newCoins;

        return await SavePlayerDataAsync();
    }


    public async Task<bool> SetCoinsAsync(
        int coins)
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogError(
                "[Firestore] Không có player data."
            );

            return false;
        }

        CurrentPlayerData.coins =
            Mathf.Max(
                0,
                coins
            );

        return await SavePlayerDataAsync();
    }


    // =========================================================
    // BEST SCORE
    // =========================================================

    public async Task<bool> TrySetBestScoreAsync(
        int score)
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogError(
                "[Firestore] Không có player data."
            );

            return false;
        }

        score =
            Mathf.Max(
                0,
                score
            );

        if (
            score <=
            CurrentPlayerData.bestScore
        )
        {
            return true;
        }

        CurrentPlayerData.bestScore =
            score;

        bool playerSaved =
            await SavePlayerDataAsync();

        if (!playerSaved)
        {
            return false;
        }

        // =====================================================
        // SYNC LEADERBOARD IMMEDIATELY
        // =====================================================

        return await SaveLeaderboardBestScoreAsync(
            CurrentPlayerData.bestScore
        );
    }


    // =========================================================
    // LEADERBOARD
    // =========================================================

    public async Task<bool> SaveLeaderboardBestScoreAsync(
        int bestScore)
    {
        PlayerData dataToSave =
            CurrentPlayerData;

        if (dataToSave == null)
        {
            Debug.LogError(
                "[Firestore] Không có CurrentPlayerData " +
                "để save Leaderboard."
            );

            return false;
        }

        FirebaseUser user =
            FirebaseAuthManager.Instance != null
                ? FirebaseAuthManager.Instance.CurrentUser
                : null;

        if (user == null)
        {
            Debug.LogError(
                "[Firestore] Không có FirebaseUser " +
                "đang đăng nhập để save Leaderboard."
            );

            return false;
        }

        string uid =
            user.UserId;

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError(
                "[Firestore] Leaderboard UID không hợp lệ."
            );

            return false;
        }

        if (dataToSave.uid != uid)
        {
            Debug.LogError(
                "[Firestore] Leaderboard UID mismatch. " +
                "Không save dữ liệu của account khác."
            );

            return false;
        }

        bestScore =
            Mathf.Max(
                0,
                bestScore
            );

        if (db == null)
        {
            InitializeFirestore();
        }

        if (db == null)
        {
            return false;
        }

        try
        {
            DocumentReference leaderboardRef =
                db.Collection("leaderboard")
                  .Document(uid);

            DocumentSnapshot existingSnapshot =
                await leaderboardRef.GetSnapshotAsync();

            int existingBestScore = 0;

            if (existingSnapshot.Exists &&
                existingSnapshot.ContainsField("bestScore"))
            {
                existingBestScore =
                    GetInt(
                        existingSnapshot,
                        "bestScore",
                        0
                    );

                existingBestScore =
                    Mathf.Max(
                        0,
                        existingBestScore
                    );
            }

            // Không bao giờ làm leaderboard tụt điểm.
            if (existingBestScore > bestScore)
            {
                bestScore =
                    existingBestScore;
            }

            Dictionary<string, object> leaderboardData =
                new Dictionary<string, object>
                {
                    {
                        "uid",
                        uid
                    },

                    {
                        "displayName",
                        string.IsNullOrEmpty(
                            dataToSave.displayName
                        )
                            ? "Player"
                            : dataToSave.displayName
                    },

                    {
                        "bestScore",
                        bestScore
                    },

                    {
                        "updatedAt",
                        FieldValue.ServerTimestamp
                    }
                };

            await leaderboardRef.SetAsync(
                leaderboardData,
                SetOptions.MergeAll
            );

            Debug.Log(
                $"[Firestore] Leaderboard updated: " +
                $"{uid} | " +
                $"{dataToSave.displayName} | " +
                $"BestScore {bestScore}"
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[Firestore] Leaderboard save failed: " +
                $"{exception}"
            );

            return false;
        }
    }


    // =========================================================
    // SYNC CURRENT PLAYER → LEADERBOARD
    // =========================================================

    public async Task<bool> SyncCurrentPlayerLeaderboardAsync()
    {
        PlayerData data =
            CurrentPlayerData;

        if (data == null)
        {
            Debug.LogWarning(
                "[Firestore] Không có CurrentPlayerData " +
                "để sync Leaderboard."
            );

            return false;
        }

        FirebaseUser user =
            FirebaseAuthManager.Instance != null
                ? FirebaseAuthManager.Instance.CurrentUser
                : null;

        if (user == null)
        {
            Debug.LogWarning(
                "[Firestore] Không có FirebaseUser " +
                "để sync Leaderboard."
            );

            return false;
        }

        string uid =
            user.UserId;

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError(
                "[Firestore] Không thể sync Leaderboard: UID rỗng."
            );

            return false;
        }

        if (data.uid != uid)
        {
            Debug.LogError(
                "[Firestore] Leaderboard sync UID mismatch."
            );

            return false;
        }

        Debug.Log(
            $"[Firestore] Syncing Leaderboard: " +
            $"{data.displayName} | " +
            $"BestScore {data.bestScore}"
        );

        return await SaveLeaderboardBestScoreAsync(
            data.bestScore
        );
    }


    // =========================================================
    // SELECTED BIKE
    // =========================================================

    public async Task<bool> SetSelectedBikeAsync(
        string bikeId)
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogError(
                "[Firestore] Không có player data."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(bikeId))
        {
            Debug.LogError(
                "[Firestore] bikeId không hợp lệ."
            );

            return false;
        }

        if (
            CurrentPlayerData.ownedBikes == null ||
            !CurrentPlayerData.ownedBikes.Contains(
                bikeId
            )
        )
        {
            Debug.LogWarning(
                $"[Firestore] Bike chưa sở hữu: {bikeId}"
            );

            return false;
        }

        CurrentPlayerData.selectedBike =
            bikeId;

        return await SavePlayerDataAsync();
    }


    // =========================================================
    // OWNED BIKES
    // =========================================================

    public async Task<bool> AddOwnedBikeAsync(
        string bikeId)
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogError(
                "[Firestore] Không có player data."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(bikeId))
        {
            Debug.LogError(
                "[Firestore] bikeId không hợp lệ."
            );

            return false;
        }

        if (CurrentPlayerData.ownedBikes == null)
        {
            CurrentPlayerData.ownedBikes =
                new List<string>();
        }

        if (
            !CurrentPlayerData.ownedBikes.Contains(
                bikeId
            )
        )
        {
            CurrentPlayerData.ownedBikes.Add(
                bikeId
            );
        }

        return await SavePlayerDataAsync();
    }


    public bool OwnsBike(
        string bikeId)
    {
        if (
            CurrentPlayerData == null ||
            CurrentPlayerData.ownedBikes == null
        )
        {
            return false;
        }

        return CurrentPlayerData.ownedBikes.Contains(
            bikeId
        );
    }


    // =========================================================
    // DEFAULT DATA
    // =========================================================

    private PlayerData CreateDefaultPlayerData(
        FirebaseUser user)
    {
        string displayName =
            user.Email;


        if (!string.IsNullOrEmpty(user.Email))
        {
            int atIndex =
                user.Email.IndexOf("@");


            if (atIndex > 0)
            {
                displayName =
                    user.Email.Substring(
                        0,
                        atIndex
                    );
            }
        }


        PlayerData data =
            new PlayerData();


        data.uid =
            user.UserId;


        data.displayName =
            displayName;


        // =====================================================
        // ITEM UPGRADE DEFAULT
        // =====================================================

        data.gumUpgradeLevel = 1;
        data.photonUpgradeLevel = 1;
        data.shieldUpgradeLevel = 1;
        data.magnetUpgradeLevel = 1;


        return data;
    }


    // =========================================================
    // PLAYER DATA → FIRESTORE
    // =========================================================

    private Dictionary<string, object>
        ConvertPlayerDataToDictionary(
            PlayerData data)
    {
        Dictionary<string, object> result =
            new Dictionary<string, object>
            {
                // -------------------------------------------------
                // ACCOUNT
                // -------------------------------------------------

                {
                    "uid",
                    data.uid ?? string.Empty
                },

                {
                    "displayName",
                    data.displayName ?? string.Empty
                },


                // -------------------------------------------------
                // PROGRESSION
                // -------------------------------------------------

                {
                    "level",
                    Mathf.Max(
                        1,
                        data.level
                    )
                },

                {
                    "exp",
                    Mathf.Max(
                        0,
                        data.exp
                    )
                },


                // -------------------------------------------------
                // ECONOMY
                // -------------------------------------------------

                {
                    "coins",
                    Mathf.Max(
                        0,
                        data.coins
                    )
                },

                {
                    "bestScore",
                    Mathf.Max(
                        0,
                        data.bestScore
                    )
                },


                // -------------------------------------------------
                // BIKE
                // -------------------------------------------------

                {
                    "selectedBike",
                    data.selectedBike ??
                    "NinjaLead"
                },

                {
                    "ownedBikes",
                    data.ownedBikes ??
                    new List<string>()
                },


                // -------------------------------------------------
                // ITEM UPGRADES
                // -------------------------------------------------

                {
                    "gumUpgradeLevel",
                    Mathf.Max(
                        1,
                        data.gumUpgradeLevel
                    )
                },

                {
                    "photonUpgradeLevel",
                    Mathf.Max(
                        1,
                        data.photonUpgradeLevel
                    )
                },

                {
                    "shieldUpgradeLevel",
                    Mathf.Max(
                        1,
                        data.shieldUpgradeLevel
                    )
                },

                {
                    "magnetUpgradeLevel",
                    Mathf.Max(
                        1,
                        data.magnetUpgradeLevel
                    )
                }
            };


        Dictionary<string, object> missionData =
            ConvertMissionDataToDictionary(
                data
            );


        foreach (
            KeyValuePair<string, object> pair
            in missionData
        )
        {
            result[pair.Key] =
                pair.Value;
        }


        return result;
    }


    // =========================================================
    // MISSION DATA → FIRESTORE
    // =========================================================

    private Dictionary<string, object>
        ConvertMissionDataToDictionary(
            PlayerData data)
    {
        List<Dictionary<string, object>>
            missionStates =
                new List<Dictionary<string, object>>();


        if (data.missionStates != null)
        {
            for (
                int i = 0;
                i < data.missionStates.Count;
                i++)
            {
                MissionSaveData mission =
                    data.missionStates[i];


                if (mission == null)
                    continue;


                missionStates.Add(
                    new Dictionary<string, object>
                    {
                        {
                            "id",
                            mission.id ??
                            string.Empty
                        },

                        {
                            "progress",
                            Mathf.Max(
                                0,
                                mission.progress
                            )
                        },

                        {
                            "claimed",
                            mission.claimed
                        }
                    }
                );
            }
        }


        return new Dictionary<string, object>
        {
            {
                "missionDate",
                data.missionDate ??
                string.Empty
            },

            {
                "missionSelection",
                data.missionSelection ??
                new List<string>()
            },

            {
                "missionDailyCoins",
                Mathf.Max(
                    0,
                    data.missionDailyCoins
                )
            },

            {
                "missionDailyDistance",
                Mathf.Max(
                    0,
                    data.missionDailyDistance
                )
            },

            {
                "missionDailyRuns",
                Mathf.Max(
                    0,
                    data.missionDailyRuns
                )
            },

            {
                "missionDailyExp",
                Mathf.Max(
                    0,
                    data.missionDailyExp
                )
            },

            {
                "missionPreviousTotalExp",
                Mathf.Max(
                    0,
                    data.missionPreviousTotalExp
                )
            },

            {
                "missionStates",
                missionStates
            }
        };
    }


    // =========================================================
    // FIRESTORE → PLAYER DATA
    // =========================================================

    private PlayerData ConvertSnapshotToPlayerData(
        DocumentSnapshot snapshot,
        string uid)
    {
        PlayerData data =
            new PlayerData();


        data.uid =
            uid;


        // =====================================================
        // ACCOUNT
        // =====================================================

        data.displayName =
            GetString(
                snapshot,
                "displayName",
                "Player"
            );


        // =====================================================
        // PROGRESSION
        // =====================================================

        data.level =
            GetInt(
                snapshot,
                "level",
                1
            );

        data.exp =
            GetInt(
                snapshot,
                "exp",
                0
            );


        // =====================================================
        // ECONOMY
        // =====================================================

        data.coins =
            GetInt(
                snapshot,
                "coins",
                0
            );

        data.bestScore =
            GetInt(
                snapshot,
                "bestScore",
                0
            );


        // =====================================================
        // BIKE
        // =====================================================

        data.selectedBike =
            GetString(
                snapshot,
                "selectedBike",
                "NinjaLead"
            );


        data.ownedBikes =
            ReadStringList(
                snapshot,
                "ownedBikes"
            );


        if (
            data.ownedBikes == null ||
            data.ownedBikes.Count == 0
        )
        {
            data.ownedBikes =
                new List<string>
                {
                    "NinjaLead"
                };
        }


        // =====================================================
        // ITEM UPGRADES
        // =====================================================

        data.gumUpgradeLevel =
            Mathf.Max(
                1,
                GetInt(
                    snapshot,
                    "gumUpgradeLevel",
                    1
                )
            );


        data.photonUpgradeLevel =
            Mathf.Max(
                1,
                GetInt(
                    snapshot,
                    "photonUpgradeLevel",
                    1
                )
            );


        data.shieldUpgradeLevel =
            Mathf.Max(
                1,
                GetInt(
                    snapshot,
                    "shieldUpgradeLevel",
                    1
                )
            );


        data.magnetUpgradeLevel =
            Mathf.Max(
                1,
                GetInt(
                    snapshot,
                    "magnetUpgradeLevel",
                    1
                )
            );


        // =====================================================
        // DAILY MISSION
        // =====================================================

        data.missionDate =
            GetString(
                snapshot,
                "missionDate",
                string.Empty
            );


        data.missionSelection =
            ReadStringList(
                snapshot,
                "missionSelection"
            );


        if (data.missionSelection == null)
        {
            data.missionSelection =
                new List<string>();
        }


        data.missionDailyCoins =
            GetInt(
                snapshot,
                "missionDailyCoins",
                0
            );


        data.missionDailyDistance =
            GetInt(
                snapshot,
                "missionDailyDistance",
                0
            );


        data.missionDailyRuns =
            GetInt(
                snapshot,
                "missionDailyRuns",
                0
            );


        data.missionDailyExp =
            GetInt(
                snapshot,
                "missionDailyExp",
                0
            );


        data.missionPreviousTotalExp =
            GetInt(
                snapshot,
                "missionPreviousTotalExp",
                data.exp
            );


        data.missionStates =
            ReadMissionStates(
                snapshot,
                "missionStates"
            );


        if (data.missionStates == null)
        {
            data.missionStates =
                new List<MissionSaveData>();
        }


        // =====================================================
        // SAFETY
        // =====================================================

        data.level =
            Mathf.Max(
                1,
                data.level
            );


        data.exp =
            Mathf.Max(
                0,
                data.exp
            );


        data.coins =
            Mathf.Max(
                0,
                data.coins
            );


        data.bestScore =
            Mathf.Max(
                0,
                data.bestScore
            );


        data.gumUpgradeLevel =
            Mathf.Max(
                1,
                data.gumUpgradeLevel
            );


        data.photonUpgradeLevel =
            Mathf.Max(
                1,
                data.photonUpgradeLevel
            );


        data.shieldUpgradeLevel =
            Mathf.Max(
                1,
                data.shieldUpgradeLevel
            );


        data.magnetUpgradeLevel =
            Mathf.Max(
                1,
                data.magnetUpgradeLevel
            );


        data.missionDailyCoins =
            Mathf.Max(
                0,
                data.missionDailyCoins
            );


        data.missionDailyDistance =
            Mathf.Max(
                0,
                data.missionDailyDistance
            );


        data.missionDailyRuns =
            Mathf.Max(
                0,
                data.missionDailyRuns
            );


        data.missionDailyExp =
            Mathf.Max(
                0,
                data.missionDailyExp
            );


        data.missionPreviousTotalExp =
            Mathf.Max(
                0,
                data.missionPreviousTotalExp
            );


        if (
            string.IsNullOrWhiteSpace(
                data.selectedBike
            )
        )
        {
            data.selectedBike =
                "NinjaLead";
        }


        return data;
    }


    // =========================================================
    // READ STRING
    // =========================================================

    private string GetString(
        DocumentSnapshot snapshot,
        string field,
        string defaultValue)
    {
        if (!snapshot.ContainsField(field))
            return defaultValue;


        try
        {
            return snapshot.GetValue<string>(
                field
            );
        }
        catch
        {
            return defaultValue;
        }
    }


    // =========================================================
    // READ INT
    // =========================================================

    private int GetInt(
        DocumentSnapshot snapshot,
        string field,
        int defaultValue)
    {
        if (!snapshot.ContainsField(field))
            return defaultValue;


        try
        {
            long value =
                snapshot.GetValue<long>(
                    field
                );


            if (
                value >
                int.MaxValue ||
                value <
                int.MinValue
            )
            {
                return defaultValue;
            }


            return (int)value;
        }
        catch
        {
            try
            {
                return snapshot.GetValue<int>(
                    field
                );
            }
            catch
            {
                return defaultValue;
            }
        }
    }


    // =========================================================
    // READ STRING LIST
    // =========================================================

    private List<string> ReadStringList(
        DocumentSnapshot snapshot,
        string field)
    {
        if (!snapshot.ContainsField(field))
        {
            return new List<string>();
        }


        try
        {
            List<object> rawList =
                snapshot.GetValue<List<object>>(
                    field
                );


            List<string> result =
                new List<string>();


            if (rawList == null)
                return result;


            for (
                int i = 0;
                i < rawList.Count;
                i++)
            {
                object item =
                    rawList[i];


                if (item == null)
                    continue;


                string value =
                    item.ToString();


                if (!string.IsNullOrEmpty(value))
                {
                    result.Add(value);
                }
            }


            return result;
        }
        catch
        {
            return new List<string>();
        }
    }


    // =========================================================
    // READ MISSION STATES
    // =========================================================

    private List<MissionSaveData> ReadMissionStates(
        DocumentSnapshot snapshot,
        string field)
    {
        List<MissionSaveData> result =
            new List<MissionSaveData>();


        if (!snapshot.ContainsField(field))
        {
            return result;
        }


        try
        {
            List<object> rawList =
                snapshot.GetValue<List<object>>(
                    field
                );


            if (rawList == null)
                return result;


            for (
                int i = 0;
                i < rawList.Count;
                i++)
            {
                object rawItem =
                    rawList[i];


                if (
                    !(rawItem
                        is Dictionary<string, object> map)
                )
                {
                    continue;
                }


                MissionSaveData mission =
                    new MissionSaveData();


                if (
                    map.TryGetValue(
                        "id",
                        out object idValue
                    )
                )
                {
                    mission.id =
                        idValue != null
                            ? idValue.ToString()
                            : string.Empty;
                }


                if (
                    map.TryGetValue(
                        "progress",
                        out object progressValue
                    )
                )
                {
                    mission.progress =
                        ConvertFirestoreInt(
                            progressValue,
                            0
                        );
                }


                if (
                    map.TryGetValue(
                        "claimed",
                        out object claimedValue
                    )
                )
                {
                    if (
                        claimedValue
                        is bool boolValue
                    )
                    {
                        mission.claimed =
                            boolValue;
                    }
                    else
                    {
                        mission.claimed =
                            ConvertFirestoreInt(
                                claimedValue,
                                0
                            ) == 1;
                    }
                }


                if (
                    !string.IsNullOrEmpty(
                        mission.id
                    )
                )
                {
                    mission.progress =
                        Mathf.Max(
                            0,
                            mission.progress
                        );


                    result.Add(
                        mission
                    );
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[Firestore] ReadMissionStates failed: {exception}"
            );
        }


        return result;
    }


    // =========================================================
    // CONVERT FIRESTORE NUMBER
    // =========================================================

    private int ConvertFirestoreInt(
        object value,
        int defaultValue)
    {
        if (value == null)
            return defaultValue;


        try
        {
            if (value is long longValue)
                return ClampToInt(longValue);


            if (value is int intValue)
                return intValue;


            if (value is double doubleValue)
                return ClampToInt(
                    (long)doubleValue
                );


            if (value is float floatValue)
                return ClampToInt(
                    (long)floatValue
                );


            return Convert.ToInt32(value);
        }
        catch
        {
            return defaultValue;
        }
    }


    private int ClampToInt(long value)
    {
        if (value > int.MaxValue)
            return int.MaxValue;

        if (value < int.MinValue)
            return int.MinValue;

        return (int)value;
    }


    // =========================================================
    // CLEAR CURRENT ACCOUNT
    // =========================================================

    public void ClearCurrentPlayerData()
    {
        CurrentPlayerData = null;

        IsLoading = false;

        OnPlayerDataCleared?.Invoke();


        Debug.Log(
            "[Firestore] Current player data cleared."
        );
    }
}