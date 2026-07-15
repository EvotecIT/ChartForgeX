using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void WaterfallHonorsAxesVisibility() {
        var compact = WaterfallSample()
            .WithAxes(false)
            .ToSvg();

        Assert(compact.Contains("data-cfx-role=\"waterfall-bar\"", System.StringComparison.Ordinal), "Waterfall bars should still render when axes are disabled.");
        Assert(!compact.Contains("data-cfx-role=\"waterfall-x-axis-label\"", System.StringComparison.Ordinal), "Waterfall x-axis labels should hide when axes are disabled.");
        Assert(!compact.Contains("data-cfx-role=\"waterfall-y-axis-label\"", System.StringComparison.Ordinal), "Waterfall y-axis labels should hide when axes are disabled.");
        Assert(!compact.Contains("data-cfx-role=\"waterfall-x-axis-title\"", System.StringComparison.Ordinal), "Waterfall x-axis titles should hide when axes are disabled.");
        Assert(!compact.Contains("data-cfx-role=\"waterfall-zero-axis\"", System.StringComparison.Ordinal), "Waterfall zero axis should hide when axes are disabled.");

        var full = WaterfallSample().ToSvg();
        Assert(full.Contains("data-cfx-role=\"waterfall-x-axis-label\"", System.StringComparison.Ordinal), "Waterfall x-axis labels should render by default.");
        Assert(full.Contains("data-cfx-role=\"waterfall-zero-axis\"", System.StringComparison.Ordinal), "Waterfall zero axis should render by default when in range.");
        Assert(WaterfallSample().WithAxes(false).ToPng().Length > 64, "Compact waterfall options should render valid PNG output.");

        var theme = ChartTheme.ReportLight();
        theme.MutedText = ChartColor.FromHex("#00FFFF");
        var yHidden = Chart.Create()
            .WithSize(560, 320)
            .WithTheme(theme)
            .WithLegend(false)
            .AddWaterfall("Delta", Points(18, -42, -12, 9));
        yHidden.Options.YAxis.Visible = false;
        var yHiddenPixels = ReadPngRgba(yHidden.ToPng(), out var width, out var height);
        var xLabelRegionLeft = width / 4;
        var xLabelRegionTop = height / 2;
        Assert(CountNearColorInRect(yHiddenPixels, width, xLabelRegionLeft, xLabelRegionTop, width - xLabelRegionLeft, height - xLabelRegionTop, 0, 255, 255, 80) > 0, "Hiding the PNG waterfall y-axis should keep visible x-axis category labels.");
        Assert(CountNearColorInRect(yHiddenPixels, width, 0, 0, width / 5, xLabelRegionTop, 0, 255, 255, 80) == 0, "Hiding the PNG waterfall y-axis should suppress its tick labels.");

        var xHidden = Chart.Create()
            .WithSize(560, 320)
            .WithTheme(theme)
            .WithLegend(false)
            .AddWaterfall("Delta", Points(18, -42, -12, 9));
        xHidden.Options.XAxis.Visible = false;
        var xHiddenPixels = ReadPngRgba(xHidden.ToPng(), out _, out _);
        Assert(CountNearColorInRect(xHiddenPixels, width, xLabelRegionLeft, xLabelRegionTop, width - xLabelRegionLeft, height - xLabelRegionTop, 0, 255, 255, 80) == 0, "Hiding the PNG waterfall x-axis should suppress bottom category labels while leaving the y-axis independent.");

        var independentTicks = WaterfallSample();
        independentTicks.Options.XAxis.TickCount = 2;
        independentTicks.Options.YAxis.TickCount = 10;
        Assert(CountOccurrences(independentTicks.ToSvg(), "data-cfx-role=\"waterfall-y-axis-label\"") > 2, "Waterfall value ticks should use the y-axis tick count independently from the category x-axis.");
        Assert(independentTicks.ToPng().Length > 64, "Waterfall y-axis tick counts should render through the PNG path.");
    }

    private static Chart WaterfallSample() => Chart.Create()
        .WithSize(560, 320)
        .WithXAxis("Stage")
        .AddWaterfall("Delta", Points(18, -42, -12, 9));
}
