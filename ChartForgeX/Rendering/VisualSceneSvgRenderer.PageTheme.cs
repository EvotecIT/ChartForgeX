using System.IO;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    // Literal scenes contribute no extra bytes, preserving their existing deterministic namespaces.
    private static void WriteColorVariableIdentity(BinaryWriter writer, ChartColor color) {
        if (color.CssVariable == null) return;
        writer.Write("css-variable"); writer.Write(color.CssVariable);
    }

    // The outer text group carries page fonts once; individual text lines leave family/size unset under opt-in.
    private static void WritePageFontAttributes(SvgMarkupWriter writer, VisualSvgOptions? options) {
        if (options?.HasPageFonts != true) return;
        writer.Attribute("font-family", options.PageFontFamily).Attribute("font-size", options.PageFontSize);
    }

    // A family-only page policy keeps each line's prepared size.
    private static void WriteTextFontAttributes(SvgMarkupWriter writer, string family, double size, VisualSvgOptions? options) {
        if (options?.HasPageFonts == true) {
            if (options.PageFontSize == null) writer.Attribute("font-size", size);
            return;
        }
        writer.Attribute("font-family", family).Attribute("font-size", size);
    }
}
