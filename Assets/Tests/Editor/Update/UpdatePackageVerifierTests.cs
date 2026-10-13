using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LEADKhongPhanh.Update;
using NUnit.Framework;

namespace LEADKhongPhanh.Update.Tests
{
    public sealed class UpdatePackageVerifierTests
    {
        private const string AbcSha256 =
            "ba7816bf8f01cfea414140de5dae2223" +
            "b00361a396177a9cb410ff61f20015ad";

        [Test]
        public async Task VerifyAsync_AcceptsMatchingSizeAndHash()
        {
            string path = CreatePackage("abc");
            try
            {
                UpdatePackageVerificationResult result =
                    await UpdatePackageVerifier.VerifyAsync(
                        path,
                        3,
                        AbcSha256,
                        1024,
                        CancellationToken.None);

                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.ActualSha256, Is.EqualTo(AbcSha256));
            }
            finally
            {
                DeletePackage(path);
            }
        }

        [Test]
        public async Task VerifyAsync_RejectsSizeMismatchBeforeHashing()
        {
            string path = CreatePackage("abc");
            try
            {
                UpdatePackageVerificationResult result =
                    await UpdatePackageVerifier.VerifyAsync(
                        path,
                        4,
                        AbcSha256,
                        1024,
                        CancellationToken.None);

                Assert.That(result.Error,
                    Is.EqualTo(UpdateDownloadError.FileSizeMismatch));
            }
            finally
            {
                DeletePackage(path);
            }
        }

        [Test]
        public async Task VerifyAsync_RejectsHashMismatch()
        {
            string path = CreatePackage("abc");
            try
            {
                UpdatePackageVerificationResult result =
                    await UpdatePackageVerifier.VerifyAsync(
                        path,
                        3,
                        new string('0', 64),
                        1024,
                        CancellationToken.None);

                Assert.That(result.Error,
                    Is.EqualTo(UpdateDownloadError.HashMismatch));
            }
            finally
            {
                DeletePackage(path);
            }
        }

        [Test]
        public async Task VerifyAsync_ReportsPreCancelledOperation()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();

                UpdatePackageVerificationResult result =
                    await UpdatePackageVerifier.VerifyAsync(
                        "not-created.zip",
                        3,
                        AbcSha256,
                        1024,
                        cancellation.Token);

                Assert.That(result.Error,
                    Is.EqualTo(UpdateDownloadError.Cancelled));
            }
        }

        [Test]
        public void ValidateExpectedSize_RejectsPackageAboveConfiguredLimit()
        {
            UpdateDownloadError result =
                UpdatePackageVerifier.ValidateExpectedSize(1025, 1024);

            Assert.That(
                result,
                Is.EqualTo(UpdateDownloadError.PackageTooLarge));
        }

        private static string CreatePackage(string contents)
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                "lead-update-test-" +
                System.Guid.NewGuid().ToString("N") +
                ".zip");
            File.WriteAllText(path, contents);
            return path;
        }

        private static void DeletePackage(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
