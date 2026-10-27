using System;
using System.IO;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void UseCaseJsonTableContracts() {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("UseCaseJsonTable.mmd")
            ?? throw new InvalidOperationException("Packed adapter probe omitted the canonical JSON vertex source.");
        using var reader = new StreamReader(stream); var source = reader.ReadToEnd();
        var options = new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject,
            UseCase = new MermaidFlowchartRenderOptions { Width = 1000, Height = 680,
                Presentation = new MermaidPresentationOptions { TopologySizing = MermaidTopologySizing.FixedViewport } } };
        var direct = RenderFixture(source, options); var fence = CreateFixtureParser(options).Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!direct.HasErrors && direct.Artifact != null && !fence.HasErrors && fence.Artifacts.Count == 1, "Packed UseCase JSON failed direct/fence conversion.");
        var chart = (TopologyChart)direct.Artifact!.Model!; var table = chart.Nodes.Single(node => node.Id == "Details").TableContent!;
        PackageAssertions.Require(table.Rows.Count == 9 && table.Rows[0].Cells[1].DisplayText == "1.00" && table.Rows[4].Metadata["mermaid.data.kind"] == "Null"
            && chart.Annotations.Single().TargetId == "Details" && chart.Edges.Any(edge => edge.TargetNodeId == "Details"), "Packed JSON table lost raw facts or linked vertex identity.");
        var svg = direct.Artifact.ToSvg(); var png = direct.Artifact.ToPng();
        PackageAssertions.Require(svg == fence.Artifacts[0].ToSvg() && png.SequenceEqual(fence.Artifacts[0].ToPng()), "Packed linked table exports differ across source boundaries.");
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(direct.Artifact.ToInterchangeJson());
        PackageAssertions.Require(envelope.Version == 2 && envelope.Nodes.Single(node => node.Id == "Details").Topology!.TableContent!.Rows.Count == 9, "Packed typed table payload was omitted or emitted as v1.");
        var loaded = envelope.ToTopologyChart(); loaded.Nodes.Single(node => node.Id == "Details").TableContent!.Rows[0].Cells[1].DisplayText = "2.00";
        PackageAssertions.Require(loaded.ToSvg().Contains("2.00", StringComparison.Ordinal) && svg.Contains("1.00", StringComparison.Ordinal), "Packed table save/load/edit detached ownership failed.");
        PackageAssertions.Png(png);
    }
}
