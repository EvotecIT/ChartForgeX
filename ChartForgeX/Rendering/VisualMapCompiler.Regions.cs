using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static void Regions(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors, double min, double max) {
        var definition = chart.Options.RegionMapDefinition ?? throw new InvalidOperationException("A region map requires a geometry definition.");
        var source = chart.Options.RegionMapBounds ?? definition.Bounds;
        if (!Finite(source.Left) || !Finite(source.Top) || !Finite(source.Width) || !Finite(source.Height) || source.Width <= 0 || source.Height <= 0)
            throw new InvalidOperationException("Region map bounds must be finite and have positive dimensions.");
        var width = chart.Options.MapRegionStrokeWidth;
        var map = Fit(plot, source.Width, source.Height, width / 2);
        var values = Values(chart, key => definition.TryResolveRegion(key, out var code) ? code : null);
        using (builder.PushGroup("region-map", "region-map", new Dictionary<string, string> {
            ["data-cfx-map-id"] = definition.Id, ["data-cfx-source-left"] = N(source.Left), ["data-cfx-source-top"] = N(source.Top),
            ["data-cfx-source-width"] = N(source.Width), ["data-cfx-source-height"] = N(source.Height), ["data-cfx-region-count"] = N(definition.Regions.Count),
            ["data-cfx-filled-region-count"] = N(values.Count), ["data-cfx-missing-region-count"] = N(definition.Regions.Count - values.Count)
        })) using (builder.PushClip(map)) {
            Surface(chart, context, builder, map, colors, "region-map-surface");
            Layers(chart.Options.MapBaseLayers, builder, source, map, "base");
            foreach (var region in definition.Regions) {
                var paths = ChartMapPathParser.ParseSubpaths(region.Path, map.Width / source.Width);
                var projected = paths.Select(path => (IReadOnlyList<ChartPoint>)path.Points.Select(point => Project(point, source, map)).ToArray()).ToArray();
                var geometry = Path(projected); var bounds = Intersect(Bounds(projected.SelectMany(path => path)), map);
                int? index = values.TryGetValue(region.Code, out var found) ? found : (int?)null;
                var full = Formatted(chart, index); var id = index.HasValue ? "series-0-point-" + index.Value : "region-map-empty-" + region.Code;
                var fill = Fill(chart, colors, index, min, max);
                builder.AddRegion(new VisualSemanticRegion(id, "region-map-region", bounds, region.Name + ": " + full));
                using (builder.PushGroup(id, "region-map-region-source", Source(chart, region.Code, region.Name, index, index.HasValue ? chart.Series[0].Points[index.Value].Y : (double?)null, full, min, max))) {
                    builder.Path(geometry, fill, chart.Options.MapRegionStrokeColor ?? colors.Surface, width, "region-map-region", close: true);
                    var pattern = Pattern(chart.Series[0], index);
                    if (index.HasValue && pattern != ChartFillPattern.None) builder.Pattern(geometry, pattern, ChartColorMath.AccessibleTextOnBackground(fill).WithAlpha(90), role: "region-map-pattern");
                    if (chart.Options.ShowMapLabels && bounds.Width > 0 && bounds.Height > 0) {
                        var anchor = region.HasLabel ? Project(region.Label, source, map) : new ChartPoint(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
                        var area = Intersect(new ChartRect(anchor.X - bounds.Width / 2, anchor.Y - bounds.Height / 2, bounds.Width, bounds.Height), bounds);
                        VisualRadialPrimitives.Text(builder, region.Code, area, TickStyle(chart, context, ChartColorMath.AccessibleTextOnBackground(fill)), "region-map-label", id + "-label");
                    }
                }
            }
            Layers(chart.Options.MapOverlayLayers, builder, source, map, "overlay");
        }
    }

    private static void Layers(IReadOnlyList<ChartMapLayer> layers, VisualSceneBuilder builder, ChartRect source, ChartRect target, string placement) {
        for (var index = 0; index < layers.Count; index++) {
            var layer = layers[index];
            using (builder.PushGroup("map-" + placement + "-layer-" + index, layer.Role, new Dictionary<string, string> { ["data-cfx-map-id"] = layer.Definition.Id, ["data-cfx-layer"] = placement })) {
                foreach (var region in layer.Definition.Regions) {
                    var paths = ChartMapPathParser.ParseSubpaths(region.Path, target.Width / source.Width);
                    // One compound fill retains holes. Stroke open paths separately, so an open overlay is never closed accidentally.
                    var closed = paths.Where(path => path.IsClosed).Select(path => (IReadOnlyList<ChartPoint>)path.Points.Select(point => Project(point, source, target)).ToArray()).ToArray();
                    if (closed.Length > 0) builder.Path(Path(closed), layer.FillColor, layer.StrokeColor, layer.StrokeWidth, layer.Role, close: true);
                    foreach (var open in paths.Where(path => !path.IsClosed)) builder.Path(Path(new[] { (IReadOnlyList<ChartPoint>)open.Points.Select(point => Project(point, source, target)).ToArray() }),
                        stroke: layer.StrokeColor, strokeWidth: layer.StrokeWidth, role: layer.Role);
                }
            }
        }
    }

    private static void Tiles(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors, double min, double max) {
        var definition = chart.Options.TileMapDefinition ?? throw new InvalidOperationException("A tile map requires a tile definition.");
        var values = Values(chart, key => definition.TryResolveRegion(key, out var code) ? code : null);
        var width = chart.Options.MapRegionStrokeWidth;
        var gap = Math.Min(context.Theme.Spacing / 2, Math.Min(plot.Width / definition.ColumnCount, plot.Height / definition.RowCount) / 6);
        var size = Math.Max(0, Math.Min((plot.Width - width - gap * (definition.ColumnCount - 1)) / definition.ColumnCount,
            (plot.Height - width - gap * (definition.RowCount - 1)) / definition.RowCount));
        var gridWidth = size * definition.ColumnCount + gap * (definition.ColumnCount - 1);
        var gridHeight = size * definition.RowCount + gap * (definition.RowCount - 1);
        using (builder.PushGroup("tile-map", "tile-map", new Dictionary<string, string> { ["data-cfx-map-id"] = definition.Id,
            ["data-cfx-region-count"] = N(definition.Regions.Count), ["data-cfx-filled-region-count"] = N(values.Count), ["data-cfx-missing-region-count"] = N(definition.Regions.Count - values.Count) })) using (builder.PushClip(plot)) {
            Surface(chart, context, builder, new ChartRect(plot.Left + (plot.Width - gridWidth) / 2, plot.Top + (plot.Height - gridHeight) / 2, gridWidth, gridHeight), colors, "tile-map-surface");
            foreach (var region in definition.Regions) {
                var x = plot.Left + (plot.Width - gridWidth) / 2 + region.Column * (size + gap);
                var y = plot.Top + (plot.Height - gridHeight) / 2 + region.Row * (size + gap);
                var bounds = new ChartRect(x, y, size, size); var inset = size * .22;
                var geometry = Path(new[] { (IReadOnlyList<ChartPoint>)new[] { new ChartPoint(x + inset, y), new ChartPoint(x + size - inset, y),
                    new ChartPoint(x + size, y + size / 2), new ChartPoint(x + size - inset, y + size), new ChartPoint(x + inset, y + size), new ChartPoint(x, y + size / 2) } });
                int? index = values.TryGetValue(region.Code, out var found) ? found : (int?)null;
                var full = Formatted(chart, index); var id = index.HasValue ? "series-0-point-" + index.Value : "tile-map-empty-" + region.Code;
                var fill = Fill(chart, colors, index, min, max);
                builder.AddRegion(new VisualSemanticRegion(id, "tile-map-region", bounds, region.Name + ": " + full));
                using (builder.PushGroup(id, "tile-map-region-source", Source(chart, region.Code, region.Name, index, index.HasValue ? chart.Series[0].Points[index.Value].Y : (double?)null, full, min, max))) {
                    builder.Path(geometry, fill, chart.Options.MapRegionStrokeColor ?? colors.Surface, width, "tile-map-region", close: true);
                    var pattern = Pattern(chart.Series[0], index);
                    if (index.HasValue && pattern != ChartFillPattern.None) builder.Pattern(geometry, pattern, ChartColorMath.AccessibleTextOnBackground(fill).WithAlpha(90), role: "tile-map-pattern");
                    if (chart.Options.ShowMapLabels) VisualRadialPrimitives.Text(builder, region.Code, new ChartRect(x + inset, y, size - inset * 2, size),
                        TickStyle(chart, context, ChartColorMath.AccessibleTextOnBackground(fill)), "tile-map-label", id + "-label");
                }
            }
        }
    }
}
