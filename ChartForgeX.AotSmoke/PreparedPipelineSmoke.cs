using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

/// <summary>Exercises the shared prepared pipeline and portable handoff in the existing native AOT executable.</summary>
internal static class PreparedPipelineSmoke {
    internal static void Run() {
        string themeJson = VisualTheme.Graphite().ToThemeJson();
        var theme = VisualTheme.FromThemeJson(themeJson);
        Require(themeJson == theme.ToThemeJson(), "Versioned paired theme roundtrip failed.");
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)), theme, mode,
                new VisualFrame("Shared AOT output", "Prepared static pipeline", showLegend: false));
            var chart = Chart.Create().WithSize(640, 400).WithXLabels("First", "Second", "Third")
                .AddBar("Counts", new[] { new ChartPoint(1, 2), new ChartPoint(2, 5), new ChartPoint(3, 4) });
            var preparedChart = chart.Prepare(context);
            AssertPrepared(preparedChart, "bar");
            var chartArtifact = preparedChart.ToArtifact("aot-v2-chart", VisualArtifactKind.Chart, title: "Prepared chart");
            var chartPortable = VisualArtifactInterchangeEnvelope.FromUtf8Json(chartArtifact.ToInterchangeUtf8Json());
            Require(chartPortable.Id == "aot-v2-chart" && chartPortable.Kind == VisualArtifactKind.Chart,
                "Prepared chart artifact identity failed.");
            var donut = Chart.Create().WithXLabels("Ready", "Waiting")
                .AddDonut("Share", new[] { new ChartPoint(1, 72), new ChartPoint(2, 28) });
            AssertPrepared(donut.Prepare(context), "donut-slice");

            var topology = CreateTopology();
            var preparedTopology = topology.Prepare(context);
            AssertPrepared(preparedTopology, "topology-node");
            AssertSemanticHandoff(preparedTopology, topology.ToVisualArtifact().ToInterchangeEnvelope(),
                VisualArtifactInterchangeFamily.Topology, 1);

            var sequence = SequenceArtifact.Create("aot-v2-sequence").WithSize(640, 400)
                .AddParticipant("client", "Client").AddParticipant("server", "Server")
                .AddMessage("client", "server", "Request\npayload")
                .AddMessage("server", "client", "Response", SequenceArtifactMessageLineStyle.Dashed, SequenceArtifactMessageKind.Return);
            var preparedSequence = sequence.Prepare(context);
            AssertPrepared(preparedSequence, "sequence-participant");
            AssertSemanticHandoff(preparedSequence, sequence.ToVisualArtifact().ToInterchangeEnvelope(),
                VisualArtifactInterchangeFamily.Sequence, 2);
        }
        Console.WriteLine("Prepared pipeline AOT smoke passed: paired theme JSON, light/dark Cartesian/radial/topology/sequence SVG/PNG and semantic handoff.");
    }

    private static TopologyChart CreateTopology() {
        var topology = TopologyChart.Create();
        topology.Id = "aot-v2-topology";
        topology.Viewport = new TopologyViewport { Width = 640, Height = 400 };
        topology.Nodes.Add(new TopologyNode { Id = "source", Label = "Source\nservice", X = 24, Y = 70,
            Width = 140, Height = 64, Status = TopologyHealthStatus.Healthy });
        topology.Nodes.Add(new TopologyNode { Id = "target", Label = "Target", X = 300, Y = 70, Width = 140, Height = 64 });
        topology.Edges.Add(new TopologyEdge { Id = "route", SourceNodeId = "source", TargetNodeId = "target",
            Direction = VisualLinkDirection.Forward, Routing = TopologyEdgeRouting.Straight,
            LineStyle = TopologyEdgeLineStyle.Solid, Label = "Request" });
        return topology;
    }

    private static void AssertPrepared(PreparedVisual prepared, string semanticRole) {
        Require(prepared.Size.Width == 640 && prepared.Size.Height == 400, "Prepared viewport changed.");
        string svg = prepared.ToSvg();
        Require(svg.Contains("<svg", StringComparison.Ordinal) && svg.Contains("data-cfx-role=\"" + semanticRole + "\"", StringComparison.Ordinal),
            "Prepared SVG marks are missing: " + semanticRole);
        AssertPng(prepared.ToPng());
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        Require(image.Width == 640 && image.Height == 400, "Prepared raster viewport changed.");
        Require(prepared.Regions.Count > 0, "Prepared semantic regions are missing.");
    }

    private static void AssertSemanticHandoff(PreparedVisual prepared, VisualArtifactInterchangeEnvelope semantics,
        VisualArtifactInterchangeFamily family, int edgeCount) {
        var artifact = prepared.ToArtifact(semantics.Id, semantics.Kind, semantics);
        string original = artifact.ToInterchangeJson();
        var portable = VisualArtifactInterchangeEnvelope.FromUtf8Json(artifact.ToInterchangeUtf8Json());
        Require(portable.Family == family && portable.Nodes.Count == 2 && portable.Edges.Count == edgeCount,
            "Prepared diagram lost native semantics: " + family);
        Require(portable.Id == semantics.Id && portable.Width == 640 && portable.Height == 400,
            "Prepared diagram lost artifact identity or dimensions.");
        AssertPng(artifact.ToPng());
        Require(artifact.ToSvg().Contains("<svg", StringComparison.Ordinal), "Prepared artifact SVG failed.");
        semantics.Nodes.Clear();
        portable.Edges.Clear();
        Require(artifact.ToInterchangeJson() == original, "Prepared semantic snapshot aliases caller or reader data.");
    }

    private static void AssertPng(byte[] bytes) => Require(bytes.Length > 64 && bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71,
        "Prepared PNG export failed.");

    private static void Require(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }
}
