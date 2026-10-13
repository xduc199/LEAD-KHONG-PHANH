using System.IO.Compression;
using LeadLauncher;

namespace LeadUpdater;

public static class ZipExtractor
{
    private const int BufferSize = 128 * 1024;

    public static async Task<OperationResult<bool>> ExtractAsync(
        PackagePreflightResult preflight,
        string installationRoot,
        string slotRoot,
        UpdaterLimits limits,
        Action<int, long>? progress = null,
        CancellationToken cancellationToken = default)
    {
        OperationResult<bool> limitsValidation = limits.Validate();
        if (!limitsValidation.IsSuccess)
            return OperationResult<bool>.Failure(
                limitsValidation.ErrorCode, limitsValidation.Details);

        try
        {
            string installRoot = Path.GetFullPath(installationRoot);
            string canonicalSlotRoot = Path.GetFullPath(slotRoot);
            OperationResult<bool> slotContainment =
                EnsureContained(installRoot, canonicalSlotRoot);
            if (!slotContainment.IsSuccess)
                return slotContainment;

            Directory.CreateDirectory(canonicalSlotRoot);
            OperationResult<bool> rootLinks =
                PathSafety.EnsureNoReparsePoints(
                    installRoot,
                    canonicalSlotRoot);
            if (!rootLinks.IsSuccess)
                return OperationResult<bool>.Failure(
                    "ExtractionFailed", rootLinks.Details);

            long totalWritten = 0;
            byte[] buffer = new byte[BufferSize];
            int completedFiles = 0;
            foreach (PackageEntryInfo item in preflight.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                OperationResult<string> resolved = PathSafety.ResolveRelativePath(
                    canonicalSlotRoot,
                    item.RelativePath);
                if (!resolved.IsSuccess || resolved.Value is null)
                    return OperationResult<bool>.Failure(
                        "UnsafeArchivePath", resolved.Details);

                OperationResult<bool> contained =
                    EnsureContained(canonicalSlotRoot, resolved.Value);
                if (!contained.IsSuccess)
                    return contained;

                if (item.IsDirectory)
                {
                    Directory.CreateDirectory(resolved.Value);
                    OperationResult<bool> directoryLinks =
                        PathSafety.EnsureNoReparsePoints(
                            canonicalSlotRoot,
                            resolved.Value);
                    if (!directoryLinks.IsSuccess)
                        return OperationResult<bool>.Failure(
                            "ExtractionFailed", directoryLinks.Details);
                    continue;
                }

                string? parent = Path.GetDirectoryName(resolved.Value);
                if (string.IsNullOrEmpty(parent))
                    return OperationResult<bool>.Failure(
                        "UnsafeArchivePath", "Archive file has no parent directory.");
                Directory.CreateDirectory(parent);

                OperationResult<bool> parentLinks =
                    PathSafety.EnsureNoReparsePoints(
                        canonicalSlotRoot,
                        parent);
                if (!parentLinks.IsSuccess)
                    return OperationResult<bool>.Failure(
                        "ExtractionFailed", parentLinks.Details);

                await using Stream input = item.SourceEntry.Open();
                await using var output = new FileStream(
                    resolved.Value,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    BufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                long entryWritten = 0;
                while (true)
                {
                    int read = await input.ReadAsync(
                        buffer.AsMemory(),
                        cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                        break;

                    entryWritten = checked(entryWritten + read);
                    totalWritten = checked(totalWritten + read);
                    if (entryWritten > limits.MaxExpandedEntryBytes ||
                        totalWritten > limits.MaxExpandedBytes ||
                        entryWritten > item.UncompressedLength)
                    {
                        return OperationResult<bool>.Failure(
                            "ArchiveTooLarge",
                            "Extracted data exceeded its validated size limits.");
                    }

                    await output.WriteAsync(
                        buffer.AsMemory(0, read),
                        cancellationToken).ConfigureAwait(false);
                }

                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                output.Flush(flushToDisk: true);
                if (entryWritten != item.UncompressedLength)
                    return OperationResult<bool>.Failure(
                        "ExtractionFailed",
                        "Extracted size differs from the ZIP central directory.");

                completedFiles++;
                progress?.Invoke(completedFiles, totalWritten);
            }

            if (totalWritten > limits.MaxExpandedBytes)
                return OperationResult<bool>.Failure(
                    "ArchiveTooLarge", "Extracted package exceeds its total size limit.");
            return OperationResult<bool>.Success(true);
        }
        catch (OperationCanceledException)
        {
            return OperationResult<bool>.Failure(
                "ExtractionFailed", "Extraction was cancelled.");
        }
        catch (InvalidDataException exception)
        {
            return OperationResult<bool>.Failure(
                "ExtractionFailed", exception.Message);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            NotSupportedException or
            OverflowException)
        {
            return OperationResult<bool>.Failure(
                "ExtractionFailed", exception.Message);
        }
    }

    private static OperationResult<bool> EnsureContained(
        string root,
        string candidate)
    {
        string relative;
        try
        {
            relative = Path.GetRelativePath(root, candidate);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            NotSupportedException or
            PathTooLongException)
        {
            return OperationResult<bool>.Failure(
                "UnsafeArchivePath", exception.Message);
        }

        return relative != "." &&
               !Path.IsPathRooted(relative) &&
               relative != ".." &&
               !relative.StartsWith(
                   ".." + Path.DirectorySeparatorChar,
                   StringComparison.Ordinal) &&
               !relative.StartsWith(
                   ".." + Path.AltDirectorySeparatorChar,
                   StringComparison.Ordinal)
            ? OperationResult<bool>.Success(true)
            : OperationResult<bool>.Failure(
                "UnsafeArchivePath",
                "Resolved path is outside its required directory.");
    }
}
