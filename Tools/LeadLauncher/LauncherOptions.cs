namespace LeadLauncher;

public sealed record LauncherOptions(
    string InstallationRoot,
    string StateRoot,
    string InstallId,
    string StableLauncherPath,
    string StablePublisher = InstallIdentity.StablePublisher)
{
    public string SlotsRoot => Path.Combine(InstallationRoot, "Slots");
    public string ActivePointerPath => Path.Combine(StateRoot, "active.json");
    public string PreviousPointerPath =>
        Path.Combine(StateRoot, "active.previous.json");
    public string TransactionPath =>
        Path.Combine(StateRoot, "transaction.json");
    public string InstallRequestPath =>
        Path.Combine(
            InstallationRoot,
            "Update",
            "Temp",
            "install-request.json");
    public string SignalsRoot => Path.Combine(StateRoot, "Signals");
    public string LogsRoot => Path.Combine(StateRoot, "Logs");
}
