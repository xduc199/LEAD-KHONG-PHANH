using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeadLauncher;

public static class AtomicFileWriter
{
    private const long MaximumStateFileBytes = 1024 * 1024;
    private static readonly TimeSpan TemporaryFileRetention =
        TimeSpan.FromDays(1);

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(
                JsonNamingPolicy.SnakeCaseUpper,
                allowIntegerValues: false)
        }
    };

    public static OperationResult<bool> WriteJson<T>(
        string targetPath,
        T value)
    {
        try
        {
            string json = JsonSerializer.Serialize(value, JsonOptions);
            return WriteAllText(targetPath, json);
        }
        catch (Exception exception) when (
            exception is JsonException or
            NotSupportedException or
            ArgumentException)
        {
            return OperationResult<bool>.Failure(
                "SerializationFailed", exception.Message);
        }
    }

    public static OperationResult<T> ReadJson<T>(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
                return OperationResult<T>.Failure(
                    "StateReadFailed", "State file does not exist.");
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                return OperationResult<T>.Failure(
                    "StateReparsePoint", "State files cannot be reparse points.");
            if (info.Length > MaximumStateFileBytes)
                return OperationResult<T>.Failure(
                    "StateTooLarge", "State file exceeds the safe size limit.");

            using FileStream stream = new(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            T? value = JsonSerializer.Deserialize<T>(stream, JsonOptions);
            return value is null
                ? OperationResult<T>.Failure(
                    "InvalidState", "JSON state was empty or null.")
                : OperationResult<T>.Success(value);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            JsonException or
            NotSupportedException)
        {
            return OperationResult<T>.Failure(
                "StateReadFailed", exception.Message);
        }
    }

    public static OperationResult<bool> WriteAllText(
        string targetPath,
        string content)
    {
        string? tempPath = null;
        try
        {
            string target = Path.GetFullPath(targetPath);
            string? directory = Path.GetDirectoryName(target);
            if (string.IsNullOrEmpty(directory))
                return OperationResult<bool>.Failure(
                    "InvalidTarget", "Target directory is unavailable.");

            Directory.CreateDirectory(directory);
            if (File.Exists(target) &&
                (File.GetAttributes(target) & FileAttributes.ReparsePoint) != 0)
            {
                return OperationResult<bool>.Failure(
                    "AtomicWriteFailed",
                    "State targets cannot be reparse points.");
            }

            tempPath = Path.Combine(
                directory,
                "." + Path.GetFileName(target) + "." +
                Guid.NewGuid().ToString("N") + ".tmp");

            byte[] bytes = new UTF8Encoding(false, true).GetBytes(content);
            using (var stream = new FileStream(
                       tempPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       4096,
                       FileOptions.WriteThrough))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(target))
                File.Replace(tempPath, target, null, ignoreMetadataErrors: true);
            else
                File.Move(tempPath, target);

            tempPath = null;
            return OperationResult<bool>.Success(true);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            NotSupportedException)
        {
            string details = exception.Message;
            if (tempPath is not null)
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch (Exception cleanupException) when (
                    cleanupException is IOException or
                    UnauthorizedAccessException)
                {
                    details += " Temporary-file cleanup failed: " +
                               cleanupException.Message;
                }
            }

            return OperationResult<bool>.Failure(
                "AtomicWriteFailed", details);
        }
    }

    public static OperationResult<bool> DeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return OperationResult<bool>.Failure(
                "StateDeleteFailed", exception.Message);
        }
    }

    public static OperationResult<bool> CleanupStaleTemporaryFiles(
        string directory)
    {
        try
        {
            if (!Directory.Exists(directory))
                return OperationResult<bool>.Success(true);

            DateTime cutoffUtc = DateTime.UtcNow - TemporaryFileRetention;
            foreach (string path in Directory.EnumerateFiles(
                         directory,
                         ".*.tmp",
                         SearchOption.TopDirectoryOnly))
            {
                if (File.GetLastWriteTimeUtc(path) < cutoffUtc)
                    File.Delete(path);
            }

            return OperationResult<bool>.Success(true);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return OperationResult<bool>.Failure(
                "TemporaryFileCleanupFailed", exception.Message);
        }
    }
}
