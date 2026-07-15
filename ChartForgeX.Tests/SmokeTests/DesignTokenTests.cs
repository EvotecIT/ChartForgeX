using System;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SharedDesignTokensStyleChartsCanvasesAndTopology() {
        var tokens = VisualDesignTokens.Dark();
        tokens.Accent = ChartColor.FromHex("#7C3AED");
        tokens.Positive = ChartColor.FromHex("#10B981");
        tokens.FontFamily = "Aptos, sans-serif";
        tokens.Palette = new[] { tokens.Accent, tokens.Positive };

        var chart = Chart.Create().WithDesignTokens(tokens).AddLine("Values", Points(1, 3, 2));
        var canvas = VisualCanvas.Create(640, 360).WithDesignTokens(tokens).AddText(20, 20, 300, "Shared brand", 24, tokens.Foreground);
        var topology = TopologyChart.Create().WithDesignTokens(tokens).AddNode("node", "Node", 100, 100, status: TopologyHealthStatus.Healthy);

        Assert(chart.Options.Theme.Palette[0].ToHex() == tokens.Accent.ToHex() && chart.Options.Theme.Positive.ToHex() == tokens.Positive.ToHex() && chart.Options.Theme.FontFamily == tokens.FontFamily, "Chart themes should receive palette, semantic, and typography tokens.");
        Assert(canvas.Theme.Accent.ToHex() == tokens.Accent.ToHex() && canvas.Theme.FontFamily == tokens.FontFamily && canvas.BackgroundTop.ToHex() == tokens.Background.ToHex(), "VisualCanvas should receive the same brand and surface tokens.");
        Assert(topology.Theme != null && topology.Theme.Accent == tokens.Accent.ToCss() && topology.Theme.Healthy == tokens.Positive.ToCss() && topology.Theme.FontFamily == tokens.FontFamily, "Topology should receive the same brand, semantic, and typography tokens.");
        Assert(canvas.ToSvg().Contains("font-family=\"Aptos, sans-serif\"", StringComparison.Ordinal), "VisualCanvas SVG should use its shared token font instead of a renderer-local font stack.");
        Assert(topology.ToSvg().Contains("font-family:Aptos, sans-serif", StringComparison.Ordinal), "Topology SVG should use the shared token font.");
    }
}
