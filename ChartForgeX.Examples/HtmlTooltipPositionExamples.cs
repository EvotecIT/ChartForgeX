using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;

/// <summary>Executable HTML examples of pointer, native-target and chart-stage tooltip placement.</summary>
public static class HtmlTooltipPositionExamples {
    /// <summary>Creates a self-contained page whose tooltip policy is configured through the public HTML adapter.</summary>
    /// <param name="anchor">The screen geometry used by the readout.</param>
    /// <param name="dark">Whether to use the dark Graphite palette.</param>
    /// <returns>A complete interactive HTML page.</returns>
    public static string CreatePage(HtmlChartTooltipAnchor anchor, bool dark = false) {
        var chart = Chart.Create().WithSize(800, 440).WithTitle(anchor + " tooltip placement")
            .WithSubtitle("Hover or focus a reading; click to retain it while resizing the chart")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddScatter("Processing time", new[] { new ChartPoint(1, 8), new ChartPoint(5, 5), new ChartPoint(9, 2) });
        return chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.Position.Anchor = anchor;
            options.Tooltip.Position.Placements = anchor == HtmlChartTooltipAnchor.Chart
                ? new[] { HtmlChartTooltipPlacement.Top, HtmlChartTooltipPlacement.Bottom }
                : new[] { HtmlChartTooltipPlacement.TopRight, HtmlChartTooltipPlacement.BottomRight, HtmlChartTooltipPlacement.BottomLeft, HtmlChartTooltipPlacement.TopLeft };
            options.Tooltip.Position.Gap = 12;
            options.Tooltip.Position.OffsetX = 6;
            options.Tooltip.Position.OffsetY = -4;
        });
    }
}
