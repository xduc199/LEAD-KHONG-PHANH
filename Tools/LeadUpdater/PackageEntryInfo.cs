using System.IO.Compression;

namespace LeadUpdater;

public sealed record PackageEntryInfo(
    string ArchiveName,
    string RelativePath,
    bool IsDirectory,
    long UncompressedLength,
    long CompressedLength,
    ZipArchiveEntry SourceEntry);

public sealed record PackagePreflightResult(
    IReadOnlyList<PackageEntryInfo> Entries,
    long TotalUncompressedBytes,
    int EntryCount);
