using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void VisibilitySourceContext(VisualArtifactInterchangeJsonWriter writer, VisualRenderContext context, bool preserveFontPaths = true) {
        writer.Property("context"); writer.StartObject(); String(writer, "themeJson", context.Theme.ToJson()); String(writer, "themeMode", context.ThemeMode.ToString());
        Number(writer, "width", context.Layout.Size.Width); Number(writer, "height", context.Layout.Size.Height);
        var padding = context.Layout.PaddingEdges; Number(writer, "left", padding.Left); Number(writer, "top", padding.Top); Number(writer, "right", padding.Right); Number(writer, "bottom", padding.Bottom);
        VisibilitySourceFont(writer, context.Font, preserveFontPaths);
        FrameRequest(writer, context.Frame, (name, style) => VisibilitySourceText(writer, name, style, preserveFontPaths));
        writer.EndObject();
    }
    private static void VisibilitySourceFont(VisualArtifactInterchangeJsonWriter writer, FontSpec font, bool preserveFontPaths = true) {
        writer.Property("font"); writer.StartObject(); String(writer, "family", font.Family); if (preserveFontPaths) OptionalString(writer, "file", font.FilePath); else Boolean(writer, "explicitFile", font.FilePath != null);
        OptionalNumber(writer, "collection", font.CollectionIndex); OptionalString(writer, "face", font.FaceName); Number(writer, "weight", font.Weight); Boolean(writer, "italic", font.Italic);
        Number(writer, "palette", font.ColorPaletteIndex); String(writer, "variations", font.Variations.Css); writer.EndObject();
    }
    private static void VisibilitySourceText(VisualArtifactInterchangeJsonWriter writer, string name, TextStyle? style, bool preserveFontPaths = true) {
        FrameText(writer, name, style, font => VisibilitySourceFont(writer, font, preserveFontPaths));
    }
    private static void VisibilitySourceMarker(VisualArtifactInterchangeJsonWriter writer, ChartMarkerOptions marker) {
        writer.Property("marker"); writer.StartObject(); Boolean(writer, "configured", marker.IsConfigured); Boolean(writer, "nonRadius", marker.HasNonRadiusConfiguration); String(writer, "shape", marker.Shape.ToString()); OptionalNumber(writer, "radius", marker.Radius);
        if (marker.Enabled.HasValue) Boolean(writer, "enabled", marker.Enabled.Value);
        OptionalString(writer, "fill", WaterfallColor(marker.Fill)); OptionalString(writer, "stroke", WaterfallColor(marker.Stroke)); OptionalNumber(writer, "strokeWidth", marker.StrokeWidth);
        Number(writer, "dashOffset", marker.StrokeDashOffset);
        if (marker.OutlineStroke is { } outline) { writer.Property("outline"); writer.StartObject(); OptionalString(writer, "color", WaterfallColor(outline.Color)); OptionalNumber(writer, "width", outline.Width);
            Number(writer, "opacity", outline.Opacity); if (outline.Dash != null) { writer.Property("dash"); writer.StartArray(); foreach (var value in outline.Dash) writer.Number(value); writer.EndArray(); } writer.EndObject(); }
        if (marker.CustomPath != null) { writer.Property("path"); writer.StartArray(); foreach (var command in marker.CustomPath.Commands) {
            writer.StartArray(); writer.String(command.Kind.ToString()); writer.Number(command.X); writer.Number(command.Y); writer.Number(command.Control1X); writer.Number(command.Control1Y); writer.Number(command.Control2X); writer.Number(command.Control2Y); writer.EndArray();
        } writer.EndArray(); }
        writer.EndObject();
    }
}
