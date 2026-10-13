using System.Security.Cryptography;
using LeadUpdater;
using Xunit;

namespace LeadUpdater.Tests;

public sealed class HashVerificationTests
{
    [Fact]
    public async Task Correct_streaming_hash_returns_locked_package()
    {
        using var workspace = new TestWorkspace();
        byte[] content = workspace.CreateValidPackage();
        string path = workspace.WritePackage(content);
        string hash = Convert.ToHexString(SHA256.HashData(content));

        var result = await HashVerifier.OpenVerifiedAsync(
            path,
            content.LongLength,
            hash,
            new UpdaterLimits());

        Assert.True(result.IsSuccess, result.Details);
        await using VerifiedPackage package = result.Value!;
        Assert.Equal(0, package.Stream.Position);
        Assert.Equal(content.LongLength, package.Stream.Length);
    }

    [Fact]
    public async Task Incorrect_hash_is_rejected()
    {
        using var workspace = new TestWorkspace();
        byte[] content = workspace.CreateValidPackage();
        string path = workspace.WritePackage(content);

        var result = await HashVerifier.OpenVerifiedAsync(
            path,
            content.LongLength,
            new string('0', 64),
            new UpdaterLimits());

        Assert.Equal("PackageHashMismatch", result.ErrorCode);
    }

    [Fact]
    public async Task Incorrect_file_size_is_rejected()
    {
        using var workspace = new TestWorkspace();
        byte[] content = workspace.CreateValidPackage();
        string path = workspace.WritePackage(content);

        var result = await HashVerifier.OpenVerifiedAsync(
            path,
            content.LongLength + 1,
            Convert.ToHexString(SHA256.HashData(content)),
            new UpdaterLimits());

        Assert.Equal("PackageSizeMismatch", result.ErrorCode);
    }

    [Fact]
    public async Task Missing_package_is_rejected()
    {
        using var workspace = new TestWorkspace();
        string path = Path.Combine(workspace.PackageDirectory, "missing.zip");

        var result = await HashVerifier.OpenVerifiedAsync(
            path,
            1,
            new string('a', 64),
            new UpdaterLimits());

        Assert.Equal("PackageMissing", result.ErrorCode);
    }

    [Fact]
    public async Task Invalid_hash_format_is_rejected_before_reading()
    {
        using var workspace = new TestWorkspace();
        string path = workspace.WritePackage(new byte[] { 1 });

        var result = await HashVerifier.OpenVerifiedAsync(
            path,
            1,
            "bad-hash",
            new UpdaterLimits());

        Assert.Equal("InvalidPackageHash", result.ErrorCode);
    }

    [Fact]
    public async Task Oversized_package_is_rejected_before_streaming()
    {
        using var workspace = new TestWorkspace();
        byte[] content = workspace.CreateValidPackage();
        string path = workspace.WritePackage(content);

        var result = await HashVerifier.OpenVerifiedAsync(
            path,
            content.LongLength,
            Convert.ToHexString(SHA256.HashData(content)),
            new UpdaterLimits(
                MaxPackageBytes: content.LongLength - 1,
                MaxCentralDirectoryBytes: content.LongLength - 1));

        Assert.Equal("ArchiveTooLarge", result.ErrorCode);
    }
}
