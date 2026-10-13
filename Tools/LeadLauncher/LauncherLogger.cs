namespace LeadLauncher;

public sealed class LauncherLogger
{
    private readonly string logPath;
    private readonly object sync = new();

    public LauncherLogger(string logsRoot)
    {
        Directory.CreateDirectory(logsRoot);
        OperationResult<bool> pathCheck =
            PathSafety.EnsureNoReparsePoints(logsRoot, logsRoot);
        if (!pathCheck.IsSuccess)
            throw new IOException(pathCheck.Details);

        DateTime cutoffUtc = DateTime.UtcNow.AddDays(-30);
        foreach (string oldLog in Directory.EnumerateFiles(
                     logsRoot,
                     "launcher-*.log",
                     SearchOption.TopDirectoryOnly))
        {
            if (File.GetLastWriteTimeUtc(oldLog) < cutoffUtc)
                File.Delete(oldLog);
        }

        logPath = Path.Combine(
            logsRoot,
            "launcher-" + DateTime.UtcNow.ToString("yyyyMMdd") + ".log");
    }

    public void Info(string message) => Write("INFO", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        string safeMessage = message
            .Replace('\r', ' ')
            .Replace('\n', ' ');
        string line = $"{DateTime.UtcNow:O} [{level}] {safeMessage}";
        lock (sync)
            File.AppendAllText(logPath, line + Environment.NewLine);
        Console.WriteLine(line);
    }
}
