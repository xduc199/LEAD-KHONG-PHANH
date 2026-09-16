using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

using Firebase.Auth;
using Firebase;

public class LoginUIController : MonoBehaviour
{
    // ============================================================
    // SCENE
    // ============================================================

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";


    // ============================================================
    // INPUT
    // ============================================================

    [Header("Input")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;


    // ============================================================
    // BUTTONS
    // ============================================================

    [Header("Buttons")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button googleLoginButton;


    // ============================================================
    // PASSWORD TOGGLE
    // ============================================================

    [Header("Password Toggle")]
    [SerializeField] private Button passwordToggleButton;


    // ============================================================
    // STATUS
    // ============================================================

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;


    // ============================================================
    // INTERNAL
    // ============================================================

    private bool isPasswordVisible;
    private bool isProcessing;
    private bool isLoadingMainMenu;
    private bool listenersBound;


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        AutoFindReferences();

        Debug.Log(
            "[Login UI] Awake() - References đã được tìm."
        );

        LogReferences();

        SetPasswordVisible(false);
    }


    // ============================================================
    // ON ENABLE
    // ============================================================

    private void OnEnable()
    {
        Debug.Log(
            "[Login UI] OnEnable() - Đang bind button listeners."
        );

        isProcessing = false;
        isLoadingMainMenu = false;

        BindButtonListeners();
    }


    // ============================================================
    // ON DISABLE
    // ============================================================

    private void OnDisable()
    {
        UnbindButtonListeners();
    }


    // ============================================================
    // DESTROY
    // ============================================================

    private void OnDestroy()
    {
        UnbindButtonListeners();
    }


    // ============================================================
    // BIND LISTENERS
    // ============================================================

    private void BindButtonListeners()
    {
        AutoFindReferences();

        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(
                OnLoginClicked
            );

            loginButton.onClick.AddListener(
                OnLoginClicked
            );

            Debug.Log(
                "[Login UI] LoginButton listener đã bind."
            );
        }
        else
        {
            Debug.LogError(
                "[Login UI] LoginButton = NULL."
            );
        }


        if (registerButton != null)
        {
            registerButton.onClick.RemoveListener(
                OnRegisterClicked
            );

            registerButton.onClick.AddListener(
                OnRegisterClicked
            );

            Debug.Log(
                "[Login UI] RegisterButton listener đã bind."
            );
        }
        else
        {
            Debug.LogError(
                "[Login UI] RegisterButton = NULL."
            );
        }


        if (googleLoginButton != null)
        {
            googleLoginButton.onClick.RemoveListener(
                OnGoogleLoginClicked
            );

            googleLoginButton.onClick.AddListener(
                OnGoogleLoginClicked
            );

            Debug.Log(
                "[Login UI] GoogleLoginButton listener đã bind."
            );
        }
        else
        {
            Debug.LogError(
                "[Login UI] GoogleLoginButton = NULL."
            );
        }


        if (passwordToggleButton != null)
        {
            passwordToggleButton.onClick.RemoveListener(
                OnPasswordToggleClicked
            );

            passwordToggleButton.onClick.AddListener(
                OnPasswordToggleClicked
            );

            Debug.Log(
                "[Login UI] PasswordToggleButton listener đã bind."
            );
        }

        listenersBound = true;
    }


    // ============================================================
    // UNBIND LISTENERS
    // ============================================================

    private void UnbindButtonListeners()
    {
        if (!listenersBound)
            return;


        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(
                OnLoginClicked
            );
        }


        if (registerButton != null)
        {
            registerButton.onClick.RemoveListener(
                OnRegisterClicked
            );
        }


        if (googleLoginButton != null)
        {
            googleLoginButton.onClick.RemoveListener(
                OnGoogleLoginClicked
            );
        }


        if (passwordToggleButton != null)
        {
            passwordToggleButton.onClick.RemoveListener(
                OnPasswordToggleClicked
            );
        }


        listenersBound = false;

        Debug.Log(
            "[Login UI] Button listeners đã unbind."
        );
    }


    // ============================================================
    // FIND REFERENCES
    // ============================================================

    private void AutoFindReferences()
    {
        if (emailInput == null)
        {
            GameObject obj =
                GameObject.Find("EmailInput");

            if (obj != null)
            {
                emailInput =
                    obj.GetComponent<TMP_InputField>();
            }
        }


        if (passwordInput == null)
        {
            GameObject obj =
                GameObject.Find("PasswordInput");

            if (obj != null)
            {
                passwordInput =
                    obj.GetComponent<TMP_InputField>();
            }
        }


        if (loginButton == null)
        {
            GameObject obj =
                GameObject.Find("LoginButton");

            if (obj != null)
            {
                loginButton =
                    obj.GetComponent<Button>();
            }
        }


        if (registerButton == null)
        {
            GameObject obj =
                GameObject.Find("RegisterButton");

            if (obj != null)
            {
                registerButton =
                    obj.GetComponent<Button>();
            }
        }


        if (googleLoginButton == null)
        {
            GameObject obj =
                GameObject.Find("GoogleLoginButton");

            if (obj != null)
            {
                googleLoginButton =
                    obj.GetComponent<Button>();
            }
        }


        if (passwordToggleButton == null)
        {
            GameObject obj =
                GameObject.Find("PasswordToggleButton");

            if (obj != null)
            {
                passwordToggleButton =
                    obj.GetComponent<Button>();
            }
        }


        if (statusText == null)
        {
            GameObject obj =
                GameObject.Find("StatusText");

            if (obj != null)
            {
                statusText =
                    obj.GetComponent<TMP_Text>();
            }
        }
    }


    // ============================================================
    // DEBUG REFERENCES
    // ============================================================

    private void LogReferences()
    {
        Debug.Log(
            "[Login UI] " +
            $"EmailInput = {emailInput != null}"
        );

        Debug.Log(
            "[Login UI] " +
            $"PasswordInput = {passwordInput != null}"
        );

        Debug.Log(
            "[Login UI] " +
            $"LoginButton = {loginButton != null}"
        );

        Debug.Log(
            "[Login UI] " +
            $"RegisterButton = {registerButton != null}"
        );

        Debug.Log(
            "[Login UI] " +
            $"GoogleLoginButton = {googleLoginButton != null}"
        );

        Debug.Log(
            "[Login UI] " +
            $"PasswordToggleButton = " +
            $"{passwordToggleButton != null}"
        );

        Debug.Log(
            "[Login UI] " +
            $"StatusText = {statusText != null}"
        );
    }


    // ============================================================
    // LOGIN
    // ============================================================

    private async void OnLoginClicked()
    {
        Debug.Log(
            "[Login] >>> LOGIN BUTTON CLICKED <<<"
        );


        if (isProcessing)
        {
            Debug.LogWarning(
                "[Login] Bỏ qua: isProcessing = true."
            );

            return;
        }


        if (isLoadingMainMenu)
        {
            Debug.LogWarning(
                "[Login] Bỏ qua: isLoadingMainMenu = true."
            );

            return;
        }


        string email =
            emailInput != null
                ? emailInput.text.Trim()
                : string.Empty;

        string password =
            passwordInput != null
                ? passwordInput.text
                : string.Empty;


        Debug.Log(
            $"[Login] Email entered = {!string.IsNullOrEmpty(email)}"
        );

        Debug.Log(
            $"[Login] Password entered = {!string.IsNullOrEmpty(password)}"
        );


        if (string.IsNullOrEmpty(email))
        {
            SetStatus(
                "Vui lòng nhập email."
            );

            return;
        }


        if (string.IsNullOrEmpty(password))
        {
            SetStatus(
                "Vui lòng nhập mật khẩu."
            );

            return;
        }


        if (!IsFirebaseAuthReady())
        {
            SetStatus(
                "Firebase chưa sẵn sàng."
            );

            return;
        }


        if (!IsFirestoreReady())
        {
            SetStatus(
                "Firestore chưa sẵn sàng."
            );

            return;
        }


        isProcessing = true;

        SetButtonsInteractable(false);

        SetStatus(
            "ĐANG ĐĂNG NHẬP..."
        );


        try
        {
            Debug.Log(
                "[Login] Đang gọi Firebase " +
                "SignInWithEmailAndPasswordAsync..."
            );


            AuthResult result =
                await FirebaseAuthManager.Instance.Auth
                    .SignInWithEmailAndPasswordAsync(
                        email,
                        password
                    );


            if (result == null ||
                result.User == null)
            {
                Debug.LogError(
                    "[Login] Firebase trả về result/user = null."
                );

                SetStatus(
                    "Đăng nhập thất bại."
                );

                return;
            }


            FirebaseUser user =
                result.User;


            Debug.Log(
                "[Login] Đăng nhập Firebase thành công."
            );

            Debug.Log(
                $"[Login] UID: {user.UserId}"
            );

            Debug.Log(
                $"[Login] Email: {user.Email}"
            );


            SetStatus(
                "ĐANG TẢI DỮ LIỆU NGƯỜI CHƠI..."
            );


            PlayerData playerData =
                await FirestorePlayerDataManager.Instance
                    .CreateOrLoadPlayerDataAsync(
                        user
                    );


            if (playerData == null)
            {
                Debug.LogError(
                    "[Login] Firebase Auth thành công " +
                    "nhưng không tải được PlayerData."
                );

                SetStatus(
                    "Đăng nhập thành công nhưng " +
                    "không tải được dữ liệu game."
                );

                return;
            }


            Debug.Log(
                "[Login] PlayerData loaded successfully."
            );

            Debug.Log(
                $"[Login] Player Name: " +
                $"{playerData.displayName}"
            );

            Debug.Log(
                $"[Login] Level: " +
                $"{playerData.level}"
            );

            Debug.Log(
                $"[Login] EXP: " +
                $"{playerData.exp}"
            );

            Debug.Log(
                $"[Login] Coins: " +
                $"{playerData.coins}"
            );

            Debug.Log(
                $"[Login] Best Score: " +
                $"{playerData.bestScore}"
            );

            Debug.Log(
                $"[Login] Selected Bike: " +
                $"{playerData.selectedBike}"
            );


            SetStatus(
                "ĐĂNG NHẬP THÀNH CÔNG!"
            );


            LoadMainMenu();
        }
        catch (FirebaseException exception)
        {
            Debug.LogError(
                $"[Login] Firebase Error Code: " +
                $"{exception.ErrorCode}"
            );

            Debug.LogError(
                $"[Login] Firebase Error Message: " +
                $"{exception.Message}"
            );

            Debug.LogError(
                $"[Login] Firebase Full Exception: " +
                $"{exception}"
            );

            SetStatus(
                GetFirebaseErrorMessage(
                    exception
                )
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                $"[Login] Login failed: {exception}"
            );

            SetStatus(
                "Đăng nhập thất bại."
            );
        }
        finally
        {
            isProcessing = false;

            if (!isLoadingMainMenu)
            {
                SetButtonsInteractable(true);
            }
        }
    }


    // ============================================================
    // REGISTER
    // ============================================================

    private async void OnRegisterClicked()
    {
        Debug.Log(
            "[Register] >>> REGISTER BUTTON CLICKED <<<"
        );


        if (isProcessing)
        {
            Debug.LogWarning(
                "[Register] Bỏ qua: isProcessing = true."
            );

            return;
        }


        if (isLoadingMainMenu)
        {
            Debug.LogWarning(
                "[Register] Bỏ qua: isLoadingMainMenu = true."
            );

            return;
        }


        string email =
            emailInput != null
                ? emailInput.text.Trim()
                : string.Empty;

        string password =
            passwordInput != null
                ? passwordInput.text
                : string.Empty;


        if (string.IsNullOrEmpty(email))
        {
            SetStatus(
                "Vui lòng nhập email."
            );

            return;
        }


        if (string.IsNullOrEmpty(password))
        {
            SetStatus(
                "Vui lòng nhập mật khẩu."
            );

            return;
        }


        if (password.Length < 6)
        {
            SetStatus(
                "Mật khẩu phải có ít nhất 6 ký tự."
            );

            return;
        }


        if (!IsFirebaseAuthReady())
        {
            SetStatus(
                "Firebase chưa sẵn sàng."
            );

            return;
        }


        if (!IsFirestoreReady())
        {
            SetStatus(
                "Firestore chưa sẵn sàng."
            );

            return;
        }


        isProcessing = true;

        SetButtonsInteractable(false);

        SetStatus(
            "ĐANG TẠO TÀI KHOẢN..."
        );


        try
        {
            AuthResult result =
                await FirebaseAuthManager.Instance.Auth
                    .CreateUserWithEmailAndPasswordAsync(
                        email,
                        password
                    );


            if (result == null ||
                result.User == null)
            {
                SetStatus(
                    "Không thể tạo tài khoản."
                );

                return;
            }


            FirebaseUser user =
                result.User;


            Debug.Log(
                "[Register] " +
                "Tạo tài khoản Firebase thành công."
            );

            Debug.Log(
                $"[Register] UID: {user.UserId}"
            );

            Debug.Log(
                $"[Register] Email: {user.Email}"
            );


            SetStatus(
                "ĐANG TẠO DỮ LIỆU NGƯỜI CHƠI..."
            );


            PlayerData playerData =
                await FirestorePlayerDataManager.Instance
                    .CreateOrLoadPlayerDataAsync(
                        user
                    );


            if (playerData == null)
            {
                Debug.LogError(
                    "[Register] Tài khoản Firebase " +
                    "đã tạo nhưng PlayerData thất bại."
                );

                SetStatus(
                    "Tạo tài khoản thành công nhưng " +
                    "không tạo được dữ liệu game."
                );

                return;
            }


            Debug.Log(
                "[Register] " +
                "PlayerData created successfully."
            );

            Debug.Log(
                $"[Register] Player Name: " +
                $"{playerData.displayName}"
            );

            Debug.Log(
                $"[Register] Level: " +
                $"{playerData.level}"
            );

            Debug.Log(
                $"[Register] EXP: " +
                $"{playerData.exp}"
            );

            Debug.Log(
                $"[Register] Coins: " +
                $"{playerData.coins}"
            );

            Debug.Log(
                $"[Register] Best Score: " +
                $"{playerData.bestScore}"
            );

            Debug.Log(
                $"[Register] Selected Bike: " +
                $"{playerData.selectedBike}"
            );


            SetStatus(
                "TẠO TÀI KHOẢN THÀNH CÔNG!"
            );


            LoadMainMenu();
        }
        catch (FirebaseException exception)
        {
            Debug.LogError(
                $"[Register] Firebase Error Code: " +
                $"{exception.ErrorCode}"
            );

            Debug.LogError(
                $"[Register] Firebase Error Message: " +
                $"{exception.Message}"
            );

            Debug.LogError(
                $"[Register] Firebase Full Exception: " +
                $"{exception}"
            );

            SetStatus(
                GetFirebaseErrorMessage(
                    exception
                )
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                $"[Register] Register failed: " +
                $"{exception}"
            );

            SetStatus(
                "Đăng ký thất bại."
            );
        }
        finally
        {
            isProcessing = false;

            if (!isLoadingMainMenu)
            {
                SetButtonsInteractable(true);
            }
        }
    }


    // ============================================================
    // LOAD MAIN MENU
    // ============================================================

    private void LoadMainMenu()
    {
        if (isLoadingMainMenu)
            return;


        if (string.IsNullOrWhiteSpace(
                mainMenuSceneName))
        {
            Debug.LogError(
                "[Login] MainMenu Scene Name đang trống."
            );

            SetStatus(
                "Chưa cấu hình MainMenu."
            );

            return;
        }


        bool canLoad =
            Application.CanStreamedLevelBeLoaded(
                mainMenuSceneName
            );


        Debug.Log(
            "[Login] " +
            $"CanLoad MainMenu = {canLoad} | " +
            $"Scene='{mainMenuSceneName}'"
        );


        if (!canLoad)
        {
            Debug.LogError(
                $"[Login] Không thể load Scene " +
                $"'{mainMenuSceneName}'. " +
                "Kiểm tra Build Profiles."
            );

            SetStatus(
                "Không thể mở Main Menu."
            );

            return;
        }


        isLoadingMainMenu = true;

        Time.timeScale = 1f;

        SetButtonsInteractable(false);


        Debug.Log(
            "[Login] LOGIN SUCCESS → " +
            "LOADING MAIN MENU..."
        );


        SceneManager.LoadScene(
            mainMenuSceneName
        );
    }


    // ============================================================
    // PASSWORD TOGGLE
    // ============================================================

    private void OnPasswordToggleClicked()
    {
        Debug.Log(
            "[Login UI] Password Toggle clicked."
        );

        SetPasswordVisible(
            !isPasswordVisible
        );
    }


    private void SetPasswordVisible(
        bool visible
    )
    {
        if (passwordInput == null)
        {
            Debug.LogError(
                "[Login UI] passwordInput chưa được gán."
            );

            return;
        }


        isPasswordVisible = visible;

        string currentText =
            passwordInput.text;

        int caretPosition =
            passwordInput.caretPosition;


        passwordInput.contentType =
            visible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;


        passwordInput.SetTextWithoutNotify(
            currentText
        );

        passwordInput.ForceLabelUpdate();


        caretPosition =
            Mathf.Clamp(
                caretPosition,
                0,
                currentText.Length
            );


        passwordInput.caretPosition =
            caretPosition;

        passwordInput.selectionAnchorPosition =
            caretPosition;

        passwordInput.selectionFocusPosition =
            caretPosition;


        Debug.Log(
            $"[Login UI] Password visible: {visible}"
        );
    }


    // ============================================================
    // GOOGLE LOGIN
    // ============================================================

    private void OnGoogleLoginClicked()
    {
        Debug.Log(
            "[Login] >>> GOOGLE LOGIN BUTTON CLICKED <<<"
        );


        if (isProcessing)
        {
            Debug.LogWarning(
                "[Login] Google Login bị bỏ qua: " +
                "isProcessing = true."
            );

            return;
        }


        if (isLoadingMainMenu)
        {
            Debug.LogWarning(
                "[Login] Google Login bị bỏ qua: " +
                "isLoadingMainMenu = true."
            );

            return;
        }


        if (!IsFirebaseAuthReady())
        {
            SetStatus(
                "Firebase chưa sẵn sàng."
            );

            return;
        }


        if (!IsFirestoreReady())
        {
            SetStatus(
                "Firestore chưa sẵn sàng."
            );

            return;
        }


        GoogleSignInManager googleManager =
            GoogleSignInManager.Instance;


        if (googleManager == null)
        {
            Debug.LogError(
                "[Login] Không tìm thấy " +
                "GoogleSignInManager."
            );

            SetStatus(
                "Google Login chưa được cấu hình."
            );

            return;
        }


        isProcessing = true;

        SetButtonsInteractable(false);

        SetStatus(
            "ĐANG MỞ GOOGLE..."
        );


        Debug.Log(
            "[Login] " +
            "Bắt đầu Google OAuth..."
        );


        googleManager.StartGoogleLogin(
            OnGoogleLoginSucceeded,
            OnGoogleLoginFailed
        );
    }


    // ============================================================
    // GOOGLE LOGIN SUCCESS
    // ============================================================

    private async void OnGoogleLoginSucceeded(
        FirebaseUser user
    )
    {
        Debug.Log(
            "[Login] " +
            "Google OAuth + Firebase Auth SUCCESS."
        );


        if (user == null)
        {
            Debug.LogError(
                "[Login] Google Login thành công " +
                "nhưng FirebaseUser = null."
            );

            HandleGoogleLoginFinished(
                "Google đăng nhập thành công nhưng " +
                "không nhận được tài khoản Firebase."
            );

            return;
        }


        Debug.Log(
            $"[Login] Google UID: {user.UserId}"
        );

        Debug.Log(
            $"[Login] Google Email: {user.Email}"
        );

        Debug.Log(
            $"[Login] Google Name: {user.DisplayName}"
        );


        try
        {
            SetStatus(
                "ĐANG TẢI DỮ LIỆU NGƯỜI CHƠI..."
            );


            PlayerData playerData =
                await FirestorePlayerDataManager.Instance
                    .CreateOrLoadPlayerDataAsync(
                        user
                    );


            if (playerData == null)
            {
                Debug.LogError(
                    "[Login] Google Auth thành công " +
                    "nhưng không tải được PlayerData."
                );

                HandleGoogleLoginFinished(
                    "Đăng nhập Google thành công nhưng " +
                    "không tải được dữ liệu game."
                );

                return;
            }


            Debug.Log(
                "[Login] " +
                "Google PlayerData loaded successfully."
            );

            Debug.Log(
                $"[Login] Player Name: " +
                $"{playerData.displayName}"
            );

            Debug.Log(
                $"[Login] Level: " +
                $"{playerData.level}"
            );

            Debug.Log(
                $"[Login] EXP: " +
                $"{playerData.exp}"
            );

            Debug.Log(
                $"[Login] Coins: " +
                $"{playerData.coins}"
            );

            Debug.Log(
                $"[Login] Best Score: " +
                $"{playerData.bestScore}"
            );

            Debug.Log(
                $"[Login] Selected Bike: " +
                $"{playerData.selectedBike}"
            );


            SetStatus(
                "ĐĂNG NHẬP GOOGLE THÀNH CÔNG!"
            );


            LoadMainMenu();
        }
        catch (FirebaseException exception)
        {
            Debug.LogError(
                $"[Google Login] Firebase Error Code: " +
                $"{exception.ErrorCode}"
            );

            Debug.LogError(
                $"[Google Login] Firebase Error Message: " +
                $"{exception.Message}"
            );

            Debug.LogError(
                $"[Google Login] Firebase Full Exception: " +
                $"{exception}"
            );


            HandleGoogleLoginFinished(
                GetFirebaseErrorMessage(
                    exception
                )
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                $"[Google Login] " +
                $"PlayerData load failed: {exception}"
            );


            HandleGoogleLoginFinished(
                "Đăng nhập Google thất bại."
            );
        }
    }


    // ============================================================
    // GOOGLE LOGIN FAILED
    // ============================================================

    private void OnGoogleLoginFailed(
        string errorMessage
    )
    {
        Debug.LogError(
            "[Login] Google Login FAILED."
        );

        Debug.LogError(
            $"[Login] Reason: {errorMessage}"
        );


        HandleGoogleLoginFinished(
            string.IsNullOrWhiteSpace(errorMessage)
                ? "Đăng nhập Google thất bại."
                : errorMessage
        );
    }


    // ============================================================
    // GOOGLE LOGIN FINISHED
    // ============================================================

    private void HandleGoogleLoginFinished(
        string statusMessage
    )
    {
        SetStatus(
            statusMessage
        );


        isProcessing = false;


        if (!isLoadingMainMenu)
        {
            SetButtonsInteractable(true);
        }
    }


    // ============================================================
    // FIREBASE AUTH READY
    // ============================================================

    private bool IsFirebaseAuthReady()
    {
        if (FirebaseAuthManager.Instance == null)
        {
            Debug.LogError(
                "[Login] " +
                "Không tìm thấy FirebaseAuthManager."
            );

            return false;
        }


        if (!FirebaseAuthManager.Instance.IsReady)
        {
            Debug.LogWarning(
                "[Login] " +
                "FirebaseAuthManager chưa Ready."
            );

            return false;
        }


        if (FirebaseAuthManager.Instance.Auth == null)
        {
            Debug.LogError(
                "[Login] " +
                "FirebaseAuth chưa được khởi tạo."
            );

            return false;
        }


        return true;
    }


    // ============================================================
    // FIRESTORE READY
    // ============================================================

    private bool IsFirestoreReady()
    {
        if (FirestorePlayerDataManager.Instance == null)
        {
            Debug.LogError(
                "[Login] " +
                "Không tìm thấy FirestorePlayerDataManager."
            );

            return false;
        }


        return true;
    }


    // ============================================================
    // FIREBASE ERROR MESSAGE
    // ============================================================

    private string GetFirebaseErrorMessage(
        FirebaseException exception
    )
    {
        if (exception == null)
            return "Đã xảy ra lỗi Firebase.";


        string message =
            exception.Message != null
                ? exception.Message.ToLowerInvariant()
                : string.Empty;


        Debug.Log(
            $"[Firebase] " +
            $"ErrorCode = {exception.ErrorCode}"
        );


        if (message.Contains(
                "invalid-email"))
        {
            return "Email không hợp lệ.";
        }


        if (message.Contains(
                "email-already-in-use"))
        {
            return "Email này đã được đăng ký.";
        }


        if (message.Contains(
                "user-not-found"))
        {
            return "Không tìm thấy tài khoản.";
        }


        if (message.Contains(
                "wrong-password"))
        {
            return "Mật khẩu không đúng.";
        }


        if (message.Contains(
                "invalid-credential"))
        {
            return "Email hoặc mật khẩu không đúng.";
        }


        if (message.Contains(
                "weak-password"))
        {
            return "Mật khẩu quá yếu.";
        }


        if (message.Contains(
                "network"))
        {
            return "Không có kết nối mạng.";
        }


        if (message.Contains(
                "internal"))
        {
            return "Firebase Authentication " +
                   "đang gặp lỗi nội bộ.";
        }


        return
            $"Firebase lỗi: {exception.Message}";
    }


    // ============================================================
    // BUTTON STATE
    // ============================================================

    private void SetButtonsInteractable(
        bool interactable
    )
    {
        if (loginButton != null)
            loginButton.interactable =
                interactable;


        if (registerButton != null)
            registerButton.interactable =
                interactable;


        if (googleLoginButton != null)
            googleLoginButton.interactable =
                interactable;


        if (passwordToggleButton != null)
            passwordToggleButton.interactable =
                interactable;
    }


    // ============================================================
    // STATUS
    // ============================================================

    private void SetStatus(
        string message
    )
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}