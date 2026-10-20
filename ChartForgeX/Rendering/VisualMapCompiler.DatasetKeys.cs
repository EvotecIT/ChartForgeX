using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    /// <summary>Measures independent map color keys through the common numeric key scene without changing their domains.</summary>
    private static ChartRect DatasetKeys(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        VisualThemeColors colors, IReadOnlyList<MapDataset> datasets) {
        var keys = datasets.Where(data => data.ResolveKeyScale(colors) != null && data.Presentation.ShowLegend).ToArray();
        if (!context.Frame.ShowLegend || keys.Length == 0) return plot;
        var style = TickStyle(chart, context); var gap = context.Theme.Spacing / 2;
        var right = chart.Options.MapScaleLegendPosition == ChartMapScaleLegendPosition.Right;
        var width = right ? Math.Min(plot.Width * .42, Math.Max(160, keys.Max(data => builder.MeasureText(data.Presentation.Name, style).Width) + gap * 4)) : plot.Width;
        var columns = !right && width >= 600 ? Math.Min(2, keys.Length) : 1;
        var columnGap = gap * 3;
        var keyWidth = (width - columnGap * (columns - 1)) / columns;
        var heights = keys.Select(data => VisualColorScaleLegendScene.Height(builder, data.ResolveKeyScale(colors)!, data.Presentation.Labels, style, gap,
            keyWidth, double.MaxValue, data.Missing, data.Presentation.ScaleValues, data.HasDomain, compact: true, min: data.Minimum, max: data.Maximum,
            heading: data.Series.Name + " · " + data.Presentation.Name, legendStyle: data.Presentation.LegendStyle)).ToArray();
        var rows = (int)Math.Ceiling(keys.Length / (double)columns);
        var rowHeights = Enumerable.Range(0, rows).Select(row => heights.Skip(row * columns).Take(columns).Max()).ToArray();
        var requested = rowHeights.Sum() + gap * Math.Max(0, rows - 1);
        var height = right ? plot.Height : Math.Min(requested, plot.Height * .45);
        if (height + .000001 < requested) builder.AddDiagnostic(new VisualDiagnostic("map.color-scale-overflow", "Independent map keys exceed the legend viewport; complete scale facts remain in descriptive regions."));
        var area = right ? new ChartRect(plot.Right - width, plot.Top, width, height) : new ChartRect(plot.Left, plot.Bottom - height, width, height);
        var y = area.Top;
        for (var row = 0; row < rows; row++) {
            var available = Math.Min(Math.Max(0, area.Bottom - y), right ? rowHeights[row] : requested > 0 ? rowHeights[row] * height / requested : 0);
            for (var column = 0; column < columns; column++) {
                var ordinal = row * columns + column;
                if (ordinal >= keys.Length) break;
                var data = keys[ordinal]; var prefix = "map-scale-" + N(data.Index);
                var bounds = new ChartRect(area.Left + column * (keyWidth + columnGap), y, keyWidth, available);
                using (builder.PushGroup(prefix + "-source", "map-color-scale-source", new Dictionary<string, string> {
                    ["data-cfx-series"] = N(data.Index), ["data-cfx-series-key"] = data.Series.InteractionIdentityKey,
                    ["data-cfx-label-color"] = data.Presentation.Name, ["data-cfx-label"] = data.Series.Name + " · " + data.Presentation.Name
                })) VisualColorScaleLegendScene.Draw(builder, data.ResolveKeyScale(colors)!, data.Presentation.Labels, style, gap, bounds, data.Minimum, data.Maximum,
                    prefix, data.Missing, ChartColorScaleSurface.NoData(data.ResolveKeyScale(colors), colors), data.Presentation.ScaleValues,
                    "map.color-scale-overflow", data.HasDomain, compact: true, heading: data.Series.Name + " · " + data.Presentation.Name,
                    valueName: data.Presentation.Name, legendStyle: data.Presentation.LegendStyle);
            }
            y += available + gap;
        }
        return right ? new ChartRect(plot.Left, plot.Top, Math.Max(0, plot.Width - width - gap), plot.Height)
            : new ChartRect(plot.Left, plot.Top, plot.Width, Math.Max(0, plot.Height - height - gap));
    }
}
