using System.Security.Cryptography;
using System.Text;

namespace LeadLauncher;

public static class InstallIdentity
{
    public const string StablePublisher = "LEAD-KHONG-PHANH";

    public static OperationResult<string> Resolve(string installationRoot)
    {
        try
        {
            string root = Path.GetFullPath(installationRoot);
            string path = Path.Combine(root, "install.id");
            if (File.Exists(path))
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                {
                    return OperationResult<string>.Failure(
                        "InvalidInstallId",
                        "install.id cannot be a reparse point.");
                }
                if (new FileInfo(path).Length > 64)
                    return OperationResult<string>.Failure(
                        "InvalidInstallId",
                        "install.id exceeds its size limit.");

                string existing = File.ReadAllText(path, Encoding.ASCII).Trim();
                return PathSafety.IsValidInstallId(existing)
                    ? OperationResult<string>.Success(existing.ToLowerInvariant())
                    : OperationResult<string>.Failure(
                        "InvalidInstallId", "install.id is malformed.");
            }

            string generated = Convert.ToHexString(
                RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            byte[] data = Encoding.ASCII.GetBytes(generated + Environment.NewLine);
            Directory.CreateDirectory(root);

            try
            {
                using var stream = new FileStream(
                    path,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.Read,
                    4096,
                    FileOptions.WriteThrough);
                stream.Write(data);
                stream.Flush(flushToDisk: true);
                return OperationResult<string>.Success(generated);
            }
            catch (IOException) when (File.Exists(path))
            {
                string concurrent = File.ReadAllText(path, Encoding.ASCII).Trim();
                return PathSafety.IsValidInstallId(concurrent)
                    ? OperationResult<string>.Success(
                        concurrent.ToLowerInvariant())
                    : OperationResult<string>.Failure(
                        "InvalidInstallId",
                        "Concurrent install.id creation produced invalid data.");
            }
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            NotSupportedException)
        {
            return OperationResult<string>.Failure(
                "InstallIdentityFailed", exception.Message);
        }
    }

    public static OperationResult<string> ResolveStateRoot(
        string localApplicationData,
        string publisher,
        string installId)
    {
        if (string.IsNullOrWhiteSpace(localApplicationData) ||
            string.IsNullOrWhiteSpace(publisher) ||
            publisher.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            publisher is "." or ".." ||
            !PathSafety.IsValidInstallId(installId))
        {
            return OperationResult<string>.Failure(
                "InvalidStateLocation", "State root components are invalid.");
        }

        string path = Path.GetFullPath(Path.Combine(
            localApplicationData,
            publisher,
            installId.ToLowerInvariant(),
            "Updater"));
        return OperationResult<string>.Success(path);
    }
}
