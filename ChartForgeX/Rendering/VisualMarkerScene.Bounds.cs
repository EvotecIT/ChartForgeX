using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualMarkerScene {
    private static double OutlineExtent(ChartSeries series, ChartMarkerOptions markers) {
        var width = FamilyStrokeWidth(series);
        return StrokeWidth(series, width > 0, width > 0 ? width : 1, markers) / 2;
    }

    /// <summary>Keeps hidden source footprints while measuring custom geometry rather than its normalization multiplier.</summary>
    internal static double FootprintExtent(ChartSeries series, double radius, ChartMarkerOptions? markerOptions = null) {
        var markers = markerOptions ?? series.Markers;
        return Math.Max(ChartMarkerGeometry.Extent(markers, radius), Extent(series, radius, markers));
    }

    /// <summary>Shares actual numeric custom-marker bounds between semantics, label obstacles and static painting.</summary>
    internal static ChartRect Bounds(ChartSeries series, double x, double y, double radius, ChartMarkerOptions? markerOptions = null) {
        var markers = markerOptions ?? series.Markers;
        return ResolvedBounds(markers, x, y, radius, OutlineExtent(series, markers));
    }

    internal static ChartRect ResolvedBounds(ChartMarkerOptions markers, double x, double y, double radius, double outlineExtent) {
        var visible = radius > 0 && markers.Enabled != false;
        if (markers.CustomPath == null) return Bounds(x, y, Math.Max(radius,
            visible ? ChartMarkerGeometry.Extent(markers, radius) + outlineExtent : 0));
        var bounds = ChartMarkerGeometry.Bounds(markers, x, y, radius);
        var padding = visible ? outlineExtent : 0;
        return new ChartRect(bounds.X - padding, bounds.Y - padding, bounds.Width + padding * 2, bounds.Height + padding * 2);
    }

    /// <summary>Unites the endpoint footprints for a source interval without inventing a circular custom-marker radius.</summary>
    internal static ChartRect PairBounds(ChartSeries series, ChartPoint first, ChartPoint second, double radius) =>
        UnionBounds(Bounds(series, first.X, first.Y, radius), Bounds(series, second.X, second.Y, radius));

    internal static ChartRect UnionBounds(ChartRect first, ChartRect second) => new(Math.Min(first.Left, second.Left), Math.Min(first.Top, second.Top),
        Math.Max(first.Right, second.Right) - Math.Min(first.Left, second.Left), Math.Max(first.Bottom, second.Bottom) - Math.Min(first.Top, second.Top));
}
