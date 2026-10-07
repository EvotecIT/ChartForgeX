using System.Text;
using System.Text.Json;
using ApiLedger;

if (args.Length > 0 && args[0] == "--self-test") { SelfTest.Run(args.Length > 1 ? args[1] : null); return; }
if (args.Length < 3) throw new ArgumentException("Usage: ApiLedger <repository> <output-directory> <integrated-ref> [consumer-root] [external-private-consumer-map.json]");
string repository = Path.GetFullPath(args[0]);
string output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
string commonGit = Inventory.Git(repository, "rev-parse", "--path-format=absolute", "--git-common-dir").Trim();
string root = args.Length > 3 ? Path.GetFullPath(args[3]) : Environment.GetEnvironmentVariable("EVOTEC_GITHUB_ROOT") ?? Path.GetDirectoryName(Path.GetDirectoryName(commonGit))!;
var evidence = new Evidence();
foreach (string consumer in new[] { "PowerBGInfo", "ImagePlayground", "OfficeIMO" }) {
    Console.WriteLine("Indexing ChartForgeX source leads: " + consumer);
    evidence.AddRepository(consumer, Path.Combine(root, consumer));
}
if (args.Length > 4) {
    var mappings = PrivateConsumerMapping.Load(repository, args[4]);
    foreach (var mapping in mappings) {
        Console.WriteLine("Indexing anonymized ChartForgeX source leads: " + mapping.NeutralName);
        evidence.AddRepository(mapping.NeutralName, Path.GetFullPath(mapping.Path), anonymized: true);
    }
}
evidence.AddRepository("ChartForgeX-tests", repository, testsOnly: true);
string[] references = ["20e3443da9521bcd3371f68b232406596564191b", "5d2c4ada0f7b4aa1fb8704e9ba150ff3b9f0d5bb", args[2]];
var summaries = new List<object>();
var snapshots = new List<Snapshot>();
string archive = Path.Combine(root, "_worktrees", "_codex-evidence", "ChartForgeX-v2-phase01", "api-ledger");
Directory.CreateDirectory(archive);
for (int index = 0; index < references.Length; index++) {
    string label = new[] { "main", "label", "integrated" }[index];
    Console.WriteLine("Resolving pinned source symbols: " + label);
    var snapshot = Inventory.Read(repository, references[index]);
    snapshots.Add(snapshot);
    var rows = snapshot.Symbols.Select(symbol => {
        Mapping map = Ownership.For(symbol);
        return new Dictionary<string, object?> {
            ["baseline"] = label, ["source_sha"] = snapshot.Sha, ["current_assembly"] = symbol.Assembly,
            ["namespace"] = symbol.Namespace, ["declaring_type"] = symbol.Type, ["member_name"] = symbol.Name,
            ["kind"] = symbol.Kind, ["accessibility"] = symbol.Accessibility, ["documentation_id"] = symbol.DocId,
            ["signature"] = symbol.Signature, ["attributes"] = symbol.Attributes, ["constant_value"] = symbol.Constant,
            ["source_path"] = symbol.Source, ["source_line"] = symbol.Line, ["implicitly_declared"] = symbol.Implicit,
            ["referenced_cfx_types"] = symbol.References, ["proposed_owner"] = map.Owner,
            ["planned_disposition"] = map.Disposition, ["implementation_state"] = "baseline-inventory-only",
            ["migration_phase"] = map.Phase, ["replacement_plan"] = map.Replacement, ["classification_rule"] = map.Rule,
            ["consumer_text_leads"] = evidence.Find(symbol, tests: false), ["existing_test_text_leads"] = evidence.Find(symbol, tests: true),
            ["required_proof"] = map.RequiredProof, ["proof_state"] = "not-qualified-by-this-ledger"
        };
    }).ToArray();
    string csv = Path.Combine(index < 2 ? archive : output, "api-ledger-" + label + ".csv");
    LedgerCsv.Write(csv, rows);
    summaries.Add(new { baseline = label, sourceSha = snapshot.Sha, sourceCounts = snapshot.SourceCounts,
        ledgerArtifact = new { pathFromConsumerRoot = Path.GetRelativePath(root, csv).Replace('\\', '/'),
            sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(csv))), bytes = new FileInfo(csv).Length },
        exportedTypes = snapshot.Symbols.Count(item => item.Kind is "Class" or "Struct" or "Enum" or "Interface" or "Delegate"),
        totalRows = rows.Length, packages = snapshot.Symbols.GroupBy(item => item.Assembly).Select(group => new { assembly = group.Key, rows = group.Count(), types = group.Count(item => item.Kind is "Class" or "Struct" or "Enum" or "Interface" or "Delegate") }),
        conditionalSources = snapshot.ConditionalSources, classificationCounts = rows.GroupBy(row => row["planned_disposition"]).Select(group => new { disposition = group.Key, rows = group.Count() }) });
    Console.WriteLine(label + " " + snapshot.Sha + ": " + rows.Length + " exported type/member rows");
}
var current = snapshots[2].Symbols.ToDictionary(item => item.Assembly + ":" + item.DocId, StringComparer.Ordinal);
var changes = snapshots.Take(2).SelectMany((baseline, index) => baseline.Symbols.Select(symbol => {
    current.TryGetValue(symbol.Assembly + ":" + symbol.DocId, out var found);
    var review = IntegrationReview.For(symbol, found);
    return new Dictionary<string, object?> { ["baseline"] = index == 0 ? "main" : "label", ["assembly"] = symbol.Assembly,
        ["documentation_id"] = symbol.DocId, ["baseline_signature"] = symbol.Signature,
        ["integrated_signature"] = found?.Signature ?? "", ["comparison"] = found == null ? "absent" : found.Signature == symbol.Signature && found.Attributes == symbol.Attributes && found.Constant == symbol.Constant ? "unchanged" : "changed",
        ["review_disposition"] = review.Disposition, ["replacement_or_effect"] = review.Replacement, ["review_evidence"] = review.Evidence };
})).ToArray();
LedgerCsv.Write(Path.Combine(output, "api-ledger-baseline-diff.csv"), changes.Where(row => (string?)row["comparison"] != "unchanged").ToArray());
var manifest = new { schemaVersion = 1, generator = "SDK-bundled Roslyn; Git archive; no runtime package dependencies",
    analysisRuntime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    frameworkScope = "Source-symbol analysis against tool runtime BCL; no source conditionals tolerated for declaring all-TFM source equivalence. Compiled four-TFM asset verification is a separate owner gate.",
    compiledAssetVerification = "not-run-by-ledger-tool", evidenceMode = "Local tracked consumer source token co-occurrence, not semantic calls or published-package proof; optional private consumers expose neutral roles and counts only, with full evidence retained externally",
    consumerRepositories = evidence.Repositories, snapshots = summaries,
    baselineDifferences = changes.GroupBy(row => row["comparison"]).Select(group => new { comparison = group.Key, rows = group.Count() }),
    omissions = new[] { "Inherited members are represented by declaring-type/base/interface rows; accessors by property/event rows.", "Internal/private/private-protected implementation members and compiler-generated backing fields are not exported API.", "Explicit interface implementations are reachable through their exported interface member rows.", "CLI arguments and PowerShell cmdlet surfaces belong to separate consumer ledgers." } };
File.WriteAllText(Path.Combine(output, "api-ledger-manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
if (snapshots.Any(snapshot => snapshot.ConditionalSources.Length != 0)) throw new InvalidOperationException("Conditional source detected; inspect manifest and add target-specific symbol passes before declaring exhaustive framework coverage.");
