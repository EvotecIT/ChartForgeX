// The Mermaid family atlas: one representative sample per supported family, rendered natively in both themes.
// Families and copy come from data/mermaid.json. Each family names its source:
//   playground:<id>  a sample from Website/tools/Playground/PlaygroundSamples.cs
//   fixture:<name>   tests/mermaid-conformance/fixtures/<name>.mmd
//   site:<name>      Website/tools/SiteVisuals/Mermaid/<name>.mmd
//   plugin-fixture:<path> source text from the isolated test corpus; no plugin is loaded or executed
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
                var source = Titled(Source(output.Website, family.GetProperty("source").GetString()!).Trim(),
                    family.TryGetProperty("title", out var title) ? title.GetString() : null) + "\n";
                var warnings = new List<string>();
                output.Text($"atlas/{id}.mmd", source);
                // Atlas cards are 4:3; each render is cropped to what it draws and widened to that shape.
                var familyTitle = family.TryGetProperty("title", out var named) ? named.GetString() : null;
                var entry = output.Pair($"atlas/{id}", (_, dark) => VisualOutput.Fit(Render(id, source, dark, warnings, CardOptions(id, familyTitle, family.GetProperty("name").GetString()!)), 4d / 3));
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

    // A title from data/mermaid.json goes into front matter unless the source already names one, so no render falls
    // back to a generic "Mermaid flowchart" heading.
    private static readonly System.Text.RegularExpressions.Regex TitleLine = new(@"(?m)^\s*(title\b|---)");

    private static string Titled(string source, string? title) =>
        string.IsNullOrWhiteSpace(title) || TitleLine.IsMatch(source) ? source : "---\ntitle: " + title + "\n---\n" + source;

    private static string Source(string website, string reference) {
        var colon = reference.IndexOf(':');
        var kind = reference.Substring(0, colon);
        var name = reference.Substring(colon + 1);
        return kind switch {
            "playground" => PlaygroundSamples.All.Single(sample => sample.Id == name).Source,
            "fixture" => File.ReadAllText(Path.Combine(website, "..", "tests", "mermaid-conformance", "fixtures", name + ".mmd")),
            "site" => File.ReadAllText(Path.Combine(website, "tools", "SiteVisuals", "Mermaid", name + ".mmd")),
            "plugin-fixture" => File.ReadAllText(Path.Combine(website, "..", "tests", "mermaid-conformance", "external-plugins", name + ".mmd")),
            _ => throw new InvalidOperationException("Unknown Mermaid source kind: " + reference)
        };
    }

    // The same path the playground engine takes: the page theme applies unless the source sets its own.
    // Atlas renders lay out on a 4:3 canvas smaller than the 960 x 560 default, so the diagram fills its card and text
    // stays readable at card size. Each family's options class takes the size and, for families that do not read
    // front matter, the title.
    private static readonly Dictionary<string, string> OptionProperties = new(StringComparer.Ordinal) {
        ["flowchart"] = "Flowchart", ["sequence"] = "Sequence", ["class"] = "Class", ["state"] = "State",
        ["er"] = "EntityRelationship", ["requirement"] = "Requirement", ["architecture"] = "Architecture", ["c4"] = "C4",
        ["block"] = "Block", ["packet"] = "Packet", ["gantt"] = "Gantt", ["timeline"] = "Timeline", ["journey"] = "Journey",
        ["kanban"] = "Kanban", ["gitgraph"] = "GitGraph", ["swimlane"] = "Swimlane", ["usecase"] = "UseCase",
        ["eventmodeling"] = "EventModeling", ["pie"] = "Pie", ["xychart"] = "XYChart", ["sankey"] = "Sankey",
        ["radar"] = "Radar", ["treemap"] = "Treemap", ["quadrant"] = "Quadrant", ["mindmap"] = "MindMap",
        ["treeview"] = "TreeView", ["venn"] = "Venn", ["ishikawa"] = "Ishikawa", ["wardley"] = "Wardley", ["cynefin"] = "Cynefin",
        ["agentflow"] = "Agentflow", ["railroad"] = "Railroad", ["zenuml"] = "ZenUml"
    };

    // Families whose node cards need more room for their labels lay out on a larger 4:3 canvas.
    private static readonly HashSet<string> RoomyFamilies = new(StringComparer.Ordinal) { "c4", "architecture", "requirement", "eventmodeling", "agentflow", "zenuml", "railroad" };

    private static MermaidRenderOptions CardOptions(string id, string? title, string name) {
        var options = new MermaidRenderOptions();
        if (!OptionProperties.TryGetValue(id, out var propertyName)) return options;
        var property = typeof(MermaidRenderOptions).GetProperty(propertyName) ?? throw new InvalidOperationException("No render options for " + id);
        var family = Activator.CreateInstance(property.PropertyType)!;
        void Set(string member, object value) {
            var target = property.PropertyType.GetProperty(member);
            if (target != null && target.CanWrite) target.SetValue(family, Convert.ChangeType(value, Nullable.GetUnderlyingType(target.PropertyType) ?? target.PropertyType, System.Globalization.CultureInfo.InvariantCulture));
        }

        Set("Width", RoomyFamilies.Contains(id) ? 920 : 760);
        Set("Height", RoomyFamilies.Contains(id) ? 690 : 570);
        // The subtitle would otherwise repeat the source keyword, such as "classDiagram".
        Set("Subtitle", name);
        if (!string.IsNullOrWhiteSpace(title)) Set("Title", title!);
        property.SetValue(options, family);
        return options;
    }

    public static string Render(string id, string source, bool dark, List<string> warnings, MermaidRenderOptions? options = null) {
        var parsed = new MermaidParser().Parse(source);
        warnings.AddRange(parsed.Diagnostics.Select(d => $"{d.Severity} {d.Code}: {d.Message}"));
        if (parsed.HasErrors || parsed.Document == null) throw new InvalidOperationException($"Mermaid sample '{id}' failed to parse.");
        var document = parsed.Document;
        if (document.Theme == null && dark) document.Theme = "dark";
        var artifact = document.ToVisualArtifact(options ?? new MermaidRenderOptions());
        return artifact.ToSvg(null, $"cfx-mm-{id}-{(dark ? "d" : "l")}");
    }
}
