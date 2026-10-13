using System.Diagnostics;

namespace LeadLauncher;

public sealed record ProcessIdentity(
    int Pid,
    DateTimeOffset ProcessStartTimeUtc,
    string ExecutablePath)
{
    public static OperationResult<ProcessIdentity> TryGet(int pid)
    {
        if (pid <= 0)
            return OperationResult<ProcessIdentity>.Failure(
                "InvalidPid", "Process id must be positive.");

        try
        {
            using Process process = Process.GetProcessById(pid);
            string? executable = process.MainModule?.FileName;
            if (process.HasExited ||
                string.IsNullOrWhiteSpace(executable))
            {
                return OperationResult<ProcessIdentity>.Failure(
                    "ProcessUnavailable", "Process is not running or has no image path.");
            }

            return OperationResult<ProcessIdentity>.Success(
                new ProcessIdentity(
                    process.Id,
                    new DateTimeOffset(process.StartTime.ToUniversalTime()),
                    Path.GetFullPath(executable)));
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            InvalidOperationException or
            System.ComponentModel.Win32Exception or
            NotSupportedException or
            UnauthorizedAccessException)
        {
            return OperationResult<ProcessIdentity>.Failure(
                "ProcessUnavailable", exception.Message);
        }
    }

    public static OperationResult<bool> TryTerminate(ProcessIdentity expected)
    {
        try
        {
            using Process process = Process.GetProcessById(expected.Pid);
            if (process.HasExited)
                return OperationResult<bool>.Success(true);

            string? imagePath = process.MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(imagePath))
                return OperationResult<bool>.Failure(
                    "ProcessIdentityUnavailable",
                    "Cannot verify the trial executable path.");

            var actual = new ProcessIdentity(
                process.Id,
                new DateTimeOffset(process.StartTime.ToUniversalTime()),
                Path.GetFullPath(imagePath));
            if (!expected.Matches(actual))
                return OperationResult<bool>.Failure(
                    "ProcessIdentityMismatch",
                    "Refusing to terminate a process with a different identity.");

            process.Kill(entireProcessTree: false);
            return process.WaitForExit(5000)
                ? OperationResult<bool>.Success(true)
                : OperationResult<bool>.Failure(
                    "ProcessStopTimeout",
                    "Verified trial process did not stop within five seconds.");
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            InvalidOperationException or
            System.ComponentModel.Win32Exception or
            NotSupportedException or
            UnauthorizedAccessException)
        {
            return OperationResult<bool>.Failure(
                "ProcessStopFailed", exception.Message);
        }
    }

    public bool Matches(ProcessIdentity actual)
    {
        if (actual.Pid != Pid ||
            actual.ProcessStartTimeUtc != ProcessStartTimeUtc)
        {
            return false;
        }

        try
        {
            return Path.GetFullPath(actual.ExecutablePath).Equals(
                Path.GetFullPath(ExecutablePath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            NotSupportedException or
            PathTooLongException)
        {
            return false;
        }
    }
}
