using System.Globalization;
using LEADKhongPhanh.Update;
using LeadLauncher;

namespace LeadUpdater;

public sealed record UpdaterOptions(
    string InstallationRoot,
    string PackagePath,
    string OperationId,
    string ExpectedVersion,
    string ExpectedSha256,
    long ExpectedFileSize,
    int LauncherPid,
    DateTimeOffset LauncherStartTimeUtc,
    int? GamePid = null,
    DateTimeOffset? GameStartTimeUtc = null)
{
    public const string ExpectedExecutableRelativePath = "LEADKhongPhanh.exe";
    public const string ExpectedDataDirectoryName = "LEADKhongPhanh_Data";

    private static readonly HashSet<string> RequiredKeys =
        new(StringComparer.Ordinal)
        {
            "--install-root",
            "--package",
            "--operation-id",
            "--expected-version",
            "--expected-sha256",
            "--expected-file-size",
            "--launcher-pid",
            "--launcher-start-time-utc"
        };

    public static OperationResult<UpdaterOptions> ParseArguments(
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count % 2 != 0)
            return OperationResult<UpdaterOptions>.Failure(
                "InvalidArguments", "Arguments must be switch/value pairs.");

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 0; index < arguments.Count; index += 2)
        {
            string key = arguments[index];
            string value = arguments[index + 1];
            if (string.IsNullOrWhiteSpace(key) ||
                string.IsNullOrWhiteSpace(value) ||
                !key.StartsWith("--", StringComparison.Ordinal) ||
                value.StartsWith("--", StringComparison.Ordinal))
            {
                return OperationResult<UpdaterOptions>.Failure(
                    "InvalidArguments", "Argument switch or value is invalid.");
            }

            if (!RequiredKeys.Contains(key) &&
                key is not "--game-pid" and not "--game-start-time-utc")
            {
                return OperationResult<UpdaterOptions>.Failure(
                    "InvalidArguments", $"Unknown argument: {key}.");
            }

            if (!values.TryAdd(key, value))
                return OperationResult<UpdaterOptions>.Failure(
                    "InvalidArguments", $"Duplicate argument: {key}.");
        }

        if (RequiredKeys.Any(key => !values.ContainsKey(key)))
            return OperationResult<UpdaterOptions>.Failure(
                "InvalidArguments", "A required argument is missing.");

        if (!Path.IsPathFullyQualified(values["--install-root"]) ||
            !Path.IsPathFullyQualified(values["--package"]))
        {
            return OperationResult<UpdaterOptions>.Failure(
                "InvalidArguments",
                "Installation root and package path must be absolute.");
        }

        if (!long.TryParse(
                values["--expected-file-size"],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long fileSize) ||
            fileSize <= 0)
        {
            return OperationResult<UpdaterOptions>.Failure(
                "InvalidFileSize", "Expected package size must be positive.");
        }

        if (!int.TryParse(
                values["--launcher-pid"],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int launcherPid) ||
            launcherPid <= 0 ||
            !TryParseUtc(values["--launcher-start-time-utc"],
                out DateTimeOffset launcherStart))
        {
            return OperationResult<UpdaterOptions>.Failure(
                "InvalidProcessIdentity",
                "Launcher PID and UTC start time are required.");
        }

        bool hasGamePid = values.TryGetValue("--game-pid", out string? gamePidText);
        bool hasGameStart = values.TryGetValue(
            "--game-start-time-utc",
            out string? gameStartText);
        if (hasGamePid != hasGameStart)
            return OperationResult<UpdaterOptions>.Failure(
                "InvalidProcessIdentity",
                "Game PID and start time must be provided together.");

        int? gamePid = null;
        DateTimeOffset? gameStart = null;
        if (hasGamePid)
        {
            if (!int.TryParse(
                    gamePidText,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int parsedGamePid) ||
                parsedGamePid <= 0 ||
                !TryParseUtc(gameStartText!, out DateTimeOffset parsedGameStart))
            {
                return OperationResult<UpdaterOptions>.Failure(
                    "InvalidProcessIdentity",
                    "Game PID and UTC start time are invalid.");
            }

            gamePid = parsedGamePid;
            gameStart = parsedGameStart;
        }

        string operationId = values["--operation-id"];
        if (!PathSafety.IsValidOperationId(operationId))
            return OperationResult<UpdaterOptions>.Failure(
                "InvalidOperationId", "Operation id must be 32 hexadecimal characters.");
        if (!PathSafety.IsValidSha256(values["--expected-sha256"]))
            return OperationResult<UpdaterOptions>.Failure(
                "InvalidPackageHash", "SHA-256 must be exactly 64 hex characters.");
        if (!UpdateVersion.TryParse(
                values["--expected-version"],
                out _))
        {
            return OperationResult<UpdaterOptions>.Failure(
                "InvalidVersion", "Expected version is malformed.");
        }

        return OperationResult<UpdaterOptions>.Success(
            new UpdaterOptions(
                values["--install-root"],
                values["--package"],
                operationId.ToLowerInvariant(),
                values["--expected-version"],
                values["--expected-sha256"].ToLowerInvariant(),
                fileSize,
                launcherPid,
                launcherStart,
                gamePid,
                gameStart));
    }

    private static bool TryParseUtc(
        string value,
        out DateTimeOffset result) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out result) &&
        result.Offset == TimeSpan.Zero;
}
