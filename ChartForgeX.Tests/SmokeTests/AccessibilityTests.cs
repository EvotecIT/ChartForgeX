using System;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Topology;
using ChartForgeX.Accessibility;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SharedAccessibilityMetadataReachesSvgRenderers() {
        var chartSvg = Chart.Create()
            .Title("Internal title")
            .Accessibility(a => a.TextAlternative("Revenue trend", "Revenue rose each quarter.", "en-GB"))
            .AddLine("Revenue", Points(4, 7, 11))
            .ToSvg();
        var canvasSvg = VisualCanvas.CreateSocialPreview()
            .Accessibility(a => a.TextAlternative("Release card", "Version 1.0 release artwork.", "en"))
            .ToSvg();
        var topologySvg = TopologyChart.Create()
            .Accessibility(a => a.TextAlternative("Service topology", "API connected to storage.", "en-US"))
            .AddNode("api", "API", 100, 100)
            .ToSvg();

        Assert(chartSvg.Contains(">Revenue trend</title>", StringComparison.Ordinal) && chartSvg.Contains(">Revenue rose each quarter.</desc>", StringComparison.Ordinal) && chartSvg.Contains("lang=\"en-GB\"", StringComparison.Ordinal), "Chart SVG should render the shared text alternative and language.");
        Assert(canvasSvg.Contains(">Release card</title>", StringComparison.Ordinal) && canvasSvg.Contains(">Version 1.0 release artwork.</desc>", StringComparison.Ordinal) && canvasSvg.Contains("lang=\"en\"", StringComparison.Ordinal), "VisualCanvas SVG should render the shared text alternative and language.");
        Assert(topologySvg.Contains(">Service topology</title>", StringComparison.Ordinal) && topologySvg.Contains(">API connected to storage.</desc>", StringComparison.Ordinal) && topologySvg.Contains("lang=\"en-US\"", StringComparison.Ordinal), "Topology SVG should preserve accessibility metadata through layout preparation.");
    }

    private static void DecorativeVisualsStayHiddenFromAssistiveTechnology() {
        var chartSvg = Chart.Create().Accessibility(a => a.Decorative()).AddLine("Values", Points(1, 2)).ToSvg();
        var canvasSvg = VisualCanvas.Create(320, 180).Accessibility(a => a.Decorative()).ToSvg();
        var topologySvg = TopologyChart.Create().Accessibility(a => a.Decorative()).AddNode("node", "Node", 100, 100).ToSvg();

        foreach (var svg in new[] { chartSvg, canvasSvg, topologySvg }) {
            Assert(svg.Contains("aria-hidden=\"true\"", StringComparison.Ordinal), "Decorative SVG should be hidden from assistive technology.");
            Assert(!svg.Contains("role=\"img\"", StringComparison.Ordinal) && !svg.Contains("aria-labelledby=", StringComparison.Ordinal) && !svg.Contains("<title", StringComparison.Ordinal), "Decorative SVG should not expose an image role or unused text alternative nodes.");
        }
    }
}
