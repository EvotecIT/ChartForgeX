using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Themes;

public sealed partial class VisualTheme {
    private const int MaximumThemeJsonCharacters = 1024 * 1024;
    private static readonly GeoJsonReadLimits ThemeJsonLimits = new GeoJsonReadLimits(4096, 256, 256).LimitDepth(32).RejectDuplicates();

    /// <summary>Loads a version 1 paired theme, including canonical palettes, typography and geometry.</summary>
    /// <remarks>
    /// This format is distinct from <see cref="FromJson"/>, which imports the generated canonical color document.
    /// Version 1 requires light/dark palettes and complete typography/geometry objects. Unknown members are ignored
    /// within bounded input limits; duplicate keys and unsupported versions are rejected. Optional per-mode
    /// <c>roles</c> preserve independent semantic and annotation colors without selecting a legacy renderer path.
    /// Optional <c>geometry.cardRadius</c> defaults to 3 logical units and controls the outer frame independently of marks.
    /// Optional <c>typography.scalarValueSize</c> and <c>typography.centerValueSize</c> default to 34 and 20 logical units.
    /// Optional <c>geometry.gaugeStrokeWidth</c> and <c>geometry.gaugeBandWidth</c> default to 14 and 4 logical units.
    /// Optional <c>effects.cardShadowOpacity</c> and <c>effects.cardShadowColor</c> retain native card shadows;
    /// omitted effects keep the default flat frame. Shadows use available authored padding without moving content.
    /// </remarks>
    /// <param name="json">The case-sensitive versioned theme document, at most one MiB of characters.</param>
    /// <returns>An immutable theme snapshot.</returns>
    /// <exception cref="ArgumentException">The document is malformed, unsupported, incomplete or outside input limits.</exception>
    public static VisualTheme FromThemeJson(string json) {
        if (json == null) throw new ArgumentNullException(nameof(json));
        if (json.Length > MaximumThemeJsonCharacters) throw new ArgumentException("Theme JSON exceeds the maximum supported size.", nameof(json));
        var root = GeoJsonValue.Parse(json, StringComparer.Ordinal, ThemeJsonLimits).AsObject("theme");
        if (Number(root, "schemaVersion", "theme") != 1) throw new ArgumentException("Only theme schemaVersion 1 is supported.", nameof(json));
        var typography = Object(root, "typography", "theme");
        var geometry = Object(root, "geometry", "theme");
        var effects = root.TryGetValue("effects", out var effectValue) ? effectValue.AsObject("effects") : new Dictionary<string, GeoJsonValue>();
        var light = ReadThemeColors(json, root, "light", VisualThemeMode.Light);
        var dark = ReadThemeColors(json, root, "dark", VisualThemeMode.Dark);
        return new VisualTheme(light, dark,
            new VisualTypography(Required(typography, "family", "typography").AsString("typography.family"),
                Number(typography, "titleSize", "typography"), Number(typography, "subtitleSize", "typography"),
                Number(typography, "axisSize", "typography"), Number(typography, "legendSize", "typography"), Number(typography, "dataLabelSize", "typography"),
                OptionalNumber(typography, "scalarValueSize", "typography", 34), OptionalNumber(typography, "centerValueSize", "typography", 20)),
            Number(geometry, "spacing", "geometry"), Number(geometry, "seriesStrokeWidth", "geometry"), Number(geometry, "markerRadius", "geometry"),
            Number(geometry, "areaOpacity", "geometry"), Number(geometry, "barRadius", "geometry"),
            Number(geometry, "gridStrokeWidth", "geometry"), Number(geometry, "axisStrokeWidth", "geometry"),
            geometry.TryGetValue("cardRadius", out var cardRadius) ? cardRadius.AsNumber("geometry.cardRadius") : DefaultCardRadius,
            effects.TryGetValue("cardShadowOpacity", out var opacity) ? opacity.AsNumber("effects.cardShadowOpacity") : 0,
            effects.TryGetValue("cardShadowColor", out var shadow) ? Hex(shadow.AsString("effects.cardShadowColor"), "cardShadowColor") : null,
            OptionalNumber(geometry, "gaugeStrokeWidth", "geometry", 14), OptionalNumber(geometry, "gaugeBandWidth", "geometry", 4));
    }

    /// <summary>Exports the complete paired color, typography and geometry theme using schemaVersion 1.</summary>
    /// <remarks>Color alpha bytes, status fill/ink pairs and ordered ramps are retained. Each array is limited to 256 values.</remarks>
    /// <returns>A deterministic JSON document suitable for <see cref="FromThemeJson"/>.</returns>
    public string ToThemeJson() {
        var writer = new VisualArtifactInterchangeJsonWriter();
        writer.StartObject();
        writer.Property("schemaVersion"); writer.Number(1);
        WriteThemeColors(writer, "light", _light.ToTokens());
        WriteThemeColors(writer, "dark", _dark.ToTokens());
        writer.Property("typography"); writer.StartObject();
        writer.Property("family"); writer.String(Typography.Family);
        WriteNumber(writer, "titleSize", Typography.TitleSize); WriteNumber(writer, "subtitleSize", Typography.SubtitleSize);
        WriteNumber(writer, "axisSize", Typography.AxisSize); WriteNumber(writer, "legendSize", Typography.LegendSize); WriteNumber(writer, "dataLabelSize", Typography.DataLabelSize);
        WriteNumber(writer, "scalarValueSize", Typography.ScalarValueSize); WriteNumber(writer, "centerValueSize", Typography.CenterValueSize);
        writer.EndObject();
        writer.Property("geometry"); writer.StartObject();
        WriteNumber(writer, "spacing", Spacing); WriteNumber(writer, "seriesStrokeWidth", SeriesStrokeWidth); WriteNumber(writer, "markerRadius", MarkerRadius);
        WriteNumber(writer, "areaOpacity", AreaOpacity); WriteNumber(writer, "barRadius", BarRadius);
        WriteNumber(writer, "gridStrokeWidth", GridStrokeWidth); WriteNumber(writer, "axisStrokeWidth", AxisStrokeWidth);
        WriteNumber(writer, "cardRadius", CardRadius);
        WriteNumber(writer, "gaugeStrokeWidth", GaugeStrokeWidth); WriteNumber(writer, "gaugeBandWidth", GaugeBandWidth);
        writer.EndObject();
        writer.Property("effects"); writer.StartObject();
        WriteNumber(writer, "cardShadowOpacity", CardShadowOpacity); WriteColor(writer, "cardShadowColor", CardShadowColor);
        writer.EndObject(); writer.EndObject();
        string json = writer.ToString();
        if (json.Length > MaximumThemeJsonCharacters) throw new ArgumentException("Theme JSON exceeds the maximum supported size.");
        // Exported documents must obey the same aggregate, array and depth budgets as imported documents.
        GeoJsonValue.Parse(json, StringComparer.Ordinal, ThemeJsonLimits);
        return json;
    }

    private static VisualDesignTokens ReadThemeColors(string json, Dictionary<string, GeoJsonValue> root, string mode, VisualThemeMode variant) {
        var tokens = VisualDesignTokens.FromJson(json, variant);
        var palette = Object(root, mode, "theme");
        if (!palette.TryGetValue("roles", out var value)) return tokens;
        var roles = value.AsObject(mode + ".roles");
        tokens.Positive = Role(roles, "positive", tokens.Positive);
        tokens.Warning = Role(roles, "warning", tokens.Warning);
        tokens.Negative = Role(roles, "negative", tokens.Negative);
        tokens.Disabled = Role(roles, "disabled", tokens.Disabled);
        tokens.Muted = NullableRole(roles, "muted", tokens.Muted); tokens.Grid = NullableRole(roles, "grid", tokens.Grid);
        tokens.Axis = NullableRole(roles, "axis", tokens.Axis); tokens.Info = NullableRole(roles, "info", tokens.Info);
        tokens.Quiet = NullableRole(roles, "quiet", tokens.Quiet); tokens.QuietLine = NullableRole(roles, "quietLine", tokens.QuietLine);
        tokens.Neutral = NullableRole(roles, "neutral", tokens.Neutral); tokens.Neutral2 = NullableRole(roles, "neutral2", tokens.Neutral2);
        tokens.Neutral3 = NullableRole(roles, "neutral3", tokens.Neutral3);
        return tokens;
    }

    private static void WriteThemeColors(VisualArtifactInterchangeJsonWriter writer, string mode, VisualDesignTokens tokens) {
        writer.Property(mode); writer.StartObject();
        writer.Property("surface"); writer.StartObject();
        WriteColor(writer, "page", tokens.Background); WriteColor(writer, "card", tokens.ElevatedSurface);
        WriteColor(writer, "cardAlt", tokens.Surface); WriteColor(writer, "line", tokens.Border); writer.EndObject();
        writer.Property("text"); writer.StartObject();
        WriteColor(writer, "primary", tokens.Foreground); WriteColor(writer, "secondary", tokens.MutedForeground); writer.EndObject();
        writer.Property("chrome"); writer.StartObject(); WriteColor(writer, "accent", tokens.SecondaryAccent); writer.EndObject();
        writer.Property("accent"); writer.StartObject(); WriteColor(writer, "base", tokens.Accent); writer.EndObject();
        writer.Property("series"); WriteColors(writer, tokens.Palette);
        writer.Property("severity"); writer.StartObject();
        WritePair(writer, "critical", tokens.Status.Critical); WritePair(writer, "high", tokens.Status.High);
        WritePair(writer, "medium", tokens.Status.Medium); WritePair(writer, "low", tokens.Status.Low); WritePair(writer, "info", tokens.Status.Info); writer.EndObject();
        writer.Property("outcome"); writer.StartObject();
        WritePair(writer, "pass", tokens.Status.Pass); WritePair(writer, "neutral", tokens.Status.Neutral); writer.EndObject();
        writer.Property("state"); writer.StartObject(); WritePair(writer, "maintenance", tokens.Status.Maintenance); writer.EndObject();
        writer.Property("ramps"); writer.StartObject();
        if (tokens.SequentialRamp is { } sequential) { writer.Property("sequential"); WriteColors(writer, sequential); }
        if (tokens.DivergingRamp is { } diverging) {
            writer.Property("diverging"); writer.StartObject();
            writer.Property("negative"); WriteColors(writer, diverging.Negative); WriteColor(writer, "neutral", diverging.Neutral);
            writer.Property("positive"); WriteColors(writer, diverging.Positive); writer.EndObject();
        }
        writer.EndObject();
        writer.Property("roles"); writer.StartObject();
        WriteColor(writer, "positive", tokens.Positive); WriteColor(writer, "warning", tokens.Warning);
        WriteColor(writer, "negative", tokens.Negative); WriteColor(writer, "disabled", tokens.Disabled);
        WriteColor(writer, "muted", tokens.Muted); WriteColor(writer, "grid", tokens.Grid); WriteColor(writer, "axis", tokens.Axis);
        WriteColor(writer, "info", tokens.Info); WriteColor(writer, "quiet", tokens.Quiet); WriteColor(writer, "quietLine", tokens.QuietLine);
        WriteColor(writer, "neutral", tokens.Neutral); WriteColor(writer, "neutral2", tokens.Neutral2); WriteColor(writer, "neutral3", tokens.Neutral3);
        writer.EndObject(); writer.EndObject();
    }

    private static void WritePair(VisualArtifactInterchangeJsonWriter writer, string name, VisualTokenColor pair) {
        writer.Property(name); writer.StartObject(); WriteColor(writer, "fill", pair.Fill); WriteColor(writer, "ink", pair.Ink); writer.EndObject();
    }

    private static void WriteColors(VisualArtifactInterchangeJsonWriter writer, IEnumerable<ChartColor> colors) {
        writer.StartArray(); foreach (var color in colors) writer.String(color.ToHexRgba()); writer.EndArray();
    }

    private static void WriteColor(VisualArtifactInterchangeJsonWriter writer, string name, ChartColor? color) {
        writer.Property(name); writer.String(color?.ToHexRgba());
    }

    private static void WriteNumber(VisualArtifactInterchangeJsonWriter writer, string name, double value) {
        writer.Property(name); writer.Number(value);
    }

    private static GeoJsonValue Required(Dictionary<string, GeoJsonValue> parent, string name, string path) =>
        parent.TryGetValue(name, out var value) && !value.IsNull ? value : throw new ArgumentException("Theme requires '" + path + "." + name + "'.");
    private static Dictionary<string, GeoJsonValue> Object(Dictionary<string, GeoJsonValue> parent, string name, string path) => Required(parent, name, path).AsObject(path + "." + name);
    private static double Number(Dictionary<string, GeoJsonValue> parent, string name, string path) => Required(parent, name, path).AsNumber(path + "." + name);
    private static double OptionalNumber(Dictionary<string, GeoJsonValue> parent, string name, string path, double fallback) =>
        parent.TryGetValue(name, out var value) ? value.AsNumber(path + "." + name) : fallback;
    private static ChartColor Role(Dictionary<string, GeoJsonValue> roles, string name, ChartColor fallback) =>
        roles.TryGetValue(name, out var value) ? Hex(value.AsString("roles." + name), name) : fallback;
    private static ChartColor? NullableRole(Dictionary<string, GeoJsonValue> roles, string name, ChartColor? fallback) =>
        roles.TryGetValue(name, out var value) ? value.IsNull ? null : Hex(value.AsString("roles." + name), name) : fallback;
    private static ChartColor Hex(string text, string name) {
        if ((text.Length != 7 && text.Length != 9) || text[0] != '#') throw new ArgumentException("Theme color '" + name + "' requires #rrggbb or #rrggbbaa.");
        return ChartColor.FromHex(text);
    }
}
