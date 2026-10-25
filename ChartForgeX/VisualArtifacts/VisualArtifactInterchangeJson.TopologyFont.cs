using ChartForgeX.Rendering;
using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void DiagramContext(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangePresentation source) {
        var font = source.ContextFont;
        var roots = font == null ? null : OwnedFontResources.Capture(font, source.Frame);
        if (font != null) {
        writer.Property("contextFont"); writer.StartObject(); String(writer, "themeFamily", font.ThemeFamily);
        FontValue(writer, font.Font, "font", roots!); CapturedFontRoots(writer, roots!); CapturedFontRequests(writer, font.Text, roots!); writer.EndObject();
        }
        if (source.Frame != null) FrameRequest(writer, source.Frame, (name, style) => FrameText(writer, name, style, value => {
            if (roots == null) FiniteFontValue(writer, value, "font"); else FontValue(writer, value, "font", roots);
        }));
    }
    private static void ReadDiagramContext(Dictionary<string, GeoJsonValue> root, VisualArtifactInterchangePresentation source) {
        var item = OptionalObject(root, "contextFont"); OwnedFontRootReader? roots = null;
        if (item != null) {
            roots = new OwnedFontRootReader(item); var font = ReadFontValue(item, "font", roots);
            source.ContextFont = new PreparedDiagramFont(font, RequiredString(item, "themeFamily"), ReadCapturedFontRequests(item, roots));
        }
        source.Frame = ReadFrameRequest(root, roots); roots?.RequireAllReferenced();
    }
}
