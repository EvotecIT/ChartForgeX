// The not-found page: an empty chart over the routes that do have pages, with the current route highlighted.
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace SiteVisuals;

internal static class NotFoundVisuals {
    public static Dictionary<string, object> Write(VisualOutput output) {
        Chart Empty(VisualDesignTokens tokens) => Chart.Create()
            .WithDesignTokens(tokens)
            .WithTransparentBackground()
            .WithCard(false)
            .WithHeader(false)
            .WithLegend(false)
            .WithSize(560, 300)
            .WithYAxis("Pages found")
            .WithYAxisBounds(0, 100)
            .WithXLabels("/docs/", "/gallery/", "/api/", "this page")
            .WithHighlightedXAxisLabel(4)
            .AddBar("Pages found", new[] { new ChartPoint(1, 0), new ChartPoint(2, 0), new ChartPoint(3, 0), new ChartPoint(4, 0) });

        return output.Pair("pages/not-found", (tokens, _) => Empty(tokens).ToSvg("cfx-not-found"));
    }
}
