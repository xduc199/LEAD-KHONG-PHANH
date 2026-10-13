using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using LEADKhongPhanh.Update;
using LeadLauncher;
using LeadUpdater;
using Xunit;

namespace LeadUpdater.Tests;

internal sealed class TestWorkspace : IDisposable
{
    public string Root { get; } = Path.Combine(
        Path.GetTempPath(),
        "lead-updater-tests-" + Guid.NewGuid().ToString("N"));
    public string InstallationRoot => Path.Combine(Root, "install");
    public string SlotsRoot => Path.Combine(InstallationRoot, "Slots");
    public string StateRoot => Path.Combine(Root, "local", "Updater");
    public string PackageDirectory =>
        Path.Combine(InstallationRoot, "Update", "Temp");
    public string InstallId { get; } = Guid.NewGuid().ToString("N");
    public string ActiveSlotId { get; } = CreateSlotId();
    public string NewSlotId { get; } = CreateSlotId();
    public string ActiveOperationId { get; } = Guid.NewGuid().ToString("N");
    public string OperationId { get; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset NowUtc { get; } = DateTimeOffset.UtcNow;

    public string ActiveSlotRoot =>
        Path.Combine(SlotsRoot, ActiveSlotId);
    public string ActiveExecutable =>
        Path.Combine(ActiveSlotRoot, UpdaterOptions.ExpectedExecutableRelativePath);
    public string ActivePointerPath => Path.Combine(StateRoot, "active.json");
    public string TransactionPath => Path.Combine(StateRoot, "transaction.json");

    public TestWorkspace()
    {
        Directory.CreateDirectory(SlotsRoot);
        Directory.CreateDirectory(StateRoot);
        Directory.CreateDirectory(Path.Combine(StateRoot, "Logs"));
        Directory.CreateDirectory(Path.Combine(StateRoot, "Signals"));
        Directory.CreateDirectory(PackageDirectory);
        File.WriteAllText(Path.Combine(InstallationRoot, "install.id"), InstallId);

        Directory.CreateDirectory(ActiveSlotRoot);
        File.WriteAllText(ActiveExecutable, "active-game-executable");
        Directory.CreateDirectory(Path.Combine(
            ActiveSlotRoot,
            UpdaterOptions.ExpectedDataDirectoryName));
        File.WriteAllText(
            Path.Combine(
                ActiveSlotRoot,
                UpdaterOptions.ExpectedDataDirectoryName,
                "globalgamemanagers"),
            "active-game-data");

        var activeMetadata = new SlotInfo(
            SlotInfo.CurrentSchemaVersion,
            ActiveSlotId,
            InstallId,
            "1.0.0",
            UpdaterOptions.ExpectedExecutableRelativePath,
            3,
            ActiveOperationId,
            NowUtc,
            true,
            new string('a', 64));
        WriteJson(Path.Combine(ActiveSlotRoot, "slot.json"), activeMetadata);
        var pointer = new ActivePointer(
            ActivePointer.CurrentSchemaVersion,
            InstallId,
            7,
            ActiveSlotId,
            null,
            "1.0.0",
            ActiveOperationId,
            3);
        WriteJson(ActivePointerPath, pointer);
    }

    public UpdaterOptions Options(
        byte[] package,
        string? version = "1.1.0",
        string? sha256 = null,
        long? fileSize = null,
        string? packagePath = null,
        string? operationId = null,
        int? gamePid = null,
        DateTimeOffset? gameStart = null)
    {
        string path = packagePath ?? WritePackage(package);
        return new UpdaterOptions(
            InstallationRoot,
            path,
            operationId ?? OperationId,
            version ?? "1.1.0",
            sha256 ?? Convert.ToHexString(SHA256.HashData(package)),
            fileSize ?? package.LongLength,
            Environment.ProcessId,
            NowUtc.AddMinutes(-1),
            gamePid,
            gameStart);
    }

    public string WritePackage(byte[] bytes, string name = "update.zip")
    {
        string path = Path.Combine(PackageDirectory, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    public byte[] CreateValidPackage(
        string executablePath = "LEADKhongPhanh.exe",
        bool includeData = true,
        string? additionalEntry = null,
        string additionalContent = "extra")
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(
                   memory,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            AddEntry(archive, executablePath, "new executable");
            if (includeData)
                AddEntry(
                    archive,
                    "LEADKhongPhanh_Data/globalgamemanagers",
                    "new game data");
            if (additionalEntry is not null)
                AddEntry(archive, additionalEntry, additionalContent);
        }

        return memory.ToArray();
    }

    public static void AddEntry(
        ZipArchive archive,
        string name,
        string content,
        CompressionLevel level = CompressionLevel.Optimal)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name, level);
        using Stream stream = entry.Open();
        byte[] data = Encoding.UTF8.GetBytes(content);
        stream.Write(data);
    }

    public ActivePointer ReadPointer()
    {
        var result = AtomicFileWriter.ReadJson<ActivePointer>(ActivePointerPath);
        Assert.True(result.IsSuccess, result.Details);
        return result.Value!;
    }

    public TransactionJournal ReadJournal()
    {
        var result = AtomicFileWriter.ReadJson<TransactionJournal>(TransactionPath);
        Assert.True(result.IsSuccess, result.Details);
        return result.Value!;
    }

    public Dictionary<string, string> SnapshotActiveSlot()
    {
        return Directory.EnumerateFiles(
                ActiveSlotRoot,
                "*",
                SearchOption.AllDirectories)
            .OrderBy(path => Path.GetRelativePath(ActiveSlotRoot, path))
            .ToDictionary(
                path => Path.GetRelativePath(ActiveSlotRoot, path),
                path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),
                StringComparer.OrdinalIgnoreCase);
    }

    public static string CreateSlotId() =>
        "slot-" + Guid.NewGuid().ToString("N");

    private static void WriteJson<T>(string path, T value)
    {
        var result = AtomicFileWriter.WriteJson(path, value);
        Assert.True(result.IsSuccess, result.Details);
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
            Directory.Delete(Root, recursive: true);
    }
}
