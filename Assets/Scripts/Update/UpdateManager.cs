using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LEADKhongPhanh.Update
{
    public enum UpdateState
    {
        Idle,
        Checking,
        NoUpdate,
        UpdateAvailable,
        Downloading,
        Verifying,
        ReadyToInstall,
        Installing,
        Failed,
        Cancelled
    }

    [DisallowMultipleComponent]
    public sealed class UpdateManager : MonoBehaviour
    {
        [SerializeField] private UpdateConfig config;
        [SerializeField] private bool checkOnStart = true;
        [SerializeField] private bool allowCheckInEditor;

        private IUpdateProvider provider;
        private UpdateDownloader downloader;
        private CancellationTokenSource operationCancellation;
        private bool operationActive;
        private bool isDestroyed;

        public UpdateState State { get; private set; } = UpdateState.Idle;
        public UpdateManifest Manifest { get; private set; }
        public UpdateVersion CurrentVersion { get; private set; }
        public bool IsForcedUpdate { get; private set; }
        public string VerifiedPackagePath { get; private set; }
        public UpdateValidationError ManifestError { get; private set; }
        public UpdateDownloadError DownloadError { get; private set; }
        public string FailureDetails { get; private set; }

        public event Action<UpdateManager> StateChanged;
        public event Action<UpdateManager, UpdateDownloadProgress>
            ProgressChanged;

        private void Awake()
        {
            Debug.Log("[UpdateManager] Awake", this);
        }

        private void Start()
        {
            Debug.Log("[UpdateManager] Start", this);

            if (!checkOnStart)
            {
                Debug.LogWarning(
                    "[Update] Automatic update check is disabled.", this);
                return;
            }

            if (Application.isEditor && !allowCheckInEditor)
            {
                Debug.Log("[Update] Automatic update check skipped in Unity Editor.", this);
                return;
            }

            if (config == null)
            {
                Debug.LogError("[Update] UpdateConfig is not assigned.", this);
                FailConfiguration(
                    UpdateValidationError.InvalidConfiguration,
                    "UpdateConfig is not assigned.");
                return;
            }

            if (string.IsNullOrWhiteSpace(config.ManifestUrl))
            {
                Debug.LogError("[Update] UpdateConfig.ManifestUrl is empty.", this);
                FailConfiguration(
                    UpdateValidationError.InvalidConfiguration,
                    "UpdateConfig.ManifestUrl is empty.");
                return;
            }

            if (!config.TryValidate(out UpdateValidationError configError))
            {
                Debug.LogError(
                    "[Update] UpdateConfig is invalid: " + configError,
                    this);
                FailConfiguration(configError, configError.ToString());
                return;
            }

            Debug.Log("[UpdateManager] Config loaded", this);
            Debug.Log(
                "[UpdateManager] Manifest URL = " +
                GetSafeManifestLocation(config.ManifestUrl), this);
            Debug.Log(
                "[UpdateManager] CurrentVersion = " + Application.version,
                this);
            Debug.Log(
                "[UpdateManager] ProductName = " + Application.productName,
                this);
            Debug.Log(
                "[Update] Automatic check started. CurrentVersion=" +
                Application.version + "; ManifestUrl=" +
                GetSafeManifestLocation(config.ManifestUrl), this);

            StartAutomaticCheck();
        }

        public async Task CheckForUpdateAsync()
        {
            if (operationActive || isDestroyed)
                return;

            operationActive = true;
            operationCancellation = new CancellationTokenSource();
            Manifest = null;
            VerifiedPackagePath = null;
            ManifestError = UpdateValidationError.None;
            DownloadError = UpdateDownloadError.None;
            FailureDetails = null;
            IsForcedUpdate = false;
            SetState(UpdateState.Checking);
            Debug.Log("[Update] CheckForUpdateAsync started.", this);

            try
            {
                if (config == null ||
                    !UpdateVersion.TryParse(
                        Application.version,
                        out UpdateVersion currentVersion))
                {
                    ManifestError =
                        UpdateValidationError.InvalidConfiguration;
                    FailureDetails = config == null
                        ? "UpdateConfig is not assigned."
                        : "Application.version is invalid.";
                    SetState(UpdateState.Failed);
                    return;
                }

                CurrentVersion = currentVersion;
                Debug.Log(
                    "[UpdateManager] CurrentVersion = " + CurrentVersion,
                    this);
                provider = new JsonUpdateProvider(config);

                UpdateManifestResult result =
                    await provider.GetLatestManifestAsync(
                        operationCancellation.Token);

                if (isDestroyed)
                    return;

                if (!result.IsSuccess)
                {
                    ManifestError = result.Error;
                    FailureDetails = result.Error.ToString();
                    Debug.LogError("[Update] Manifest request failed: " + result.Error, this);
                    SetState(
                        result.Error == UpdateValidationError.Cancelled
                            ? UpdateState.Cancelled
                            : UpdateState.Failed);
                    return;
                }

                Manifest = result.Manifest;
                Debug.Log(
                    "[UpdateManager] RemoteVersion = " + Manifest.Version +
                    "; minimumSupportedVersion = " +
                    Manifest.MinimumSupportedVersion,
                    this);
                Debug.Log(
                    "[Update] Manifest loaded. current=" + CurrentVersion +
                    "; latest=" + Manifest.Version +
                    "; minimumSupported=" + Manifest.MinimumSupportedVersion,
                    this);
                IsForcedUpdate =
                    CurrentVersion < Manifest.MinimumSupportedVersion;

                bool updateAvailable =
                    Manifest.Version > CurrentVersion || IsForcedUpdate;
                Debug.Log(
                    "[UpdateManager] UpdateAvailable = " + updateAvailable,
                    this);

                if (updateAvailable)
                    SetState(UpdateState.UpdateAvailable);
                else
                    SetState(UpdateState.NoUpdate);
            }
            catch (Exception exception)
            {
                FailureDetails = "Update check failed (" +
                                 exception.GetType().Name + ").";
                ManifestError = UpdateValidationError.RequestFailed;
                SetState(UpdateState.Failed);
                Debug.LogError("[Update] Check failed: " + FailureDetails, this);
            }
            finally
            {
                EndOperation();
            }
        }

        public async Task DownloadUpdateAsync()
        {
            if (operationActive ||
                isDestroyed ||
                (State != UpdateState.UpdateAvailable &&
                 State != UpdateState.Failed &&
                 State != UpdateState.Cancelled) ||
                Manifest == null)
            {
                return;
            }

            operationActive = true;
            operationCancellation = new CancellationTokenSource();
            DownloadError = UpdateDownloadError.None;
            FailureDetails = null;
            VerifiedPackagePath = null;
            downloader = new UpdateDownloader(config);
            SetState(UpdateState.Downloading);

            try
            {
                UpdateDownloadResult result =
                    await downloader.DownloadAndVerifyAsync(
                        Manifest,
                        operationCancellation.Token,
                        OnDownloadProgress,
                        OnVerificationStarted);

                if (isDestroyed)
                    return;

                if (!result.IsSuccess)
                {
                    DownloadError = result.Error;
                    FailureDetails = result.Details;
                    SetState(
                        result.Error == UpdateDownloadError.Cancelled
                            ? UpdateState.Cancelled
                            : UpdateState.Failed);

                    if (result.Error != UpdateDownloadError.Cancelled)
                    {
                        Debug.LogWarning(
                            "[Update] Download/verification failed: " +
                            result.Error +
                            (string.IsNullOrEmpty(result.Details)
                                ? string.Empty
                                : " | " + result.Details),
                            this);
                    }

                    return;
                }

                VerifiedPackagePath = result.PackagePath;
                SetState(UpdateState.ReadyToInstall);
                Debug.Log("[UpdateManager] ReadyToInstall", this);
                Debug.Log(
                    "[Update] Package downloaded and SHA-256 verified.",
                    this);
            }
            catch (Exception exception)
            {
                FailureDetails = "Update download failed (" +
                                 exception.GetType().Name + ").";
                DownloadError = UpdateDownloadError.VerificationFailed;
                SetState(UpdateState.Failed);
                Debug.LogError(
                    "[Update] Download/verification failed: " + FailureDetails,
                    this);
            }
            finally
            {
                EndOperation();
            }
        }

        public void CancelDownload()
        {
            if (State == UpdateState.Downloading ||
                State == UpdateState.Verifying)
            {
                operationCancellation?.Cancel();
            }
        }

        public async Task InstallUpdateAsync()
        {
            if (operationActive ||
                isDestroyed ||
                State != UpdateState.ReadyToInstall ||
                Manifest == null ||
                string.IsNullOrWhiteSpace(VerifiedPackagePath))
            {
                Debug.LogWarning(
                    "[UpdateManager] Install request ignored because an " +
                    "operation is active or the verified package is not ready.",
                    this);
                return;
            }

            Debug.Log("[UpdateManager] Install requested", this);
            operationActive = true;
            FailureDetails = null;
            SetState(UpdateState.Installing);

            try
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                await CreateHandoffAndExitAsync();
#else
                throw new PlatformNotSupportedException(
                    "Transactional installation is available only in a Windows standalone build.");
#endif
            }
            catch (Exception exception)
            {
                FailureDetails = "Installer handoff failed (" +
                                 exception.GetType().Name + ").";
                SetState(UpdateState.ReadyToInstall);
                Debug.LogError(
                    "[UpdateManager] " + FailureDetails + " " +
                    exception.Message,
                    this);
            }
            finally
            {
                operationActive = false;
            }
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private async Task CreateHandoffAndExitAsync()
        {
            string dataPath = Path.GetFullPath(Application.dataPath);
            string productName = Application.productName;
            if (string.IsNullOrWhiteSpace(productName) ||
                !Path.GetFileName(productName).Equals(
                    productName,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Application.productName is not a valid Unity build name.");
            }

            string expectedDataDirectoryName = productName + "_Data";
            if (!Path.GetFileName(dataPath).Equals(
                    expectedDataDirectoryName,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Current Unity data directory '" +
                    Path.GetFileName(dataPath) +
                    "' does not match the configured build name '" +
                    expectedDataDirectoryName + "'.");
            }

            DirectoryInfo slotDirectory =
                Directory.GetParent(dataPath) ??
                throw new InvalidOperationException(
                    "Current slot directory could not be resolved.");
            DirectoryInfo slotsDirectory =
                slotDirectory.Parent ??
                throw new InvalidOperationException(
                    "Slots directory could not be resolved.");
            DirectoryInfo installationDirectory =
                slotsDirectory.Parent ??
                throw new InvalidOperationException(
                    "Installation root could not be resolved.");
            string installationRoot = installationDirectory.FullName;
            string slotId = slotDirectory.Name;
            if (!IsSlotId(slotId))
                throw new InvalidOperationException(
                    "Current executable is not inside a managed slot.");

            using Process currentProcess = Process.GetCurrentProcess();
            string executablePath = Path.GetFullPath(
                currentProcess.MainModule?.FileName ??
                throw new InvalidOperationException(
                    "Current game executable path could not be resolved."));
            string expectedExecutable = Path.Combine(
                slotDirectory.FullName,
                productName + ".exe");
            if (!executablePath.Equals(
                    Path.GetFullPath(expectedExecutable),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Current process executable '" +
                    Path.GetFileName(executablePath) +
                    "' does not match the configured active-slot executable '" +
                    Path.GetFileName(expectedExecutable) + "'.");
            }

            DateTime processStartTimeUtc =
                currentProcess.StartTime.ToUniversalTime();
            Debug.Log(
                "[UpdateManager] Active-slot process identity captured. pid=" +
                currentProcess.Id +
                "; startTimeUtc=" + processStartTimeUtc.ToString("O") +
                "; executable=" + executablePath,
                this);

            string installIdPath =
                Path.Combine(installationRoot, "install.id");
            if (!File.Exists(installIdPath))
                throw new FileNotFoundException(
                    "Stable installation identity is missing.",
                    installIdPath);
            string installId = (await File.ReadAllTextAsync(installIdPath))
                .Trim();
            if (!IsHex(installId, 32))
                throw new InvalidDataException(
                    "Stable installation identity is malformed.");

            if (!UpdateVersion.TryParse(
                    Application.version,
                    out UpdateVersion updateVersion))
            {
                throw new InvalidDataException(
                    "Current application version is malformed.");
            }

            byte[] operationBytes = new byte[16];
            using (RandomNumberGenerator random =
                   RandomNumberGenerator.Create())
            {
                random.GetBytes(operationBytes);
            }
            string operationId = BitConverter
                .ToString(operationBytes)
                .Replace("-", string.Empty)
                .ToLowerInvariant();

            string packagesDirectory = Path.Combine(
                installationRoot, "Update", "Temp", "Packages");
            Directory.CreateDirectory(packagesDirectory);
            EnsureNoReparsePoints(installationRoot, packagesDirectory);

            string packageName = "update_" + operationId + ".zip";
            string packagePath = Path.Combine(packagesDirectory, packageName);
            try
            {
                await CopyPackageAsync(VerifiedPackagePath, packagePath);
            }
            catch
            {
                if (File.Exists(packagePath))
                    File.Delete(packagePath);
                throw;
            }

            UpdatePackageVerificationResult copiedPackage =
                await UpdatePackageVerifier.VerifyAsync(
                    packagePath,
                    Manifest.FileSize,
                    Manifest.Sha256,
                    config.MaxPackageBytes,
                    CancellationToken.None);
            if (!copiedPackage.IsSuccess)
            {
                File.Delete(packagePath);
                throw new InvalidDataException(
                    "Copied update package failed size/hash verification: " +
                    copiedPackage.Error);
            }

            var handoff = new InstallHandoff
            {
                schemaVersion = 1,
                operationId = operationId,
                packageRelativePath = Path.Combine(
                    "Update",
                    "Temp",
                    "Packages",
                    packageName),
                expectedVersion = Manifest.Version.ToString(),
                expectedSha256 = Manifest.Sha256,
                expectedFileSize = Manifest.FileSize,
                gamePid = currentProcess.Id,
                gameStartTimeUtc = new DateTimeOffset(
                    processStartTimeUtc).ToString("O")
            };

            string requestPath = Path.Combine(
                installationRoot,
                "Update",
                "Temp",
                "install-request.json");
            EnsureNoReparsePoints(
                installationRoot,
                Path.GetDirectoryName(requestPath));
            if (File.Exists(requestPath) || Directory.Exists(requestPath))
                throw new IOException(
                    "Another update handoff request is already present.");

            string launcherPath = Path.Combine(
                installationRoot,
                "LEADKhongPhanh.exe");
            EnsureNoReparsePoints(installationRoot, launcherPath);
            if (!File.Exists(launcherPath) ||
                (File.GetAttributes(launcherPath) &
                 (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            {
                throw new FileNotFoundException(
                    "Stable launcher is missing or invalid.",
                    launcherPath);
            }

            Debug.Log("[UpdateManager] Creating install request", this);
            Debug.Log(
                "[UpdateManager] Install request path = " + requestPath,
                this);
            Debug.Log(
                "[UpdateManager] Install request details: operationId=" +
                operationId + "; version=" + Manifest.Version +
                "; package=" + handoff.packageRelativePath +
                "; size=" + Manifest.FileSize,
                this);
            string temporaryPath = requestPath + "." +
                                   operationId + ".tmp";
            byte[] json = new UTF8Encoding(false, true).GetBytes(
                JsonUtility.ToJson(handoff));
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       4096,
                       FileOptions.WriteThrough))
            {
                await stream.WriteAsync(json, 0, json.Length);
                stream.Flush(true);
            }
            try
            {
                File.Move(temporaryPath, requestPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }

            FileInfo requestFile = new FileInfo(requestPath);
            if (!requestFile.Exists || requestFile.Length != json.LongLength)
            {
                throw new IOException(
                    "Install request was not fully written to disk.");
            }
            Debug.Log("[UpdateManager] Install request created", this);

            var startInfo = new ProcessStartInfo
            {
                FileName = launcherPath,
                WorkingDirectory = installationRoot,
                UseShellExecute = false,
                CreateNoWindow = false
            };
            Process launcher;
            try
            {
                launcher = Process.Start(startInfo) ??
                    throw new InvalidOperationException(
                        "Windows did not start the stable launcher.");
            }
            catch
            {
                File.Delete(requestPath);
                File.Delete(packagePath);
                throw;
            }
            launcher.Dispose();
            Debug.Log(
                "[Update] Stable launcher started for operation " +
                operationId + "; exiting game for transactional update.",
                this);
            Debug.Log("[UpdateManager] Calling Application.Quit", this);
            Application.Quit();
        }

        private static async Task CopyPackageAsync(
            string sourcePath,
            string destinationPath)
        {
            string source = Path.GetFullPath(sourcePath);
            if (!File.Exists(source) ||
                (File.GetAttributes(source) &
                 (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            {
                throw new FileNotFoundException(
                    "Verified package cache file is missing or invalid.",
                    source);
            }
            if (File.Exists(destinationPath) ||
                Directory.Exists(destinationPath))
            {
                throw new IOException(
                    "Update package destination already exists.");
            }

            using (var input = new FileStream(
                       source,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read,
                       1024 * 1024,
                       FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var output = new FileStream(
                       destinationPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       1024 * 1024,
                       FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await input.CopyToAsync(output, 1024 * 1024);
                await output.FlushAsync();
                output.Flush(true);
            }
        }

        private static void EnsureNoReparsePoints(
            string rootPath,
            string candidatePath)
        {
            string root = Path.GetFullPath(rootPath);
            string candidate = Path.GetFullPath(candidatePath);
            bool rootHasTrailingSeparator =
                root.EndsWith(
                    Path.DirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal) ||
                root.EndsWith(
                    Path.AltDirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal);
            string prefix = rootHasTrailingSeparator
                ? root
                : root + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Update path escaped the installation root.");
            }

            string current = root;
            string relative = Path.GetRelativePath(root, candidate);
            foreach (string segment in relative.Split(
                         Path.DirectorySeparatorChar,
                         Path.AltDirectorySeparatorChar))
            {
                current = Path.Combine(current, segment);
                if ((File.Exists(current) || Directory.Exists(current)) &&
                    (File.GetAttributes(current) &
                     FileAttributes.ReparsePoint) != 0)
                {
                    throw new IOException(
                        "Update path contains a reparse point.");
                }
            }
        }

        private static bool IsSlotId(string value) =>
            value.Length == 37 &&
            value.StartsWith("slot-", StringComparison.Ordinal) &&
            IsHex(value.Substring(5), 32);

        private static bool IsHex(string value, int length)
        {
            if (value == null || value.Length != length)
                return false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f') ||
                      (character >= 'A' && character <= 'F')))
                    return false;
            }
            return true;
        }

        [Serializable]
        private sealed class InstallHandoff
        {
            public int schemaVersion;
            public string operationId;
            public string packageRelativePath;
            public string expectedVersion;
            public string expectedSha256;
            public long expectedFileSize;
            public int gamePid;
            public string gameStartTimeUtc;
        }
#endif

        private async void StartAutomaticCheck()
        {
            await CheckForUpdateAsync();
        }

        private void OnDownloadProgress(UpdateDownloadProgress progress)
        {
            if (!isDestroyed)
                ProgressChanged?.Invoke(this, progress);
        }

        private void OnVerificationStarted()
        {
            if (!isDestroyed)
                SetState(UpdateState.Verifying);
        }

        private void SetState(UpdateState state)
        {
            State = state;
            Debug.Log("[Update] State = " + state, this);
            StateChanged?.Invoke(this);
        }

        private void FailConfiguration(
            UpdateValidationError error,
            string safeDetails)
        {
            ManifestError = error;
            FailureDetails = safeDetails;
            SetState(UpdateState.Failed);
        }

        private static string GetSafeManifestLocation(string manifestUrl)
        {
            if (!Uri.TryCreate(manifestUrl, UriKind.Absolute, out Uri uri))
                return "<invalid URL>";

            // Query strings may carry tokens; omit them from diagnostics.
            return uri.GetLeftPart(UriPartial.Path);
        }

        private void EndOperation()
        {
            operationActive = false;
            operationCancellation?.Dispose();
            operationCancellation = null;
        }

        private void OnDestroy()
        {
            isDestroyed = true;
            operationCancellation?.Cancel();
            StateChanged = null;
            ProgressChanged = null;
        }
    }
}
