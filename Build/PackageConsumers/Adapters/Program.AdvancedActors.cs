using System;
using System.IO;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void AdvancedActorContracts() {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("AdvancedActors.mmd") ?? throw new InvalidOperationException("Packed actor source was omitted.");
        using var reader = new StreamReader(stream); var source = reader.ReadToEnd().Replace("direction TB", "direction LR", StringComparison.Ordinal);
        var options = new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject,
            UseCase = new MermaidFlowchartRenderOptions { Width = 390, Height = 1160,
                Presentation = new MermaidPresentationOptions { TopologySizing = MermaidTopologySizing.FixedViewport } } };
        var direct = RenderFixture(source, options); var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!direct.HasErrors && direct.Diagnostics.Count == 0 && direct.Artifact != null && !fence.HasErrors && fence.Artifacts.Count == 1,
            "Packed finite actor source did not lower through direct and fence owners.");
        var chart = (TopologyChart)direct.Artifact!.Model!; var captured = chart.Prepare(); var svg = captured.ToSvg(); var png = captured.ToPng();
        PackageAssertions.Require(chart.Nodes.Select(node => node.Shape).SequenceEqual(new TopologyNodeShape?[] { TopologyNodeShape.Actor, TopologyNodeShape.ActorHollow,
            TopologyNodeShape.ActorBusiness, TopologyNodeShape.ActorBusinessHollow, TopologyNodeShape.ActorSilhouette, TopologyNodeShape.ActorIcon, TopologyNodeShape.BusinessEllipse })
            && chart.Nodes[5].IconShape == TopologyIconShape.Router && chart.Nodes[5].IconId == "network:router" && chart.Edges.Select(edge => edge.Id).Distinct().Count() == 6,
            "Packed native shapes, resolved finite glyph or association identities were lost.");
        PackageAssertions.Require(direct.Artifact.ToSvg() == fence.Artifacts[0].ToSvg() && direct.Artifact.ToPng().SequenceEqual(fence.Artifacts[0].ToPng()), "Packed actor direct/fence outputs differ.");
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(captured.ToInterchangeEnvelope().ToJson()); var loaded = envelope.ToTopologyChart();
        PackageAssertions.Require(envelope.Version == 2 && loaded.Nodes[5].IconShape == TopologyIconShape.Router && loaded.Nodes[5].IconId == "network:router"
            && loaded.Nodes[3].Shape == TopologyNodeShape.ActorBusinessHollow && loaded.Annotations.Single().TargetId == "Submit", "Packed typed restoration lost actor forms, glyph or attached note.");
        chart.Nodes[4].Shape = TopologyNodeShape.Actor; chart.Nodes[5].IconShape = TopologyIconShape.Badge; chart.Edges.Clear();
        PackageAssertions.Require(svg == captured.ToSvg() && png.SequenceEqual(captured.ToPng()), "Packed actor edits changed a detached prepared export.");
        PackageAssertions.Png(png); PackageAssertions.Png(loaded.ToPng());
    }
}
