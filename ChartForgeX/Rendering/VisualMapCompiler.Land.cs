using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static void DottedLand(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect map, ChartMapViewport viewport, double dot, VisualThemeColors colors) {
        Surface(chart, context, builder, map, colors, "dotted-map-surface");
        if (chart.Options.ShowGrid) {
            for (var index = 1; index < 4; index++) builder.Line(map.Left + map.Width * index / 4, map.Top, map.Left + map.Width * index / 4, map.Bottom,
                ChartColorMath.WithOpacity(colors.Border, .22), context.Theme.GridStrokeWidth, "dotted-map-graticule");
            for (var index = 1; index < 3; index++) builder.Line(map.Left, map.Top + map.Height * index / 3, map.Right, map.Top + map.Height * index / 3,
                ChartColorMath.WithOpacity(colors.Border, .18), context.Theme.GridStrokeWidth, "dotted-map-graticule");
        }
        var boundaries = BoundaryLines(viewport); var light = ChartDottedMapSurface.IsLightSurface(colors.Surface);
        var landColor = ChartColorMath.WithOpacity(ChartDottedMapSurface.LandAreaColor(colors.Surface, colors.MutedForeground), ChartDottedMapSurface.LandAreaOpacity(colors.Surface));
        var boundary = ChartColorMath.WithOpacity(ChartDottedMapSurface.BoundaryColor(colors.Surface, colors.MutedForeground), ChartDottedMapSurface.BoundaryOpacity(colors.Surface));
        foreach (var line in boundaries) {
            var closed = line.Length >= 4 && Math.Abs(line[0].X - line[line.Length - 1].X) <= .9 && Math.Abs(line[0].Y - line[line.Length - 1].Y) <= .9;
            var geometry = Path(new[] { (IReadOnlyList<ChartPoint>)line.Select(point => Geographic(point, viewport, map)).ToArray() });
            builder.Path(geometry, closed ? landColor : (ChartColor?)null, boundary, Math.Max(.5, dot * .3), "dotted-map-boundary", close: closed);
        }
        if (Same(viewport, ChartMapViewport.Poland())) builder.Path(Path(new[] { (IReadOnlyList<ChartPoint>)WorldMapDots.PolandOutline.Select(point => Geographic(point, viewport, map)).ToArray() }),
            landColor, boundary, Math.Max(.5, dot * .3), "dotted-map-outline", close: true);
        if (light && (boundaries.Length > 0 || Same(viewport, ChartMapViewport.Poland()))) return;
        var dots = LandDots(viewport); var offsets = Offsets(viewport);
        var fill = ChartColorMath.WithOpacity(ChartDottedMapSurface.LandDotColor(colors.Surface, colors.MutedForeground), ChartDottedMapSurface.LandDotOpacity(colors.Surface));
        foreach (var source in dots) foreach (var offset in offsets) {
            var coordinate = new ChartPoint(source.X + offset.X, source.Y + offset.Y);
            if (!Visible(coordinate, viewport) || Same(viewport, ChartMapViewport.Poland()) && !Inside(WorldMapDots.PolandOutline, coordinate)) continue;
            var point = Geographic(coordinate, viewport, map);
            var radius = boundaries.Length == 0 ? dot / 2 : Math.Min(dot / 2, Math.Max(.8, dot * .34));
            builder.Ellipse(point.X, point.Y, radius, radius, fill, role: "dotted-map-land-dot");
        }
    }

    private static ChartPoint[] LandDots(ChartMapViewport viewport) => Same(viewport, ChartMapViewport.Poland()) ? WorldMapDots.PolandLand
        : Same(viewport, ChartMapViewport.Europe()) ? WorldMapDots.EuropeLand : Same(viewport, ChartMapViewport.NorthAmerica()) ? WorldMapDots.NorthAmericaLand
        : Same(viewport, ChartMapViewport.SouthAmerica()) ? WorldMapDots.SouthAmericaLand : Same(viewport, ChartMapViewport.Africa()) ? WorldMapDots.AfricaLand
        : Same(viewport, ChartMapViewport.Asia()) ? WorldMapDots.AsiaLand : Same(viewport, ChartMapViewport.Oceania()) ? WorldMapDots.OceaniaLand : WorldMapDots.Land;
    private static ChartPoint[][] BoundaryLines(ChartMapViewport viewport) => Same(viewport, ChartMapViewport.World()) ? WorldMapDots.WorldBoundaries
        : Same(viewport, ChartMapViewport.Europe()) ? WorldMapDots.EuropeBoundaries : Same(viewport, ChartMapViewport.NorthAmerica()) ? WorldMapDots.NorthAmericaBoundaries
        : Same(viewport, ChartMapViewport.SouthAmerica()) ? WorldMapDots.SouthAmericaBoundaries : Same(viewport, ChartMapViewport.Africa()) ? WorldMapDots.AfricaBoundaries
        : Same(viewport, ChartMapViewport.Asia()) ? WorldMapDots.AsiaBoundaries : Same(viewport, ChartMapViewport.Oceania()) ? WorldMapDots.OceaniaBoundaries : Array.Empty<ChartPoint[]>();
    private static ChartPoint[] Offsets(ChartMapViewport viewport) {
        var longitude = viewport.MaximumLongitude - viewport.MinimumLongitude; var latitude = viewport.MaximumLatitude - viewport.MinimumLatitude;
        if (Same(viewport, ChartMapViewport.World()) || Same(viewport, ChartMapViewport.Europe()) || Same(viewport, ChartMapViewport.Poland()) || longitude >= 45 || latitude >= 30) return new[] { new ChartPoint(0, 0) };
        if (longitude <= 30 || latitude <= 18) return Enumerable.Range(-1, 3).SelectMany(x => Enumerable.Range(-1, 3).Select(y => new ChartPoint(x * 1.05, y * 1.05))).ToArray();
        return new[] { new ChartPoint(0, 0), new ChartPoint(-.9, -.9), new ChartPoint(.9, -.9), new ChartPoint(-.9, .9), new ChartPoint(.9, .9) };
    }
    private static bool Inside(IReadOnlyList<ChartPoint> polygon, ChartPoint point) {
        var inside = false;
        for (var index = 0; index < polygon.Count; index++) {
            var a = polygon[index]; var b = polygon[(index + polygon.Count - 1) % polygon.Count];
            if ((a.Y > point.Y) != (b.Y > point.Y) && point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }
}
