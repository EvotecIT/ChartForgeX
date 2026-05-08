using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawLegend(RgbaCanvas c, Chart chart) {
        if (!ShouldDrawLegend(chart)) return;
        var theme = chart.Options.Theme;
        var fontSize = PngLegendFontSize(chart);
        var symbolWidth = 18;
        var rowHeight = fontSize + 6;
        var area = PngLegendArea(chart);
        var rows = BuildPngLegendRows(chart, area.Width);
        var y = PngLegendStartY(chart, area, rows.Count);

        foreach (var row in rows) {
            if (y > area.Bottom) break;
            var x = PngLegendRowX(chart, area, row.Width);
            foreach (var item in row.Items) {
                var itemX = x + item.X;
                DrawLegendSymbol(c, chart.Series[item.SeriesIndex].Kind, itemX, y - 5, item.Color, theme.CardBackground);
                var labelMaxWidth = System.Math.Max(8, item.Width - symbolWidth - 14);
                var labelFontSize = TextFontSizeForEmphasizedWidth(item.Label, labelMaxWidth, fontSize);
                var label = TrimReadablePngLabelToWidth(item.Label, labelFontSize, labelMaxWidth);
                if (label.Length > 0) DrawPngTextStyled(c, itemX + symbolWidth + 8, y - labelFontSize + 3, label, chart.Options.LegendStyle, theme.MutedText, labelFontSize, emphasized: true);
            }

            y += rowHeight;
        }
    }

    private static System.Collections.Generic.List<PngLegendRow> BuildPngLegendRows(Chart chart, double width) {
        var rows = new System.Collections.Generic.List<PngLegendRow>();
        if (chart.Series.Count == 0) return rows;

        var maxX = System.Math.Max(64, width);
        var vertical = PngIsVerticalLegend(chart.Options.LegendPosition);
        var row = new PngLegendRow();
        rows.Add(row);
        var x = 0.0;
        foreach (var entry in BuildPngLegendEntries(chart)) {
            var itemWidth = System.Math.Min(maxX, 46 + EstimatePngEmphasizedTextWidth(entry.Label, PngLegendFontSize(chart)) + 18);
            if (row.Items.Count > 0 && (vertical || x + itemWidth > maxX)) {
                row = new PngLegendRow();
                rows.Add(row);
                x = 0;
            }

            row.Items.Add(new PngLegendItem(entry.SeriesIndex, entry.PointIndex, x, itemWidth, entry.Label, entry.Color));
            row.Width = System.Math.Max(row.Width, x + itemWidth);
            x += itemWidth;
        }

        return rows;
    }

    private static string PngLegendLabel(Chart chart, int index) =>
        TrimReadablePngLabelToWidth(chart.Series[index].Name, PngLegendFontSize(chart), PngLegendLabelMaxWidth(chart));

    private static System.Collections.Generic.List<PngLegendEntry> BuildPngLegendEntries(Chart chart) {
        if (!chart.Options.ShowPointLegend || chart.Series.Count != 1 || !chart.Series[0].ShowInLegend || !CanUsePointLegend(chart.Series[0])) {
            var entries = new System.Collections.Generic.List<PngLegendEntry>();
            for (var i = 0; i < chart.Series.Count; i++) {
                if (chart.Series[i].ShowInLegend) entries.Add(new PngLegendEntry(i, -1, PngLegendLabel(chart, i), SeriesColor(chart, i)));
            }

            return entries;
        }

        var series = chart.Series[0];
        var pointEntries = new System.Collections.Generic.List<PngLegendEntry>();
        var count = VisualPointCount(series);
        for (var i = 0; i < count; i++) {
            var rawIndex = VisualPointRawIndex(series, i);
            if (rawIndex < 0 || rawIndex >= series.Points.Count) continue;
            var label = LegendPointLabel(chart, series.Points[rawIndex], i);
            label = TrimReadablePngLabelToWidth(label, PngLegendFontSize(chart), PngLegendLabelMaxWidth(chart));
            pointEntries.Add(new PngLegendEntry(0, i, label, PngLegendPointColor(chart, series, 0, i)));
        }

        if (pointEntries.Count == 0) pointEntries.Add(new PngLegendEntry(0, -1, PngLegendLabel(chart, 0), SeriesColor(chart, 0)));
        return pointEntries;
    }

    private static ChartColor PngLegendPointColor(Chart chart, ChartSeries series, int seriesIndex, int pointIndex) {
        if (pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue) return series.PointColors[pointIndex]!.Value;
        if (series.Color.HasValue) return series.Color.Value;
        if (PngUsesPalettePointColors(series.Kind)) return chart.Options.Theme.Palette[pointIndex % chart.Options.Theme.Palette.Length];
        return SeriesColor(chart, seriesIndex);
    }

    private static bool PngUsesPalettePointColors(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Funnel || kind == ChartSeriesKind.Pictorial || kind == ChartSeriesKind.ProgressBar || kind == ChartSeriesKind.Treemap || kind == ChartSeriesKind.WordCloud;

    private static ChartRect ApplyPngLegendReserve(Chart chart, ChartRect plot) {
        if (!ShouldDrawLegend(chart)) return plot;
        if (PngIsBottomLegend(chart.Options.LegendPosition)) {
            var maxBottom = System.Math.Max(plot.Top + 1, chart.Options.Size.Height - PngLegendBottomReserve(chart));
            return plot.Bottom <= maxBottom ? plot : new ChartRect(plot.X, plot.Y, plot.Width, System.Math.Max(1, maxBottom - plot.Y));
        }

        if (PngIsTopLegend(chart.Options.LegendPosition)) {
            var reserve = PngLegendBottomReserve(chart);
            return new ChartRect(plot.X, plot.Y + reserve, plot.Width, System.Math.Max(1, plot.Height - reserve));
        }

        if (PngIsLeftLegend(chart.Options.LegendPosition)) {
            var reserve = PngLegendSideReserve(chart) + ChartVisualPrimitives.SideLegendPlotGap;
            return new ChartRect(plot.X + reserve, plot.Y, System.Math.Max(1, plot.Width - reserve), plot.Height);
        }

        if (PngIsRightLegend(chart.Options.LegendPosition)) {
            var reserve = PngLegendSideReserve(chart) + ChartVisualPrimitives.SideLegendPlotGap;
            return new ChartRect(plot.X, plot.Y, System.Math.Max(1, plot.Width - reserve), plot.Height);
        }

        return plot;
    }

    private static bool ShouldDrawLegend(Chart chart) => chart.Options.ShowLegend && PngHasLegendEntries(chart) && !IsMapChart(chart);

    private static bool PngHasLegendEntries(Chart chart) {
        for (var i = 0; i < chart.Series.Count; i++) if (chart.Series[i].ShowInLegend) return true;
        return false;
    }

    private static double PngLegendLabelMaxWidth(Chart chart) {
        var width = PngIsVerticalLegend(chart.Options.LegendPosition) ? 240 : chart.Options.Size.Width - 80;
        return System.Math.Max(48, System.Math.Min(PngIsVerticalLegend(chart.Options.LegendPosition) ? 170 : 260, width * 0.72));
    }

    private static ChartRect PngLegendArea(Chart chart) {
        var padding = 32.0;
        var position = chart.Options.LegendPosition;
        if (PngIsLeftLegend(position)) return new ChartRect(padding, chart.Options.ShowHeader ? 100 : 48, PngLegendSideReserve(chart), System.Math.Max(1, chart.Options.Size.Height - (chart.Options.ShowHeader ? 130 : 78)));
        if (PngIsRightLegend(position)) {
            var width = PngLegendSideReserve(chart);
            return new ChartRect(System.Math.Max(padding, chart.Options.Size.Width - width - padding), chart.Options.ShowHeader ? 100 : 48, width, System.Math.Max(1, chart.Options.Size.Height - (chart.Options.ShowHeader ? 130 : 78)));
        }

        var reserve = PngLegendBottomReserve(chart);
        var y = PngIsTopLegend(position) ? (chart.Options.ShowHeader ? 98 : 44) : System.Math.Max(44, chart.Options.Size.Height - reserve - 4);
        return new ChartRect(40, y, System.Math.Max(1, chart.Options.Size.Width - 80), reserve);
    }

    private static double PngLegendStartY(Chart chart, ChartRect area, int rows) =>
        PngIsBottomLegend(chart.Options.LegendPosition) ? area.Bottom - 24 - System.Math.Max(0, rows - 1) * (PngLegendFontSize(chart) + 6) : area.Top + 14;

    private static double PngLegendRowX(Chart chart, ChartRect area, double rowWidth) {
        var position = chart.Options.LegendPosition;
        if (position == ChartLegendPosition.TopRight || position == ChartLegendPosition.BottomRight || position == ChartLegendPosition.Right) return area.Right - System.Math.Min(area.Width, rowWidth);
        if (position == ChartLegendPosition.Top || position == ChartLegendPosition.Bottom) return area.X + System.Math.Max(0, (area.Width - rowWidth) / 2.0);
        return area.X;
    }

    private static double PngLegendBottomReserve(Chart chart) => 18 + PngLegendRowCount(chart) * (PngLegendFontSize(chart) + 6) + ChartVisualPrimitives.LegendPlotGap;

    private static double PngLegendSideReserve(Chart chart) {
        if (chart.Series.Count == 0) return 0;
        var fontSize = PngLegendFontSize(chart);
        var widest = 0.0;
        foreach (var entry in BuildPngLegendEntries(chart)) widest = System.Math.Max(widest, EstimatePngEmphasizedTextWidth(entry.Label, fontSize));
        return System.Math.Min(240, System.Math.Max(124, widest + 54));
    }

    private static bool PngIsTopLegend(ChartLegendPosition position) => position == ChartLegendPosition.Top || position == ChartLegendPosition.TopLeft || position == ChartLegendPosition.TopRight;

    private static bool PngIsBottomLegend(ChartLegendPosition position) => position == ChartLegendPosition.Bottom || position == ChartLegendPosition.BottomLeft || position == ChartLegendPosition.BottomRight;

    private static bool PngIsLeftLegend(ChartLegendPosition position) => position == ChartLegendPosition.Left;

    private static bool PngIsRightLegend(ChartLegendPosition position) => position == ChartLegendPosition.Right;

    private static bool PngIsVerticalLegend(ChartLegendPosition position) => PngIsLeftLegend(position) || PngIsRightLegend(position);

    private static bool CanUsePointLegend(ChartSeries series) => VisualPointCount(series) > 1;

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
            if (System.Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        }

        return "Item " + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class PngLegendRow {
        public System.Collections.Generic.List<PngLegendItem> Items { get; } = new();
        public double Width { get; set; }
    }

    private sealed class PngLegendItem {
        public PngLegendItem(int seriesIndex, int pointIndex, double x, double width, string label, ChartColor color) {
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

    private readonly struct PngLegendEntry {
        public PngLegendEntry(int seriesIndex, int pointIndex, string label, ChartColor color) {
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

    private static void DrawLegendSymbol(RgbaCanvas c, ChartSeriesKind kind, double x, double y, ChartColor color, ChartColor background) {
        if (IsLineLikeLegend(kind)) {
            c.DrawLine(x, y, x + 18, y, color, ChartVisualPrimitives.LegendLineStrokeWidth);
            c.DrawCircle(x + 9, y, ChartVisualPrimitives.PngLegendMarkerOutlineRadius, background);
            c.DrawCircle(x + 9, y, ChartVisualPrimitives.PngLegendLineMarkerRadius, color);
        } else if (kind == ChartSeriesKind.Scatter || kind == ChartSeriesKind.Bubble) {
            c.DrawCircle(x + 9, y, ChartVisualPrimitives.PngLegendMarkerOutlineRadius, background);
            c.DrawCircle(x + 9, y, ChartVisualPrimitives.LegendMarkerRadius, color);
        } else if (kind == ChartSeriesKind.Candlestick || kind == ChartSeriesKind.Ohlc) {
            c.DrawLine(x + 9, y - 6, x + 9, y + 6, color, ChartVisualPrimitives.LegendFinanceStrokeWidth);
            c.FillRoundedRect(x + 4, y - ChartVisualPrimitives.LegendFinanceBodyHeight / 2, ChartVisualPrimitives.LegendFinanceBodyWidth, ChartVisualPrimitives.LegendFinanceBodyHeight, 1.5, color);
        } else {
            c.FillRoundedRect(x, y - ChartVisualPrimitives.LegendSwatchSize / 2, ChartVisualPrimitives.LegendSwatchSize, ChartVisualPrimitives.LegendSwatchSize, ChartVisualPrimitives.LegendSwatchRadius, color);
        }
    }
}
