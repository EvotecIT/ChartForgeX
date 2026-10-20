using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    /// <summary>Allocates optional map weight keys through the same circle measurement and painting owner used by Bubble.</summary>
    private static ChartRect SizeKeyViewport(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        IReadOnlyList<MapDataset> datasets, out List<(MapDataset Data, VisualMarkerSizeKeyScene Key)> keys) {
        keys = new List<(MapDataset, VisualMarkerSizeKeyScene)>();
        if (!context.Frame.ShowLegend) return plot;
        var style = TickStyle(chart, context); var gap = context.Theme.Spacing / 2;
        foreach (var data in datasets.Where(data => data.SizeScale?.HasValues == true && data.SizeScale.Presentation.ShowLegend)) {
            var key = VisualMarkerSizeKeyScene.Measure(builder, data.SizeScale!, style, gap, plot, compact: true, prefix: "map-size-key-" + N(data.Index));
            keys.Add((data, key));
        }
        var height = keys.Sum(item => item.Key.Height + gap);
        if (height > plot.Height * .4) {
            foreach (var item in keys) item.Key.Omit();
            builder.AddDiagnostic(new VisualDiagnostic("map.size-scale-overflow", "Complete map size references do not fit the remaining legend budget; their source facts remain in descriptive regions."));
            return plot;
        }
        var y = plot.Bottom - height;
        foreach (var item in keys) {
            item.Key.Bounds = new ChartRect(plot.Left, y, plot.Width, item.Key.Height);
            if (!item.Key.Fits) {
                item.Key.Omit();
                builder.AddDiagnostic(new VisualDiagnostic("map.size-scale-overflow", "Complete map size circles and captions do not fit the viewport; their source facts remain in descriptive regions."));
            }
            y += item.Key.Bounds.Height + gap;
        }
        return new ChartRect(plot.Left, plot.Top, plot.Width, Math.Max(0, plot.Height - height));
    }
}
