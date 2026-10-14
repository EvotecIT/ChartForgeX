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

        // Horizontal bars stack from the bottom, so reverse the categories to read in filter order from the top.
        Array.Reverse(categories);
        Array.Reverse(counts);

        Chart Composition(VisualDesignTokens tokens) => Chart.Create()
            .WithDesignTokens(tokens)
            .WithTransparentBackground()
            .WithCard(false)
            .WithHeader(false)
            .WithLegend(false)
            .WithSize(560, 300)
            .WithXLabels(categories.Select(category => category.Label).ToArray())
            .WithDataLabels()
            .AddHorizontalBar("Examples", counts.Select((count, index) => new ChartPoint(index + 1, count)).ToArray());

        var entry = output.Pair("gallery/categories", (tokens, _) => Composition(tokens).ToSvg("cfx-gallery-categories"));
        entry["total"] = counts.Sum();
        return entry;
    }
}
