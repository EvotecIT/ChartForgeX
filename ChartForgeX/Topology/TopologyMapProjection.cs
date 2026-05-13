using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal readonly struct TopologyProjectedPoint {
    public TopologyProjectedPoint(double x, double y, bool isInsideViewport) {
        X = x;
        Y = y;
        IsInsideViewport = isInsideViewport;
    }

    public double X { get; }

    public double Y { get; }

    public bool IsInsideViewport { get; }
}

internal static class TopologyMapProjection {
    public const string ProjectionName = "equirectangular";

    public static ChartRect MapRect(TopologyChart chart) {
        var pad = Math.Max(24, chart.Viewport.Padding);
        var top = pad + (string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72);
        var legendOffset = TopologyRenderPrimitives.LegendReservedHeight(chart.Legend, chart.Viewport);
        var plot = new ChartRect(
            pad + 8,
            top + 8,
            Math.Max(1, chart.Viewport.Width - pad * 2 - 16),
            Math.Max(1, chart.Viewport.Height - top - pad - legendOffset - 16));
        return FitMap(plot, chart.MapViewport);
    }

    public static TopologyProjectedPoint Project(ChartRect map, ChartMapViewport viewport, double longitude, double latitude) {
        var visible = IsVisible(viewport, longitude, latitude);
        var clampedLongitude = Clamp(longitude, viewport.MinimumLongitude, viewport.MaximumLongitude);
        var clampedLatitude = Clamp(latitude, viewport.MinimumLatitude, viewport.MaximumLatitude);
        var x = map.Left + (clampedLongitude - viewport.MinimumLongitude) / (viewport.MaximumLongitude - viewport.MinimumLongitude) * map.Width;
        var y = map.Top + (viewport.MaximumLatitude - clampedLatitude) / (viewport.MaximumLatitude - viewport.MinimumLatitude) * map.Height;
        return new TopologyProjectedPoint(x, y, visible);
    }

    public static bool IsVisible(ChartMapViewport viewport, double longitude, double latitude) {
        return longitude >= viewport.MinimumLongitude && longitude <= viewport.MaximumLongitude &&
               latitude >= viewport.MinimumLatitude && latitude <= viewport.MaximumLatitude;
    }

    public static ChartPoint[] LandDots(ChartMapViewport viewport) =>
        IsPolandViewport(viewport) ? WorldMapDots.PolandLand :
        IsNorthAmericaViewport(viewport) ? WorldMapDots.NorthAmericaLand :
        IsSouthAmericaViewport(viewport) ? WorldMapDots.SouthAmericaLand :
        IsAfricaViewport(viewport) ? WorldMapDots.AfricaLand :
        IsAsiaViewport(viewport) ? WorldMapDots.AsiaLand :
        IsOceaniaViewport(viewport) ? WorldMapDots.OceaniaLand :
        IsEuropeViewport(viewport) ? WorldMapDots.EuropeLand : WorldMapDots.Land;

    public static ChartPoint[] LandOffsets(ChartMapViewport viewport) {
        if (IsWorldViewport(viewport) || IsEuropeViewport(viewport) || IsPolandViewport(viewport)) return WorldLandOffsets;
        var longitudeSpan = viewport.MaximumLongitude - viewport.MinimumLongitude;
        var latitudeSpan = viewport.MaximumLatitude - viewport.MinimumLatitude;
        if (longitudeSpan >= 45 || latitudeSpan >= 30) return WorldLandOffsets;
        return longitudeSpan <= 30 || latitudeSpan <= 18 ? CountryLandOffsets : RegionalLandOffsets;
    }

    public static ChartPoint[][] BoundaryLines(ChartMapViewport viewport) =>
        IsWorldViewport(viewport) ? WorldBoundaries :
        IsNorthAmericaViewport(viewport) ? WorldMapDots.NorthAmericaBoundaries :
        IsSouthAmericaViewport(viewport) ? WorldMapDots.SouthAmericaBoundaries :
        IsAfricaViewport(viewport) ? WorldMapDots.AfricaBoundaries :
        IsAsiaViewport(viewport) ? WorldMapDots.AsiaBoundaries :
        IsOceaniaViewport(viewport) ? WorldMapDots.OceaniaBoundaries :
        IsEuropeViewport(viewport) ? WorldMapDots.EuropeBoundaries : Array.Empty<ChartPoint[]>();

    public static double LandDotRadius(ChartRect map, ChartMapViewport viewport) {
        const double sourceDotStep = 3.5;
        var longitudeSlots = Math.Max(1, (viewport.MaximumLongitude - viewport.MinimumLongitude) / sourceDotStep);
        var latitudeSlots = Math.Max(1, (viewport.MaximumLatitude - viewport.MinimumLatitude) / sourceDotStep);
        var projectedStep = Math.Min(map.Width / longitudeSlots, map.Height / latitudeSlots);
        var maximum = IsWorldViewport(viewport) || IsEuropeViewport(viewport) || IsPolandViewport(viewport) ? 3.6 : 5.2;
        var radius = Clamp(projectedStep * 0.42, 1.4, maximum);
        return BoundaryLines(viewport).Length == 0 ? radius / 2 : Math.Max(0.8, radius * 0.34);
    }

    public static bool CanFillBoundary(ChartPoint[] line) {
        if (line.Length < 3) return false;
        var first = line[0];
        var last = line[line.Length - 1];
        return Math.Abs(first.X - last.X) < 0.000001 && Math.Abs(first.Y - last.Y) < 0.000001;
    }

    private static ChartRect FitMap(ChartRect plot, ChartMapViewport viewport) {
        var aspect = (viewport.MaximumLongitude - viewport.MinimumLongitude) / (viewport.MaximumLatitude - viewport.MinimumLatitude);
        var width = Math.Min(plot.Width, plot.Height * aspect);
        var height = width / aspect;
        if (height > plot.Height) {
            height = plot.Height;
            width = height * aspect;
        }

        return new ChartRect(
            plot.Left + Math.Max(0, (plot.Width - width) / 2),
            plot.Top + Math.Max(0, (plot.Height - height) / 2),
            width,
            height);
    }

    private static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

    private static bool IsWorldViewport(ChartMapViewport viewport) => Matches(viewport, WorldMapDots.MinimumLongitude, WorldMapDots.MaximumLongitude, WorldMapDots.MinimumLatitude, WorldMapDots.MaximumLatitude);

    private static bool IsEuropeViewport(ChartMapViewport viewport) => Matches(viewport, -11, 35, 36, 72);

    private static bool IsNorthAmericaViewport(ChartMapViewport viewport) => Matches(viewport, -170, -50, 5, 72);

    private static bool IsSouthAmericaViewport(ChartMapViewport viewport) => Matches(viewport, -86, -32, -56, 14);

    private static bool IsAfricaViewport(ChartMapViewport viewport) => Matches(viewport, -20, 55, -36, 38);

    private static bool IsAsiaViewport(ChartMapViewport viewport) => Matches(viewport, 44, 150, -10, 72);

    private static bool IsOceaniaViewport(ChartMapViewport viewport) => Matches(viewport, 108, 180, -48, 2);

    private static bool IsPolandViewport(ChartMapViewport viewport) => Matches(viewport, 13.5, 24.5, 48.5, 55.2);

    private static bool Matches(ChartMapViewport viewport, double minimumLongitude, double maximumLongitude, double minimumLatitude, double maximumLatitude) =>
        Math.Abs(viewport.MinimumLongitude - minimumLongitude) < 0.000001 &&
        Math.Abs(viewport.MaximumLongitude - maximumLongitude) < 0.000001 &&
        Math.Abs(viewport.MinimumLatitude - minimumLatitude) < 0.000001 &&
        Math.Abs(viewport.MaximumLatitude - maximumLatitude) < 0.000001;

    private static readonly ChartPoint[][] WorldBoundaries = CombineBoundaries(
        WorldMapDots.NorthAmericaBoundaries,
        WorldMapDots.SouthAmericaBoundaries,
        WorldMapDots.EuropeBoundaries,
        WorldMapDots.AfricaBoundaries,
        WorldMapDots.AsiaBoundaries,
        WorldMapDots.OceaniaBoundaries);

    private static ChartPoint[][] CombineBoundaries(params ChartPoint[][][] sources) {
        var count = 0;
        foreach (var source in sources) count += source.Length;
        var result = new ChartPoint[count][];
        var index = 0;
        foreach (var source in sources) {
            foreach (var boundary in source) result[index++] = boundary;
        }

        return result;
    }

    private static readonly ChartPoint[] WorldLandOffsets = {
        new(0, 0)
    };

    private static readonly ChartPoint[] RegionalLandOffsets = {
        new(0, 0),
        new(1.2, 1.2),
        new(-1.2, 1.2),
        new(1.2, -1.2),
        new(-1.2, -1.2)
    };

    private static readonly ChartPoint[] CountryLandOffsets = {
        new(0, 0),
        new(0.8, 0),
        new(-0.8, 0),
        new(0, 0.8),
        new(0, -0.8),
        new(0.8, 0.8),
        new(-0.8, 0.8),
        new(0.8, -0.8),
        new(-0.8, -0.8)
    };
}
