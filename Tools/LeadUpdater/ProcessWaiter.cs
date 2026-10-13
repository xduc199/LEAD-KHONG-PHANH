using System.Diagnostics;
using LeadLauncher;

namespace LeadUpdater;

public sealed record ProcessProbe(
    bool IsAvailable,
    bool HasExited,
    ProcessIdentity? Identity,
    string Error)
{
    public static ProcessProbe Exited() => new(true, true, null, string.Empty);
    public static ProcessProbe Running(ProcessIdentity identity) =>
        new(true, false, identity, string.Empty);
    public static ProcessProbe Failed(string error) =>
        new(false, false, null, error);
}

public sealed class ProcessWaiter
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(120);
    public static readonly TimeSpan DefaultPollInterval =
        TimeSpan.FromMilliseconds(250);

    private readonly Func<int, ProcessProbe> probe;

    public ProcessWaiter()
        : this(ProbeProcess)
    {
    }

    public ProcessWaiter(Func<int, ProcessProbe> processProbe)
    {
        probe = processProbe;
    }

    public async Task<OperationResult<bool>> WaitForExitAsync(
        ProcessIdentity expected,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        if (expected.Pid <= 0 ||
            expected.ProcessStartTimeUtc == default ||
            string.IsNullOrWhiteSpace(expected.ExecutablePath))
        {
            return OperationResult<bool>.Failure(
                "InvalidProcessIdentity", "Expected process identity is incomplete.");
        }

        TimeSpan allowed = timeout ?? DefaultTimeout;
        TimeSpan poll = pollInterval ?? DefaultPollInterval;
        if (allowed <= TimeSpan.Zero || poll <= TimeSpan.Zero)
            return OperationResult<bool>.Failure(
                "InvalidProcessIdentity", "Process wait timing must be positive.");

        Stopwatch timer = Stopwatch.StartNew();
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
                return OperationResult<bool>.Failure(
                    "ProcessStillRunning", "Process wait was cancelled.");
            ProcessProbe current = probe(expected.Pid);
            if (!current.IsAvailable)
                return OperationResult<bool>.Failure(
                    "ProcessIdentityUnavailable", current.Error);
            if (current.HasExited)
                return OperationResult<bool>.Success(true);
            if (current.Identity is null ||
                !expected.Matches(current.Identity))
            {
                return OperationResult<bool>.Failure(
                    "ProcessIdentityMismatch",
                    "Refusing to wait on a process with a different identity.");
            }

            if (timer.Elapsed >= allowed)
                return OperationResult<bool>.Failure(
                    "ProcessStillRunning",
                    "Verified game process did not exit before the timeout.");

            TimeSpan delay = allowed - timer.Elapsed;
            if (delay > poll)
                delay = poll;
            try
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return OperationResult<bool>.Failure(
                    "ProcessStillRunning", "Process wait was cancelled.");
            }
        }
    }

    private static ProcessProbe ProbeProcess(int pid)
    {
        try
        {
            using Process process = Process.GetProcessById(pid);
            if (process.HasExited)
                return ProcessProbe.Exited();

            string? imagePath = process.MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(imagePath))
                return ProcessProbe.Failed(
                    "Running process image path is unavailable.");

            return ProcessProbe.Running(new ProcessIdentity(
                process.Id,
                new DateTimeOffset(process.StartTime.ToUniversalTime()),
                Path.GetFullPath(imagePath)));
        }
        catch (ArgumentException)
        {
            return ProcessProbe.Exited();
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
            System.ComponentModel.Win32Exception or
            NotSupportedException or
            UnauthorizedAccessException or
            IOException or
            ArgumentException)
        {
            return ProcessProbe.Failed(exception.Message);
        }
    }
}
