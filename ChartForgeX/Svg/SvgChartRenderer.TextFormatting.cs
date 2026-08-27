using ChartForgeX.Typography;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void WriteSvgTextStyleAttributes(SvgMarkupWriter writer, TextStyleOverride? style) {
        if (style == null) return;
        if (style.Italic) writer.Attribute("font-style", "italic");
        var underline = style.UnderlineStyle ?? (style.Underline ? TextDecorationStyle.Single : TextDecorationStyle.None);
        var strike = style.StrikethroughStyle ?? (style.Strikethrough ? TextDecorationStyle.Single : TextDecorationStyle.None);
        if (underline != TextDecorationStyle.None || strike != TextDecorationStyle.None) {
            writer.Attribute("text-decoration", (underline != TextDecorationStyle.None ? "underline" : string.Empty) + (underline != TextDecorationStyle.None && strike != TextDecorationStyle.None ? " " : string.Empty) + (strike != TextDecorationStyle.None ? "line-through" : string.Empty));
            writer.Attribute("text-decoration-style", SvgDecorationStyle(underline != TextDecorationStyle.None ? underline : strike));
        }
        if (style.Baseline == TextBaseline.Superscript) writer.Attribute("baseline-shift", "super");
        else if (style.Baseline == TextBaseline.Subscript) writer.Attribute("baseline-shift", "sub");
    }

    private static string SvgDecorationStyle(TextDecorationStyle style) => style switch {
        TextDecorationStyle.Dotted => "dotted",
        TextDecorationStyle.Dashed => "dashed",
        TextDecorationStyle.Wavy => "wavy",
        TextDecorationStyle.Double => "double",
        _ => "solid"
    };

    private static string StyleText(TextStyleOverride? style, string text) => style?.TransformText(text) ?? text;
}
