using System.Text.Json;

namespace LeadLauncher;

public sealed record ValidatedSlot(
    SlotInfo Metadata,
    string SlotRoot,
    string ExecutablePath,
    string DataDirectory);

public sealed class SlotValidator
{
    private readonly string installationRoot;
    private readonly string slotsRoot;
    private readonly string installId;

    public SlotValidator(string installationRoot, string installId)
    {
        this.installationRoot = Path.GetFullPath(installationRoot);
        slotsRoot = Path.GetFullPath(Path.Combine(this.installationRoot, "Slots"));
        this.installId = installId;
    }

    public OperationResult<ValidatedSlot> Validate(string slotId)
    {
        if (!PathSafety.IsValidSlotId(slotId))
            return OperationResult<ValidatedSlot>.Failure(
                "InvalidSlotId", "Slot id is malformed.");

        string slotRoot = Path.GetFullPath(Path.Combine(slotsRoot, slotId));
        if (!IsWithin(slotsRoot, slotRoot))
            return OperationResult<ValidatedSlot>.Failure(
                "PathEscape", "Slot directory escapes Slots.");
        if (!Directory.Exists(slotRoot))
            return OperationResult<ValidatedSlot>.Failure(
                "SlotDirectoryMissing", "Slot directory does not exist.");

        OperationResult<bool> noLinks =
            PathSafety.EnsureNoReparsePoints(installationRoot, slotRoot);
        if (!noLinks.IsSuccess)
            return OperationResult<ValidatedSlot>.Failure(
                noLinks.ErrorCode, noLinks.Details);

        string metadataPath = Path.Combine(slotRoot, "slot.json");
        OperationResult<bool> metadataLinks =
            PathSafety.EnsureNoReparsePoints(slotRoot, metadataPath);
        if (!metadataLinks.IsSuccess)
            return OperationResult<ValidatedSlot>.Failure(
                metadataLinks.ErrorCode, metadataLinks.Details);
        OperationResult<SlotInfo> metadataResult =
            AtomicFileWriter.ReadJson<SlotInfo>(metadataPath);
        if (!metadataResult.IsSuccess || metadataResult.Value is null)
            return OperationResult<ValidatedSlot>.Failure(
                "SlotMetadataInvalid", metadataResult.Details);

        SlotInfo metadata = metadataResult.Value;
        OperationResult<LEADKhongPhanh.Update.UpdateVersion> infoValidation =
            metadata.ValidateFor(installId, slotId);
        if (!infoValidation.IsSuccess)
            return OperationResult<ValidatedSlot>.Failure(
                infoValidation.ErrorCode, infoValidation.Details);

        OperationResult<string> executable = PathSafety.ResolveRelativePath(
            slotRoot,
            metadata.ExecutableRelativePath);
        if (!executable.IsSuccess || executable.Value is null)
            return OperationResult<ValidatedSlot>.Failure(
                executable.ErrorCode, executable.Details);

        OperationResult<bool> executableLinks =
            PathSafety.EnsureNoReparsePoints(slotRoot, executable.Value);
        if (!executableLinks.IsSuccess)
            return OperationResult<ValidatedSlot>.Failure(
                executableLinks.ErrorCode, executableLinks.Details);

        if (!File.Exists(executable.Value))
            return OperationResult<ValidatedSlot>.Failure(
                "ExecutableMissing", "Game executable does not exist.");
        if ((File.GetAttributes(executable.Value) & FileAttributes.Directory) != 0)
            return OperationResult<ValidatedSlot>.Failure(
                "ExecutableIsDirectory", "Game executable is a directory.");
        if (!Path.GetExtension(executable.Value).Equals(
                ".exe",
                StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<ValidatedSlot>.Failure(
                "InvalidExecutableExtension", "Game executable must be an EXE.");
        }

        string dataDirectory = Path.Combine(
            Path.GetDirectoryName(executable.Value)!,
            Path.GetFileNameWithoutExtension(executable.Value) + "_Data");
        if (!Directory.Exists(dataDirectory))
            return OperationResult<ValidatedSlot>.Failure(
                "DataDirectoryMissing", "Unity game _Data directory is missing.");

        OperationResult<bool> dataLinks =
            PathSafety.EnsureNoReparsePoints(slotRoot, dataDirectory);
        if (!dataLinks.IsSuccess)
            return OperationResult<ValidatedSlot>.Failure(
                dataLinks.ErrorCode, dataLinks.Details);

        return OperationResult<ValidatedSlot>.Success(
            new ValidatedSlot(
                metadata,
                slotRoot,
                Path.GetFullPath(executable.Value),
                Path.GetFullPath(dataDirectory)));
    }

    private static bool IsWithin(string root, string path)
    {
        string prefix = Path.EndsInDirectorySeparator(root)
            ? root
            : root + Path.DirectorySeparatorChar;
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
