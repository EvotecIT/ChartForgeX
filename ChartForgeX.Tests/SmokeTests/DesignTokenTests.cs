using System;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SharedDesignTokensStyleChartsCanvasesAndTopology() {
        var tokens = VisualDesignTokens.Dark();
        tokens.Accent = ChartColor.FromHex("#7C3AED");
        tokens.Positive = ChartColor.FromHex("#10B981");
        tokens.Status.Pass = new VisualTokenColor(tokens.Positive, ChartColorMath.AccessibleTextOnBackground(tokens.Positive));
        tokens.Palette = new[] { tokens.Accent, tokens.Positive };

        const string family = "Aptos, sans-serif";
        var theme = new VisualTheme(tokens, tokens, new VisualTypography(family));
        var chart = Chart.Create().WithTheme(theme, VisualThemeMode.Dark).AddLine("Values", Points(1, 3, 2));
        var canvas = VisualCanvas.Create(640, 360).WithTheme(theme, VisualThemeMode.Dark).AddText(20, 20, 300, "Shared brand", 24, tokens.Foreground);
        var topology = TopologyChart.Create().WithTheme(theme, VisualThemeMode.Dark).AddNode("node", "Node", 100, 100, status: TopologyHealthStatus.Healthy);

        var chartColors = chart.Options.Theme.Resolve(chart.Options.ThemeMode);
        var canvasColors = canvas.Theme.Resolve(canvas.ThemeMode);
        var topologyColors = topology.Theme.Resolve(topology.ThemeMode);
        Assert(chartColors.Palette[0].Equals(tokens.Accent) && chartColors.Positive.Equals(tokens.Positive) && chart.Options.Theme.Typography.Family == family, "Charts should receive the shared palette, semantic colors and typography.");
        Assert(canvasColors.Accent.Equals(tokens.Accent) && canvas.Theme.Typography.Family == family && canvas.BackgroundTop.Equals(tokens.Background), "VisualCanvas should receive the same palette and surface colors.");
        Assert(topologyColors.Accent.Equals(tokens.Accent) && topologyColors.Status.Pass.Fill.Equals(tokens.Positive) && topology.Theme.Typography.Family == family, "Topology should receive the same palette, semantic colors and typography.");
        Assert(canvas.ToSvg().Contains("font-family=\"Aptos, sans-serif\"", StringComparison.Ordinal), "VisualCanvas SVG should use its shared token font instead of a renderer-local font stack.");
        var topologyLabels = TopologyRoleTexts(topology.ToSvg(), "topology-node-label");
        Assert(topologyLabels.Length > 0 && System.Linq.Enumerable.All(topologyLabels, label => (string?)label.Attribute("font-family") == family), "Topology SVG should use the shared token font on rendered labels.");
    }
}
