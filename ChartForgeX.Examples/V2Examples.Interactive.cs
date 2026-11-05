using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;

public static partial class V2Examples {
    private const string InteractiveAssetFolder = "interactive";
    // Pages fetch the feature bundles they declare from the same folder.
    private static readonly HtmlAssetFile[] InteractiveAssets = new[] {
        HtmlInteractiveAssetFiles.ChartStyle, HtmlInteractiveAssetFiles.ChartScript, HtmlInteractiveAssetFiles.NativeTextScript
    }.Concat(HtmlInteractiveAssetFiles.ChartFeatureScripts).ToArray();

    private static bool IsScenarioVariant(string variant) => variant is "latency-week" or "issuers" or "licence-waste" or "availability";

    /// <summary>Writes the gallery's live page: the same chart with tooltips, hover and legend toggles, sharing one runtime copy.</summary>
    private static void WriteInteractive(string output, Chart chart, string id, string title, string subtitle, VisualThemeMode mode, int width, int height, bool? legend) {
        chart.WithTitle(title).WithSubtitle(subtitle).WithTheme(VisualTheme.Graphite(), mode).WithSize(width, height);
        if (legend.HasValue) chart.WithLegend(legend.Value);
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".interactive.html"), chart.ToInteractiveHtmlPage(options => {
            options.PageTitle = title;
            // The gallery shows these pages in narrow frames: scale the whole chart and keep the toolbar small.
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Toolbar = HtmlChartToolbarLayout.Compact;
            // Every live page offers zoom, pan, brush, export and the chart menu; charts a saved page cannot re-project
            // keep their zoom controls disabled. The primary Line page also carries the range navigator.
            options.Interaction.Features = ChartInteractionFeatures.ReportReview | ChartInteractionFeatures.Zoom
                | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export;
            options.IncludeContextMenu = true;
            if (id.StartsWith("family-line-wide-", StringComparison.Ordinal)) options.XNavigation.Enabled = true;
            options.ExternalAssets = new HtmlAssetReferences(InteractiveAssetFolder + "/");
        }));
    }

    /// <summary>Writes a chart grid's live page: each panel as an interactive chart in the grid's columns, sharing one runtime copy.</summary>
    private static void WriteInteractiveGrid(string output, ChartGrid grid, string id, string title, VisualThemeMode mode, int width) {
        // Two columns at the gallery width keep each panel readable instead of stacking full-size charts down the page.
        var columns = Math.Max(1, Math.Min(2, grid.Charts.Count));
        var panelWidth = Math.Max(280, (width - 16 * (columns - 1)) / columns - 34);
        var charts = grid.Charts.Select(chart => chart.WithTheme(VisualTheme.Graphite(), mode).WithSize(panelWidth, (int)(panelWidth * 0.62))).ToArray();
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".interactive.html"), charts.ToInteractiveHtmlDashboardPage(options => {
            options.PageTitle = title;
            options.Columns = columns;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Toolbar = HtmlChartToolbarLayout.Compact;
            options.Interaction.Features = ChartInteractionFeatures.ReportReview | ChartInteractionFeatures.Zoom
                | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush;
            options.ExternalAssets = new HtmlAssetReferences(InteractiveAssetFolder + "/");
        }));
    }

    /// <summary>Writes a diagram's live page: pan, zoom and node details with its runtime inlined (topology has no shared-asset option).</summary>
    private static void WriteInteractiveTopology(string output, ChartForgeX.Topology.TopologyChart chart, string id, string title, string subtitle, VisualThemeMode mode) {
        chart.WithTitle(title).WithSubtitle(subtitle).WithTheme(VisualTheme.Graphite(), mode);
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".interactive.html"), chart.ToInteractiveHtmlPage(chart.DefaultRenderOptions));
    }

    private static IEnumerable<string> WriteInteractiveAssets(string output) {
        HtmlInteractiveAssetFiles.WriteTo(Path.Combine(output, InteractiveAssetFolder), InteractiveAssets);
        return InteractiveAssets.Select(file => InteractiveAssetFolder + "/" + file.FileName);
    }
}
