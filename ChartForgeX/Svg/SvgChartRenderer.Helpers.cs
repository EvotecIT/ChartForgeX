using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    // Shared fitted text implementations live in SvgChartRenderer.TextHelpers.cs: DrawSvgTextCenteredX, DrawSvgTextLeft, DrawSvgXAxisTitle, DrawSvgYAxisTitle.
    private static void DrawLegend(StringBuilder sb, Chart chart, int w, int h) {
        if (!ShouldDrawLegend(chart)) return;
        var t = chart.Options.Theme;
        var area = LegendArea(chart, w, h);
        var rows = BuildLegendRows(chart, area.Width);
        var y = LegendStartY(chart, area, rows.Count);
        var writer = new SvgMarkupWriter(4096);
        writer.StartElement("g").Attribute("data-cfx-role", "legend").Attribute("data-cfx-position", chart.Options.LegendPosition.ToString()).EndStartElement().Line();
        foreach (var row in rows) {
            if (y > area.Bottom - 4) break;
            var xShift = LegendRowX(chart.Options.LegendPosition, area, row.Width);
            writer.StartElement("g").Attribute("data-cfx-role", "legend-row").Attribute("transform", "translate(" + F(area.X + xShift) + " " + F(y) + ")").EndStartElement().Line();
            foreach (var item in row.Items) {
                var series = chart.Series[item.SeriesIndex];
                writer.StartElement("g").Attribute("data-cfx-role", "legend-item").Attribute("data-cfx-series", item.SeriesIndex);
                if (item.PointIndex >= 0) writer.Attribute("data-cfx-point", item.PointIndex);
                writer.Attribute("data-cfx-kind", series.Kind.ToString()).Attribute("data-cfx-label", item.Label).EndStartElement().Line();
                DrawLegendSymbol(writer, series.Kind, item.X, -4, item.Color, t.CardBackground);
                var style = chart.Options.LegendStyle;
                var labelMaxWidth = Math.Max(8, item.Width - 30);
                var labelFontSize = TextFontSizeForSvgWidth(item.Label, labelMaxWidth, StyleFontSize(style, t.LegendFontSize));
                var label = TrimSvgLabelToWidth(item.Label, labelFontSize, labelMaxWidth);
                if (label.Length > 0) {
                    writer.StartElement("text").Attribute("data-cfx-role", "legend-label").Attribute("data-cfx-series", item.SeriesIndex);
                    if (item.PointIndex >= 0) writer.Attribute("data-cfx-point", item.PointIndex);
                    writer.Attribute("x", item.X + 26).Attribute("y", "0").Attribute("fill", StyleColor(style, t.MutedText).ToCss()).Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style))).Attribute("font-size", labelFontSize).Attribute("font-weight", StyleWeight(style, "600"));
                    WriteSvgTextStyleAttributes(writer, style);
                    writer.Raw(Escape(label)).EndElement().Line();
                }
                writer.EndElement().Line();
            }

            writer.EndElement().Line();
            y += LegendRowHeight;
        }
        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static List<LegendRow> BuildLegendRows(Chart chart, double width) {
        var rows = new List<LegendRow>();
        if (chart.Series.Count == 0) return rows;

        var maxX = Math.Max(64, width);
        var vertical = IsVerticalLegend(chart.Options.LegendPosition);
        var row = new LegendRow();
        rows.Add(row);
        var x = 0.0;
        foreach (var entry in BuildLegendEntries(chart, width)) {
            var itemWidth = Math.Min(maxX, 34 + EstimateTextWidth(entry.Label, chart.Options.Theme.LegendFontSize) + 18);
            if (row.Items.Count > 0 && (vertical || x + itemWidth > maxX)) {
                row = new LegendRow();
                rows.Add(row);
                x = 0;
            }

            row.Items.Add(new LegendItem(entry.SeriesIndex, entry.PointIndex, x, itemWidth, entry.Label, entry.Color));
            row.Width = Math.Max(row.Width, x + itemWidth);
            x += itemWidth;
        }

        return rows;
    }

    private static string SvgLegendLabel(Chart chart, int index, double width) =>
        TrimSvgLabelToWidth(chart.Series[index].Name, chart.Options.Theme.LegendFontSize, LegendLabelMaxWidth(width));

    private static List<LegendEntry> BuildLegendEntries(Chart chart, double width) {
        if (!chart.Options.ShowPointLegend || chart.Series.Count != 1 || !chart.Series[0].ShowInLegend || !CanUsePointLegend(chart.Series[0])) {
            return chart.Series
                .Select((series, index) => new { series, index })
                .Where(item => item.series.ShowInLegend)
                .Select(item => new LegendEntry(item.index, -1, SvgLegendLabel(chart, item.index, width), Color(chart, item.index)))
                .ToList();
        }

        var series0 = chart.Series[0];
        var entries = new List<LegendEntry>();
        var count = VisualPointCount(series0);
        for (var i = 0; i < count; i++) {
            var rawIndex = VisualPointRawIndex(series0, i);
            if (rawIndex < 0 || rawIndex >= series0.Points.Count) continue;
            var label = LegendPointLabel(chart, series0.Points[rawIndex], i);
            label = TrimSvgLabelToWidth(label, chart.Options.Theme.LegendFontSize, LegendLabelMaxWidth(width));
            entries.Add(new LegendEntry(0, i, label, LegendPointColor(chart, series0, 0, i)));
        }

        return entries.Count == 0 ? new List<LegendEntry> { new(0, -1, SvgLegendLabel(chart, 0, width), Color(chart, 0)) } : entries;
    }

    private static ChartColor LegendPointColor(Chart chart, ChartSeries series, int seriesIndex, int pointIndex) {
        if (pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue) return series.PointColors[pointIndex]!.Value;
        if (series.Color.HasValue) return series.Color.Value;
        if (UsesPalettePointColors(series.Kind)) return chart.Options.Theme.Palette[pointIndex % chart.Options.Theme.Palette.Length];
        return Color(chart, seriesIndex);
    }

    private static bool UsesPalettePointColors(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Funnel || kind == ChartSeriesKind.Pictorial || kind == ChartSeriesKind.ProgressBar || kind == ChartSeriesKind.Treemap || kind == ChartSeriesKind.WordCloud;

    private static double LegendLabelMaxWidth(double width) => Math.Max(48, Math.Min(IsVerticalLegendWidth(width) ? 170 : 260, width * 0.72));

    private static bool IsVerticalLegendWidth(double width) => width <= 230;

    private static ChartRect LegendArea(Chart chart, int w, int h) {
        var padding = 32.0;
        var position = chart.Options.LegendPosition;
        if (IsLeftLegend(position)) return new ChartRect(padding, chart.Options.ShowHeader ? 100 : 48, LegendSideReserve(chart), Math.Max(1, h - (chart.Options.ShowHeader ? 130 : 78)));
        if (IsRightLegend(position)) {
            var width = LegendSideReserve(chart);
            return new ChartRect(Math.Max(padding, w - width - padding), chart.Options.ShowHeader ? 100 : 48, width, Math.Max(1, h - (chart.Options.ShowHeader ? 130 : 78)));
        }

        var y = IsTopLegend(position) ? (chart.Options.ShowHeader ? 98 : 44) : Math.Max(44, h - LegendBottomReserve(chart) - 4);
        return new ChartRect(40, y, Math.Max(1, w - 80), LegendBottomReserve(chart));
    }

    private static double LegendStartY(Chart chart, ChartRect area, int rowCount) {
        if (IsBottomLegend(chart.Options.LegendPosition)) return area.Bottom - 24 - Math.Max(0, rowCount - 1) * LegendRowHeight;
        return area.Top + 14;
    }

    private static double LegendRowX(ChartLegendPosition position, ChartRect area, double rowWidth) {
        if (position == ChartLegendPosition.TopRight || position == ChartLegendPosition.BottomRight || position == ChartLegendPosition.Right) return area.Width - Math.Min(area.Width, rowWidth);
        if (position == ChartLegendPosition.Top || position == ChartLegendPosition.Bottom) return Math.Max(0, (area.Width - rowWidth) / 2.0);
        return 0;
    }

    private static double LegendBottomReserve(Chart chart) => 18 + BuildLegendRows(chart, Math.Max(1, chart.Options.Size.Width - 80)).Count * LegendRowHeight + ChartVisualPrimitives.LegendPlotGap;

    private static bool ShouldDrawLegend(Chart chart) => chart.Options.ShowLegend && chart.Series.Any(series => series.ShowInLegend) && !IsMapChart(chart);

    private static double LegendSideReserve(Chart chart) {
        if (chart.Series.Count == 0) return 0;
        var t = chart.Options.Theme;
        var widest = BuildLegendEntries(chart, LegendSideReserveMaximumWidth).Max(item => EstimateTextWidth(item.Label, t.LegendFontSize));
        return Math.Min(240, Math.Max(124, widest + 54));
    }

    private const double LegendSideReserveMaximumWidth = 240;

    private static bool IsTopLegend(ChartLegendPosition position) => position == ChartLegendPosition.Top || position == ChartLegendPosition.TopLeft || position == ChartLegendPosition.TopRight;

    private static bool IsBottomLegend(ChartLegendPosition position) => position == ChartLegendPosition.Bottom || position == ChartLegendPosition.BottomLeft || position == ChartLegendPosition.BottomRight;

    private static bool IsLeftLegend(ChartLegendPosition position) => position == ChartLegendPosition.Left;

    private static bool IsRightLegend(ChartLegendPosition position) => position == ChartLegendPosition.Right;

    private static bool IsVerticalLegend(ChartLegendPosition position) => IsLeftLegend(position) || IsRightLegend(position);

    private static void DrawLegendSymbol(SvgMarkupWriter writer, ChartSeriesKind kind, double x, double y, ChartColor color, ChartColor background) {
        if (IsLineLikeLegend(kind)) {
            WriteLegendLineSymbol(writer, x, y, color);
            WriteLegendCircleSymbol(writer, x, y, color, background);
        } else if (kind == ChartSeriesKind.Scatter || kind == ChartSeriesKind.Bubble) {
            WriteLegendCircleSymbol(writer, x, y, color, background);
        } else if (kind == ChartSeriesKind.Candlestick || kind == ChartSeriesKind.Ohlc) {
            writer.StartElement("line").Attribute("x1", x + 9).Attribute("y1", y - 6).Attribute("x2", x + 9).Attribute("y2", y + 6).Attribute("stroke", color.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.LegendFinanceStrokeWidth).Attribute("stroke-linecap", "round").EndEmptyElement().Line();
            writer.StartElement("rect").Attribute("x", x + 4).Attribute("y", y - ChartVisualPrimitives.LegendFinanceBodyHeight / 2).Attribute("width", ChartVisualPrimitives.LegendFinanceBodyWidth).Attribute("height", ChartVisualPrimitives.LegendFinanceBodyHeight).Attribute("rx", "1.5").Attribute("fill", color.ToCss()).EndEmptyElement().Line();
        } else {
            writer.StartElement("rect").Attribute("x", x).Attribute("y", y - 5).Attribute("width", "10").Attribute("height", "10").Attribute("rx", "2").Attribute("fill", color.ToCss()).EndEmptyElement().Line();
        }
    }

    private static void WriteLegendLineSymbol(SvgMarkupWriter writer, double x, double y, ChartColor color) {
        writer.StartElement("line").Attribute("x1", x).Attribute("y1", y).Attribute("x2", x + 18).Attribute("y2", y).Attribute("stroke", color.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.LegendLineStrokeWidth).Attribute("stroke-linecap", "round").EndEmptyElement().Line();
    }

    private static void WriteLegendCircleSymbol(SvgMarkupWriter writer, double x, double y, ChartColor color, ChartColor background) {
        writer.StartElement("circle").Attribute("cx", x + 9).Attribute("cy", y).Attribute("r", ChartVisualPrimitives.LegendMarkerRadius).Attribute("fill", color.ToCss()).Attribute("stroke", background.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.LegendMarkerStrokeWidth).EndEmptyElement().Line();
    }

    private static bool IsLineLikeLegend(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Line || kind == ChartSeriesKind.StepLine || kind == ChartSeriesKind.Area || kind == ChartSeriesKind.StepArea || kind == ChartSeriesKind.StackedArea || kind == ChartSeriesKind.Slope || kind == ChartSeriesKind.RangeBand || kind == ChartSeriesKind.RangeArea || kind == ChartSeriesKind.Lollipop || kind == ChartSeriesKind.Dumbbell || kind == ChartSeriesKind.ErrorBar || kind == ChartSeriesKind.Radar || kind == ChartSeriesKind.TrendLine;

    private static void DrawLabelPill(StringBuilder sb, Chart chart, string label, double x, double y, ChartColor textColor, string anchor, ChartRect plot) {
        var t = chart.Options.Theme;
        var maxWidth = Math.Max(42, PlotLabelMaxWidth(plot));
        var fontSize = TextFontSizeForSvgWidth(label, Math.Max(24, maxWidth - 18), t.TickLabelFontSize);
        label = TrimSvgLabelToWidth(label, fontSize, Math.Max(24, maxWidth - 18));
        if (label.Length == 0) return;
        var width = Math.Min(maxWidth, Math.Max(36, EstimateTextWidth(label, fontSize) + 18));
        var placement = PlaceLabelPill(x, width, anchor, plot);
        var textX = placement.Anchor == "end" ? placement.X - 9 : placement.X + 9;
        var rectY = Clamp(y - 16, plot.Top + 5, plot.Bottom - 27);
        var writer = new SvgMarkupWriter(512);
        writer.StartElement("rect").Attribute("data-cfx-role", "annotation-label").Attribute("data-cfx-label", label).Attribute("x", placement.RectX).Attribute("y", rectY).Attribute("width", width).Attribute("height", "23").Attribute("rx", "5").Attribute("fill", t.CardBackground.ToCss()).Attribute("opacity", "0.92").Attribute("stroke", textColor.ToCss()).Attribute("stroke-opacity", "0.36").EndEmptyElement().Line();
        writer.StartElement("text").Attribute("data-cfx-role", "annotation-label-text").Attribute("data-cfx-label", label).Attribute("x", textX).Attribute("y", rectY + 16).Attribute("text-anchor", placement.Anchor).Attribute("fill", textColor.ToCss()).Attribute("font-family", SvgFontFamilyAttributeValue(t.FontFamily)).Attribute("font-size", fontSize).Attribute("font-weight", "750").Raw(Escape(label)).EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void DrawDataLabel(StringBuilder sb, Chart chart, string label, double x, double y, ChartRect plot, string role = "data-label", ChartSeries? series = null, int pointIndex = -1) {
        var t = chart.Options.Theme;
        var style = DataLabelStyle(chart, series, pointIndex);
        var fontSize = StyleFontSize(style, t.DataLabelFontSize);
        label = TrimSvgLabelToWidth(label, fontSize, PlotLabelMaxWidth(plot));
        if (label.Length == 0) return;

        var safeY = Clamp(y, plot.Top + ChartVisualPrimitives.DataLabelPlotInset + fontSize / 2.0, plot.Bottom - ChartVisualPrimitives.DataLabelPlotInset - fontSize / 2.0);
        var anchor = EdgeAwareAnchor(label, x, plot, fontSize);
        var safeX = EdgeAwareTextX(label, x, plot, fontSize);
        var writer = new SvgMarkupWriter(512);
        WriteSvgDataLabelText(writer, chart, style, role, label, safeX, safeY, anchor, t.Text, t.CardBackground, fontSize);
        sb.Append(writer.Build());
    }

    private static bool ShouldDrawDataLabels(Chart chart, ChartSeries series) => series.ShowDataLabels ?? chart.Options.ShowDataLabels;

    private static bool HasHorizontalBarDataLabels(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.HorizontalBar && ShouldDrawDataLabels(chart, series));

    private static ChartDataLabelPlacement DataLabelPlacement(Chart chart, ChartSeries? series) => series?.DataLabelPlacement ?? chart.Options.DataLabelPlacement;

    private static ChartColor DataLabelConnectorColor(Chart chart) => chart.Options.DataLabelConnectorColor ?? chart.Options.Theme.MutedText;

    private static ChartTextStyle SeriesDataLabelStyle(Chart chart, ChartSeries? series) => DataLabelStyle(chart, series);

    private static ChartTextStyle DataLabelStyle(Chart chart, ChartSeries? series, int pointIndex = -1) {
        if (series != null && pointIndex >= 0 && pointIndex < series.PointDataLabelStyles.Count) {
            var pointStyle = series.PointDataLabelStyles[pointIndex];
            if (pointStyle != null && pointStyle.HasOverrides) return pointStyle;
        }

        return series != null && series.DataLabelStyle.HasOverrides ? series.DataLabelStyle : chart.Options.DataLabelStyle;
    }

    private static void DrawHorizontalValueLabel(StringBuilder sb, Chart chart, string label, double x, double y, string anchor, ChartRect plot, ChartSeries? series = null, int pointIndex = -1) {
        var t = chart.Options.Theme;
        var style = DataLabelStyle(chart, series, pointIndex);
        var fontSize = StyleFontSize(style, t.DataLabelFontSize);
        label = TrimSvgLabelToWidth(label, fontSize, PlotLabelMaxWidth(plot));
        if (label.Length == 0) return;

        var width = EstimateTextWidth(label, fontSize);
        var effectiveAnchor = anchor == "end" ? "end" : "start";
        var safeX = effectiveAnchor == "end"
            ? Clamp(x, plot.Left + width + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - ChartVisualPrimitives.DataLabelPlotInset)
            : Clamp(x, plot.Left + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - width - ChartVisualPrimitives.DataLabelPlotInset);
        if (safeX < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) {
            effectiveAnchor = "start";
            safeX = plot.Left + ChartVisualPrimitives.DataLabelPlotInset;
        } else if (safeX > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) {
            effectiveAnchor = "end";
            safeX = plot.Right - ChartVisualPrimitives.DataLabelPlotInset;
        }

        var safeY = Clamp(y, plot.Top + ChartVisualPrimitives.DataLabelPlotInset + fontSize / 2.0, plot.Bottom - ChartVisualPrimitives.DataLabelPlotInset - fontSize / 2.0);
        var writer = new SvgMarkupWriter(512);
        WriteSvgDataLabelText(writer, chart, style, "data-label", label, safeX, safeY, effectiveAnchor, t.Text, t.CardBackground, fontSize);
        sb.Append(writer.Build());
    }

    private static bool ReserveSvgHorizontalLabel(string label, double x, double y, string anchor, Chart chart, ChartRect plot, List<ChartLabelBounds> reserved) {
        var fontSize = chart.Options.Theme.DataLabelFontSize;
        label = TrimSvgLabelToWidth(label, fontSize, PlotLabelMaxWidth(plot));
        if (label.Length == 0) return false;

        var width = EstimateTextWidth(label, fontSize) + 8;
        var height = fontSize + 6;
        var effectiveAnchor = anchor == "end" ? "end" : "start";
        var safeX = effectiveAnchor == "end"
            ? Clamp(x, plot.Left + width + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - ChartVisualPrimitives.DataLabelPlotInset)
            : Clamp(x, plot.Left + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - width - ChartVisualPrimitives.DataLabelPlotInset);
        if (safeX < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) {
            effectiveAnchor = "start";
            safeX = plot.Left + ChartVisualPrimitives.DataLabelPlotInset;
        } else if (safeX > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) {
            effectiveAnchor = "end";
            safeX = plot.Right - ChartVisualPrimitives.DataLabelPlotInset;
        }

        var left = effectiveAnchor == "end" ? safeX - width : safeX;
        var safeY = Clamp(y, plot.Top + ChartVisualPrimitives.DataLabelPlotInset + height / 2.0, plot.Bottom - ChartVisualPrimitives.DataLabelPlotInset - height / 2.0);
        var bounds = new ChartLabelBounds(left, safeY - height / 2, width, height);
        foreach (var item in reserved) if (bounds.Intersects(item)) return false;
        reserved.Add(bounds);
        return true;
    }

    private static LabelPillPlacement PlaceLabelPill(double x, double width, string anchor, ChartRect plot) {
        var minX = plot.Left + 4;
        var maxX = plot.Right - 4;
        var effectiveAnchor = anchor == "end" ? "end" : "start";
        var effectiveX = Clamp(x, minX, maxX);
        var rectX = effectiveAnchor == "end" ? effectiveX - width : effectiveX;

        if (rectX < minX) {
            effectiveAnchor = "start";
            effectiveX = minX;
            rectX = effectiveX;
        }

        if (rectX + width > maxX) {
            effectiveAnchor = "end";
            effectiveX = maxX;
            rectX = effectiveX - width;
        }

        if (rectX < minX) rectX = minX;
        return new LabelPillPlacement(effectiveX, rectX, effectiveAnchor);
    }

    private static string EdgeAwareAnchor(string label, double x, ChartRect plot, double fontSize) {
        var halfWidth = EstimateTextWidth(label, fontSize) / 2;
        if (x - halfWidth < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) return "start";
        if (x + halfWidth > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) return "end";
        return "middle";
    }

    private static double EdgeAwareTextX(string label, double x, ChartRect plot, double fontSize) {
        var halfWidth = EstimateTextWidth(label, fontSize) / 2;
        if (x - halfWidth < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) return plot.Left + ChartVisualPrimitives.DataLabelPlotInset;
        if (x + halfWidth > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) return plot.Right - ChartVisualPrimitives.DataLabelPlotInset;
        return x;
    }

    private static string RotatedAnchor(string label, double x, ChartRect plot, double angle, double fontSize) {
        var projectedWidth = EstimateTextWidth(label, fontSize) * Math.Abs(Math.Cos(angle * Math.PI / 180));
        if (x - projectedWidth < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) return "start";
        if (x + projectedWidth > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) return "end";
        return angle < 0 ? "end" : "start";
    }

    private static double PlotLabelMaxWidth(ChartRect plot) =>
        Math.Max(8, plot.Width - ChartVisualPrimitives.DataLabelPlotInset * 2);

    private static double EstimateTextWidth(string text, double fontSize) {
        var width = 0.0;
        foreach (var ch in text) width += char.IsWhiteSpace(ch) ? fontSize * 0.34 : char.IsUpper(ch) ? fontSize * 0.62 : fontSize * 0.54;
        return width;
    }

    private static string TrimSvgLabelToWidth(string value, double fontSize, double maxWidth) {
        if (string.IsNullOrEmpty(value) || EstimateTextWidth(value, fontSize) <= maxWidth) return value;
        const string suffix = "...";
        if (EstimateTextWidth(suffix, fontSize) > maxWidth) return string.Empty;

        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = low + (high - low + 1) / 2;
            var candidate = value.Substring(0, mid).TrimEnd() + suffix;
            if (EstimateTextWidth(candidate, fontSize) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return low == 0 ? suffix : value.Substring(0, low).TrimEnd() + suffix;
    }

    private static double TextFontSizeForSvgWidth(string text, double maxWidth, double preferredFontSize, double minFontSize = 8) {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0) return preferredFontSize;
        var fontSize = preferredFontSize;
        while (fontSize > minFontSize && EstimateTextWidth(text, fontSize) > maxWidth) fontSize -= 0.5;
        return Math.Max(minFontSize, fontSize);
    }

    private static ChartColor StyleColor(ChartTextStyle? style, ChartColor fallback) => style?.Color ?? fallback;

    private static double StyleFontSize(ChartTextStyle? style, double fallback) => style?.FontSize ?? fallback;

    private static string StyleWeight(ChartTextStyle? style, string fallback) => style?.FontWeight ?? fallback;

    private static string StyleFontFamily(Chart chart, ChartTextStyle? style) => style?.FontFamily ?? chart.Options.Theme.FontFamily;

    private static string SvgTextStyleAttributes(ChartTextStyle? style) {
        if (style == null) return string.Empty;
        var value = string.Empty;
        if (style.Italic) value += " font-style=\"italic\"";
        if (style.Underline) value += " text-decoration=\"underline\"";
        return value;
    }

    private static ChartColor Color(Chart chart, int index) => chart.Series[index].Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];

    private static ChartColor PointColor(Chart chart, ChartSeries series, int seriesIndex, int pointIndex) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? series.PointColors[pointIndex]!.Value
            : Color(chart, seriesIndex);

    private static string BarFill(Chart chart, ChartSeries series, int seriesIndex, int pointIndex, string id) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? series.PointColors[pointIndex]!.Value.ToCss()
            : $"url(#{id}-seriesFill{seriesIndex})";

    private static ChartFillPattern FillPattern(ChartSeries series, int pointIndex) =>
        pointIndex >= 0 && pointIndex < series.PointFillPatterns.Count && series.PointFillPatterns[pointIndex].HasValue
            ? series.PointFillPatterns[pointIndex]!.Value
            : series.FillPattern;

    private static void AppendFillPatternDefinitions(StringBuilder sb, Chart chart, string id) {
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            if (series.FillPattern != ChartFillPattern.None) AppendSvgFillPatternDefinition(sb, FillPatternId(id, seriesIndex, -1), series.FillPattern);
            for (var pointIndex = 0; pointIndex < series.PointFillPatterns.Count; pointIndex++) {
                if (!series.PointFillPatterns[pointIndex].HasValue || series.PointFillPatterns[pointIndex] == ChartFillPattern.None) continue;
                AppendSvgFillPatternDefinition(sb, FillPatternId(id, seriesIndex, pointIndex), series.PointFillPatterns[pointIndex]!.Value);
            }
        }
    }

    private static void AppendSvgFillPatternDefinition(StringBuilder sb, string patternId, ChartFillPattern pattern) {
        var forward = pattern == ChartFillPattern.DiagonalForward || pattern == ChartFillPattern.Crosshatch;
        var backward = pattern == ChartFillPattern.DiagonalBackward || pattern == ChartFillPattern.Crosshatch;
        var opacity = pattern == ChartFillPattern.Crosshatch ? 0.2 : 0.28;
        AppendSvg(sb, writer => {
            writer.StartElement("pattern").Attribute("id", patternId).Attribute("width", "8").Attribute("height", "8").Attribute("patternUnits", "userSpaceOnUse").EndStartElement().Line();
            if (forward) writer.StartElement("path").Attribute("d", "M -2 8 L 8 -2 M 0 10 L 10 0").Attribute("stroke", "#fff").Attribute("stroke-opacity", opacity).Attribute("stroke-width", "1.25").Attribute("stroke-linecap", "round").EndEmptyElement().Line();
            if (backward) writer.StartElement("path").Attribute("d", "M -2 0 L 8 10 M 0 -2 L 10 8").Attribute("stroke", "#fff").Attribute("stroke-opacity", opacity).Attribute("stroke-width", "1.25").Attribute("stroke-linecap", "round").EndEmptyElement().Line();
            writer.EndElement().Line();
        });
    }

    private static string FillPatternId(string id, int seriesIndex, int pointIndex) =>
        pointIndex >= 0 ? $"{id}-fillPattern{seriesIndex}Point{pointIndex}" : $"{id}-fillPattern{seriesIndex}";

    private static string? FillPatternReference(ChartSeries series, int seriesIndex, int pointIndex, string id) {
        var pattern = FillPattern(series, pointIndex);
        if (pattern == ChartFillPattern.None) return null;
        var hasPointPattern = pointIndex >= 0 && pointIndex < series.PointFillPatterns.Count && series.PointFillPatterns[pointIndex].HasValue && series.PointFillPatterns[pointIndex] != ChartFillPattern.None;
        return $"url(#{FillPatternId(id, seriesIndex, hasPointPattern ? pointIndex : -1)})";
    }

    private static void DrawSvgFillPatternOverlay(StringBuilder sb, ChartSeries series, int seriesIndex, int pointIndex, string id, double x, double y, double width, double height, double radius, string role) {
        var writer = new SvgMarkupWriter(512);
        WriteFillPatternOverlay(writer, series, seriesIndex, pointIndex, id, x, y, width, height, radius, role);
        sb.Append(writer.Build());
    }

    private static void WriteFillPatternOverlay(SvgMarkupWriter writer, ChartSeries series, int seriesIndex, int pointIndex, string id, double x, double y, double width, double height, double radius, string role) {
        if (width <= 0.5 || height <= 0.5) return;
        var fill = FillPatternReference(series, seriesIndex, pointIndex, id);
        if (fill == null) return;
        writer.StartElement("rect")
            .Attribute("data-cfx-role", role)
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-fill-pattern", FillPattern(series, pointIndex).ToString())
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", width)
            .Attribute("height", height)
            .Attribute("rx", radius)
            .Attribute("fill", fill)
            .Attribute("pointer-events", "none")
            .EndEmptyElement()
            .Line();
    }

    private static bool ShowXAxis(Chart chart) => !IsMapChart(chart) && chart.Options.ShowAxes && chart.Options.ShowXAxis;

    private static bool ShowYAxis(Chart chart) => !IsMapChart(chart) && chart.Options.ShowAxes && chart.Options.ShowYAxis;

    private static bool ShowAxisLines(Chart chart) => !IsMapChart(chart) && chart.Options.ShowAxes && chart.Options.ShowAxisLines;

    private static bool IsMapChart(Chart chart) => IsCalendarHeatmapChart(chart) || IsDottedMapChart(chart) || IsRegionMapChart(chart) || IsTileMapChart(chart);

    private static bool IsSpatialMapChart(Chart chart) => IsDottedMapChart(chart) || IsRegionMapChart(chart) || IsTileMapChart(chart);

    private static ChartRect SpatialMapPlotArea(Chart chart) {
        var o = chart.Options;
        var left = Math.Min(o.Padding.Left, 42);
        var right = Math.Min(o.Padding.Right, 42);
        var top = o.ShowHeader ? Math.Min(o.Padding.Top + 10, 88) : Math.Min(o.Padding.Top, 42);
        var bottom = Math.Min(o.Padding.Bottom, 42);
        return new ChartRect(left, top, Math.Max(1, o.Size.Width - left - right), Math.Max(1, o.Size.Height - top - bottom));
    }

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatNumber(double v) => ChartNumericFormatter.FormatCompact(v);

    private static string FormatValue(Chart chart, double value) {
        var formatter = chart.Options.ValueFormatter;
        if (formatter == null) return FormatNumber(value);
        return formatter(value) ?? string.Empty;
    }

    private static string FormatDataLabel(Chart chart, ChartSeries series, int pointIndex, double value) {
        if (pointIndex >= 0 && pointIndex < series.PointLabels.Count && series.PointLabels[pointIndex] != null) return series.PointLabels[pointIndex]!;
        return FormatValue(chart, value);
    }

    private static string SeriesSemanticRole(ChartSeries series, string fallback) =>
        string.IsNullOrWhiteSpace(series.SemanticRole) ? fallback : series.SemanticRole!;

    private static string FormatSecondaryValue(Chart chart, double value) {
        var formatter = chart.Options.SecondaryYAxisValueFormatter;
        if (formatter == null) return FormatValue(chart, value);
        return formatter(value) ?? string.Empty;
    }

    private static string FormatPercent(double v) => v.ToString("0.#%", CultureInfo.InvariantCulture);

    private static string SvgFontFamily(string value) => Escape(string.IsNullOrWhiteSpace(value) ? "system-ui, sans-serif" : value);

    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

    private static IReadOnlyList<double> GetXTicks(Chart chart, ChartRange range, ChartRect plot) {
        if (chart.Options.XAxisLabels.Count == 0) {
            var ticks = ChartTicks.GenerateInside(range.MinX, range.MaxX, chart.Options.TickCount);
            if (chart.Options.XAxisLabelDensity == ChartLabelDensity.All || ticks.Count < 3) return ticks;
            var generatedLabels = ticks.Select(tick => new ChartAxisLabel(tick, FormatXAxisValue(chart, tick))).ToArray();
            return SelectXAxisTickValues(chart, range, plot, generatedLabels);
        }

        var labels = chart.Options.XAxisLabels
            .Where(label => label.Value >= range.MinX && label.Value <= range.MaxX)
            .OrderBy(label => label.Value)
            .ToArray();
        return SelectXAxisTickValues(chart, range, plot, labels);
    }

    private static IReadOnlyList<double> SelectXAxisTickValues(Chart chart, ChartRange range, ChartRect plot, IReadOnlyList<ChartAxisLabel> labels) {
        if (chart.Options.XAxisLabelDensity == ChartLabelDensity.All || labels.Count < 3) return labels.Select(label => label.Value).ToArray();
        var widest = labels.Max(label => EstimateTextWidth(label.Text, chart.Options.Theme.TickLabelFontSize));
        var densityFactor = chart.Options.XAxisLabelDensity == ChartLabelDensity.Dense ? 0.72 : chart.Options.XAxisLabelDensity == ChartLabelDensity.Relaxed ? 1.35 : 1.0;
        var minSpacing = Math.Max(28, (widest + 18) * densityFactor);
        var maxCount = Math.Max(2, (int)Math.Floor(plot.Width / minSpacing) + 1);
        if (labels.Count <= maxCount && LabelsHaveMinimumLabelGap(labels, range, plot, chart.Options.Theme.TickLabelFontSize, 6)) return labels.Select(label => label.Value).ToArray();

        var lastLabel = labels[labels.Count - 1];
        var step = Math.Max(1, (int)Math.Ceiling((labels.Count - 1) / (double)(maxCount - 1)));
        var selected = new List<ChartAxisLabel>();
        selected.Add(labels[0]);
        for (var i = step; i < labels.Count - 1; i += step) {
            if (LabelGap(selected[selected.Count - 1], labels[i], range, plot, chart.Options.Theme.TickLabelFontSize) >= 6 &&
                LabelGap(labels[i], lastLabel, range, plot, chart.Options.Theme.TickLabelFontSize) >= 6) selected.Add(labels[i]);
        }

        if (selected.Count > 1 && LabelGap(selected[selected.Count - 1], lastLabel, range, plot, chart.Options.Theme.TickLabelFontSize) < 6) selected.RemoveAt(selected.Count - 1);
        selected.Add(lastLabel);
        return selected.Select(label => label.Value).ToArray();
    }

    private static bool LabelsHaveMinimumLabelGap(IReadOnlyList<ChartAxisLabel> labels, ChartRange range, ChartRect plot, double fontSize, double minGap) {
        for (var i = 1; i < labels.Count; i++) {
            if (LabelGap(labels[i - 1], labels[i], range, plot, fontSize) < minGap) return false;
        }

        return true;
    }

    private static double LabelGap(ChartAxisLabel left, ChartAxisLabel right, ChartRange range, ChartRect plot, double fontSize) {
        var leftWidth = EstimateTextWidth(left.Text, fontSize);
        var rightWidth = EstimateTextWidth(right.Text, fontSize);
        var leftX = Clamp(ProjectX(left.Value, range, plot) - leftWidth / 2.0, plot.Left + 2, plot.Right - leftWidth - 2);
        var rightX = Clamp(ProjectX(right.Value, range, plot) - rightWidth / 2.0, plot.Left + 2, plot.Right - rightWidth - 2);
        return rightX - (leftX + leftWidth);
    }

    private static IReadOnlyList<double> GetHorizontalCategoryTicks(Chart chart, ChartRange range) {
        var categories = new SortedSet<double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var point in series.Points) {
                if (point.X >= range.MinY && point.X <= range.MaxY) categories.Add(point.X);
            }
        }

        if (categories.Count > 0) return categories.ToArray();
        return ChartTicks.GenerateInside(range.MinY, range.MaxY, chart.Options.TickCount);
    }

    private static double ProjectX(double value, ChartRange range, ChartRect plot) {
        var span = range.MaxX - range.MinX;
        if (Math.Abs(span) < 0.0000001) return plot.Left + plot.Width / 2;
        return plot.Left + (value - range.MinX) / span * plot.Width;
    }

    private static string FormatX(Chart chart, double value) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - value) < 0.000001) return label.Text;
        }

        return FormatXAxisValue(chart, value);
    }

    private static string FormatXAxisValue(Chart chart, double value) {
        var formatter = chart.Options.XAxisValueFormatter;
        if (formatter == null) return FormatNumber(value);
        return formatter(value) ?? string.Empty;
    }

    private static string BuildLinePath(IReadOnlyList<ChartPoint> points, bool smooth) {
        return BuildPath(ChartPathBuilder.FromPoints(points, ChartSeriesKind.Line, smooth));
    }

    private static string BuildStepLinePath(IReadOnlyList<ChartPoint> points) {
        return BuildPath(ChartPathBuilder.FromPoints(points, ChartSeriesKind.StepLine, false));
    }

    private static string BuildPath(ChartPath chartPath) {
        if (chartPath.Commands.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        foreach (var command in chartPath.Commands) {
            if (command.Kind == ChartPathCommandKind.MoveTo) {
                sb.Append("M ").Append(F(command.X)).Append(' ').Append(F(command.Y));
            } else if (command.Kind == ChartPathCommandKind.LineTo) {
                sb.Append(" L ").Append(F(command.X)).Append(' ').Append(F(command.Y));
            } else if (command.Kind == ChartPathCommandKind.CubicTo) {
                sb.Append(" C ").Append(F(command.Control1X)).Append(' ').Append(F(command.Control1Y)).Append(' ')
                    .Append(F(command.Control2X)).Append(' ').Append(F(command.Control2Y)).Append(' ')
                    .Append(F(command.X)).Append(' ').Append(F(command.Y));
            }
        }

        return sb.ToString();
    }

    private static string BuildId(Chart chart, string idScope) {
        unchecked {
            uint hash = 2166136261;
            Add(ref hash, idScope ?? string.Empty);
            Add(ref hash, chart.Title);
            Add(ref hash, chart.Subtitle);
            Add(ref hash, chart.Options.Size.Width.ToString(CultureInfo.InvariantCulture));
            Add(ref hash, chart.Options.Size.Height.ToString(CultureInfo.InvariantCulture));
            foreach (var series in chart.Series) {
                Add(ref hash, series.Name);
                Add(ref hash, series.Kind.ToString());
                Add(ref hash, series.ShowInLegend.ToString(CultureInfo.InvariantCulture));
                Add(ref hash, series.SemanticRole ?? string.Empty);
                Add(ref hash, series.FillPattern.ToString());
                foreach (var point in series.Points) {
                    Add(ref hash, point.X.ToString("R", CultureInfo.InvariantCulture));
                    Add(ref hash, point.Y.ToString("R", CultureInfo.InvariantCulture));
                }
                foreach (var label in series.PointLabels) Add(ref hash, label ?? string.Empty);
                foreach (var pattern in series.PointFillPatterns) Add(ref hash, pattern?.ToString() ?? string.Empty);
            }

            return "cfx" + hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }

    private static void Add(ref uint hash, string value) {
        AddRaw(ref hash, value.Length.ToString(CultureInfo.InvariantCulture));
        AddRaw(ref hash, ":");
        AddRaw(ref hash, value);
        AddRaw(ref hash, "|");
    }

    private static void AddRaw(ref uint hash, string value) {
        foreach (var ch in value) {
            hash ^= ch;
            hash *= 16777619;
        }
    }

    private static int VisualPointCount(ChartSeries series) {
        var tupleSize = VisualTupleSize(series.Kind);
        return tupleSize <= 1 ? series.Points.Count : series.Points.Count / tupleSize;
    }

    private static int VisualPointRawIndex(ChartSeries series, int pointIndex) {
        var tupleSize = VisualTupleSize(series.Kind);
        return tupleSize <= 1 ? pointIndex : pointIndex * tupleSize;
    }

    private static int VisualTupleSize(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Bubble || kind == ChartSeriesKind.RangeBand || kind == ChartSeriesKind.RangeArea || kind == ChartSeriesKind.RangeBar || kind == ChartSeriesKind.Dumbbell
            ? 2
            : kind == ChartSeriesKind.ErrorBar
                ? 3
                : kind == ChartSeriesKind.Candlestick || kind == ChartSeriesKind.Ohlc
                    ? 4
                    : kind == ChartSeriesKind.BoxPlot
                        ? 5
                        : 1;

    private static string LegendPointLabel(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        }

        return "Item " + (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private sealed class LegendRow {
        public List<LegendItem> Items { get; } = new();

        public double Width { get; set; }
    }

    private readonly struct LegendItem {
        public LegendItem(int seriesIndex, int pointIndex, double x, double width, string label, ChartColor color) {
            SeriesIndex = seriesIndex;
            PointIndex = pointIndex;
            X = x;
            Width = width;
            Label = label;
            Color = color;
        }

        public int SeriesIndex { get; }

        public int PointIndex { get; }

        public double X { get; }

        public double Width { get; }

        public string Label { get; }

        public ChartColor Color { get; }
    }

    private readonly struct LegendEntry {
        public LegendEntry(int seriesIndex, int pointIndex, string label, ChartColor color) {
            SeriesIndex = seriesIndex;
            PointIndex = pointIndex;
            Label = label;
            Color = color;
        }

        public int SeriesIndex { get; }

        public int PointIndex { get; }

        public string Label { get; }

        public ChartColor Color { get; }
    }

    private readonly struct BarLayoutInfo {
        public BarLayoutInfo(double barWidth, double offset) {
            BarWidth = barWidth;
            Offset = offset;
        }

        public double BarWidth { get; }

        public double Offset { get; }
    }

    private readonly struct HorizontalBarLayoutInfo {
        public HorizontalBarLayoutInfo(double barHeight, double offset) {
            BarHeight = barHeight;
            Offset = offset;
        }

        public double BarHeight { get; }

        public double Offset { get; }
    }

    private readonly struct LabelPillPlacement {
        public LabelPillPlacement(double x, double rectX, string anchor) {
            X = x;
            RectX = rectX;
            Anchor = anchor;
        }

        public double X { get; }

        public double RectX { get; }

        public string Anchor { get; }
    }
}
