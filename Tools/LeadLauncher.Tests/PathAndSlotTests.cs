using LeadLauncher;
using LEADKhongPhanh.Update;
using Xunit;

namespace LeadLauncher.Tests;

public sealed class PathAndSlotTests
{
    [Theory]
    [InlineData("../outside.exe")]
    [InlineData("..\\outside.exe")]
    [InlineData("C:\\game.exe")]
    [InlineData("C:game.exe")]
    [InlineData("\\\\server\\share\\game.exe")]
    [InlineData("//server/share/game.exe")]
    [InlineData("folder/file.exe:stream")]
    [InlineData("CON.txt")]
    [InlineData("name.")]
    [InlineData("name ")]
    public void ResolveRelativePath_RejectsUnsafePaths(string path)
    {
        OperationResult<string> result =
            PathSafety.ResolveRelativePath("C:\\install\\slot", path);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ResolveRelativePath_ResolvesInsideRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        OperationResult<string> result =
            PathSafety.ResolveRelativePath(root, "Game\\LEAD.exe");

        Assert.True(result.IsSuccess);
        Assert.StartsWith(
            Path.GetFullPath(root) + Path.DirectorySeparatorChar,
            result.Value!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InstallIdentity_IsStableAcrossRepeatedResolution()
    {
        using var workspace = new TestWorkspace();

        OperationResult<string> first =
            InstallIdentity.Resolve(workspace.InstallationRoot);
        OperationResult<string> second =
            InstallIdentity.Resolve(workspace.InstallationRoot);

        Assert.True(first.IsSuccess);
        Assert.Equal(first.Value, second.Value);
        Assert.True(PathSafety.IsValidInstallId(first.Value));
    }

    [Fact]
    public void InstallIdentity_RejectsCorruptedExistingId()
    {
        using var workspace = new TestWorkspace();
        File.WriteAllText(
            Path.Combine(workspace.InstallationRoot, "install.id"),
            "not-an-id");

        OperationResult<string> result =
            InstallIdentity.Resolve(workspace.InstallationRoot);

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidInstallId", result.ErrorCode);
    }

    [Fact]
    public void StateRoot_UsesLocalApplicationDataAndStableIdentity()
    {
        OperationResult<string> result = InstallIdentity.ResolveStateRoot(
            "C:\\Users\\test\\AppData\\Local",
            InstallIdentity.StablePublisher,
            "0123456789abcdef0123456789abcdef");

        Assert.True(result.IsSuccess);
        Assert.Contains(
            InstallIdentity.StablePublisher,
            result.Value!,
            StringComparison.Ordinal);
        Assert.EndsWith(
            Path.Combine(
                "0123456789abcdef0123456789abcdef",
                "Updater"),
            result.Value!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateVersion_UsesNumericVersionOrdering()
    {
        Assert.True(UpdateVersion.TryParse("1.10.0", out var newer));
        Assert.True(UpdateVersion.TryParse("1.9.0", out var older));
        Assert.True(newer > older);
        Assert.True(UpdateVersion.TryParse("1.10.0", out var same));
        Assert.Equal(0, newer.CompareTo(same));
    }

    [Fact]
    public void SlotValidator_AcceptsCompleteSlot()
    {
        using var workspace = new TestWorkspace();
        ValidatedSlot expected = workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 1, workspace.OldOperationId);

        OperationResult<ValidatedSlot> actual =
            new SlotValidator(
                workspace.InstallationRoot,
                workspace.InstallId).Validate(workspace.OldSlotId);

        Assert.True(actual.IsSuccess, actual.Details);
        Assert.Equal(expected.ExecutablePath, actual.Value!.ExecutablePath);
    }

    [Fact]
    public void SlotValidator_RejectsMissingExecutable()
    {
        using var workspace = new TestWorkspace();
        workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 1, workspace.OldOperationId);
        File.Delete(Path.Combine(
            workspace.SlotsRoot,
            workspace.OldSlotId,
            "LEADKhongPhanh.exe"));

        OperationResult<ValidatedSlot> result =
            new SlotValidator(
                workspace.InstallationRoot,
                workspace.InstallId).Validate(workspace.OldSlotId);

        Assert.Equal("ExecutableMissing", result.ErrorCode);
    }

    [Fact]
    public void SlotValidator_RejectsMissingUnityDataDirectory()
    {
        using var workspace = new TestWorkspace();
        workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 1, workspace.OldOperationId);
        Directory.Delete(Path.Combine(
            workspace.SlotsRoot,
            workspace.OldSlotId,
            "LEADKhongPhanh_Data"));

        OperationResult<ValidatedSlot> result =
            new SlotValidator(
                workspace.InstallationRoot,
                workspace.InstallId).Validate(workspace.OldSlotId);

        Assert.Equal("DataDirectoryMissing", result.ErrorCode);
    }

    [Fact]
    public void SlotValidator_RejectsMetadataPathTraversal()
    {
        using var workspace = new TestWorkspace();
        workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 1, workspace.OldOperationId);
        var malicious = new SlotInfo(
            1,
            workspace.OldSlotId,
            workspace.InstallId,
            "1.0.0",
            "..\\outside.exe",
            1,
            workspace.OldOperationId,
            workspace.Now,
            false,
            null);
        TestWorkspace.AssertWrite(AtomicFileWriter.WriteJson(
            Path.Combine(workspace.SlotsRoot, workspace.OldSlotId, "slot.json"),
            malicious));

        OperationResult<ValidatedSlot> result =
            new SlotValidator(
                workspace.InstallationRoot,
                workspace.InstallId).Validate(workspace.OldSlotId);

        Assert.Equal("UnsafePath", result.ErrorCode);
    }

    [Fact]
    public void SlotInfo_RejectsInstallIdentityMismatch()
    {
        using var workspace = new TestWorkspace();
        SlotInfo info = new(
            1, workspace.OldSlotId, Guid.NewGuid().ToString("N"),
            "1.0.0", "LEADKhongPhanh.exe", 1,
            workspace.OldOperationId, workspace.Now, false, null);

        OperationResult<UpdateVersion> validation =
            info.ValidateFor(workspace.InstallId, workspace.OldSlotId);

        Assert.Equal("InstallIdMismatch", validation.ErrorCode);
    }

    [Fact]
    public void SlotInfo_RejectsInvalidVersionAndHash()
    {
        using var workspace = new TestWorkspace();
        SlotInfo badVersion = new(
            1, workspace.OldSlotId, workspace.InstallId,
            "1.0", "LEADKhongPhanh.exe", 1,
            workspace.OldOperationId, workspace.Now, false, null);
        SlotInfo badHash = badVersion with
        {
            Version = "1.0.0",
            PackageSha256 = "xyz"
        };

        Assert.Equal(
            "InvalidVersion",
            badVersion.ValidateFor(workspace.InstallId, workspace.OldSlotId)
                .ErrorCode);
        Assert.Equal(
            "InvalidPackageHash",
            badHash.ValidateFor(workspace.InstallId, workspace.OldSlotId)
                .ErrorCode);
    }

    [Fact]
    public void SlotValidator_RejectsSlotIdMismatch()
    {
        using var workspace = new TestWorkspace();
        workspace.CreateSlot(
            workspace.OldSlotId, "1.0.0", 1, workspace.OldOperationId);

        OperationResult<ValidatedSlot> result =
            new SlotValidator(
                workspace.InstallationRoot,
                workspace.InstallId).Validate(workspace.NewSlotId);

        Assert.False(result.IsSuccess);
    }
}
