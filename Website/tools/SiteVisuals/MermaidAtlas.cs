// The Mermaid family atlas: one representative sample per supported family, rendered natively in both themes.
// Families and copy come from data/mermaid.json. Each family names its source:
//   playground:<id>  a sample from Website/tools/Playground/PlaygroundSamples.cs
//   fixture:<name>   tests/mermaid-conformance/fixtures/<name>.mmd
//   site:<name>      Website/tools/SiteVisuals/Mermaid/<name>.mmd
using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Playground;
using ChartForgeX.VisualArtifacts;

namespace SiteVisuals;

internal static class MermaidAtlas {
    public static Dictionary<string, object> Write(VisualOutput output) {
        using var copy = JsonDocument.Parse(File.ReadAllText(Path.Combine(output.Website, "data", "mermaid.json")));
        var families = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (var group in copy.RootElement.GetProperty("groups").EnumerateArray()) {
            foreach (var family in group.GetProperty("families").EnumerateArray()) {
                var id = family.GetProperty("id").GetString()!;
                var source = Source(output.Website, family.GetProperty("source").GetString()!).Trim() + "\n";
                var warnings = new List<string>();
                output.Text($"atlas/{id}.mmd", source);
                var entry = output.Pair($"atlas/{id}", (_, dark) => Render(id, source, dark, warnings));
                entry["source"] = source.TrimEnd();
                entry["lines"] = source.TrimEnd().Split('\n').Length;
                entry["warnings"] = warnings.Distinct(StringComparer.Ordinal).Count();
                families[id] = entry;
                VisualOutput.Report("mermaid " + id, warnings.Distinct(StringComparer.Ordinal));
            }
        }

        // Recognised families report a diagnostic instead of output; the atlas quotes that diagnostic.
        var recognised = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var family in copy.RootElement.GetProperty("recognised").EnumerateArray()) {
            var parsed = new MermaidParser().Parse(family.GetProperty("header").GetString() + "\n");
            var document = parsed.Document ?? throw new InvalidOperationException("Recognised family did not parse: " + family.GetProperty("id").GetString());
            if (!document.IsDiagnosticOnly) throw new InvalidOperationException(family.GetProperty("id").GetString() + " renders now; move it into a group.");
            var message = parsed.Diagnostics.Select(d => d.Message).FirstOrDefault()
                ?? "Recognised, but ChartForgeX does not render this diagram family yet.";
            recognised[family.GetProperty("id").GetString()!] = message;
        }

        return new Dictionary<string, object> { ["families"] = families, ["recognised"] = recognised };
    }

    private static string Source(string website, string reference) {
        var colon = reference.IndexOf(':');
        var kind = reference.Substring(0, colon);
        var name = reference.Substring(colon + 1);
        return kind switch {
            "playground" => PlaygroundSamples.All.Single(sample => sample.Id == name).Source,
            "fixture" => File.ReadAllText(Path.Combine(website, "..", "tests", "mermaid-conformance", "fixtures", name + ".mmd")),
            "site" => File.ReadAllText(Path.Combine(website, "tools", "SiteVisuals", "Mermaid", name + ".mmd")),
            _ => throw new InvalidOperationException("Unknown Mermaid source kind: " + reference)
        };
    }

    // The same path the playground engine takes: the page theme applies unless the source sets its own.
    public static string Render(string id, string source, bool dark, List<string> warnings) {
        var parsed = new MermaidParser().Parse(source);
        warnings.AddRange(parsed.Diagnostics.Select(d => $"{d.Severity} {d.Code}: {d.Message}"));
        if (parsed.HasErrors || parsed.Document == null) throw new InvalidOperationException($"Mermaid sample '{id}' failed to parse.");
        var document = parsed.Document;
        if (document.Theme == null && dark) document.Theme = "dark";
        var artifact = document.ToVisualArtifact(new MermaidRenderOptions());
        return artifact.ToSvg(null, $"cfx-mm-{id}-{(dark ? "d" : "l")}");
    }
}
