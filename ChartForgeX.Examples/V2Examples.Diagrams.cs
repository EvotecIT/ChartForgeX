using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

public static partial class V2Examples {
    private static void WriteDiagrams(string output, ICollection<ProofArtifact> artifacts) {
        foreach (var family in new[] { "topology", "sequence" }) {
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
                var id = family + "-feasibility-" + mode.ToString().ToLowerInvariant();
                var title = family == "topology" ? "Service request route" : "Client and server exchange";
                IVisualRenderable model = family == "topology" ? CreateTopology() : CreateSequence();
                var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)),
                    VisualTheme.Graphite(), mode, new VisualFrame(title, "Product-neutral diagram semantics", showLegend: false), FontSpec.FromFamily(ProofFont));
                var prepared = model.Prepare(context);
                File.WriteAllText(Path.Combine(output, id + ".svg"), prepared.ToSvg(id));
                File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
                var thumbnail = model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)),
                    VisualTheme.Graphite(), mode, new VisualFrame("", "", showLegend: false), FontSpec.FromFamily(ProofFont)));
                File.WriteAllText(Path.Combine(output, id + ".thumbnail.svg"), thumbnail.ToSvg(id + "-thumbnail"));
                File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), DiagramSnippet(family, title, mode));
                WritePage(output, id, title, mode);
                artifacts.Add(new ProofArtifact(id, family, title, "feasibility", mode.ToString().ToLowerInvariant(), 640, 400,
                    prepared.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), prepared.Regions.Count));
            }
        }
    }

    /// <summary>Creates the bounded native topology proof without product-specific data or layout policy.</summary>
    // <topology-source>
    public static TopologyChart CreateTopology() {
        var chart = TopologyChart.Create();
        chart.Id = "service-request";
        chart.Viewport = new TopologyViewport { Width = 640, Height = 400 };
        chart.Nodes.Add(new TopologyNode { Id = "source", Label = "Source\nservice", X = 24, Y = 70, Width = 140, Height = 64, Status = TopologyHealthStatus.Healthy });
        chart.Nodes.Add(new TopologyNode { Id = "target", Label = "Target", X = 300, Y = 70, Width = 140, Height = 64 });
        chart.Edges.Add(new TopologyEdge { Id = "route", SourceNodeId = "source", TargetNodeId = "target", Direction = VisualLinkDirection.Forward,
            Routing = TopologyEdgeRouting.Straight, LineStyle = TopologyEdgeLineStyle.Solid, Label = "Request" });
        return chart;
    }
    // </topology-source>

    /// <summary>Creates a multiline call and dashed return with ordinary participants.</summary>
    // <sequence-source>
    public static SequenceArtifact CreateSequence() => SequenceArtifact.Create("session").WithSize(640, 400)
        .AddParticipant("client", "Client").AddParticipant("server", "Server")
        .AddMessage("client", "server", "Request\npayload")
        .AddMessage("server", "client", "Response", SequenceArtifactMessageLineStyle.Dashed, SequenceArtifactMessageKind.Return);
    // </sequence-source>

    private static string DiagramSnippet(string family, string title, VisualThemeMode mode) {
        using var stream = typeof(V2Examples).Assembly.GetManifestResourceStream("ChartForgeX.Examples.V2DiagramSource")
            ?? throw new InvalidOperationException("Diagram example source is missing.");
        using var reader = new StreamReader(stream);
        var source = reader.ReadToEnd();
        var marker = "// <" + family + "-source>";
        var start = source.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = source.IndexOf("// </" + family + "-source>", start, StringComparison.Ordinal);
        var factory = source.Substring(start, end - start).Trim().Replace("public static ", "static ", StringComparison.Ordinal);
        return "using ChartForgeX.Primitives;\nusing ChartForgeX.Rendering;\nusing ChartForgeX.Themes;\nusing ChartForgeX.Topology;\nusing ChartForgeX.Typography;\nusing ChartForgeX.VisualArtifacts;\n\n" +
            "FontRegistry.Register(\"" + ProofFont + "\", \"fonts/Carlito-Regular.ttf\", 400);\nFontRegistry.Register(\"" + ProofFont + "\", \"fonts/Carlito-Bold.ttf\", 700);\n" +
            "var model = " + (family == "topology" ? "CreateTopology" : "CreateSequence") + "();\n" +
            "var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)), VisualTheme.Graphite(), VisualThemeMode." + mode + ",\n" +
            "    new VisualFrame(" + Literal(title) + ", \"Product-neutral diagram semantics\", showLegend: false), FontSpec.FromFamily(\"" + ProofFont + "\"));\n" +
            "var prepared = model.Prepare(context);\nSystem.IO.File.WriteAllText(\"diagram.svg\", prepared.ToSvg());\nSystem.IO.File.WriteAllBytes(\"diagram.png\", prepared.ToPng());\n\n" + factory + "\n";
    }
}
