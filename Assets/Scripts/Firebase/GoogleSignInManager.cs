using System;
using System.Collections;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Firebase.Auth;

public sealed class GoogleSignInManager : MonoBehaviour
{
    public static GoogleSignInManager Instance { get; private set; }

    // ============================================================
    // GOOGLE OAUTH
    // ============================================================

    [Header("Google OAuth")]
    [SerializeField]
    private string clientId = "";

    [SerializeField]
    private string clientSecret = "";

    [SerializeField]
    private int callbackPort = 45837;

    [SerializeField]
    private string redirectPath = "/oauth2callback/";

    [Header("OAuth Endpoints")]
    [SerializeField]
    private string authorizationEndpoint =
        "https://accounts.google.com/o/oauth2/v2/auth";

    [SerializeField]
    private string tokenEndpoint =
        "https://oauth2.googleapis.com/token";

    // ============================================================
    // INTERNAL
    // ============================================================

    private HttpListener httpListener;

    private Thread listenerThread;

    private string authorizationCode;
    private string receivedState;

    private string codeVerifier;
    private string expectedState;

    private bool callbackReceived;
    private bool callbackFailed;

    private string callbackError;

    private bool isProcessing;

    // ============================================================
    // EVENTS
    // ============================================================

    public event Action<FirebaseUser> OnGoogleLoginSucceeded;

    public event Action<string> OnGoogleLoginFailed;

    // ============================================================
    // PROPERTIES
    // ============================================================

    public bool IsProcessing => isProcessing;

    private string RedirectUri
    {
        get
        {
            return
                $"http://127.0.0.1:{callbackPort}" +
                redirectPath;
        }
    }

    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        Debug.Log(
            "[Google Auth] GoogleSignInManager initialized."
        );
    }

    // ============================================================
    // LOGIN
    // ============================================================

    public void StartGoogleLogin(
        Action<FirebaseUser> successCallback,
        Action<string> failedCallback
    )
    {
        if (isProcessing)
        {
            Debug.LogWarning(
                "[Google Auth] Login already running."
            );

            return;
        }

        if (FirebaseAuthManager.Instance == null)
        {
            failedCallback?.Invoke(
                "Không tìm thấy FirebaseAuthManager."
            );

            return;
        }

        if (!FirebaseAuthManager.Instance.IsReady)
        {
            failedCallback?.Invoke(
                "Firebase Authentication chưa sẵn sàng."
            );

            return;
        }

        if (FirebaseAuthManager.Instance.Auth == null)
        {
            failedCallback?.Invoke(
                "FirebaseAuth chưa được khởi tạo."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            failedCallback?.Invoke(
                "Google Client ID chưa được cấu hình."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            failedCallback?.Invoke(
                "Google Client Secret chưa được cấu hình."
            );

            return;
        }

        if (callbackPort < 1024 ||
            callbackPort > 65535)
        {
            failedCallback?.Invoke(
                "Callback Port không hợp lệ."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(redirectPath))
        {
            failedCallback?.Invoke(
                "Redirect Path chưa được cấu hình."
            );

            return;
        }

        OnGoogleLoginSucceeded =
            successCallback;

        OnGoogleLoginFailed =
            failedCallback;

        StartCoroutine(
            GoogleLoginCoroutine()
        );
    }

    // ============================================================
    // MAIN GOOGLE FLOW
    // ============================================================

    private IEnumerator GoogleLoginCoroutine()
    {
        isProcessing = true;

        callbackReceived = false;
        callbackFailed = false;

        authorizationCode = null;
        receivedState = null;
        callbackError = null;

        codeVerifier =
            GenerateCodeVerifier();

        expectedState =
            GenerateRandomString(32);

        Debug.Log(
            "[Google Auth] Starting OAuth flow..."
        );

        Debug.Log(
            "[Google Auth] Client ID configured: " +
            !string.IsNullOrWhiteSpace(clientId)
        );

        Debug.Log(
            "[Google Auth] Client Secret configured: " +
            !string.IsNullOrWhiteSpace(clientSecret)
        );

        Debug.Log(
            "[Google Auth] Redirect URI: " +
            RedirectUri
        );

        StartCallbackListener();

        if (callbackFailed)
        {
            isProcessing = false;

            string listenerError =
                string.IsNullOrWhiteSpace(callbackError)
                    ? "Không thể khởi động OAuth callback listener."
                    : callbackError;

            OnGoogleLoginFailed?.Invoke(
                listenerError
            );

            yield break;
        }

        string authorizationUrl =
            BuildAuthorizationUrl();

        Debug.Log(
            "[Google Auth] Opening browser..."
        );

        Application.OpenURL(
            authorizationUrl
        );

        float timeout = 180f;
        float elapsed = 0f;

        while (!callbackReceived &&
               !callbackFailed &&
               elapsed < timeout)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        if (!callbackReceived)
        {
            StopCallbackListener();

            isProcessing = false;

            string error =
                callbackFailed
                    ? callbackError
                    : "Google Login timeout.";

            Debug.LogError(
                "[Google Auth] " +
                error
            );

            OnGoogleLoginFailed?.Invoke(
                ConvertGoogleErrorMessage(
                    error
                )
            );

            yield break;
        }

        StopCallbackListener();

        if (string.IsNullOrWhiteSpace(
                authorizationCode))
        {
            isProcessing = false;

            Debug.LogError(
                "[Google Auth] " +
                "Authorization code is empty."
            );

            OnGoogleLoginFailed?.Invoke(
                "Google không trả về authorization code."
            );

            yield break;
        }

        if (!string.Equals(
                receivedState,
                expectedState,
                StringComparison.Ordinal))
        {
            isProcessing = false;

            Debug.LogError(
                "[Google Auth] " +
                "OAuth state validation failed."
            );

            OnGoogleLoginFailed?.Invoke(
                "OAuth state không hợp lệ."
            );

            yield break;
        }

        yield return StartCoroutine(
            ExchangeCodeForFirebase(
                authorizationCode
            )
        );

        isProcessing = false;
    }

    // ============================================================
    // BUILD GOOGLE AUTHORIZATION URL
    // ============================================================

    private string BuildAuthorizationUrl()
    {
        string scope =
            Uri.EscapeDataString(
                "openid email profile"
            );

        string encodedRedirect =
            Uri.EscapeDataString(
                RedirectUri
            );

        string encodedState =
            Uri.EscapeDataString(
                expectedState
            );

        string codeChallenge =
            GenerateCodeChallenge(
                codeVerifier
            );

        string encodedChallenge =
            Uri.EscapeDataString(
                codeChallenge
            );

        StringBuilder builder =
            new StringBuilder(1024);

        builder.Append(
            authorizationEndpoint
        );

        builder.Append(
            "?client_id="
        );

        builder.Append(
            Uri.EscapeDataString(clientId)
        );

        builder.Append(
            "&redirect_uri="
        );

        builder.Append(
            encodedRedirect
        );

        builder.Append(
            "&response_type=code"
        );

        builder.Append(
            "&scope="
        );

        builder.Append(
            scope
        );

        builder.Append(
            "&state="
        );

        builder.Append(
            encodedState
        );

        builder.Append(
            "&code_challenge="
        );

        builder.Append(
            encodedChallenge
        );

        builder.Append(
            "&code_challenge_method=S256"
        );

        builder.Append(
            "&access_type=offline"
        );

        builder.Append(
            "&prompt=select_account"
        );

        return builder.ToString();
    }

    // ============================================================
    // LOCAL CALLBACK SERVER
    // ============================================================

    private void StartCallbackListener()
    {
        try
        {
            StopCallbackListener();

            httpListener =
                new HttpListener();

            httpListener.Prefixes.Add(
                RedirectUri
            );

            httpListener.Start();

            listenerThread =
                new Thread(
                    ListenForCallback
                );

            listenerThread.IsBackground =
                true;

            listenerThread.Start();

            Debug.Log(
                "[Google Auth] " +
                "Local OAuth callback listener started."
            );

            Debug.Log(
                "[Google Auth] Listening on: " +
                RedirectUri
            );
        }
        catch (Exception exception)
        {
            callbackFailed = true;

            callbackError =
                exception.Message;

            Debug.LogError(
                "[Google Auth] " +
                $"Cannot start callback listener: {exception}"
            );
        }
    }

    private void ListenForCallback()
    {
        try
        {
            HttpListenerContext context =
                httpListener.GetContext();

            HttpListenerRequest request =
                context.Request;

            string code =
                request.QueryString["code"];

            string state =
                request.QueryString["state"];

            string error =
                request.QueryString["error"];

            string errorDescription =
                request.QueryString[
                    "error_description"
                ];

            if (!string.IsNullOrEmpty(error))
            {
                callbackError =
                    string.IsNullOrWhiteSpace(
                        errorDescription
                    )
                        ? error
                        : $"{error}: {errorDescription}";

                callbackFailed = true;

                SendBrowserResponse(
                    context,
                    "Đăng nhập Google đã bị hủy."
                );

                return;
            }

            if (string.IsNullOrEmpty(code))
            {
                callbackError =
                    "Google không trả về authorization code.";

                callbackFailed = true;

                SendBrowserResponse(
                    context,
                    "Google Login thất bại."
                );

                return;
            }

            if (!string.Equals(
                    state,
                    expectedState,
                    StringComparison.Ordinal))
            {
                callbackError =
                    "OAuth state validation failed.";

                callbackFailed = true;

                SendBrowserResponse(
                    context,
                    "OAuth validation failed."
                );

                return;
            }

            authorizationCode =
                code;

            receivedState =
                state;

            callbackReceived = true;

            SendBrowserResponse(
                context,
                "Đăng nhập Google thành công. Bạn có thể đóng cửa sổ này và quay lại game."
            );
        }
        catch (HttpListenerException exception)
        {
            if (isProcessing)
            {
                callbackError =
                    exception.Message;

                callbackFailed = true;
            }
        }
        catch (ObjectDisposedException)
        {
            // Listener đã được đóng.
        }
        catch (Exception exception)
        {
            callbackError =
                exception.Message;

            callbackFailed = true;

            Debug.LogError(
                "[Google Auth] Callback listener exception: " +
                exception
            );
        }
    }

    // ============================================================
    // BROWSER RESPONSE
    // ============================================================

    private void SendBrowserResponse(
        HttpListenerContext context,
        string message
    )
    {
        try
        {
            string html =
                "<html>" +
                "<head>" +
                "<meta charset='utf-8'>" +
                "<title>LEAD KHÔNG PHANH</title>" +
                "</head>" +
                "<body style='font-family:Arial;text-align:center;padding-top:80px;'>" +
                $"<h1>{message}</h1>" +
                "<p>Bạn có thể quay lại game.</p>" +
                "</body>" +
                "</html>";

            byte[] buffer =
                Encoding.UTF8.GetBytes(html);

            context.Response.ContentType =
                "text/html; charset=utf-8";

            context.Response.ContentLength64 =
                buffer.Length;

            context.Response.StatusCode = 200;

            context.Response.OutputStream.Write(
                buffer,
                0,
                buffer.Length
            );

            context.Response.OutputStream.Close();
        }
        catch
        {
            // Callback đã nhận được.
        }
    }

    // ============================================================
    // TOKEN EXCHANGE
    // ============================================================

    private IEnumerator ExchangeCodeForFirebase(
        string code
    )
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            Debug.LogError(
                "[Google Auth] Authorization code is empty."
            );

            OnGoogleLoginFailed?.Invoke(
                "Authorization code không hợp lệ."
            );

            yield break;
        }

        if (string.IsNullOrWhiteSpace(codeVerifier))
        {
            Debug.LogError(
                "[Google Auth] PKCE code verifier is empty."
            );

            OnGoogleLoginFailed?.Invoke(
                "PKCE verifier không hợp lệ."
            );

            yield break;
        }

        Debug.Log(
            "[Google Auth] " +
            "Exchanging authorization code for Google tokens..."
        );

        WWWForm form =
            new WWWForm();

        form.AddField(
            "client_id",
            clientId
        );

        form.AddField(
            "client_secret",
            clientSecret
        );

        form.AddField(
            "code",
            code
        );

        form.AddField(
            "code_verifier",
            codeVerifier
        );

        form.AddField(
            "redirect_uri",
            RedirectUri
        );

        form.AddField(
            "grant_type",
            "authorization_code"
        );

        using UnityWebRequest request =
            UnityWebRequest.Post(
                tokenEndpoint,
                form
            );

        request.timeout = 30;

        yield return request.SendWebRequest();

        string responseBody =
            request.downloadHandler != null
                ? request.downloadHandler.text
                : string.Empty;

        if (request.result !=
            UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                "[Google Auth] Token exchange failed: " +
                request.error
            );

            Debug.LogError(
                "[Google Auth] HTTP status: " +
                request.responseCode
            );

            Debug.LogError(
                "[Google Auth] Google token response: " +
                responseBody
            );

            GoogleTokenError tokenError = null;

            if (!string.IsNullOrWhiteSpace(
                    responseBody))
            {
                try
                {
                    tokenError =
                        JsonUtility.FromJson<GoogleTokenError>(
                            responseBody
                        );
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        "[Google Auth] " +
                        "Cannot parse Google error response: " +
                        exception
                    );
                }
            }

            if (tokenError != null)
            {
                Debug.LogError(
                    "[Google Auth] Google error: " +
                    tokenError.error
                );

                Debug.LogError(
                    "[Google Auth] " +
                    "Google error description: " +
                    tokenError.error_description
                );
            }

            OnGoogleLoginFailed?.Invoke(
                BuildTokenExchangeErrorMessage(
                    tokenError
                )
            );

            yield break;
        }

        Debug.Log(
            "[Google Auth] " +
            "Google token exchange SUCCESS."
        );

        GoogleTokenResponse tokenResponse = null;

        try
        {
            tokenResponse =
                JsonUtility.FromJson<GoogleTokenResponse>(
                    responseBody
                );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[Google Auth] " +
                $"Cannot parse token response: {exception}"
            );

            Debug.LogError(
                "[Google Auth] Raw token response: " +
                responseBody
            );

            OnGoogleLoginFailed?.Invoke(
                "Không đọc được phản hồi Google."
            );

            yield break;
        }

        if (tokenResponse == null ||
            string.IsNullOrWhiteSpace(
                tokenResponse.id_token))
        {
            Debug.LogError(
                "[Google Auth] " +
                "Google không trả về ID token."
            );

            OnGoogleLoginFailed?.Invoke(
                "Google không trả về ID token."
            );

            yield break;
        }

        Debug.Log(
            "[Google Auth] " +
            "ID token received successfully."
        );

        yield return StartCoroutine(
            SignInFirebase(
                tokenResponse.id_token,
                tokenResponse.access_token
            )
        );
    }

    // ============================================================
    // TOKEN ERROR
    // ============================================================

    private string BuildTokenExchangeErrorMessage(
        GoogleTokenError tokenError
    )
    {
        if (tokenError == null)
        {
            return
                "Không thể nhận token Google.";
        }

        if (string.Equals(
                tokenError.error,
                "invalid_grant",
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "Google từ chối authorization code hoặc PKCE verifier.";
        }

        if (string.Equals(
                tokenError.error,
                "invalid_client",
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "Google Client ID hoặc Client Secret không hợp lệ.";
        }

        if (string.Equals(
                tokenError.error,
                "invalid_request",
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "Google từ chối yêu cầu token.";
        }

        if (string.Equals(
                tokenError.error,
                "redirect_uri_mismatch",
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "Redirect URI không khớp cấu hình OAuth.";
        }

        if (!string.IsNullOrWhiteSpace(
                tokenError.error_description))
        {
            return
                $"Google Token Error: {tokenError.error_description}";
        }

        return
            "Không thể nhận token Google.";
    }

    // ============================================================
    // FIREBASE SIGN-IN
    // ============================================================

    private IEnumerator SignInFirebase(
        string googleIdToken,
        string googleAccessToken
    )
    {
        if (string.IsNullOrWhiteSpace(
                googleIdToken))
        {
            Debug.LogError(
                "[Google Auth] Google ID token is empty."
            );

            OnGoogleLoginFailed?.Invoke(
                "Google ID token không hợp lệ."
            );

            yield break;
        }

        Debug.Log(
            "[Google Auth] " +
            "Đang đăng nhập Google credential vào Firebase..."
        );

        Credential credential =
            GoogleAuthProvider.GetCredential(
                googleIdToken,
                googleAccessToken
            );

        var task =
            FirebaseAuthManager.Instance.Auth
                .SignInWithCredentialAsync(
                    credential
                );

        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsCanceled)
        {
            Debug.LogError(
                "[Google Auth] Firebase Google login canceled."
            );

            OnGoogleLoginFailed?.Invoke(
                "Đăng nhập Google đã bị hủy."
            );

            yield break;
        }

        if (task.IsFaulted)
        {
            Debug.LogError(
                "[Google Auth] Firebase Google login failed."
            );

            Debug.LogError(
                task.Exception
            );

            OnGoogleLoginFailed?.Invoke(
                "Firebase không thể xác thực tài khoản Google."
            );

            yield break;
        }

        FirebaseUser user =
            task.Result;

        if (user == null)
        {
            Debug.LogError(
                "[Google Auth] " +
                "Firebase không trả về FirebaseUser."
            );

            OnGoogleLoginFailed?.Invoke(
                "Firebase không trả về tài khoản."
            );

            yield break;
        }

        Debug.Log(
            "[Google Auth] " +
            "Google Firebase login SUCCESS."
        );

        Debug.Log(
            $"[Google Auth] UID = {user.UserId}"
        );

        Debug.Log(
            $"[Google Auth] Email = {user.Email}"
        );

        Debug.Log(
            $"[Google Auth] Name = {user.DisplayName}"
        );

        OnGoogleLoginSucceeded?.Invoke(
            user
        );
    }

    // ============================================================
    // STOP LISTENER
    // ============================================================

    private void StopCallbackListener()
    {
        try
        {
            if (httpListener != null)
            {
                if (httpListener.IsListening)
                {
                    httpListener.Stop();
                }

                httpListener.Close();

                httpListener = null;
            }
        }
        catch
        {
            httpListener = null;
        }

        listenerThread = null;
    }

    // ============================================================
    // PKCE
    // ============================================================

    private string GenerateCodeVerifier()
    {
        byte[] bytes =
            new byte[32];

        using RandomNumberGenerator rng =
            RandomNumberGenerator.Create();

        rng.GetBytes(bytes);

        return Base64UrlEncode(bytes);
    }

    private string GenerateCodeChallenge(
        string verifier
    )
    {
        byte[] bytes =
            Encoding.ASCII.GetBytes(verifier);

        using SHA256 sha256 =
            SHA256.Create();

        byte[] hash =
            sha256.ComputeHash(bytes);

        return Base64UrlEncode(hash);
    }

    private string GenerateRandomString(
        int length
    )
    {
        byte[] bytes =
            new byte[length];

        using RandomNumberGenerator rng =
            RandomNumberGenerator.Create();

        rng.GetBytes(bytes);

        return Base64UrlEncode(bytes);
    }

    private string Base64UrlEncode(
        byte[] bytes
    )
    {
        return Convert
            .ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    // ============================================================
    // ERROR
    // ============================================================

    private string ConvertGoogleErrorMessage(
        string error
    )
    {
        if (string.IsNullOrEmpty(error))
        {
            return
                "Đăng nhập Google thất bại.";
        }

        if (error.Contains("access_denied"))
        {
            return
                "Bạn đã hủy đăng nhập Google.";
        }

        if (error.Contains("timeout"))
        {
            return
                "Google Login hết thời gian chờ.";
        }

        if (error.Contains("state"))
        {
            return
                "OAuth state không hợp lệ.";
        }

        return
            $"Google Login thất bại: {error}";
    }

    // ============================================================
    // CLEANUP
    // ============================================================

    private void OnDestroy()
    {
        StopCallbackListener();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ============================================================
    // TOKEN RESPONSE
    // ============================================================

    [Serializable]
    private sealed class GoogleTokenResponse
    {
        public string access_token;
        public string id_token;
        public string token_type;
        public int expires_in;
        public string scope;
        public string refresh_token;
    }

    // ============================================================
    // TOKEN ERROR RESPONSE
    // ============================================================

    [Serializable]
    private sealed class GoogleTokenError
    {
        public string error;
        public string error_description;
        public string error_uri;
    }
}