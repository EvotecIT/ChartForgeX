using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static bool UsesDatasetComposition(Chart chart) => chart.Options.MapViewport.CrossesAntimeridian
        || chart.Options.MapBaseLayers.Concat(chart.Options.MapOverlayLayers).Any(layer => layer.Definition.PolygonViewport.HasValue) || chart.Series.Count > 1 || chart.Series.Any(series =>
        series.RegionMapDefinition?.PolygonViewport != null || series.ColorScale != null || series.ColorDimension.HasOverrides || series.MapConnectors.Count > 0
        || series.MapPointSize.HasOverrides || series.MapClustering != null || series.MapPoints.Any(point => point.ColorValue.HasValue)) || chart.Options.MapConnectors.Count > 0
            && chart.Series[0].RegionMapDefinition?.GeographicProjection != null;

    /// <summary>Fits one declared coordinate frame and draws retained datasets and independent tile insets in authored paint order.</summary>
    private static void Composed(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartColorPresentationSet? colorScales) {
        if (!Finite(chart.Options.MapRegionStrokeWidth) || chart.Options.MapRegionStrokeWidth < 0)
            throw new InvalidOperationException("Map region stroke width must be non-negative and finite.");
        if (!chart.Series.All(series => ChartMapComposition.IsMap(series.Kind)))
            throw new InvalidOperationException("Map composition requires map datasets.");
        var projection = ChartMapComposition.Projection(chart);
        var colors = context.Theme.Resolve(context.ThemeMode);
        colorScales ??= new ChartColorPresentationSet(chart);
        var datasets = chart.Series.Select((_, index) => colorScales.MapDataset(index)).ToArray();
        var source = CompositionBounds(chart, projection);
        if (!Finite(source.Left) || !Finite(source.Top) || !Finite(source.Width) || !Finite(source.Height) || source.Width <= 0 || source.Height <= 0)
            throw new InvalidOperationException("Map source bounds must be finite and have positive dimensions.");
        foreach (var data in datasets) data.SizeScale?.SetFrame(source.Width, source.Height,
            chart.Series.Where(series => series.Kind != ChartSeriesKind.TileMap).All(series => series.Kind == ChartSeriesKind.DottedMap) && !UsesCompactMapDot(chart.Options.MapViewport) ? 5.2 : 3.6);
        var viewport = DatasetKeys(chart, context, builder, plot, colors, datasets);
        viewport = SizeKeyViewport(chart, context, builder, viewport, datasets, out var sizeKeys);
        var initial = Fit(viewport, source.Width, source.Height);
        var dot = Math.Min(3.6, Math.Min(initial.Width / Math.Max(1, source.Width / 3.5), initial.Height / Math.Max(1, source.Height / 3.5)) * .42);
        if (datasets.Any(data => data.SizeScale != null)) dot = datasets.First(data => data.SizeScale != null).SizeScale!.Dot(viewport);
        var radius = datasets.Where(data => data.SizeScale != null).SelectMany(data => data.Weights.Select(weight => data.SizeScale!.Radius(weight, dot)))
            .DefaultIfEmpty(0).Max();
        radius = Math.Max(radius, datasets.Where(data => data.Series.MapClustering != null)
            .Select(data => data.Series.MapClustering!.Radius).DefaultIfEmpty(0).Max());
        var map = Fit(viewport, source.Width, source.Height, Math.Max(MapCalloutInset(chart, viewport),
            Math.Max(chart.Options.MapRegionStrokeWidth / 2, radius * 1.85 + Math.Max(1, dot * .55) / 2)));
        foreach (var data in datasets.Where(data => data.Series.Kind == ChartSeriesKind.DottedMap))
            data.CaptureClusters(map, GeographicPoint, IsVisible);
        ChartPoint GeographicPoint(ChartPoint point) => Project(ChartGeographicProjection.Project(point.X, point.Y, projection!.Value, chart.Options.MapViewport), source, map);
        ChartPoint[] RoutePoints(IReadOnlyList<ChartPoint> points) => ChartGeographicProjection.ProjectRoute(points, projection!.Value, chart.Options.MapViewport)
            .Select(point => Project(point, source, map)).ToArray();
        bool IsVisible(ChartPoint point) {
            if (chart.Options.MapViewport.CrossesAntimeridian && !ChartGeographicProjection.Contains(point, chart.Options.MapViewport)) return false;
            var projected = ChartGeographicProjection.Project(point.X, point.Y, projection!.Value, chart.Options.MapViewport);
            return projected.X >= source.Left && projected.X <= source.Right && projected.Y >= source.Top && projected.Y <= source.Bottom;
        }
        var labels = new List<MapLabel>(); var obstacles = new List<LabelObstacle>();
        var routeOwner = Array.FindIndex(datasets, data => data.Series.Kind == ChartSeriesKind.DottedMap);
        if (routeOwner < 0) routeOwner = 0;
        var mapMetadata = new Dictionary<string, string> {
            ["data-cfx-projection"] = !projection.HasValue ? "arbitrary" : projection == ChartMapGeoJsonProjection.WebMercator ? "web-mercator" : "equirectangular",
            ["data-cfx-source-left"] = N(source.Left), ["data-cfx-source-top"] = N(source.Top),
            ["data-cfx-source-width"] = N(source.Width), ["data-cfx-source-height"] = N(source.Height),
            ["data-cfx-dataset-count"] = N(datasets.Length)
        };
        if (chart.Options.MapViewport.CrossesAntimeridian) {
            var window = chart.Options.MapViewport;
            mapMetadata["data-cfx-crosses-antimeridian"] = "true";
            mapMetadata["data-cfx-viewport-min-longitude"] = N(window.MinimumLongitude); mapMetadata["data-cfx-viewport-max-longitude"] = N(window.MaximumLongitude);
            mapMetadata["data-cfx-unwrapped-longitude-span"] = N(window.LongitudeSpan);
        }
        using (builder.PushGroup(projection.HasValue ? "geographic-map" : "arbitrary-map", projection.HasValue ? "geographic-map" : "arbitrary-map", mapMetadata))
        using (builder.PushClip(viewport)) {
            using (builder.PushClip(map)) {
                if (chart.Series.Where(series => series.Kind != ChartSeriesKind.TileMap).All(series => series.Kind == ChartSeriesKind.DottedMap))
                    DottedLand(chart, context, builder, map, chart.Options.MapViewport, dot, colors);
                else Surface(chart, context, builder, map, colors, "region-map-surface");
                Layers(chart.Options.MapBaseLayers, builder, source, map, "base");
            }
            foreach (var data in datasets) using (builder.PushGroup(data.Prefix, data.Series.Kind == ChartSeriesKind.DottedMap ? "dotted-map" : "map-series", new Dictionary<string, string> {
                ["data-cfx-series"] = N(data.Index), ["data-cfx-series-key"] = data.Series.InteractionIdentityKey,
                ["data-cfx-label"] = data.Series.Name, ["data-cfx-min-value"] = N(data.Minimum), ["data-cfx-max-value"] = N(data.Maximum),
                ["aria-label"] = chart.Options.Labels.Describe(DescriptionFacts(chart, true, data.Index))
            })) {
                if (data.Series.ObservationCount == 0) builder.AddDiagnostic(new VisualDiagnostic("map.no-data", "Map dataset '" + data.Series.Name + "' has no observations."));
                if (data.Series.Kind == ChartSeriesKind.TileMap) {
                    var inset = data.Series.MapInset!.Value;
                    var tilePlot = new ChartRect(viewport.Left + inset.Left * viewport.Width, viewport.Top + inset.Top * viewport.Height,
                        inset.Width * viewport.Width, inset.Height * viewport.Height);
                    DrawTiles(chart, context, builder, tilePlot, colors, data.Minimum, data.Maximum, data.Presentation, data: data);
                    continue;
                }
                if (data.Series.Kind == ChartSeriesKind.RegionMap) using (builder.PushClip(map)) DrawRegions(chart, context, builder, source, map, colors, data, labels, obstacles, viewport);
                var routes = data.Series.MapConnectors.AsEnumerable();
                if (data.Index == routeOwner) routes = chart.Options.MapConnectors.Concat(routes);
                using (builder.PushClip(map)) DrawRoutes(chart, context, builder, map, viewport, dot, data.Weights, data.MinimumWeight, data.MaximumWeight,
                    colors, labels, data.Series, data.Index, routes.ToArray(), GeographicPoint, IsVisible, data.SizeScale, datasets,
                    chart.Options.MapViewport.CrossesAntimeridian ? RoutePoints : null,
                    chart.Options.MapViewport.CrossesAntimeridian ? 360 * map.Width / source.Width : (double?)null);
                if (data.Series.Kind == ChartSeriesKind.DottedMap)
                    DrawPoints(chart, context, builder, map, viewport, dot, data.Weights, data.MinimumWeight, data.MaximumWeight, colors,
                        labels, obstacles, data, GeographicPoint, IsVisible);
            }
            using (builder.PushClip(map)) Layers(chart.Options.MapOverlayLayers, builder, source, map, "overlay");
        }
        DrawMapLabels(builder, viewport, labels, obstacles, context.Theme.Spacing, outline: datasets.Any(data => data.Series.Kind == ChartSeriesKind.RegionMap));
        foreach (var item in sizeKeys) using (builder.PushGroup(null, "map-size-scale-source", new Dictionary<string, string> {
            ["data-cfx-series"] = N(item.Data.Index), ["data-cfx-series-key"] = item.Data.Series.InteractionIdentityKey
        })) item.Key.Draw(builder, viewport);
    }

    private static ChartRect CompositionBounds(Chart chart, ChartMapGeoJsonProjection? projection) {
        if (chart.Options.RegionMapBounds.HasValue) return chart.Options.RegionMapBounds.Value;
        if (projection.HasValue && (chart.Options.HasExplicitMapViewport || chart.Series.Where(series => series.Kind != ChartSeriesKind.TileMap).All(series => series.Kind == ChartSeriesKind.DottedMap)))
            return ChartGeographicProjection.Bounds(chart.Options.MapViewport, projection.Value);
        var points = new List<ChartPoint>();
        foreach (var series in chart.Series) {
            if (series.RegionMapDefinition is ChartMapDefinition definition) {
                points.Add(new ChartPoint(definition.Bounds.Left, definition.Bounds.Top));
                points.Add(new ChartPoint(definition.Bounds.Right, definition.Bounds.Bottom));
            }
            if (projection.HasValue) {
                foreach (var point in series.MapPoints) points.Add(ChartGeographicProjection.Project(point.Longitude, point.Latitude, projection.Value));
                foreach (var route in series.MapConnectors) AddRouteBounds(points, route, projection.Value);
            }
        }
        if (projection.HasValue) foreach (var route in chart.Options.MapConnectors) AddRouteBounds(points, route, projection.Value);
        var bounds = Bounds(points);
        return new ChartRect(bounds.Left, bounds.Top, Math.Max(.000001, bounds.Width), Math.Max(.000001, bounds.Height));
    }

    private static void AddRouteBounds(List<ChartPoint> points, ChartMapConnector route, ChartMapGeoJsonProjection projection) {
        if (route.RoutePoints.Count > 0) foreach (var point in route.RoutePoints) points.Add(ChartGeographicProjection.Project(point.Longitude, point.Latitude, projection));
        else {
            points.Add(ChartGeographicProjection.Project(route.FromLongitude, route.FromLatitude, projection));
            points.Add(ChartGeographicProjection.Project(route.ToLongitude, route.ToLatitude, projection));
        }
    }
}
