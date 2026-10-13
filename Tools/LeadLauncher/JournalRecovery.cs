namespace LeadLauncher;

public sealed record RecoveryOutcome(
    ValidatedSlot ActiveSlot,
    ProcessIdentity? ExistingTrialProcess,
    string Decision);

public sealed class JournalRecovery
{
    private readonly LauncherOptions options;
    private readonly SlotValidator slots;
    private readonly Func<DateTimeOffset> utcNow;
    private readonly Func<int, OperationResult<ProcessIdentity>> processLookup;
    private readonly Func<ProcessIdentity, OperationResult<bool>>
        terminateTrialProcess;
    private readonly Func<ValidatedSlot, TransactionJournal,
        OperationResult<ProcessIdentity>> launchTrial;
    private readonly Func<TimeSpan, CancellationToken, Task> delay;
    private readonly CancellationToken cancellationToken;
    private readonly LauncherLogger? logger;

    public JournalRecovery(
        LauncherOptions options,
        Func<DateTimeOffset>? utcNow = null,
        Func<int, OperationResult<ProcessIdentity>>? processLookup = null,
        Func<ProcessIdentity, OperationResult<bool>>? terminateTrialProcess = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null,
        CancellationToken cancellationToken = default,
        LauncherLogger? logger = null,
        Func<ValidatedSlot, TransactionJournal,
            OperationResult<ProcessIdentity>>? launchTrial = null)
    {
        this.options = options;
        slots = new SlotValidator(options.InstallationRoot, options.InstallId);
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        this.processLookup = processLookup ?? ProcessIdentity.TryGet;
        this.terminateTrialProcess =
            terminateTrialProcess ?? ProcessIdentity.TryTerminate;
        this.launchTrial = launchTrial ??
            ((slot, journal) =>
                new GameLauncher().LaunchTrial(
                    slot, journal, options.SignalsRoot));
        this.delay = delay ?? Task.Delay;
        this.cancellationToken = cancellationToken;
        this.logger = logger;
    }

    public async Task<OperationResult<RecoveryOutcome>> RecoverAsync()
    {
        try
        {
            Directory.CreateDirectory(options.StateRoot);
            Directory.CreateDirectory(options.SignalsRoot);
            OperationResult<bool> signalsPathCheck =
                PathSafety.EnsureNoReparsePoints(
                    options.StateRoot,
                    options.SignalsRoot);
            if (!signalsPathCheck.IsSuccess)
                return Failure(
                    signalsPathCheck.ErrorCode,
                    signalsPathCheck.Details);
            OperationResult<bool> temporaryCleanup =
                AtomicFileWriter.CleanupStaleTemporaryFiles(
                    options.StateRoot);
            if (!temporaryCleanup.IsSuccess)
                return Failure(
                    temporaryCleanup.ErrorCode,
                    temporaryCleanup.Details);
            if (Directory.Exists(options.TransactionPath))
                return Failure(
                    "InvalidTransaction",
                    "transaction.json cannot be a directory.");
            if (!File.Exists(options.TransactionPath))
            {
                logger?.Info("Transaction phase=NONE.");
                return RecoverWithoutTransaction();
            }

            OperationResult<TransactionJournal> journalResult =
                AtomicFileWriter.ReadJson<TransactionJournal>(
                    options.TransactionPath);
            if (!journalResult.IsSuccess || journalResult.Value is null)
                return Failure("InvalidTransaction", journalResult.Details);

            TransactionJournal journal = journalResult.Value;
            OperationResult<bool> journalValidation =
                journal.Validate(options.InstallId);
            if (!journalValidation.IsSuccess)
                return Failure(
                    journalValidation.ErrorCode,
                    journalValidation.Details);
            logger?.Info($"Transaction phase={journal.Phase}.");

            OperationResult<ValidatedSlot> oldSlot =
                slots.Validate(journal.OldSlotId);
            if (!oldSlot.IsSuccess || oldSlot.Value is null)
                return Failure("OldSlotInvalid", oldSlot.Details);
            if (!oldSlot.Value.Metadata.HealthConfirmed)
                return Failure(
                    "OldSlotUnconfirmed",
                    "Recovery cannot launch an unconfirmed old slot.");

            bool requiresNewSlot = journal.Phase is
                TransactionPhase.Prepared or
                TransactionPhase.Trial or
                TransactionPhase.HealthyConfirmed or
                TransactionPhase.Committed;
            OperationResult<ValidatedSlot> newSlotResult =
                slots.Validate(journal.NewSlotId);
            ValidatedSlot? newSlot =
                newSlotResult.IsSuccess ? newSlotResult.Value : null;
            if (requiresNewSlot && newSlot is null)
                return Failure("NewSlotInvalid", newSlotResult.Details);
            if (newSlot is not null &&
                !MatchesTransaction(journal, oldSlot.Value, newSlot))
            {
                if (requiresNewSlot)
                {
                    return Failure(
                        "TransactionInconsistent",
                        "Transaction slot metadata does not match its journal.");
                }
                newSlot = null;
            }

            switch (journal.Phase)
            {
                case TransactionPhase.Preparing:
                    return RecoverUncommitted(
                        journal, oldSlot.Value,
                        "Uncommitted transaction rolled back.");
                case TransactionPhase.Prepared:
                    return await StartPreparedTrialAsync(
                        journal, oldSlot.Value, newSlot!)
                        .ConfigureAwait(false);
                case TransactionPhase.Trial:
                {
                    OperationResult<ActivePointer?> active =
                        ReadPointerIfPresent(options.ActivePointerPath);
                    OperationResult<ActivePointer?> previous =
                        ReadPointerIfPresent(options.PreviousPointerPath);
                    if (!active.IsSuccess || !previous.IsSuccess)
                        return Failure(
                            "PointerInvalid",
                            !active.IsSuccess
                                ? active.Details
                                : previous.Details);
                    return await RecoverTrialAsync(
                        journal, oldSlot.Value, newSlot!,
                        active.Value, previous.Value);
                }
                case TransactionPhase.HealthyConfirmed:
                    return Commit(
                        journal,
                        oldSlot.Value,
                        newSlot!,
                        FindRunningTrialProcess(journal, newSlot!));
                case TransactionPhase.Committed:
                {
                    OperationResult<ActivePointer?> active =
                        ReadPointerLenient(options.ActivePointerPath);
                    OperationResult<ActivePointer?> previous =
                        ReadPointerIfPresent(options.PreviousPointerPath);
                    if (!previous.IsSuccess)
                        return Failure(
                            "PreviousPointerInvalid",
                            previous.Details);
                    return RecoverCommitted(
                        journal, oldSlot.Value, newSlot!,
                        active.Value, previous.Value);
                }
                case TransactionPhase.RollbackRequired:
                    return RollBack(journal, oldSlot.Value);
                case TransactionPhase.RolledBack:
                    return CompleteRollback(journal, oldSlot.Value);
                default:
                    return Failure(
                        "InvalidTransactionPhase",
                        $"Unsupported persisted phase {journal.Phase}.");
            }
        }
        catch (OperationCanceledException)
        {
            return Failure("RecoveryCancelled", "Recovery was cancelled.");
        }
        catch (Exception exception)
        {
            return Failure("RecoveryFailed", exception.Message);
        }
    }

    public static TrialDecision EvaluateTrial(
        bool receiptValid,
        DateTimeOffset nowUtc,
        DateTimeOffset trialStartedAtUtc,
        DateTimeOffset? healthyAtUtc)
    {
        DateTimeOffset deadline =
            trialStartedAtUtc + HealthPolicy.TrialTimeout;
        if (!receiptValid)
            return nowUtc >= deadline
                ? TrialDecision.RollBack
                : TrialDecision.Pending;

        if (healthyAtUtc is null ||
            healthyAtUtc.Value < trialStartedAtUtc ||
            healthyAtUtc.Value >
                deadline - HealthPolicy.HealthyConfirmationWindow)
        {
            return TrialDecision.RollBack;
        }

        return nowUtc >= healthyAtUtc.Value +
               HealthPolicy.HealthyConfirmationWindow
            ? TrialDecision.Commit
            : nowUtc >= deadline
                ? TrialDecision.RollBack
                : TrialDecision.Pending;
    }

    private async Task<OperationResult<RecoveryOutcome>> StartPreparedTrialAsync(
        TransactionJournal journal,
        ValidatedSlot oldSlot,
        ValidatedSlot newSlot)
    {
        OperationResult<ActivePointer?> active =
            ReadPointerIfPresent(options.ActivePointerPath);
        OperationResult<ActivePointer?> previous =
            ReadPointerIfPresent(options.PreviousPointerPath);
        if (!active.IsSuccess || !previous.IsSuccess)
            return Failure(
                "PointerInvalid",
                !active.IsSuccess ? active.Details : previous.Details);
        if (active.Value is null ||
            !active.Value.Matches(oldSlot) ||
            !PointerHistoryMatches(active.Value, previous.Value))
        {
            return Failure(
                "TransactionInconsistent",
                "PREPARED must leave the old active and previous pointers unchanged.");
        }
        if (newSlot.Metadata.HealthConfirmed ||
            File.Exists(Path.Combine(
                options.SignalsRoot,
                journal.OperationId + ".healthy.json")))
        {
            return Failure(
                "TransactionInconsistent",
                "A prepared trial cannot already be health-confirmed.");
        }

        logger?.Info(
            $"Starting trial. operationId={journal.OperationId}; " +
            $"oldSlot={journal.OldSlotId}; newSlot={journal.NewSlotId}.");
        OperationResult<ProcessIdentity> launched =
            launchTrial(newSlot, journal);
        if (!launched.IsSuccess || launched.Value is null)
        {
            return RollBack(
                journal,
                oldSlot,
                $"Trial executable could not start: {launched.ErrorCode}.");
        }

        ProcessIdentity launchedIdentity = launched.Value;
        var expectedIdentity = new ProcessIdentity(
            launchedIdentity.Pid,
            launchedIdentity.ProcessStartTimeUtc,
            newSlot.ExecutablePath);
        if (!expectedIdentity.Matches(launchedIdentity))
            return Failure(
                "ProcessIdentityMismatch",
                "Trial launcher returned an unexpected process identity.");

        OperationResult<ProcessIdentity> actual =
            processLookup(launchedIdentity.Pid);
        if (!actual.IsSuccess || actual.Value is null ||
            !expectedIdentity.Matches(actual.Value))
        {
            OperationResult<bool> stopped =
                terminateTrialProcess(expectedIdentity);
            if (!stopped.IsSuccess)
                return Failure(stopped.ErrorCode, stopped.Details);
            return RollBack(
                journal,
                oldSlot,
                "Trial process identity could not be verified after launch.");
        }

        OperationResult<TransactionJournal?> trial =
            Transition(
                journal,
                TransactionPhase.Trial,
                actual.Value);
        if (!trial.IsSuccess || trial.Value is null)
        {
            OperationResult<bool> stopped =
                terminateTrialProcess(actual.Value);
            if (!stopped.IsSuccess)
                return Failure(
                    "TrialStateWriteFailed",
                    $"{trial.Details}; verified trial process stop failed: " +
                    stopped.Details);
            return Failure(trial.ErrorCode, trial.Details);
        }

        logger?.Info(
            $"Transaction phase=TRIAL; operationId={journal.OperationId}; " +
            $"pid={actual.Value.Pid}.");
        return await RecoverTrialAsync(
            trial.Value,
            oldSlot,
            newSlot,
            active.Value,
            previous.Value).ConfigureAwait(false);
    }

    private OperationResult<RecoveryOutcome> RecoverWithoutTransaction()
    {
        OperationResult<ActivePointer?> active =
            ReadPointerIfPresent(options.ActivePointerPath);
        OperationResult<ActivePointer?> previous =
            ReadPointerIfPresent(options.PreviousPointerPath);
        if (!active.IsSuccess || active.Value is null)
            return Failure(
                "ActivePointerInvalid",
                active.IsSuccess
                    ? "active.json is missing."
                    : active.Details);
        if (!previous.IsSuccess)
            return Failure("PreviousPointerInvalid", previous.Details);

        OperationResult<ValidatedSlot> activeSlot =
            ValidatePointerSlot(active.Value);
        if (!activeSlot.IsSuccess || activeSlot.Value is null)
            return Failure("ActiveSlotInvalid", activeSlot.Details);

        if (previous.Value is not null)
        {
            OperationResult<ValidatedSlot> previousSlot =
                ValidatePointerSlot(previous.Value);
            if (!previousSlot.IsSuccess ||
                !previousSlot.Value!.Metadata.HealthConfirmed)
                return Failure(
                    "PreviousSlotInvalid",
                    previousSlot.IsSuccess
                        ? "Previous slot is not health-confirmed."
                        : previousSlot.Details);
            if (active.Value.PreviousSlotId is not null &&
                !active.Value.PreviousSlotId.Equals(
                    previous.Value.ActiveSlotId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Failure(
                    "PointerHistoryMismatch",
                    "Active and previous pointer references disagree.");
            }
            if (active.Value.Sequence <= previous.Value.Sequence)
                return Failure(
                    "PointerSequenceAmbiguous",
                    "Active pointer sequence must exceed its backup.");
        }
        else if (active.Value.PreviousSlotId is not null)
        {
            return Failure(
                "PreviousPointerMissing",
                "Active pointer names a previous slot but backup pointer is missing.");
        }
        if (!activeSlot.Value.Metadata.HealthConfirmed)
            return Failure(
                "ActiveSlotUnconfirmed",
                "Active slot is not health-confirmed.");

        logger?.Info(
            $"Active pointer validated for slot " +
            $"{activeSlot.Value.Metadata.SlotId}.");

        return Success(activeSlot.Value, null, "Active pointer validated.");
    }

    private OperationResult<RecoveryOutcome> RecoverUncommitted(
        TransactionJournal journal,
        ValidatedSlot oldSlot,
        string decision)
    {
        OperationResult<TransactionJournal?> transition =
            Transition(journal, TransactionPhase.RollbackRequired);
        if (!transition.IsSuccess)
            return Failure(transition.ErrorCode, transition.Details);
        return RollBack(transition.Value!, oldSlot, decision);
    }

    private async Task<OperationResult<RecoveryOutcome>> RecoverTrialAsync(
        TransactionJournal journal,
        ValidatedSlot oldSlot,
        ValidatedSlot newSlot,
        ActivePointer? active,
        ActivePointer? previous)
    {
        if (active is null || !active.Matches(oldSlot))
            return Failure(
                "TransactionInconsistent",
                "TRIAL must leave the old slot as the active pointer.");
        if (newSlot.Metadata.HealthConfirmed)
            return Failure(
                "TransactionInconsistent",
                "A TRIAL slot must not already be marked health-confirmed.");
        if (!PointerHistoryMatches(active, previous) ||
            !oldSlot.Metadata.HealthConfirmed)
            return Failure(
                "TransactionInconsistent",
                "TRIAL changed or corrupted the old pointer history.");

        DateTimeOffset started = journal.TrialStartedAtUtc!.Value;
        DateTimeOffset? healthyAt = null;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DateTimeOffset now = utcNow();
            OperationResult<ProcessIdentity> actual =
                processLookup(journal.TrialProcessPid);
            if (!actual.IsSuccess || actual.Value is null)
                return RollBack(
                    journal, oldSlot,
                    "TRIAL process exited or identity could not be verified.");
            var expectedProcess = new ProcessIdentity(
                journal.TrialProcessPid,
                journal.TrialProcessStartTimeUtc!.Value,
                newSlot.ExecutablePath);
            if (!expectedProcess.Matches(actual.Value))
                return RollBack(
                    journal,
                    oldSlot,
                    "TRIAL process identity no longer matches the new slot.");

            string signalPath = Path.Combine(
                options.SignalsRoot,
                journal.OperationId + ".healthy.json");
            bool signalExists = File.Exists(signalPath);
            bool receiptValid = false;
            if (signalExists)
            {
                OperationResult<HealthReceipt> receipt =
                    HealthReceiptValidator.ReadAndValidate(
                        options.SignalsRoot,
                        journal,
                        newSlot,
                        actual.Value,
                        now);
                if (!receipt.IsSuccess || receipt.Value is null)
                    return RollBackTrialProcess(
                        journal,
                        oldSlot,
                        actual.Value,
                        "TRIAL health receipt failed identity validation.");

                receiptValid = true;
                healthyAt = receipt.Value.HealthyAtUtc;
            }

            TrialDecision decision = EvaluateTrial(
                receiptValid, now, started, healthyAt);
            if (decision == TrialDecision.RollBack)
                return RollBackTrialProcess(
                    journal,
                    oldSlot,
                    actual.Value,
                    "TRIAL timed out before health confirmation.");
            if (decision == TrialDecision.Commit)
            {
                OperationResult<TransactionJournal?> transition =
                    Transition(
                        journal,
                        TransactionPhase.HealthyConfirmed);
                if (!transition.IsSuccess)
                    return Failure(
                        transition.ErrorCode,
                        transition.Details);
                return Commit(transition.Value!, oldSlot, newSlot, actual.Value);
            }

            TimeSpan remaining = started + HealthPolicy.TrialTimeout - now;
            await delay(
                remaining < TimeSpan.FromSeconds(1)
                    ? remaining
                    : TimeSpan.FromSeconds(1),
                cancellationToken);
        }
    }

    private OperationResult<RecoveryOutcome> Commit(
        TransactionJournal journal,
        ValidatedSlot oldSlot,
        ValidatedSlot newSlot,
        ProcessIdentity? existingTrialProcess = null)
    {
        if (journal.Phase == TransactionPhase.HealthyConfirmed)
        {
            OperationResult<ValidatedSlot> oldValidation =
                slots.Validate(journal.OldSlotId);
            OperationResult<ValidatedSlot> newValidation =
                slots.Validate(journal.NewSlotId);
            if (!oldValidation.IsSuccess || oldValidation.Value is null ||
                !newValidation.IsSuccess || newValidation.Value is null ||
                !MatchesTransaction(
                    journal, oldValidation.Value, newValidation.Value))
            {
                return Failure(
                    "CommitSlotValidationFailed",
                    !oldValidation.IsSuccess ? oldValidation.Details :
                    !newValidation.IsSuccess ? newValidation.Details :
                    "Slot metadata no longer matches the transaction.");
            }
            oldSlot = oldValidation.Value;
            newSlot = newValidation.Value;

            var expectedProcess = new ProcessIdentity(
                journal.TrialProcessPid,
                journal.TrialProcessStartTimeUtc!.Value,
                newSlot.ExecutablePath);
            OperationResult<HealthReceipt> receipt =
                HealthReceiptValidator.ReadAndValidate(
                    options.SignalsRoot,
                    journal,
                    newSlot,
                    expectedProcess,
                    utcNow());
            if (!receipt.IsSuccess)
                return Failure(
                    "HealthReceiptInvalid",
                    "HEALTHY_CONFIRMED has no matching receipt: " +
                    receipt.Details);

            OperationResult<ActivePointer?> activeRead =
                ReadPointerIfPresent(options.ActivePointerPath);
            OperationResult<ActivePointer?> previousRead =
                ReadPointerIfPresent(options.PreviousPointerPath);
            if (!activeRead.IsSuccess || !previousRead.IsSuccess)
                return Failure(
                    "PointerInvalid",
                    !activeRead.IsSuccess
                        ? activeRead.Details
                        : previousRead.Details);

            ActivePointer? activePointer = activeRead.Value;
            if (activePointer is not null &&
                activePointer.Matches(oldSlot))
            {
                bool previousAlreadyBackedUp =
                    previousRead.Value is not null &&
                    previousRead.Value.Matches(oldSlot) &&
                    previousRead.Value.Sequence == activePointer.Sequence &&
                    previousRead.Value.PreviousSlotId ==
                        activePointer.PreviousSlotId &&
                    previousRead.Value.OperationId ==
                        activePointer.OperationId;
                if (!previousAlreadyBackedUp &&
                    !PointerHistoryMatches(activePointer, previousRead.Value))
                {
                    return Failure(
                        "CommitPointerAmbiguous",
                        "The old active pointer history changed before commit.");
                }

                OperationResult<ValidatedSlot> confirmed =
                    MarkHealthConfirmed(newSlot);
                if (!confirmed.IsSuccess || confirmed.Value is null)
                    return Failure(
                        confirmed.ErrorCode,
                        confirmed.Details);
                newSlot = confirmed.Value;

                if (activePointer.Sequence == long.MaxValue)
                    return Failure(
                        "PointerSequenceOverflow",
                        "Active pointer sequence cannot be advanced safely.");

                if (!previousAlreadyBackedUp)
                {
                    OperationResult<bool> backup =
                        AtomicFileWriter.WriteJson(
                            options.PreviousPointerPath,
                            activePointer);
                    if (!backup.IsSuccess)
                        return Failure(backup.ErrorCode, backup.Details);
                }

                ActivePointer newPointer = MakePointerForSlot(
                    newSlot,
                    oldSlot.Metadata.SlotId,
                    activePointer.Sequence + 1);
                OperationResult<bool> active =
                    AtomicFileWriter.WriteJson(
                        options.ActivePointerPath,
                        newPointer);
                if (!active.IsSuccess)
                    return Failure(active.ErrorCode, active.Details);
            }
            else if (activePointer is null ||
                     !activePointer.Matches(newSlot) ||
                     previousRead.Value is null ||
                     !previousRead.Value.Matches(oldSlot) ||
                     !string.Equals(
                         activePointer.PreviousSlotId,
                         oldSlot.Metadata.SlotId,
                         StringComparison.OrdinalIgnoreCase))
            {
                return Failure(
                    "CommitPointerAmbiguous",
                    "Pointers do not match either the pre-commit or committed state.");
            }

            OperationResult<ActivePointer?> committedActive =
                ReadPointerIfPresent(options.ActivePointerPath);
            OperationResult<ActivePointer?> committedPrevious =
                ReadPointerIfPresent(options.PreviousPointerPath);
            if (!committedActive.IsSuccess ||
                !committedPrevious.IsSuccess ||
                committedActive.Value is null ||
                !committedActive.Value.Matches(newSlot) ||
                !string.Equals(
                    committedActive.Value.PreviousSlotId,
                    oldSlot.Metadata.SlotId,
                    StringComparison.OrdinalIgnoreCase) ||
                committedPrevious.Value is null ||
                !committedPrevious.Value.Matches(oldSlot))
            {
                return Failure(
                    "CommitPointerValidationFailed",
                    "Active and previous pointers failed final validation.");
            }

            OperationResult<TransactionJournal?> committed =
                Transition(journal, TransactionPhase.Committed);
            if (!committed.IsSuccess || committed.Value is null)
                return Failure(committed.ErrorCode, committed.Details);
            journal = committed.Value;
            logger?.Info(
                $"Transaction phase=COMMITTED; operationId={journal.OperationId}; " +
                $"oldSlot={oldSlot.Metadata.SlotId}; " +
                $"newSlot={newSlot.Metadata.SlotId}.");
        }
        else if (journal.Phase != TransactionPhase.Committed)
        {
            return Failure(
                "InvalidCommitPhase",
                "Only HEALTHY_CONFIRMED or COMMITTED may finish commit.");
        }

        OperationResult<ActivePointer?> finalActive =
            ReadPointerIfPresent(options.ActivePointerPath);
        OperationResult<ActivePointer?> finalPrevious =
            ReadPointerIfPresent(options.PreviousPointerPath);
        if (!finalActive.IsSuccess || !finalPrevious.IsSuccess)
            return Failure(
                "PointerInvalid",
                !finalActive.IsSuccess
                    ? finalActive.Details
                    : finalPrevious.Details);
        return RecoverCommitted(
            journal,
            oldSlot,
            newSlot,
            finalActive.Value,
            finalPrevious.Value,
            existingTrialProcess);
    }

    private OperationResult<RecoveryOutcome> RecoverCommitted(
        TransactionJournal journal,
        ValidatedSlot oldSlot,
        ValidatedSlot newSlot,
        ActivePointer? active,
        ActivePointer? previous,
        ProcessIdentity? existingTrialProcess = null)
    {
        if (!newSlot.Metadata.HealthConfirmed ||
            !MatchesTransaction(journal, oldSlot, newSlot) ||
            previous is null ||
            !previous.Matches(oldSlot))
            return Failure(
                "CommittedStateInvalid",
                "COMMITTED requires validated slots and the old active backup.");

        var expectedProcess = new ProcessIdentity(
            journal.TrialProcessPid,
            journal.TrialProcessStartTimeUtc!.Value,
            newSlot.ExecutablePath);
        OperationResult<HealthReceipt> receipt =
            HealthReceiptValidator.ReadAndValidate(
                options.SignalsRoot,
                journal,
                newSlot,
                expectedProcess,
                utcNow());
        if (!receipt.IsSuccess)
            return Failure(
                "CommittedHealthReceiptInvalid",
                receipt.Details);

        if (active is not null &&
            !active.Matches(oldSlot) &&
            !active.Matches(newSlot))
            return Failure(
                "CommittedStateAmbiguous",
                "Active pointer does not identify either transaction slot.");

        if (active is null || active.Matches(oldSlot))
        {
            long sequence = active?.Sequence ?? previous.Sequence;
            if (sequence == long.MaxValue)
                return Failure(
                    "PointerSequenceOverflow",
                    "Active pointer sequence cannot be advanced safely.");
            ActivePointer newPointer = MakePointerForSlot(
                newSlot,
                oldSlot.Metadata.SlotId,
                sequence + 1);
            OperationResult<bool> writeActive =
                AtomicFileWriter.WriteJson(
                    options.ActivePointerPath,
                    newPointer);
            if (!writeActive.IsSuccess)
                return Failure(
                    writeActive.ErrorCode,
                    writeActive.Details);
        }
        else if (!string.Equals(
                     active.PreviousSlotId,
                     oldSlot.Metadata.SlotId,
                     StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                "CommittedStateAmbiguous",
                "Committed active pointer does not preserve the old slot.");
        }

        OperationResult<ValidatedSlot> activeValidation =
            ValidateActiveMatches(newSlot);
        OperationResult<ValidatedSlot> previousValidation =
            ValidatePointerPathMatches(
                options.PreviousPointerPath,
                oldSlot);
        if (!activeValidation.IsSuccess || !previousValidation.IsSuccess)
            return Failure(
                "CommittedPointerInvalid",
                !activeValidation.IsSuccess
                    ? activeValidation.Details
                    : previousValidation.Details);

        OperationResult<TransactionJournal?> cleared =
            Transition(journal, TransactionPhase.None);
        if (!cleared.IsSuccess)
            return Failure(cleared.ErrorCode, cleared.Details);
        OperationResult<bool> delete =
            AtomicFileWriter.DeleteFile(options.TransactionPath);
        return delete.IsSuccess
            ? Success(
                newSlot,
                existingTrialProcess ??
                    FindRunningTrialProcess(journal, newSlot),
                "COMMITTED transaction verified and cleared.")
            : Failure(delete.ErrorCode, delete.Details);
    }

    private OperationResult<RecoveryOutcome> RollBack(
        TransactionJournal journal,
        ValidatedSlot oldSlot,
        string decision = "Transaction rolled back.")
    {
        OperationResult<ValidatedSlot> active =
            ValidatePointerPathMatches(
                options.ActivePointerPath,
                oldSlot);
        if (!active.IsSuccess)
            return Failure(
                "RollbackActiveInvalid",
                "Rollback will not replace an unknown active pointer: " +
                active.Details);

        if (journal.Phase != TransactionPhase.RollbackRequired)
        {
            OperationResult<TransactionJournal?> required =
                Transition(journal, TransactionPhase.RollbackRequired);
            if (!required.IsSuccess || required.Value is null)
                return Failure(
                    required.ErrorCode,
                    required.Details);
            journal = required.Value;
        }

        OperationResult<ValidatedSlot> oldValidation =
            slots.Validate(oldSlot.Metadata.SlotId);
        if (!oldValidation.IsSuccess || oldValidation.Value is null)
            return Failure("OldSlotInvalid", oldValidation.Details);

        OperationResult<TransactionJournal?> rolledBack =
            Transition(journal, TransactionPhase.RolledBack);
        if (!rolledBack.IsSuccess)
            return Failure(rolledBack.ErrorCode, rolledBack.Details);
        OperationResult<bool> journalWrite =
            AtomicFileWriter.WriteJson(
                options.TransactionPath,
                rolledBack.Value!);
        if (!journalWrite.IsSuccess)
            return Failure(journalWrite.ErrorCode, journalWrite.Details);

        OperationResult<TransactionJournal?> cleared =
            Transition(rolledBack.Value!, TransactionPhase.None);
        if (!cleared.IsSuccess)
            return Failure(cleared.ErrorCode, cleared.Details);
        OperationResult<bool> delete =
            AtomicFileWriter.DeleteFile(options.TransactionPath);
        return delete.IsSuccess
            ? Success(oldValidation.Value, null, decision)
            : Failure(delete.ErrorCode, delete.Details);
    }

    private OperationResult<RecoveryOutcome> RollBackTrialProcess(
        TransactionJournal journal,
        ValidatedSlot oldSlot,
        ProcessIdentity process,
        string decision)
    {
        OperationResult<bool> stopped =
            terminateTrialProcess(process);
        if (!stopped.IsSuccess)
            return Failure(stopped.ErrorCode, stopped.Details);
        return RollBack(journal, oldSlot, decision);
    }

    private OperationResult<RecoveryOutcome> CompleteRollback(
        TransactionJournal journal,
        ValidatedSlot oldSlot)
    {
        OperationResult<ValidatedSlot> active =
            ValidatePointerPathMatches(
                options.ActivePointerPath,
                oldSlot);
        if (!active.IsSuccess)
            return Failure(
                "RollbackActiveInvalid",
                "ROLLED_BACK has an unexpected active pointer: " +
                active.Details);

        OperationResult<TransactionJournal?> cleared =
            Transition(journal, TransactionPhase.None);
        if (!cleared.IsSuccess)
            return Failure(cleared.ErrorCode, cleared.Details);
        OperationResult<bool> delete =
            AtomicFileWriter.DeleteFile(options.TransactionPath);
        return delete.IsSuccess
            ? Success(active.Value!, null, "ROLLED_BACK transaction cleared.")
            : Failure(delete.ErrorCode, delete.Details);
    }

    private OperationResult<ValidatedSlot> ValidateActiveMatches(
        ValidatedSlot slot) =>
        ValidatePointerPathMatches(options.ActivePointerPath, slot);

    private OperationResult<ValidatedSlot> ValidatePointerPathMatches(
        string path,
        ValidatedSlot slot)
    {
        OperationResult<ActivePointer?> pointer = ReadPointerIfPresent(path);
        return pointer.IsSuccess &&
               pointer.Value is not null &&
               pointer.Value.Matches(slot)
            ? OperationResult<ValidatedSlot>.Success(slot)
            : OperationResult<ValidatedSlot>.Failure(
                "PointerMismatch",
                pointer.IsSuccess
                    ? "Active pointer does not match expected slot."
                    : pointer.Details);
    }

    private OperationResult<ActivePointer?> ReadPointerIfPresent(string path)
    {
        if (!File.Exists(path))
            return OperationResult<ActivePointer?>.Success(null);
        OperationResult<ActivePointer> result =
            AtomicFileWriter.ReadJson<ActivePointer>(path);
        if (!result.IsSuccess || result.Value is null)
            return OperationResult<ActivePointer?>.Failure(
                "PointerReadFailed", result.Details);
        OperationResult<LEADKhongPhanh.Update.UpdateVersion> validation =
            result.Value.Validate(options.InstallId);
        if (!validation.IsSuccess)
            return OperationResult<ActivePointer?>.Failure(
                validation.ErrorCode, validation.Details);
        OperationResult<ValidatedSlot> slot =
            ValidatePointerSlot(result.Value);
        if (!slot.IsSuccess)
            return OperationResult<ActivePointer?>.Failure(
                slot.ErrorCode, slot.Details);
        if (result.Value.PreviousSlotId is not null)
        {
            OperationResult<ValidatedSlot> previousSlot =
                slots.Validate(result.Value.PreviousSlotId);
            if (!previousSlot.IsSuccess ||
                previousSlot.Value is null ||
                previousSlot.Value.Metadata.Generation >=
                    slot.Value!.Metadata.Generation)
            {
                return OperationResult<ActivePointer?>.Failure(
                    "PreviousSlotInvalid",
                    previousSlot.IsSuccess
                        ? "Pointer slot generations are ambiguous."
                        : previousSlot.Details);
            }
        }
        return OperationResult<ActivePointer?>.Success(result.Value);
    }

    private OperationResult<ActivePointer?> ReadPointerLenient(string path)
    {
        OperationResult<ActivePointer?> result = ReadPointerIfPresent(path);
        return result.IsSuccess
            ? result
            : OperationResult<ActivePointer?>.Success(null);
    }

    private OperationResult<ValidatedSlot> ValidatePointerSlot(
        ActivePointer pointer)
    {
        OperationResult<ValidatedSlot> slot = slots.Validate(pointer.ActiveSlotId);
        return slot.IsSuccess &&
               slot.Value is not null &&
               pointer.Matches(slot.Value)
            ? slot
            : OperationResult<ValidatedSlot>.Failure(
                slot.IsSuccess
                    ? "PointerSlotMismatch"
                    : slot.ErrorCode,
                slot.IsSuccess
                    ? "Pointer metadata does not match referenced slot."
                    : slot.Details);
    }

    private OperationResult<ValidatedSlot> MarkHealthConfirmed(
        ValidatedSlot slot)
    {
        if (slot.Metadata.HealthConfirmed)
            return OperationResult<ValidatedSlot>.Success(slot);

        string metadataPath = Path.Combine(slot.SlotRoot, "slot.json");
        OperationResult<bool> write = AtomicFileWriter.WriteJson(
            metadataPath,
            slot.Metadata with { HealthConfirmed = true });
        if (!write.IsSuccess)
            return OperationResult<ValidatedSlot>.Failure(
                    write.ErrorCode, write.Details);
        return slots.Validate(slot.Metadata.SlotId);
    }

    private ProcessIdentity? FindRunningTrialProcess(
        TransactionJournal journal,
        ValidatedSlot slot)
    {
        if (journal.TrialProcessPid <= 0 ||
            journal.TrialProcessStartTimeUtc is null)
        {
            return null;
        }

        OperationResult<ProcessIdentity> actual =
            processLookup(journal.TrialProcessPid);
        if (!actual.IsSuccess || actual.Value is null)
            return null;

        var expected = new ProcessIdentity(
            journal.TrialProcessPid,
            journal.TrialProcessStartTimeUtc.Value,
            slot.ExecutablePath);
        return expected.Matches(actual.Value) ? actual.Value : null;
    }

    private bool MatchesTransaction(
        TransactionJournal journal,
        ValidatedSlot oldSlot,
        ValidatedSlot newSlot)
    {
        if (newSlot.Metadata.Generation != journal.Generation ||
            newSlot.Metadata.OperationId != journal.OperationId ||
            newSlot.Metadata.Version != journal.NewVersion ||
            oldSlot.Metadata.Version != journal.OldVersion ||
            !oldSlot.Metadata.HealthConfirmed ||
            oldSlot.Metadata.Generation == long.MaxValue ||
            journal.Generation != oldSlot.Metadata.Generation + 1)
        {
            return false;
        }

        return string.Equals(
            journal.PackageSha256,
            newSlot.Metadata.PackageSha256,
            StringComparison.OrdinalIgnoreCase);
    }

    private OperationResult<TransactionJournal?> Transition(
        TransactionJournal journal,
        TransactionPhase next,
        ProcessIdentity? trialProcess = null)
    {
        OperationResult<TransactionJournal?> transition =
            TransactionStateMachine.TryTransition(
                journal,
                next,
                utcNow(),
                trialProcess);
        if (!transition.IsSuccess || transition.Value is null)
            return transition;

        OperationResult<bool> write =
            AtomicFileWriter.WriteJson(
                options.TransactionPath,
                transition.Value);
        return write.IsSuccess
            ? transition
            : OperationResult<TransactionJournal?>.Failure(
                write.ErrorCode, write.Details);
    }

    private ActivePointer MakePointerForSlot(
        ValidatedSlot activeSlot,
        string? previousSlot,
        long sequence) =>
        new(
            ActivePointer.CurrentSchemaVersion,
            options.InstallId,
            sequence,
            activeSlot.Metadata.SlotId,
            previousSlot,
            activeSlot.Metadata.Version,
            activeSlot.Metadata.OperationId,
            activeSlot.Metadata.Generation);

    private static bool PointerHistoryMatches(
        ActivePointer active,
        ActivePointer? previous) =>
        active.PreviousSlotId is null
            ? previous is null
            : previous is not null &&
              previous.ActiveSlotId.Equals(
                  active.PreviousSlotId,
                  StringComparison.OrdinalIgnoreCase) &&
              previous.Sequence < active.Sequence;

    private static OperationResult<RecoveryOutcome> Success(
        ValidatedSlot slot,
        ProcessIdentity? existingProcess,
        string decision) =>
        OperationResult<RecoveryOutcome>.Success(
            new RecoveryOutcome(slot, existingProcess, decision));

    private static OperationResult<RecoveryOutcome> Failure(
        string code,
        string details) =>
        OperationResult<RecoveryOutcome>.Failure(code, details);
}

public enum TrialDecision
{
    Pending,
    Commit,
    RollBack
}
