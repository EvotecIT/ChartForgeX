// The gallery masthead chart: how many examples each category holds, read from data/gallery.json.
using System.Text.Json;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace SiteVisuals;

internal static class GalleryVisuals {
    public static Dictionary<string, object> Write(VisualOutput output) {
        using var gallery = JsonDocument.Parse(File.ReadAllText(Path.Combine(output.Website, "data", "gallery.json")));
        var categories = gallery.RootElement.GetProperty("categories").EnumerateArray()
            .Select(category => (Id: category.GetProperty("id").GetString()!, Label: category.GetProperty("label").GetString()!))
            .ToArray();
        var counts = categories.Select(category => (double)gallery.RootElement.GetProperty("items").EnumerateArray()
            .Count(item => item.GetProperty("category").GetString() == category.Id)).ToArray();

        // One stacked series per category, each with a single value on its own row, so every bar takes its category's
        // legend colour. Keys past six are hollow on the page; their bars reuse the first series colours.
        // Horizontal bars stack from the bottom, so rows are listed in reverse to read in filter order from the top.
        var rows = categories.Length;
        Chart Composition(VisualThemeMode mode) {
            var palette = VisualTheme.Graphite().Resolve(mode).Palette;
            var chart = Chart.Create()
                .WithThemeMode(mode)
                .WithTransparentBackground()
                .WithCard(false)
                .WithHeader(false)
                .WithLegend(false)
                .WithSize(560, 300)
                .WithStackedBars()
                .WithStackTotals()
                .WithXLabels(Enumerable.Reverse(categories).Select(category => category.Label).ToArray());
            for (var index = 0; index < rows; index++) {
                var row = rows - index;
                var points = new[] { new ChartPoint(row, counts[index]) };
                chart.AddHorizontalBar(categories[index].Label, points, palette[index % 6]);
            }

            return chart;
        }
        var entry = output.Pair("gallery/categories", (mode, _) => Composition(mode).ToSvg("cfx-gallery-categories"));
        entry["total"] = counts.Sum();
        return entry;
    }
}
