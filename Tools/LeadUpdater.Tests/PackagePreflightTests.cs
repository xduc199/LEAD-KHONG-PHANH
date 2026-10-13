using System.IO.Compression;
using LeadUpdater;
using Xunit;

namespace LeadUpdater.Tests;

public sealed class PackagePreflightTests
{
    [Fact]
    public void Malformed_zip_is_rejected_by_archive_reader()
    {
        using var memory = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        Assert.Throws<InvalidDataException>(() =>
            new ZipArchive(memory, ZipArchiveMode.Read));
    }

    [Fact]
    public void Oversized_archive_entry_count_is_rejected()
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(
                   memory,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            TestWorkspace.AddEntry(archive, "LEADKhongPhanh.exe", "exe");
            TestWorkspace.AddEntry(
                archive,
                "LEADKhongPhanh_Data/file",
                "data");
            TestWorkspace.AddEntry(archive, "extra", "x");
        }

        memory.Position = 0;
        using var readArchive = new ZipArchive(memory, ZipArchiveMode.Read);
        var result = PackagePreflight.Inspect(
            readArchive,
            Path.GetTempPath(),
            new UpdaterLimits(MaxArchiveEntries: 2));

        Assert.Equal("TooManyEntries", result.ErrorCode);
    }

    [Fact]
    public void Entry_count_is_rejected_before_zip_entries_are_materialized()
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(
                   memory,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            TestWorkspace.AddEntry(archive, "one", "1");
            TestWorkspace.AddEntry(archive, "two", "2");
            TestWorkspace.AddEntry(archive, "three", "3");
        }

        memory.Position = 0;
        var result = PackagePreflight.CheckEntryCountBeforeArchiveOpen(
            memory,
            maximumEntries: 2);
        Assert.Equal("TooManyEntries", result.ErrorCode);
        Assert.Equal("TooManyEntries", result.ErrorCode);
    }

    [Fact]
    public void Oversized_central_directory_is_rejected_before_archive_open()
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(
                   memory,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            TestWorkspace.AddEntry(archive, "one", "1");
        }

        memory.Position = 0;
        var result = PackagePreflight.CheckEntryCountBeforeArchiveOpen(
            memory,
            maximumEntries: 10,
            maximumCentralDirectoryBytes: 1);

        Assert.Equal("ArchiveTooLarge", result.ErrorCode);
    }

    [Fact]
    public void Oversized_single_entry_is_rejected()
    {
        using var workspace = new TestWorkspace();
        byte[] bytes = workspace.CreateValidPackage(
            additionalEntry: "large.bin",
            additionalContent: new string('x', 128));
        using var memory = new MemoryStream(bytes);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);

        var result = PackagePreflight.Inspect(
            archive,
            Path.GetTempPath(),
            new UpdaterLimits(MaxExpandedEntryBytes: 100));

        Assert.Equal("EntryTooLarge", result.ErrorCode);
    }

    [Fact]
    public void Expanded_size_limit_rejects_archive()
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(
                   memory,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            TestWorkspace.AddEntry(
                archive,
                "LEADKhongPhanh.exe",
                new string('x', 60));
            TestWorkspace.AddEntry(
                archive,
                "LEADKhongPhanh_Data/file",
                new string('y', 60));
        }

        memory.Position = 0;
        using var readArchive = new ZipArchive(memory, ZipArchiveMode.Read);

        var result = PackagePreflight.Inspect(
            readArchive,
            Path.GetTempPath(),
            new UpdaterLimits(
                MaxExpandedBytes: 100,
                MaxExpandedEntryBytes: 70));

        Assert.Equal("ArchiveTooLarge", result.ErrorCode);
    }

    [Fact]
    public void Compression_bomb_ratio_is_rejected()
    {
        using var workspace = new TestWorkspace();
        byte[] bytes = workspace.CreateValidPackage(
            additionalEntry: "zeros.bin",
            additionalContent: new string('0', 100_000));
        using var memory = new MemoryStream(bytes);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);

        var result = PackagePreflight.Inspect(
            archive,
            Path.GetTempPath(),
            new UpdaterLimits(MaxCompressionRatio: 2));

        Assert.Equal("ArchiveBombDetected", result.ErrorCode);
    }

    [Fact]
    public void Expanded_size_addition_overflow_is_rejected()
    {
        var result = PackagePreflight.CheckedAddExpandedSize(
            long.MaxValue,
            1,
            long.MaxValue);

        Assert.Equal("ArchiveTooLarge", result.ErrorCode);
    }

    [Fact]
    public void Expanded_size_above_limit_is_rejected_without_overflow()
    {
        var result = PackagePreflight.CheckedAddExpandedSize(8, 3, 10);

        Assert.Equal("ArchiveTooLarge", result.ErrorCode);
    }

    [Fact]
    public void Missing_contracted_executable_is_rejected()
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(
                   memory,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            TestWorkspace.AddEntry(
                archive,
                "LEADKhongPhanh_Data/file",
                "data");
        }

        memory.Position = 0;
        using var readArchive = new ZipArchive(memory, ZipArchiveMode.Read);
        var result = PackagePreflight.Inspect(
            readArchive,
            Path.GetTempPath(),
            new UpdaterLimits());

        Assert.Equal("SlotValidationFailed", result.ErrorCode);
    }

    [Fact]
    public void Missing_contracted_data_directory_is_rejected()
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(
                   memory,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            TestWorkspace.AddEntry(archive, "LEADKhongPhanh.exe", "exe");
        }

        memory.Position = 0;
        using var readArchive = new ZipArchive(memory, ZipArchiveMode.Read);
        var result = PackagePreflight.Inspect(
            readArchive,
            Path.GetTempPath(),
            new UpdaterLimits());

        Assert.Equal("SlotValidationFailed", result.ErrorCode);
    }

    [Fact]
    public async Task Malformed_package_is_rejected_before_transaction_creation()
    {
        using var workspace = new TestWorkspace();
        byte[] malformed = [1, 2, 3, 4, 5];
        UpdaterOptions options = workspace.Options(malformed);

        var result = await new UpdateInstaller(
                stateRootOverride: workspace.StateRoot)
            .RunAsync(options);

        Assert.Equal("MalformedArchive", result.ErrorCode);
        Assert.False(File.Exists(workspace.TransactionPath));
    }
}
