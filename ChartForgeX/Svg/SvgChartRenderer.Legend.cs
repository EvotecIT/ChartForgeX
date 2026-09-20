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
    private static void DrawLegend(StringBuilder sb, Chart chart, int w, int h) {
        if (!ShouldDrawLegend(chart)) return;
        var t = chart.Options.Theme;
        var area = LegendArea(chart, w, h);
        var rows = BuildLegendRows(chart, area.Width, IsVerticalLegend(chart.Options.LegendPosition) ? area.Height : (double?)null);
        var y = LegendStartY(chart, area, rows.Count);
        var writer = new SvgMarkupWriter(4096);
        writer.StartElement("g").Attribute("data-cfx-role", "legend").Attribute("data-cfx-position", chart.Options.LegendPosition.ToString()).EndStartElement().Line();
        foreach (var row in rows) {
            if (y > area.Bottom - 4) break;
            if (row.Omitted > 0) {
                DrawLegendOverflow(writer, chart, area, y, row.Omitted);
                y += LegendRowHeight(chart);
                continue;
            }
            var xShift = LegendRowX(chart.Options.LegendPosition, area, row.Width);
            writer.StartElement("g").Attribute("data-cfx-role", "legend-row").Attribute("transform", "translate(" + F(area.X + xShift) + " " + F(y) + ")").EndStartElement().Line();
            foreach (var item in row.Items) {
                var series = chart.Series[item.SeriesIndex];
                writer.StartElement("g")
                    .Attribute("data-cfx-role", "legend-item")
                    .Attribute("data-cfx-series", item.SeriesIndex)
                    .Attribute("data-cfx-series-name", series.Name)
                    .Attribute("data-cfx-series-key", SeriesInteractionKey(series));
                if (item.PointIndex >= 0) writer.Attribute("data-cfx-point", item.PointIndex);
                writer.Attribute("data-cfx-kind", series.Kind.ToString()).Attribute("data-cfx-label", item.Label).EndStartElement().Line();
                DrawLegendSymbol(writer, series.Kind, item.X, -4, item.Color, t.CardBackground, (series.MarkerRadius ?? chart.Options.Theme.MarkerRadius) > 0);
                var style = chart.Options.LegendStyle;
                var labelMaxWidth = Math.Max(8, item.Width - 30);
                var labelFontSize = TextFontSizeForSvgWidth(item.Label, labelMaxWidth, StyleFontSize(style, t.LegendFontSize));
                var label = TrimSvgLabelToWidth(item.Label, labelFontSize, labelMaxWidth);
                if (label.Length > 0) {
                    writer.StartElement("text")
                        .Attribute("data-cfx-role", "legend-label")
                        .Attribute("data-cfx-series", item.SeriesIndex)
                        .Attribute("data-cfx-series-name", series.Name)
                        .Attribute("data-cfx-series-key", SeriesInteractionKey(series));
                    if (item.PointIndex >= 0) writer.Attribute("data-cfx-point", item.PointIndex);
                    writer.Attribute("x", item.X + 26).Attribute("y", "0").Attribute("fill", StyleColor(style, t.MutedText).ToCss()).Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style))).Attribute("font-size", labelFontSize).Attribute("font-weight", StyleWeight(style, "600"));
                    WriteSvgTextStyleAttributes(writer, style);
                    WriteSvgStyledTextContent(writer, style, label).EndElement().Line();
                }
                writer.EndElement().Line();
            }

            writer.EndElement().Line();
            y += LegendRowHeight(chart);
        }
        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static List<LegendRow> BuildLegendRows(Chart chart, double width, double? availableHeight = null) {
        var rows = new List<LegendRow>();
        if (chart.Series.Count == 0) return rows;

        var maxX = Math.Max(1, width);
        var vertical = IsVerticalLegend(chart.Options.LegendPosition);
        var row = new LegendRow();
        rows.Add(row);
        var x = 0.0;
        var style = chart.Options.LegendStyle;
        var preferredFontSize = StyleFontSize(style, chart.Options.Theme.LegendFontSize);
        var labelWidthLimit = LegendLabelMaxWidth(width);
        foreach (var entry in BuildLegendEntries(chart, width)) {
            var transformedLabel = StyleText(style, entry.Label);
            var label = TrimSvgLabelToWidth(transformedLabel, preferredFontSize, labelWidthLimit);
            var itemWidth = vertical
                ? Math.Min(maxX, 34 + EstimateTextWidth(label, preferredFontSize) + 18)
                : LegendRowBudget.HorizontalItemWidth(transformedLabel, preferredFontSize, maxX, 52);
            if (row.Items.Count > 0 && (vertical || x + itemWidth > maxX)) {
                row = new LegendRow();
                rows.Add(row);
                x = 0;
            }

            row.Items.Add(new LegendItem(entry.SeriesIndex, entry.PointIndex, x, itemWidth, label, entry.Color));
            row.Width = Math.Max(row.Width, x + itemWidth);
            x += itemWidth;
        }

        return LegendRowBudget.Apply(rows, chart, row => row.Items.Count, omitted => new LegendRow { Omitted = omitted, Width = Math.Min(width, 140) }, availableHeight);
    }

    private static string SeriesInteractionKey(ChartSeries series) => series.InteractionIdentityKey;

    private static void WriteSeriesInteractionMap(SvgMarkupWriter writer, Chart chart) {
        for (var index = 0; index < chart.Series.Count; index++) {
            writer.Attribute("data-cfx-series-key-" + index.ToString(CultureInfo.InvariantCulture), SeriesInteractionKey(chart.Series[index]));
            writer.Attribute("data-cfx-series-name-" + index.ToString(CultureInfo.InvariantCulture), chart.Series[index].Name);
            writer.Attribute("data-cfx-series-source-points-" + index.ToString(CultureInfo.InvariantCulture), chart.Series[index].SourcePointCount);
            writer.Attribute("data-cfx-series-rendered-points-" + index.ToString(CultureInfo.InvariantCulture), chart.Series[index].Points.Count);
            if (chart.Series[index].DecimationMode.HasValue) {
                writer.Attribute("data-cfx-series-decimation-" + index.ToString(CultureInfo.InvariantCulture), chart.Series[index].DecimationMode!.Value.ToString());
                writer.Attribute("data-cfx-series-source-indices-" + index.ToString(CultureInfo.InvariantCulture), string.Join(",", chart.Series[index].SourcePointIndices));
            }
        }
    }

    private static string SvgLegendLabel(Chart chart, int index, double width) => chart.Series[index].Name;

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
        if (IsBottomLegend(chart.Options.LegendPosition)) {
            var precedingRowsHeight = Math.Max(0, rowCount - 1) * LegendRowHeight(chart);
            var bottomInset = Math.Min(24, Math.Max(4, area.Height - 14 - precedingRowsHeight));
            return area.Bottom - bottomInset - precedingRowsHeight;
        }
        return Math.Min(area.Top + 14, area.Bottom - 4);
    }

    private static double LegendSideInset(double availableHeight) => Math.Min(20, Math.Max(0, (availableHeight - 20) / 2.0));

    private static double LegendRowX(ChartLegendPosition position, ChartRect area, double rowWidth) {
        if (position == ChartLegendPosition.TopRight || position == ChartLegendPosition.BottomRight || position == ChartLegendPosition.Right) return area.Width - Math.Min(area.Width, rowWidth);
        if (position == ChartLegendPosition.Top || position == ChartLegendPosition.Bottom) return Math.Max(0, (area.Width - rowWidth) / 2.0);
        return 0;
    }

    private static double LegendRowHeight(Chart chart) => LegendRowBudget.RowHeight(chart);

    private static double LegendBottomReserve(Chart chart) => LegendRowBudget.HorizontalReserve(chart, BuildLegendRows(chart, Math.Max(1, chart.Options.Size.Width - 80)).Count);

    private static bool ShouldDrawLegend(Chart chart) => chart.Options.ShowLegend && chart.Series.Any(series => series.ShowInLegend) && !IsMapChart(chart);

    private static double LegendSideReserve(Chart chart) {
        if (chart.Series.Count == 0) return 0;
        var t = chart.Options.Theme;
        var style = chart.Options.LegendStyle;
        var fontSize = StyleFontSize(style, t.LegendFontSize);
        var entries = BuildLegendEntries(chart, LegendSideReserveMaximumWidth);
        var availableHeight = Math.Max(1, chart.Options.Size.Height - (chart.Options.ShowHeader ? 130 : 78));
        if (LegendRowBudget.MaximumRows(chart, availableHeight) == 0) return 0;
        var visible = LegendRowBudget.VisibleVerticalEntryCount(chart, entries.Count, availableHeight);
        var widest = entries.Take(visible)
            .Select(item => EstimateTextWidth(StyleText(style, item.Label), fontSize))
            .DefaultIfEmpty(0)
            .Max();
        if (visible < entries.Count) widest = Math.Max(widest, EstimateTextWidth(LegendRowBudget.Summary(entries.Count - visible), fontSize));
        return Math.Min(240, Math.Max(124, widest + 54));
    }

    private const double LegendSideReserveMaximumWidth = 240;

    private static bool IsTopLegend(ChartLegendPosition position) => position == ChartLegendPosition.Top || position == ChartLegendPosition.TopLeft || position == ChartLegendPosition.TopRight;

    private static bool IsBottomLegend(ChartLegendPosition position) => position == ChartLegendPosition.Bottom || position == ChartLegendPosition.BottomLeft || position == ChartLegendPosition.BottomRight;

    private static bool IsLeftLegend(ChartLegendPosition position) => position == ChartLegendPosition.Left;

    private static bool IsRightLegend(ChartLegendPosition position) => position == ChartLegendPosition.Right;

    private static bool IsVerticalLegend(ChartLegendPosition position) => IsLeftLegend(position) || IsRightLegend(position);

    private static void DrawLegendSymbol(SvgMarkupWriter writer, ChartSeriesKind kind, double x, double y, ChartColor color, ChartColor background, bool showOptionalLineMarker) {
        if (IsLineLikeLegend(kind)) {
            WriteLegendLineSymbol(writer, x, y, color);
            if (!ChartSeriesKindTraits.UsesOptionalLineMarker(kind) || showOptionalLineMarker) WriteLegendCircleSymbol(writer, x, y, color, background);
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

    private static bool IsLineLikeLegend(ChartSeriesKind kind) => ChartSeriesKindTraits.IsLineLikeLegendKind(kind);

    private sealed class LegendRow {
        public int Omitted { get; set; }
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

}
