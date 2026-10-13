using System.Collections;
using UnityEngine;
using Firebase.Auth;

public sealed class FirebaseAuthManager : MonoBehaviour
{
    // =====================================================================
    // SINGLETON
    // =====================================================================

    public static FirebaseAuthManager Instance { get; private set; }


    // =====================================================================
    // FIREBASE AUTH
    // =====================================================================

    public FirebaseAuth Auth { get; private set; }

    public FirebaseUser CurrentUser =>
        Auth != null ? Auth.CurrentUser : null;

    public bool IsReady { get; private set; }


    // =====================================================================
    // PLAYER DATA STATE
    // =====================================================================

    /// <summary>
    /// True khi Firebase Auth đã sẵn sàng và PlayerData của user hiện tại
    /// đã được yêu cầu load/create.
    /// </summary>
    public bool IsPlayerDataLoading { get; private set; }

    /// <summary>
    /// True khi quá trình load/create PlayerData đã hoàn thành.
    /// </summary>
    public bool IsPlayerDataReady { get; private set; }


    // =====================================================================
    // INTERNAL STATE
    // =====================================================================

    private Coroutine initializeCoroutine;
    private Coroutine playerDataCoroutine;

    private string loadedUserId;


    // =====================================================================
    // UNITY LIFECYCLE
    // =====================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        initializeCoroutine = StartCoroutine(
            WaitForFirebaseAndInitialize()
        );
    }


    // =====================================================================
    // WAIT FOR FIREBASE
    // =====================================================================

    private IEnumerator WaitForFirebaseAndInitialize()
    {
        Debug.Log(
            "[Firebase Auth] Đang chờ Firebase..."
        );

        // -------------------------------------------------------------
        // Wait for FirebaseManager
        // -------------------------------------------------------------

        while (FirebaseManager.Instance == null)
        {
            yield return null;
        }


        // -------------------------------------------------------------
        // Wait until FirebaseManager is ready
        // -------------------------------------------------------------

        while (!FirebaseManager.Instance.IsReady)
        {
            yield return null;
        }


        // -------------------------------------------------------------
        // Initialize Firebase Auth
        // -------------------------------------------------------------

        InitializeAuth();
    }


    // =====================================================================
    // INITIALIZE AUTH
    // =====================================================================

    private void InitializeAuth()
    {
        if (IsReady)
        {
            return;
        }

        try
        {
            Auth = FirebaseAuth.DefaultInstance;

            if (Auth == null)
            {
                Debug.LogError(
                    "[Firebase Auth] Không thể tạo FirebaseAuth."
                );

                return;
            }


            // ---------------------------------------------------------
            // Subscribe BEFORE checking CurrentUser.
            // ---------------------------------------------------------

            Auth.StateChanged += OnAuthStateChanged;


            IsReady = true;

            Debug.Log(
                "[Firebase Auth] Initialized successfully."
            );


            // ---------------------------------------------------------
            // IMPORTANT:
            //
            // CurrentUser may already exist before StateChanged fires.
            // Therefore we explicitly process it here.
            // ---------------------------------------------------------

            FirebaseUser currentUser = Auth.CurrentUser;

            if (currentUser != null)
            {
                Debug.Log(
                    $"[Firebase Auth] Existing user: {currentUser.UserId}"
                );

                HandleAuthenticatedUser(currentUser);
            }
            else
            {
                Debug.Log(
                    "[Firebase Auth] No user signed in."
                );
            }
        }
        catch (System.Exception exception)
        {
            IsReady = false;

            Debug.LogError(
                $"[Firebase Auth] Initialization failed: {exception}"
            );
        }
    }


    // =====================================================================
    // AUTH STATE CHANGED
    // =====================================================================

    private void OnAuthStateChanged(
        object sender,
        System.EventArgs eventArgs
    )
    {
        if (Auth == null)
        {
            return;
        }

        FirebaseUser user = Auth.CurrentUser;

        if (user != null)
        {
            Debug.Log(
                $"[Firebase Auth] User signed in: {user.UserId}"
            );

            HandleAuthenticatedUser(user);
        }
        else
        {
            Debug.Log(
                "[Firebase Auth] No user signed in."
            );

            HandleSignedOutUser();
        }
    }


    // =====================================================================
    // AUTHENTICATED USER
    // =====================================================================

    private void HandleAuthenticatedUser(FirebaseUser user)
    {
        if (user == null)
        {
            return;
        }

        string userId = user.UserId;

        // -------------------------------------------------------------
        // Already loaded this exact user.
        // -------------------------------------------------------------

        if (IsPlayerDataReady &&
            loadedUserId == userId)
        {
            Debug.Log(
                $"[Firebase Auth] PlayerData đã sẵn sàng: {userId}"
            );

            return;
        }


        // -------------------------------------------------------------
        // Prevent duplicate loading coroutine.
        // -------------------------------------------------------------

        if (IsPlayerDataLoading)
        {
            Debug.Log(
                $"[Firebase Auth] PlayerData đang được load: {userId}"
            );

            return;
        }


        LoadPlayerData(user);
    }


    // =====================================================================
    // LOAD PLAYER DATA
    // =====================================================================

    private void LoadPlayerData(FirebaseUser user)
    {
        if (user == null)
        {
            return;
        }

        if (FirestorePlayerDataManager.Instance == null)
        {
            Debug.LogError(
                "[Firebase Auth] Không tìm thấy " +
                "FirestorePlayerDataManager."
            );

            return;
        }


        if (playerDataCoroutine != null)
        {
            StopCoroutine(playerDataCoroutine);
        }

        playerDataCoroutine = StartCoroutine(
            LoadPlayerDataRoutine(user)
        );
    }


    // =====================================================================
    // PLAYER DATA ROUTINE
    // =====================================================================

    private IEnumerator LoadPlayerDataRoutine(FirebaseUser user)
    {
        if (user == null)
        {
            yield break;
        }


        IsPlayerDataLoading = true;
        IsPlayerDataReady = false;

        string userId = user.UserId;


        Debug.Log(
            $"[Firebase Auth] Đang load PlayerData: {userId}"
        );


        // -------------------------------------------------------------
        // Wait until FirestorePlayerDataManager exists.
        // -------------------------------------------------------------

        while (FirestorePlayerDataManager.Instance == null)
        {
            yield return null;
        }


        // -------------------------------------------------------------
        // IMPORTANT:
        //
        // Your FirestorePlayerDataManager initializes Firestore
        // independently.
        //
        // Wait one frame before invoking its async method to avoid
        // initialization-order race during scene startup.
        // -------------------------------------------------------------

        yield return null;


        // -------------------------------------------------------------
        // Create / Load PlayerData
        // -------------------------------------------------------------

        var task =
            FirestorePlayerDataManager.Instance
                .CreateOrLoadPlayerDataAsync(user);


        // -------------------------------------------------------------
        // Wait for Firebase Task
        // -------------------------------------------------------------

        while (!task.IsCompleted)
        {
            yield return null;
        }


        // -------------------------------------------------------------
        // Task failed
        // -------------------------------------------------------------

        if (task.IsCanceled)
        {
            IsPlayerDataLoading = false;

            Debug.LogError(
                "[Firebase Auth] Load PlayerData bị cancel."
            );

            yield break;
        }


        if (task.IsFaulted)
        {
            IsPlayerDataLoading = false;

            Debug.LogError(
                $"[Firebase Auth] Load PlayerData thất bại: " +
                $"{task.Exception}"
            );

            yield break;
        }


        // -------------------------------------------------------------
        // Verify the auth user has not changed during async operation.
        // -------------------------------------------------------------

        FirebaseUser currentUser =
            Auth != null
                ? Auth.CurrentUser
                : null;


        if (currentUser == null)
        {
            IsPlayerDataLoading = false;

            Debug.LogWarning(
                "[Firebase Auth] User đã đăng xuất trong lúc " +
                "đang load PlayerData."
            );

            yield break;
        }


        if (currentUser.UserId != userId)
        {
            IsPlayerDataLoading = false;

            Debug.LogWarning(
                "[Firebase Auth] User thay đổi trong lúc " +
                "đang load PlayerData."
            );

            yield break;
        }


        // -------------------------------------------------------------
        // Verify Firestore cache
        // -------------------------------------------------------------

        if (FirestorePlayerDataManager.Instance.CurrentPlayerData == null)
        {
            IsPlayerDataLoading = false;

            Debug.LogError(
                "[Firebase Auth] CreateOrLoadPlayerDataAsync hoàn tất " +
                "nhưng CurrentPlayerData vẫn NULL."
            );

            yield break;
        }


        // -------------------------------------------------------------
        // SUCCESS
        // -------------------------------------------------------------

        loadedUserId = userId;

        IsPlayerDataLoading = false;
        IsPlayerDataReady = true;


        Debug.Log(
            "[Firebase Auth] PlayerData READY | " +
            $"UID = {userId} | " +
            $"GEM = " +
            $"{FirestorePlayerDataManager.Instance.CurrentPlayerData.gems}"
        );
    }


    // =====================================================================
    // SIGNED OUT
    // =====================================================================

    private void HandleSignedOutUser()
    {
        loadedUserId = null;

        IsPlayerDataLoading = false;
        IsPlayerDataReady = false;

        if (playerDataCoroutine != null)
        {
            StopCoroutine(playerDataCoroutine);
            playerDataCoroutine = null;
        }

        Debug.Log(
            "[Firebase Auth] PlayerData state reset."
        );
    }


    // =====================================================================
    // CLEANUP
    // =====================================================================

    private void OnDestroy()
    {
        if (Auth != null)
        {
            Auth.StateChanged -= OnAuthStateChanged;
        }

        if (initializeCoroutine != null)
        {
            StopCoroutine(initializeCoroutine);
        }

        if (playerDataCoroutine != null)
        {
            StopCoroutine(playerDataCoroutine);
        }
    }
}