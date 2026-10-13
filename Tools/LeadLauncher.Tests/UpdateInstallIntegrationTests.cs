using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using LeadLauncher;
using Xunit;

namespace LeadLauncher.Tests;

public sealed class UpdateInstallIntegrationTests
{
    [Fact]
    public async Task LauncherUpdaterTrialAndCommit_RunInIsolatedInstallation()
    {
        using var workspace = new TestWorkspace();
        string installIdPath = Path.Combine(
            workspace.InstallationRoot,
            "install.id");
        File.WriteAllText(installIdPath, workspace.InstallId);

        string localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        OperationResult<string> resolvedState =
            InstallIdentity.ResolveStateRoot(
                localAppData,
                InstallIdentity.StablePublisher,
                workspace.InstallId);
        Assert.True(resolvedState.IsSuccess, resolvedState.Details);
        string stateRoot = resolvedState.Value!;
        string? trialReceiptPath = null;
        ProcessIdentity? oldGameIdentity = null;
        ProcessIdentity? trialGameIdentity = null;
        ProcessIdentity? launcherIdentity = null;
        Process? oldGame = null;
        Process? stableLauncher = null;

        try
        {
            Directory.CreateDirectory(stateRoot);
            Directory.CreateDirectory(Path.Combine(stateRoot, "Signals"));
            Directory.CreateDirectory(Path.Combine(stateRoot, "Logs"));

            ValidatedSlot oldSlot = CreateFixtureSlot(
                workspace,
                workspace.OldSlotId,
                "1.0.0",
                10,
                workspace.OldOperationId,
                healthConfirmed: true);
            string oldSlotDigest = DirectoryDigest(oldSlot.SlotRoot);

            ActivePointer oldPointer = workspace.Pointer(oldSlot);
            string activePath = Path.Combine(stateRoot, "active.json");
            workspace.WritePointer(activePath, oldPointer);

            CopyDirectory(
                Path.Combine(AppContext.BaseDirectory, "LauncherFixture"),
                workspace.InstallationRoot);
            string launcherPath = Path.Combine(
                workspace.InstallationRoot,
                "LEADKhongPhanh.exe");
            Assert.True(File.Exists(launcherPath));

            CopyDirectory(
                Path.Combine(AppContext.BaseDirectory, "UpdaterFixture"),
                Path.Combine(workspace.InstallationRoot, "Updater"));

            string packageDirectory = Path.Combine(
                workspace.InstallationRoot,
                "Update",
                "Temp",
                "Packages");
            Directory.CreateDirectory(packageDirectory);
            string packagePath = Path.Combine(
                packageDirectory,
                "phase4c-integration.zip");
            CreateUpdatePackage(
                Path.Combine(AppContext.BaseDirectory, "TrialFixture"),
                packagePath);
            (long packageSize, string packageHash) =
                GetPackageIdentity(packagePath);

            var oldGameStartInfo = new ProcessStartInfo
            {
                FileName = oldSlot.ExecutablePath,
                WorkingDirectory = oldSlot.SlotRoot,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            oldGameStartInfo.ArgumentList.Add("--hold-game-ms");
            oldGameStartInfo.ArgumentList.Add("1500");
            oldGame = Process.Start(oldGameStartInfo) ??
                throw new InvalidOperationException(
                    "Could not start the controlled old-slot process.");
            oldGameIdentity = GetIdentity(oldGame);

            var handoff = new UpdateInstallRequest(
                UpdateInstallRequest.CurrentSchemaVersion,
                workspace.OperationId,
                Path.GetRelativePath(
                    workspace.InstallationRoot,
                    packagePath),
                "1.1.0",
                packageHash,
                packageSize,
                oldGameIdentity.Pid,
                oldGameIdentity.ProcessStartTimeUtc.ToUniversalTime()
                    .ToString("O"));
            TestWorkspace.AssertWrite(AtomicFileWriter.WriteJson(
                Path.Combine(
                    workspace.InstallationRoot,
                    "Update",
                    "Temp",
                    "install-request.json"),
                handoff));

            trialReceiptPath = Path.Combine(
                stateRoot,
                "Signals",
                workspace.OperationId + ".healthy.json");
            var launcherStartInfo = new ProcessStartInfo
            {
                FileName = launcherPath,
                WorkingDirectory = workspace.InstallationRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            stableLauncher = Process.Start(launcherStartInfo) ??
                throw new InvalidOperationException(
                    "Could not start the stable launcher fixture.");
            launcherIdentity = GetIdentity(stableLauncher);
            Assert.True(
                stableLauncher.WaitForExit(
                    (int)TimeSpan.FromSeconds(75).TotalMilliseconds),
                "Stable launcher did not finish before its bounded test deadline.");
            string launcherError =
                await stableLauncher.StandardError.ReadToEndAsync();
            Assert.Equal(0, stableLauncher.ExitCode);
            Assert.True(
                string.IsNullOrWhiteSpace(launcherError),
                launcherError);

            OperationResult<ActivePointer> activeResult =
                AtomicFileWriter.ReadJson<ActivePointer>(activePath);
            Assert.True(activeResult.IsSuccess, activeResult.Details);
            Assert.Equal("1.1.0", activeResult.Value!.ActiveVersion);
            Assert.Equal(
                workspace.OperationId,
                activeResult.Value.OperationId);
            Assert.NotEqual(
                workspace.OldSlotId,
                activeResult.Value.ActiveSlotId);
            Assert.Equal(workspace.OldSlotId, activeResult.Value.PreviousSlotId);
            OperationResult<ValidatedSlot> committedSlot =
                new SlotValidator(
                    workspace.InstallationRoot,
                    workspace.InstallId).Validate(
                        activeResult.Value.ActiveSlotId);
            Assert.True(committedSlot.IsSuccess, committedSlot.Details);
            Assert.Equal("1.1.0", committedSlot.Value!.Metadata.Version);

            OperationResult<ActivePointer> previousResult =
                AtomicFileWriter.ReadJson<ActivePointer>(
                    Path.Combine(stateRoot, "active.previous.json"));
            Assert.True(previousResult.IsSuccess, previousResult.Details);
            Assert.True(previousResult.Value!.Matches(oldSlot));
            Assert.Equal(oldSlotDigest, DirectoryDigest(oldSlot.SlotRoot));
            Assert.False(File.Exists(
                Path.Combine(stateRoot, "transaction.json")));

            OperationResult<HealthReceipt> receipt =
                AtomicFileWriter.ReadJson<HealthReceipt>(trialReceiptPath);
            Assert.True(receipt.IsSuccess, receipt.Details);
            trialGameIdentity = new ProcessIdentity(
                receipt.Value!.Pid,
                receipt.Value.ProcessStartTimeUtc,
                receipt.Value.ExecutablePath);
            Assert.True(
                receipt.Value.HealthyAtUtc >=
                    receipt.Value.ProcessStartTimeUtc);
            Assert.True(
                DateTimeOffset.UtcNow - receipt.Value.HealthyAtUtc >=
                    HealthPolicy.HealthyConfirmationWindow);

            string newSlotRoot = Path.Combine(
                workspace.SlotsRoot,
                activeResult.Value.ActiveSlotId);
            Assert.True(Directory.Exists(newSlotRoot));
            Assert.True(File.Exists(Path.Combine(
                newSlotRoot,
                "LEADKhongPhanh.exe")));
            Assert.True(Directory.Exists(Path.Combine(
                newSlotRoot,
                "LEADKhongPhanh_Data")));
            Assert.True(AtomicFileWriter.ReadJson<SlotInfo>(
                Path.Combine(newSlotRoot, "slot.json"))
                .Value!.HealthConfirmed);
        }
        finally
        {
            if (trialGameIdentity is null &&
                trialReceiptPath is not null &&
                File.Exists(trialReceiptPath))
            {
                OperationResult<HealthReceipt> receipt =
                    AtomicFileWriter.ReadJson<HealthReceipt>(
                        trialReceiptPath);
                if (receipt.IsSuccess && receipt.Value is not null)
                {
                    trialGameIdentity = new ProcessIdentity(
                        receipt.Value.Pid,
                        receipt.Value.ProcessStartTimeUtc,
                        receipt.Value.ExecutablePath);
                }
            }
            StopVerifiedProcess(trialGameIdentity);
            StopVerifiedProcess(oldGameIdentity);
            StopVerifiedProcess(launcherIdentity);
            string[] slotExecutablePaths =
                Directory.Exists(workspace.SlotsRoot)
                    ? Directory.EnumerateDirectories(workspace.SlotsRoot)
                        .Select(path => Path.Combine(
                            path,
                            "LEADKhongPhanh.exe"))
                        .ToArray()
                    : [];
            StopProcessesAtPaths(
                [
                    .. slotExecutablePaths,
                    Path.Combine(
                        workspace.InstallationRoot,
                        "LEADKhongPhanh.exe"),
                    Path.Combine(
                        workspace.InstallationRoot,
                        "Updater",
                        "LeadUpdater.exe")
                ]);
            oldGame?.Dispose();
            stableLauncher?.Dispose();
            if (Directory.Exists(stateRoot))
                Directory.Delete(stateRoot, recursive: true);
            string? isolatedStateParent =
                Directory.GetParent(stateRoot)?.FullName;
            if (isolatedStateParent is not null &&
                Directory.Exists(isolatedStateParent) &&
                !Directory.EnumerateFileSystemEntries(isolatedStateParent)
                    .Any())
            {
                Directory.Delete(isolatedStateParent);
            }
        }
    }

    private static ValidatedSlot CreateFixtureSlot(
        TestWorkspace workspace,
        string slotId,
        string version,
        long generation,
        string operationId,
        bool healthConfirmed)
    {
        ValidatedSlot slot = workspace.CreateSlot(
            slotId,
            version,
            generation,
            operationId,
            healthConfirmed: healthConfirmed);
        CopyDirectory(
            Path.Combine(AppContext.BaseDirectory, "TrialFixture"),
            slot.SlotRoot);
        OperationResult<ValidatedSlot> validated =
            new SlotValidator(
                workspace.InstallationRoot,
                workspace.InstallId).Validate(slotId);
        Assert.True(validated.IsSuccess, validated.Details);
        return validated.Value!;
    }

    private static void CreateUpdatePackage(
        string fixtureDirectory,
        string packagePath)
    {
        using FileStream output = new(
            packagePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);
        using var archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            leaveOpen: false);
        foreach (string path in Directory.EnumerateFiles(fixtureDirectory))
        {
            ZipArchiveEntry entry = archive.CreateEntry(
                Path.GetFileName(path),
                CompressionLevel.NoCompression);
            using FileStream source = File.OpenRead(path);
            using Stream destination = entry.Open();
            source.CopyTo(destination);
        }
        archive.CreateEntry("LEADKhongPhanh_Data/");
    }

    private static (long Size, string Sha256) GetPackageIdentity(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using SHA256 sha256 = SHA256.Create();
        return (stream.Length, Convert.ToHexString(sha256.ComputeHash(stream)));
    }

    private static void CopyDirectory(string sourceRoot, string destinationRoot)
    {
        Directory.CreateDirectory(destinationRoot);
        foreach (string source in Directory.EnumerateFiles(sourceRoot))
        {
            File.Copy(
                source,
                Path.Combine(destinationRoot, Path.GetFileName(source)),
                overwrite: true);
        }
    }

    private static ProcessIdentity GetIdentity(Process process)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            process.Refresh();
            if (process.HasExited)
                throw new InvalidOperationException(
                    $"Fixture process exited before identity capture " +
                    $"(PID {process.Id}, exit code {process.ExitCode}).");

            string? executablePath = process.MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                return new ProcessIdentity(
                    process.Id,
                    new DateTimeOffset(process.StartTime.ToUniversalTime()),
                    Path.GetFullPath(executablePath));
            }
            Thread.Sleep(25);
        }

        throw new InvalidOperationException(
            $"Fixture process image was unavailable for PID {process.Id}.");
    }

    private static void StopVerifiedProcess(ProcessIdentity? identity)
    {
        if (identity is not null)
            ProcessIdentity.TryTerminate(identity);
    }

    private static void StopProcessesAtPaths(params string[] executablePaths)
    {
        var expectedPaths = executablePaths
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (Process process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.HasExited)
                        continue;

                    string? executablePath = process.MainModule?.FileName;
                    if (string.IsNullOrWhiteSpace(executablePath))
                        continue;

                    string fullPath = Path.GetFullPath(executablePath);
                    if (!expectedPaths.Contains(fullPath))
                        continue;

                    var identity = new ProcessIdentity(
                        process.Id,
                        new DateTimeOffset(process.StartTime.ToUniversalTime()),
                        fullPath);
                    OperationResult<bool> stopResult =
                        ProcessIdentity.TryTerminate(identity);
                    Assert.True(stopResult.IsSuccess, stopResult.Details);
                }
                catch (Exception exception) when (
                    exception is ArgumentException or
                    InvalidOperationException or
                    System.ComponentModel.Win32Exception or
                    NotSupportedException or
                    UnauthorizedAccessException)
                {
                    // A process can exit between enumeration and identity capture.
                }
            }
        }
    }

    private static string DirectoryDigest(string root)
    {
        using IncrementalHash hash =
            IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string file in Directory.EnumerateFiles(
                     root,
                     "*",
                     SearchOption.AllDirectories)
                 .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            byte[] pathBytes = System.Text.Encoding.UTF8.GetBytes(
                Path.GetRelativePath(root, file));
            hash.AppendData(pathBytes);
            hash.AppendData(File.ReadAllBytes(file));
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
