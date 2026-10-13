using LeadLauncher;
using LeadUpdater;
using Xunit;

namespace LeadUpdater.Tests;

public sealed class SlotStagingTests
{
    [Fact]
    public async Task Complete_staging_writes_prepared_only_after_slot_validation()
    {
        using var workspace = new TestWorkspace();
        byte[] package = workspace.CreateValidPackage();
        UpdaterOptions options = workspace.Options(package);
        byte[] pointerBefore = File.ReadAllBytes(workspace.ActivePointerPath);
        Dictionary<string, string> activeBefore =
            workspace.SnapshotActiveSlot();

        var installer = new UpdateInstaller(
            stateRootOverride: workspace.StateRoot,
            slotIdFactory: () => workspace.NewSlotId);
        OperationResult<StagingResult> result =
            await installer.RunAsync(options);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.Details}");
        Assert.Equal(TransactionPhase.Prepared, result.Value!.Phase);
        Assert.NotEqual(workspace.ActiveSlotId, result.Value.SlotId);
        Assert.True(File.Exists(Path.Combine(result.Value.SlotPath, "slot.json")));
        Assert.Equal(pointerBefore, File.ReadAllBytes(workspace.ActivePointerPath));
        Assert.Equal(activeBefore, workspace.SnapshotActiveSlot());
        var validated = new SlotValidator(
            workspace.InstallationRoot,
            workspace.InstallId).Validate(
            result.Value.SlotId,
            options.OperationId,
            options.ExpectedVersion,
            options.ExpectedSha256,
            result.Value.Generation);
        Assert.True(validated.IsSuccess, validated.Details);

        TransactionJournal journal = workspace.ReadJournal();
        Assert.Equal(TransactionPhase.Prepared, journal.Phase);
        Assert.Equal(workspace.ActiveSlotId, journal.OldSlotId);
        Assert.Equal(result.Value.SlotId, journal.NewSlotId);
        Assert.Equal(options.OperationId, journal.OperationId);
        AssertStagingLockReleased(workspace.StateRoot);
    }

    [Fact]
    public async Task Active_slot_collision_is_rejected_without_mutation()
    {
        using var workspace = new TestWorkspace();
        byte[] package = workspace.CreateValidPackage();
        UpdaterOptions options = workspace.Options(package);
        byte[] pointerBefore = File.ReadAllBytes(workspace.ActivePointerPath);
        Dictionary<string, string> activeBefore =
            workspace.SnapshotActiveSlot();
        var installer = new UpdateInstaller(
            stateRootOverride: workspace.StateRoot,
            slotIdFactory: () => workspace.ActiveSlotId);

        var result = await installer.RunAsync(options);

        Assert.Equal("ActiveSlotConflict", result.ErrorCode);
        Assert.Equal(pointerBefore, File.ReadAllBytes(workspace.ActivePointerPath));
        Assert.Equal(activeBefore, workspace.SnapshotActiveSlot());
        Assert.False(File.Exists(workspace.TransactionPath));
    }

    [Fact]
    public async Task Previous_slot_identity_is_not_reused()
    {
        using var workspace = new TestWorkspace();
        string previousSlotId = TestWorkspace.CreateSlotId();
        var pointer = workspace.ReadPointer() with
        {
            PreviousSlotId = previousSlotId
        };
        var saved = AtomicFileWriter.WriteJson(
            workspace.ActivePointerPath,
            pointer);
        Assert.True(saved.IsSuccess, saved.Details);
        byte[] package = workspace.CreateValidPackage();
        UpdaterOptions options = workspace.Options(package);
        var installer = new UpdateInstaller(
            stateRootOverride: workspace.StateRoot,
            slotIdFactory: () => previousSlotId);

        var result = await installer.RunAsync(options);

        Assert.Equal("ActiveSlotConflict", result.ErrorCode);
        Assert.False(Directory.Exists(Path.Combine(
            workspace.SlotsRoot,
            previousSlotId)));
        Assert.False(File.Exists(workspace.TransactionPath));
    }

    [Fact]
    public async Task Existing_destination_is_not_overwritten_or_deleted()
    {
        using var workspace = new TestWorkspace();
        byte[] package = workspace.CreateValidPackage();
        UpdaterOptions options = workspace.Options(package);
        string collision = Path.Combine(workspace.SlotsRoot, workspace.NewSlotId);
        Directory.CreateDirectory(collision);
        File.WriteAllText(Path.Combine(collision, "sentinel.txt"), "keep");
        var installer = new UpdateInstaller(
            stateRootOverride: workspace.StateRoot,
            slotIdFactory: () => workspace.NewSlotId);

        var result = await installer.RunAsync(options);

        Assert.Equal("ActiveSlotConflict", result.ErrorCode);
        Assert.Equal("keep", File.ReadAllText(
            Path.Combine(collision, "sentinel.txt")));
        Assert.False(File.Exists(workspace.TransactionPath));
    }

    [Fact]
    public async Task Missing_executable_never_creates_prepared_transaction()
    {
        using var workspace = new TestWorkspace();
        byte[] package = workspace.CreateValidPackage(
            executablePath: "SomeOtherGame.exe");
        UpdaterOptions options = workspace.Options(package);
        byte[] pointerBefore = File.ReadAllBytes(workspace.ActivePointerPath);

        var result = await new UpdateInstaller(
                stateRootOverride: workspace.StateRoot)
            .RunAsync(options);

        Assert.Equal("SlotValidationFailed", result.ErrorCode);
        Assert.False(File.Exists(workspace.TransactionPath));
        Assert.Equal(pointerBefore, File.ReadAllBytes(workspace.ActivePointerPath));
    }

    [Fact]
    public async Task Missing_data_directory_never_creates_prepared_transaction()
    {
        using var workspace = new TestWorkspace();
        byte[] package = workspace.CreateValidPackage(includeData: false);
        UpdaterOptions options = workspace.Options(package);

        var result = await new UpdateInstaller(
                stateRootOverride: workspace.StateRoot)
            .RunAsync(options);

        Assert.Equal("SlotValidationFailed", result.ErrorCode);
        Assert.False(File.Exists(workspace.TransactionPath));
    }

    [Fact]
    public async Task Extraction_failure_cleans_partial_slot_and_transaction()
    {
        using var workspace = new TestWorkspace();
        byte[] package = workspace.CreateValidPackage();
        UpdaterOptions options = workspace.Options(package);
        using var cancellation = new CancellationTokenSource();
        byte[] pointerBefore = File.ReadAllBytes(workspace.ActivePointerPath);
        var logger = new RecordingLogger();

        var result = await new UpdateInstaller(
                logger: logger,
                stateRootOverride: workspace.StateRoot,
                slotIdFactory: () => workspace.NewSlotId,
                extractionProgress: (_, _) => cancellation.Cancel())
            .RunAsync(options, cancellation.Token);

        Assert.Equal("ExtractionFailed", result.ErrorCode);
        Assert.False(Directory.Exists(Path.Combine(
            workspace.SlotsRoot,
            workspace.NewSlotId)));
        Assert.False(File.Exists(workspace.TransactionPath));
        Assert.Equal(pointerBefore, File.ReadAllBytes(workspace.ActivePointerPath));
        Assert.Contains(
            logger.Messages,
            message => message.Contains(
                "Transaction PREPARING",
                StringComparison.Ordinal));
        Assert.DoesNotContain(
            logger.Messages,
            message => message.Contains(
                "Transaction PREPARED",
                StringComparison.Ordinal));
        AssertStagingLockReleased(workspace.StateRoot);
    }

    [Fact]
    public async Task Process_wait_timeout_releases_staging_lock()
    {
        using var workspace = new TestWorkspace();
        byte[] package = workspace.CreateValidPackage();
        var gameIdentity = new ProcessIdentity(
            42,
            workspace.NowUtc.AddMinutes(-2),
            workspace.ActiveExecutable);
        UpdaterOptions options = workspace.Options(
            package,
            gamePid: gameIdentity.Pid,
            gameStart: gameIdentity.ProcessStartTimeUtc);
        var waiter = new ProcessWaiter(_ =>
            ProcessProbe.Running(gameIdentity));

        OperationResult<StagingResult> result =
            await new UpdateInstaller(
                    stateRootOverride: workspace.StateRoot,
                    processWaitTimeout: TimeSpan.FromMilliseconds(10),
                    processWaiter: waiter)
                .RunAsync(options);

        Assert.Equal("ProcessStillRunning", result.ErrorCode);
        Assert.False(File.Exists(workspace.TransactionPath));
        AssertStagingLockReleased(workspace.StateRoot);
    }

    [Fact]
    public async Task Exception_after_lock_acquisition_releases_staging_lock()
    {
        using var workspace = new TestWorkspace();
        byte[] package = workspace.CreateValidPackage();
        UpdaterOptions options = workspace.Options(package);
        var installer = new UpdateInstaller(
            logger: new ThrowingInfoLogger(),
            stateRootOverride: workspace.StateRoot);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => installer.RunAsync(options));

        AssertStagingLockReleased(workspace.StateRoot);
    }

    private static void AssertStagingLockReleased(string stateRoot)
    {
        string lockPath = Path.Combine(stateRoot, "staging.lock");
        using var acquired = new FileStream(
            lockPath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None);
    }

    private sealed class ThrowingInfoLogger : IUpdaterLogger
    {
        public void Info(string message) =>
            throw new InvalidOperationException("Injected logger failure.");

        public void Error(string message)
        {
        }
    }

    [Fact]
    public async Task Invalid_operation_id_is_rejected_before_state_mutation()
    {
        using var workspace = new TestWorkspace();
        byte[] package = workspace.CreateValidPackage();
        UpdaterOptions options = workspace.Options(package, operationId: "bad");

        var result = await new UpdateInstaller(
                stateRootOverride: workspace.StateRoot)
            .RunAsync(options);

        Assert.Equal("InvalidOperationId", result.ErrorCode);
        Assert.False(File.Exists(workspace.TransactionPath));
    }

    [Fact]
    public async Task Package_outside_trusted_cache_is_rejected()
    {
        using var workspace = new TestWorkspace();
        byte[] package = workspace.CreateValidPackage();
        string outsidePath = Path.Combine(workspace.Root, "outside.zip");
        File.WriteAllBytes(outsidePath, package);
        UpdaterOptions options = workspace.Options(
            package,
            packagePath: outsidePath);

        var result = await new UpdateInstaller(
                stateRootOverride: workspace.StateRoot)
            .RunAsync(options);

        Assert.Equal("PackageNotRegularFile", result.ErrorCode);
        Assert.False(File.Exists(workspace.TransactionPath));
    }

    [Fact]
    public async Task Equal_or_downgrade_version_is_rejected()
    {
        using var workspace = new TestWorkspace();
        byte[] package = workspace.CreateValidPackage();

        foreach (string version in new[] { "1.0.0", "0.9.9" })
        {
            var result = await new UpdateInstaller(
                    stateRootOverride: workspace.StateRoot)
                .RunAsync(workspace.Options(package, version: version));

            Assert.Equal("InvalidVersion", result.ErrorCode);
            Assert.False(File.Exists(workspace.TransactionPath));
        }
    }

    [Fact]
    public async Task Slot_validator_rejects_metadata_operation_mismatch()
    {
        using var workspace = new TestWorkspace();
        string slotId = workspace.NewSlotId;
        string slotRoot = Path.Combine(workspace.SlotsRoot, slotId);
        Directory.CreateDirectory(slotRoot);
        File.WriteAllText(
            Path.Combine(slotRoot, UpdaterOptions.ExpectedExecutableRelativePath),
            "exe");
        Directory.CreateDirectory(Path.Combine(
            slotRoot,
            UpdaterOptions.ExpectedDataDirectoryName));
        var metadata = new SlotInfo(
            SlotInfo.CurrentSchemaVersion,
            slotId,
            workspace.InstallId,
            "1.1.0",
            UpdaterOptions.ExpectedExecutableRelativePath,
            4,
            Guid.NewGuid().ToString("N"),
            workspace.NowUtc,
            false,
            new string('b', 64));
        var write = AtomicFileWriter.WriteJson(
            Path.Combine(slotRoot, "slot.json"),
            metadata);
        Assert.True(write.IsSuccess, write.Details);

        var result = new SlotValidator(
            workspace.InstallationRoot,
            workspace.InstallId).Validate(
            slotId,
            workspace.OperationId,
            "1.1.0",
            new string('b', 64),
            4);

        Assert.Equal("SlotValidationFailed", result.ErrorCode);
    }

    private sealed class RecordingLogger : IUpdaterLogger
    {
        public List<string> Messages { get; } = [];

        public void Info(string message) => Messages.Add(message);

        public void Error(string message) => Messages.Add(message);
    }
}
