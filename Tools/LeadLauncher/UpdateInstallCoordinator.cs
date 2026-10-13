using System.Diagnostics;
using LEADKhongPhanh.Update;

namespace LeadLauncher;

public sealed class UpdateInstallCoordinator
{
    private readonly LauncherOptions options;
    private readonly LauncherLogger? logger;
    private readonly Func<ProcessIdentity> launcherIdentity;

    public UpdateInstallCoordinator(
        LauncherOptions options,
        LauncherLogger? logger = null,
        Func<ProcessIdentity>? launcherIdentity = null)
    {
        this.options = options;
        this.logger = logger;
        this.launcherIdentity = launcherIdentity ?? GetCurrentLauncherIdentity;
    }

    public async Task<OperationResult<RecoveryOutcome>> RunAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            string requestPath = options.InstallRequestPath;
            OperationResult<bool> requestPathCheck =
                PathSafety.EnsureNoReparsePoints(
                    options.InstallationRoot,
                    requestPath);
            if (!requestPathCheck.IsSuccess)
                return Failure(
                    "InvalidInstallRequest",
                    requestPathCheck.Details);

            OperationResult<UpdateInstallRequest> read =
                AtomicFileWriter.ReadJson<UpdateInstallRequest>(requestPath);
            if (!read.IsSuccess || read.Value is null)
                return Failure(
                    "InvalidInstallRequest",
                    read.Details);
            UpdateInstallRequest request = read.Value;
            OperationResult<DateTimeOffset> requestValidation =
                request.Validate();
            if (!requestValidation.IsSuccess)
                return Failure(
                    requestValidation.ErrorCode,
                    requestValidation.Details);

            OperationResult<ActivePointer> pointerRead =
                AtomicFileWriter.ReadJson<ActivePointer>(
                    options.ActivePointerPath);
            if (!pointerRead.IsSuccess || pointerRead.Value is null)
                return Failure(
                    "ActivePointerInvalid",
                    pointerRead.Details);
            OperationResult<UpdateVersion> pointerValidation =
                pointerRead.Value.Validate(options.InstallId);
            if (!pointerValidation.IsSuccess)
                return Failure(
                    pointerValidation.ErrorCode,
                    pointerValidation.Details);

            var slots = new SlotValidator(
                options.InstallationRoot,
                options.InstallId);
            OperationResult<ValidatedSlot> active =
                slots.Validate(pointerRead.Value.ActiveSlotId);
            if (!active.IsSuccess || active.Value is null ||
                !pointerRead.Value.Matches(active.Value))
            {
                return Failure(
                    "ActiveSlotInvalid",
                    active.IsSuccess
                        ? "Active pointer does not match its slot."
                        : active.Details);
            }

            OperationResult<string> packagePath =
                ResolvePackagePath(request.PackageRelativePath);
            if (!packagePath.IsSuccess || packagePath.Value is null)
                return Failure(
                    packagePath.ErrorCode,
                    packagePath.Details);

            OperationResult<ProcessIdentity> game =
                ProcessIdentity.TryGet(request.GamePid);
            var expectedGame = new ProcessIdentity(
                request.GamePid,
                requestValidation.Value,
                active.Value.ExecutablePath);
            if (!game.IsSuccess || game.Value is null ||
                !expectedGame.Matches(game.Value))
            {
                return Failure(
                    "ProcessIdentityMismatch",
                    "Install request does not identify the running active game.");
            }

            ProcessIdentity launcher = launcherIdentity();
            if (!launcher.ExecutablePath.Equals(
                    Path.GetFullPath(options.StableLauncherPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                return Failure(
                    "ProcessIdentityMismatch",
                    "Updater handoff must run from the stable launcher.");
            }

            string updaterPath = Path.Combine(
                options.InstallationRoot,
                "Updater",
                "LeadUpdater.exe");
            OperationResult<bool> updaterPathCheck =
                PathSafety.EnsureNoReparsePoints(
                    options.InstallationRoot,
                    updaterPath);
            if (!updaterPathCheck.IsSuccess ||
                !File.Exists(updaterPath) ||
                (File.GetAttributes(updaterPath) &
                 (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            {
                return Failure(
                    "UpdaterMissing",
                    updaterPathCheck.IsSuccess
                        ? "Trusted LeadUpdater.exe is missing or not a regular file."
                        : updaterPathCheck.Details);
            }

            OperationResult<bool> removeRequest =
                AtomicFileWriter.DeleteFile(requestPath);
            if (!removeRequest.IsSuccess)
                return Failure(
                    "InstallRequestDeleteFailed",
                    removeRequest.Details);

            using Process? updater = StartUpdater(
                updaterPath,
                packagePath.Value,
                request,
                launcher,
                game.Value);
            if (updater is null)
                return Failure(
                    "UpdaterLaunchFailed",
                    "Windows did not create LeadUpdater.exe.");

            logger?.Info(
                $"Updater started. operationId={request.OperationId}; " +
                $"package={packagePath.Value}; version={request.ExpectedVersion}; " +
                $"launcherPid={launcher.Pid}; gamePid={game.Value.Pid}.");

            await updater.WaitForExitAsync(cancellationToken)
                .ConfigureAwait(false);
            if (updater.ExitCode != 0)
            {
                logger?.Error(
                    $"Updater failed. operationId={request.OperationId}; " +
                    $"exitCode={updater.ExitCode}.");
                return Failure(
                    "UpdaterFailed",
                    $"LeadUpdater.exe returned exit code {updater.ExitCode}.");
            }

            logger?.Info(
                $"Updater staged package. operationId={request.OperationId}; " +
                "phase=PREPARED.");
            return await new JournalRecovery(
                    options,
                    cancellationToken: cancellationToken,
                    logger: logger)
                .RecoverAsync()
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Failure(
                "InstallCancelled",
                "Update coordination was cancelled.");
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            InvalidOperationException or
            ArgumentException or
            System.ComponentModel.Win32Exception)
        {
            return Failure(
                "InstallCoordinationFailed",
                exception.Message);
        }
    }

    private OperationResult<string> ResolvePackagePath(string relativePath)
    {
        OperationResult<string> resolved =
            PathSafety.ResolveRelativePath(
                options.InstallationRoot,
                relativePath,
                requireExisting: true);
        if (!resolved.IsSuccess || resolved.Value is null)
            return resolved;

        string canonical = resolved.Value;
        bool underTemp = IsWithin(
            Path.Combine(options.InstallationRoot, "Update", "Temp"),
            canonical);
        bool underCache = IsWithin(
            Path.Combine(options.InstallationRoot, "Update", "Cache"),
            canonical);
        if (!underTemp && !underCache)
            return OperationResult<string>.Failure(
                "PackageNotRegularFile",
                "Package must be inside the trusted Update/Temp or Update/Cache directory.");

        OperationResult<bool> links =
            PathSafety.EnsureNoReparsePoints(
                options.InstallationRoot,
                canonical);
        if (!links.IsSuccess)
            return OperationResult<string>.Failure(
                links.ErrorCode,
                links.Details);

        FileAttributes attributes = File.GetAttributes(canonical);
        return (attributes &
                (FileAttributes.Directory | FileAttributes.ReparsePoint)) == 0
            ? OperationResult<string>.Success(canonical)
            : OperationResult<string>.Failure(
                "PackageNotRegularFile",
                "Update package must be a regular file.");
    }

    private Process? StartUpdater(
        string updaterPath,
        string packagePath,
        UpdateInstallRequest request,
        ProcessIdentity launcher,
        ProcessIdentity game)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = updaterPath,
            WorkingDirectory = Path.GetDirectoryName(updaterPath)!,
            UseShellExecute = false,
            CreateNoWindow = false
        };
        AddArgument(startInfo, "--install-root", options.InstallationRoot);
        AddArgument(startInfo, "--package", packagePath);
        AddArgument(startInfo, "--operation-id", request.OperationId);
        AddArgument(startInfo, "--expected-version", request.ExpectedVersion);
        AddArgument(startInfo, "--expected-sha256", request.ExpectedSha256);
        AddArgument(
            startInfo,
            "--expected-file-size",
            request.ExpectedFileSize.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        AddArgument(
            startInfo,
            "--launcher-pid",
            launcher.Pid.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        AddArgument(
            startInfo,
            "--launcher-start-time-utc",
            launcher.ProcessStartTimeUtc.ToUniversalTime().ToString("O"));
        AddArgument(
            startInfo,
            "--game-pid",
            game.Pid.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        AddArgument(
            startInfo,
            "--game-start-time-utc",
            game.ProcessStartTimeUtc.ToUniversalTime().ToString("O"));
        return Process.Start(startInfo);
    }

    private static void AddArgument(
        ProcessStartInfo startInfo,
        string name,
        string value)
    {
        startInfo.ArgumentList.Add(name);
        startInfo.ArgumentList.Add(value);
    }

    private ProcessIdentity GetCurrentLauncherIdentity()
    {
        OperationResult<ProcessIdentity> identity =
            ProcessIdentity.TryGet(Environment.ProcessId);
        if (!identity.IsSuccess || identity.Value is null)
            throw new InvalidOperationException(
                "Current stable launcher identity could not be verified: " +
                identity.Details);
        return identity.Value;
    }

    private static bool IsWithin(string rootPath, string candidatePath)
    {
        string root = Path.GetFullPath(rootPath);
        string candidate = Path.GetFullPath(candidatePath);
        string prefix = Path.EndsInDirectorySeparator(root)
            ? root
            : root + Path.DirectorySeparatorChar;
        return candidate.StartsWith(
            prefix,
            StringComparison.OrdinalIgnoreCase);
    }

    private static OperationResult<RecoveryOutcome> Failure(
        string code,
        string details) =>
        OperationResult<RecoveryOutcome>.Failure(code, details);
}
