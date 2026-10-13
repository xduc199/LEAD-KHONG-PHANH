using LeadLauncher;
using Xunit;

namespace LeadLauncher.Tests;

public sealed class TransactionAndRecoveryTests
{
    [Fact]
    public void StateMachine_AllowsRequiredTransitions()
    {
        Assert.True(TransactionStateMachine.CanTransition(
            TransactionPhase.None, TransactionPhase.Preparing));
        Assert.True(TransactionStateMachine.CanTransition(
            TransactionPhase.Preparing, TransactionPhase.Prepared));
        Assert.True(TransactionStateMachine.CanTransition(
            TransactionPhase.Prepared, TransactionPhase.Trial));
        Assert.True(TransactionStateMachine.CanTransition(
            TransactionPhase.Trial, TransactionPhase.HealthyConfirmed));
        Assert.True(TransactionStateMachine.CanTransition(
            TransactionPhase.HealthyConfirmed, TransactionPhase.Committed));
        Assert.True(TransactionStateMachine.CanTransition(
            TransactionPhase.RollbackRequired, TransactionPhase.RolledBack));
        Assert.True(TransactionStateMachine.CanTransition(
            TransactionPhase.RolledBack, TransactionPhase.None));
    }

    [Theory]
    [InlineData(TransactionPhase.None, TransactionPhase.Committed)]
    [InlineData(TransactionPhase.Prepared, TransactionPhase.Committed)]
    [InlineData(TransactionPhase.Trial, TransactionPhase.Committed)]
    [InlineData(TransactionPhase.Committed, TransactionPhase.Trial)]
    [InlineData(TransactionPhase.Preparing, TransactionPhase.HealthyConfirmed)]
    public void StateMachine_RejectsIllegalTransitions(
        TransactionPhase current,
        TransactionPhase next)
    {
        Assert.False(TransactionStateMachine.CanTransition(current, next));
    }

    [Fact]
    public void StateMachine_StartCreatesPreparingJournal()
    {
        using var workspace = new TestWorkspace();
        ValidatedSlot oldSlot = workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 10, workspace.OldOperationId,
            healthConfirmed: true);
        ValidatedSlot newSlot = workspace.CreateSlot(
            workspace.NewSlotId, "1.1.0", 11, workspace.OperationId);
        TransactionJournal draft = workspace.Journal(
            TransactionPhase.None, oldSlot, newSlot);

        OperationResult<TransactionJournal> started =
            TransactionStateMachine.Start(draft, workspace.Now);

        Assert.True(started.IsSuccess, started.Details);
        Assert.Equal(TransactionPhase.Preparing, started.Value!.Phase);
    }

    [Fact]
    public async Task Recovery_PreparedRestoresOldActiveSlot()
    {
        using var workspace = new TestWorkspace();
        Scenario scenario = CreateScenario(workspace, TransactionPhase.Prepared);
        workspace.WritePointer(
            Path.Combine(workspace.StateRoot, "active.json"),
            workspace.Pointer(scenario.OldSlot));
        workspace.WriteJournal(scenario.Journal);

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(
                workspace.Options(),
                launchTrial: (_, _) =>
                    OperationResult<ProcessIdentity>.Failure(
                        "LaunchFailed", "Expected fixture launch failure."))
                .RecoverAsync();

        Assert.True(result.IsSuccess, result.Details);
        Assert.Equal(scenario.OldSlot.Metadata.SlotId,
            result.Value!.ActiveSlot.Metadata.SlotId);
        Assert.False(File.Exists(workspace.Options().TransactionPath));
    }

    [Fact]
    public async Task Recovery_TrialWithoutReceiptRollsBackAfterTimeout()
    {
        using var workspace = new TestWorkspace();
        DateTimeOffset started = workspace.Now.AddMinutes(-3);
        DateTimeOffset processStart = workspace.Now.AddMinutes(-5);
        Scenario scenario = CreateScenario(
            workspace,
            TransactionPhase.Trial,
            started,
            Environment.ProcessId,
            processStart);
        SetupTrialPointers(workspace, scenario);
        workspace.WriteJournal(scenario.Journal);

        var recovery = new JournalRecovery(
            workspace.Options(),
            utcNow: () => workspace.Now,
            processLookup: _ => OperationResult<ProcessIdentity>.Success(
                new ProcessIdentity(
                    Environment.ProcessId,
                    processStart,
                    scenario.NewSlot.ExecutablePath)),
            terminateTrialProcess: _ =>
                OperationResult<bool>.Success(true));
        OperationResult<RecoveryOutcome> result =
            await recovery.RecoverAsync();

        Assert.True(result.IsSuccess, result.Details);
        Assert.Equal(scenario.OldSlot.Metadata.SlotId,
            result.Value!.ActiveSlot.Metadata.SlotId);
    }

    [Fact]
    public async Task Recovery_TrialWithValidHealthAndStableProcessCommits()
    {
        using var workspace = new TestWorkspace();
        DateTimeOffset started = workspace.Now.AddMinutes(-2);
        DateTimeOffset processStart = workspace.Now.AddMinutes(-5);
        const int trialPid = 12345;
        Scenario scenario = CreateScenario(
            workspace,
            TransactionPhase.Trial,
            started,
            trialPid,
            processStart);
        SetupTrialPointers(workspace, scenario);
        workspace.WriteJournal(scenario.Journal);
        var actual = new ProcessIdentity(
            trialPid, processStart, scenario.NewSlot.ExecutablePath);
        workspace.WriteHealth(new HealthReceipt(
            HealthReceipt.CurrentSchemaVersion,
            workspace.OperationId,
            scenario.NewSlot.Metadata.SlotId,
            scenario.NewSlot.Metadata.Version,
            trialPid,
            processStart,
            scenario.NewSlot.ExecutablePath,
            started.AddSeconds(1)));

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(
                workspace.Options(),
                utcNow: () => started.AddSeconds(31),
                processLookup: _ =>
                    OperationResult<ProcessIdentity>.Success(actual))
            .RecoverAsync();

        Assert.True(result.IsSuccess, result.Details);
        Assert.Equal(scenario.NewSlot.Metadata.SlotId,
            result.Value!.ActiveSlot.Metadata.SlotId);
        OperationResult<SlotInfo> metadata =
            AtomicFileWriter.ReadJson<SlotInfo>(
                Path.Combine(
                    scenario.NewSlot.SlotRoot,
                    "slot.json"));
        Assert.True(metadata.IsSuccess, metadata.Details);
        Assert.True(metadata.Value!.HealthConfirmed);
        Assert.Equal(trialPid, result.Value.ExistingTrialProcess!.Pid);
        Assert.False(File.Exists(workspace.Options().TransactionPath));
    }

    [Fact]
    public async Task Recovery_HealthyConfirmedCommitsNewPointer()
    {
        using var workspace = new TestWorkspace();
        Scenario scenario = CreateScenario(
            workspace,
            TransactionPhase.HealthyConfirmed,
            workspace.Now.AddSeconds(-31),
            12345,
            workspace.Now.AddMinutes(-2));
        workspace.WritePointer(
            Path.Combine(workspace.StateRoot, "active.json"),
            workspace.Pointer(scenario.OldSlot));
        workspace.WriteJournal(scenario.Journal);
        workspace.WriteHealth(CreateReceipt(workspace, scenario));

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.True(result.IsSuccess, result.Details);
        Assert.Equal(scenario.NewSlot.Metadata.SlotId,
            result.Value!.ActiveSlot.Metadata.SlotId);
    }

    [Fact]
    public async Task Recovery_HealthyConfirmedAfterPreviousWriteCompletesCommit()
    {
        using var workspace = new TestWorkspace();
        Scenario scenario = CreateScenario(
            workspace,
            TransactionPhase.HealthyConfirmed,
            workspace.Now.AddSeconds(-31),
            12345,
            workspace.Now.AddMinutes(-2));
        ActivePointer oldPointer = workspace.Pointer(scenario.OldSlot);
        workspace.WritePointer(
            workspace.Options().ActivePointerPath,
            oldPointer);
        workspace.WritePointer(
            workspace.Options().PreviousPointerPath,
            oldPointer);
        workspace.WriteJournal(scenario.Journal);
        workspace.WriteHealth(CreateReceipt(workspace, scenario));

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.True(result.IsSuccess, result.Details);
        Assert.Equal(
            scenario.NewSlot.Metadata.SlotId,
            result.Value!.ActiveSlot.Metadata.SlotId);
        Assert.True(AtomicFileWriter.ReadJson<ActivePointer>(
            workspace.Options().PreviousPointerPath).Value!
            .Matches(scenario.OldSlot));
    }

    [Fact]
    public async Task Recovery_HealthyConfirmedAfterActiveWriteCompletesJournal()
    {
        using var workspace = new TestWorkspace();
        Scenario scenario = CreateScenario(
            workspace,
            TransactionPhase.HealthyConfirmed,
            workspace.Now.AddSeconds(-31),
            12345,
            workspace.Now.AddMinutes(-2));
        workspace.WritePointer(
            workspace.Options().PreviousPointerPath,
            workspace.Pointer(scenario.OldSlot));
        workspace.WritePointer(
            workspace.Options().ActivePointerPath,
            workspace.Pointer(
                scenario.NewSlot,
                scenario.OldSlot.Metadata.SlotId,
                sequence: 2));
        string slotInfoPath = Path.Combine(
            scenario.NewSlot.SlotRoot,
            "slot.json");
        TestWorkspace.AssertWrite(AtomicFileWriter.WriteJson(
            slotInfoPath,
            scenario.NewSlot.Metadata with { HealthConfirmed = true }));
        workspace.WriteJournal(scenario.Journal);
        workspace.WriteHealth(CreateReceipt(workspace, scenario));

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.True(result.IsSuccess, result.Details);
        Assert.Equal(
            scenario.NewSlot.Metadata.SlotId,
            result.Value!.ActiveSlot.Metadata.SlotId);
        Assert.False(File.Exists(workspace.Options().TransactionPath));
    }

    [Fact]
    public async Task EndToEnd_PreparedTrialConfirmsHealthAndCommitsAtomically()
    {
        using var workspace = new TestWorkspace();
        Scenario scenario = CreateScenario(
            workspace,
            TransactionPhase.Prepared);
        CopyTrialFixture(scenario.NewSlot.SlotRoot);

        string activePath = workspace.Options().ActivePointerPath;
        ActivePointer oldPointer = workspace.Pointer(scenario.OldSlot);
        workspace.WritePointer(activePath, oldPointer);
        byte[] activeBeforeTrial = File.ReadAllBytes(activePath);
        workspace.WriteJournal(scenario.Journal);

        TimeSpan logicalAdvance = TimeSpan.Zero;
        ProcessIdentity? fixtureProcess = null;
        bool observedTrial = false;
        bool completedHealthyWindow = false;
        var gameLauncher = new GameLauncher();
        var recovery = new JournalRecovery(
            workspace.Options(),
            utcNow: () => DateTimeOffset.UtcNow + logicalAdvance,
            processLookup: ProcessIdentity.TryGet,
            delay: async (_, cancellationToken) =>
            {
                TransactionJournal running =
                    AtomicFileWriter.ReadJson<TransactionJournal>(
                        workspace.Options().TransactionPath).Value!;
                Assert.Equal(TransactionPhase.Trial, running.Phase);
                Assert.Equal(
                    activeBeforeTrial,
                    File.ReadAllBytes(activePath));
                observedTrial = true;
                string receiptPath = Path.Combine(
                    workspace.Options().SignalsRoot,
                    workspace.OperationId + ".healthy.json");
                logicalAdvance += File.Exists(receiptPath) &&
                                  !completedHealthyWindow
                    ? TimeSpan.FromSeconds(30)
                    : TimeSpan.FromSeconds(1);
                if (File.Exists(receiptPath))
                    completedHealthyWindow = true;
                await Task.Delay(
                    TimeSpan.FromMilliseconds(50),
                    cancellationToken);
            },
            launchTrial: (slot, journal) =>
            {
                OperationResult<ProcessIdentity> launch =
                    gameLauncher.LaunchTrial(
                        slot,
                        journal,
                        workspace.Options().SignalsRoot);
                if (launch.IsSuccess)
                    fixtureProcess = launch.Value;
                return launch;
            });

        try
        {
            OperationResult<RecoveryOutcome> result =
                await recovery.RecoverAsync();

            Assert.True(result.IsSuccess, result.Details);
            Assert.True(observedTrial);
            Assert.True(
                scenario.NewSlot.Metadata.SlotId.Equals(
                    result.Value!.ActiveSlot.Metadata.SlotId,
                    StringComparison.Ordinal),
                result.Value.Decision);
            Assert.Equal(
                "COMMITTED transaction verified and cleared.",
                result.Value.Decision);
            Assert.Equal(
                scenario.NewSlot.Metadata.SlotId,
                AtomicFileWriter.ReadJson<ActivePointer>(
                    activePath).Value!.ActiveSlotId);

            ActivePointer previous =
                AtomicFileWriter.ReadJson<ActivePointer>(
                    workspace.Options().PreviousPointerPath).Value!;
            Assert.True(previous.Matches(scenario.OldSlot));
            Assert.False(File.Exists(
                workspace.Options().TransactionPath));
            Assert.True(AtomicFileWriter.ReadJson<SlotInfo>(
                Path.Combine(scenario.NewSlot.SlotRoot, "slot.json"))
                .Value!.HealthConfirmed);
        }
        finally
        {
            if (fixtureProcess is not null)
                ProcessIdentity.TryTerminate(fixtureProcess);
        }
    }

    [Fact]
    public async Task Recovery_CommittedClearsCompletedJournal()
    {
        using var workspace = new TestWorkspace();
        Scenario scenario = CreateScenario(
            workspace,
            TransactionPhase.Committed,
            workspace.Now.AddSeconds(-31),
            12345,
            workspace.Now.AddMinutes(-2));
        SetupTrialPointers(workspace, scenario);
        workspace.WriteHealth(CreateReceipt(workspace, scenario));
        workspace.WriteJournal(scenario.Journal);

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.True(result.IsSuccess, result.Details);
        Assert.Equal(scenario.NewSlot.Metadata.SlotId,
            result.Value!.ActiveSlot.Metadata.SlotId);
        Assert.False(File.Exists(workspace.Options().TransactionPath));
    }

    [Fact]
    public async Task Recovery_CommittedCompletesNewActiveFromPreviousAfterCorruption()
    {
        using var workspace = new TestWorkspace();
        Scenario scenario = CreateScenario(
            workspace,
            TransactionPhase.Committed,
            workspace.Now.AddSeconds(-31),
            12345,
            workspace.Now.AddMinutes(-2));
        workspace.WritePointer(
            Path.Combine(workspace.StateRoot, "active.json"),
            workspace.Pointer(scenario.NewSlot, scenario.OldSlot.Metadata.SlotId, 2));
        workspace.WritePointer(
            Path.Combine(workspace.StateRoot, "active.previous.json"),
            workspace.Pointer(scenario.OldSlot, sequence: 1));
        workspace.WriteHealth(CreateReceipt(workspace, scenario));
        File.WriteAllText(
            workspace.Options().ActivePointerPath,
            "{ damaged");
        workspace.WriteJournal(scenario.Journal);

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.True(result.IsSuccess, result.Details);
        Assert.Equal(scenario.NewSlot.Metadata.SlotId,
            result.Value!.ActiveSlot.Metadata.SlotId);
    }

    [Fact]
    public async Task Recovery_RollbackRequiredRestoresOldSlot()
    {
        using var workspace = new TestWorkspace();
        Scenario scenario = CreateScenario(
            workspace, TransactionPhase.RollbackRequired);
        workspace.WritePointer(
            Path.Combine(workspace.StateRoot, "active.json"),
            workspace.Pointer(scenario.OldSlot));
        workspace.WriteJournal(scenario.Journal);

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.True(result.IsSuccess, result.Details);
        Assert.Equal(scenario.OldSlot.Metadata.SlotId,
            result.Value!.ActiveSlot.Metadata.SlotId);
    }

    [Fact]
    public async Task Recovery_RolledBackClearsJournal()
    {
        using var workspace = new TestWorkspace();
        Scenario scenario = CreateScenario(
            workspace, TransactionPhase.RolledBack);
        workspace.WritePointer(
            Path.Combine(workspace.StateRoot, "active.json"),
            workspace.Pointer(scenario.OldSlot));
        workspace.WriteJournal(scenario.Journal);

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.True(result.IsSuccess, result.Details);
        Assert.False(File.Exists(workspace.Options().TransactionPath));
    }

    [Fact]
    public async Task Recovery_PreparingWorksWhenNewSlotIsIncomplete()
    {
        using var workspace = new TestWorkspace();
        ValidatedSlot oldSlot = workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 10, workspace.OldOperationId,
            healthConfirmed: true);
        string slotDirectory = Path.Combine(workspace.SlotsRoot, workspace.NewSlotId);
        Directory.CreateDirectory(slotDirectory);
        TransactionJournal journal = workspace.Journal(
            TransactionPhase.Preparing,
            oldSlot,
            new ValidatedSlot(
                new SlotInfo(1, workspace.NewSlotId, workspace.InstallId,
                    "1.1.0", "LEADKhongPhanh.exe", 11,
                    workspace.OperationId,
                    workspace.Now,
                    false,
                    TestWorkspace.PackageSha256),
                slotDirectory,
                Path.Combine(slotDirectory, "LEADKhongPhanh.exe"),
                Path.Combine(slotDirectory, "LEADKhongPhanh_Data")));
        workspace.WritePointer(
            Path.Combine(workspace.StateRoot, "active.json"),
            workspace.Pointer(oldSlot));
        workspace.WriteJournal(journal);

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.True(result.IsSuccess, result.Details);
        Assert.Equal(oldSlot.Metadata.SlotId,
            result.Value!.ActiveSlot.Metadata.SlotId);
    }

    [Fact]
    public async Task Recovery_FailsClosedOnCorruptedTransactionJson()
    {
        using var workspace = new TestWorkspace();
        File.WriteAllText(workspace.Options().TransactionPath, "{ broken");

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidTransaction", result.ErrorCode);
    }

    [Fact]
    public async Task Recovery_FailsClosedOnCorruptedActiveJsonWithoutJournal()
    {
        using var workspace = new TestWorkspace();
        File.WriteAllText(workspace.Options().ActivePointerPath, "{ broken");

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Recovery_FailsClosedWhenActiveSlotExecutableIsMissing()
    {
        using var workspace = new TestWorkspace();
        ValidatedSlot slot = workspace.CreateSlot(
            workspace.OldSlotId,
            "1.0.0",
            10,
            workspace.OldOperationId,
            healthConfirmed: true);
        workspace.WritePointer(
            workspace.Options().ActivePointerPath,
            workspace.Pointer(slot));
        File.Delete(slot.ExecutablePath);

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Recovery_FailsClosedOnMissingActivePointer()
    {
        using var workspace = new TestWorkspace();

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("ActivePointerInvalid", result.ErrorCode);
    }

    [Fact]
    public async Task Recovery_RejectsInstallIdMismatch()
    {
        using var workspace = new TestWorkspace();
        Scenario scenario = CreateScenario(
            workspace,
            TransactionPhase.Prepared,
            installId: Guid.NewGuid().ToString("N"));
        workspace.WriteJournal(scenario.Journal);

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("InstallIdMismatch", result.ErrorCode);
    }

    [Fact]
    public async Task Recovery_RejectsOperationIdMismatch()
    {
        using var workspace = new TestWorkspace();
        Scenario scenario = CreateScenario(
            workspace,
            TransactionPhase.Trial,
            workspace.Now.AddSeconds(-10),
            12345,
            workspace.Now.AddMinutes(-1),
            operationId: Guid.NewGuid().ToString("N"));
        workspace.WriteJournal(scenario.Journal);

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("TransactionInconsistent", result.ErrorCode);
    }

    [Fact]
    public async Task Recovery_RejectsGenerationConflict()
    {
        using var workspace = new TestWorkspace();
        Scenario scenario = CreateScenario(
            workspace,
            TransactionPhase.HealthyConfirmed,
            workspace.Now.AddSeconds(-31),
            12345,
            workspace.Now.AddMinutes(-1),
            generation: 12);
        workspace.WriteJournal(scenario.Journal);

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("TransactionInconsistent", result.ErrorCode);
    }

    [Fact]
    public async Task Recovery_FailsClosedOnInvalidPreviousPointer()
    {
        using var workspace = new TestWorkspace();
        ValidatedSlot activeSlot = workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 10, workspace.OldOperationId,
            healthConfirmed: true);
        var pointer = workspace.Pointer(activeSlot);
        workspace.WritePointer(workspace.Options().ActivePointerPath, pointer);
        TestWorkspace.AssertWrite(AtomicFileWriter.WriteJson(
            workspace.Options().PreviousPointerPath,
            pointer with { PreviousSlotId = workspace.NewSlotId }));

        OperationResult<RecoveryOutcome> result =
            await new JournalRecovery(workspace.Options()).RecoverAsync();

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void EvaluateTrial_Uses120SecondTimeoutAnd30SecondHealthWindow()
    {
        DateTimeOffset started = DateTimeOffset.UtcNow;

        Assert.Equal(
            TrialDecision.RollBack,
            JournalRecovery.EvaluateTrial(
                false,
                started + HealthPolicy.TrialTimeout + TimeSpan.FromSeconds(1),
                started,
                null));
        Assert.Equal(
            TrialDecision.Pending,
            JournalRecovery.EvaluateTrial(
                true,
                started + TimeSpan.FromSeconds(29),
                started,
                started));
        Assert.Equal(
            TrialDecision.Commit,
            JournalRecovery.EvaluateTrial(
                true,
                started + HealthPolicy.HealthyConfirmationWindow,
                started,
                started));
        Assert.Equal(
            TrialDecision.RollBack,
            JournalRecovery.EvaluateTrial(
                true,
                started + HealthPolicy.TrialTimeout,
                started,
                started + HealthPolicy.TrialTimeout -
                    HealthPolicy.HealthyConfirmationWindow +
                    TimeSpan.FromSeconds(1)));
    }

    private static void SetupTrialPointers(
        TestWorkspace workspace,
        Scenario scenario)
    {
        workspace.WritePointer(
            Path.Combine(workspace.StateRoot, "active.json"),
            workspace.Pointer(scenario.Journal.Phase ==
                    TransactionPhase.Committed
                ? scenario.NewSlot
                : scenario.OldSlot,
                scenario.Journal.Phase == TransactionPhase.Committed
                    ? scenario.OldSlot.Metadata.SlotId
                    : null,
                scenario.Journal.Phase == TransactionPhase.Committed
                    ? 2
                    : 1));
        if (scenario.Journal.Phase == TransactionPhase.Committed)
        {
            workspace.WritePointer(
                Path.Combine(workspace.StateRoot, "active.previous.json"),
                workspace.Pointer(scenario.OldSlot, sequence: 1));
        }
    }

    private static HealthReceipt CreateReceipt(
        TestWorkspace workspace,
        Scenario scenario)
    {
        DateTimeOffset trialStarted =
            scenario.Journal.TrialStartedAtUtc ??
            workspace.Now.AddSeconds(-31);
        return new HealthReceipt(
            HealthReceipt.CurrentSchemaVersion,
            scenario.Journal.OperationId,
            scenario.NewSlot.Metadata.SlotId,
            scenario.NewSlot.Metadata.Version,
            scenario.Journal.TrialProcessPid,
            scenario.Journal.TrialProcessStartTimeUtc!.Value,
            scenario.NewSlot.ExecutablePath,
            trialStarted.AddSeconds(1));
    }

    private static void CopyTrialFixture(string slotRoot)
    {
        string fixtureRoot = Path.Combine(
            AppContext.BaseDirectory,
            "TrialFixture");
        foreach (string source in Directory.EnumerateFiles(fixtureRoot))
        {
            string target = Path.Combine(
                slotRoot,
                Path.GetFileName(source));
            File.Copy(source, target, overwrite: true);
        }
    }

    private static Scenario CreateScenario(
        TestWorkspace workspace,
        TransactionPhase phase,
        DateTimeOffset? trialStart = null,
        int trialPid = 0,
        DateTimeOffset? processStart = null,
        string? operationId = null,
        string? installId = null,
        long? generation = null)
    {
        ValidatedSlot oldSlot = workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 10, workspace.OldOperationId,
            healthConfirmed: true);
        ValidatedSlot newSlot = workspace.CreateSlot(
            workspace.NewSlotId, "1.1.0", 11,
            workspace.OperationId,
            healthConfirmed: phase is TransactionPhase.Committed);
        TransactionJournal journal = workspace.Journal(
            phase,
            oldSlot,
            newSlot,
            trialStart,
            trialPid,
            processStart,
            operationId,
            installId,
            generation);
        return new Scenario(oldSlot, newSlot, journal);
    }

    private sealed record Scenario(
        ValidatedSlot OldSlot,
        ValidatedSlot NewSlot,
        TransactionJournal Journal);
}
