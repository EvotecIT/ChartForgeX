using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void FlowchartCurveContracts() {
        foreach (var lane in new[] { false, true }) {
            var source = (lane ? "swimlane-beta" : "flowchart") + " LR\nA e@--> B\nB --> C\ne@{curve:stepAfter}";
            var artifact = PackedNativeDiagram("curve-" + (lane ? "swimlane" : "flowchart"), source);
            var chart = (TopologyChart)artifact.Model!;
            PackageAssertions.Require(chart.Edges[0].Interpolation == TopologyEdgeInterpolation.StepAfter
                && chart.Edges[1].Interpolation == (lane ? TopologyEdgeInterpolation.Rounded : TopologyEdgeInterpolation.Basis)
                && chart.Theme.Typography.DataLabelSize == 16, "Packed source curve or typography default was lost.");
            var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
            var restored = envelope.ToTopologyChart().ToVisualArtifact();
            PackageAssertions.Require(EdgeCurveInk(artifact.ToSvg()).SequenceEqual(EdgeCurveInk(restored.ToSvg())), "Packed source curve numeric paint changed on native restoration.");
            PackageAssertions.Png(restored.ToPng());
        }
        const string grammar = "flowchart LR\nlinkStyle default interpolate natural\nA --> B\nB --> C\nlinkStyle 1 interpolate stepBefore,stroke:#123456";
        var styled = (TopologyChart)PackedNativeDiagram("curve-source-order", grammar).Model!;
        PackageAssertions.Require(styled.Edges[0].Interpolation == TopologyEdgeInterpolation.Natural
            && styled.Edges[1].Interpolation == TopologyEdgeInterpolation.StepBefore && styled.Edges[1].Color == "#123456", "Packed source-order curve grammar changed its finite native choices.");
    }

    private static string[] EdgeCurveInk(string svg) => XDocument.Parse(svg).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == "topology-edge")
        .SelectMany(element => element.Descendants().Where(child => child.Name.LocalName == "path"))
        .Select(element => string.Join("|", new[] { "d", "fill", "stroke", "stroke-width", "stroke-dasharray" }.Select(name => (string?)element.Attribute(name) ?? ""))).ToArray();
}
