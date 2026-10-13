using LEADKhongPhanh.Update;

namespace LeadLauncher;

public sealed record TransactionJournal(
    int SchemaVersion,
    string InstallId,
    string OperationId,
    TransactionPhase Phase,
    string OldSlotId,
    string NewSlotId,
    string OldVersion,
    string NewVersion,
    string? PackageSha256,
    int LauncherPid,
    DateTimeOffset LauncherStartTimeUtc,
    long Generation,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? TrialStartedAtUtc = null,
    int TrialProcessPid = 0,
    DateTimeOffset? TrialProcessStartTimeUtc = null)
{
    public const int CurrentSchemaVersion = 1;

    public OperationResult<bool> Validate(string expectedInstallId)
    {
        if (SchemaVersion != CurrentSchemaVersion)
            return OperationResult<bool>.Failure(
                "InvalidJournalSchema", "Unsupported transaction schema.");
        if (!PathSafety.IsValidInstallId(InstallId) ||
            !InstallId.Equals(expectedInstallId, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<bool>.Failure(
                "InstallIdMismatch", "Transaction install identity does not match.");
        }
        if (!PathSafety.IsValidOperationId(OperationId))
            return OperationResult<bool>.Failure(
                "InvalidOperationId", "Transaction operation id is malformed.");
        if (Phase == TransactionPhase.None)
            return OperationResult<bool>.Failure(
                "InvalidJournalPhase", "A persisted transaction cannot be NONE.");
        if (!Enum.IsDefined(Phase))
            return OperationResult<bool>.Failure(
                "InvalidJournalPhase", "Transaction phase is unknown.");
        if (!PathSafety.IsValidSlotId(OldSlotId) ||
            !PathSafety.IsValidSlotId(NewSlotId) ||
            OldSlotId.Equals(NewSlotId, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<bool>.Failure(
                "InvalidJournalSlots", "Transaction slot ids are invalid.");
        }
        if (!UpdateVersion.TryParse(OldVersion, out _) ||
            !UpdateVersion.TryParse(NewVersion, out _))
        {
            return OperationResult<bool>.Failure(
                "InvalidJournalVersion", "Transaction version is malformed.");
        }
        if (Generation < 1 || LauncherPid <= 0 ||
            LauncherStartTimeUtc == default || UpdatedAtUtc == default)
        {
            return OperationResult<bool>.Failure(
                "InvalidJournalIdentity", "Transaction identity fields are invalid.");
        }
        if (!PathSafety.IsValidSha256(PackageSha256))
        {
            return OperationResult<bool>.Failure(
                "InvalidPackageHash", "Transaction package SHA-256 is malformed.");
        }
        if ((Phase == TransactionPhase.Trial ||
             Phase == TransactionPhase.HealthyConfirmed ||
             Phase == TransactionPhase.Committed) &&
            (TrialStartedAtUtc is null ||
             TrialProcessPid <= 0 ||
             TrialProcessStartTimeUtc is null))
        {
            return OperationResult<bool>.Failure(
                "MissingTrialIdentity",
                "Trial time and launched game process identity are required.");
        }
        if (TrialStartedAtUtc is not null &&
            (TrialStartedAtUtc > UpdatedAtUtc ||
             TrialProcessStartTimeUtc > TrialStartedAtUtc))
        {
            return OperationResult<bool>.Failure(
                "InvalidTrialTime",
                "Trial timestamps are inconsistent with journal time.");
        }
        return OperationResult<bool>.Success(true);
    }

    public TransactionJournal WithPhase(
        TransactionPhase phase,
        DateTimeOffset nowUtc) =>
        this with
        {
            Phase = phase,
            UpdatedAtUtc = nowUtc,
            TrialStartedAtUtc = phase == TransactionPhase.Trial
                    ? nowUtc
                    : TrialStartedAtUtc,
                TrialProcessPid = TrialProcessPid
            };
}
