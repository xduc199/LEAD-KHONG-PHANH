using LEADKhongPhanh.Update;

namespace LeadLauncher;

public sealed record ActivePointer(
    int SchemaVersion,
    string InstallId,
    long Sequence,
    string ActiveSlotId,
    string? PreviousSlotId,
    string ActiveVersion,
    string OperationId,
    long Generation)
{
    public const int CurrentSchemaVersion = 1;

    public OperationResult<UpdateVersion> Validate(string expectedInstallId)
    {
        if (SchemaVersion != CurrentSchemaVersion)
            return OperationResult<UpdateVersion>.Failure(
                "InvalidPointerSchema", "Unsupported active pointer schema.");
        if (!PathSafety.IsValidInstallId(InstallId) ||
            !InstallId.Equals(expectedInstallId, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<UpdateVersion>.Failure(
                "InstallIdMismatch", "Pointer install identity does not match.");
        }
        if (Sequence < 1 || Generation < 1)
            return OperationResult<UpdateVersion>.Failure(
                "InvalidPointerSequence", "Pointer sequence and generation must be positive.");
        if (!PathSafety.IsValidSlotId(ActiveSlotId) ||
            (PreviousSlotId is not null &&
             !PathSafety.IsValidSlotId(PreviousSlotId)))
        {
            return OperationResult<UpdateVersion>.Failure(
                "InvalidPointerSlot", "Pointer references an invalid slot id.");
        }
        if (PreviousSlotId is not null &&
            PreviousSlotId.Equals(ActiveSlotId, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<UpdateVersion>.Failure(
                "PointerSlotConflict", "Active and previous slots must differ.");
        }
        if (!PathSafety.IsValidOperationId(OperationId))
            return OperationResult<UpdateVersion>.Failure(
                "InvalidOperationId", "Pointer operation id is malformed.");
        return UpdateVersion.TryParse(ActiveVersion, out UpdateVersion version)
            ? OperationResult<UpdateVersion>.Success(version)
            : OperationResult<UpdateVersion>.Failure(
                "InvalidVersion", "Pointer version is malformed.");
    }

    public bool Matches(ValidatedSlot slot) =>
        slot.Metadata.SlotId.Equals(
            ActiveSlotId,
            StringComparison.OrdinalIgnoreCase) &&
        slot.Metadata.Version == ActiveVersion &&
        slot.Metadata.Generation == Generation &&
        slot.Metadata.OperationId.Equals(
            OperationId,
            StringComparison.OrdinalIgnoreCase);
}
