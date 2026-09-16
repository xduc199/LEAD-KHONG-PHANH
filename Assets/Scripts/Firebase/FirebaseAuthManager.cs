using System.Collections;
using UnityEngine;
using Firebase.Auth;

public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance { get; private set; }

    public FirebaseAuth Auth { get; private set; }

    public FirebaseUser CurrentUser =>
        Auth != null ? Auth.CurrentUser : null;

    public bool IsReady { get; private set; }

    private Coroutine initializeCoroutine;

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

    private IEnumerator WaitForFirebaseAndInitialize()
    {
        Debug.Log(
            "[Firebase Auth] Đang chờ Firebase..."
        );

        // Chờ FirebaseManager tồn tại
        while (FirebaseManager.Instance == null)
        {
            yield return null;
        }

        // Chờ FirebaseManager hoàn tất initialization
        while (!FirebaseManager.Instance.IsReady)
        {
            yield return null;
        }

        InitializeAuth();
    }

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

            Auth.StateChanged += OnAuthStateChanged;

            IsReady = true;

            Debug.Log(
                "[Firebase Auth] Initialized successfully."
            );

            FirebaseUser currentUser = Auth.CurrentUser;

            if (currentUser != null)
            {
                Debug.Log(
                    $"[Firebase Auth] Existing user: {currentUser.UserId}"
                );
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
        }
        else
        {
            Debug.Log(
                "[Firebase Auth] No user signed in."
            );
        }
    }

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
    }
}