using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using Mermaider;

if (args.Length != 2 || !Path.IsPathRooted(args[0]) || !Path.IsPathRooted(args[1])) {
    Console.Error.WriteLine("Usage: MermaiderPeer <absolute corpus root> <absolute output directory>");
    return 2;
}
var root = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
using var configuration = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "peer", "reference.json")));
var reference = configuration.RootElement;
var identity = typeof(MermaidRenderer).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
if (identity.Split('+')[0] != reference.GetProperty("version").GetString()) throw new InvalidDataException("The peer assembly must match the pinned package version.");
Directory.CreateDirectory(output);
var reports = new List<object>();
foreach (var item in reference.GetProperty("fixtures").EnumerateArray()) {
    var name = item.GetString()!;
    var source = File.ReadAllText(Path.Combine(root, "fixtures", name + ".mmd"));
    var expectedPath = Path.Combine(root, "fixtures", name + ".expected.json");
    var expectedText = Array.Empty<string>();
    var expectedIds = Array.Empty<string>();
    if (File.Exists(expectedPath)) {
        using var expected = JsonDocument.Parse(File.ReadAllText(expectedPath));
        if (expected.RootElement.TryGetProperty("peerVisibleText", out var labels)) expectedText = labels.EnumerateArray().Select(label => label.GetString()!).ToArray();
        if (expected.RootElement.TryGetProperty("peerExplicitIds", out var ids)) expectedIds = ids.EnumerateArray().Select(id => id.GetString()!).ToArray();
    }
    object? graph = null;
    string? semanticGap = null;
    if (source.StartsWith("stateDiagram", StringComparison.Ordinal) || source.StartsWith("flowchart", StringComparison.Ordinal)) {
        try {
            var parsed = MermaidRenderer.Parse(source);
            var groups = new HashSet<string>();
            void GroupIds(IEnumerable<Mermaider.Models.MermaidSubgraph> items) {
                foreach (var group in items) { groups.Add(group.Id); GroupIds(group.Children); }
            }
            GroupIds(parsed.Subgraphs);
            graph = new { nodes = parsed.NodeOrder.Where(parsed.Nodes.ContainsKey).Select(id => new { id, parsed.Nodes[id].Label, shape = parsed.Nodes[id].Shape.ToString() }).ToArray(),
                nodeOrderMissingIds = parsed.NodeOrder.Where(id => !parsed.Nodes.ContainsKey(id)).ToArray(),
                edges = parsed.Edges.Select(edge => new { edge.Source, edge.Target, edge.Label }).ToArray(),
                notes = parsed.Notes.Select(note => new { note.TargetNodeId, note.Text, placement = note.Position.ToString() }).ToArray(),
                missingExplicitIds = expectedIds.Where(id => !parsed.Nodes.ContainsKey(id) && !groups.Contains(id)).ToArray() };
        } catch (MermaidParseException error) { semanticGap = "Public peer parser rejected the source: " + error.Message; }
    } else semanticGap = "The public peer Parse API exposes flowchart/state models only. This family is compared through visible SVG text; internal parsers are not reflected.";
    try {
        var svg = MermaidRenderer.RenderSvg(source);
        var document = XDocument.Parse(svg);
        var labels = document.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        var observedText = string.Join("\n", labels);
        var missingText = expectedText.Where(text => !observedText.Contains(text, StringComparison.Ordinal)).ToArray();
        var svgPath = Path.Combine(output, name + ".peer.svg");
        File.WriteAllText(svgPath, svg);
        reports.Add(new { fixture = name, outcome = "rendered", labels, expectedText, missingText, graph, semanticGap,
            width = document.Root?.Attribute("width")?.Value, height = document.Root?.Attribute("height")?.Value, svg = Path.GetFileName(svgPath) });
    } catch (MermaidParseException error) {
        reports.Add(new { fixture = name, outcome = "rejected", error = error.Message, expectedText,
            note = "Peer rejection is recorded as a difference. The official pinned lanes and native contracts remain separate." });
    } catch (MermaidRenderException error) {
        reports.Add(new { fixture = name, outcome = "engine-failure", error = error.Message, graph, semanticGap, expectedText,
            note = "A peer rendering failure is not a qualified syntax rejection and is not converted to a native failure." });
    }
}
var summary = new { package = reference.GetProperty("package").GetString(), version = reference.GetProperty("version").GetString(), identity,
    license = reference.GetProperty("license").GetString(), role = reference.GetProperty("role").GetString(), reports };
File.WriteAllText(Path.Combine(output, "peer-report.json"), JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }) + "\n");
Console.WriteLine("Mermaider 0.15.1 peer: " + reports.Count + " independent source observations; see peer-report.json for acceptance and semantic differences.");
return 0;
