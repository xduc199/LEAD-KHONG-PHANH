namespace LeadLauncher;

public enum TransactionPhase
{
    None,
    Preparing,
    Prepared,
    Trial,
    HealthyConfirmed,
    Committed,
    RollbackRequired,
    RolledBack
}
