using System.Text.Json;
using LEADKhongPhanh.Update;

namespace LeadLauncher;

public sealed record HealthReceipt(
    int SchemaVersion,
    string OperationId,
    string SlotId,
    string Version,
    int Pid,
    DateTimeOffset ProcessStartTimeUtc,
    string ExecutablePath,
    DateTimeOffset HealthyAtUtc)
{
    public const int CurrentSchemaVersion = 1;
}

public static class HealthPolicy
{
    public static readonly TimeSpan HealthyConfirmationWindow =
        TimeSpan.FromSeconds(30);
    public static readonly TimeSpan TrialTimeout =
        TimeSpan.FromSeconds(120);
}

public static class HealthReceiptValidator
{
    public static OperationResult<HealthReceipt> ReadAndValidate(
        string signalsRoot,
        TransactionJournal transaction,
        ValidatedSlot trialSlot,
        ProcessIdentity actualProcess,
        DateTimeOffset nowUtc)
    {
        string path = Path.Combine(
            signalsRoot,
            transaction.OperationId + ".healthy.json");
        OperationResult<HealthReceipt> read =
            AtomicFileWriter.ReadJson<HealthReceipt>(path);
        if (!read.IsSuccess || read.Value is null)
            return OperationResult<HealthReceipt>.Failure(
                "HealthReceiptInvalid", read.Details);

        HealthReceipt receipt = read.Value;
        OperationResult<bool> validation = Validate(
            receipt,
            transaction,
            trialSlot,
            actualProcess,
            nowUtc);
        return validation.IsSuccess
            ? OperationResult<HealthReceipt>.Success(receipt)
            : OperationResult<HealthReceipt>.Failure(
                validation.ErrorCode, validation.Details);
    }

    public static OperationResult<bool> Validate(
        HealthReceipt receipt,
        TransactionJournal transaction,
        ValidatedSlot trialSlot,
        ProcessIdentity actualProcess,
        DateTimeOffset nowUtc)
    {
        DateTimeOffset? trialStarted = transaction.TrialStartedAtUtc;
        if (receipt.SchemaVersion != HealthReceipt.CurrentSchemaVersion ||
            string.IsNullOrWhiteSpace(receipt.OperationId) ||
            string.IsNullOrWhiteSpace(receipt.SlotId) ||
            string.IsNullOrWhiteSpace(receipt.Version) ||
            string.IsNullOrWhiteSpace(receipt.ExecutablePath) ||
            !receipt.OperationId.Equals(
                transaction.OperationId,
                StringComparison.OrdinalIgnoreCase) ||
            !receipt.SlotId.Equals(
                transaction.NewSlotId,
                StringComparison.OrdinalIgnoreCase) ||
            !receipt.Version.Equals(
                transaction.NewVersion,
                StringComparison.Ordinal) ||
            receipt.Pid != transaction.TrialProcessPid ||
            receipt.ProcessStartTimeUtc !=
                transaction.TrialProcessStartTimeUtc ||
            receipt.Pid <= 0 ||
            receipt.ExecutablePath.Contains('\0') ||
            trialStarted is null ||
            receipt.HealthyAtUtc < trialStarted.Value ||
            receipt.HealthyAtUtc >
                trialStarted.Value +
                HealthPolicy.TrialTimeout -
                HealthPolicy.HealthyConfirmationWindow ||
            receipt.HealthyAtUtc > nowUtc ||
            !UpdateVersion.TryParse(receipt.Version, out _))
        {
            return OperationResult<bool>.Failure(
                "HealthReceiptMismatch",
                "Health receipt identity or time window is invalid.");
        }

        var expectedProcess = new ProcessIdentity(
            transaction.TrialProcessPid,
            transaction.TrialProcessStartTimeUtc!.Value,
            trialSlot.ExecutablePath);
        bool executableMatches;
        try
        {
            executableMatches =
                Path.GetFullPath(receipt.ExecutablePath).Equals(
                    Path.GetFullPath(trialSlot.ExecutablePath),
                    StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            NotSupportedException or
            PathTooLongException)
        {
            executableMatches = false;
        }

        if (!expectedProcess.Matches(actualProcess) || !executableMatches)
        {
            return OperationResult<bool>.Failure(
                "HealthProcessMismatch",
                "Health receipt process does not match the running slot executable.");
        }

        return OperationResult<bool>.Success(true);
    }
}
