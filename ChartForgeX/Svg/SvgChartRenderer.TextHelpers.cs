using System;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawSvgTextCenteredX(StringBuilder sb, Chart chart, string role, string text, double centerX, double y, ChartColor fill, double fontSize, double maxWidth, string fontWeight, ChartColor? stroke = null, double strokeWidth = 0, bool middleBaseline = true, TextStyleOverride? style = null) =>
        DrawSvgTextCenteredX(sb, chart, role, text, centerX, y, SvgPaint.Plain(fill), fontSize, maxWidth, fontWeight, stroke, strokeWidth, middleBaseline, style);

    /// <summary>Draws centred text whose fill is a typed paint; a colour set on <paramref name="style"/> still wins.</summary>
    private static void DrawSvgTextCenteredX(StringBuilder sb, Chart chart, string role, string text, double centerX, double y, SvgPaint fill, double fontSize, double maxWidth, string fontWeight, ChartColor? stroke = null, double strokeWidth = 0, bool middleBaseline = true, TextStyleOverride? style = null) {
        if (chart.Options.Theme.UseGraphiteLayout) { style ??= GraphiteTextStyle(chart, role); strokeWidth = 0; }
        var preferredFontSize = fontSize;
        var resolvedStyle = style ?? new TextStyleOverride();
        var fittedFontSize = TextFontSizeForSvgWidth(chart, text, Math.Max(8, maxWidth), preferredFontSize, resolvedStyle, emphasized: IsEmphasizedWeight(fontWeight), minFontSize: Math.Min(8, preferredFontSize));
        var fittedText = TrimSvgLabelToWidth(chart, text, fittedFontSize, Math.Max(8, maxWidth), resolvedStyle, emphasized: IsEmphasizedWeight(fontWeight));
        if (fittedText.Length == 0) return;

        var writer = new SvgMarkupWriter(512);
        writer.StartElement("text");
        if (!string.IsNullOrEmpty(role)) writer.Attribute("data-cfx-role", role);
        writer.Attribute("x", centerX).Attribute("y", y).Attribute("text-anchor", "middle");
        if (middleBaseline) writer.Attribute("dominant-baseline", "middle");
        writer.Paint("fill", style?.Color is { } styleColor ? SvgPaint.Plain(styleColor) : fill);
        if (stroke.HasValue && strokeWidth > 0) {
            writer.Attribute("stroke", stroke.Value.ToCss()).Attribute("stroke-width", strokeWidth).Attribute("paint-order", "stroke fill").Attribute("stroke-linejoin", "round");
        }
        writer.Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style))).Attribute("font-size", fittedFontSize).Attribute("font-weight", StyleWeight(style, fontWeight));
        WriteSvgTextStyleAttributes(writer, style);
        WriteSvgStyledTextContent(writer, style, fittedText).EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void DrawSvgTextLeft(StringBuilder sb, Chart chart, string role, string text, double x, double y, ChartColor fill, double fontSize, double maxWidth, string fontWeight, TextStyleOverride? style = null) {
        if (chart.Options.Theme.UseGraphiteLayout) style ??= GraphiteTextStyle(chart, role);
        var preferredFontSize = fontSize;
        var resolvedStyle = style ?? new TextStyleOverride();
        var fittedFontSize = TextFontSizeForSvgWidth(chart, text, Math.Max(8, maxWidth), preferredFontSize, resolvedStyle, emphasized: IsEmphasizedWeight(fontWeight), minFontSize: Math.Min(8, preferredFontSize));
        var fittedText = TrimSvgLabelToWidth(chart, text, fittedFontSize, Math.Max(8, maxWidth), resolvedStyle, emphasized: IsEmphasizedWeight(fontWeight));
        if (fittedText.Length == 0) return;
        var writer = new SvgMarkupWriter(512);
        writer.StartElement("text");
        if (!string.IsNullOrEmpty(role)) writer.Attribute("data-cfx-role", role);
        writer.Attribute("x", x).Attribute("y", y).Attribute("fill", StyleColor(style, fill).ToCss()).Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style))).Attribute("font-size", fittedFontSize).Attribute("font-weight", StyleWeight(style, fontWeight));
        WriteSvgTextStyleAttributes(writer, style);
        WriteSvgStyledTextContent(writer, style, fittedText).EndElement().Line();
        sb.Append(writer.Build());
    }

    private static string XAxisTitleText(Chart chart) => ChartTimeScale.DecorateTitle(chart);

    private static void DrawSvgXAxisTitle(StringBuilder sb, Chart chart, ChartRect plot, double y, string role = "") {
        var title = XAxisTitleText(chart);
        if (string.IsNullOrWhiteSpace(title)) return;
        DrawSvgTextCenteredX(sb, chart, role, title, plot.Left + plot.Width / 2, y, chart.Options.Theme.MutedText, StyleFontSize(chart.Options.AxisTitleStyle, chart.Options.Theme.AxisTitleFontSize), plot.Width - 4, "600", middleBaseline: false, style: chart.Options.AxisTitleStyle);
    }

    private static void DrawSvgYAxisTitle(StringBuilder sb, Chart chart, ChartRect plot, double axisX, string role = "") {
        if (string.IsNullOrWhiteSpace(chart.YAxisTitle)) return;
        var t = chart.Options.Theme;
        if (t.UseGraphiteLayout) {
            DrawSvgTextLeft(sb, chart, string.IsNullOrEmpty(role) ? "y-axis-title" : role, chart.YAxisTitle,
                chart.Options.Padding.Left, plot.Top - 8, t.MutedText, t.AxisTitleFontSize, Math.Max(32, plot.Left - chart.Options.Padding.Left - 8), "400", chart.Options.AxisTitleStyle);
            return;
        }
        var maxWidth = Math.Max(40, plot.Height * 0.72);
        var style = chart.Options.AxisTitleStyle;
        var fontSize = TextFontSizeForSvgWidth(chart, chart.YAxisTitle, maxWidth, StyleFontSize(style, t.AxisTitleFontSize), style, emphasized: true);
        var text = TrimSvgLabelToWidth(chart, chart.YAxisTitle, fontSize, maxWidth, style, emphasized: true);
        if (text.Length == 0) return;
        var writer = new SvgMarkupWriter(512);
        writer.StartElement("text");
        if (!string.IsNullOrWhiteSpace(role)) writer.Attribute("data-cfx-role", role);
        writer.Attribute("transform", "translate(" + F(axisX) + " " + F(plot.Top + plot.Height / 2) + ") rotate(-90)").Attribute("text-anchor", "middle").Attribute("fill", StyleColor(style, t.MutedText).ToCss()).Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style))).Attribute("font-size", fontSize).Attribute("font-weight", StyleWeight(style, "600"));
        WriteSvgTextStyleAttributes(writer, style);
        WriteSvgStyledTextContent(writer, style, text).EndElement().Line();
        sb.Append(writer.Build());
    }

    private static double SvgXAxisTitleHeight(Chart chart, double maxWidth) {
        if (string.IsNullOrWhiteSpace(XAxisTitleText(chart))) return 0;
        var style = chart.Options.AxisTitleStyle;
        var fontSize = TextFontSizeForSvgWidth(chart, XAxisTitleText(chart), Math.Max(48, maxWidth), StyleFontSize(style, chart.Options.Theme.AxisTitleFontSize), style, emphasized: true);
        return EstimateSvgStyledTextHeight(fontSize, style);
    }

    private static double SvgYAxisTitleHeight(Chart chart, double maxWidth) {
        if (string.IsNullOrWhiteSpace(chart.YAxisTitle)) return 0;
        var style = chart.Options.AxisTitleStyle;
        var fontSize = TextFontSizeForSvgWidth(chart, chart.YAxisTitle, Math.Max(40, maxWidth * 0.72), StyleFontSize(style, chart.Options.Theme.AxisTitleFontSize), style, emphasized: true);
        return EstimateSvgStyledTextHeight(fontSize, style);
    }

    private static bool IsEmphasizedWeight(string value) {
        if (int.TryParse(value, out var numeric)) return numeric >= 600;
        return string.Equals(value, "bold", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "bolder", StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteSvgDataLabelText(SvgMarkupWriter writer, Chart chart, TextStyleOverride style, string role, string label, double x, double y, string anchor, ChartColor fill, ChartColor stroke, double fontSize, ChartSeries? series = null, int pointIndex = -1) {
        writer.StartElement("text").Attribute("data-cfx-role", role).Attribute("x", x).Attribute("y", y).Attribute("text-anchor", anchor).Attribute("dominant-baseline", "middle").Attribute("fill", StyleColor(style, chart.Options.Theme.UseGraphiteLayout ? chart.Options.Theme.Text2 : fill).ToCss()).Attribute("stroke", chart.Options.Theme.UseGraphiteLayout ? null : stroke.ToCss()).Attribute("stroke-width", chart.Options.Theme.UseGraphiteLayout ? null : "3").Attribute("paint-order", "stroke fill").Attribute("stroke-linejoin", "round").Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style))).Attribute("font-size", fontSize).Attribute("font-weight", StyleWeight(style, chart.Options.Theme.UseGraphiteLayout ? "400" : "700"));
        if (series != null) {
            for (var i = 0; i < chart.Series.Count; i++) if (ReferenceEquals(chart.Series[i], series)) { writer.Attribute("data-cfx-series", i); break; }
            if (pointIndex >= 0) writer.Attribute("data-cfx-point", pointIndex);
        }
        WriteSvgTextStyleAttributes(writer, style);
        WriteSvgStyledTextContent(writer, style, label).EndElement().Line();
    }
}
