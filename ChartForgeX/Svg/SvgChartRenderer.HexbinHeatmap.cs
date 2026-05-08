using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawHexbinHeatmap(StringBuilder sb, Chart chart, ChartRect basePlot) {
        var rows = chart.Series.Where(series => series.Kind == ChartSeriesKind.HexbinHeatmap).ToArray();
        if (rows.Length == 0) return;

        var columns = HeatmapColumns(rows);
        if (columns.Length == 0) return;

        var values = rows.SelectMany(series => series.Points.Select(point => point.Y)).ToArray();
        var min = values.Length == 0 ? 0 : values.Min();
        var max = values.Length == 0 ? 1 : values.Max();
        if (Math.Abs(max - min) < 0.000001) max = min + 1;

        var plot = ApplyHexbinHeatmapReserve(chart, basePlot, rows, columns);
        var layout = ChartHexbinLayout.Build(plot, rows.Length, columns.Length);
        var body = new StringBuilder();

        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++) {
            var series = rows[rowIndex];
            var cy = layout.Top + layout.Radius + rowIndex * layout.RowStep;
            if (chart.Options.ShowAxes) WriteHexbinAxisLabel(body, chart, plot.Left - 12, cy, "end", series.Name, "hexbin-heatmap-row-label");

            for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++) {
                var pointIndex = HeatmapPointIndex(series, columns[columnIndex]);
                if (pointIndex < 0) continue;
                var value = FindHeatmapValue(series, columns[columnIndex]);
                var cx = layout.Left + layout.HexWidth / 2 + columnIndex * layout.ColumnStep + (rowIndex % 2) * layout.HexWidth / 2;
                var color = HeatmapColor(chart, series.Color, value, min, max);
                var status = HeatmapStatus(HeatmapRatio(value, min, max));
                var summary = series.Name + ", " + FormatX(chart, columns[columnIndex]) + ": " + FormatValue(chart, value);
                if (chart.Options.HeatmapScale == ChartHeatmapScale.Semantic) summary += ", " + status;
                WriteHexbinCell(body, chart, rowIndex, columnIndex, cx, cy, layout.Radius, color, status, summary);
                if (ShouldDrawDataLabels(chart, series) && layout.Radius >= 16) {
                    DrawSvgTextCenteredX(body, chart, "data-label", FormatDataLabel(chart, series, pointIndex, value), cx, cy + chart.Options.Theme.DataLabelFontSize * 0.35, HeatmapTextColor(color), chart.Options.Theme.DataLabelFontSize, layout.HexWidth - 8, "750", style: DataLabelStyle(chart, series, pointIndex));
                }
            }
        }

        if (chart.Options.ShowAxes && chart.Options.ShowHeatmapColumnLabels) {
            var columnLabelFontSize = Math.Min(chart.Options.Theme.TickLabelFontSize, Math.Max(9, layout.ColumnStep * 0.32));
            var columnLabelWidth = Math.Max(20, layout.ColumnStep * 0.82);
            for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++) {
                var cx = layout.Left + layout.HexWidth / 2 + columnIndex * layout.ColumnStep;
                WriteHexbinAxisLabel(body, chart, cx, plot.Bottom + 22, "middle", FormatX(chart, columns[columnIndex]), "hexbin-heatmap-column-label", columnLabelFontSize, columnLabelWidth);
            }

            DrawSvgXAxisTitle(body, chart, plot, plot.Bottom + 48, "hexbin-heatmap-x-axis-title");
            if (!string.IsNullOrWhiteSpace(chart.YAxisTitle)) DrawSvgYAxisTitle(body, chart, plot, Math.Max(24, plot.Left - 86), "hexbin-heatmap-y-axis-title");
        }

        if (chart.Options.ShowHeatmapScale) DrawHeatmapScale(body, chart, plot, min, max, rows[0].Color);

        AppendSvg(sb, writer => writer
            .StartElement("g")
            .Attribute("data-cfx-role", "hexbin-heatmap")
            .Attribute("data-cfx-row-count", rows.Length)
            .Attribute("data-cfx-column-count", columns.Length)
            .Attribute("data-cfx-min", min)
            .Attribute("data-cfx-max", max)
            .Raw(Environment.NewLine)
            .Raw(body.ToString())
            .EndElement()
            .Line());
    }

    private static void WriteHexbinCell(StringBuilder sb, Chart chart, int rowIndex, int columnIndex, double cx, double cy, double radius, ChartColor color, string status, string summary) {
        AppendSvg(sb, writer => writer
            .StartElement("polygon")
            .Attribute("class", "cfx-interactive-region")
            .Attribute("tabindex", "0")
            .Attribute("focusable", "true")
            .Attribute("data-cfx-role", "hexbin-cell")
            .Attribute("data-cfx-row", rowIndex)
            .Attribute("data-cfx-column", columnIndex)
            .Attribute("data-cfx-status", status)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .Attribute("points", HexbinPointsAttribute(cx, cy, radius))
            .Attribute("fill", color.ToCss())
            .Attribute("stroke", chart.Options.Theme.CardBackground.ToCss())
            .Attribute("stroke-opacity", ChartVisualPrimitives.HeatmapCellBorderOpacity)
            .Attribute("stroke-width", Math.Max(1, ChartVisualPrimitives.HeatmapCellBorderStrokeWidth + 0.8))
            .EndStartElement()
            .StartElement("title")
            .Text(summary)
            .EndElement()
            .EndElement()
            .Line());
    }

    private static void WriteHexbinAxisLabel(StringBuilder sb, Chart chart, double x, double y, string anchor, string text, string role, double? fontSize = null, double? maxWidth = null) {
        var t = chart.Options.Theme;
        var resolvedFontSize = fontSize ?? t.TickLabelFontSize;
        var resolvedMaxWidth = maxWidth ?? (anchor == "end" ? Math.Max(24, x - 8) : 84);
        var label = TrimSvgLabelToWidth(text, resolvedFontSize, resolvedMaxWidth);
        if (label.Length == 0) return;
        AppendSvg(sb, writer => writer
            .StartElement("text")
            .Attribute("data-cfx-role", role)
            .Attribute("x", x)
            .Attribute("y", y + 4)
            .Attribute("text-anchor", anchor)
            .Attribute("fill", t.MutedText.ToCss())
            .Attribute("font-family", SvgFontFamilyAttributeValue(t.FontFamily))
            .Attribute("font-size", resolvedFontSize)
            .Attribute("font-weight", "650")
            .Text(label)
            .EndElement()
            .Line());
    }

    private static ChartRect ApplyHexbinHeatmapReserve(Chart chart, ChartRect plot, ChartSeries[] rows, double[] columns) {
        var t = chart.Options.Theme;
        var rowLabelReserve = chart.Options.ShowAxes ? rows.Max(series => EstimateTextWidth(series.Name, t.TickLabelFontSize)) + 18 : 0;
        var bottomReserve = chart.Options.ShowAxes ? (chart.Options.ShowHeatmapScale ? 58 : chart.Options.ShowHeatmapColumnLabels ? 38 : 10) : chart.Options.ShowHeatmapScale ? 46 : 0;
        var desiredLeft = plot.Left + rowLabelReserve;
        var maxLeft = Math.Max(plot.Left, plot.Right - 160);
        var left = Math.Max(plot.Left, Math.Min(maxLeft, desiredLeft));
        return new ChartRect(left, plot.Top, Math.Max(1, plot.Right - left), Math.Max(1, plot.Height - bottomReserve));
    }

    private static string HexbinPointsAttribute(double cx, double cy, double radius) {
        var points = ChartHexbinLayout.Points(cx, cy, radius);
        var sb = new StringBuilder();
        for (var i = 0; i < points.Count; i++) {
            if (i > 0) sb.Append(' ');
            sb.Append(F(points[i].X)).Append(',').Append(F(points[i].Y));
        }

        return sb.ToString();
    }
}
