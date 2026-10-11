using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Executable HTML examples of typed crosshair line paint.</summary>
public static class HtmlCrosshairExamples {
    /// <summary>Creates an interactive page with explicit line paint and the existing pointer readout behavior.</summary>
    /// <param name="lineStyle">The pattern used by both guide lines.</param>
    /// <param name="dark">Whether to use the dark Graphite palette.</param>
    /// <returns>A complete interactive HTML page.</returns>
    public static string CreatePage(HtmlChartCrosshairLineStyle lineStyle, bool dark = false) {
        var chart = Chart.Create().WithSize(800, 440).WithTitle(lineStyle + " crosshair lines")
            .WithSubtitle("Hover a reading or nearby plot space to see the guide")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddScatter("Processing time", new[] { new ChartPoint(2, 7), new ChartPoint(5, 5), new ChartPoint(8, 3) });
        return chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Crosshair.Stroke = dark ? ChartColor.FromRgb(125, 211, 252) : ChartColor.FromRgb(14, 116, 144);
            options.Crosshair.StrokeWidth = 2;
            options.Crosshair.LineStyle = lineStyle;
        });
    }
}
