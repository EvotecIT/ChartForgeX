using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawPolar(RgbaCanvas canvas, Chart chart, ChartRect plot) {
        var seriesItems = chart.Series
            .Select((series, index) => new PolarSeriesItem(series, index))
            .Where(item => item.Series.Kind == ChartSeriesKind.Polar && item.Series.Points.Count > 0)
            .ToArray();
        if (seriesItems.Length == 0) return;

        var geometry = PolarChartGeometry.Create(chart, plot);
        DrawPolarGrid(canvas, chart, plot, geometry);
        foreach (var item in seriesItems) {
            var mapped = item.Series.Points.Select(geometry.Map).ToArray();
            var color = item.Series.Color ?? chart.Options.Theme.Palette[item.Index % chart.Options.Theme.Palette.Length];
            DrawPremiumPngLinePath(canvas, mapped, color, item.Series.StrokeWidth, chart.Options.LineVisualStyle);
            for (var pointIndex = 0; pointIndex < mapped.Length; pointIndex++) {
                var point = mapped[pointIndex];
                var raw = item.Series.Points[pointIndex];
                DrawMarker(canvas, chart, point.X, point.Y, Math.Max(ChartVisualPrimitives.PolarPointRadius, chart.Options.Theme.MarkerRadius), PointColor(chart, item.Series, item.Index, pointIndex));
                if (!ShouldDrawDataLabels(chart, item.Series)) continue;
                var labelPoint = PolarDataLabelPoint(geometry, raw, point);
                var label = FormatDataLabel(chart, item.Series, pointIndex, raw.Y);
                var fontSize = PngDataLabelFontSize(chart, item.Series, pointIndex);
                DrawReadablePngLabel(canvas, plot, labelPoint.X - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0, labelPoint.Y - fontSize / 2.0, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, DataLabelStyle(chart, item.Series, pointIndex));
            }
        }
    }

    private static void DrawPolarGrid(RgbaCanvas canvas, Chart chart, ChartRect plot, PolarChartGeometry geometry) {
        var theme = chart.Options.Theme;
        var tickFontSize = PngTickFontSize(chart);
        foreach (var tick in geometry.RadiusTicks) {
            if (tick <= geometry.MinimumRadius) continue;
            var radius = geometry.RingRadius(tick);
            if (chart.Options.ShowGrid) canvas.DrawCircleOutline(geometry.CenterX, geometry.CenterY, radius, ApplyOpacity(theme.Grid, ChartVisualPrimitives.PolarRingOpacity), ChartVisualPrimitives.GridStrokeWidth);
            if (chart.Options.ShowAxes && chart.Options.YAxis.Visible && !geometry.IsOuterRadius(tick)) {
                var maxWidth = Math.Max(28, chart.Options.Size.Width - chart.Options.Padding.Right - geometry.CenterX - 14);
                var label = FormatYAxisValue(chart, tick);
                var fontSize = TextFontSizeForWidth(label, maxWidth, tickFontSize);
                label = TrimPngLabelToWidth(label, fontSize, maxWidth);
                if (label.Length > 0) canvas.DrawText(geometry.CenterX + 7, geometry.CenterY - radius + 14 - fontSize + 1, label, theme.MutedText, fontSize);
            }
        }

        foreach (var angle in geometry.AngleTicks) {
            var end = geometry.OnOuterRing(angle);
            if (chart.Options.ShowGrid) canvas.DrawLine(geometry.CenterX, geometry.CenterY, end.X, end.Y, ApplyOpacity(theme.Grid, ChartVisualPrimitives.PolarSpokeOpacity), ChartVisualPrimitives.GridStrokeWidth);
            if (!chart.Options.ShowAxes || !chart.Options.XAxis.Visible) continue;
            var rawLabel = FormatX(chart, angle);
            var maxWidth = Math.Max(44, PngPolarLabelWidth(chart, angle));
            var fontSize = TextFontSizeForEmphasizedWidth(rawLabel, maxWidth, tickFontSize);
            var label = TrimReadablePngLabelToWidth(rawLabel, fontSize, maxWidth);
            if (label.Length == 0) continue;
            var target = geometry.OnOuterRing(angle, 24);
            var labelWidth = EstimatePngEmphasizedTextWidth(label, fontSize);
            var labelX = Clamp(target.X - labelWidth / 2.0, chart.Options.Padding.Left + 2, chart.Options.Size.Width - chart.Options.Padding.Right - labelWidth - 2);
            var labelY = Clamp(target.Y - fontSize / 2.0, chart.Options.Padding.Top + 12, chart.Options.Size.Height - chart.Options.Padding.Bottom - 18);
            canvas.DrawTextEmphasized(labelX, labelY, label, theme.MutedText, fontSize);
        }
    }

    private static double PngPolarLabelWidth(Chart chart, double angle) {
        var sideRoom = chart.Options.Size.Width * 0.18;
        return Math.Abs(Math.Cos(angle)) < 0.32 ? chart.Options.Size.Width * 0.26 : sideRoom;
    }

    private static ChartPoint PolarDataLabelPoint(PolarChartGeometry geometry, ChartPoint raw, ChartPoint mapped) {
        var x = Math.Cos(raw.X);
        var y = -Math.Sin(raw.X);
        return raw.Y <= geometry.MinimumRadius ? new ChartPoint(mapped.X, mapped.Y - 16) : new ChartPoint(mapped.X + x * 16, mapped.Y + y * 16);
    }

    private static bool IsPolarChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Polar);

    private readonly struct PolarSeriesItem {
        public PolarSeriesItem(ChartSeries series, int index) {
            Series = series;
            Index = index;
        }

        public ChartSeries Series { get; }

        public int Index { get; }
    }
}
