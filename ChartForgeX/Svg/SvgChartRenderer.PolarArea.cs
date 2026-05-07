using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawPolarArea(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        var series = chart.Series[0];
        var values = series.Points
            .Select((point, index) => new IndexedPieValue(point, index))
            .Where(item => item.Point.Y > 0)
            .ToArray();
        if (values.Length == 0) return;
        var legendValues = series.Points
            .Select((point, index) => new IndexedPieValue(point, index))
            .ToArray();

        var t = chart.Options.Theme;
        var max = values.Max(item => item.Point.Y);
        var total = values.Sum(item => item.Point.Y);
        var chartPlot = PieChartPlot(chart, plot, legendValues);
        var radius = Math.Max(ChartVisualPrimitives.PolarAreaMinRadius, Math.Min(chartPlot.Width, chartPlot.Height) * ChartVisualPrimitives.PolarAreaRadiusFactor);
        var cx = chartPlot.Left + chartPlot.Width * ChartVisualPrimitives.PolarAreaCenterXFactor;
        var cy = chartPlot.Top + chartPlot.Height / 2;
        var sweep = Math.PI * 2 / series.Points.Count;

        var writer = new SvgMarkupWriter(4096);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "polar-area-chart")
            .EndStartElement()
            .Line();
        if (chart.Options.ShowGrid) DrawPolarAreaGrid(writer, chart, cx, cy, radius);
        DrawPolarAreaZeroSlots(writer, chart, series, cx, cy, radius, sweep);
        for (var i = 0; i < values.Length; i++) {
            var point = values[i].Point;
            var pointIndex = values[i].PointIndex;
            var start = -Math.PI / 2 + pointIndex * sweep;
            var end = start + sweep;
            var segmentRadius = radius * Math.Sqrt(point.Y / max);
            var label = SliceLabel(chart, point, pointIndex);
            var percent = point.Y / total;
            var summary = label + ": " + FormatValue(chart, point.Y) + ", " + FormatPercent(percent);
            writer
                .StartElement("path")
                .Attribute("data-cfx-role", "polar-area-segment")
                .Attribute("data-cfx-point", pointIndex)
                .Attribute("data-cfx-label", label)
                .Attribute("data-cfx-value", point.Y)
                .Attribute("data-cfx-percent", percent)
                .Attribute("role", "img")
                .Attribute("aria-label", summary)
                .Attribute("d", BuildSlicePath(cx, cy, segmentRadius, 0, start, end))
                .Attribute("fill", PieSliceFill(chart, series, pointIndex, id))
                .Attribute("stroke", t.CardBackground.ToCss())
                .Attribute("stroke-width", ChartVisualPrimitives.SliceSeparatorStrokeWidth)
                .EndEmptyElement()
                .Line();

            if (ShouldDrawDataLabels(chart, series) && segmentRadius > ChartVisualPrimitives.PolarAreaLabelMinRadius) {
                var mid = start + sweep / 2;
                var labelRadius = segmentRadius * ChartVisualPrimitives.PolarAreaLabelRadiusFactor;
                DrawDataLabel(sb, chart, FormatValue(chart, point.Y), cx + Math.Cos(mid) * labelRadius, cy + Math.Sin(mid) * labelRadius, plot, "polar-area-label", series, pointIndex);
            }
        }

        sb.Append(writer.ToString());
        if (chart.Options.ShowLegend) DrawSliceLegend(sb, chart, series, legendValues, plot, total);
        sb.AppendLine("</g>");
    }

    private static void DrawPolarAreaZeroSlots(SvgMarkupWriter writer, Chart chart, ChartSeries series, double cx, double cy, double radius, double sweep) {
        for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
            var point = series.Points[pointIndex];
            if (point.Y > 0) continue;
            var start = -Math.PI / 2 + pointIndex * sweep;
            var end = start + sweep;
            var label = SliceLabel(chart, point, pointIndex);
            var summary = label + ": " + FormatValue(chart, point.Y) + ", 0%";
            var color = PieSliceColor(chart, series, pointIndex);
            var inner = radius * ChartVisualPrimitives.PolarAreaZeroSlotInnerRadiusFactor;
            writer
                .StartElement("path")
                .Attribute("data-cfx-role", "polar-area-zero-slot")
                .Attribute("data-cfx-point", pointIndex)
                .Attribute("data-cfx-label", label)
                .Attribute("data-cfx-value", point.Y)
                .Attribute("data-cfx-percent", 0)
                .Attribute("data-cfx-inner-radius-factor", ChartVisualPrimitives.PolarAreaZeroSlotInnerRadiusFactor)
                .Attribute("role", "img")
                .Attribute("aria-label", summary)
                .Attribute("d", BuildSlicePath(cx, cy, radius, inner, start, end))
                .Attribute("fill", color.ToCss())
                .Attribute("fill-opacity", ChartVisualPrimitives.PolarAreaZeroSlotFillOpacity)
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-opacity", ChartVisualPrimitives.PolarAreaZeroSlotStrokeOpacity)
                .Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth)
                .Attribute("stroke-dasharray", "3 4")
                .EndEmptyElement()
                .Line();
        }
    }

    private static void DrawPolarAreaGrid(SvgMarkupWriter writer, Chart chart, double cx, double cy, double radius) {
        var t = chart.Options.Theme;
        for (var i = 1; i <= ChartVisualPrimitives.PolarAreaGridRings; i++) {
            var r = radius * i / ChartVisualPrimitives.PolarAreaGridRings;
            writer
                .StartElement("circle")
                .Attribute("data-cfx-role", "polar-area-ring")
                .Attribute("cx", cx)
                .Attribute("cy", cy)
                .Attribute("r", r)
                .Attribute("fill", "none")
                .Attribute("stroke", t.Grid.ToCss())
                .Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth)
                .Attribute("opacity", ChartVisualPrimitives.PolarAreaGridOpacity)
                .EndEmptyElement()
                .Line();
        }
    }
}
