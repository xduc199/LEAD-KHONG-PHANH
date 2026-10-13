using System.Security.Cryptography;
using LeadLauncher;

namespace LeadUpdater;

public sealed class VerifiedPackage : IAsyncDisposable
{
    internal VerifiedPackage(FileStream stream)
    {
        Stream = stream;
    }

    public FileStream Stream { get; }

    public ValueTask DisposeAsync() => Stream.DisposeAsync();
}

public static class HashVerifier
{
    private const int BufferSize = 128 * 1024;

    public static async Task<OperationResult<VerifiedPackage>> OpenVerifiedAsync(
        string path,
        long expectedFileSize,
        string expectedSha256,
        UpdaterLimits limits,
        CancellationToken cancellationToken = default)
    {
        if (!PathSafety.IsValidSha256(expectedSha256))
            return OperationResult<VerifiedPackage>.Failure(
                "InvalidPackageHash", "SHA-256 must be exactly 64 hex characters.");
        OperationResult<bool> limitsValidation = limits.Validate();
        if (!limitsValidation.IsSuccess)
            return OperationResult<VerifiedPackage>.Failure(
                limitsValidation.ErrorCode, limitsValidation.Details);
        if (expectedFileSize <= 0 || expectedFileSize > limits.MaxPackageBytes)
            return OperationResult<VerifiedPackage>.Failure(
                "ArchiveTooLarge", "Package size exceeds the configured limit.");

        FileStream? stream = null;
        try
        {
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
                return OperationResult<VerifiedPackage>.Failure(
                    "PackageNotRegularFile",
                    "Package must be a regular non-reparse file.");

            stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (stream.Length != expectedFileSize)
                return FailAndDispose(
                    stream,
                    "PackageSizeMismatch",
                    "Package size does not match the trusted expected size.");

            using IncrementalHash hash =
                IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            byte[] buffer = new byte[BufferSize];
            long total = 0;
            while (true)
            {
                int read = await stream.ReadAsync(
                    buffer.AsMemory(),
                    cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;

                total = checked(total + read);
                if (total > expectedFileSize || total > limits.MaxPackageBytes)
                    return FailAndDispose(
                        stream,
                        "PackageSizeMismatch",
                        "Package changed or exceeded its declared size.");
                hash.AppendData(buffer, 0, read);
            }

            if (total != expectedFileSize)
                return FailAndDispose(
                    stream,
                    "PackageSizeMismatch",
                    "Package length changed during verification.");

            byte[] actualHash = hash.GetHashAndReset();
            byte[] expectedHash = Convert.FromHexString(expectedSha256);
            if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
                return FailAndDispose(
                    stream,
                    "PackageHashMismatch",
                    "Package SHA-256 does not match the trusted manifest.");

            stream.Position = 0;
            return OperationResult<VerifiedPackage>.Success(
                new VerifiedPackage(stream));
        }
        catch (OperationCanceledException)
        {
            if (stream is not null)
                await stream.DisposeAsync().ConfigureAwait(false);
            return OperationResult<VerifiedPackage>.Failure(
                "DownloadCancelled", "Package verification was cancelled.");
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            NotSupportedException or
            OverflowException or
            FormatException or
            CryptographicException)
        {
            if (stream is not null)
                await stream.DisposeAsync().ConfigureAwait(false);
            string code = exception switch
            {
                FileNotFoundException or DirectoryNotFoundException =>
                    "PackageMissing",
                CryptographicException => "HashVerificationFailed",
                _ => "PackageNotRegularFile"
            };
            return OperationResult<VerifiedPackage>.Failure(code, exception.Message);
        }
    }

    private static OperationResult<VerifiedPackage> FailAndDispose(
        FileStream stream,
        string code,
        string details)
    {
        stream.Dispose();
        return OperationResult<VerifiedPackage>.Failure(code, details);
    }
}
