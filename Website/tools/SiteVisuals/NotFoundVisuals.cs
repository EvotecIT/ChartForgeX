// The not-found page: an empty chart over the routes that do have pages, with the current route highlighted.
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace SiteVisuals;

internal static class NotFoundVisuals {
    public static Dictionary<string, object> Write(VisualOutput output) {
        Chart Empty(ThemeMode mode) => Chart.Create()
            .ThemeMode(mode)
            .TransparentBackground()
            .ShowCard(false)
            .ShowHeader(false)
            .Legend(false)
            .Size(560, 300)
            .YAxis(y => y.Title("Pages found").Bounds(0, 100))
            .XLabels("/docs/", "/gallery/", "/api/", "this page")
            .HighlightXAxisLabel(4)
            .AddBar("Pages found", new[] { new ChartPoint(1, 0), new ChartPoint(2, 0), new ChartPoint(3, 0), new ChartPoint(4, 0) });

        return output.Pair("pages/not-found", (mode, _) => Empty(mode).ToSvg("cfx-not-found"));
    }
}
