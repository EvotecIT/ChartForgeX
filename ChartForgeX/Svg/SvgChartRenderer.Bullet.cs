using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawBullet(StringBuilder sb, Chart chart, ChartRect basePlot, string id) {
        var rows = chart.Series
            .Select((series, index) => new { series, index })
            .Where(item => item.series.Kind == ChartSeriesKind.Bullet && item.series.Points.Count >= 2)
            .ToArray();
        if (rows.Length == 0) return;

        var t = chart.Options.Theme;
        var labeledRows = rows.Where(row => row.series.ShowDataLabels != false).ToArray();
        var labelReserve = labeledRows.Length == 0 ? 10 : Math.Min(240, Math.Max(128, labeledRows.Max(row => EstimateTextWidth(row.series.Name, t.LegendFontSize)) + 34));
        var valueReserve = labeledRows.Length == 0 ? 12 : Math.Min(142, Math.Max(84, labeledRows.Max(row => EstimateTextWidth(FormatValue(chart, BulletValue(row.series)), t.DataLabelFontSize)) + 38));
        var content = BulletContentBounds(basePlot);
        FitBulletReserves(content.Width, ref labelReserve, ref valueReserve);
        var plot = new ChartRect(content.X + labelReserve, content.Y + 18, Math.Max(1, content.Width - labelReserve - valueReserve), Math.Max(1, content.Height - 54));
        var rowHeight = Math.Min(64, plot.Height / Math.Max(1, rows.Length));
        var barHeight = Math.Max(16, Math.Min(26, rowHeight * 0.38));

        var writer = new SvgMarkupWriter(4096);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "bullet-chart")
            .EndStartElement()
            .Line();
        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++) {
            var row = rows[rowIndex];
            var y = plot.Top + rowHeight * rowIndex + rowHeight / 2;
            var min = BulletMin(row.series);
            var max = BulletMax(row.series);
            if (Math.Abs(max - min) < 0.000001) max = min + 1;
            var accent = row.series.Color ?? chart.Options.Theme.Palette[row.index % chart.Options.Theme.Palette.Length];
            var actualValue = BulletValue(row.series);
            var targetValue = BulletTarget(row.series);
            var status = BulletStatus(actualValue, targetValue);
            var rowSummary = row.series.Name + ": " + FormatValue(chart, actualValue) + ", target " + FormatValue(chart, targetValue) + ", " + status.Replace("-", " ");
            var statusColor = BulletStatusColor(t, status);
            var rowLabelMaxWidth = Math.Max(8, labelReserve - 16);
            var rowLabelFontSize = TextFontSizeForSvgWidth(row.series.Name, rowLabelMaxWidth, t.LegendFontSize);
            var rowLabel = TrimSvgLabelToWidth(row.series.Name, rowLabelFontSize, rowLabelMaxWidth);
            var rawValueLabel = FormatValue(chart, actualValue);
            var valueLabelMaxWidth = Math.Max(8, valueReserve - 24);
            var valueLabelFontSize = TextFontSizeForSvgWidth(rawValueLabel, valueLabelMaxWidth, t.DataLabelFontSize);
            var valueLabel = TrimSvgLabelToWidth(rawValueLabel, valueLabelFontSize, valueLabelMaxWidth);
            var showLabels = row.series.ShowDataLabels != false;

            writer
                .StartElement("g")
                .Attribute("data-cfx-role", "bullet-row")
                .Attribute("data-cfx-series", row.index)
                .Attribute("data-cfx-status", status)
                .Attribute("data-cfx-label", row.series.Name)
                .Attribute("data-cfx-value", F(actualValue))
                .Attribute("data-cfx-target", F(targetValue))
                .Attribute("data-cfx-min", F(min))
                .Attribute("data-cfx-max", F(max))
                .Attribute("role", "group")
                .Attribute("aria-label", rowSummary)
                .EndStartElement()
                .Line();
            if (showLabels && rowLabel.Length > 0) {
                WriteBulletText(writer, "bullet-row-label", content.Left, y + 4, rowLabel, t.Text.ToCss(), BulletFontFamily(t.FontFamily), rowLabelFontSize, "700");
            }
            DrawBulletRanges(writer, row.series, plot, y, barHeight, min, max, accent);

            var value = Clamp(actualValue, min, max);
            var target = Clamp(targetValue, min, max);
            var valueX = BulletX(plot, min, max, value);
            var targetX = BulletX(plot, min, max, target);
            writer
                .StartElement("rect")
                .Attribute("data-cfx-role", "bullet-value")
                .Attribute("data-cfx-series", row.index)
                .Attribute("data-cfx-value", F(actualValue))
                .Attribute("x", F(plot.Left))
                .Attribute("y", F(y - barHeight * 0.24))
                .Attribute("width", F(Math.Max(2, valueX - plot.Left)))
                .Attribute("height", F(barHeight * 0.48))
                .Attribute("rx", F(barHeight * 0.24))
                .Attribute("fill", "url(#" + id + "-seriesFill" + row.index + ")")
                .EndEmptyElement()
                .Line()
                .StartElement("line")
                .Attribute("data-cfx-role", "bullet-target")
                .Attribute("data-cfx-series", row.index)
                .Attribute("data-cfx-target", F(targetValue))
                .Attribute("x1", F(targetX))
                .Attribute("y1", F(y - barHeight * 0.68))
                .Attribute("x2", F(targetX))
                .Attribute("y2", F(y + barHeight * 0.68))
                .Attribute("stroke", t.Text.ToCss())
                .Attribute("stroke-width", F(ChartVisualPrimitives.BulletTargetStrokeWidth))
                .Attribute("stroke-linecap", "round")
                .EndEmptyElement()
                .Line();
            if (showLabels) {
                DrawBulletTargetLabel(writer, chart, FormatValue(chart, targetValue), targetX, y - barHeight * 0.92, plot);
                writer
                    .StartElement("circle")
                    .Attribute("data-cfx-role", "bullet-status-marker")
                    .Attribute("data-cfx-status", status)
                    .Attribute("cx", F(plot.Right + 8))
                    .Attribute("cy", F(y))
                    .Attribute("r", F(ChartVisualPrimitives.StatusMarkerRadius))
                    .Attribute("fill", statusColor.ToCss())
                    .Attribute("stroke", t.CardBackground.ToCss())
                    .Attribute("stroke-width", F(ChartVisualPrimitives.StatusMarkerStrokeWidth))
                    .EndEmptyElement()
                    .Line();
                if (valueLabel.Length > 0) {
                    WriteBulletText(writer, "bullet-value-label", plot.Right + 18, y + 4, valueLabel, t.Text.ToCss(), BulletFontFamily(t.FontFamily), valueLabelFontSize, "800");
                }
            }
            writer.EndElement().Line();
        }

        DrawBulletAxis(writer, chart, plot, rows[0].series, content.Bottom - 12);
        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static ChartRect BulletContentBounds(ChartRect basePlot) =>
        new(
            basePlot.X + ChartVisualPrimitives.BulletContentInset,
            basePlot.Y + ChartVisualPrimitives.BulletContentInset,
            Math.Max(1, basePlot.Width - ChartVisualPrimitives.BulletContentInset * 2),
            Math.Max(1, basePlot.Height - ChartVisualPrimitives.BulletContentInset * 2));

    private static void FitBulletReserves(double contentWidth, ref double labelReserve, ref double valueReserve) {
        var minimumPlotWidth = Math.Min(80, Math.Max(1, contentWidth * 0.25));
        var reserveBudget = Math.Max(0, contentWidth - minimumPlotWidth);
        var totalReserve = labelReserve + valueReserve;
        if (totalReserve <= reserveBudget || totalReserve <= 0) return;
        var ratio = reserveBudget / totalReserve;
        labelReserve *= ratio;
        valueReserve *= ratio;
    }

    private static void DrawBulletTargetLabel(SvgMarkupWriter writer, Chart chart, string label, double x, double y, ChartRect plot) {
        var t = chart.Options.Theme;
        var text = "target " + label;
        var maxWidth = Math.Max(8, plot.Width - 8);
        var fontSize = TextFontSizeForSvgWidth(text, maxWidth, t.TickLabelFontSize);
        text = TrimSvgLabelToWidth(text, fontSize, maxWidth);
        if (text.Length == 0) return;

        var width = EstimateTextWidth(text, fontSize);
        var anchor = EdgeAwareAnchor(text, x, plot, fontSize);
        var safeX = Clamp(x, plot.Left + width / 2 + 4, plot.Right - width / 2 - 4);
        if (anchor == "start") safeX = Clamp(x, plot.Left + 4, plot.Right - width - 4);
        if (anchor == "end") safeX = Clamp(x, plot.Left + width + 4, plot.Right - 4);
        var safeY = Clamp(y, plot.Top + fontSize, plot.Bottom - 4);
        writer
            .StartElement("text")
            .Attribute("data-cfx-role", "bullet-target-label")
            .Attribute("x", F(safeX))
            .Attribute("y", F(safeY))
            .Attribute("text-anchor", anchor)
            .Attribute("fill", t.MutedText.ToCss())
            .Attribute("stroke", t.CardBackground.ToCss())
            .Attribute("stroke-width", "3")
            .Attribute("paint-order", "stroke fill")
            .Attribute("stroke-linejoin", "round")
            .Attribute("font-family", BulletFontFamily(t.FontFamily))
            .Attribute("font-size", F(fontSize))
            .Attribute("font-weight", "650")
            .Raw(Escape(text))
            .EndElement()
            .Line();
    }

    private static void DrawBulletRanges(SvgMarkupWriter writer, ChartSeries series, ChartRect plot, double y, double barHeight, double min, double max, ChartColor accent) {
        var previous = min;
        var ends = BulletRangeEnds(series, min, max);
        for (var i = 0; i < ends.Length; i++) {
            var end = Clamp(ends[i], min, max);
            if (end <= previous) continue;
            var x = BulletX(plot, min, max, previous);
            var width = BulletX(plot, min, max, end) - x;
            var opacity = Math.Max(0.10, 0.30 - i * 0.055);
            writer
                .StartElement("rect")
                .Attribute("data-cfx-role", "bullet-range")
                .Attribute("x", F(x))
                .Attribute("y", F(y - barHeight / 2))
                .Attribute("width", F(width))
                .Attribute("height", F(barHeight))
                .Attribute("rx", F(barHeight / 2))
                .Attribute("fill", accent.ToCss())
                .Attribute("opacity", F(opacity))
                .EndEmptyElement()
                .Line();
            previous = end;
        }
    }

    private static void DrawBulletAxis(SvgMarkupWriter writer, Chart chart, ChartRect plot, ChartSeries reference, double y) {
        if (!chart.Options.ShowAxes) return;
        var t = chart.Options.Theme;
        var min = BulletMin(reference);
        var max = BulletMax(reference);
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var ticks = new[] { min, min + (max - min) / 2, max };
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "bullet-axis")
            .EndStartElement()
            .Line()
            .StartElement("line")
            .Attribute("x1", F(plot.Left))
            .Attribute("y1", F(y))
            .Attribute("x2", F(plot.Right))
            .Attribute("y2", F(y))
            .Attribute("stroke", t.Axis.ToCss())
            .Attribute("stroke-width", F(ChartVisualPrimitives.BulletAxisStrokeWidth))
            .EndEmptyElement()
            .Line();
        foreach (var tick in ticks) {
            var x = BulletX(plot, min, max, tick);
            var label = FormatValue(chart, tick);
            var anchor = EdgeAwareAnchor(label, x, plot, t.TickLabelFontSize);
            var safeX = EdgeAwareTextX(label, x, plot, t.TickLabelFontSize);
            writer
                .StartElement("line")
                .Attribute("data-cfx-role", "bullet-axis-tick")
                .Attribute("x1", F(x))
                .Attribute("y1", F(y - 4))
                .Attribute("x2", F(x))
                .Attribute("y2", F(y + 4))
                .Attribute("stroke", t.Axis.ToCss())
                .Attribute("stroke-width", F(ChartVisualPrimitives.BulletAxisStrokeWidth))
                .EndEmptyElement()
                .Line();
            WriteBulletText(writer, "bullet-axis-label", safeX, y + 20, label, t.MutedText.ToCss(), BulletFontFamily(t.FontFamily), t.TickLabelFontSize, null, anchor);
        }
        writer.EndElement().Line();
    }

    private static void WriteBulletText(SvgMarkupWriter writer, string role, double x, double y, string text, string fill, string fontFamily, double fontSize, string? fontWeight, string? textAnchor = null) {
        writer
            .StartElement("text")
            .Attribute("data-cfx-role", role)
            .Attribute("x", F(x))
            .Attribute("y", F(y));
        if (textAnchor != null) writer.Attribute("text-anchor", textAnchor);
        writer
            .Attribute("fill", fill)
            .Attribute("font-family", fontFamily)
            .Attribute("font-size", F(fontSize));
        if (fontWeight != null) writer.Attribute("font-weight", fontWeight);
        writer
            .Raw(Escape(text))
            .EndElement()
            .Line();
    }

    private static string BulletFontFamily(string value) => string.IsNullOrWhiteSpace(value) ? "system-ui, sans-serif" : value;

    private static double BulletMin(ChartSeries series) => series.Points[0].X;

    private static double BulletMax(ChartSeries series) => series.Points[1].X;

    private static double BulletValue(ChartSeries series) => series.Points[0].Y;

    private static double BulletTarget(ChartSeries series) => series.Points[1].Y;

    private static double BulletX(ChartRect plot, double min, double max, double value) => plot.Left + (value - min) / (max - min) * plot.Width;

    private static string BulletStatus(double value, double target) {
        if (value < target - 0.000001) return "below-target";
        if (value > target + 0.000001) return "above-target";
        return "meets-target";
    }

    private static ChartColor BulletStatusColor(ChartForgeX.Themes.ChartTheme theme, string status) {
        return status == "below-target" ? theme.Negative : theme.Positive;
    }

    private static double[] BulletRangeEnds(ChartSeries series, double min, double max) {
        var ends = series.Points.Skip(2).Select(point => point.X).Where(value => value > min && value < max).OrderBy(value => value).ToList();
        if (ends.Count == 0) {
            var span = max - min;
            ends.Add(min + span * 0.6);
            ends.Add(min + span * 0.8);
        }

        ends.Add(max);
        return ends.Distinct().ToArray();
    }
}
