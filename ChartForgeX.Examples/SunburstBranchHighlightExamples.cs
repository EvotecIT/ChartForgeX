using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Executable examples of Sunburst root-branch emphasis with native semantic paint.</summary>
public static class SunburstBranchHighlightExamples {
    /// <summary>Creates an allocation chart with repeated labels, a zero fact, numeric color and a warning hatch.</summary>
    /// <param name="width">The chart width in pixels.</param>
    /// <param name="dark">Whether to use dark colors.</param>
    /// <param name="graphite">Whether to use the Graphite look.</param>
    /// <returns>The chart model.</returns>
    public static Chart CreateChart(int width = 800, bool dark = false, bool graphite = true) {
        var chart = Chart.Create().WithSize(width, width < 500 ? 360 : 460).WithDataLabels()
            .WithTitle("Work allocation").WithSubtitle("Hover or focus a team to follow its branch; click to pin a reading")
            .WithTheme(graphite ? VisualTheme.Graphite() : VisualTheme.Report(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .AddSeries(ChartSeries.CreateSunburst(new ChartSeriesId("allocation"), "Allocated work", new[] {
                new ChartHierarchyItem("all", "All teams"),
                new ChartHierarchyItem("engineering", "Engineering", "all", colorValue: 12),
                new ChartHierarchyItem("operations", "Operations", "all", colorValue: 0),
                new ChartHierarchyItem("platform", "Platform", "engineering", 35, -4),
                new ChartHierarchyItem("engineering-support", "Support", "engineering", 25, 5),
                new ChartHierarchyItem("operations-support", "Support", "operations", 40),
                new ChartHierarchyItem("reserve", "Reserve", "operations", 0)
            }));
        chart.ConfigureSunburst(options => {
            options.ColorDimension.Name = "Change (%)";
            options.ColorScale = ChartColorScale.Diverging(ChartColor.FromHex("#8b7bd4"), ChartColor.FromHex("#b8b8b8"), ChartColor.FromHex("#dd8e59"), 0).WithValueRange(-10, 15);
        });
        chart.Series[0].WithNodeState("engineering-support", ChartSeriesState.Warning).WithPointFillPattern(4, ChartFillPattern.DiagonalForward);
        return chart;
    }

    /// <summary>Creates the interactive gallery page through the public HTML options.</summary>
    /// <param name="dark">Whether to use dark colors.</param>
    /// <returns>A complete interactive HTML page.</returns>
    public static string CreatePage(bool dark = false) => CreateChart(dark: dark).ToInteractiveHtmlPage(options => {
        options.Highlight.Mode = HtmlChartHighlightMode.RootBranch;
        options.Highlight.HighlightedItem.Stroke = ChartColor.FromHex("#38bdf8");
        options.Highlight.HighlightedItem.StrokeWidth = 2;
        options.Highlight.UnhighlightedBranch.Opacity = .22;
    });

    internal static void Write(string output) {
        foreach (var dark in new[] { false, true }) {
            var name = "sunburst-branch-highlight-" + (dark ? "dark" : "light");
            ExampleArtifactWriter.WriteText(Path.Combine(output, name + ".html"), CreatePage(dark));
            ExampleArtifactWriter.WriteText(Path.Combine(output, name + ".csharp.txt"), Source(dark));
        }
    }

    private static string Source(bool dark) => """
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

var chart = Chart.Create().WithSize(800, 460).WithTitle("Work allocation").WithDataLabels()
    .WithSubtitle("Hover or focus a team to follow its branch; click to pin a reading")
    .WithTheme(THEME)
    .AddSunburst("Allocated work", new[] {
        new ChartHierarchyItem("all", "All teams"),
        new ChartHierarchyItem("engineering", "Engineering", "all", colorValue: 12),
        new ChartHierarchyItem("operations", "Operations", "all", colorValue: 0),
        new ChartHierarchyItem("platform", "Platform", "engineering", 35, -4),
        new ChartHierarchyItem("engineering-support", "Support", "engineering", 25, 5),
        new ChartHierarchyItem("operations-support", "Support", "operations", 40),
        new ChartHierarchyItem("reserve", "Reserve", "operations", 0)
    });
chart.ConfigureSunburst(options => {
    options.ColorDimension.Name = "Change (%)";
    options.ColorScale = ChartColorScale.Diverging(ChartColor.FromHex("#8b7bd4"), ChartColor.FromHex("#b8b8b8"), ChartColor.FromHex("#dd8e59"), 0).WithValueRange(-10, 15);
});
chart.Series[0].WithNodeState("engineering-support", ChartSeriesState.Warning).WithPointFillPattern(4, ChartFillPattern.DiagonalForward);
chart.SaveInteractiveHtml("work-allocation.html", options => {
    options.Highlight.Mode = HtmlChartHighlightMode.RootBranch;
    options.Highlight.HighlightedItem.Stroke = ChartColor.FromHex("#38bdf8");
    options.Highlight.HighlightedItem.StrokeWidth = 2;
    options.Highlight.UnhighlightedBranch.Opacity = .22;
});
""".Replace("THEME", dark ? "VisualTheme.Graphite()" : "VisualTheme.Graphite()");
}
