using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static void Regions(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors, double min, double max, ChartColorPresentation presentation) {
        var definition = chart.Series[0].RegionMapDefinition ?? throw new InvalidOperationException("A region map requires a geometry definition.");
        var source = chart.Options.RegionMapBounds ?? definition.Bounds;
        if (!Finite(source.Left) || !Finite(source.Top) || !Finite(source.Width) || !Finite(source.Height) || source.Width <= 0 || source.Height <= 0)
            throw new InvalidOperationException("Region map bounds must be finite and have positive dimensions.");
        var width = chart.Options.MapRegionStrokeWidth;
        var map = Fit(plot, source.Width, source.Height, Math.Max(width / 2, MapCalloutInset(chart, plot)));
        var labels = new List<MapLabel>(); var obstacles = new List<LabelObstacle>();
        var values = Values(chart, key => definition.TryResolveRegion(key, out var code) ? code : null);
        using (builder.PushGroup("region-map", "region-map", new Dictionary<string, string> {
            ["role"] = "group", ["aria-label"] = chart.Options.Labels.Describe(DescriptionFacts(chart, true, 0)),
            ["data-cfx-map-id"] = definition.Id, ["data-cfx-source-left"] = N(source.Left), ["data-cfx-source-top"] = N(source.Top),
            ["data-cfx-source-width"] = N(source.Width), ["data-cfx-source-height"] = N(source.Height), ["data-cfx-region-count"] = N(definition.Regions.Count),
            ["data-cfx-filled-region-count"] = N(values.Count), ["data-cfx-missing-region-count"] = N(definition.Regions.Count - values.Count)
        })) using (builder.PushClip(map)) {
            Surface(chart, context, builder, map, colors, "region-map-surface");
            Layers(chart.Options.MapBaseLayers, builder, source, map, "base");
            DrawRegions(chart, context, builder, source, map, colors, new MapDataset(chart, 0, presentation), labels, obstacles, plot);
            Layers(chart.Options.MapOverlayLayers, builder, source, map, "overlay");
        }
        if (chart.Options.MapCallouts != null) DrawMapLabels(builder, plot, labels, obstacles, context.Theme.Spacing, outline: true);
    }

    /// <summary>Draws polygon facts on the caller's shared source-to-scene frame.</summary>
    private static void DrawRegions(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect source, ChartRect map,
        VisualThemeColors colors, MapDataset data, List<MapLabel>? labels = null, List<LabelObstacle>? obstacles = null, ChartRect? labelBounds = null) {
        var definition = data.Series.RegionMapDefinition!;
        var values = data.RegionIndices();
        var width = chart.Options.MapRegionStrokeWidth;
        foreach (var region in definition.Regions) {
            var paths = ChartMapPathParser.ParseSubpaths(region.Path, map.Width / source.Width);
            var projected = paths.Select(path => (IReadOnlyList<ChartPoint>)path.Points.Select(point => Project(point, source, map)).ToArray()).ToArray();
            var geometry = Path(projected); var bounds = Intersect(Bounds(projected.SelectMany(path => path)), map);
            int? index = values.TryGetValue(region.Code, out var found) ? found : (int?)null;
            var full = index.HasValue ? data.Presentation.Value(data.ColorValues[index.Value]!.Value) : data.Presentation.NoData;
            var id = index.HasValue ? data.Prefix + "-point-" + index.Value : (data.Index == 0 ? "region-map-empty-" : data.Prefix + "-region-empty-") + region.Code;
            var blend = data.RegionFill(colors, index); var fill = blend.Color;
            var metadata = data.RegionSource(region.Code, region.Name, index, full);
            AddFeatureFacts(metadata, region.Feature);
            if (region.GeoJsonGeometry != null) metadata["data-cfx-geojson-geometry"] = region.GeoJsonGeometry;
            var shape = definition.PolygonViewport.HasValue ? new LabelMarkShape(projected.Select(contour => contour.ToList()).ToArray(), true, width, map) : null;
            var visible = shape == null || shape.Intersects(map);
            if (shape != null) metadata["data-cfx-visible"] = visible ? "true" : "false";
            if (visible) builder.AddRegion(new VisualSemanticRegion(id, "region-map-region", bounds, region.Name + ": " + full));
            using (builder.PushGroup(id, "region-map-region-source", metadata)) {
                if (!visible) continue;
                builder.Path(geometry, fill, chart.Options.MapRegionStrokeColor ?? colors.Surface, width, "region-map-region", close: true,
                    paint: new VisualScenePaintBinding(blend.Paint, SvgPaint.Of(chart.Options.MapRegionStrokeColor ?? colors.Surface, SvgColorRole.Surface)));
                var pattern = Pattern(data.Series, index);
                if (index.HasValue && pattern != ChartFillPattern.None) builder.Pattern(geometry, pattern, ChartColorMath.AccessibleTextOnBackground(fill).WithAlpha(90), role: "region-map-pattern");
                if (chart.Options.ShowMapLabels && bounds.Width > 0 && bounds.Height > 0) {
                    var anchor = region.HasLabel ? Project(region.Label, source, map) : new ChartPoint(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
                    if (chart.Options.MapCallouts is ChartMapCallouts callouts && labels != null && obstacles != null) {
                        var text = callouts.RegionLabel == ChartMapRegionLabel.Name ? region.Name
                            : callouts.RegionLabel == ChartMapRegionLabel.NameAndCode ? region.Name + " (" + region.Code + ")" : region.Code;
                        var style = MapDataStyle(chart, context, index ?? -1, series: data.Series);
                        obstacles.Add(shape == null ? new LabelObstacle(id, bounds) : new LabelObstacle(id, shape));
                        AddMapLabel(builder, labels, text, text, anchor, style, id + "-label", "region-map-label", labelBounds ?? map,
                            context.Theme.Spacing, 40, data.Series.DataLabelPlacement ?? chart.Options.DataLabelPlacement,
                            leaderColor: colors.Foreground, callouts: callouts);
                    } else {
                        var area = Intersect(new ChartRect(anchor.X - bounds.Width / 2, anchor.Y - bounds.Height / 2, bounds.Width, bounds.Height), bounds);
                        var style = TickStyle(chart, context, ChartColorMath.AccessibleTextOnBackground(fill));
                        VisualRadialPrimitives.Text(builder, region.Code, area, style, "region-map-label", id + "-label",
                            chart.Options.TickLabelStyle.Color.HasValue ? VisualChartPaint.Text(style) : SvgPaint.Literal(style.Color));
                    }
                }
            }
        }
    }

    private static void Layers(IReadOnlyList<ChartMapLayer> layers, VisualSceneBuilder builder, ChartRect source, ChartRect target, string placement) {
        for (var index = 0; index < layers.Count; index++) {
            var layer = layers[index];
            using (builder.PushGroup("map-" + placement + "-layer-" + index, layer.Role, new Dictionary<string, string> { ["data-cfx-map-id"] = layer.Definition.Id, ["data-cfx-layer"] = placement })) {
                foreach (var region in layer.Definition.Regions) {
                    var metadata = new Dictionary<string, string>();
                    if (layer.Definition.PolygonViewport.HasValue) {
                        metadata["data-cfx-region-code"] = region.Code; AddFeatureFacts(metadata, region.Feature);
                        if (region.GeoJsonGeometry != null) metadata["data-cfx-geojson-geometry"] = region.GeoJsonGeometry;
                    }
                    using var sourceGroup = layer.Definition.PolygonViewport.HasValue ? builder.PushGroup(null, "map-layer-region-source", metadata) : null;
                    var paths = ChartMapPathParser.ParseSubpaths(region.Path, target.Width / source.Width);
                    // One compound fill retains holes. Stroke open paths separately, so an open overlay is never closed accidentally.
                    var closed = paths.Where(path => path.IsClosed).Select(path => (IReadOnlyList<ChartPoint>)path.Points.Select(point => Project(point, source, target)).ToArray()).ToArray();
                    if (closed.Length > 0) builder.Path(Path(closed), layer.FillColor, layer.StrokeColor, layer.StrokeWidth, layer.Role, close: true,
                        paint: new VisualScenePaintBinding(layer.FillColor.HasValue ? SvgPaint.Of(layer.FillColor.Value, SvgColorRole.Surface) : (SvgPaint?)null,
                            layer.StrokeColor.HasValue ? SvgPaint.Of(layer.StrokeColor.Value, SvgColorRole.Surface) : (SvgPaint?)null));
                    foreach (var open in paths.Where(path => !path.IsClosed)) builder.Path(Path(new[] { (IReadOnlyList<ChartPoint>)open.Points.Select(point => Project(point, source, target)).ToArray() }),
                        stroke: layer.StrokeColor, strokeWidth: layer.StrokeWidth, role: layer.Role,
                        paint: layer.StrokeColor.HasValue ? VisualChartPaint.Stroke(layer.StrokeColor.Value, SvgColorRole.Surface) : (VisualScenePaintBinding?)null);
                }
            }
        }
    }

    private static void Tiles(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors, double min, double max,
        ChartColorPresentation presentation, TileMapLegendInset? legendInset = null) {
        DrawTiles(chart, context, builder, plot, colors, min, max, presentation, legendInset);
    }

    private static void DrawTiles(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors, double min, double max,
        ChartColorPresentation presentation, TileMapLegendInset? legendInset = null, MapDataset? data = null) {
        var series = data?.Series ?? chart.Series[0];
        var seriesIndex = data?.Index ?? 0;
        var definition = series.TileMapDefinition ?? throw new InvalidOperationException("A tile map requires a tile definition.");
        var values = Values(series, key => definition.TryResolveRegion(key, out var code) ? code : null);
        var width = chart.Options.MapRegionStrokeWidth;
        var gap = legendInset?.TileGap ?? TileGap(chart, context, builder, definition, plot, width);
        var size = legendInset?.TileSize ?? Math.Max(0, Math.Min((plot.Width - width - gap * (definition.ColumnCount - 1)) / definition.ColumnCount,
            (plot.Height - width - gap * (definition.RowCount - 1)) / definition.RowCount));
        var gridWidth = size * definition.ColumnCount + gap * (definition.ColumnCount - 1);
        var gridHeight = size * definition.RowCount + gap * (definition.RowCount - 1);
        var grid = legendInset?.Grid ?? new ChartRect(plot.Left + (plot.Width - gridWidth) / 2, plot.Top + (plot.Height - gridHeight) / 2, gridWidth, gridHeight);
        var metadata = new Dictionary<string, string> { ["data-cfx-map-id"] = definition.Id,
            ["role"] = "group", ["aria-label"] = chart.Options.Labels.Describe(DescriptionFacts(chart, true, seriesIndex)),
            ["data-cfx-region-count"] = N(definition.Regions.Count), ["data-cfx-filled-region-count"] = N(values.Count), ["data-cfx-missing-region-count"] = N(definition.Regions.Count - values.Count) };
        if (data != null) {
            var inset = series.MapInset!.Value;
            metadata["data-cfx-coordinate-system"] = "independent-tiles";
            metadata["data-cfx-inset-left"] = N(inset.Left); metadata["data-cfx-inset-top"] = N(inset.Top);
            metadata["data-cfx-inset-width"] = N(inset.Width); metadata["data-cfx-inset-height"] = N(inset.Height);
        }
        using (builder.PushGroup(data == null ? "tile-map" : data.Prefix + "-tile-map", "tile-map", metadata)) using (builder.PushClip(plot)) {
            Surface(chart, context, builder, grid, colors, "tile-map-surface");
            foreach (var region in definition.Regions) {
                var x = grid.Left + region.Column * (size + gap);
                var y = grid.Top + region.Row * (size + gap);
                var bounds = new ChartRect(x, y, size, size); var inset = size * TileHexInsetRatio;
                var contour = new List<ChartPoint> { new ChartPoint(x + inset, y), new ChartPoint(x + size - inset, y),
                    new ChartPoint(x + size, y + size / 2), new ChartPoint(x + size - inset, y + size), new ChartPoint(x + inset, y + size), new ChartPoint(x, y + size / 2) };
                var geometry = Path(new[] { (IReadOnlyList<ChartPoint>)contour });
                int? index = values.TryGetValue(region.Code, out var found) ? found : (int?)null;
                var full = index.HasValue ? presentation.Value(series.RenderPoints[index.Value].Y) : presentation.NoData;
                var id = index.HasValue ? "series-" + N(seriesIndex) + "-point-" + index.Value : (data == null ? "tile-map-empty-" : data.Prefix + "-tile-map-empty-") + region.Code;
                var blend = data == null ? Fill(chart, colors, index, min, max) : data.RegionFill(colors, index); var fill = blend.Color;
                builder.AddRegion(new VisualSemanticRegion(id, "tile-map-region", bounds, region.Name + ": " + full));
                using (builder.PushGroup(id, "tile-map-region-source", data?.RegionSource(region.Code, region.Name, index, full) ?? Source(chart, region.Code, region.Name, index, index.HasValue ? series.RenderPoints[index.Value].Y : (double?)null, full, min, max, presentation))) {
                    builder.Path(geometry, fill, chart.Options.MapRegionStrokeColor ?? colors.Surface, width, "tile-map-region", close: true,
                        paint: new VisualScenePaintBinding(blend.Paint, SvgPaint.Of(chart.Options.MapRegionStrokeColor ?? colors.Surface, SvgColorRole.Surface)));
                    var pattern = Pattern(series, index);
                    if (index.HasValue && pattern != ChartFillPattern.None) builder.Pattern(geometry, pattern, ChartColorMath.AccessibleTextOnBackground(fill).WithAlpha(90), role: "tile-map-pattern");
                    if (chart.Options.ShowMapLabels) {
                        var style = TickStyle(chart, context, ChartColorMath.AccessibleTextOnBackground(fill));
                        TileLabel(builder, region.Code, bounds, contour, width / 2, style, id + "-label",
                            chart.Options.TickLabelStyle.Color.HasValue ? VisualChartPaint.Text(style) : SvgPaint.Literal(style.Color));
                    }
                }
            }
        }
    }
}
