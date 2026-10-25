using System;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

/// <summary>A native crossing handoff with unchanged logical relationships and editable paint policy.</summary>
public static class TopologyCrossingPaintRecipe {
    /// <summary>Creates compact or wide Arc, Gap or ordinary native paint in either theme.</summary>
    public static TopologyChart Create(string variant = "wide-arc", VisualThemeMode mode = VisualThemeMode.Light) {
        variant = variant == "wide" ? "wide-arc" : variant == "compact" ? "compact-arc" : variant;
        if (variant is not ("wide-arc" or "compact-arc" or "wide-gap" or "compact-gap" or "wide-none" or "compact-none")) throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = TopologyChart.Create().WithId("native-crossing-handoff").WithTitle("Independent handoffs")
            .WithViewport(variant.StartsWith("compact", StringComparison.Ordinal) ? 390 : 760, 460, 24).WithLayout(TopologyLayoutMode.Manual)
            .WithTheme(VisualTheme.Graphite(), mode).WithLegend(null);
        chart.AddNode("receive", "Receive", 60, 200, width: 110, height: 50).AddNode("send", "Send", 590, 200, width: 110, height: 50)
            .AddNode("check", "Check", 330, 50, width: 110, height: 50).AddNode("finish", "Finish", 330, 350, width: 110, height: 50);
        foreach (var node in chart.Nodes) { node.Kind = TopologyNodeKind.Generic; node.ShowStatusBadge = false; }
        chart.AddEdge("dispatch", "receive", "send", direction: VisualLinkDirection.Forward)
            .AddEdge("approval", "check", "finish", direction: VisualLinkDirection.Forward);
        chart.Edges[0].PathGeometry = new TopologyPathGeometry("M 170 225 L 590 225");
        chart.Edges[1].PathGeometry = new TopologyPathGeometry("M 385 100 L 385 350");
        foreach (var edge in chart.Edges) { edge.StrokeWidth = 3; edge.TargetMarkerStyle = new TopologyMarkerStyle { Scale = .8, StrokeWidth = 1 }; }
        chart.Edges[0].Color = mode == VisualThemeMode.Dark ? "#38bdf8" : "#0369a1";
        chart.Edges[1].Color = mode == VisualThemeMode.Dark ? "#fbbf24" : "#92400e";
        if (!variant.EndsWith("none", StringComparison.Ordinal)) chart.CrossingPaint = new TopologyCrossingPaint(variant.EndsWith("gap", StringComparison.Ordinal) ? TopologyCrossingPaintStyle.Gap : TopologyCrossingPaintStyle.Arc);
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = variant.StartsWith("compact", StringComparison.Ordinal) });
        return chart;
    }
}
