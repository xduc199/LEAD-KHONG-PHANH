using System.Globalization;
using LEADKhongPhanh.Update;

namespace LeadLauncher;

public sealed record UpdateInstallRequest(
    int SchemaVersion,
    string OperationId,
    string PackageRelativePath,
    string ExpectedVersion,
    string ExpectedSha256,
    long ExpectedFileSize,
    int GamePid,
    string GameStartTimeUtc)
{
    public const int CurrentSchemaVersion = 1;

    public OperationResult<DateTimeOffset> Validate()
    {
        if (SchemaVersion != CurrentSchemaVersion)
            return OperationResult<DateTimeOffset>.Failure(
                "InvalidInstallRequest", "Unsupported request schema.");
        if (!PathSafety.IsValidOperationId(OperationId))
            return OperationResult<DateTimeOffset>.Failure(
                "InvalidOperationId", "Operation id is malformed.");
        if (!UpdateVersion.TryParse(ExpectedVersion, out _))
            return OperationResult<DateTimeOffset>.Failure(
                "InvalidVersion", "Expected version is malformed.");
        if (!PathSafety.IsValidSha256(ExpectedSha256))
            return OperationResult<DateTimeOffset>.Failure(
                "InvalidPackageHash", "Package SHA-256 is malformed.");
        if (ExpectedFileSize <= 0 || GamePid <= 0 ||
            string.IsNullOrWhiteSpace(PackageRelativePath))
        {
            return OperationResult<DateTimeOffset>.Failure(
                "InvalidInstallRequest",
                "Package path, size, and game PID are required.");
        }
        if (!DateTimeOffset.TryParse(
                GameStartTimeUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTimeOffset processStart) ||
            processStart == default ||
            processStart.Offset != TimeSpan.Zero)
        {
            return OperationResult<DateTimeOffset>.Failure(
                "InvalidProcessIdentity",
                "Game start time must be a round-trip UTC timestamp.");
        }

        return OperationResult<DateTimeOffset>.Success(processStart);
    }
}
