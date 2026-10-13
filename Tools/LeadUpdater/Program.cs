namespace LeadUpdater;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        UpdaterLogger? logger = null;
        try
        {
            OperationResult<UpdaterOptions> parsed =
                UpdaterOptions.ParseArguments(args);
            if (!parsed.IsSuccess || parsed.Value is null)
            {
                Console.Error.WriteLine(
                    $"{parsed.ErrorCode}: {parsed.Details}");
                return 2;
            }

            if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
            {
                Console.Error.WriteLine(
                    "LeadUpdater requires Windows x64.");
                return 3;
            }

            string root = Path.GetFullPath(parsed.Value.InstallationRoot);
            OperationResult<(string Root, string InstallId)> identity =
                ResolveIdentity(root);
            if (!identity.IsSuccess)
            {
                Console.Error.WriteLine(
                    $"{identity.ErrorCode}: {identity.Details}");
                return 4;
            }

            string localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            LeadLauncher.OperationResult<string> statePath =
                LeadLauncher.InstallIdentity.ResolveStateRoot(
                    localAppData,
                    LeadLauncher.InstallIdentity.StablePublisher,
                    identity.Value.InstallId);
            if (!statePath.IsSuccess || statePath.Value is null)
            {
                Console.Error.WriteLine(
                    $"RecoveryRequired: {statePath.Details}");
                return 4;
            }

            string logsPath = Path.Combine(statePath.Value, "Logs");
            OperationResult<bool> statePathCheck =
                LeadLauncher.PathSafety.EnsureNoReparsePoints(
                    localAppData,
                    statePath.Value);
            if (!statePathCheck.IsSuccess)
            {
                Console.Error.WriteLine(
                    $"RecoveryRequired: {statePathCheck.Details}");
                return 4;
            }
            Directory.CreateDirectory(statePath.Value);
            statePathCheck = LeadLauncher.PathSafety.EnsureNoReparsePoints(
                localAppData,
                statePath.Value);
            if (!statePathCheck.IsSuccess)
            {
                Console.Error.WriteLine(
                    $"RecoveryRequired: {statePathCheck.Details}");
                return 4;
            }
            logger = new UpdaterLogger(logsPath);
            logger.Info(
                $"Startup. operationId={parsed.Value.OperationId}; " +
                $"version={parsed.Value.ExpectedVersion}; " +
                $"expectedSize={parsed.Value.ExpectedFileSize}.");

            var installer = new UpdateInstaller(logger: logger);
            OperationResult<StagingResult> result =
                await installer.RunAsync(parsed.Value).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                logger.Error(
                    $"Update staging failed: {result.ErrorCode}: {result.Details}");
                Console.Error.WriteLine(
                    $"{result.ErrorCode}: {result.Details}");
                return 5;
            }

            logger.Info(
                $"Staging complete. slotId={result.Value.SlotId}; " +
                $"generation={result.Value.Generation}; phase=PREPARED.");
            return 0;
        }
        catch (Exception exception)
        {
            string details =
                $"Fatal updater boundary failure: {exception.Message}";
            if (logger is null)
                Console.Error.WriteLine(details);
            else
                logger.Error(details);
            return 10;
        }
        finally
        {
            logger?.Dispose();
        }
    }

    private static OperationResult<(string Root, string InstallId)>
        ResolveIdentity(string root)
    {
        try
        {
            if (!Directory.Exists(root))
                return OperationResult<(string, string)>.Failure(
                    "RecoveryRequired", "Installation root does not exist.");
            OperationResult<bool> rootCheck =
                LeadLauncher.PathSafety.EnsureNoReparsePoints(
                    Path.GetPathRoot(root)!,
                    root);
            if (!rootCheck.IsSuccess)
                return OperationResult<(string, string)>.Failure(
                    "RecoveryRequired", rootCheck.Details);
            string idPath = Path.Combine(root, "install.id");
            if (!File.Exists(idPath))
                return OperationResult<(string, string)>.Failure(
                    "RecoveryRequired", "install.id does not exist.");
            if ((File.GetAttributes(idPath) &
                 FileAttributes.ReparsePoint) != 0)
            {
                return OperationResult<(string, string)>.Failure(
                    "RecoveryRequired", "install.id cannot be a reparse point.");
            }
            string installId = File.ReadAllText(idPath).Trim();
            return LeadLauncher.PathSafety.IsValidInstallId(installId)
                ? OperationResult<(string, string)>.Success(
                    (root, installId.ToLowerInvariant()))
                : OperationResult<(string, string)>.Failure(
                    "RecoveryRequired", "install.id is malformed.");
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            NotSupportedException)
        {
            return OperationResult<(string, string)>.Failure(
                "RecoveryRequired", exception.Message);
        }
    }
}
