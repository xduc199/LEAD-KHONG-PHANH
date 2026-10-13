namespace LeadUpdater;

public sealed record UpdaterLimits(
    long MaxPackageBytes = 4L * 1024 * 1024 * 1024,
    int MaxArchiveEntries = 50_000,
    long MaxExpandedBytes = 8L * 1024 * 1024 * 1024,
    long MaxExpandedEntryBytes = 4L * 1024 * 1024 * 1024,
    double MaxCompressionRatio = 200d,
    long MaxCentralDirectoryBytes = 64L * 1024 * 1024)
{
    public OperationResult<bool> Validate()
    {
        if (MaxPackageBytes <= 0 ||
            MaxArchiveEntries <= 0 ||
            MaxExpandedBytes <= 0 ||
            MaxExpandedEntryBytes <= 0 ||
            MaxExpandedEntryBytes > MaxExpandedBytes ||
            MaxCentralDirectoryBytes <= 0 ||
            MaxCentralDirectoryBytes > MaxPackageBytes ||
            MaxCompressionRatio <= 0d ||
            double.IsNaN(MaxCompressionRatio) ||
            double.IsInfinity(MaxCompressionRatio))
        {
            return OperationResult<bool>.Failure(
                "InvalidConfiguration", "Archive limits are invalid.");
        }

        return OperationResult<bool>.Success(true);
    }
}
