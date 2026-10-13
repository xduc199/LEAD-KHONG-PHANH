using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LEADKhongPhanh.Update
{
    public readonly struct UpdateDownloadProgress
    {
        public long DownloadedBytes { get; }
        public long TotalBytes { get; }
        public float Progress01 { get; }

        public UpdateDownloadProgress(long downloadedBytes, long totalBytes)
        {
            DownloadedBytes = Math.Max(0, downloadedBytes);
            TotalBytes = Math.Max(0, totalBytes);
            Progress01 = TotalBytes == 0
                ? 0f
                : Math.Min(1f, (float)DownloadedBytes / TotalBytes);
        }
    }

    public readonly struct UpdateDownloadResult
    {
        public UpdateDownloadError Error { get; }
        public string PackagePath { get; }
        public string Details { get; }
        public bool IsSuccess => Error == UpdateDownloadError.None;

        private UpdateDownloadResult(
            UpdateDownloadError error,
            string packagePath,
            string details)
        {
            Error = error;
            PackagePath = packagePath;
            Details = details;
        }

        public static UpdateDownloadResult Success(string packagePath)
        {
            return new UpdateDownloadResult(
                UpdateDownloadError.None,
                packagePath,
                null);
        }

        public static UpdateDownloadResult Failure(
            UpdateDownloadError error,
            string details = null)
        {
            if (error == UpdateDownloadError.None)
                throw new ArgumentException(
                    "Failure result requires an error.",
                    nameof(error));

            return new UpdateDownloadResult(error, null, details);
        }
    }

    public sealed class UpdateDownloader
    {
        private const int ProgressIntervalMilliseconds = 100;
        private const int MaximumDownloadAttempts = 3;
        private readonly UpdateConfig config;
        private readonly string cacheDirectory;

        public UpdateDownloader(UpdateConfig config)
        {
            this.config = config ??
                throw new ArgumentNullException(nameof(config));

            cacheDirectory = Path.GetFullPath(Path.Combine(
                Application.temporaryCachePath,
                "LEADKhongPhanh",
                "UpdateCache"));
        }

        public async Task<UpdateDownloadResult> DownloadAndVerifyAsync(
            UpdateManifest manifest,
            CancellationToken cancellationToken,
            Action<UpdateDownloadProgress> progressChanged,
            Action verificationStarted)
        {
            if (manifest == null)
            {
                return UpdateDownloadResult.Failure(
                    UpdateDownloadError.InvalidManifest);
            }

            if (!config.TryValidate(out _))
            {
                return UpdateDownloadResult.Failure(
                    UpdateDownloadError.InvalidConfiguration);
            }

            UpdateDownloadError sizeError =
                UpdatePackageVerifier.ValidateExpectedSize(
                    manifest.FileSize,
                    config.MaxPackageBytes);
            if (sizeError != UpdateDownloadError.None)
                return UpdateDownloadResult.Failure(sizeError);

            if (!UpdateManifest.TryParseHttpsUrl(
                    manifest.DownloadUrl.OriginalString,
                    out Uri packageUri,
                    out _))
            {
                return UpdateDownloadResult.Failure(
                    UpdateDownloadError.NonHttpsUrl);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return UpdateDownloadResult.Failure(
                    UpdateDownloadError.Cancelled);
            }

            string temporaryPath = null;

            try
            {
                Directory.CreateDirectory(cacheDirectory);

                string verifiedPath = BuildPackagePath(manifest);
                CleanupStaleCacheFiles(verifiedPath);
                if (File.Exists(verifiedPath))
                {
                    UpdatePackageVerificationResult cachedVerification =
                        await UpdatePackageVerifier.VerifyAsync(
                            verifiedPath,
                            manifest.FileSize,
                            manifest.Sha256,
                            config.MaxPackageBytes,
                            cancellationToken);

                    if (cachedVerification.IsSuccess)
                    {
                        progressChanged?.Invoke(
                            new UpdateDownloadProgress(
                                manifest.FileSize,
                                manifest.FileSize));
                        return UpdateDownloadResult.Success(verifiedPath);
                    }

                    DeleteIfExists(verifiedPath);
                }

                UpdateDownloadError requestError =
                    UpdateDownloadError.DownloadFailed;
                for (int attempt = 0;
                     attempt < MaximumDownloadAttempts;
                     attempt++)
                {
                    temporaryPath = Path.Combine(
                        cacheDirectory,
                        "download_" +
                        Guid.NewGuid().ToString("N") +
                        ".partial");

                    requestError = await DownloadToTemporaryFileAsync(
                        packageUri,
                        temporaryPath,
                        manifest.FileSize,
                        cancellationToken,
                        progressChanged);

                    if (requestError == UpdateDownloadError.None)
                        break;

                    DeleteIfExists(temporaryPath);
                    temporaryPath = null;

                    if (requestError == UpdateDownloadError.Cancelled ||
                        attempt + 1 == MaximumDownloadAttempts)
                    {
                        break;
                    }

                    await Task.Delay(
                        TimeSpan.FromMilliseconds(500 * (attempt + 1)),
                        cancellationToken);
                }

                if (requestError != UpdateDownloadError.None)
                {
                    return UpdateDownloadResult.Failure(requestError);
                }

                cancellationToken.ThrowIfCancellationRequested();
                verificationStarted?.Invoke();

                UpdatePackageVerificationResult verification =
                    await UpdatePackageVerifier.VerifyAsync(
                        temporaryPath,
                        manifest.FileSize,
                        manifest.Sha256,
                        config.MaxPackageBytes,
                        cancellationToken);

                if (!verification.IsSuccess)
                {
                    return UpdateDownloadResult.Failure(
                        verification.Error);
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (File.Exists(verifiedPath))
                {
                    UpdatePackageVerificationResult racedCacheVerification =
                        await UpdatePackageVerifier.VerifyAsync(
                            verifiedPath,
                            manifest.FileSize,
                            manifest.Sha256,
                            config.MaxPackageBytes,
                            cancellationToken);

                    if (racedCacheVerification.IsSuccess)
                    {
                        DeleteIfExists(temporaryPath);
                        temporaryPath = null;
                        return UpdateDownloadResult.Success(verifiedPath);
                    }

                    DeleteIfExists(verifiedPath);
                }

                File.Move(temporaryPath, verifiedPath);
                temporaryPath = null;

                return UpdateDownloadResult.Success(verifiedPath);
            }
            catch (OperationCanceledException)
            {
                return UpdateDownloadResult.Failure(
                    UpdateDownloadError.Cancelled);
            }
            catch (UnauthorizedAccessException exception)
            {
                return UpdateDownloadResult.Failure(
                    UpdateDownloadError.FileAccessDenied,
                    exception.Message);
            }
            catch (IOException exception)
            {
                return UpdateDownloadResult.Failure(
                    UpdateDownloadError.VerificationFailed,
                    exception.Message);
            }
            catch (ArgumentException exception)
            {
                return UpdateDownloadResult.Failure(
                    UpdateDownloadError.VerificationFailed,
                    exception.Message);
            }
            finally
            {
                if (temporaryPath != null)
                    DeleteIfExists(temporaryPath);
            }
        }

        private async Task<UpdateDownloadError> DownloadToTemporaryFileAsync(
            Uri packageUri,
            string temporaryPath,
            long expectedSize,
            CancellationToken cancellationToken,
            Action<UpdateDownloadProgress> progressChanged)
        {
            using (var request = new UnityWebRequest(
                       packageUri,
                       UnityWebRequest.kHttpVerbGET))
            {
                var handler = new DownloadHandlerFile(temporaryPath);
                handler.removeFileOnAbort = true;
                request.downloadHandler = handler;
                request.timeout = config.DownloadTimeoutSeconds;
                request.redirectLimit = 0;

                using (cancellationToken.Register(request.Abort))
                {
                    progressChanged?.Invoke(
                        new UpdateDownloadProgress(0, expectedSize));

                    UnityWebRequestAsyncOperation operation;

                    try
                    {
                        operation = request.SendWebRequest();
                    }
                    catch (Exception)
                    {
                        return cancellationToken.IsCancellationRequested
                            ? UpdateDownloadError.Cancelled
                            : UpdateDownloadError.DownloadFailed;
                    }

                    long lastReportedBytes = -1;
                    var interval = TimeSpan.FromMilliseconds(
                        ProgressIntervalMilliseconds);
                    DateTime lastReportUtc = DateTime.UtcNow;

                    while (!operation.isDone)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            request.Abort();

                        long downloadedBytes =
                            (long)request.downloadedBytes;
                        if (downloadedBytes > expectedSize)
                        {
                            request.Abort();
                            return UpdateDownloadError.FileSizeMismatch;
                        }

                        DateTime now = DateTime.UtcNow;
                        if (now - lastReportUtc >= interval)
                        {
                            if (downloadedBytes != lastReportedBytes)
                            {
                                progressChanged?.Invoke(
                                    new UpdateDownloadProgress(
                                        downloadedBytes,
                                        expectedSize));
                                lastReportedBytes = downloadedBytes;
                            }

                            lastReportUtc = now;
                        }

                        await Task.Delay(
                            ProgressIntervalMilliseconds / 2);
                    }

                    if (cancellationToken.IsCancellationRequested)
                        return UpdateDownloadError.Cancelled;

                    if ((long)request.downloadedBytes > expectedSize)
                        return UpdateDownloadError.FileSizeMismatch;

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        return request.result ==
                               UnityWebRequest.Result.ConnectionError
                            ? UpdateDownloadError.NetworkUnavailable
                            : UpdateDownloadError.DownloadFailed;
                    }

                    progressChanged?.Invoke(
                        new UpdateDownloadProgress(
                            (long)request.downloadedBytes,
                            expectedSize));

                    if (!File.Exists(temporaryPath))
                        return UpdateDownloadError.FileNotFound;

                    return UpdateDownloadError.None;
                }
            }
        }

        private string BuildPackagePath(UpdateManifest manifest)
        {
            string filename = "update_" +
                              manifest.Version +
                              "_" +
                              manifest.Sha256 +
                              ".zip";
            return Path.GetFullPath(Path.Combine(cacheDirectory, filename));
        }

        private void CleanupStaleCacheFiles(string packageToKeep)
        {
            DateTime nowUtc = DateTime.UtcNow;
            CleanupFilesOlderThan(
                "download_*.partial",
                packageToKeep,
                nowUtc,
                TimeSpan.FromDays(1));
            CleanupFilesOlderThan(
                "update_*.zip",
                packageToKeep,
                nowUtc,
                TimeSpan.FromDays(30));
        }

        private void CleanupFilesOlderThan(
            string searchPattern,
            string packageToKeep,
            DateTime nowUtc,
            TimeSpan retention)
        {
            foreach (string path in Directory.EnumerateFiles(
                         cacheDirectory,
                         searchPattern,
                         SearchOption.TopDirectoryOnly))
            {
                string fullPath = Path.GetFullPath(path);
                if (string.Equals(
                        fullPath,
                        packageToKeep,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                DateTime lastWriteUtc = File.GetLastWriteTimeUtc(fullPath);
                if (nowUtc - lastWriteUtc > retention)
                    File.Delete(fullPath);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
