using System.Text.RegularExpressions;

namespace ApiLedger;

internal sealed record EvidenceFile(string Repository, string Sha, string Path, HashSet<string> Tokens, bool Anonymized);
internal sealed record EvidenceRepository(string Name, string Sha, string Status, int Files);

/// <summary>Indexes caller tokens conservatively; text hits remain leads rather than compiled call proof.</summary>
internal sealed class Evidence {
    private readonly Dictionary<string, List<EvidenceFile>> _byToken = new(StringComparer.Ordinal);
    internal List<EvidenceRepository> Repositories { get; } = [];

    internal void AddRepository(string name, string path, bool testsOnly = false, bool anonymized = false) {
        if (!Directory.Exists(path)) { Repositories.Add(new(name, "", "not-found", 0)); return; }
        string sha = anonymized ? "" : Inventory.Git(path, "rev-parse", "HEAD").Trim();
        string status = anonymized ? "private-audit-details-external" : Inventory.Git(path, "status", "--porcelain").Length == 0 ? "clean-local-source" : "dirty-local-source-text-leads-only";
        var tracked = Inventory.Git(path, "ls-files", "-z").Split('\0', StringSplitOptions.RemoveEmptyEntries);
        int count = 0;
        foreach (string file in tracked) {
            if (!(file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".ps1", StringComparison.Ordinal))) continue;
            if (testsOnly && !file.StartsWith("ChartForgeX.Tests/", StringComparison.Ordinal)) continue;
            string fullPath = Path.Combine(path, file.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath)) continue;
            string content = File.ReadAllText(fullPath);
            if (!content.Contains("ChartForgeX", StringComparison.Ordinal)) continue;
            var tokens = Regex.Matches(content, @"\b[A-Za-z_][A-Za-z_0-9]*\b").Select(match => match.Value).ToHashSet(StringComparer.Ordinal);
            var evidence = new EvidenceFile(name, sha, anonymized ? "" : file, tokens, anonymized);
            foreach (string token in tokens) {
                if (!_byToken.TryGetValue(token, out var leads)) _byToken.Add(token, leads = []);
                leads.Add(evidence);
            }
            count++;
        }
        Repositories.Add(new(name, sha, status, count));
    }

    internal string Find(ApiSymbol symbol, bool tests) {
        string typeName = symbol.Receiver;
        string member = symbol.Name.StartsWith(".", StringComparison.Ordinal) ? typeName : symbol.Name;
        if (!_byToken.TryGetValue(typeName, out var files)) return "";
        return string.Join(";", files.Where(file => (file.Repository == "ChartForgeX-tests") == tests &&
            file.Tokens.Contains(typeName) && (symbol.Kind is "Class" or "Struct" or "Enum" or "Interface" or "Delegate" || file.Tokens.Contains(member)))
            .Take(5)
            .GroupBy(file => file.Anonymized ? file.Repository : file.Repository + "@" + file.Sha[..8] + ":" + file.Path, StringComparer.Ordinal)
            .Select(group => group.First().Anonymized ? group.Key + "#displayed-text-leads=" + group.Count() : group.Key));
    }
}
