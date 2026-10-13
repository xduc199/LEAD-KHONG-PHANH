using LeadLauncher;

namespace LeadUpdater;

public sealed record ActiveSlotState(
    string InstallId,
    string StateRoot,
    ActivePointer Pointer,
    ValidatedSlot ActiveSlot);

public sealed class UpdateStateManager
{
    private readonly string installationRoot;
    private readonly string stateRoot;

    public UpdateStateManager(string installationRoot, string stateRoot)
    {
        this.installationRoot = Path.GetFullPath(installationRoot);
        this.stateRoot = Path.GetFullPath(stateRoot);
    }

    public OperationResult<ActiveSlotState> LoadActiveSlot()
    {
        try
        {
            if (!Directory.Exists(installationRoot))
                return OperationResult<ActiveSlotState>.Failure(
                    "RecoveryRequired", "Installation root does not exist.");
            OperationResult<bool> installRootCheck =
                PathSafety.EnsureNoReparsePoints(
                    installationRoot,
                    installationRoot);
            if (!installRootCheck.IsSuccess)
                return OperationResult<ActiveSlotState>.Failure(
                    "RecoveryRequired", installRootCheck.Details);

            string installIdPath = Path.Combine(installationRoot, "install.id");
            if (!File.Exists(installIdPath))
                return OperationResult<ActiveSlotState>.Failure(
                    "RecoveryRequired", "Installation identity file is missing.");
            if ((File.GetAttributes(installIdPath) & FileAttributes.ReparsePoint) != 0 ||
                new FileInfo(installIdPath).Length > 64)
            {
                return OperationResult<ActiveSlotState>.Failure(
                    "RecoveryRequired", "Installation identity file is unsafe.");
            }

            string installId = File.ReadAllText(installIdPath).Trim();
            if (!PathSafety.IsValidInstallId(installId))
                return OperationResult<ActiveSlotState>.Failure(
                    "RecoveryRequired", "Installation identity is malformed.");
            installId = installId.ToLowerInvariant();

            if (!Directory.Exists(stateRoot))
                return OperationResult<ActiveSlotState>.Failure(
                    "RecoveryRequired", "Updater state directory does not exist.");
            OperationResult<bool> stateRootCheck =
                PathSafety.EnsureNoReparsePoints(stateRoot, stateRoot);
            if (!stateRootCheck.IsSuccess)
                return OperationResult<ActiveSlotState>.Failure(
                    "RecoveryRequired", stateRootCheck.Details);

            string transactionPath = Path.Combine(stateRoot, "transaction.json");
            if (File.Exists(transactionPath) || Directory.Exists(transactionPath))
                return OperationResult<ActiveSlotState>.Failure(
                    "RecoveryRequired",
                    "A prior transaction exists and must be recovered by the launcher.");

            string activePath = Path.Combine(stateRoot, "active.json");
            OperationResult<ActivePointer> pointerResult =
                AtomicFileWriter.ReadJson<ActivePointer>(activePath);
            if (!pointerResult.IsSuccess || pointerResult.Value is null)
                return OperationResult<ActiveSlotState>.Failure(
                    "RecoveryRequired", pointerResult.Details);

            ActivePointer pointer = pointerResult.Value;
            OperationResult<LEADKhongPhanh.Update.UpdateVersion> pointerValidation =
                pointer.Validate(installId);
            if (!pointerValidation.IsSuccess)
                return OperationResult<ActiveSlotState>.Failure(
                    "RecoveryRequired", pointerValidation.Details);

            OperationResult<ValidatedSlot> activeResult =
                new LeadLauncher.SlotValidator(
                    installationRoot,
                    installId).Validate(pointer.ActiveSlotId);
            if (!activeResult.IsSuccess || activeResult.Value is null ||
                !pointer.Matches(activeResult.Value))
            {
                return OperationResult<ActiveSlotState>.Failure(
                    "RecoveryRequired",
                    activeResult.IsSuccess
                        ? "Active pointer does not match the validated slot."
                        : activeResult.Details);
            }

            return OperationResult<ActiveSlotState>.Success(
                new ActiveSlotState(
                    installId,
                    stateRoot,
                    pointer,
                    activeResult.Value));
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            NotSupportedException)
        {
            return OperationResult<ActiveSlotState>.Failure(
                "RecoveryRequired", exception.Message);
        }
    }

    public OperationResult<bool> WriteJournal(TransactionJournal journal) =>
        Convert(AtomicFileWriter.WriteJson(
            Path.Combine(stateRoot, "transaction.json"),
            journal));

    public OperationResult<TransactionJournal> ReadJournal() =>
        Convert(AtomicFileWriter.ReadJson<TransactionJournal>(
            Path.Combine(stateRoot, "transaction.json")));

    public OperationResult<bool> ClearJournal() =>
        Convert(AtomicFileWriter.DeleteFile(
            Path.Combine(stateRoot, "transaction.json")));

    private static OperationResult<T> Convert<T>(LeadLauncher.OperationResult<T> result) =>
        result.IsSuccess && result.Value is not null
            ? OperationResult<T>.Success(result.Value)
            : OperationResult<T>.Failure(result.ErrorCode, result.Details);
}
