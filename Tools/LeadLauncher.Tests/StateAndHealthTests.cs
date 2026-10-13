using LeadLauncher;
using Xunit;

namespace LeadLauncher.Tests;

public sealed class StateAndHealthTests
{
    [Fact]
    public void ActivePointer_RejectsInstallIdentityMismatch()
    {
        using var workspace = new TestWorkspace();
        ValidatedSlot slot = workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 10, workspace.OldOperationId);

        ActivePointer pointer = workspace.Pointer(slot) with
        {
            InstallId = Guid.NewGuid().ToString("N")
        };

        Assert.Equal(
            "InstallIdMismatch",
            pointer.Validate(workspace.InstallId).ErrorCode);
    }

    [Fact]
    public void ActivePointer_RejectsAmbiguousSlotReferences()
    {
        using var workspace = new TestWorkspace();
        ValidatedSlot slot = workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 10, workspace.OldOperationId);
        ActivePointer pointer = workspace.Pointer(
            slot, workspace.OldSlotId);

        OperationResult<LEADKhongPhanh.Update.UpdateVersion> result =
            pointer.Validate(workspace.InstallId);

        Assert.Equal("PointerSlotConflict", result.ErrorCode);
    }

    [Fact]
    public void ActivePointer_RoundTripsStrictJson()
    {
        using var workspace = new TestWorkspace();
        ValidatedSlot slot = workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 10, workspace.OldOperationId);
        string path = Path.Combine(workspace.StateRoot, "active.json");
        workspace.WritePointer(path, workspace.Pointer(slot));

        OperationResult<ActivePointer> read =
            AtomicFileWriter.ReadJson<ActivePointer>(path);

        Assert.True(read.IsSuccess, read.Details);
        Assert.True(read.Value!.Validate(workspace.InstallId).IsSuccess);
    }

    [Fact]
    public void TransactionJournal_RejectsOperationIdMismatchShape()
    {
        using var workspace = new TestWorkspace();
        ValidatedSlot oldSlot = workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 10, workspace.OldOperationId,
            healthConfirmed: true);
        ValidatedSlot newSlot = workspace.CreateSlot(
            workspace.NewSlotId, "1.1.0", 11, workspace.OperationId);
        TransactionJournal journal = workspace.Journal(
            TransactionPhase.Prepared, oldSlot, newSlot) with
        {
            OperationId = "invalid"
        };

        Assert.Equal(
            "InvalidOperationId",
            journal.Validate(workspace.InstallId).ErrorCode);
    }

    [Fact]
    public void TransactionJournal_RejectsInstallIdMismatch()
    {
        using var workspace = new TestWorkspace();
        ValidatedSlot oldSlot = workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 10, workspace.OldOperationId,
            healthConfirmed: true);
        ValidatedSlot newSlot = workspace.CreateSlot(
            workspace.NewSlotId, "1.1.0", 11, workspace.OperationId);
        TransactionJournal journal = workspace.Journal(
            TransactionPhase.Prepared, oldSlot, newSlot);

        Assert.Equal(
            "InstallIdMismatch",
            journal.Validate(Guid.NewGuid().ToString("N")).ErrorCode);
    }

    [Fact]
    public void TransactionJournal_RejectsTrialWithoutProcessIdentity()
    {
        using var workspace = new TestWorkspace();
        ValidatedSlot oldSlot = workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 10, workspace.OldOperationId);
        ValidatedSlot newSlot = workspace.CreateSlot(
            workspace.NewSlotId, "1.1.0", 11, workspace.OperationId);
        TransactionJournal journal = workspace.Journal(
            TransactionPhase.Trial, oldSlot, newSlot);

        Assert.Equal(
            "MissingTrialIdentity",
            journal.Validate(workspace.InstallId).ErrorCode);
    }

    [Fact]
    public void ProcessIdentity_RequiresPidStartTimeAndExecutablePathMatch()
    {
        DateTimeOffset start = DateTimeOffset.UtcNow;
        var expected = new ProcessIdentity(123, start, "C:\\Slots\\game.exe");

        Assert.True(expected.Matches(
            new ProcessIdentity(123, start, "c:\\slots\\GAME.exe")));
        Assert.False(expected.Matches(
            new ProcessIdentity(124, start, "C:\\Slots\\game.exe")));
        Assert.False(expected.Matches(
            new ProcessIdentity(123, start.AddSeconds(1), "C:\\Slots\\game.exe")));
        Assert.False(expected.Matches(
            new ProcessIdentity(123, start, "C:\\Other\\game.exe")));
    }

    [Fact]
    public void ProcessIdentity_TryGetRejectsInvalidPid()
    {
        OperationResult<ProcessIdentity> result = ProcessIdentity.TryGet(0);

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidPid", result.ErrorCode);
    }

    [Fact]
    public void ProcessIdentity_TerminationRefusesReusedPidWithDifferentImage()
    {
        OperationResult<ProcessIdentity> current =
            ProcessIdentity.TryGet(Environment.ProcessId);
        Assert.True(current.IsSuccess, current.Details);
        var wrongIdentity = current.Value! with
        {
            ExecutablePath = Path.Combine(
                Path.GetTempPath(), "not-the-running-process.exe")
        };

        OperationResult<bool> result =
            ProcessIdentity.TryTerminate(wrongIdentity);

        Assert.False(result.IsSuccess);
        Assert.Equal("ProcessIdentityMismatch", result.ErrorCode);
    }

    [Fact]
    public void HealthReceipt_RejectsWrongOperationId()
    {
        using var workspace = new TestWorkspace();
        DateTimeOffset trialStart = workspace.Now.AddSeconds(-40);
        ValidatedSlot oldSlot = workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 10, workspace.OldOperationId);
        ValidatedSlot newSlot = workspace.CreateSlot(
            workspace.NewSlotId, "1.1.0", 11, workspace.OperationId);
        TransactionJournal journal = workspace.Journal(
            TransactionPhase.Trial,
            oldSlot,
            newSlot,
            trialStart,
            55,
            workspace.Now.AddMinutes(-1));
        var process = new ProcessIdentity(
            55, journal.TrialProcessStartTimeUtc!.Value,
            newSlot.ExecutablePath);
        HealthReceipt receipt = MakeReceipt(
            workspace, newSlot, journal, process) with
        {
            OperationId = Guid.NewGuid().ToString("N")
        };

        OperationResult<bool> result = HealthReceiptValidator.Validate(
            receipt, journal, newSlot, process, workspace.Now);

        Assert.Equal("HealthReceiptMismatch", result.ErrorCode);
    }

    [Fact]
    public void HealthReceipt_RejectsPidMismatch()
    {
        using var workspace = new TestWorkspace();
        (TransactionJournal journal, ValidatedSlot newSlot, ProcessIdentity process) =
            CreateTrialFixture(workspace);
        HealthReceipt receipt = MakeReceipt(
            workspace, newSlot, journal, process) with { Pid = process.Pid + 1 };

        OperationResult<bool> result = HealthReceiptValidator.Validate(
            receipt, journal, newSlot, process, workspace.Now);

        Assert.Equal("HealthReceiptMismatch", result.ErrorCode);
    }

    [Fact]
    public void HealthReceipt_RejectsProcessStartTimeMismatch()
    {
        using var workspace = new TestWorkspace();
        (TransactionJournal journal, ValidatedSlot newSlot, ProcessIdentity process) =
            CreateTrialFixture(workspace);
        HealthReceipt receipt = MakeReceipt(
            workspace, newSlot, journal, process) with
        {
            ProcessStartTimeUtc = process.ProcessStartTimeUtc.AddSeconds(-1)
        };

        OperationResult<bool> result = HealthReceiptValidator.Validate(
            receipt, journal, newSlot, process, workspace.Now);

        Assert.Equal("HealthReceiptMismatch", result.ErrorCode);
    }

    [Fact]
    public void HealthReceipt_RejectsExecutablePathMismatch()
    {
        using var workspace = new TestWorkspace();
        (TransactionJournal journal, ValidatedSlot newSlot, ProcessIdentity process) =
            CreateTrialFixture(workspace);
        HealthReceipt receipt = MakeReceipt(
            workspace, newSlot, journal, process) with
        {
            ExecutablePath = Path.Combine(workspace.Root, "outside.exe")
        };

        OperationResult<bool> result = HealthReceiptValidator.Validate(
            receipt, journal, newSlot, process, workspace.Now);

        Assert.Equal("HealthProcessMismatch", result.ErrorCode);
    }

    [Fact]
    public void HealthReceipt_RejectsSignalOutsideTrialWindow()
    {
        using var workspace = new TestWorkspace();
        (TransactionJournal journal, ValidatedSlot newSlot, ProcessIdentity process) =
            CreateTrialFixture(workspace);
        HealthReceipt receipt = MakeReceipt(
            workspace, newSlot, journal, process) with
        {
            HealthyAtUtc = journal.TrialStartedAtUtc!.Value +
                           HealthPolicy.TrialTimeout +
                           TimeSpan.FromSeconds(1)
        };

        OperationResult<bool> result = HealthReceiptValidator.Validate(
            receipt, journal, newSlot, process, workspace.Now);

        Assert.Equal("HealthReceiptMismatch", result.ErrorCode);
    }

    [Fact]
    public void HealthReceipt_AcceptsMatchingLiveProcessAndWindow()
    {
        using var workspace = new TestWorkspace();
        (TransactionJournal journal, ValidatedSlot newSlot, ProcessIdentity process) =
            CreateTrialFixture(workspace);
        HealthReceipt receipt = MakeReceipt(
            workspace, newSlot, journal, process);

        OperationResult<bool> result = HealthReceiptValidator.Validate(
            receipt, journal, newSlot, process, workspace.Now);

        Assert.True(result.IsSuccess, result.Details);
    }

    [Fact]
    public void AtomicFileWriter_WritesAndReplacesCompleteState()
    {
        using var workspace = new TestWorkspace();
        string path = Path.Combine(workspace.StateRoot, "state.json");

        Assert.True(AtomicFileWriter.WriteJson(
            path, new ActivePointer(1, workspace.InstallId, 1,
                workspace.OldSlotId, null, "1.0.0",
                workspace.OldOperationId, 10)).IsSuccess);
        Assert.True(AtomicFileWriter.WriteJson(
            path, new ActivePointer(1, workspace.InstallId, 2,
                workspace.NewSlotId, workspace.OldSlotId, "1.1.0",
                workspace.OperationId, 11)).IsSuccess);

        OperationResult<ActivePointer> result =
            AtomicFileWriter.ReadJson<ActivePointer>(path);

        Assert.True(result.IsSuccess, result.Details);
        Assert.Equal(2, result.Value!.Sequence);
        Assert.DoesNotContain(
            Directory.EnumerateFiles(workspace.StateRoot),
            file => file.EndsWith(".tmp", StringComparison.Ordinal));
    }

    [Fact]
    public void AtomicFileWriter_FailsWhenTargetCannotBeReplaced()
    {
        using var workspace = new TestWorkspace();
        string blocker = Path.Combine(workspace.StateRoot, "blocker");
        File.WriteAllText(blocker, "file");
        string impossibleTarget = Path.Combine(blocker, "state.json");

        OperationResult<bool> result =
            AtomicFileWriter.WriteAllText(impossibleTarget, "{}");

        Assert.False(result.IsSuccess);
        Assert.Equal("AtomicWriteFailed", result.ErrorCode);
    }

    [Fact]
    public void AtomicFileWriter_RejectsUnknownJsonProperties()
    {
        using var workspace = new TestWorkspace();
        string path = Path.Combine(workspace.StateRoot, "state.json");
        File.WriteAllText(path,
            "{\"schemaVersion\":1,\"installId\":\"" +
            workspace.InstallId +
            "\",\"sequence\":1,\"activeSlotId\":\"" +
            workspace.OldSlotId +
            "\",\"previousSlotId\":null,\"activeVersion\":\"1.0.0\"," +
            "\"operationId\":\"" + workspace.OldOperationId +
            "\",\"generation\":1,\"command\":\"cmd.exe\"}");

        OperationResult<ActivePointer> result =
            AtomicFileWriter.ReadJson<ActivePointer>(path);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void AtomicFileWriter_CleansOnlyStaleAtomicTemps()
    {
        using var workspace = new TestWorkspace();
        string stale = Path.Combine(workspace.StateRoot, ".active.json.old.tmp");
        string fresh = Path.Combine(workspace.StateRoot, ".active.json.new.tmp");
        File.WriteAllText(stale, "partial");
        File.WriteAllText(fresh, "writing");
        File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddDays(-2));

        OperationResult<bool> result =
            AtomicFileWriter.CleanupStaleTemporaryFiles(workspace.StateRoot);

        Assert.True(result.IsSuccess, result.Details);
        Assert.False(File.Exists(stale));
        Assert.True(File.Exists(fresh));
    }

    [Fact]
    public void TransactionJournal_UsesUppercaseNamedPhaseTokens()
    {
        using var workspace = new TestWorkspace();
        ValidatedSlot oldSlot = workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 10, workspace.OldOperationId,
            healthConfirmed: true);
        ValidatedSlot newSlot = workspace.CreateSlot(
            workspace.NewSlotId, "1.1.0", 11, workspace.OperationId);
        TransactionJournal journal = workspace.Journal(
            TransactionPhase.Prepared, oldSlot, newSlot);
        string path = Path.Combine(workspace.StateRoot, "transaction.json");

        Assert.True(AtomicFileWriter.WriteJson(path, journal).IsSuccess);
        Assert.Contains("\"phase\": \"PREPARED\"", File.ReadAllText(path));
        Assert.True(
            AtomicFileWriter.ReadJson<TransactionJournal>(path).IsSuccess);
    }

    private static (TransactionJournal Journal, ValidatedSlot Slot,
        ProcessIdentity Process) CreateTrialFixture(TestWorkspace workspace)
    {
        DateTimeOffset start = workspace.Now.AddSeconds(-40);
        DateTimeOffset processStart = workspace.Now.AddMinutes(-2);
        ValidatedSlot oldSlot = workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 10, workspace.OldOperationId);
        ValidatedSlot newSlot = workspace.CreateSlot(
            workspace.NewSlotId, "1.1.0", 11, workspace.OperationId);
        TransactionJournal journal = workspace.Journal(
            TransactionPhase.Trial,
            oldSlot,
            newSlot,
            start,
            987,
            processStart);
        var process = new ProcessIdentity(
            987, processStart, newSlot.ExecutablePath);
        return (journal, newSlot, process);
    }

    private static HealthReceipt MakeReceipt(
        TestWorkspace workspace,
        ValidatedSlot slot,
        TransactionJournal journal,
        ProcessIdentity process) =>
        new(
            HealthReceipt.CurrentSchemaVersion,
            workspace.OperationId,
            slot.Metadata.SlotId,
            slot.Metadata.Version,
            process.Pid,
            process.ProcessStartTimeUtc,
            process.ExecutablePath,
            journal.TrialStartedAtUtc!.Value.AddSeconds(1));
}
