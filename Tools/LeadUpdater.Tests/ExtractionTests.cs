using System.IO.Compression;
using LeadUpdater;
using Xunit;

namespace LeadUpdater.Tests;

public sealed class ExtractionTests
{
    [Fact]
    public async Task Valid_preflighted_package_extracts_with_expected_size()
    {
        using var workspace = new TestWorkspace();
        byte[] bytes = workspace.CreateValidPackage();
        using var memory = new MemoryStream(bytes);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
        string slotRoot = Path.Combine(workspace.SlotsRoot, TestWorkspace.CreateSlotId());
        var preflight = PackagePreflight.Inspect(
            archive,
            slotRoot,
            new UpdaterLimits());
        Assert.True(preflight.IsSuccess, preflight.Details);

        var result = await ZipExtractor.ExtractAsync(
            preflight.Value!,
            workspace.InstallationRoot,
            slotRoot,
            new UpdaterLimits());

        Assert.True(result.IsSuccess, result.Details);
        Assert.True(File.Exists(Path.Combine(
            slotRoot,
            UpdaterOptions.ExpectedExecutableRelativePath)));
        Assert.True(Directory.Exists(Path.Combine(
            slotRoot,
            UpdaterOptions.ExpectedDataDirectoryName)));
    }

    [Fact]
    public void Traversal_is_rejected_before_any_file_is_written()
    {
        using var workspace = new TestWorkspace();
        byte[] bytes = workspace.CreateValidPackage(additionalEntry: "../escape");
        using var memory = new MemoryStream(bytes);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
        string slotRoot = Path.Combine(workspace.SlotsRoot, TestWorkspace.CreateSlotId());
        var preflight = PackagePreflight.Inspect(
            archive,
            slotRoot,
            new UpdaterLimits());

        Assert.Equal("UnsafeArchivePath", preflight.ErrorCode);
        Assert.False(Directory.Exists(slotRoot));
        Assert.False(File.Exists(Path.Combine(workspace.InstallationRoot, "escape")));
    }

    [Fact]
    public async Task Extraction_rejects_destination_outside_slots()
    {
        using var workspace = new TestWorkspace();
        byte[] bytes = workspace.CreateValidPackage();
        using var memory = new MemoryStream(bytes);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
        string outside = Path.Combine(workspace.Root, "outside");
        var preflight = PackagePreflight.Inspect(
            archive,
            outside,
            new UpdaterLimits());
        Assert.True(preflight.IsSuccess, preflight.Details);

        var result = await ZipExtractor.ExtractAsync(
            preflight.Value!,
            workspace.InstallationRoot,
            outside,
            new UpdaterLimits());

        Assert.Equal("UnsafeArchivePath", result.ErrorCode);
        Assert.False(Directory.Exists(outside));
    }

    [Fact]
    public async Task Extracted_size_mismatch_returns_failure()
    {
        using var workspace = new TestWorkspace();
        byte[] bytes = workspace.CreateValidPackage();
        using var memory = new MemoryStream(bytes);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
        string slotRoot = Path.Combine(workspace.SlotsRoot, TestWorkspace.CreateSlotId());
        var preflight = PackagePreflight.Inspect(
            archive,
            slotRoot,
            new UpdaterLimits());
        Assert.True(preflight.IsSuccess, preflight.Details);

        PackageEntryInfo[] changed = preflight.Value!.Entries
            .Select(entry => entry.IsDirectory
                ? entry
                : entry with
                {
                    UncompressedLength = entry.UncompressedLength + 1
                })
            .ToArray();
        var tamperedPlan = preflight.Value with { Entries = changed };

        var result = await ZipExtractor.ExtractAsync(
            tamperedPlan,
            workspace.InstallationRoot,
            slotRoot,
            new UpdaterLimits());

        Assert.Equal("ExtractionFailed", result.ErrorCode);
    }
}
