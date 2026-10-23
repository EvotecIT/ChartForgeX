using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void DetailTextStyle(VisualArtifactInterchangeJsonWriter writer, TextStyleOverride? style) {
        if (style == null) return;
        writer.Property("textStyle"); writer.StartObject();
        OptionalString(writer, "color", style.Color is { } color ? color.A == 255 ? color.ToHex() : color.ToHexRgba() : null);
        OptionalString(writer, "fontFamily", style.FontFamily); OptionalString(writer, "fontWeight", style.FontWeight);
        OptionalNumber(writer, "fontSize", style.FontSize);
        if (style.ItalicOverride.HasValue) Boolean(writer, "italic", style.ItalicOverride.Value);
        if (style.UnderlineOverride.HasValue) Boolean(writer, "underline", style.UnderlineOverride.Value);
        if (style.StrikethroughOverride.HasValue) Boolean(writer, "strikethrough", style.StrikethroughOverride.Value);
        OptionalString(writer, "underlineStyle", style.UnderlineStyle?.ToString());
        OptionalString(writer, "strikethroughStyle", style.StrikethroughStyle?.ToString());
        OptionalString(writer, "baseline", style.Baseline?.ToString()); OptionalString(writer, "textCase", style.TextCase?.ToString());
        OptionalString(writer, "openTypeLanguageTag", style.OpenTypeLanguageTag); OptionalNumber(writer, "colorPaletteIndex", style.ColorPaletteIndex);
        if (style.Variations != null) {
            writer.Property("variations"); writer.StartObject();
            foreach (var axis in style.Variations) Number(writer, axis.Key, axis.Value);
            writer.EndObject();
        }
        writer.EndObject();
    }

    private static TextStyleOverride? ReadDetailTextStyle(Dictionary<string, GeoJsonValue> detail) {
        if (!detail.TryGetValue("textStyle", out var value) || value.IsNull) return null;
        var item = value.AsObject("detail text style");
        var style = new TextStyleOverride {
            FontFamily = OptionalString(item, "fontFamily"), FontWeight = OptionalString(item, "fontWeight"), FontSize = OptionalNumber(item, "fontSize"),
            UnderlineStyle = OptionalEnum<TextDecorationStyle>(item, "underlineStyle"), StrikethroughStyle = OptionalEnum<TextDecorationStyle>(item, "strikethroughStyle"),
            Baseline = OptionalEnum<TextBaseline>(item, "baseline"), TextCase = OptionalEnum<TextCaseTransform>(item, "textCase"),
            OpenTypeLanguageTag = OptionalString(item, "openTypeLanguageTag"), ColorPaletteIndex = OptionalInt(item, "colorPaletteIndex")
        };
        if (OptionalString(item, "color") is string color) style.Color = ChartColor.Parse(color);
        if (OptionalBool(item, "italic") is bool italic) style.Italic = italic;
        if (OptionalBool(item, "underline") is bool underline) style.Underline = underline;
        if (OptionalBool(item, "strikethrough") is bool strike) style.Strikethrough = strike;
        if (item.TryGetValue("variations", out var variations) && !variations.IsNull) {
            var axes = variations.AsObject("font variations");
            if (axes.Count > 32) throw new ArgumentException("Interchange font variations support at most32axes.");
            var coordinates = new List<KeyValuePair<string, double>>();
            foreach (var axis in axes) coordinates.Add(new KeyValuePair<string, double>(axis.Key, axis.Value.AsNumber("font variation")));
            style.Variations = new FontVariationSettings(coordinates);
        }
        return style;
    }
}
