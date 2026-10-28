using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void PreparedEdgePlan(VisualArtifactInterchangeJsonWriter writer, TopologyPreparedEdgePlan? plan) {
        if (plan == null) return;
        writer.Property("preparedEdgePlan"); writer.StartObject(); Number(writer, "defaultUnits", plan.DefaultUnits);
        if (plan.SelectedInput) Boolean(writer, "selectedInput", true);
        if (!plan.HighlightedInput) Boolean(writer, "highlightedInput", false);
        if (plan.Geometry != null) PreparedEdgeGeometry(writer, plan.Geometry);
        writer.EndObject();
    }
    private static TopologyPreparedEdgePlan? ReadPreparedEdgePlan(Dictionary<string, GeoJsonValue> root) {
        var item = OptionalObject(root, "preparedEdgePlan");
        if (item == null) return null;
        return TopologyPreparedEdgePlan.Restore(RequiredNumber(item, "defaultUnits"), ReadPreparedEdgeGeometry(item),
            OptionalBool(item, "selectedInput") ?? false, OptionalBool(item, "highlightedInput") ?? true);
    }
    private static void PreparedEdgeGeometry(VisualArtifactInterchangeJsonWriter writer, TopologyPreparedEdgeGeometry geometry) {
        writer.Property("geometry"); writer.StartObject(); OptionalString(writer, "interpolation", geometry.Interpolation?.ToString());
        OptionalNumber(writer, "cornerBlendLength", geometry.CornerBlendLength); Number(writer, "order", geometry.Order);
        OptionalNumber(writer, "trunkOwnerOrder", geometry.TrunkOwnerOrder);
        writer.Property("input"); writer.StartArray();
        foreach (var point in geometry.Input) { writer.StartObject(); Number(writer, "x", point.X); Number(writer, "y", point.Y); writer.EndObject(); }
        writer.EndArray();
        Path("fullPath", geometry.FullPath); Path("bodyPath", geometry.BodyPath); Path("tailPath", geometry.TailPath);
        if (geometry.MainLabel is { } main) { writer.Property("mainLabel"); writer.StartObject(); Rect(main.Bounds);
            Number(writer, "anchorX", main.Anchor.X); Number(writer, "anchorY", main.Anchor.Y); writer.EndObject(); }
        Endpoint("sourceLabel", geometry.SourceLabel); Endpoint("targetLabel", geometry.TargetLabel); writer.EndObject();
        void Path(string name, ChartPath? path) { if (path == null) return; writer.Property(name); writer.StartObject();
            PathCommands(writer, TopologyPathGeometry.FromNativePath(path).Commands); writer.EndObject(); }
        void Rect(ChartRect bounds) => RectangleValues(writer, bounds);
        void Endpoint(string name, TopologyPreparedEndpointLabel? label) { if (label == null) return;
            writer.Property(name); writer.StartObject(); Rect(label.Bounds); Number(writer, "fontSize", label.FontSize); writer.EndObject(); }
    }
    private static TopologyPreparedEdgeGeometry? ReadPreparedEdgeGeometry(Dictionary<string, GeoJsonValue> parent) {
        var item = OptionalObject(parent, "geometry"); if (item == null) return null;
        var input = OptionalArray(item, "input"); if (input.Count < 2 || input.Count > 10000) throw new ArgumentException("Captured interpolation requires 2 through 10000 input vertices.");
        var points = new ChartPoint[input.Count];
        for (var i = 0; i < points.Length; i++) { var point = ReadPoint(input[i].AsObject("captured interpolation input")); points[i] = new ChartPoint(point.X, point.Y); }
        var full = Path("fullPath") ?? throw new ArgumentException("Captured interpolation requires its full numeric path.");
        var main = OptionalObject(item, "mainLabel");
        return new TopologyPreparedEdgeGeometry(OptionalEnum<TopologyEdgeInterpolation>(item, "interpolation"), OptionalNumber(item, "cornerBlendLength"),
            points, full, Path("bodyPath"), Path("tailPath"), RequiredInt(item, "order"), OptionalInt(item, "trunkOwnerOrder"),
            main == null ? null : new TopologyPreparedMainLabel(Rect(main), new ChartPoint(RequiredNumber(main, "anchorX"), RequiredNumber(main, "anchorY"))),
            Endpoint("sourceLabel"), Endpoint("targetLabel"));
        ChartPath? Path(string name) { var value = OptionalObject(item, name); return value == null ? null : ReadPathCommands(value).Capture(); }
        ChartRect Rect(Dictionary<string, GeoJsonValue> value) => ReadRectangle(value);
        TopologyPreparedEndpointLabel? Endpoint(string name) { var value = OptionalObject(item, name); return value == null ? null : new TopologyPreparedEndpointLabel(Rect(value), RequiredNumber(value, "fontSize")); }
    }
}
