using System;
using System.IO;
using System.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Provides the optional browser renderer for category and time-series data.
/// Hosts load this script on demand and call <c>ChartForgeXRenderer.create</c>.
/// </summary>
public static class HtmlDataChartAssets {
    private static readonly Lazy<string> ScriptResource = new(() => {
        using var stream = typeof(HtmlDataChartAssets).Assembly.GetManifestResourceStream(
            "ChartForgeX.Interactivity.Html.Assets.cfx-renderer.js")
            ?? throw new InvalidOperationException("The browser data renderer resource is missing.");
        using var reader = new StreamReader(stream);
        // Browser projections consume the same surface policy as native chart layout.
        // Ship ScriptFile, which contains this resolved policy, rather than the resource template.
        var theme = VisualTheme.Graphite();
        var palette = string.Join(",", theme.Resolve(VisualThemeMode.Light).Palette.Select(color =>
            "\"#" + color.R.ToString("x2") + color.G.ToString("x2") + color.B.ToString("x2") + "\""));
        var policy = FormattableString.Invariant($$"""
            {"bandFill":{{ChartMarkSurface.BandFill}},"gap":{{ChartMarkSurface.SurfaceGap}},"separator":{{ChartMarkSurface.StackSeparator}},"minimumSegment":{{ChartMarkSurface.MinimumSegmentExtent}},"columnMaximum":{{theme.MaximumBarWidth}},"barMaximum":{{ChartMarkSurface.MaximumThickness(theme.MaximumBarWidth, true)}},"palette":[{{palette}}],"lineWidth":{{theme.SeriesStrokeWidth}},"markerRadius":{{theme.MarkerRadius}},"areaOpacity":{{theme.AreaOpacity}}}
            """);
        return reader.ReadToEnd().Replace("__CFX_NATIVE_SURFACE__", policy);
    });
    private static readonly Lazy<HtmlAssetFile> ScriptAsset = new(() => new HtmlAssetFile("cfx-renderer", "js", Script));

    /// <summary>
    /// Gets the standalone script for line, area, bar, column, stacked, win/loss and bullet charts.
    /// The renderer accepts data descriptors and supports updates, selection and explicit disposal.
    /// </summary>
    public static string Script => ScriptResource.Value;

    /// <summary>Gets the renderer as a content-addressed shared JavaScript asset.</summary>
    public static HtmlAssetFile ScriptFile => ScriptAsset.Value;
}
