using System;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    // Both diagram families use the same measured, multiline text plan for SVG and native pixels.
    private static void WriteDiagramLabel(SvgMarkupWriter writer, string text, ChartRect bounds, ResolvedVisualTheme theme, ChartColor color, double size, int weight, string role, bool preserveWords = false) {
        var style = new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Weight = weight }, FontSize = VisualBlockRendering.BodyFontSize(theme, size), LineHeight = 1.15 };
        var width = Math.Max(1, bounds.Width); var height = Math.Max(1, bounds.Height);
        var wrap = TextWrapMode.Word;
        if (preserveWords && VisualBlockRendering.DiagramWordWidth(text, style) > width) {
            var preferred = style.FontSize;
            style.FontSize = Math.Min(size, preferred);
            if (VisualBlockRendering.DiagramWordWidth(text, style) <= width) {
                style.FontSize = ChartForgeX.Rendering.ChartTextFitting.FitFontSize(style.FontSize, preferred, candidate => {
                    style.FontSize = candidate;
                    return VisualBlockRendering.DiagramWordWidth(text, style) <= width;
                });
            } else {
                // A word wider than a normal-size fixed cell is explicitly ellipsized. Its
                // complete source stays in the title instead of becoming arbitrary fragments.
                wrap = TextWrapMode.NoWrap;
            }
        }
        var initial = style.FontSize;
        var layout = TextLayoutEngine.Layout(text, width, style, wrap);
        if (layout.Metrics.Height > height) {
            style.FontSize = Math.Max(.001, ChartForgeX.Rendering.ChartTextFitting.FitFontSize(0, initial, candidate => {
                style.FontSize = candidate;
                return TextLayoutEngine.Layout(text, width, style, wrap).Metrics.Height <= height;
            }));
            layout = TextLayoutEngine.Layout(text, width, style, wrap);
        }
        var y = bounds.Top + (height - layout.Metrics.Height) / 2 + VisualBlockRendering.TextAscent(style);
        writer.StartElement("g").Attribute("data-cfx-role", role).EndStartElement();
        writer.StartElement("title").Text(text).EndElement();
        foreach (var line in layout.Lines) {
            writer.StartElement("text").Attribute("x", F(bounds.Left + bounds.Width / 2)).Attribute("y", F(y)).Attribute("text-anchor", "middle")
                .Attribute("fill", color.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", F(style.FontSize)).Attribute("font-weight", weight)
                .Text(line.Text).EndElement();
            y += layout.Metrics.LineHeight;
        }
        writer.EndElement().Line();
    }

    private static void DiagramRect(SvgMarkupWriter writer, ChartRect bounds, ChartColor fill, ChartColor stroke, double radius = 5) =>
        writer.StartElement("rect").Attribute("x", F(bounds.X)).Attribute("y", F(bounds.Y)).Attribute("width", F(bounds.Width)).Attribute("height", F(bounds.Height))
            .Attribute("rx", F(radius)).Attribute("fill", fill.ToCss()).Attribute("stroke", stroke.ToCss()).EndEmptyElement().Line();
}
