namespace LeadLauncher;

internal static class Program
{
    private static int Main(string[] args)
    {
        LauncherLogger? logger = null;
        try
        {
            if (args.Length != 0)
            {
                Console.Error.WriteLine(
                    "LeadLauncher does not accept command-line arguments.");
                return 2;
            }

            string? executable = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executable))
            {
                Console.Error.WriteLine(
                    "Unable to resolve the stable launcher executable path.");
                return 2;
            }

            string launcherPath = Path.GetFullPath(executable);
            if (!Path.GetFileName(launcherPath).Equals(
                    "LEADKhongPhanh.exe",
                    StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine(
                    "LeadLauncher must be published as LEADKhongPhanh.exe.");
                return 2;
            }

            string installationRoot =
                Path.GetDirectoryName(launcherPath)!;
            OperationResult<string> identity =
                InstallIdentity.Resolve(installationRoot);
            if (!identity.IsSuccess || identity.Value is null)
            {
                Console.Error.WriteLine(
                    $"Install identity error: {identity.ErrorCode}: {identity.Details}");
                return 3;
            }

            OperationResult<string> stateRoot =
                InstallIdentity.ResolveStateRoot(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    InstallIdentity.StablePublisher,
                    identity.Value);
            if (!stateRoot.IsSuccess || stateRoot.Value is null)
            {
                Console.Error.WriteLine(
                    $"Updater state path error: {stateRoot.ErrorCode}: {stateRoot.Details}");
                return 3;
            }

            var options = new LauncherOptions(
                installationRoot,
                stateRoot.Value,
                identity.Value,
                launcherPath);
            Directory.CreateDirectory(options.StateRoot);
            OperationResult<bool> statePathCheck =
                PathSafety.EnsureNoReparsePoints(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    options.StateRoot);
            if (!statePathCheck.IsSuccess)
            {
                Console.Error.WriteLine(
                    $"Updater state path validation failed: {statePathCheck.Details}");
                return 3;
            }

            logger = new LauncherLogger(options.LogsRoot);
            logger.Info($"Startup. installId={options.InstallId}");

            if (!OperatingSystem.IsWindows())
            {
                logger.Error("LeadLauncher requires Windows.");
                return 4;
            }

            OperationResult<RecoveryOutcome> result;
            if (File.Exists(options.InstallRequestPath))
            {
                result = new UpdateInstallCoordinator(options, logger)
                    .RunAsync()
                    .GetAwaiter()
                    .GetResult();
                if (!result.IsSuccess || result.Value is null)
                {
                    logger.Error(
                        $"Update handoff failed: {result.ErrorCode}: " +
                        $"{result.Details}; recovering current installation.");
                    result = new JournalRecovery(options, logger: logger)
                        .RecoverAsync()
                        .GetAwaiter()
                        .GetResult();
                }
            }
            else
            {
                result = new JournalRecovery(options, logger: logger)
                    .RecoverAsync()
                    .GetAwaiter()
                    .GetResult();
            }
            if (!result.IsSuccess || result.Value is null)
            {
                logger.Error(
                    $"Recovery error: {result.ErrorCode}: {result.Details}");
                return 5;
            }

            logger.Info(
                $"Recovery decision: {result.Value.Decision}; " +
                $"activeSlot={result.Value.ActiveSlot.Metadata.SlotId}; " +
                "recovery complete.");

            if (result.Value.ExistingTrialProcess is not null)
            {
                logger.Info(
                    "Verified trial game process is already running; " +
                    "not starting a duplicate.");
                return 0;
            }

            OperationResult<ProcessIdentity> launch =
                new GameLauncher().Launch(result.Value.ActiveSlot);
            if (!launch.IsSuccess || launch.Value is null)
            {
                logger.Error(
                    $"Game launch failed: {launch.ErrorCode}: {launch.Details}");
                return 6;
            }

            logger.Info(
                $"Launched validated active slot " +
                $"{result.Value.ActiveSlot.Metadata.SlotId}; pid={launch.Value.Pid}.");
            return 0;
        }
        catch (Exception exception)
        {
            string message =
                $"Fatal launcher boundary failure: {exception.Message}";
            if (logger is null)
                Console.Error.WriteLine(message);
            else
                logger.Error(message);
            return 10;
        }
    }
}
