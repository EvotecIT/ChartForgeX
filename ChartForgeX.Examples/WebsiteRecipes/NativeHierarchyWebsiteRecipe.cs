#region imports
// Uses post-3.0.1 hierarchy-view APIs. Build current source or use a later package containing them.
// Exact documentation typography uses Carlito regular/bold installed or registered on the host; other hosts retain normal font fallback.
using System;
using System.IO;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
#endregion

/// <summary>Full and subtree preparations reuse the existing shared allocation recipe without editing its source items.</summary>
public static class NativeHierarchyWebsiteRecipe {
    /// <summary>Creates the full source model for either supported area-hierarchy family.</summary>
    public static Chart Create(ChartSeriesKind kind, ThemeMode mode = ThemeMode.Light) => CreateChart(kind, mode);

    /// <summary>Captures one native full or existing-root view; null restores the full hierarchy.</summary>
    public static PreparedVisual Prepare(ChartSeriesKind kind, ThemeMode mode = ThemeMode.Light, string? rootId = null) {
        var chart = CreateChart(kind, mode);
        return chart.PrepareHierarchy(Context(mode), rootId == null ? null : new ChartHierarchyView(rootId));
    }

    /// <summary>Writes both families and both views as ordinary SVG and native PNG files.</summary>
    public static void Save(ThemeMode mode = ThemeMode.Light) {
#region views
        foreach (var kind in new[] { ChartSeriesKind.Treemap, ChartSeriesKind.Sunburst }) {
            var chart = CreateChart(kind, mode);
            foreach (var rootId in new string?[] { null, "engineering" }) {
                var id = kind.ToString().ToLowerInvariant() + "-" + (rootId ?? "full");
                var prepared = chart.PrepareHierarchy(Context(mode), rootId == null ? null : new ChartHierarchyView(rootId));
                File.WriteAllText(id + ".svg", prepared.ToSvg(id));
                File.WriteAllBytes(id + ".png", prepared.ToPng());
            }
        }
#endregion
    }

#region helpers
    static Chart CreateChart(ChartSeriesKind kind, ThemeMode mode) {
        if (kind is not (ChartSeriesKind.Treemap or ChartSeriesKind.Sunburst)) throw new ArgumentOutOfRangeException(nameof(kind));
        // This existing recipe owns every ID, parent and value. Its null group values are valid for both families.
        var sunburst = SunburstRecipe.Create("wide", mode).Size(800, 505);
        if (kind == ChartSeriesKind.Sunburst) return sunburst.Sunburst(options => {
            options.SectorGap = 6;
            options.MinimumLabelFontSize = 10;
            options.CornerRadius = 4;
        });
        var source = sunburst.Series[0];
        var treemap = ChartSeries.CreateTreemap(source.Id, source.Name, source.HierarchyItems);
        treemap.PointColors.AddRange(source.PointColors);
        treemap.PointFillPatterns.AddRange(source.PointFillPatterns);
        foreach (var state in source.NodeStates) treemap.NodeState(state.Key, state.Value);
        return Chart.Create().Size(800, 505).DataLabels().AddSeries(treemap)
            .ValueFormat(sunburst.Options.ValueFormat);
    }

    static VisualRenderContext Context(ThemeMode mode) => new(
        new VisualLayoutOptions(new VisualSize(800, 505)), VisualTheme.Graphite(), mode,
        new VisualFrame("Team allocation", "Same source hierarchy; full view or Engineering as the visible root", showLegend: false),
        FontSpec.FromFamily("Carlito"));
#endregion
}
