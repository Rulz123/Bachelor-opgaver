using System.Security.Cryptography;
namespace MobilityTicketing.Reports.Application;

public sealed class ReportWorkflow(
    IReportObjectStore storage, IReportCatalogue catalogue, IUploadReceipts receipts, ExportLocation destination)
{
    public async Task<string> StageAsync(string reportId, byte[] bytes)
    {
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var rows = bytes.Count(b => b == (byte)'\n');
        receipts.Reserve(reportId);
        var version = await storage.UploadAsync(destination, bytes);
        if (string.IsNullOrWhiteSpace(version) || version.Equals("null", StringComparison.OrdinalIgnoreCase)
            || version.Equals("None", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The upload returned no usable version ID; investigate the reserved upload.");
        await receipts.WriteAsync(new ReportInput(reportId, destination.Bucket, destination.Key, version, sha, rows));
        return version;
    }

    public async Task<string> PublishAsync(string reportId, byte[] bytes, string? supersedes)
    {
        await StageAsync(reportId, bytes);
        return await RecoverAsync(reportId, supersedes);
    }

    public async Task<string> RecoverAsync(string reportId, string? supersedes)
    {
        var input = await receipts.ReadAsync(reportId);
        var result = await CheckAsync(input);
        if (result != Verification.Matches)
            throw new InvalidOperationException($"Cannot publish {reportId}: {result}.");
        await catalogue.SaveAsync(input, supersedes);
        return $"Saved {reportId}";
    }

    public async Task<Verification> VerifyAsync(string reportId)
    {
        ReportInput? input;
        try { input = await catalogue.FindAsync(reportId); }
        catch (CatalogueException) { return Verification.CatalogueReadFailed; }
        return input is null ? Verification.UnknownReport : await CheckAsync(input);
    }

    private async Task<Verification> CheckAsync(ReportInput input)
    {
        try
        {
            var bytes = await storage.ReadVersionAsync(new ExportLocation(input.Bucket, input.Key), input.VersionId);
            var sha = Convert.ToHexString(SHA256.HashData(bytes));
            return sha.Equals(input.Sha256, StringComparison.OrdinalIgnoreCase)
                && bytes.Count(b => b == (byte)'\n') == input.RowCount
                ? Verification.Matches : Verification.Mismatch;
        }
        catch (ObjectStoreException) { return Verification.ReadFailed; }
    }
}
