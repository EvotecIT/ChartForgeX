using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Themes;

public sealed partial class VisualTheme {
    /// <summary>Reads the optional <c>series</c> object: one entry per camel-case series kind, such as <c>line</c> or <c>horizontalBar</c>.</summary>
    /// <remarks>Unknown kinds, curves and marker shapes fail so a misspelled theme does not silently restyle nothing.</remarks>
    private static IEnumerable<KeyValuePair<ChartSeriesKind, VisualSeriesDefaults>> ReadSeriesDefaults(Dictionary<string, GeoJsonValue> root) {
        if (!root.TryGetValue("series", out var value) || value.IsNull) yield break;
        foreach (var entry in value.AsObject("series")) {
            var kind = ParseName<ChartSeriesKind>(entry.Key, "series kind");
            var path = "series." + entry.Key;
            var source = entry.Value.AsObject(path);
            var defaults = new VisualSeriesDefaults();
            if (source.TryGetValue("strokeWidth", out var width)) defaults.StrokeWidth = width.AsNumber(path + ".strokeWidth");
            if (source.TryGetValue("dash", out var dash)) defaults.Dash = dash.AsArray(path + ".dash").Select(item => item.AsNumber(path + ".dash")).ToArray();
            if (source.TryGetValue("curve", out var curve)) defaults.Interpolation = ParseName<ChartInterpolation>(curve.AsString(path + ".curve"), "curve");
            if (source.TryGetValue("cornerRadius", out var radius)) defaults.CornerRadius = radius.AsNumber(path + ".cornerRadius");
            if (source.TryGetValue("marker", out var marker)) defaults.ConfigureMarkers(markers => ReadMarker(markers, marker.AsObject(path + ".marker"), path + ".marker"));
            if (source.TryGetValue("dataLabel", out var label)) defaults.ConfigureDataLabelStyle(style => ReadLabel(style, label.AsObject(path + ".dataLabel"), path + ".dataLabel"));
            yield return new KeyValuePair<ChartSeriesKind, VisualSeriesDefaults>(kind, defaults);
        }
    }

    private static void ReadMarker(ChartMarkerOptions markers, Dictionary<string, GeoJsonValue> source, string path) {
        if (source.TryGetValue("shape", out var shape)) markers.Shape = ParseName<ChartMarkerShape>(shape.AsString(path + ".shape"), "marker shape");
        if (source.TryGetValue("radius", out var radius)) markers.Radius = radius.AsNumber(path + ".radius");
        if (source.TryGetValue("enabled", out var enabled)) markers.Enabled = enabled.AsBoolean(path + ".enabled");
        if (source.TryGetValue("strokeWidth", out var width)) markers.StrokeWidth = width.AsNumber(path + ".strokeWidth");
        if (source.TryGetValue("fill", out var fill)) markers.Fill = Hex(fill.AsString(path + ".fill"), "marker.fill");
        if (source.TryGetValue("stroke", out var stroke)) markers.Stroke = Hex(stroke.AsString(path + ".stroke"), "marker.stroke");
    }

    private static void ReadLabel(ChartForgeX.Typography.TextStyleOverride style, Dictionary<string, GeoJsonValue> source, string path) {
        if (source.TryGetValue("fontFamily", out var family)) style.FontFamily = family.AsString(path + ".fontFamily");
        if (source.TryGetValue("fontSize", out var size)) style.FontSize = size.AsNumber(path + ".fontSize");
        if (source.TryGetValue("fontWeight", out var weight)) style.FontWeight = weight.AsString(path + ".fontWeight");
        if (source.TryGetValue("color", out var color)) style.Color = Hex(color.AsString(path + ".color"), "dataLabel.color");
    }

    private void WriteSeriesDefaults(VisualArtifactInterchangeJsonWriter writer) {
        if (_seriesDefaults.Count == 0) return;
        writer.Property("series"); writer.StartObject();
        foreach (var kind in SeriesDefaultKinds) {
            var defaults = _seriesDefaults[kind];
            writer.Property(CamelCase(kind.ToString())); writer.StartObject();
            if (defaults.StrokeWidth is { } width) WriteNumber(writer, "strokeWidth", width);
            if (defaults.Dash is { } dash) { writer.Property("dash"); writer.StartArray(); foreach (var length in dash) writer.Number(length); writer.EndArray(); }
            if (defaults.Interpolation is { } curve) { writer.Property("curve"); writer.String(CamelCase(curve.ToString())); }
            if (defaults.CornerRadius is { } radius) WriteNumber(writer, "cornerRadius", radius);
            if (defaults.Markers.IsConfigured) WriteMarker(writer, defaults.Markers);
            if (defaults.DataLabelStyle.HasOverrides) WriteLabel(writer, defaults.DataLabelStyle);
            writer.EndObject();
        }
        writer.EndObject();
    }

    private static void WriteMarker(VisualArtifactInterchangeJsonWriter writer, ChartMarkerOptions markers) {
        writer.Property("marker"); writer.StartObject();
        if (markers.HasShape) { writer.Property("shape"); writer.String(CamelCase(markers.Shape.ToString())); }
        if (markers.Radius is { } radius) WriteNumber(writer, "radius", radius);
        if (markers.Enabled is { } enabled) { writer.Property("enabled"); writer.Boolean(enabled); }
        if (markers.StrokeWidth is { } width) WriteNumber(writer, "strokeWidth", width);
        if (markers.Fill is { } fill) WriteColor(writer, "fill", fill);
        if (markers.Stroke is { } stroke) WriteColor(writer, "stroke", stroke);
        writer.EndObject();
    }

    private static void WriteLabel(VisualArtifactInterchangeJsonWriter writer, ChartForgeX.Typography.TextStyleOverride style) {
        writer.Property("dataLabel"); writer.StartObject();
        if (style.FontFamily != null) { writer.Property("fontFamily"); writer.String(style.FontFamily); }
        if (style.FontSize is { } size) WriteNumber(writer, "fontSize", size);
        if (style.FontWeight != null) { writer.Property("fontWeight"); writer.String(style.FontWeight); }
        if (style.Color is { } color) WriteColor(writer, "color", color);
        writer.EndObject();
    }

    private static T ParseName<T>(string text, string what) where T : struct {
        // Names only: Enum.TryParse would also accept numeric strings and comma lists.
        if (text.Length == 0 || !text.All(char.IsLetter) || !Enum.TryParse<T>(text, true, out var value))
            throw new ArgumentException("Theme uses an unknown " + what + " '" + text + "'.");
        return value;
    }

    private static string CamelCase(string name) => char.ToLowerInvariant(name[0]) + name.Substring(1);
}
