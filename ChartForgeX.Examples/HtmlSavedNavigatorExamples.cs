using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;

/// <summary>Executable example of the X navigator in a saved offline report, with no host connection.</summary>
public static class HtmlSavedNavigatorExamples {
    /// <summary>Creates a self-contained page whose navigator, presets, scrollbar and reset work offline.</summary>
    /// <param name="dark">Whether to use the dark Graphite palette.</param>
    /// <returns>A complete interactive HTML page using the standalone reader.</returns>
    public static string CreatePage(bool dark = false) {
        var first = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var readings = Enumerable.Range(0, 24 * 90).Select(hour => new ChartPoint(first.AddHours(hour),
            120 + 40 * Math.Sin(hour / 24.0 * Math.PI * 2) + 25 * Math.Sin(hour / 700.0))).ToArray();
        var chart = Chart.Create().Size(900, 440).Title("Domain controller replication latency")
            .Subtitle("Hourly readings for one quarter; use the range controls below the chart")
            .Theme(VisualTheme.Graphite()).ThemeMode(dark ? ThemeMode.Dark : ThemeMode.Light)
            .XAxis(x => x.TimeScale()).YAxis(y => y.Bounds(0, 220).Title("Latency (ms)")).DataLabels(false)
            .AddLine("Latency", readings);
        var issues = HtmlChartXNavigationSupport.DiagnoseStandalone(chart);
        if (issues.Count != 0) throw new InvalidOperationException("The saved navigator example must be supported: " + string.Join("; ", issues));
        return chart.ToInteractiveHtmlPage(options => {
            options.Runtime = HtmlChartRuntime.Standalone;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.KeyboardNavigation
                | ChartInteractionFeatures.Crosshair | ChartInteractionFeatures.Export;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.XNavigation.Enabled = true;
            options.XNavigation.RangePresets.Add(HtmlChartXRangePreset.FromDuration("1D", TimeSpan.FromDays(1)));
            options.XNavigation.RangePresets.Add(HtmlChartXRangePreset.FromDuration("1W", TimeSpan.FromDays(7)));
            options.XNavigation.RangePresets.Add(HtmlChartXRangePreset.FromDuration("1M", TimeSpan.FromDays(30)));
        });
    }
}
