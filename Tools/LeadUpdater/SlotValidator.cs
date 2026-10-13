using LeadLauncher;

namespace LeadUpdater;

public sealed class SlotValidator
{
    private readonly LeadLauncher.SlotValidator inner;

    public SlotValidator(string installationRoot, string installId)
    {
        inner = new LeadLauncher.SlotValidator(installationRoot, installId);
    }

    public OperationResult<ValidatedSlot> Validate(
        string slotId,
        string expectedOperationId,
        string expectedVersion,
        string expectedPackageSha256,
        long expectedGeneration)
    {
        OperationResult<ValidatedSlot> result = inner.Validate(slotId);
        if (!result.IsSuccess || result.Value is null)
            return OperationResult<ValidatedSlot>.Failure(
                "SlotValidationFailed", result.Details);

        SlotInfo info = result.Value.Metadata;
        if (!info.OperationId.Equals(
                expectedOperationId,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                info.Version,
                expectedVersion,
                StringComparison.Ordinal) ||
            !string.Equals(
                info.PackageSha256,
                expectedPackageSha256,
                StringComparison.OrdinalIgnoreCase) ||
            info.Generation != expectedGeneration ||
            info.HealthConfirmed ||
            !info.ExecutableRelativePath.Equals(
                UpdaterOptions.ExpectedExecutableRelativePath,
                StringComparison.Ordinal))
        {
            return OperationResult<ValidatedSlot>.Failure(
                "SlotValidationFailed",
                "Staged slot metadata differs from the trusted operation contract.");
        }

        string expectedData = Path.Combine(
            result.Value.SlotRoot,
            UpdaterOptions.ExpectedDataDirectoryName);
        if (!result.Value.ExecutablePath.Equals(
                Path.GetFullPath(Path.Combine(
                    result.Value.SlotRoot,
                    UpdaterOptions.ExpectedExecutableRelativePath)),
                StringComparison.OrdinalIgnoreCase) ||
            !result.Value.DataDirectory.Equals(
                Path.GetFullPath(expectedData),
                StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<ValidatedSlot>.Failure(
                "SlotValidationFailed",
                "Executable or Unity data directory differs from the build contract.");
        }

        return result;
    }
}
