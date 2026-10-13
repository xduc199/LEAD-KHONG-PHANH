using System.Diagnostics;

namespace LeadLauncher;

public sealed class GameLauncher
{
    public OperationResult<ProcessIdentity> Launch(ValidatedSlot slot) =>
        Launch(slot, []);

    public OperationResult<ProcessIdentity> LaunchTrial(
        ValidatedSlot slot,
        TransactionJournal transaction,
        string signalsRoot)
    {
        if (!PathSafety.IsValidOperationId(transaction.OperationId) ||
            !PathSafety.IsValidSlotId(transaction.NewSlotId) ||
            !LEADKhongPhanh.Update.UpdateVersion.TryParse(
                transaction.NewVersion,
                out _))
        {
            return OperationResult<ProcessIdentity>.Failure(
                "InvalidTrialArguments",
                "Trial identity is invalid.");
        }

        string canonicalSignals;
        try
        {
            canonicalSignals = Path.GetFullPath(signalsRoot);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            NotSupportedException or
            PathTooLongException)
        {
            return OperationResult<ProcessIdentity>.Failure(
                "InvalidSignalPath", exception.Message);
        }

        return Launch(
            slot,
            [
                "--trial-operation-id", transaction.OperationId,
                "--trial-slot-id", transaction.NewSlotId,
                "--trial-version", transaction.NewVersion,
                "--trial-signal-directory", canonicalSignals
            ]);
    }

    private static OperationResult<ProcessIdentity> Launch(
        ValidatedSlot slot,
        IReadOnlyList<string> arguments)
    {
        try
        {
            if (!File.Exists(slot.ExecutablePath) ||
                !Directory.Exists(slot.SlotRoot))
            {
                return OperationResult<ProcessIdentity>.Failure(
                    "LaunchTargetMissing", "Validated slot changed before launch.");
            }

            OperationResult<bool> linkCheck =
                PathSafety.EnsureNoReparsePoints(
                    slot.SlotRoot,
                    slot.ExecutablePath);
            if (!linkCheck.IsSuccess)
                return OperationResult<ProcessIdentity>.Failure(
                    linkCheck.ErrorCode, linkCheck.Details);

            var startInfo = new ProcessStartInfo
            {
                FileName = slot.ExecutablePath,
                WorkingDirectory = slot.SlotRoot,
                UseShellExecute = false,
                CreateNoWindow = false
            };
            foreach (string argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using Process? process = Process.Start(startInfo);
            if (process is null)
                return OperationResult<ProcessIdentity>.Failure(
                    "LaunchFailed", "Windows did not create the game process.");

            ProcessIdentity identity = new(
                process.Id,
                new DateTimeOffset(process.StartTime.ToUniversalTime()),
                Path.GetFullPath(slot.ExecutablePath));
            return OperationResult<ProcessIdentity>.Success(identity);
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
            System.ComponentModel.Win32Exception or
            IOException or
            UnauthorizedAccessException or
            ArgumentException)
        {
            return OperationResult<ProcessIdentity>.Failure(
                "LaunchFailed", exception.Message);
        }
    }
}
