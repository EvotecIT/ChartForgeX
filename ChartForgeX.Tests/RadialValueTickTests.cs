using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RadialValueTickTests {
    [Theory]
    [InlineData(ChartSeriesKind.Radar, "radar-ring-label")]
    [InlineData(ChartSeriesKind.Polar, "polar-radius-label")]
    [InlineData(ChartSeriesKind.RadialBar, "radial-value-label")]
    [InlineData(ChartSeriesKind.RadialColumn, "radial-value-label")]
    public void PartialValueLabelsRetainGeneratedTicksAndFormatting(ChartSeriesKind kind, string role) {
        var chart = Chart.Create().WithSize(700, 480).WithLegend(false).WithDataLabels(false)
            .WithXLabels("Coverage", "Policy", "Alerts", "Response");
        var values = new[] { new ChartPoint(1, 92), new ChartPoint(2, 74), new ChartPoint(3, 88), new ChartPoint(4, 81) };
        switch (kind) {
            case ChartSeriesKind.Radar: chart.AddRadar("Current", values); break;
            case ChartSeriesKind.Polar: chart.AddPolar("Current", values); break;
            case ChartSeriesKind.RadialBar: chart.AddRadialBar("Current", values); break;
            case ChartSeriesKind.RadialColumn: chart.AddRadialColumn("Current", values); break;
        }
        chart.Options.YAxis.WithBounds(0, 100); chart.Options.YAxis.TickCount = 6;
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(20, "explicit-ring"));
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(35, "extra-ring"));
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(120, "outside-ring"));
        chart.Options.YAxis.LabelFormatter = value => "ring-" + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = prepared.Regions.Where(region => region.Role == role).Select(region => region.Label).ToArray();
        Assert.Contains("explicit-ring", labels); Assert.Contains("extra-ring", labels);
        Assert.Contains("ring-40", labels); Assert.Contains("ring-60", labels); Assert.Contains("ring-80", labels);
        Assert.DoesNotContain("ring-20", labels); Assert.DoesNotContain("outside-ring", labels);
        Assert.Equal(labels.Length, labels.Distinct().Count());
        Assert.True(prepared.ToPng().Length > 64);
    }
}
