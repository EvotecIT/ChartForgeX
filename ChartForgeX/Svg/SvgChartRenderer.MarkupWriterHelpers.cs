using System;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void AppendSvg(StringBuilder sb, int capacity, Action<SvgMarkupWriter> write) {
        var writer = new SvgMarkupWriter(capacity);
        write(writer);
        sb.Append(writer.Build());
    }

    private static void DrawSvgTextCenteredX(SvgMarkupWriter writer, Chart chart, string role, string text, double centerX, double y, ChartColor fill, double fontSize, double maxWidth, string fontWeight, ChartColor? stroke = null, double strokeWidth = 0, bool middleBaseline = true, ChartTextStyle? style = null) {
        var preferredFontSize = StyleFontSize(style, fontSize);
        var fittedFontSize = TextFontSizeForSvgWidth(text, Math.Max(8, maxWidth), preferredFontSize);
        var fittedText = TrimSvgLabelToWidth(text, fittedFontSize, Math.Max(8, maxWidth));
        if (fittedText.Length == 0) return;

        writer
            .StartElement("text")
            .Attribute("data-cfx-role", role)
            .Attribute("x", centerX)
            .Attribute("y", y)
            .Attribute("text-anchor", "middle");
        if (middleBaseline) writer.Attribute("dominant-baseline", "middle");
        writer.Attribute("fill", StyleColor(style, fill).ToCss());
        if (stroke.HasValue && strokeWidth > 0) {
            writer
                .Attribute("stroke", stroke.Value.ToCss())
                .Attribute("stroke-width", strokeWidth)
                .Attribute("paint-order", "stroke fill")
                .Attribute("stroke-linejoin", "round");
        }

        writer
            .Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style)))
            .Attribute("font-size", fittedFontSize)
            .Attribute("font-weight", StyleWeight(style, fontWeight));
        WriteSvgTextStyleAttributes(writer, style);
        writer
            .Text(fittedText)
            .EndElement()
            .Line();
    }
}
