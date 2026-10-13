namespace LeadLauncher;

public static class TransactionStateMachine
{
    public static OperationResult<TransactionJournal> Start(
        TransactionJournal draft,
        DateTimeOffset nowUtc)
    {
        if (draft.Phase != TransactionPhase.None ||
            !CanTransition(TransactionPhase.None, TransactionPhase.Preparing))
        {
            return OperationResult<TransactionJournal>.Failure(
                "InvalidInitialPhase", "A new journal must start at NONE.");
        }

        TransactionJournal preparing =
            draft.WithPhase(TransactionPhase.Preparing, nowUtc);
        OperationResult<bool> validation =
            preparing.Validate(draft.InstallId);
        return validation.IsSuccess
            ? OperationResult<TransactionJournal>.Success(preparing)
            : OperationResult<TransactionJournal>.Failure(
                validation.ErrorCode, validation.Details);
    }

    public static bool CanTransition(
        TransactionPhase current,
        TransactionPhase next) =>
        (current, next) switch
        {
            (TransactionPhase.None, TransactionPhase.Preparing) => true,
            (TransactionPhase.Preparing, TransactionPhase.Prepared) => true,
            (TransactionPhase.Preparing, TransactionPhase.RollbackRequired) => true,
            (TransactionPhase.Prepared, TransactionPhase.Trial) => true,
            (TransactionPhase.Prepared, TransactionPhase.RollbackRequired) => true,
            (TransactionPhase.Trial, TransactionPhase.HealthyConfirmed) => true,
            (TransactionPhase.Trial, TransactionPhase.RollbackRequired) => true,
            (TransactionPhase.HealthyConfirmed, TransactionPhase.Committed) => true,
            (TransactionPhase.Committed, TransactionPhase.None) => true,
            (TransactionPhase.RollbackRequired, TransactionPhase.RolledBack) => true,
            (TransactionPhase.RolledBack, TransactionPhase.None) => true,
            _ => false
        };

    public static OperationResult<TransactionJournal?> TryTransition(
        TransactionJournal? journal,
        TransactionPhase next,
        DateTimeOffset nowUtc,
        ProcessIdentity? trialProcess = null)
    {
        TransactionPhase current = journal?.Phase ?? TransactionPhase.None;
        if (!CanTransition(current, next))
        {
            return OperationResult<TransactionJournal?>.Failure(
                "InvalidTransition",
                $"Transition {current} -> {next} is not allowed.");
        }

        if (next == TransactionPhase.None)
            return OperationResult<TransactionJournal?>.Success(null);
        if (journal is null)
        {
            return OperationResult<TransactionJournal?>.Failure(
                "MissingJournal", "Cannot create a transaction without identity.");
        }

        TransactionJournal updated = journal.WithPhase(next, nowUtc);
        if (next == TransactionPhase.Trial)
        {
            if (trialProcess is null || trialProcess.Pid <= 0 ||
                trialProcess.ProcessStartTimeUtc == default ||
                string.IsNullOrWhiteSpace(trialProcess.ExecutablePath))
            {
                return OperationResult<TransactionJournal?>.Failure(
                    "MissingTrialProcess",
                    "TRIAL requires the identity of the launched game process.");
            }

            updated = updated with
            {
                TrialStartedAtUtc = nowUtc,
                TrialProcessPid = trialProcess.Pid,
                TrialProcessStartTimeUtc =
                    trialProcess.ProcessStartTimeUtc
            };
        }

        return OperationResult<TransactionJournal?>.Success(updated);
    }
}
