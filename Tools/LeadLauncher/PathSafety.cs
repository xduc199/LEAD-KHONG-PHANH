using System.Text.RegularExpressions;

namespace LeadLauncher;

public static partial class PathSafety
{
    private static readonly HashSet<string> ReservedDeviceNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "CONIN$", "CONOUT$",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7",
            "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5",
            "LPT6", "LPT7", "LPT8", "LPT9",
            "COM¹", "COM²", "COM³", "LPT¹", "LPT²", "LPT³"
        };

    [GeneratedRegex("^[a-fA-F0-9]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex InstallIdRegex();

    [GeneratedRegex("^slot-[a-fA-F0-9]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex SlotIdRegex();

    [GeneratedRegex("^[a-fA-F0-9]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex OperationIdRegex();

    [GeneratedRegex("^[a-fA-F0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Regex();

    public static bool IsValidInstallId(string? value) =>
        value is not null && InstallIdRegex().IsMatch(value);

    public static bool IsValidSlotId(string? value) =>
        value is not null && SlotIdRegex().IsMatch(value);

    public static bool IsValidOperationId(string? value) =>
        value is not null && OperationIdRegex().IsMatch(value);

    public static bool IsValidSha256(string? value) =>
        value is not null && Sha256Regex().IsMatch(value);

    public static OperationResult<string> ResolveRelativePath(
        string rootPath,
        string relativePath,
        bool requireExisting = false)
    {
        if (string.IsNullOrWhiteSpace(rootPath) ||
            string.IsNullOrEmpty(relativePath))
        {
            return OperationResult<string>.Failure(
                "InvalidPath", "Root and relative path are required.");
        }

        if (relativePath.IndexOf('\0') >= 0 ||
            relativePath.StartsWith('\\') ||
            relativePath.StartsWith('/') ||
            Path.IsPathRooted(relativePath) ||
            relativePath.Contains(':', StringComparison.Ordinal))
        {
            return OperationResult<string>.Failure(
                "UnsafePath", "Absolute, UNC, drive, and ADS paths are forbidden.");
        }

        string[] segments = relativePath.Split(
            ['/', '\\'],
            StringSplitOptions.None);
        if (segments.Length == 0 || segments.Any(segment =>
                !IsSafeSegment(segment)))
        {
            return OperationResult<string>.Failure(
                "UnsafePath", "Path contains an invalid or traversal segment.");
        }

        try
        {
            string canonicalRoot = Path.GetFullPath(rootPath);
            string candidate = Path.GetFullPath(
                Path.Combine(canonicalRoot, Path.Combine(segments)));
            string rootPrefix = Path.EndsInDirectorySeparator(canonicalRoot)
                ? canonicalRoot
                : canonicalRoot + Path.DirectorySeparatorChar;

            if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<string>.Failure(
                    "PathEscape", "Resolved path escapes its allowed root.");
            }

            if (requireExisting && !File.Exists(candidate) && !Directory.Exists(candidate))
            {
                return OperationResult<string>.Failure(
                    "PathNotFound", "Resolved path does not exist.");
            }

            return OperationResult<string>.Success(candidate);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            NotSupportedException or
            PathTooLongException or
            IOException)
        {
            return OperationResult<string>.Failure(
                "InvalidPath", exception.Message);
        }
    }

    public static OperationResult<bool> EnsureNoReparsePoints(
        string boundaryRoot,
        string candidatePath)
    {
        try
        {
            string root = Path.GetFullPath(boundaryRoot);
            string candidate = Path.GetFullPath(candidatePath);
            string prefix = Path.EndsInDirectorySeparator(root)
                ? root
                : root + Path.DirectorySeparatorChar;
            if (!candidate.Equals(root, StringComparison.OrdinalIgnoreCase) &&
                !candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<bool>.Failure(
                    "PathEscape", "Candidate is outside the verified root.");
            }

            string current = root;
            if (HasReparsePoint(current))
                return OperationResult<bool>.Failure(
                    "ReparsePoint", "Root contains a reparse point.");

            string relative = Path.GetRelativePath(root, candidate);
            if (relative == ".")
                return OperationResult<bool>.Success(true);

            foreach (string segment in relative.Split(
                         Path.DirectorySeparatorChar,
                         Path.AltDirectorySeparatorChar))
            {
                current = Path.Combine(current, segment);
                if ((File.Exists(current) || Directory.Exists(current)) &&
                    HasReparsePoint(current))
                {
                    return OperationResult<bool>.Failure(
                        "ReparsePoint", "Path contains a reparse point.");
                }
            }

            return OperationResult<bool>.Success(true);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            NotSupportedException or
            PathTooLongException or
            IOException or
            UnauthorizedAccessException)
        {
            return OperationResult<bool>.Failure(
                "PathValidationFailed", exception.Message);
        }
    }

    private static bool IsSafeSegment(string segment)
    {
        if (segment.Length == 0 || segment is "." or ".." ||
            segment.EndsWith('.') || segment.EndsWith(' '))
        {
            return false;
        }

        if (segment.Any(character =>
                char.IsControl(character) ||
                "<>\"|?*:".Contains(character, StringComparison.Ordinal)))
        {
            return false;
        }

        string deviceBase = segment.Split('.')[0];
        return !ReservedDeviceNames.Contains(deviceBase);
    }

    private static bool HasReparsePoint(string path)
    {
        FileAttributes attributes = File.GetAttributes(path);
        return (attributes & FileAttributes.ReparsePoint) != 0;
    }
}
