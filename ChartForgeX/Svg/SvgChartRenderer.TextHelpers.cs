using System;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawSvgTextCenteredX(StringBuilder sb, Chart chart, string role, string text, double centerX, double y, ChartColor fill, double fontSize, double maxWidth, string fontWeight, ChartColor? stroke = null, double strokeWidth = 0, bool middleBaseline = true, ChartTextStyle? style = null) {
        var preferredFontSize = StyleFontSize(style, fontSize);
        var fittedFontSize = TextFontSizeForSvgWidth(text, Math.Max(8, maxWidth), preferredFontSize);
        var fittedText = TrimSvgLabelToWidth(text, fittedFontSize, Math.Max(8, maxWidth));
        if (fittedText.Length == 0) return;

        var writer = new SvgMarkupWriter(512);
        writer.StartElement("text");
        if (!string.IsNullOrEmpty(role)) writer.Attribute("data-cfx-role", role);
        writer.Attribute("x", centerX).Attribute("y", y).Attribute("text-anchor", "middle");
        if (middleBaseline) writer.Attribute("dominant-baseline", "middle");
        writer.Attribute("fill", StyleColor(style, fill).ToCss());
        if (stroke.HasValue && strokeWidth > 0) {
            writer.Attribute("stroke", stroke.Value.ToCss()).Attribute("stroke-width", strokeWidth).Attribute("paint-order", "stroke fill").Attribute("stroke-linejoin", "round");
        }
        writer.Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style))).Attribute("font-size", fittedFontSize).Attribute("font-weight", StyleWeight(style, fontWeight));
        WriteSvgTextStyleAttributes(writer, style);
        writer.Raw(Escape(fittedText)).EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void DrawSvgTextLeft(StringBuilder sb, Chart chart, string role, string text, double x, double y, ChartColor fill, double fontSize, double maxWidth, string fontWeight, ChartTextStyle? style = null) {
        var preferredFontSize = StyleFontSize(style, fontSize);
        var fittedFontSize = TextFontSizeForSvgWidth(text, Math.Max(8, maxWidth), preferredFontSize);
        var fittedText = TrimSvgLabelToWidth(text, fittedFontSize, Math.Max(8, maxWidth));
        if (fittedText.Length == 0) return;
        var writer = new SvgMarkupWriter(512);
        writer.StartElement("text");
        if (!string.IsNullOrEmpty(role)) writer.Attribute("data-cfx-role", role);
        writer.Attribute("x", x).Attribute("y", y).Attribute("fill", StyleColor(style, fill).ToCss()).Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style))).Attribute("font-size", fittedFontSize).Attribute("font-weight", StyleWeight(style, fontWeight));
        WriteSvgTextStyleAttributes(writer, style);
        writer.Raw(Escape(fittedText)).EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void DrawSvgXAxisTitle(StringBuilder sb, Chart chart, ChartRect plot, double y, string role = "") {
        if (string.IsNullOrWhiteSpace(chart.XAxisTitle)) return;
        DrawSvgTextCenteredX(sb, chart, role, chart.XAxisTitle, plot.Left + plot.Width / 2, y, chart.Options.Theme.MutedText, chart.Options.Theme.AxisTitleFontSize, plot.Width - 4, "600", middleBaseline: false, style: chart.Options.AxisTitleStyle);
    }

    private static void DrawSvgYAxisTitle(StringBuilder sb, Chart chart, ChartRect plot, double axisX, string role = "") {
        if (string.IsNullOrWhiteSpace(chart.YAxisTitle)) return;
        var t = chart.Options.Theme;
        var maxWidth = Math.Max(40, plot.Height * 0.72);
        var style = chart.Options.AxisTitleStyle;
        var fontSize = TextFontSizeForSvgWidth(chart.YAxisTitle, maxWidth, StyleFontSize(style, t.AxisTitleFontSize));
        var text = TrimSvgLabelToWidth(chart.YAxisTitle, fontSize, maxWidth);
        if (text.Length == 0) return;
        var writer = new SvgMarkupWriter(512);
        writer.StartElement("text");
        if (!string.IsNullOrWhiteSpace(role)) writer.Attribute("data-cfx-role", role);
        writer.Attribute("transform", "translate(" + F(axisX) + " " + F(plot.Top + plot.Height / 2) + ") rotate(-90)").Attribute("text-anchor", "middle").Attribute("fill", StyleColor(style, t.MutedText).ToCss()).Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style))).Attribute("font-size", fontSize).Attribute("font-weight", StyleWeight(style, "600"));
        WriteSvgTextStyleAttributes(writer, style);
        writer.Raw(Escape(text)).EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void WriteSvgDataLabelText(SvgMarkupWriter writer, Chart chart, ChartTextStyle style, string role, string label, double x, double y, string anchor, ChartColor fill, ChartColor stroke, double fontSize) {
        writer.StartElement("text").Attribute("data-cfx-role", role).Attribute("x", x).Attribute("y", y).Attribute("text-anchor", anchor).Attribute("dominant-baseline", "middle").Attribute("fill", StyleColor(style, fill).ToCss()).Attribute("stroke", stroke.ToCss()).Attribute("stroke-width", "3").Attribute("paint-order", "stroke fill").Attribute("stroke-linejoin", "round").Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style))).Attribute("font-size", StyleFontSize(style, fontSize)).Attribute("font-weight", StyleWeight(style, "700"));
        WriteSvgTextStyleAttributes(writer, style);
        writer.Raw(Escape(label)).EndElement().Line();
    }
}
