using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void TopologyPathCaptureContracts() {
        var chart = TopologyChart.Create().WithId("maintenance-plan").WithViewport(740, 420).WithLegend(null)
            .AddNode("gateway", "Gateway", 60, 100, width: 220, height: 150).AddNode("worker", "Worker", 440, 100, width: 220, height: 150)
            .AddEdge("dispatch", "gateway", "worker", "Dispatch");
        chart.Nodes[0].Subtitle = "Healthy service";
        chart.Nodes[0].SubtitleTextStyle = new TextStyleOverride { FontFamily = PackageAssertions.OutlineFontFamily, FontSize = 14, Color = ChartColor.FromHex("#2367AB") };
        chart.PathDecorations.Add(new TopologyPathDecoration("maintenance-zone", "M 350.12345678901232 60 C 320 140 380 220 350 340",
            stroke: ChartColor.FromHex("#92400E"), strokeWidth: 2.1234567890123457, dashPattern: new[] { 8.123456789012346, 4d }));
        var prepared = chart.Prepare(); var saved = prepared.ToInterchangeEnvelope();
        var loaded = VisualArtifactInterchangeEnvelope.FromUtf8Json(saved.ToUtf8Json()); var restored = loaded.ToTopologyChart();
        PackageAssertions.Require(saved.Version == 2 && saved.ToJson() == loaded.ToJson(), "Native path capture did not retain its version-2 typed JSON.");
        var path = saved.Topology!.PathDecorations.Single(); var ink = restored.PathDecorations.Single();
        PackageAssertions.Require(path.Id == ink.Id && path.PathData == ink.PathData && path.StrokeWidth == ink.StrokeWidth && path.DashPattern!.SequenceEqual(ink.DashPattern!), "Packed path capture lost numeric geometry or paint.");
        PackageAssertions.Require(restored.Nodes.Count == 2 && restored.Edges.Count == 1 && restored.Nodes[0].SubtitleTextStyle!.FontSize == 14, "Packed native decoration changed graph facts or dropped subtitle style.");
        PackageAssertions.Require(restored.ToSvg().Contains("maintenance-zone"), "Restored packed geometry was not painted.");
        PackageAssertions.Png(restored.ToPng());
        chart.PathDecorations.Clear(); chart.Nodes[0].SubtitleTextStyle = null;
        PackageAssertions.Require(chart.Prepare().ToInterchangeEnvelope().Version == 1, "Ordinary packed topology no longer emits version 1.");
    }
}
