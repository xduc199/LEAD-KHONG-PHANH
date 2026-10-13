using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace LEADKhongPhanh.Update
{
    public enum UpdateDownloadError
    {
        None,
        InvalidConfiguration,
        InvalidManifest,
        NonHttpsUrl,
        PackageTooLarge,
        FileNotFound,
        FileAccessDenied,
        FileSizeMismatch,
        HashMismatch,
        NetworkUnavailable,
        DownloadFailed,
        Cancelled,
        VerificationFailed
    }

    public readonly struct UpdatePackageVerificationResult
    {
        public UpdateDownloadError Error { get; }
        public string ActualSha256 { get; }
        public bool IsSuccess => Error == UpdateDownloadError.None;

        internal UpdatePackageVerificationResult(
            UpdateDownloadError error,
            string actualSha256)
        {
            Error = error;
            ActualSha256 = actualSha256;
        }
    }

    public static class UpdatePackageVerifier
    {
        private const int HashBufferSize = 64 * 1024;
        private static readonly byte[] EmptyBytes = new byte[0];

        public static UpdateDownloadError ValidateExpectedSize(
            long expectedSize,
            long maximumPackageBytes)
        {
            if (maximumPackageBytes <= 0 || expectedSize <= 0)
                return UpdateDownloadError.InvalidConfiguration;

            return expectedSize > maximumPackageBytes
                ? UpdateDownloadError.PackageTooLarge
                : UpdateDownloadError.None;
        }

        public static async Task<UpdatePackageVerificationResult> VerifyAsync(
            string filePath,
            long expectedSize,
            string expectedSha256,
            long maximumPackageBytes,
            CancellationToken cancellationToken)
        {
            UpdateDownloadError sizeError = ValidateExpectedSize(
                expectedSize,
                maximumPackageBytes);
            if (sizeError != UpdateDownloadError.None)
            {
                return new UpdatePackageVerificationResult(sizeError, null);
            }

            if (string.IsNullOrWhiteSpace(filePath) ||
                !IsValidSha256(expectedSha256))
            {
                return new UpdatePackageVerificationResult(
                    UpdateDownloadError.InvalidManifest,
                    null);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return new UpdatePackageVerificationResult(
                    UpdateDownloadError.Cancelled,
                    null);
            }

            try
            {
                return await Task.Run(
                    () => VerifyFile(
                        filePath,
                        expectedSize,
                        expectedSha256,
                        cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return new UpdatePackageVerificationResult(
                    UpdateDownloadError.Cancelled,
                    null);
            }
            catch (FileNotFoundException)
            {
                return new UpdatePackageVerificationResult(
                    UpdateDownloadError.FileNotFound,
                    null);
            }
            catch (DirectoryNotFoundException)
            {
                return new UpdatePackageVerificationResult(
                    UpdateDownloadError.FileNotFound,
                    null);
            }
            catch (UnauthorizedAccessException)
            {
                return new UpdatePackageVerificationResult(
                    UpdateDownloadError.FileAccessDenied,
                    null);
            }
            catch (IOException)
            {
                return new UpdatePackageVerificationResult(
                    UpdateDownloadError.VerificationFailed,
                    null);
            }
            catch (ArgumentException)
            {
                return new UpdatePackageVerificationResult(
                    UpdateDownloadError.VerificationFailed,
                    null);
            }
        }

        private static UpdatePackageVerificationResult VerifyFile(
            string filePath,
            long expectedSize,
            string expectedSha256,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                return new UpdatePackageVerificationResult(
                    UpdateDownloadError.FileNotFound,
                    null);
            }

            if (fileInfo.Length != expectedSize)
            {
                return new UpdatePackageVerificationResult(
                    UpdateDownloadError.FileSizeMismatch,
                    null);
            }

            string actualHash = ComputeSha256(filePath, cancellationToken);
            bool matches = string.Equals(
                actualHash,
                expectedSha256,
                StringComparison.OrdinalIgnoreCase);

            return new UpdatePackageVerificationResult(
                matches
                    ? UpdateDownloadError.None
                    : UpdateDownloadError.HashMismatch,
                actualHash);
        }

        private static string ComputeSha256(
            string filePath,
            CancellationToken cancellationToken)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = new FileStream(
                       filePath,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read,
                       HashBufferSize,
                       FileOptions.SequentialScan))
            {
                byte[] buffer = new byte[HashBufferSize];
                int bytesRead;

                while ((bytesRead = stream.Read(
                           buffer,
                           0,
                           buffer.Length)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    algorithm.TransformBlock(
                        buffer,
                        0,
                        bytesRead,
                        buffer,
                        0);
                }

                cancellationToken.ThrowIfCancellationRequested();
                algorithm.TransformFinalBlock(EmptyBytes, 0, 0);

                byte[] hash = algorithm.Hash;
                char[] hexadecimal = new char[hash.Length * 2];
                const string digits = "0123456789abcdef";

                for (int i = 0; i < hash.Length; i++)
                {
                    hexadecimal[i * 2] = digits[hash[i] >> 4];
                    hexadecimal[i * 2 + 1] =
                        digits[hash[i] & 0x0f];
                }

                return new string(hexadecimal);
            }
        }

        private static bool IsValidSha256(string value)
        {
            if (value == null || value.Length != 64)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                bool valid = character >= '0' && character <= '9' ||
                             character >= 'a' && character <= 'f' ||
                             character >= 'A' && character <= 'F';
                if (!valid)
                    return false;
            }

            return true;
        }
    }
}
