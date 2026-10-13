using LEADKhongPhanh.Update;

namespace LeadLauncher;

public sealed record SlotInfo(
    int SchemaVersion,
    string SlotId,
    string InstallId,
    string Version,
    string ExecutableRelativePath,
    long Generation,
    string OperationId,
    DateTimeOffset PreparedAtUtc,
    bool HealthConfirmed,
    string? PackageSha256)
{
    public const int CurrentSchemaVersion = 1;

    public OperationResult<UpdateVersion> ValidateFor(
        string expectedInstallId,
        string expectedSlotId)
    {
        if (SchemaVersion != CurrentSchemaVersion)
            return OperationResult<UpdateVersion>.Failure(
                "InvalidSlotSchema", "Unsupported slot metadata schema.");
        if (!PathSafety.IsValidInstallId(InstallId) ||
            !InstallId.Equals(expectedInstallId, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<UpdateVersion>.Failure(
                "InstallIdMismatch", "Slot install identity does not match.");
        }
        if (!PathSafety.IsValidSlotId(SlotId) ||
            !SlotId.Equals(expectedSlotId, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<UpdateVersion>.Failure(
                "SlotIdMismatch", "Slot metadata identity does not match its directory.");
        }
        if (!UpdateVersion.TryParse(Version, out UpdateVersion version))
            return OperationResult<UpdateVersion>.Failure(
                "InvalidVersion", "Slot version is malformed.");
        if (Generation < 1)
            return OperationResult<UpdateVersion>.Failure(
                "InvalidGeneration", "Slot generation must be positive.");
        if (!PathSafety.IsValidOperationId(OperationId))
            return OperationResult<UpdateVersion>.Failure(
                "InvalidOperationId", "Slot operation id is malformed.");
        if (PreparedAtUtc == default)
            return OperationResult<UpdateVersion>.Failure(
                "InvalidPreparedAt", "Slot preparation time is required.");
        if (PackageSha256 is not null &&
            !PathSafety.IsValidSha256(PackageSha256))
        {
            return OperationResult<UpdateVersion>.Failure(
                "InvalidPackageHash", "Slot package SHA-256 is malformed.");
        }

        if (string.IsNullOrWhiteSpace(ExecutableRelativePath))
            return OperationResult<UpdateVersion>.Failure(
                "InvalidExecutablePath", "Executable path is required.");

        return OperationResult<UpdateVersion>.Success(version);
    }
}
