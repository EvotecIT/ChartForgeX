using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;

public static partial class V2Examples {
    private const string InteractiveAssetFolder = "interactive";
    private static readonly HtmlAssetFile[] InteractiveAssets = {
        HtmlInteractiveAssetFiles.ChartStyle, HtmlInteractiveAssetFiles.ChartScript, HtmlInteractiveAssetFiles.NativeTextScript
    };

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
            if (id.StartsWith("family-line-wide-", StringComparison.Ordinal))
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
