using System.Globalization;
using ChartForgeX.Typography;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static string FontPaletteRules(Core.Chart chart) {
        var options = chart.Options;
        var styles = new System.Collections.Generic.List<TextStyleOverride> { options.TitleStyle, options.SubtitleStyle, options.AxisTitleStyle, options.TickLabelStyle, options.LegendStyle, options.DataLabelStyle };
        foreach (var series in chart.Series) {
            styles.Add(series.DataLabelStyle);
            foreach (var pointStyle in series.PointDataLabelStyles) if (pointStyle != null) styles.Add(pointStyle);
        }
        return TypographyPaletteCss.Rules(options.Theme.FontFamily, styles.ToArray());
    }

    private static void WriteSvgTextStyleAttributes(SvgMarkupWriter writer, TextStyleOverride? style) {
        if (style == null) return;
        var css = TypographyCss.Role(style);
        if (css.Length != 0) writer.Attribute("style", css);
        if (style.Italic) writer.Attribute("font-style", "italic");
        var underline = style.UnderlineStyle ?? (style.Underline ? TextDecorationStyle.Single : TextDecorationStyle.None);
        var strike = style.StrikethroughStyle ?? (style.Strikethrough ? TextDecorationStyle.Single : TextDecorationStyle.None);
        var splitDecorations = RequiresSeparateSvgDecorations(underline, strike);
        if (underline != TextDecorationStyle.None || strike != TextDecorationStyle.None) {
            writer.Attribute("text-decoration", (underline != TextDecorationStyle.None && !splitDecorations ? "underline" : string.Empty) + (underline != TextDecorationStyle.None && strike != TextDecorationStyle.None && !splitDecorations ? " " : string.Empty) + (strike != TextDecorationStyle.None ? "line-through" : string.Empty));
            writer.Attribute("text-decoration-style", SvgDecorationStyle(splitDecorations ? strike : underline != TextDecorationStyle.None ? underline : strike));
        }
        if (style.Baseline == TextBaseline.Superscript) writer.Attribute("baseline-shift", "super");
        else if (style.Baseline == TextBaseline.Subscript) writer.Attribute("baseline-shift", "sub");
    }

    private static SvgMarkupWriter WriteSvgStyledTextContent(SvgMarkupWriter writer, TextStyleOverride? style, string text) {
        if (style != null) {
            var underline = style.UnderlineStyle ?? (style.Underline ? TextDecorationStyle.Single : TextDecorationStyle.None);
            var strike = style.StrikethroughStyle ?? (style.Strikethrough ? TextDecorationStyle.Single : TextDecorationStyle.None);
            if (RequiresSeparateSvgDecorations(underline, strike)) {
                writer.EndStartElement()
                    .StartElement("tspan")
                    .Attribute("text-decoration", "underline")
                    .Attribute("text-decoration-style", SvgDecorationStyle(underline))
                    .Text(text)
                    .EndElement();
                return writer;
            }
        }

        return writer.Text(text);
    }

    private static bool RequiresSeparateSvgDecorations(TextDecorationStyle underline, TextDecorationStyle strike) =>
        underline != TextDecorationStyle.None && strike != TextDecorationStyle.None && underline != strike;

    private static string SvgDecorationStyle(TextDecorationStyle style) => style switch {
        TextDecorationStyle.Dotted => "dotted",
        TextDecorationStyle.Dashed => "dashed",
        TextDecorationStyle.Wavy => "wavy",
        TextDecorationStyle.Double => "double",
        _ => "solid"
    };

    private static string StyleText(TextStyleOverride? style, string text) => style?.TransformText(text, CultureInfo.InvariantCulture) ?? text;
}
