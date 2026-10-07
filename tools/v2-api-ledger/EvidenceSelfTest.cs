using System.Text;
using System.Text.Json;

namespace ApiLedger;

/// <summary>Proves public evidence boundaries with disposable synthetic consumer source.</summary>
internal static class EvidenceSelfTest {
    internal static void Run(string? scratchDirectory, ApiSymbol symbol) {
        string scratch = System.IO.Path.GetFullPath(scratchDirectory ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ChartForgeX-api-ledger-self-test"));
        string run = System.IO.Path.Combine(scratch, "synthetic-" + Guid.NewGuid().ToString("N"));
        string consumer = System.IO.Path.Combine(run, "synthetic-private-source");
        Directory.CreateDirectory(consumer);
        Inventory.Git(consumer, "-c", "init.defaultBranch=self-test", "init");
        for (int index = 0; index < 6; index++)
            File.WriteAllText(System.IO.Path.Combine(consumer, "private-source-" + index + ".cs"), "// ChartForgeX consumer lead\nclass Caller { void Run() { Api.Add(3); } }\n");
        Inventory.Git(consumer, "add", ".");
        Inventory.Git(consumer, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid", "-c", "core.hooksPath=" + run,
            "commit", "--no-verify", "--no-gpg-sign", "-m", "Synthetic consumer fixture");
        string sha = Inventory.Git(consumer, "rev-parse", "HEAD").Trim();
        string mappingPath = System.IO.Path.Combine(run, "private-map.json");
        File.WriteAllText(mappingPath, JsonSerializer.Serialize(new[] { new { neutralName = "private-consumer-reporting", path = consumer } }));
        var mappings = PrivateConsumerMapping.Load(consumer, mappingPath);
        var evidence = new Evidence();
        foreach (var mapping in mappings) evidence.AddRepository(mapping.NeutralName, mapping.Path, anonymized: true);
        string leads = evidence.Find(symbol, tests: false);
        Require(leads == "private-consumer-reporting#displayed-text-leads=5", "Private matches must retain the five-lead cap as an anonymous count.");
        Require(evidence.Repositories.Single().Files == 6, "Manifest must retain the indexed-file count.");
        Require(evidence.Repositories.Single().Sha == "" && evidence.Repositories.Single().Status == "private-audit-details-external", "Private source metadata must remain external.");
        string publicEvidence = JsonSerializer.Serialize(new { consumerRepositories = evidence.Repositories, consumer_text_leads = leads });
        Require(!publicEvidence.Contains(sha[..8], StringComparison.Ordinal) && !publicEvidence.Contains("synthetic-private-source", StringComparison.Ordinal) &&
            !publicEvidence.Contains("private-source-", StringComparison.Ordinal) && !publicEvidence.Contains(consumer, StringComparison.Ordinal), "Public evidence must not expose private heads or paths.");
        File.WriteAllText(System.IO.Path.Combine(run, "public-evidence.json"), publicEvidence);
        Reject<ArgumentException>(() => PrivateConsumerMapping.Load(consumer, System.IO.Path.Combine(consumer, "mapping.json")));
        File.WriteAllText(mappingPath, "[{\"neutralName\":\"source-identity\",\"path\":\"/fixture\"}]");
        Reject<ArgumentException>(() => PrivateConsumerMapping.Load(consumer, mappingPath));
        File.WriteAllText(mappingPath, "[{\"neutralName\":\"private-consumer-reporting\"}]");
        Reject<ArgumentException>(() => PrivateConsumerMapping.Load(consumer, mappingPath));
        File.WriteAllText(mappingPath, "null");
        Reject<ArgumentException>(() => PrivateConsumerMapping.Load(consumer, mappingPath));
        File.WriteAllText(mappingPath, "[");
        Reject<JsonException>(() => PrivateConsumerMapping.Load(consumer, mappingPath));
        string csv = System.IO.Path.Combine(run, "writer.csv");
        LedgerCsv.Write(csv, [new() { ["label"] = "quote\"comma,", ["value"] = "first\nsecond" }]);
        byte[] bytes = File.ReadAllBytes(csv);
        Require(!bytes.Contains((byte)13) && !bytes.AsSpan().StartsWith(new byte[] { 239, 187, 191 }), "CSV must use LF without a UTF-8 BOM.");
        Require(Encoding.UTF8.GetString(bytes) == "\"label\",\"value\"\n\"quote\"\"comma,\",\"first\nsecond\"\n", "CSV quoting and embedded LF must preserve cell values.");
        Console.WriteLine("Synthetic evidence and writer proof: " + run);
    }

    private static void Reject<TException>(Action action) where TException : Exception {
        try { action(); } catch (TException) { return; }
        throw new InvalidOperationException("Expected " + typeof(TException).Name + " for invalid private mapping.");
    }

    private static void Require(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }
}
