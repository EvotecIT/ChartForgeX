using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawPolar(StringBuilder sb, Chart chart, ChartRect plot) {
        var seriesItems = chart.Series
            .Select((series, index) => new { series, index })
            .Where(item => item.series.Kind == ChartSeriesKind.Polar && item.series.Points.Count > 0)
            .ToArray();
        if (seriesItems.Length == 0) return;

        var geometry = PolarChartGeometry.Create(chart, plot);
        AppendSvgStart(sb, writer => writer.StartElement("g").Attribute("data-cfx-role", "polar-chart").EndStartElement().Line());
        DrawPolarGrid(sb, chart, plot, geometry);
        foreach (var item in seriesItems) {
            var mapped = item.series.Points.Select(geometry.Map).ToArray();
            var color = item.series.Color ?? chart.Options.Theme.Palette[item.index % chart.Options.Theme.Palette.Length];
            var path = BuildLinePath(mapped, false);
            var summary = BuildPolarSummary(chart, item.series);
            DrawPremiumSvgLinePath(sb, "polar-line", item.index, mapped.Length, path, color, item.series.StrokeWidth, chart.Options.LineVisualStyle, writer => writer.Attribute("role", "img").Attribute("aria-label", summary));
            for (var pointIndex = 0; pointIndex < mapped.Length; pointIndex++) {
                var point = mapped[pointIndex];
                var raw = item.series.Points[pointIndex];
                var pointColor = PointColor(chart, item.series, item.index, pointIndex);
                AppendSvg(sb, writer => writer
                    .StartElement("circle")
                    .Attribute("data-cfx-role", "polar-point")
                    .Attribute("data-cfx-series", item.index)
                    .Attribute("data-cfx-point", pointIndex)
                    .Attribute("data-cfx-angle", raw.X)
                    .Attribute("data-cfx-radius", raw.Y)
                    .Attribute("cx", point.X)
                    .Attribute("cy", point.Y)
                    .Attribute("r", Math.Max(ChartVisualPrimitives.PolarPointRadius, chart.Options.Theme.MarkerRadius))
                    .Attribute("fill", pointColor.ToCss())
                    .Attribute("stroke", chart.Options.Theme.CardBackground.ToCss())
                    .Attribute("stroke-width", ChartVisualPrimitives.MarkerStrokeWidth)
                    .EndEmptyElement()
                    .Line());
                if (ShouldDrawDataLabels(chart, item.series)) {
                    var labelPoint = PolarDataLabelPoint(geometry, raw, point);
                    DrawDataLabel(sb, chart, FormatDataLabel(chart, item.series, pointIndex, raw.Y), labelPoint.X, labelPoint.Y, plot, "polar-data-label", item.series, pointIndex);
                }
            }
        }

        DrawLegend(sb, chart, chart.Options.Size.Width, chart.Options.Size.Height);
        AppendSvgEnd(sb, "g");
    }

    private static void DrawPolarGrid(StringBuilder sb, Chart chart, ChartRect plot, PolarChartGeometry geometry) {
        var theme = chart.Options.Theme;
        foreach (var tick in geometry.RadiusTicks) {
            if (tick <= geometry.MinimumRadius) continue;
            var radius = geometry.RingRadius(tick);
            if (chart.Options.ShowGrid) {
                AppendSvg(sb, writer => writer
                    .StartElement("circle")
                    .Attribute("data-cfx-role", "polar-ring")
                    .Attribute("data-cfx-value", tick)
                    .Attribute("cx", geometry.CenterX)
                    .Attribute("cy", geometry.CenterY)
                    .Attribute("r", radius)
                    .Attribute("fill", "none")
                    .Attribute("stroke", theme.Grid.ToCss())
                    .Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth)
                    .Attribute("opacity", ChartVisualPrimitives.PolarRingOpacity)
                    .EndEmptyElement()
                    .Line());
            }

            if (chart.Options.ShowAxes && chart.Options.YAxis.Visible && !geometry.IsOuterRadius(tick)) {
                DrawSvgTextLeft(sb, chart, "polar-radius-label", FormatYAxisValue(chart, tick), geometry.CenterX + 7, geometry.CenterY - radius + 14, theme.MutedText, theme.TickLabelFontSize, Math.Max(28, plot.Right - geometry.CenterX - 14), "400");
            }
        }

        foreach (var angle in geometry.AngleTicks) {
            var end = geometry.OnOuterRing(angle);
            if (chart.Options.ShowGrid) {
                AppendSvg(sb, writer => writer
                    .StartElement("line")
                    .Attribute("data-cfx-role", "polar-spoke")
                    .Attribute("data-cfx-angle", angle)
                    .Attribute("x1", geometry.CenterX)
                    .Attribute("y1", geometry.CenterY)
                    .Attribute("x2", end.X)
                    .Attribute("y2", end.Y)
                    .Attribute("stroke", theme.Grid.ToCss())
                    .Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth)
                    .Attribute("opacity", ChartVisualPrimitives.PolarSpokeOpacity)
                    .EndEmptyElement()
                    .Line());
            }

            if (chart.Options.ShowAxes && chart.Options.XAxis.Visible) DrawPolarAngleLabel(sb, chart, plot, geometry, angle);
        }
    }

    private static void DrawPolarAngleLabel(StringBuilder sb, Chart chart, ChartRect plot, PolarChartGeometry geometry, double angle) {
        var theme = chart.Options.Theme;
        var rawLabel = FormatX(chart, angle);
        var maxWidth = Math.Max(44, SvgPolarLabelWidth(chart, angle));
        var fontSize = TextFontSizeForSvgWidth(rawLabel, maxWidth, theme.TickLabelFontSize);
        var label = TrimSvgLabelToWidth(rawLabel, fontSize, maxWidth);
        if (label.Length == 0) return;
        var target = geometry.OnOuterRing(angle, 24);
        var bounds = new ChartRect(24, Math.Min(24, plot.Top), Math.Max(1, chart.Options.Size.Width - 48), Math.Max(1, chart.Options.Size.Height - 48));
        var safeX = EdgeAwareTextX(label, target.X, bounds, fontSize);
        var safeY = Clamp(target.Y, bounds.Top + fontSize, bounds.Bottom - fontSize);
        var anchor = EdgeAwareAnchor(label, safeX, bounds, fontSize);
        if (anchor == "middle") anchor = Math.Cos(angle) > 0.32 ? "start" : Math.Cos(angle) < -0.32 ? "end" : "middle";
        AppendSvg(sb, writer => writer
            .StartElement("text")
            .Attribute("data-cfx-role", "polar-angle-label")
            .Attribute("data-cfx-angle", angle)
            .Attribute("x", safeX)
            .Attribute("y", safeY)
            .Attribute("text-anchor", anchor)
            .Attribute("dominant-baseline", "middle")
            .Attribute("fill", theme.MutedText.ToCss())
            .Attribute("font-family", SvgFontFamilyAttributeValue(theme.FontFamily))
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", "650")
            .Text(label)
            .EndElement()
            .Line());
    }

    private static double SvgPolarLabelWidth(Chart chart, double angle) {
        var sideRoom = chart.Options.Size.Width * 0.18;
        return Math.Abs(Math.Cos(angle)) < 0.32 ? chart.Options.Size.Width * 0.26 : sideRoom;
    }

    private static ChartPoint PolarDataLabelPoint(PolarChartGeometry geometry, ChartPoint raw, ChartPoint mapped) {
        var x = Math.Cos(raw.X);
        var y = -Math.Sin(raw.X);
        return raw.Y <= geometry.MinimumRadius ? new ChartPoint(mapped.X, mapped.Y - 16) : new ChartPoint(mapped.X + x * 16, mapped.Y + y * 16);
    }

    private static string BuildPolarSummary(Chart chart, ChartSeries series) {
        var text = new StringBuilder(series.Name).Append(": ");
        for (var i = 0; i < series.Points.Count; i++) {
            if (i > 0) text.Append(", ");
            text.Append(FormatX(chart, series.Points[i].X)).Append(" rad / ").Append(FormatValue(chart, series.Points[i].Y));
        }

        return text.ToString();
    }
}
