using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

public static partial class V2GalleryModels {
    private static Chart? Radial(ChartSeriesKind kind, string variant) {
        var chart = Categories(kind); var values = Observations(variant);
        switch (kind) {
            case ChartSeriesKind.RadialBar:
            case ChartSeriesKind.RadialColumn: return NumericRadial(kind, variant);
            case ChartSeriesKind.Pie: chart.AddPie("Revenue", values); break;
            case ChartSeriesKind.Donut: chart.AddDonut("Revenue", values); break;
            case ChartSeriesKind.Gauge:
                chart.AddGauge("Capacity", 76).ConfigureGauge(options => {
                    options.Form = variant == "options" ? ChartGaugeForm.Needle : ChartGaugeForm.Arc;
                    options.Target = 85; options.Caption = "Available capacity";
                    options.Bands.Add(new ChartGaugeBand(0, 50, ChartSeriesState.Danger));
                    options.Bands.Add(new ChartGaugeBand(50, 80, ChartSeriesState.Warning));
                    options.Bands.Add(new ChartGaugeBand(80, 100, ChartSeriesState.Success));
                }); break;
            case ChartSeriesKind.Circle: chart.AddCircle("Completed", 76); break;
            case ChartSeriesKind.ProgressRing: chart.AddProgressRing("Completion", values).WithProgressRingCenterLabel(); break;
            case ChartSeriesKind.LayeredRadial:
                chart.AddLayeredRadial("Completion", new[] {
                    new ChartRadialLayer("Reviewed", 76) { RadiusRatio = 1, StrokeRatio = .13, SweepAngleDegrees = 300, StartAngleDegrees = -60 },
                    new ChartRadialLayer("Verified", 58) { RadiusRatio = .7, StrokeRatio = .13, SweepAngleDegrees = 240, StartAngleDegrees = -30, SeparatorCount = 8 },
                    new ChartRadialLayer("Complete", 42) { RadiusRatio = .4, StrokeRatio = .13, SweepAngleDegrees = 360 }
                }); break;
            case ChartSeriesKind.Bullet:
                chart.AddBullet("Reviewed", 76, 85, rangeEnds: new[] { 50d, 80d }).AddBullet("Verified", 58, 75, rangeEnds: new[] { 40d, 70d }); break;
            case ChartSeriesKind.Radar: chart.AddRadar("Observed", values).AddRadar("Expected", Observations(variant, 12)); break;
            case ChartSeriesKind.Polar: chart = Chart.Create().AddPolar("Observed", Enumerable.Range(0, 6).Select(index => new ChartPoint(index * Math.PI / 3, 25 + index * 7)));
                chart.ConfigureXAxis(axis => axis.LabelFormatter = angle => (angle * 180 / Math.PI).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "°"); break;
            case ChartSeriesKind.PolarArea: chart.AddPolarArea("Revenue", values); break;
            default: return null;
        }
        if (kind is ChartSeriesKind.Pie or ChartSeriesKind.Donut && variant == "options") {
            chart.WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.Label);
            chart.Series[0].WithPointSliceOffset(0, .12).WithPointFillPattern(1, ChartFillPattern.Crosshatch);
            if (kind == ChartSeriesKind.Donut) chart.WithDonutCenterText("184", "Reviewed");
        }
        if (variant == "options" && kind == ChartSeriesKind.ProgressRing) chart.WithRadialProgressRadiusScale(.85).WithRadialProgressStrokeScale(1.2);
        if (variant == "options" && kind is ChartSeriesKind.Radar or ChartSeriesKind.Polar) {
            chart.Options.YAxis.WithBounds(0, 100); chart.Options.YAxis.TickCount = 6;
            chart.Options.YAxis.Labels.Add(new ChartAxisLabel(20, "20 · Target"));
        }
        return chart;
    }
}
