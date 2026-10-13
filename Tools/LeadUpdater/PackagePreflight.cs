using System.IO.Compression;
using LeadLauncher;

namespace LeadUpdater;

public static class PackagePreflight
{
    private const int UnixFileTypeMask = 0xF000;
    private const int UnixSymbolicLinkType = 0xA000;

    public static OperationResult<PackagePreflightResult> Inspect(
        ZipArchive archive,
        string stagingRoot,
        UpdaterLimits limits)
    {
        OperationResult<bool> limitsValidation = limits.Validate();
        if (!limitsValidation.IsSuccess)
            return OperationResult<PackagePreflightResult>.Failure(
                limitsValidation.ErrorCode, limitsValidation.Details);

        try
        {
            if (archive.Entries.Count > limits.MaxArchiveEntries)
                return OperationResult<PackagePreflightResult>.Failure(
                    "TooManyEntries", "Archive has too many entries.");

            string canonicalRoot = Path.GetFullPath(stagingRoot);
            var entries = new List<PackageEntryInfo>(archive.Entries.Count);
            var nodes = new Dictionary<string, PathNode>(
                StringComparer.OrdinalIgnoreCase);
            long totalUncompressed = 0;
            long totalCompressed = 0;
            bool executableFound = false;
            bool dataDirectoryFound = false;

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                OperationResult<EntryPath> pathResult =
                    ValidateEntryPath(entry.FullName);
                if (!pathResult.IsSuccess || pathResult.Value is null)
                    return OperationResult<PackagePreflightResult>.Failure(
                        pathResult.ErrorCode, pathResult.Details);
                if (pathResult.Value.RelativePath.Equals(
                        "slot.json",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return OperationResult<PackagePreflightResult>.Failure(
                        "UnsafeArchivePath",
                        "Package cannot supply updater-owned slot metadata.");
                }

                bool directoryAttribute =
                    (entry.ExternalAttributes & (int)FileAttributes.Directory) != 0;
                bool isDirectory = pathResult.Value.IsDirectory || directoryAttribute;
                if (HasUnsupportedFileType(entry))
                    return OperationResult<PackagePreflightResult>.Failure(
                        "UnsafeArchivePath",
                        "Special, symbolic-link, and reparse-point entries are forbidden.");

                if (entry.Length < 0 || entry.CompressedLength < 0)
                    return OperationResult<PackagePreflightResult>.Failure(
                        "MalformedArchive", "Archive entry sizes are invalid.");
                if (entry.Length > limits.MaxExpandedEntryBytes)
                    return OperationResult<PackagePreflightResult>.Failure(
                        "EntryTooLarge", "An archive entry exceeds the size limit.");

                if (isDirectory && entry.Length != 0)
                    return OperationResult<PackagePreflightResult>.Failure(
                        "MalformedArchive", "Directory entries cannot contain file data.");
                if (!isDirectory && entry.Length > 0)
                {
                    if (entry.CompressedLength == 0 ||
                        (double)entry.Length / entry.CompressedLength >
                        limits.MaxCompressionRatio)
                    {
                        return OperationResult<PackagePreflightResult>.Failure(
                            "ArchiveBombDetected",
                            "An entry exceeds the configured compression ratio.");
                    }
                }

                OperationResult<long> newTotal = CheckedAddExpandedSize(
                    totalUncompressed,
                    entry.Length,
                    limits.MaxExpandedBytes);
                if (!newTotal.IsSuccess)
                    return OperationResult<PackagePreflightResult>.Failure(
                        newTotal.ErrorCode,
                        newTotal.Details);
                totalUncompressed = newTotal.Value;
                if (totalUncompressed > limits.MaxExpandedBytes)
                    return OperationResult<PackagePreflightResult>.Failure(
                        "ArchiveTooLarge",
                        "Archive expanded size exceeds the configured limit.");

                try
                {
                    totalCompressed = checked(
                        totalCompressed + entry.CompressedLength);
                }
                catch (OverflowException)
                {
                    return OperationResult<PackagePreflightResult>.Failure(
                        "MalformedArchive",
                        "Compressed size calculation overflowed.");
                }
                if (totalUncompressed > 0 &&
                    (totalCompressed == 0 ||
                     (double)totalUncompressed / totalCompressed >
                     limits.MaxCompressionRatio))
                {
                    return OperationResult<PackagePreflightResult>.Failure(
                        "ArchiveBombDetected",
                        "Archive exceeds the configured total compression ratio.");
                }

                OperationResult<string> resolved = PathSafety.ResolveRelativePath(
                    canonicalRoot,
                    pathResult.Value.RelativePath);
                if (!resolved.IsSuccess || resolved.Value is null)
                    return OperationResult<PackagePreflightResult>.Failure(
                        "UnsafeArchivePath", resolved.Details);
                if (!IsContained(canonicalRoot, resolved.Value))
                    return OperationResult<PackagePreflightResult>.Failure(
                        "UnsafeArchivePath",
                        "Archive entry escapes the staging directory.");

                OperationResult<bool> registered = RegisterPath(
                    nodes,
                    pathResult.Value.RelativePath,
                    isDirectory);
                if (!registered.IsSuccess)
                    return OperationResult<PackagePreflightResult>.Failure(
                        registered.ErrorCode, registered.Details);

                if (!isDirectory &&
                    pathResult.Value.RelativePath.Equals(
                        UpdaterOptions.ExpectedExecutableRelativePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (entry.Length == 0)
                        return OperationResult<PackagePreflightResult>.Failure(
                            "SlotValidationFailed",
                            "Contracted game executable cannot be empty.");
                    executableFound = true;
                }

                string dataPrefix =
                    UpdaterOptions.ExpectedDataDirectoryName + "/";
                if (isDirectory &&
                    pathResult.Value.RelativePath.TrimEnd('/')
                        .Equals(
                            UpdaterOptions.ExpectedDataDirectoryName,
                            StringComparison.OrdinalIgnoreCase))
                {
                    dataDirectoryFound = true;
                }
                else if (pathResult.Value.RelativePath.StartsWith(
                             dataPrefix,
                             StringComparison.OrdinalIgnoreCase))
                {
                    dataDirectoryFound = true;
                }

                entries.Add(new PackageEntryInfo(
                    entry.FullName,
                    pathResult.Value.RelativePath,
                    isDirectory,
                    entry.Length,
                    entry.CompressedLength,
                    entry));
            }

            if (!executableFound || !dataDirectoryFound)
                return OperationResult<PackagePreflightResult>.Failure(
                    "SlotValidationFailed",
                    "Archive lacks the contracted executable or Unity _Data directory.");

            return OperationResult<PackagePreflightResult>.Success(
                new PackagePreflightResult(
                    entries,
                    totalUncompressed,
                    entries.Count));
        }
        catch (InvalidDataException exception)
        {
            return OperationResult<PackagePreflightResult>.Failure(
                "MalformedArchive", exception.Message);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            NotSupportedException or
            OverflowException)
        {
            return OperationResult<PackagePreflightResult>.Failure(
                "MalformedArchive", exception.Message);
        }
    }

    public static OperationResult<int> CheckEntryCountBeforeArchiveOpen(
        Stream stream,
        int maximumEntries,
        long maximumCentralDirectoryBytes = 64L * 1024 * 1024)
    {
        if (!stream.CanRead ||
            !stream.CanSeek ||
            maximumEntries <= 0 ||
            maximumCentralDirectoryBytes <= 0)
            return OperationResult<int>.Failure(
                "MalformedArchive",
                "ZIP stream must be readable, seekable, and have valid limits.");

        try
        {
            const int EndRecordLength = 22;
            const int MaxCommentLength = ushort.MaxValue;
            if (stream.Length < EndRecordLength)
                return OperationResult<int>.Failure(
                    "MalformedArchive", "ZIP end record is missing.");

            int tailLength = (int)Math.Min(
                stream.Length,
                EndRecordLength + MaxCommentLength);
            byte[] tail = new byte[tailLength];
            stream.Position = stream.Length - tailLength;
            stream.ReadExactly(tail);

            int endOffset = -1;
            for (int index = tail.Length - EndRecordLength; index >= 0; index--)
            {
                if (ReadUInt32(tail, index) != 0x06054b50)
                    continue;
                ushort commentLength = ReadUInt16(tail, index + 20);
                if (index + EndRecordLength + commentLength == tail.Length)
                {
                    endOffset = index;
                    break;
                }
            }

            if (endOffset < 0)
                return OperationResult<int>.Failure(
                    "MalformedArchive", "ZIP end record is invalid.");

            ushort disk = ReadUInt16(tail, endOffset + 4);
            ushort centralDirectoryDisk = ReadUInt16(tail, endOffset + 6);
            ushort entriesOnDisk = ReadUInt16(tail, endOffset + 8);
            ushort totalEntries = ReadUInt16(tail, endOffset + 10);
            uint centralDirectorySize32 = ReadUInt32(tail, endOffset + 12);
            uint centralDirectoryOffset32 = ReadUInt32(tail, endOffset + 16);
            if (disk != 0 || centralDirectoryDisk != 0)
                return OperationResult<int>.Failure(
                    "MalformedArchive", "Multi-disk ZIP archives are not supported.");

            ulong entryCount;
            ulong centralDirectorySize;
            ulong centralDirectoryOffset;
            if (totalEntries != ushort.MaxValue)
            {
                if (entriesOnDisk != totalEntries)
                    return OperationResult<int>.Failure(
                        "MalformedArchive", "ZIP entry counts are inconsistent.");
                entryCount = totalEntries;
                centralDirectorySize = centralDirectorySize32;
                centralDirectoryOffset = centralDirectoryOffset32;
            }
            else
            {
                long endRecordAbsoluteOffset =
                    stream.Length - tailLength + endOffset;
                if (endRecordAbsoluteOffset < 20)
                    return OperationResult<int>.Failure(
                        "MalformedArchive", "ZIP64 locator is missing.");

                byte[] locator = new byte[20];
                stream.Position = endRecordAbsoluteOffset - locator.Length;
                stream.ReadExactly(locator);
                if (ReadUInt32(locator, 0) != 0x07064b50 ||
                    ReadUInt32(locator, 4) != 0 ||
                    ReadUInt32(locator, 16) != 1)
                {
                    return OperationResult<int>.Failure(
                        "MalformedArchive", "ZIP64 locator is invalid.");
                }

                ulong zip64Offset = ReadUInt64(locator, 8);
                if (zip64Offset > (ulong)(stream.Length - 56))
                    return OperationResult<int>.Failure(
                        "MalformedArchive", "ZIP64 end record is outside the archive.");

                byte[] zip64End = new byte[56];
                stream.Position = (long)zip64Offset;
                stream.ReadExactly(zip64End);
                if (ReadUInt32(zip64End, 0) != 0x06064b50 ||
                    ReadUInt32(zip64End, 16) != 0 ||
                    ReadUInt32(zip64End, 20) != 0)
                {
                    return OperationResult<int>.Failure(
                        "MalformedArchive", "ZIP64 end record is invalid.");
                }

                ulong onDiskCount = ReadUInt64(zip64End, 24);
                entryCount = ReadUInt64(zip64End, 32);
                centralDirectorySize = ReadUInt64(zip64End, 40);
                centralDirectoryOffset = ReadUInt64(zip64End, 48);
                if (onDiskCount != entryCount)
                    return OperationResult<int>.Failure(
                        "MalformedArchive", "ZIP64 entry counts are inconsistent.");
            }

            long endRecordOffset = stream.Length - tailLength + endOffset;
            if (centralDirectorySize > (ulong)maximumCentralDirectoryBytes ||
                centralDirectoryOffset > (ulong)stream.Length ||
                centralDirectorySize >
                (ulong)stream.Length - centralDirectoryOffset ||
                centralDirectoryOffset + centralDirectorySize >
                (ulong)endRecordOffset)
            {
                return OperationResult<int>.Failure(
                    "ArchiveTooLarge",
                    "Central directory size or bounds exceed configured limits.");
            }

            if (entryCount > (ulong)maximumEntries)
                return OperationResult<int>.Failure(
                    "TooManyEntries", "Archive has too many entries.");
            if (entryCount > int.MaxValue)
                return OperationResult<int>.Failure(
                    "TooManyEntries", "Archive entry count exceeds supported limits.");

            stream.Position = 0;
            return OperationResult<int>.Success((int)entryCount);
        }
        catch (Exception exception) when (
            exception is IOException or
            ArgumentException or
            NotSupportedException or
            OverflowException)
        {
            return OperationResult<int>.Failure(
                "MalformedArchive", exception.Message);
        }
    }

    public static OperationResult<EntryPath> ValidateEntryPath(string? name)
    {
        if (string.IsNullOrEmpty(name) || name.IndexOf('\0') >= 0 ||
            name.Any(char.IsControl))
        {
            return OperationResult<EntryPath>.Failure(
                "UnsafeArchivePath", "Archive path is empty or contains controls.");
        }

        bool trailingSeparator = name.EndsWith('/') || name.EndsWith('\\');
        string normalized = name.Replace('\\', '/');
        string relative = trailingSeparator
            ? normalized.TrimEnd('/')
            : normalized;
        if (relative.Length == 0)
            return OperationResult<EntryPath>.Failure(
                "UnsafeArchivePath", "Archive root entries are not allowed.");

        OperationResult<string> safe = PathSafety.ResolveRelativePath(
            Path.GetTempPath(),
            relative);
        if (!safe.IsSuccess)
            return OperationResult<EntryPath>.Failure(
                "UnsafeArchivePath", safe.Details);

        return OperationResult<EntryPath>.Success(
            new EntryPath(relative, trailingSeparator));
    }

    public static OperationResult<long> CheckedAddExpandedSize(
        long current,
        long entryLength,
        long maximum)
    {
        if (current < 0 || entryLength < 0 || maximum <= 0)
            return OperationResult<long>.Failure(
                "MalformedArchive", "Expanded size values are invalid.");

        try
        {
            long total = checked(current + entryLength);
            return total <= maximum
                ? OperationResult<long>.Success(total)
                : OperationResult<long>.Failure(
                    "ArchiveTooLarge",
                    "Archive expanded size exceeds the configured limit.");
        }
        catch (OverflowException)
        {
            return OperationResult<long>.Failure(
                "ArchiveTooLarge",
                "Expanded size calculation overflowed.");
        }
    }

    private static OperationResult<bool> RegisterPath(
        IDictionary<string, PathNode> nodes,
        string relativePath,
        bool isDirectory)
    {
        string[] segments = relativePath.Split('/');
        string current = string.Empty;

        for (int index = 0; index < segments.Length; index++)
        {
            current = current.Length == 0
                ? segments[index]
                : current + "/" + segments[index];
            bool isLeaf = index == segments.Length - 1;
            bool currentIsDirectory = !isLeaf || isDirectory;

            if (nodes.TryGetValue(current, out PathNode? existing))
            {
                if (existing.IsDirectory != currentIsDirectory)
                    return OperationResult<bool>.Failure(
                        "DuplicateArchivePath",
                        "Archive contains a file/directory path collision.");

                if (isLeaf && existing.IsExplicit)
                    return OperationResult<bool>.Failure(
                        "DuplicateArchivePath",
                        "Archive contains duplicate logical paths.");

                if (isLeaf)
                    nodes[current] = existing with { IsExplicit = true };
            }
            else
            {
                nodes.Add(current, new PathNode(
                    currentIsDirectory,
                    isLeaf));
            }
        }

        return OperationResult<bool>.Success(true);
    }

    private static bool HasUnsupportedFileType(ZipArchiveEntry entry)
    {
        int unixType = (entry.ExternalAttributes >> 16) & UnixFileTypeMask;
        return unixType == UnixSymbolicLinkType ||
               (unixType != 0 &&
                unixType != UnixSymbolicLinkType &&
                unixType != 0x8000 &&
                unixType != 0x4000) ||
               (entry.ExternalAttributes &
                (int)FileAttributes.ReparsePoint) != 0;
    }

    private static ushort ReadUInt16(byte[] bytes, int offset) =>
        (ushort)(bytes[offset] | (bytes[offset + 1] << 8));

    private static uint ReadUInt32(byte[] bytes, int offset) =>
        (uint)(bytes[offset] |
               (bytes[offset + 1] << 8) |
               (bytes[offset + 2] << 16) |
               (bytes[offset + 3] << 24));

    private static ulong ReadUInt64(byte[] bytes, int offset) =>
        ReadUInt32(bytes, offset) |
        ((ulong)ReadUInt32(bytes, offset + 4) << 32);

    private static bool IsContained(string root, string candidate)
    {
        string relative = Path.GetRelativePath(root, candidate);
        return relative != "." &&
               !Path.IsPathRooted(relative) &&
               relative != ".." &&
               !relative.StartsWith(
                   ".." + Path.DirectorySeparatorChar,
                   StringComparison.Ordinal) &&
               !relative.StartsWith(
                   ".." + Path.AltDirectorySeparatorChar,
                   StringComparison.Ordinal);
    }

    private sealed record PathNode(bool IsDirectory, bool IsExplicit);
}

public sealed record EntryPath(string RelativePath, bool IsDirectory);
