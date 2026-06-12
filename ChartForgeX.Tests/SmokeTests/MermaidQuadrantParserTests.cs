using System;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesQuadrantAxesLabelsAndPoints() {
        const string source = @"quadrantChart
title Reach and Engagement
x-axis Low Reach --> High Reach
y-axis Low Engagement --> High Engagement
quadrant-1 Expand
quadrant-2 Promote
Campaign A: [0.3, 0.6]
Campaign B: [0.7, 0.8]";

        var result = new MermaidParser().ParseQuadrant(source);

        Assert(!result.HasErrors, "Mermaid quadrant parser should parse axes, labels, and points: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid quadrant parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Quadrant, "Mermaid quadrant parser should produce a quadrant document.");
        Assert(document.Title == "Reach and Engagement", "Mermaid quadrant parser should preserve title statements.");
        Assert(document.XAxisStart == "Low Reach" && document.XAxisEnd == "High Reach", "Mermaid quadrant parser should parse x-axis labels.");
        Assert(document.YAxisStart == "Low Engagement" && document.YAxisEnd == "High Engagement", "Mermaid quadrant parser should parse y-axis labels.");
        Assert(document.QuadrantLabels.Count == 2 && document.QuadrantLabels[1] == "Expand", "Mermaid quadrant parser should parse quadrant labels.");
        Assert(document.Points.Count == 2 && Math.Abs(document.Points[1].X - 0.7) < 0.001 && Math.Abs(document.Points[1].Y - 0.8) < 0.001, "Mermaid quadrant parser should parse numeric point coordinates.");
    }

    private static void MermaidQuadrantConvertsToChartArtifactAndRenders() {
        const string source = @"quadrantChart
title Reach and Engagement
x-axis Low Reach --> High Reach
y-axis Low Engagement --> High Engagement
quadrant-1 Expand
quadrant-2 Promote
Campaign A: [0.3, 0.6]
Campaign B: [0.7, 0.8]";

        var result = new MermaidParser().ParseQuadrant(source);
        Assert(!result.HasErrors, "Mermaid quadrant parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid quadrant parser should produce a document.");
        var chart = document.ToChart(new MermaidQuadrantRenderOptions { Id = "reach-engagement" });
        Assert(chart.Series.Count == 1 && chart.Series[0].Kind == ChartSeriesKind.Scatter, "Mermaid quadrant conversion should produce a ChartForgeX scatter chart.");
        Assert(chart.Title == "Reach and Engagement", "Mermaid quadrant conversion should use Mermaid titles by default.");
        Assert(chart.XAxisTitle.Contains("Low Reach", StringComparison.Ordinal), "Mermaid quadrant conversion should map x-axis labels.");

        var artifact = document.ToVisualArtifact(new MermaidQuadrantRenderOptions { Id = "reach-engagement" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid quadrant visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is Chart, "Mermaid quadrant visual artifact should carry a renderable chart model.");
        Assert(artifact.Metadata["mermaid.points"] == "2" && artifact.Metadata["mermaid.quadrants"] == "2", "Mermaid quadrant artifacts should expose point and quadrant-label counts.");
        Assert(artifact.Metadata["render.model"] == nameof(Chart), "Mermaid quadrant artifacts should expose the chart render model.");

        var svg = document.ToSvg(new MermaidQuadrantRenderOptions { Id = "reach-engagement" });
        var png = document.ToPng(new MermaidQuadrantRenderOptions { Id = "reach-engagement" });
        Assert(svg.Contains("data-cfx-role=\"mermaid-quadrant-point\"", StringComparison.Ordinal), "Mermaid quadrant SVG rendering should emit ChartForgeX scatter marks with Mermaid semantic roles.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid quadrant PNG rendering should emit a valid PNG.");
    }

    private static void MermaidQuadrantRejectsOutOfRangePoints() {
        const string source = @"quadrantChart
Outlier: [2, 0.5]";

        var result = new MermaidParser().ParseQuadrant(source);

        Assert(result.HasErrors, "Mermaid quadrant parser should reject normalized coordinates outside zero to one.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("between zero and one", StringComparison.Ordinal)), "Mermaid quadrant coordinate diagnostics should explain the normalized range.");
    }

    private static void MermaidQuadrantStripsInlineCommentsBeforeParsingPoints() {
        const string source = @"quadrantChart
Campaign A: [0.2, 0.3] %% note";

        var result = new MermaidParser().ParseQuadrant(source);

        Assert(!result.HasErrors, "Mermaid quadrant parser should ignore trailing Mermaid comments after points: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid quadrant parser should produce a document.");
        Assert(document.Points.Count == 1 && document.Points[0].Label == "Campaign A" && Math.Abs(document.Points[0].X - 0.2) < 0.001, "Mermaid quadrant parser should parse point coordinates before trailing comments.");
    }

    private static void MermaidQuadrantRendersQuadrantLabels() {
        const string source = @"quadrantChart
quadrant-1 Expand
quadrant-2 Promote
Campaign A: [0.3, 0.6]";

        var result = new MermaidParser().ParseQuadrant(source);

        Assert(!result.HasErrors, "Mermaid quadrant parser should parse quadrant labels: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid quadrant parser should produce a document.");
        var chart = document.ToChart();
        var svg = document.ToSvg();
        Assert(chart.Subtitle != null && chart.Subtitle.Contains("Q1: Expand", StringComparison.Ordinal) && chart.Subtitle.Contains("Q2: Promote", StringComparison.Ordinal), "Mermaid quadrant conversion should keep quadrant labels visible in rendered chart text.");
        Assert(svg.Contains("Expand", StringComparison.Ordinal) && svg.Contains("Promote", StringComparison.Ordinal), "Mermaid quadrant SVG rendering should include quadrant labels.");
    }
}
