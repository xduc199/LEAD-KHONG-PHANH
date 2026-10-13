using System.IO.Compression;
using System.Security.Cryptography;
using LEADKhongPhanh.Update;
using LeadLauncher;

namespace LeadUpdater;

public sealed record StagingResult(
    string SlotId,
    long Generation,
    TransactionPhase Phase,
    string SlotPath);

public sealed class UpdateInstaller
{
    private readonly UpdaterLimits limits;
    private readonly IUpdaterLogger? logger;
    private readonly string? stateRootOverride;
    private readonly TimeSpan processWaitTimeout;
    private readonly Func<string> slotIdFactory;
    private readonly ProcessWaiter processWaiter;
    private readonly Action<int, long>? extractionProgress;

    public UpdateInstaller(
        UpdaterLimits? limits = null,
        IUpdaterLogger? logger = null,
        string? stateRootOverride = null,
        TimeSpan? processWaitTimeout = null,
        Func<string>? slotIdFactory = null,
        ProcessWaiter? processWaiter = null,
        Action<int, long>? extractionProgress = null)
    {
        this.limits = limits ?? new UpdaterLimits();
        this.logger = logger;
        this.stateRootOverride = stateRootOverride;
        this.processWaitTimeout =
            processWaitTimeout ?? ProcessWaiter.DefaultTimeout;
        this.slotIdFactory = slotIdFactory ?? NewSlotId;
        this.processWaiter = processWaiter ?? new ProcessWaiter();
        this.extractionProgress = extractionProgress;
    }

    public async Task<OperationResult<StagingResult>> RunAsync(
        UpdaterOptions options,
        CancellationToken cancellationToken = default)
    {
        OperationResult<bool> limitsValidation = limits.Validate();
        if (!limitsValidation.IsSuccess)
            return Failure<StagingResult>(
                limitsValidation.ErrorCode,
                limitsValidation.Details);

        if (!PathSafety.IsValidOperationId(options.OperationId))
            return Failure<StagingResult>(
                "InvalidOperationId", "Operation id is malformed.");
        if (!PathSafety.IsValidSha256(options.ExpectedSha256))
            return Failure<StagingResult>(
                "InvalidPackageHash", "Expected SHA-256 is malformed.");
        if (!UpdateVersion.TryParse(options.ExpectedVersion, out UpdateVersion newVersion))
            return Failure<StagingResult>(
                "InvalidVersion", "Expected version is malformed.");
        if (options.ExpectedFileSize <= 0 ||
            options.ExpectedFileSize > limits.MaxPackageBytes)
        {
            return Failure<StagingResult>(
                "ArchiveTooLarge", "Package size exceeds configured limits.");
        }
        if (options.LauncherPid <= 0 ||
            options.LauncherStartTimeUtc == default ||
            options.LauncherStartTimeUtc.Offset != TimeSpan.Zero)
        {
            return Failure<StagingResult>(
                "InvalidProcessIdentity",
                "Trusted launcher process identity is invalid.");
        }
        if (!Path.IsPathFullyQualified(options.InstallationRoot))
            return Failure<StagingResult>(
                "RecoveryRequired",
                "Installation root must be an absolute trusted path.");
        if (options.GamePid.HasValue != options.GameStartTimeUtc.HasValue ||
            options.GamePid is <= 0 ||
            (options.GameStartTimeUtc is DateTimeOffset suppliedStart &&
             (suppliedStart == default ||
              suppliedStart.Offset != TimeSpan.Zero)))
        {
            return Failure<StagingResult>(
                "InvalidProcessIdentity",
                "Optional game process identity is incomplete or invalid.");
        }

        OperationResult<(string Root, string InstallId)> install =
            ResolveInstallationIdentity(options.InstallationRoot);
        if (!install.IsSuccess)
            return Failure<StagingResult>(
                install.ErrorCode, install.Details);

        string installationRoot = install.Value.Root;
        string installId = install.Value.InstallId;
        string stateRoot = stateRootOverride ??
            ResolveStateRoot(installId);
        if (string.IsNullOrWhiteSpace(stateRoot))
            return Failure<StagingResult>(
                "RecoveryRequired", "Updater state root could not be resolved.");

        string? stateBoundary = stateRootOverride is null
            ? Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData)
            : null;
        OperationResult<bool> stateRootReady =
            EnsureStateRoot(stateRoot, stateBoundary);
        if (!stateRootReady.IsSuccess)
            return Failure<StagingResult>(
                "RecoveryRequired", stateRootReady.Details);

        OperationResult<FileStream> lockResult =
            AcquireOperationLock(stateRoot);
        if (!lockResult.IsSuccess || lockResult.Value is null)
            return Failure<StagingResult>(
                "RecoveryRequired", lockResult.Details);
        using FileStream operationLock = lockResult.Value;

        var state = new UpdateStateManager(installationRoot, stateRoot);
        OperationResult<ActiveSlotState> activeResult = state.LoadActiveSlot();
        if (!activeResult.IsSuccess || activeResult.Value is null)
            return Failure<StagingResult>(
                activeResult.ErrorCode, activeResult.Details);
        ActiveSlotState active = activeResult.Value;
        logger?.Info(
            $"Active slot validated. slotId={active.Pointer.ActiveSlotId}; " +
            $"version={active.Pointer.ActiveVersion}; " +
            $"generation={active.Pointer.Generation}.");

        if (!UpdateVersion.TryParse(
                active.ActiveSlot.Metadata.Version,
                out UpdateVersion activeVersion) ||
            newVersion <= activeVersion)
            return Failure<StagingResult>(
                "InvalidVersion",
                "Updater refuses versions equal to or older than the active slot.");

        OperationResult<string> packagePathResult =
            ValidatePackageLocation(
                installationRoot,
                options.PackagePath);
        if (!packagePathResult.IsSuccess || packagePathResult.Value is null)
            return Failure<StagingResult>(
                packagePathResult.ErrorCode, packagePathResult.Details);
        string packagePath = packagePathResult.Value;

        logger?.Info(
            $"Starting staging. operationId={options.OperationId}; " +
            $"package={packagePath}; version={options.ExpectedVersion}; " +
            $"expectedSize={options.ExpectedFileSize}.");

        if (options.GamePid is int gamePid &&
            options.GameStartTimeUtc is DateTimeOffset gameStart)
        {
            var expectedGame = new ProcessIdentity(
                gamePid,
                gameStart,
                active.ActiveSlot.ExecutablePath);
            OperationResult<bool> wait =
                await processWaiter.WaitForExitAsync(
                    expectedGame,
                    processWaitTimeout,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!wait.IsSuccess)
            {
                logger?.Error(
                    $"Game process wait failed: {wait.ErrorCode}: {wait.Details}");
                return Failure<StagingResult>(
                    wait.ErrorCode, wait.Details);
            }
        }

        OperationResult<VerifiedPackage> verifiedResult =
            await HashVerifier.OpenVerifiedAsync(
                packagePath,
                options.ExpectedFileSize,
                options.ExpectedSha256,
                limits,
                cancellationToken).ConfigureAwait(false);
        if (!verifiedResult.IsSuccess || verifiedResult.Value is null)
        {
            logger?.Error(
                $"Package verification failed: {verifiedResult.ErrorCode}: " +
                verifiedResult.Details);
            return Failure<StagingResult>(
                verifiedResult.ErrorCode, verifiedResult.Details);
        }

        await using VerifiedPackage verified = verifiedResult.Value;
        logger?.Info(
            $"Package SHA-256 verified. operationId={options.OperationId}; " +
            $"sha256={options.ExpectedSha256}.");

        OperationResult<int> entryCountCheck =
            PackagePreflight.CheckEntryCountBeforeArchiveOpen(
                verified.Stream,
                limits.MaxArchiveEntries,
                limits.MaxCentralDirectoryBytes);
        if (!entryCountCheck.IsSuccess)
        {
            logger?.Error(
                $"ZIP entry-count check failed: {entryCountCheck.ErrorCode}: " +
                entryCountCheck.Details);
            return Failure<StagingResult>(
                entryCountCheck.ErrorCode, entryCountCheck.Details);
        }

        ZipArchive archive;
        try
        {
            archive = new ZipArchive(
                verified.Stream,
                ZipArchiveMode.Read,
                leaveOpen: true);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or
            IOException or
            ArgumentException)
        {
            logger?.Error(
                $"ZIP open failed: MalformedArchive: {exception.Message}");
            return Failure<StagingResult>(
                "MalformedArchive", exception.Message);
        }

        using (archive)
        {
            string slotsRoot = Path.Combine(installationRoot, "Slots");
            string probeSlotRoot = Path.Combine(
                slotsRoot,
                "slot-" + new string('0', 32));
            OperationResult<PackagePreflightResult> preflight =
                PackagePreflight.Inspect(archive, probeSlotRoot, limits);
            if (!preflight.IsSuccess || preflight.Value is null)
            {
                logger?.Error(
                    $"ZIP preflight failed: {preflight.ErrorCode}: " +
                    preflight.Details);
                return Failure<StagingResult>(
                    preflight.ErrorCode, preflight.Details);
            }

            logger?.Info(
                $"ZIP preflight passed. operationId={options.OperationId}; " +
                $"entries={preflight.Value.EntryCount}; " +
                $"expandedBytes={preflight.Value.TotalUncompressedBytes}.");

            string newSlotId;
            try
            {
                newSlotId = slotIdFactory();
            }
            catch (Exception exception) when (
                exception is CryptographicException or
                InvalidOperationException)
            {
                return Failure<StagingResult>(
                    "StateWriteFailed", exception.Message);
            }

            if (!PathSafety.IsValidSlotId(newSlotId))
                return Failure<StagingResult>(
                    "StateWriteFailed",
                    "Generated slot identity does not meet the slot contract.");
            if (newSlotId.Equals(
                    active.Pointer.ActiveSlotId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Failure<StagingResult>(
                    "ActiveSlotConflict",
                    "New slot identity matches the active slot.");
            }
            if (newSlotId.Equals(
                    active.Pointer.PreviousSlotId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Failure<StagingResult>(
                    "ActiveSlotConflict",
                    "New slot identity matches the previous slot.");
            }

            string newSlotRoot = Path.Combine(slotsRoot, newSlotId);
            if (File.Exists(newSlotRoot) || Directory.Exists(newSlotRoot))
                return Failure<StagingResult>(
                    "ActiveSlotConflict",
                    "Generated slot destination already exists.");

            long generation;
            try
            {
                generation = checked(active.Pointer.Generation + 1);
            }
            catch (OverflowException)
            {
                return Failure<StagingResult>(
                    "RecoveryRequired", "Active generation cannot be incremented.");
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            var draft = new TransactionJournal(
                TransactionJournal.CurrentSchemaVersion,
                installId,
                options.OperationId,
                TransactionPhase.None,
                active.Pointer.ActiveSlotId,
                newSlotId,
                active.ActiveSlot.Metadata.Version,
                options.ExpectedVersion,
                options.ExpectedSha256,
                options.LauncherPid,
                options.LauncherStartTimeUtc,
                generation,
                now);
            OperationResult<TransactionJournal> started =
                TransactionStateMachine.Start(draft, now);
            if (!started.IsSuccess || started.Value is null)
                return Failure<StagingResult>(
                    "StateWriteFailed", started.Details);

            OperationResult<bool> preparingSaved =
                state.WriteJournal(started.Value);
            if (!preparingSaved.IsSuccess)
            {
                logger?.Error(
                    $"PREPARING journal write failed: {preparingSaved.Details}");
                return Failure<StagingResult>(
                    "StateWriteFailed", preparingSaved.Details);
            }
            logger?.Info(
                $"Transaction PREPARING. operationId={options.OperationId}; " +
                $"newSlotId={newSlotId}.");

            OperationResult<bool> staged = await ExtractAndValidateAsync(
                archive,
                preflight.Value,
                installationRoot,
                installId,
                newSlotRoot,
                newSlotId,
                generation,
                options,
                limits,
                extractionProgress,
                cancellationToken).ConfigureAwait(false);

            if (!staged.IsSuccess)
            {
                logger?.Error(
                    $"Staging failed: {staged.ErrorCode}: {staged.Details}");
                return await AbortPreparingAsync(
                    state,
                    started.Value,
                    installationRoot,
                    newSlotRoot,
                    staged.ErrorCode,
                    staged.Details,
                    cleanupSlot: staged.ErrorCode != "ActiveSlotConflict")
                    .ConfigureAwait(false);
            }
            logger?.Info(
                $"Extraction complete and slot validation passed. " +
                $"operationId={options.OperationId}; slotId={newSlotId}.");

            OperationResult<TransactionJournal?> preparedTransition =
                TransactionStateMachine.TryTransition(
                    started.Value,
                    TransactionPhase.Prepared,
                    DateTimeOffset.UtcNow);
            if (!preparedTransition.IsSuccess ||
                preparedTransition.Value is null)
            {
                return await AbortPreparingAsync(
                    state,
                    started.Value,
                    installationRoot,
                    newSlotRoot,
                    "StateWriteFailed",
                    preparedTransition.Details).ConfigureAwait(false);
            }

            OperationResult<bool> journalValidation =
                preparedTransition.Value.Validate(installId);
            if (!journalValidation.IsSuccess)
            {
                return await AbortPreparingAsync(
                    state,
                    started.Value,
                    installationRoot,
                    newSlotRoot,
                    "StateWriteFailed",
                    journalValidation.Details).ConfigureAwait(false);
            }

            OperationResult<bool> preparedSaved =
                state.WriteJournal(preparedTransition.Value);
            if (!preparedSaved.IsSuccess)
            {
                logger?.Error(
                    $"PREPARED journal write failed; preserving staging for recovery: " +
                    preparedSaved.Details);
                return Failure<StagingResult>(
                    "RecoveryRequired", preparedSaved.Details);
            }

            logger?.Info(
                $"Transaction PREPARED. operationId={options.OperationId}; " +
                $"newSlotId={newSlotId}; activeSlotUnchanged=true.");
            return OperationResult<StagingResult>.Success(
                new StagingResult(
                    newSlotId,
                    generation,
                    TransactionPhase.Prepared,
                    newSlotRoot));
        }
    }

    private static async Task<OperationResult<bool>> ExtractAndValidateAsync(
        ZipArchive archive,
        PackagePreflightResult preflight,
        string installationRoot,
        string installId,
        string newSlotRoot,
        string newSlotId,
        long generation,
        UpdaterOptions options,
        UpdaterLimits limits,
        Action<int, long>? extractionProgress,
        CancellationToken cancellationToken)
    {
        try
        {
            if (File.Exists(newSlotRoot) || Directory.Exists(newSlotRoot))
                return OperationResult<bool>.Failure(
                    "ActiveSlotConflict", "Slot destination appeared before extraction.");

            OperationResult<bool> extracted = await ZipExtractor.ExtractAsync(
                preflight,
                installationRoot,
                newSlotRoot,
                limits,
                extractionProgress,
                cancellationToken).ConfigureAwait(false);
            if (!extracted.IsSuccess)
                return extracted;

            var metadata = new SlotInfo(
                SlotInfo.CurrentSchemaVersion,
                newSlotId,
                installId,
                options.ExpectedVersion,
                UpdaterOptions.ExpectedExecutableRelativePath,
                generation,
                options.OperationId,
                DateTimeOffset.UtcNow,
                false,
                options.ExpectedSha256);
            OperationResult<bool> metadataWrite =
                AtomicFileWriter.WriteJson(
                    Path.Combine(newSlotRoot, "slot.json"),
                    metadata);
            if (!metadataWrite.IsSuccess)
                return OperationResult<bool>.Failure(
                    "SlotValidationFailed", metadataWrite.Details);

            OperationResult<ValidatedSlot> slotValidation =
                new SlotValidator(installationRoot, installId).Validate(
                    newSlotId,
                    options.OperationId,
                    options.ExpectedVersion,
                    options.ExpectedSha256,
                    generation);
            if (!slotValidation.IsSuccess)
                return OperationResult<bool>.Failure(
                    "SlotValidationFailed", slotValidation.Details);

            return OperationResult<bool>.Success(true);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            NotSupportedException)
        {
            return OperationResult<bool>.Failure(
                "SlotValidationFailed", exception.Message);
        }
    }

    private static async Task<OperationResult<StagingResult>> AbortPreparingAsync(
        UpdateStateManager state,
        TransactionJournal preparing,
        string installationRoot,
        string newSlotRoot,
        string originalError,
        string originalDetails,
        bool cleanupSlot = true)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        OperationResult<TransactionJournal?> rollbackTransition =
            TransactionStateMachine.TryTransition(
                preparing,
                TransactionPhase.RollbackRequired,
                DateTimeOffset.UtcNow);
        if (!rollbackTransition.IsSuccess ||
            rollbackTransition.Value is null)
        {
            return Failure<StagingResult>(
                "RecoveryRequired",
                $"{originalError}: {originalDetails}; rollback transition failed: " +
                rollbackTransition.Details);
        }

        OperationResult<bool> rollbackSaved =
            state.WriteJournal(rollbackTransition.Value);
        if (!rollbackSaved.IsSuccess)
            return Failure<StagingResult>(
                "RecoveryRequired",
                $"{originalError}: {originalDetails}; rollback journal failed: " +
                rollbackSaved.Details);

        if (cleanupSlot)
        {
            OperationResult<bool> cleanup = DeleteIncompleteSlot(
                installationRoot,
                newSlotRoot);
            if (!cleanup.IsSuccess)
                return Failure<StagingResult>(
                    "RecoveryRequired",
                    $"{originalError}: {originalDetails}; incomplete slot cleanup failed: " +
                    cleanup.Details);
        }

        OperationResult<TransactionJournal?> rolledBackTransition =
            TransactionStateMachine.TryTransition(
                rollbackTransition.Value,
                TransactionPhase.RolledBack,
                DateTimeOffset.UtcNow);
        if (!rolledBackTransition.IsSuccess ||
            rolledBackTransition.Value is null)
        {
            return Failure<StagingResult>(
                "RecoveryRequired",
                $"{originalError}: {originalDetails}; ROLLED_BACK transition failed: " +
                rolledBackTransition.Details);
        }

        OperationResult<bool> rolledBackSaved =
            state.WriteJournal(rolledBackTransition.Value);
        if (!rolledBackSaved.IsSuccess)
            return Failure<StagingResult>(
                "RecoveryRequired",
                $"{originalError}: {originalDetails}; ROLLED_BACK journal failed: " +
                rolledBackSaved.Details);

        OperationResult<bool> cleared = state.ClearJournal();
        if (!cleared.IsSuccess)
            return Failure<StagingResult>(
                "RecoveryRequired",
                $"{originalError}: {originalDetails}; transaction cleanup failed: " +
                cleared.Details);

        return Failure<StagingResult>(originalError, originalDetails);
    }

    private static OperationResult<bool> DeleteIncompleteSlot(
        string installationRoot,
        string slotRoot)
    {
        try
        {
            if (!Directory.Exists(slotRoot))
                return OperationResult<bool>.Success(true);

            string root = Path.GetFullPath(installationRoot);
            string slot = Path.GetFullPath(slotRoot);
            string relative = Path.GetRelativePath(root, slot);
            if (Path.IsPathRooted(relative) ||
                relative == "." ||
                relative == ".." ||
                relative.StartsWith(
                    ".." + Path.DirectorySeparatorChar,
                    StringComparison.Ordinal) ||
                relative.StartsWith(
                    ".." + Path.AltDirectorySeparatorChar,
                    StringComparison.Ordinal))
            {
                return OperationResult<bool>.Failure(
                    "RecoveryRequired",
                    "Refusing to clean a slot outside the installation.");
            }

            OperationResult<bool> noLinks =
                PathSafety.EnsureNoReparsePoints(root, slot);
            if (!noLinks.IsSuccess)
                return OperationResult<bool>.Failure(
                    "RecoveryRequired", noLinks.Details);

            Directory.Delete(slot, recursive: true);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            NotSupportedException)
        {
            return OperationResult<bool>.Failure(
                "RecoveryRequired", exception.Message);
        }
    }

    private static OperationResult<(string Root, string InstallId)>
        ResolveInstallationIdentity(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return OperationResult<(string, string)>.Failure(
                    "RecoveryRequired", "Installation root is missing.");

            string root = Path.GetFullPath(path);
            string volumeRoot = Path.GetPathRoot(root)!;
            OperationResult<bool> rootCheck =
                PathSafety.EnsureNoReparsePoints(volumeRoot, root);
            if (!rootCheck.IsSuccess)
                return OperationResult<(string, string)>.Failure(
                    "RecoveryRequired", rootCheck.Details);

            string idPath = Path.Combine(root, "install.id");
            if (!File.Exists(idPath))
                return OperationResult<(string, string)>.Failure(
                    "RecoveryRequired", "install.id is missing.");
            if ((File.GetAttributes(idPath) & FileAttributes.ReparsePoint) != 0 ||
                new FileInfo(idPath).Length > 64)
            {
                return OperationResult<(string, string)>.Failure(
                    "RecoveryRequired", "install.id is unsafe.");
            }

            string installId = File.ReadAllText(idPath).Trim();
            if (!PathSafety.IsValidInstallId(installId))
                return OperationResult<(string, string)>.Failure(
                    "RecoveryRequired", "install.id is malformed.");

            return OperationResult<(string, string)>.Success(
                (root, installId.ToLowerInvariant()));
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

    private string ResolveStateRoot(string installId)
    {
        string localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        OperationResult<string> result = InstallIdentity.ResolveStateRoot(
            localAppData,
            InstallIdentity.StablePublisher,
            installId);
        return result.IsSuccess && result.Value is not null
            ? result.Value
            : string.Empty;
    }

    private static OperationResult<bool> EnsureStateRoot(
        string path,
        string? boundaryRoot)
    {
        try
        {
            OperationResult<bool> beforeCreate =
                PathSafety.EnsureNoReparsePoints(
                    boundaryRoot ?? path,
                    path);
            if (!beforeCreate.IsSuccess)
                return OperationResult<bool>.Failure(
                    "RecoveryRequired", beforeCreate.Details);

            Directory.CreateDirectory(path);
            OperationResult<bool> result =
                PathSafety.EnsureNoReparsePoints(
                    boundaryRoot ?? path,
                    path);
            return result.IsSuccess
                ? result
                : OperationResult<bool>.Failure(
                    "RecoveryRequired", result.Details);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            NotSupportedException)
        {
            return OperationResult<bool>.Failure(
                "RecoveryRequired", exception.Message);
        }
    }

    private static OperationResult<FileStream> AcquireOperationLock(
        string stateRoot)
    {
        try
        {
            string lockPath = Path.Combine(stateRoot, "staging.lock");
            if (File.Exists(lockPath) &&
                (File.GetAttributes(lockPath) & FileAttributes.ReparsePoint) != 0)
            {
                return OperationResult<FileStream>.Failure(
                    "RecoveryRequired",
                    "Staging lock cannot be a reparse point.");
            }

            return OperationResult<FileStream>.Success(new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                1,
                FileOptions.WriteThrough));
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            NotSupportedException)
        {
            return OperationResult<FileStream>.Failure(
                "RecoveryRequired",
                "Another staging operation may be active or the lock is unavailable: " +
                exception.Message);
        }
    }

    private static OperationResult<string> ValidatePackageLocation(
        string installationRoot,
        string packagePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(packagePath) ||
                !Path.IsPathFullyQualified(packagePath))
                return OperationResult<string>.Failure(
                    "PackageMissing", "Package path is required.");

            string root = Path.GetFullPath(installationRoot);
            string canonicalPackage = Path.GetFullPath(packagePath);
            if (!Path.GetExtension(canonicalPackage).Equals(
                    ".zip",
                    StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<string>.Failure(
                    "PackageNotRegularFile", "Update package must be a ZIP file.");
            }

            string[] trustedLocations =
            [
                Path.Combine(root, "Update", "Cache"),
                Path.Combine(root, "Update", "Temp")
            ];
            bool trusted = false;
            foreach (string location in trustedLocations)
            {
                if (!Directory.Exists(location))
                    continue;
                OperationResult<bool> noLinks =
                    PathSafety.EnsureNoReparsePoints(root, location);
                if (!noLinks.IsSuccess)
                    return OperationResult<string>.Failure(
                        "PackageNotRegularFile", noLinks.Details);

                string relative = Path.GetRelativePath(location, canonicalPackage);
                if (relative == "." ||
                    Path.IsPathRooted(relative) ||
                    relative == ".." ||
                    relative.StartsWith(
                        ".." + Path.DirectorySeparatorChar,
                        StringComparison.Ordinal) ||
                    relative.StartsWith(
                        ".." + Path.AltDirectorySeparatorChar,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                OperationResult<string> safeRelative =
                    PathSafety.ResolveRelativePath(location, relative);
                if (!safeRelative.IsSuccess || safeRelative.Value is null)
                    return OperationResult<string>.Failure(
                        "PackageNotRegularFile", safeRelative.Details);
                OperationResult<bool> packageLinks =
                    PathSafety.EnsureNoReparsePoints(root, safeRelative.Value);
                if (!packageLinks.IsSuccess)
                    return OperationResult<string>.Failure(
                        "PackageNotRegularFile", packageLinks.Details);
                if (!safeRelative.Value.Equals(
                        canonicalPackage,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return OperationResult<string>.Failure(
                        "PackageNotRegularFile",
                        "Canonical package path differs from its trusted cache path.");
                }

                trusted = true;
                break;
            }

            if (!trusted)
                return OperationResult<string>.Failure(
                    "PackageNotRegularFile",
                    "Package must be inside Update/Cache or Update/Temp.");
            if (!File.Exists(canonicalPackage))
                return OperationResult<string>.Failure(
                    "PackageMissing", "Package file does not exist.");

            FileAttributes attributes = File.GetAttributes(canonicalPackage);
            if ((attributes &
                 (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            {
                return OperationResult<string>.Failure(
                    "PackageNotRegularFile",
                    "Package must be a regular file, not a directory or reparse point.");
            }

            return OperationResult<string>.Success(canonicalPackage);
        }
        catch (FileNotFoundException exception)
        {
            return OperationResult<string>.Failure(
                "PackageMissing", exception.Message);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            NotSupportedException)
        {
            return OperationResult<string>.Failure(
                "PackageNotRegularFile", exception.Message);
        }
    }

    private static string NewSlotId() =>
        "slot-" + Convert.ToHexString(
            RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    private static OperationResult<T> Failure<T>(
        string errorCode,
        string details) =>
        OperationResult<T>.Failure(errorCode, details);
}
