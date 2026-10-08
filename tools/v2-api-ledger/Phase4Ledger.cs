using System.Text;
using System.Text.Json;

namespace ApiLedger;

/// <summary>Records assembly/type closure separately from the original planning inventory.</summary>
internal static class Phase4Ledger {
    internal static void Run(string[] args) {
        if (args.Length != 4) throw new ArgumentException("Usage: ApiLedger --phase4 <repository> <output-directory> <candidate-ref>");
        var baseline = Inventory.Read(args[1], "9cd02a52af97426eab4bf64b9775ce93057e5986");
        var current = Inventory.Read(args[1], args[3]);
        Directory.CreateDirectory(args[2]);
        var rows = current.Symbols.Select(symbol => {
            var mapping = Ownership.For(symbol);
            return new Dictionary<string, object?> {
                ["source_sha"] = current.Sha, ["assembly"] = symbol.Assembly, ["documentation_id"] = symbol.DocId,
                ["signature"] = symbol.Signature, ["source_path"] = symbol.Source, ["source_line"] = symbol.Line,
                ["referenced_cfx_types"] = symbol.References, ["owner"] = mapping.Owner,
                ["implementation_state"] = "source-inventory", ["proof_state"] = "package-execution-recorded-separately"
            };
        }).ToArray();
        LedgerCsv.Write(Path.Combine(args[2], "api-ledger-phase4.csv"), rows);
        var byId = current.Symbols.GroupBy(symbol => symbol.DocId).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var changes = baseline.Symbols.Select(symbol => {
            var found = byId.TryGetValue(symbol.DocId, out var matches) ? matches.SingleOrDefault(item => item.Assembly == symbol.Assembly) ?? matches.SingleOrDefault() : null;
            var fate = found == null ? "removed-or-replaced" : found.Assembly != symbol.Assembly ? "assembly-move" : found.Signature != symbol.Signature ? "signature-change" : "retained";
            return new Dictionary<string, object?> {
                ["baseline_sha"] = baseline.Sha, ["candidate_sha"] = current.Sha, ["documentation_id"] = symbol.DocId,
                ["old_assembly"] = symbol.Assembly, ["new_assembly"] = found?.Assembly ?? "",
                ["old_signature"] = symbol.Signature, ["new_signature"] = found?.Signature ?? "",
                ["fate"] = fate, ["planned_owner"] = Ownership.For(symbol).Owner
            };
        }).ToArray();
        LedgerCsv.Write(Path.Combine(args[2], "api-ledger-phase4-diff.csv"), changes.Where(row => (string?)row["fate"] != "retained").ToArray());
        var manifest = new {
            schemaVersion = 1, baselineSha = baseline.Sha, candidateSha = current.Sha,
            assemblies = current.SourceCounts, exportedRows = rows.Length,
            changes = changes.GroupBy(row => (string)row["fate"]!).ToDictionary(group => group.Key, group => group.Count()),
            scope = "Source symbols; packed four-framework runtime/dependency/resource proof is a separate gate."
        };
        File.WriteAllText(Path.Combine(args[2], "api-ledger-phase4-manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }).Replace("\r\n", "\n") + "\n", new UTF8Encoding(false));
        Console.WriteLine($"Phase 4 source inventory: {rows.Length} rows in {current.SourceCounts.Count} assemblies.");
    }
}
