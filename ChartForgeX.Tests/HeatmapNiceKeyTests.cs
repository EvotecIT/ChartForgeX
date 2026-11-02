using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HeatmapNiceKeyTests {
    [Theory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(800, VisualThemeMode.Light)]
    [InlineData(800, VisualThemeMode.Dark)]
    public void DefaultKeyUsesNamedRoundedStepsWithTrueEndpointsAndMeasuredGaps(int width, VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(width, 440).WithThemeMode(mode).WithDataLabels(false)
            .AddHeatmapRow("Service score", new[] { 41d, 55, 70, 92 });
        RoundedRadialSeriesTests.UseFixtureFont(chart);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "heatmap-scale-title"
            && node.Text.Lines.Single().Text == "Service score");
        var steps = prepared.Regions.Where(region => region.Role == "heatmap-scale-step").ToArray();
        Assert.Equal(new[] { "41", "50", "60", "70", "80", "90", "92" }, steps.Select(region => region.Label));
        Assert.True(steps.Last().Bounds.Right - steps.First().Bounds.Left >= width * .65, "The key shares the plot width.");
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "heatmap-scale-label")
            .OrderBy(node => node.LineLeft(node.Text.Lines[0])).ToArray();
        Assert.Equal("41", labels.First().Text.Lines.Single().Text);
        Assert.Equal("92", labels.Last().Text.Lines.Single().Text);
        Assert.Contains(labels, node => node.Text.Lines.Single().Text == "70");
        var seventy = labels.Single(node => node.Text.Lines.Single().Text == "70");
        Assert.Equal(steps.First().Bounds.Left + (steps.Last().Bounds.Right + 2 - steps.First().Bounds.Left) * (70d - 41) / (92 - 41),
            seventy.LineLeft(seventy.Text.Lines[0]) + seventy.Text.Metrics.Width / 2, 5);
        for (var index = 1; index < labels.Length; index++) Assert.True(
            labels[index].LineLeft(labels[index].Text.Lines[0]) - labels[index - 1].LineLeft(labels[index - 1].Text.Lines[0])
                - labels[index - 1].Text.Metrics.Width >= 3.9, "Key captions need a visible gap.");
        Assert.NotEmpty(prepared.ToPng());
        chart.WithValueFormat(ChartValueFormat.Number("0.0 '°C'", CultureInfo.GetCultureInfo("de-DE")));
        var authored = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Contains(authored.Regions, region => region.Role == "heatmap-scale-step" && region.Label == "50,0 °C");
        chart.WithHeatmapColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#DAE8F8"), ChartColor.FromHex("#1C5CAB")));
        var configured = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes.OfType<VisualSceneText>().ToArray();
        Assert.Contains(configured, node => node.Role == "heatmap-scale-title" && node.Text.Lines.Single().Text == "Service score");
        Assert.Contains(configured, node => node.Role == "heatmap-scale-label" && node.Text.Lines.Single().Text == "70,0 °C");

        chart.WithValueFormat(ChartValueFormat.Number("0 '°C'", CultureInfo.InvariantCulture))
            .WithHeatmapColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#DAE8F8"), ChartColor.FromHex("#1C5CAB")).WithValueRange(-30, 55));
        var temperature = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var bands = temperature.Regions.Where(region => region.Role == "heatmap-scale-step").ToArray();
        Assert.Equal(new[] { "-30 °C–-20 °C", "-20 °C–0 °C", "0 °C–20 °C", "20 °C–40 °C", "40 °C–55 °C" }, bands.Select(region => region.Label));
        var tickLabels = temperature.Scene.Nodes.OfType<VisualSceneText>()
            .Where(node => node.Id?.StartsWith("heatmap-scale-label-step-", StringComparison.Ordinal) == true).ToArray();
        Assert.NotEmpty(tickLabels);
        foreach (var label in tickLabels) {
            var center = label.LineLeft(label.Text.Lines[0]) + label.Text.Metrics.Width / 2;
            Assert.Contains(bands.Skip(1), band => Math.Abs(band.Bounds.Left - center) < .001);
        }
        Assert.NotEmpty(temperature.ToPng());
    }
}
