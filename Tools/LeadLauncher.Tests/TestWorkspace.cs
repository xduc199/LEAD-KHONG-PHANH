using LEADKhongPhanh.Update;
using LeadLauncher;

namespace LeadLauncher.Tests;

internal sealed class TestWorkspace : IDisposable
{
    public const string PackageSha256 =
        "0123456789abcdef0123456789abcdef" +
        "0123456789abcdef0123456789abcdef";
    public string Root { get; } = Path.Combine(
        Path.GetTempPath(),
        "lead-launcher-tests-" + Guid.NewGuid().ToString("N"));
    public string InstallId { get; } = Guid.NewGuid().ToString("N");
    public string OldSlotId { get; } = "slot-" + Guid.NewGuid().ToString("N");
    public string NewSlotId { get; } = "slot-" + Guid.NewGuid().ToString("N");
    public string OldOperationId { get; } = Guid.NewGuid().ToString("N");
    public string OperationId { get; } = Guid.NewGuid().ToString("N");
    public string StateRoot => Path.Combine(Root, "local", InstallId, "Updater");
    public string SlotsRoot => Path.Combine(Root, "install", "Slots");
    public string InstallationRoot => Path.Combine(Root, "install");
    public DateTimeOffset Now { get; } = DateTimeOffset.UtcNow;

    public TestWorkspace()
    {
        Directory.CreateDirectory(InstallationRoot);
        Directory.CreateDirectory(SlotsRoot);
        Directory.CreateDirectory(StateRoot);
        Directory.CreateDirectory(Path.Combine(StateRoot, "Signals"));
        Directory.CreateDirectory(Path.Combine(StateRoot, "Logs"));
    }

    public ValidatedSlot CreateSlot(
        string slotId,
        string version,
        long generation,
        string operationId,
        string? packageSha256 = PackageSha256,
        bool healthConfirmed = false)
    {
        string slotRoot = Path.Combine(SlotsRoot, slotId);
        Directory.CreateDirectory(slotRoot);
        string executable = Path.Combine(slotRoot, "LEADKhongPhanh.exe");
        File.WriteAllText(executable, "fixture executable");
        Directory.CreateDirectory(Path.Combine(
            slotRoot, "LEADKhongPhanh_Data"));
        var info = new SlotInfo(
            SlotInfo.CurrentSchemaVersion,
            slotId,
            InstallId,
            version,
            "LEADKhongPhanh.exe",
            generation,
            operationId,
            Now,
            healthConfirmed,
            packageSha256);
        AssertWrite(AtomicFileWriter.WriteJson(
            Path.Combine(slotRoot, "slot.json"),
            info));

        OperationResult<ValidatedSlot> result =
            new SlotValidator(InstallationRoot, InstallId).Validate(slotId);
        if (!result.IsSuccess || result.Value is null)
            throw new InvalidOperationException(result.Details);
        return result.Value;
    }

    public ActivePointer Pointer(ValidatedSlot active, string? previous = null,
        long sequence = 1) =>
        new(
            ActivePointer.CurrentSchemaVersion,
            InstallId,
            sequence,
            active.Metadata.SlotId,
            previous,
            active.Metadata.Version,
            active.Metadata.OperationId,
            active.Metadata.Generation);

    public TransactionJournal Journal(
        TransactionPhase phase,
        ValidatedSlot oldSlot,
        ValidatedSlot newSlot,
        DateTimeOffset? trialStart = null,
        int trialPid = 0,
        DateTimeOffset? processStart = null,
        string? operationId = null,
        string? installId = null,
        long? generation = null) =>
        new(
            TransactionJournal.CurrentSchemaVersion,
            installId ?? InstallId,
            operationId ?? OperationId,
            phase,
            oldSlot.Metadata.SlotId,
            newSlot.Metadata.SlotId,
            oldSlot.Metadata.Version,
            newSlot.Metadata.Version,
            newSlot.Metadata.PackageSha256,
            Environment.ProcessId,
            Now.AddMinutes(-1),
            generation ?? newSlot.Metadata.Generation,
            Now,
            trialStart,
            trialPid,
            processStart);

    public LauncherOptions Options(
        string? stateRoot = null,
        string? stableLauncherPath = null) =>
        new(
            InstallationRoot,
            stateRoot ?? StateRoot,
            InstallId,
            stableLauncherPath ??
                Path.Combine(InstallationRoot, "LEADKhongPhanh.exe"));

    public void WritePointer(string path, ActivePointer pointer) =>
        AssertWrite(AtomicFileWriter.WriteJson(path, pointer));

    public void WriteJournal(TransactionJournal journal) =>
        AssertWrite(AtomicFileWriter.WriteJson(
            Path.Combine(StateRoot, "transaction.json"),
            journal));

    public void WriteHealth(HealthReceipt receipt) =>
        AssertWrite(AtomicFileWriter.WriteJson(
            Path.Combine(
                StateRoot,
                "Signals",
                receipt.OperationId + ".healthy.json"),
            receipt));

    public static void AssertWrite(OperationResult<bool> result)
    {
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Details);
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
            Directory.Delete(Root, recursive: true);
    }
}
