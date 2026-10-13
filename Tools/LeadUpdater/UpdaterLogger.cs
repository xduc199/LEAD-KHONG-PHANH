namespace LeadUpdater;

public interface IUpdaterLogger
{
    void Info(string message);
    void Error(string message);
}

public sealed class UpdaterLogger : IUpdaterLogger, IDisposable
{
    private readonly object sync = new();
    private readonly StreamWriter writer;

    public UpdaterLogger(string logsRoot)
    {
        Directory.CreateDirectory(logsRoot);
        string path = Path.Combine(
            logsRoot,
            "updater-" + DateTime.UtcNow.ToString("yyyyMMdd") + ".log");
        if (File.Exists(path) &&
            (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException("Updater log cannot be a reparse point.");
        }
        writer = new StreamWriter(new FileStream(
            path,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            4096,
            FileOptions.WriteThrough));
    }

    public void Info(string message) => Write("INFO", message);

    public void Error(string message) => Write("ERROR", message);

    public void Dispose() => writer.Dispose();

    private void Write(string level, string message)
    {
        string line = $"{DateTimeOffset.UtcNow:O} [{level}] {message}";
        lock (sync)
        {
            writer.WriteLine(line);
            writer.Flush();
        }
    }
}
