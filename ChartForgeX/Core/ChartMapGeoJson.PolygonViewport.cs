using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Core;

public static partial class ChartMapGeoJson {
    private static GeoJsonValue ReadPolygonViewportSource(string json) {
        if (json.Length > VisualArtifactInterchangeEnvelope.MaximumJsonCharacters)
            throw new ArgumentException("Polygon GeoJSON exceeds the supported JSON size.", nameof(json));
        var maximum = VisualArtifactInterchangeEnvelope.MaximumJsonValues;
        return GeoJsonValue.Parse(json, StringComparer.OrdinalIgnoreCase,
            new GeoJsonReadLimits(maximum, maximum, maximum).KeepNumberSpelling().KeepFeaturePropertyNames().RejectDuplicates());
    }

    private static bool TryReadViewportPath(GeoJsonFeature feature, ChartMapGeoJsonOptions options, ObservationImportBudget budget,
        out string path, out ChartPoint label, out Bounds bounds, out CoordinateBounds coordinates) {
        path = string.Empty; label = default; bounds = EmptyBounds(); coordinates = EmptyCoordinateBounds();
        if (feature.Geometry == null) return false;
        var type = feature.GeometryType;
        var polygon = string.Equals(type, "Polygon", StringComparison.OrdinalIgnoreCase);
        if (!polygon && !string.Equals(type, "MultiPolygon", StringComparison.OrdinalIgnoreCase)) return false;
        var input = feature.Geometry.RequiredArray("coordinates");
        // Validate/count the parsed source before allocating projected rings, metadata strings or periodic output copies.
        long vertices = 0;
        if (polygon) CountViewportPolygon(input, ref vertices, ref coordinates);
        else foreach (var item in input) CountViewportPolygon(item.AsArray("MultiPolygon polygon"), ref vertices, ref coordinates);
        if (vertices == 0) return false;
        if (!options.IncludesCoordinateBounds(coordinates.MinLongitude, coordinates.MaxLongitude, coordinates.MinLatitude, coordinates.MaxLatitude)) return true;
        var geometry = GeoJsonValue.Object(feature.Geometry);
        var metadataCharacters = checked((long)PolygonJsonLength(feature.IdValue) + PolygonJsonLength(feature.PropertiesValue) + PolygonJsonLength(geometry));
        budget.Reserve(vertices, 0, metadataCharacters);
        var viewport = options.PolygonViewport!.Value;
        var plans = new List<ViewportPolygon>();
        if (polygon) plans.Add(ProjectViewportPolygon(input, options.Projection, viewport));
        else foreach (var item in input) plans.Add(ProjectViewportPolygon(item.AsArray("MultiPolygon polygon"), options.Projection, viewport));
        long additionalVertices = 0;
        foreach (var plan in plans) additionalVertices = checked(additionalVertices + (long)(plan.LastCopy - plan.FirstCopy) * plan.VertexCount);
        budget.Reserve(additionalVertices, 0);
        var output = new StringBuilder(); var labelPoints = new List<ChartPoint>();
        foreach (var plan in plans) {
            // The label anchor uses one outer ring, not an average of geographically equivalent copies.
            labelPoints.AddRange(plan.Rings[0]);
            for (var copy = plan.FirstCopy; copy <= plan.LastCopy; copy++) foreach (var ring in plan.Rings) {
                for (var index = 0; index < ring.Length; index++) {
                    var point = new ChartPoint(ring[index].X + copy * 360, ring[index].Y);
                    Include(ref bounds, point);
                    output.Append(index == 0 ? 'M' : 'L').Append(Format(point.X, options.CoordinatePrecision)).Append(' ').Append(Format(point.Y, options.CoordinatePrecision));
                }
                output.Append('Z');
            }
        }
        path = output.ToString(); label = Average(labelPoints); return path.Length > 0;
    }

    private static void CountViewportPolygon(List<GeoJsonValue> rings, ref long vertices, ref CoordinateBounds bounds) {
        if (rings.Count == 0) throw new ArgumentException("A seam-aware Polygon must contain an outer ring.", "geoJson");
        foreach (var ringValue in rings) {
            var ring = ringValue.AsArray("Polygon ring");
            if (ring.Count < 4) throw new ArgumentException("Seam-aware Polygon rings must contain at least four positions and close explicitly.", "geoJson");
            ChartPoint first = default; ChartPoint last = default;
            for (var index = 0; index < ring.Count; index++) {
                var position = GeographicPosition(ring[index], "Polygon vertex", null);
                last = new ChartPoint(position.Longitude, position.Latitude);
                if (index == 0) first = last;
                Include(ref bounds, last.X, last.Y);
            }
            if (first.Y != last.Y || first.X != last.X && Math.Abs(first.X - last.X) != 360)
                throw new ArgumentException("Seam-aware Polygon rings must close at the same geographic position.", "geoJson");
            vertices = checked(vertices + ring.Count);
        }
    }

    private static ViewportPolygon ProjectViewportPolygon(List<GeoJsonValue> rings, ChartMapGeoJsonProjection projection, ChartMapViewport viewport) {
        var projected = new List<ChartPoint[]>(); long vertices = 0; double outerCenter = 0;
        for (var ringIndex = 0; ringIndex < rings.Count; ringIndex++) {
            var positions = rings[ringIndex].AsArray("Polygon ring");
            var raw = new ChartPoint[positions.Count];
            for (var index = 0; index < raw.Length; index++) {
                var position = GeographicPosition(positions[index], "Polygon vertex", null);
                raw[index] = new ChartPoint(position.Longitude, position.Latitude);
            }
            var ring = ChartGeographicProjection.ProjectRoute(raw, projection, viewport);
            // A closed ring with a nonzero globe winding has no unambiguous planar interior in this contract.
            if (Math.Abs(ring[ring.Length - 1].X - ring[0].X) > 180)
                throw new NotSupportedException("Seam-aware Polygon rings cannot wind around the globe.");
            ring[ring.Length - 1] = ring[0];
            var minimum = ring[0].X; var maximum = minimum;
            foreach (var point in ring) { minimum = Math.Min(minimum, point.X); maximum = Math.Max(maximum, point.X); }
            if (maximum - minimum >= 360) throw new NotSupportedException("Seam-aware Polygon rings must occupy less than one full longitude turn.");
            var center = minimum + (maximum - minimum) / 2;
            if (ringIndex == 0) outerCenter = center;
            else {
                var shift = ChartGeographicProjection.LongitudeNear(center, outerCenter) - center;
                for (var index = 0; index < ring.Length; index++) ring[index] = new ChartPoint(ring[index].X + shift, ring[index].Y);
            }
            projected.Add(ring); vertices = checked(vertices + ring.Length);
        }
        var left = projected[0][0].X; var right = left;
        foreach (var point in projected[0]) { left = Math.Min(left, point.X); right = Math.Max(right, point.X); }
        var firstCopy = (int)Math.Ceiling((viewport.MinimumLongitude - right) / 360);
        var lastCopy = (int)Math.Floor((viewport.MinimumLongitude + viewport.LongitudeSpan - left) / 360);
        // View clipping never filters a raw feature. Retain one off-view representative when none intersects the window.
        if (firstCopy > lastCopy) firstCopy = lastCopy = 0;
        return new ViewportPolygon(projected, firstCopy, lastCopy, vertices);
    }

    private static int PolygonJsonLength(GeoJsonValue? value) {
        if (value == null) return 0;
        var counter = new VisualArtifactInterchangeJsonWriter(writeText: false, escapeMarkupUnsafeCharacters: true);
        counter.Value(value); return counter.CharactersWritten;
    }

    private static string PolygonGeometryJson(GeoJsonFeature feature) {
        var writer = new VisualArtifactInterchangeJsonWriter(escapeMarkupUnsafeCharacters: true);
        writer.Value(GeoJsonValue.Object(feature.Geometry!)); return writer.ToString();
    }

    private sealed class ViewportPolygon {
        internal readonly List<ChartPoint[]> Rings;
        internal readonly int FirstCopy;
        internal readonly int LastCopy;
        internal readonly long VertexCount;
        internal ViewportPolygon(List<ChartPoint[]> rings, int firstCopy, int lastCopy, long vertexCount) {
            Rings = rings; FirstCopy = firstCopy; LastCopy = lastCopy; VertexCount = vertexCount;
        }
    }
}
