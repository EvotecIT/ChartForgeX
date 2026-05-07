using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawHorizontalBarGrid(StringBuilder sb, Chart chart, ChartRect plot, IReadOnlyList<double> xTicks, IReadOnlyList<double> categoryTicks, ChartMapper map) {
        var o = chart.Options;
        var t = o.Theme;
        var tickStyle = o.TickLabelStyle;
        var tickFontSize = StyleFontSize(tickStyle, t.TickLabelFontSize);
        var xLabels = XAxisTickLabels(chart, xTicks, true);
        var xLabelMaxWidth = AxisTickLabelMaxWidth(plot, xTicks.Count, 0);
        for (var i = 0; i < xTicks.Count; i++) {
            var x = map.X(xTicks[i]);
            var label = TrimSvgLabelToWidth(xLabels[i], tickFontSize, xLabelMaxWidth);
            if (o.ShowGrid) WriteHorizontalAxisLine(sb, null, x, plot.Top, x, plot.Bottom, t.Grid.ToCss(), ChartVisualPrimitives.GridStrokeWidth, ChartVisualPrimitives.HorizontalBarValueGridOpacity);
            if (ShowXAxis(chart) && label.Length > 0) DrawXAxisLabel(sb, chart, plot, label, x, plot.Bottom + 21, 0, maxWidth: xLabelMaxWidth);
        }

        foreach (var category in categoryTicks) {
            var y = map.Y(category);
            if (o.ShowGrid) WriteHorizontalAxisLine(sb, null, plot.Left, y, plot.Right, y, t.Grid.ToCss(), ChartVisualPrimitives.GridStrokeWidth, ChartVisualPrimitives.HorizontalBarCategoryGridOpacity);
            if (ShowYAxis(chart)) DrawHorizontalCategoryLabel(sb, chart, plot, FormatX(chart, category), y);
        }

        var zeroX = map.X(0);
        if (ShowXAxis(chart) && ShowAxisLines(chart) && zeroX > plot.Left && zeroX < plot.Right) {
            WriteHorizontalAxisLine(sb, null, zeroX, plot.Top, zeroX, plot.Bottom, t.Axis.ToCss(), ChartVisualPrimitives.ZeroAxisStrokeWidth);
        }

        if (ShowXAxis(chart)) {
            if (ShowAxisLines(chart)) WriteHorizontalAxisLine(sb, null, plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis.ToCss(), ChartVisualPrimitives.AxisStrokeWidth);
            DrawSvgXAxisTitle(sb, chart, plot, plot.Bottom + XAxisTitleOffset(chart, xLabels));
        }
        if (ShowYAxis(chart)) {
            if (ShowAxisLines(chart)) WriteHorizontalAxisLine(sb, null, plot.Left, plot.Top, plot.Left, plot.Bottom, t.Axis.ToCss(), ChartVisualPrimitives.AxisStrokeWidth);
            DrawSvgYAxisTitle(sb, chart, plot, 26);
        }
    }

    private static void WriteHorizontalAxisLine(StringBuilder sb, string? role, double x1, double y1, double x2, double y2, string stroke, double strokeWidth, double? opacity = null) {
        var writer = new SvgMarkupWriter(256);
        writer.StartElement("line");
        writer.Attribute("data-cfx-role", role);
        writer
            .Attribute("x1", x1)
            .Attribute("y1", y1)
            .Attribute("x2", x2)
            .Attribute("y2", y2)
            .Attribute("stroke", stroke)
            .Attribute("stroke-width", strokeWidth);
        if (opacity.HasValue) writer.Attribute("opacity", opacity.Value);
        writer.EndEmptyElement().Line();
        sb.Append(writer.Build());
    }

    private static ChartRect ApplyHorizontalBarReserve(Chart chart, ChartRect plot, IReadOnlyList<double> categoryTicks) {
        if (categoryTicks.Count == 0) return plot;
        var t = chart.Options.Theme;
        var style = chart.Options.TickLabelStyle;
        var wrapWidth = SvgHorizontalCategoryWrapWidth(chart);
        var leftShift = 0.0;
        if (ShowYAxis(chart)) {
            var widest = categoryTicks.Max(tick => WrappedSvgLabelWidth(FormatX(chart, tick), StyleFontSize(style, t.TickLabelFontSize), wrapWidth));
            var desiredLeft = Math.Max(plot.Left, widest + 58);
            var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 180);
            var adjustedLeft = Math.Min(desiredLeft, maxLeft);
            leftShift = Math.Max(0, adjustedLeft - plot.Left);
        }
        var rightReserve = HorizontalValueLabelReserve(chart);
        if (leftShift <= 0 && rightReserve <= 0) return plot;
        return new ChartRect(plot.X + leftShift, plot.Y, Math.Max(1, plot.Width - leftShift - rightReserve), plot.Height);
    }

    private static void DrawHorizontalCategoryLabel(StringBuilder sb, Chart chart, ChartRect plot, string label, double y) {
        var t = chart.Options.Theme;
        var style = chart.Options.TickLabelStyle;
        var fontSize = StyleFontSize(style, t.TickLabelFontSize);
        var lines = WrapSvgHorizontalCategoryLabel(label, fontSize, SvgHorizontalCategoryWrapWidth(chart));
        var lineHeight = fontSize + 3;
        var firstBaseline = y + 4 - (lines.Length - 1) * lineHeight / 2.0;
        var maxWidth = Math.Max(8, plot.Left - 24);
        for (var i = 0; i < lines.Length; i++) {
            var lineFontSize = TextFontSizeForSvgWidth(lines[i], maxWidth, fontSize);
            var line = TrimSvgLabelToWidth(lines[i], lineFontSize, maxWidth);
            if (line.Length == 0) continue;
            WriteHorizontalCategoryLabelLine(sb, chart, style, i, plot.Left - 12, firstBaseline + i * lineHeight, StyleColor(style, t.MutedText).ToCss(), lineFontSize, line);
        }
    }

    private static void WriteHorizontalCategoryLabelLine(StringBuilder sb, Chart chart, ChartTextStyle style, int lineIndex, double x, double y, string fill, double fontSize, string label) {
        var writer = new SvgMarkupWriter(384);
        writer
            .StartElement("text")
            .Attribute("data-cfx-role", "horizontal-category-label")
            .Attribute("data-cfx-line", lineIndex)
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("text-anchor", "end")
            .Attribute("fill", fill)
            .Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style)))
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", StyleWeight(style, "600"))
            .Raw(SvgTextStyleAttributes(style))
            .Text(label)
            .EndElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static double SvgHorizontalCategoryWrapWidth(Chart chart) => Math.Max(90, Math.Min(230, chart.Options.Size.Width * 0.28));

    private static double WrappedSvgLabelWidth(string label, double fontSize, double maxWidth) {
        var lines = WrapSvgHorizontalCategoryLabel(label, fontSize, maxWidth);
        var widest = 0.0;
        foreach (var line in lines) widest = Math.Max(widest, EstimateTextWidth(line, fontSize));
        return widest;
    }

    private static string[] WrapSvgHorizontalCategoryLabel(string label, double fontSize, double maxWidth) {
        if (EstimateTextWidth(label, fontSize) <= maxWidth) return new[] { label };
        var words = label.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2) return new[] { label };

        var bestIndex = 1;
        var bestScore = double.PositiveInfinity;
        for (var i = 1; i < words.Length; i++) {
            var first = string.Join(" ", words, 0, i);
            var second = string.Join(" ", words, i, words.Length - i);
            var firstWidth = EstimateTextWidth(first, fontSize);
            var secondWidth = EstimateTextWidth(second, fontSize);
            var score = Math.Max(firstWidth, secondWidth) + Math.Abs(firstWidth - secondWidth) * 0.18;
            if (score >= bestScore) continue;
            bestScore = score;
            bestIndex = i;
        }

        return new[] { string.Join(" ", words, 0, bestIndex), string.Join(" ", words, bestIndex, words.Length - bestIndex) };
    }
}
