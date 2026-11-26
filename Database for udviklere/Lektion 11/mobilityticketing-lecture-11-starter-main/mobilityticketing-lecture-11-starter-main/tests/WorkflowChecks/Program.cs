using System.Text;
using System.Security.Cryptography;
using MobilityTicketing.Reports.Application;

var store = new Store();
var catalogue = new Catalogue();
var receipts = new Receipts();
var workflow = new ReportWorkflow(store, catalogue, receipts, new("reports", "export.jsonl"));
var bytes = Encoding.UTF8.GetBytes("one\ntwo\nthree\n");
var checks = 0;
void Check(bool success, string name) { if (!success) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
async Task Reject(Func<Task> action, string name) {
    try { await action(); } catch (InvalidOperationException) { Check(true, name); return; }
    throw new Exception("Expected rejection: " + name);
}
await workflow.StageAsync("R1", bytes);
Check(catalogue.Rows.Count == 0, "staging does not publish");
var receipt = receipts.Rows["R1"];
Check(receipt.RowCount == 3 && receipt.Sha256 == Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), "receipt contains byte measurements");
Check(await workflow.VerifyAsync("R1") == Verification.UnknownReport, "unpublished report is unknown");
await workflow.RecoverAsync("R1", null);
await workflow.RecoverAsync("R1", null);
Check(store.Uploads == 1 && catalogue.Rows.Count == 1, "recovery retries do not upload again");
Check(await workflow.VerifyAsync("R1") == Verification.Matches, "published exact version matches");
await workflow.PublishAsync("R2", Encoding.UTF8.GetBytes("corrected\n"), "R1");
Check(await workflow.VerifyAsync("R1") == Verification.Matches, "replaced key preserves original version");
receipts.Rows["R2"] = receipts.Rows["R2"] with { Sha256 = new string('0', 64) };
await Reject(() => workflow.RecoverAsync("R2", "R1"), "corrupt receipt blocks recovery");
store.FailReads = true;
Check(await workflow.VerifyAsync("R1") == Verification.ReadFailed, "object outage is not a mismatch");
store.FailReads = false;
catalogue.FailReads = true;
Check(await workflow.VerifyAsync("R1") == Verification.CatalogueReadFailed, "catalogue outage has its own result");
catalogue.FailReads = false;
await Reject(() => workflow.StageAsync("R1", bytes), "duplicate reservation prevents another upload");
Check(store.Uploads == 2, "duplicate stage did not issue PUT");
store.Versions[receipt.VersionId] = Encoding.UTF8.GetBytes("tampered\n");
Check(await workflow.VerifyAsync("R1") == Verification.Mismatch, "changed bytes are a mismatch");
store.BlankVersion = true;
await Reject(() => workflow.StageAsync("R3", bytes), "blank upload version rejected");
Check(receipts.Reserved.Contains("R3") && !receipts.Rows.ContainsKey("R3"), "ambiguous upload keeps reservation");
Console.WriteLine($"{checks} workflow checks passed.");

sealed class Store : IReportObjectStore {
    public Dictionary<string, byte[]> Versions = new();
    public int Uploads;
    public bool FailReads, BlankVersion;
    public Task<string> UploadAsync(ExportLocation location, byte[] bytes) {
        var id = (++Uploads).ToString(); Versions[id] = bytes.ToArray();
        return Task.FromResult(BlankVersion ? "" : id);
    }
    public Task<byte[]> ReadVersionAsync(ExportLocation location, string versionId) => FailReads
        ? throw new ObjectStoreException("offline", new IOException()) : Task.FromResult(Versions[versionId]);
}
sealed class Catalogue : IReportCatalogue {
    public Dictionary<string, ReportInput> Rows = new();
    public bool FailReads;
    public Task<ReportInput?> FindAsync(string id) => FailReads
        ? throw new CatalogueException("offline", new IOException()) : Task.FromResult(Rows.GetValueOrDefault(id));
    public Task SaveAsync(ReportInput input, string? supersedes) { Rows[input.ReportId] = input; return Task.CompletedTask; }
}
sealed class Receipts : IUploadReceipts {
    public HashSet<string> Reserved = new();
    public Dictionary<string, ReportInput> Rows = new();
    public void Reserve(string id) { if (!Reserved.Add(id)) throw new InvalidOperationException("reserved"); }
    public Task WriteAsync(ReportInput input) { Rows[input.ReportId] = input; return Task.CompletedTask; }
    public Task<ReportInput> ReadAsync(string id) => Task.FromResult(Rows[id]);
}
