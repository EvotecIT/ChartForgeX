using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void WaterfallHonorsAxesVisibility() {
        var compact = WaterfallSample().WithAxes(false);
        var compactScene = compact.Prepare(VisualExportRequest.ForChart(compact).Context).Scene;
        Assert(compactScene.Nodes.Count(node => node.Role == "waterfall-bar") == 5, "Disabling axes must preserve every waterfall step and its derived total.");
        Assert(!compactScene.Nodes.Any(node => node.Role?.StartsWith("axis-", System.StringComparison.Ordinal) == true), "Disabling axes must suppress both value and category axes.");
        Assert(compact.ToPng().Length > 64, "Compact waterfall charts must render native PNG output.");

        var full = WaterfallSample();
        var fullScene = full.Prepare(VisualExportRequest.ForChart(full).Context).Scene;
        Assert(fullScene.Nodes.Any(node => node.Role == "axis-x-label"), "Waterfall categories should render by default.");
        Assert(fullScene.Nodes.Any(node => node.Role == "axis-y-label"), "Waterfall value ticks should render by default.");
        Assert(fullScene.Regions.Any(region => region.Role == "axis-x-label" && region.Label?.StartsWith("Total", System.StringComparison.Ordinal) == true), "The derived total must keep its category label.");

        foreach (var hideX in new[] { false, true }) {
            var chart = WaterfallSample().WithLegend(false).WithTickLabelStyle(style => style.WithColor("#00FFFF"));
            chart.Options.XAxis.Visible = !hideX;
            chart.Options.YAxis.Visible = hideX;
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            Assert(!prepared.Scene.Nodes.Any(node => node.Role == (hideX ? "axis-x-label" : "axis-y-label")), "Each waterfall axis must hide independently.");
            Assert(prepared.Scene.Nodes.Any(node => node.Role == (hideX ? "axis-y-label" : "axis-x-label")), "Hiding one axis must keep the other axis labels.");
            var pixels = ReadPngRgba(chart.ToPng(), out var width, out var height);
            Assert(CountNearColorInRect(pixels, width, 0, 0, width, height, 0, 255, 255, 80) > 0, "The remaining tick labels must also appear in native PNG output.");
        }

        var independentTicks = WaterfallSample();
        independentTicks.Options.XAxis.TickCount = 2;
        independentTicks.Options.YAxis.TickCount = 10;
        var ticks = independentTicks.Prepare(VisualExportRequest.ForChart(independentTicks).Context).Scene;
        Assert(ticks.Nodes.Count(node => node.Role == "axis-y-label") > 2, "Value ticks must honor their own density independently of categorical ticks.");

        var cramped = Chart.Create().WithSize(420, 220).WithLegend(false).WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Inside)
            .WithDataLabelStyle(style => style.WithColor("#FF00FF").WithFontSize(72))
            .AddWaterfall("Delta", Points(.01, 100, -25));
        cramped.Options.YAxis.Visible = false;
        var withLabels = cramped.Prepare(VisualExportRequest.ForChart(cramped).Context).Scene;
        var withoutLabels = cramped.WithDataLabels(false).Prepare(VisualExportRequest.ForChart(cramped).Context).Scene;
        string[] Categories(VisualScene scene) => scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "axis-x-label")
            .SelectMany(node => node.Text.Lines.Select(line => line.Text)).ToArray();
        Assert(Categories(withLabels).SequenceEqual(Categories(withoutLabels)), "An oversized inside data label must not suppress an independent category label.");
    }

    private static Chart WaterfallSample() => Chart.Create().WithSize(560, 320).WithXAxis("Stage")
        .AddWaterfall("Delta", Points(18, -42, -12, 9));
}
