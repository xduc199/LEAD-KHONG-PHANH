using System.Globalization;
using LeadLauncher;
using LeadUpdater;
using Xunit;

namespace LeadUpdater.Tests;

public sealed class UpdaterStateTests
{
    [Fact]
    public void State_manager_loads_active_pointer_and_slot()
    {
        using var workspace = new TestWorkspace();

        var result = new UpdateStateManager(
            workspace.InstallationRoot,
            workspace.StateRoot).LoadActiveSlot();

        Assert.True(result.IsSuccess, result.Details);
        Assert.Equal(workspace.ActiveSlotId, result.Value!.Pointer.ActiveSlotId);
        Assert.Equal(workspace.ActiveSlotId, result.Value.ActiveSlot.Metadata.SlotId);
    }

    [Fact]
    public void Existing_transaction_requires_launcher_recovery()
    {
        using var workspace = new TestWorkspace();
        File.WriteAllText(workspace.TransactionPath, "{");

        var result = new UpdateStateManager(
            workspace.InstallationRoot,
            workspace.StateRoot).LoadActiveSlot();

        Assert.Equal("RecoveryRequired", result.ErrorCode);
    }

    [Fact]
    public void Pointer_install_identity_mismatch_fails_closed()
    {
        using var workspace = new TestWorkspace();
        var pointer = workspace.ReadPointer() with
        {
            InstallId = Guid.NewGuid().ToString("N")
        };
        var write = AtomicFileWriter.WriteJson(
            workspace.ActivePointerPath,
            pointer);
        Assert.True(write.IsSuccess, write.Details);

        var result = new UpdateStateManager(
            workspace.InstallationRoot,
            workspace.StateRoot).LoadActiveSlot();

        Assert.Equal("RecoveryRequired", result.ErrorCode);
    }

    [Fact]
    public void Pointer_operation_mismatch_fails_closed()
    {
        using var workspace = new TestWorkspace();
        var pointer = workspace.ReadPointer() with
        {
            OperationId = Guid.NewGuid().ToString("N")
        };
        var write = AtomicFileWriter.WriteJson(
            workspace.ActivePointerPath,
            pointer);
        Assert.True(write.IsSuccess, write.Details);

        var result = new UpdateStateManager(
            workspace.InstallationRoot,
            workspace.StateRoot).LoadActiveSlot();

        Assert.Equal("RecoveryRequired", result.ErrorCode);
    }

    [Fact]
    public void Argument_parser_requires_trusted_process_identity()
    {
        var result = UpdaterOptions.ParseArguments(
            ["--install-root", "C:\\game"]);

        Assert.Equal("InvalidArguments", result.ErrorCode);
    }

    [Fact]
    public void Argument_parser_rejects_duplicate_or_unknown_switches()
    {
        string[] args = ValidArguments();
        string[] duplicate = args.Concat(
            ["--package", "C:\\game\\Update\\Temp\\other.zip"]).ToArray();
        Assert.Equal(
            "InvalidArguments",
            UpdaterOptions.ParseArguments(duplicate).ErrorCode);

        string[] unknown = args.Concat(["--command", "anything"]).ToArray();
        Assert.Equal(
            "InvalidArguments",
            UpdaterOptions.ParseArguments(unknown).ErrorCode);
    }

    [Fact]
    public void Argument_parser_rejects_incomplete_game_identity()
    {
        string[] args = ValidArguments().Concat(["--game-pid", "123"]).ToArray();

        var result = UpdaterOptions.ParseArguments(args);

        Assert.Equal("InvalidProcessIdentity", result.ErrorCode);
    }

    [Fact]
    public void Argument_parser_accepts_valid_trusted_contract()
    {
        var result = UpdaterOptions.ParseArguments(ValidArguments());

        Assert.True(result.IsSuccess, result.Details);
        Assert.Equal("1.1.0", result.Value!.ExpectedVersion);
    }

    [Fact]
    public void Argument_parser_rejects_relative_paths()
    {
        string[] args = ValidArguments();
        args[1] = "relative-install";

        var result = UpdaterOptions.ParseArguments(args);

        Assert.Equal("InvalidArguments", result.ErrorCode);
    }

    [Fact]
    public async Task Process_wait_accepts_exited_process()
    {
        var expected = Identity(42);
        var waiter = new ProcessWaiter(_ => ProcessProbe.Exited());

        var result = await waiter.WaitForExitAsync(
            expected,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(1));

        Assert.True(result.IsSuccess, result.Details);
    }

    [Fact]
    public async Task Process_wait_fails_when_identity_pid_differs()
    {
        var expected = Identity(42);
        var waiter = new ProcessWaiter(_ =>
            ProcessProbe.Running(Identity(43)));

        var result = await waiter.WaitForExitAsync(
            expected,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(1));

        Assert.Equal("ProcessIdentityMismatch", result.ErrorCode);
    }

    [Fact]
    public async Task Process_wait_fails_when_start_time_differs()
    {
        var expected = Identity(42);
        var actual = expected with
        {
            ProcessStartTimeUtc = expected.ProcessStartTimeUtc.AddSeconds(1)
        };
        var waiter = new ProcessWaiter(_ => ProcessProbe.Running(actual));

        var result = await waiter.WaitForExitAsync(
            expected,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(1));

        Assert.Equal("ProcessIdentityMismatch", result.ErrorCode);
    }

    [Fact]
    public async Task Process_wait_fails_when_executable_path_differs()
    {
        var expected = Identity(42);
        var actual = expected with { ExecutablePath = "C:\\other\\game.exe" };
        var waiter = new ProcessWaiter(_ => ProcessProbe.Running(actual));

        var result = await waiter.WaitForExitAsync(
            expected,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(1));

        Assert.Equal("ProcessIdentityMismatch", result.ErrorCode);
    }

    [Fact]
    public async Task Process_wait_times_out_without_terminating_process()
    {
        var expected = Identity(42);
        int probes = 0;
        var waiter = new ProcessWaiter(_ =>
        {
            probes++;
            return ProcessProbe.Running(expected);
        });

        var result = await waiter.WaitForExitAsync(
            expected,
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(1));

        Assert.Equal("ProcessStillRunning", result.ErrorCode);
        Assert.True(probes > 1);
    }

    [Fact]
    public async Task Process_wait_fails_closed_when_process_cannot_be_inspected()
    {
        var waiter = new ProcessWaiter(_ => ProcessProbe.Failed("denied"));

        var result = await waiter.WaitForExitAsync(
            Identity(42),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(1));

        Assert.Equal("ProcessIdentityUnavailable", result.ErrorCode);
    }

    [Fact]
    public void Limits_reject_invalid_entry_and_archive_relationship()
    {
        Assert.False(new UpdaterLimits(
            MaxExpandedBytes: 10,
            MaxExpandedEntryBytes: 11).Validate().IsSuccess);
        Assert.False(new UpdaterLimits(
            MaxCompressionRatio: double.PositiveInfinity).Validate().IsSuccess);
    }

    [Fact]
    public async Task Concurrent_staging_lock_fails_closed()
    {
        using var workspace = new TestWorkspace();
        string lockPath = Path.Combine(workspace.StateRoot, "staging.lock");
        using var heldLock = new FileStream(
            lockPath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None);
        byte[] package = workspace.CreateValidPackage();
        UpdaterOptions options = workspace.Options(package);
        byte[] pointerBefore = File.ReadAllBytes(workspace.ActivePointerPath);

        var result = await new UpdateInstaller(
                stateRootOverride: workspace.StateRoot)
            .RunAsync(options);

        Assert.Equal("RecoveryRequired", result.ErrorCode);
        Assert.False(File.Exists(workspace.TransactionPath));
        Assert.Equal(pointerBefore, File.ReadAllBytes(workspace.ActivePointerPath));
    }

    private static string[] ValidArguments() =>
    [
        "--install-root", "C:\\game",
        "--package", "C:\\game\\Update\\Temp\\update.zip",
        "--operation-id", Guid.NewGuid().ToString("N"),
        "--expected-version", "1.1.0",
        "--expected-sha256", new string('a', 64),
        "--expected-file-size", "123",
        "--launcher-pid", "1234",
        "--launcher-start-time-utc",
        DateTimeOffset.UtcNow.AddMinutes(-1).ToString(
            "O",
            CultureInfo.InvariantCulture)
    ];

    private static ProcessIdentity Identity(int pid) =>
        new(
            pid,
            new DateTimeOffset(2026, 10, 6, 0, 0, 0, TimeSpan.Zero),
            "C:\\game\\LEADKhongPhanh.exe");
}
