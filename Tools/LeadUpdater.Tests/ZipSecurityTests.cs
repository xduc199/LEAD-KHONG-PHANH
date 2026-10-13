using System.IO.Compression;
using LeadUpdater;
using Xunit;

namespace LeadUpdater.Tests;

public sealed class ZipSecurityTests
{
    [Theory]
    [InlineData("../outside")]
    [InlineData("..\\outside")]
    [InlineData("/rooted")]
    [InlineData("\\\\server\\share\\file")]
    [InlineData("C:\\outside")]
    [InlineData("folder/file:stream")]
    [InlineData("folder/CON.txt")]
    [InlineData("folder/trailing.")]
    [InlineData("folder/trailing ")]
    [InlineData("folder/name\u0001.txt")]
    public void Entry_path_rejects_unsafe_windows_names(string path)
    {
        OperationResult<EntryPath> result =
            PackagePreflight.ValidateEntryPath(path);

        Assert.False(result.IsSuccess);
        Assert.Equal("UnsafeArchivePath", result.ErrorCode);
    }

    [Fact]
    public void Entry_path_rejects_null_and_control_characters()
    {
        Assert.False(PackagePreflight.ValidateEntryPath(null).IsSuccess);
        Assert.False(PackagePreflight.ValidateEntryPath("file\0name").IsSuccess);
        Assert.False(PackagePreflight.ValidateEntryPath("file\nname").IsSuccess);
    }

    [Fact]
    public void Valid_archive_passes_complete_preflight()
    {
        using var workspace = new TestWorkspace();
        byte[] bytes = workspace.CreateValidPackage();
        using var memory = new MemoryStream(bytes);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);

        var result = PackagePreflight.Inspect(
            archive,
            Path.Combine(workspace.SlotsRoot, TestWorkspace.CreateSlotId()),
            new UpdaterLimits());

        Assert.True(result.IsSuccess, result.Details);
        Assert.Equal(2, result.Value!.EntryCount);
    }

    [Fact]
    public void Case_insensitive_duplicate_is_rejected()
    {
        using var workspace = new TestWorkspace();
        byte[] bytes = workspace.CreateValidPackage(
            additionalEntry: "leadkhongphanh.exe");
        using var memory = new MemoryStream(bytes);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);

        var result = PackagePreflight.Inspect(
            archive,
            Path.Combine(workspace.SlotsRoot, TestWorkspace.CreateSlotId()),
            new UpdaterLimits());

        Assert.Equal("DuplicateArchivePath", result.ErrorCode);
    }

    [Fact]
    public void File_directory_collision_is_rejected()
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
            TestWorkspace.AddEntry(archive, "conflict", "file");
            TestWorkspace.AddEntry(archive, "conflict/child.txt", "child");
        }

        memory.Position = 0;
        using var readArchive = new ZipArchive(memory, ZipArchiveMode.Read);
        var result = PackagePreflight.Inspect(
            readArchive,
            Path.Combine(Path.GetTempPath(), TestWorkspace.CreateSlotId()),
            new UpdaterLimits());

        Assert.Equal("DuplicateArchivePath", result.ErrorCode);
    }

    [Fact]
    public void Duplicate_explicit_directory_is_rejected()
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
            archive.CreateEntry("folder/");
            archive.CreateEntry("FOLDER/");
        }

        memory.Position = 0;
        using var readArchive = new ZipArchive(memory, ZipArchiveMode.Read);
        var result = PackagePreflight.Inspect(
            readArchive,
            Path.GetTempPath(),
            new UpdaterLimits());

        Assert.Equal("DuplicateArchivePath", result.ErrorCode);
    }

    [Fact]
    public void Symbolic_link_entry_is_rejected()
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
            ZipArchiveEntry link = archive.CreateEntry("link");
            link.ExternalAttributes = unchecked((int)0xA0000000);
        }

        memory.Position = 0;
        using var readArchive = new ZipArchive(memory, ZipArchiveMode.Read);
        var result = PackagePreflight.Inspect(
            readArchive,
            Path.GetTempPath(),
            new UpdaterLimits());

        Assert.Equal("UnsafeArchivePath", result.ErrorCode);
    }

    [Fact]
    public void Updater_owned_slot_metadata_is_rejected()
    {
        using var workspace = new TestWorkspace();
        byte[] bytes = workspace.CreateValidPackage(
            additionalEntry: "SLOT.JSON",
            additionalContent: "{}");
        using var memory = new MemoryStream(bytes);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);

        var result = PackagePreflight.Inspect(
            archive,
            Path.GetTempPath(),
            new UpdaterLimits());

        Assert.Equal("UnsafeArchivePath", result.ErrorCode);
    }
}
