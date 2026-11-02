using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GaugeHorizontalGeneratedScaleTests {
    [Theory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(760, VisualThemeMode.Light)]
    [InlineData(760, VisualThemeMode.Dark)]
    public void GeneratedHorizontalScaleKeepsCompleteEndpointsWithoutIntersectingTheirNeighbours(int width, VisualThemeMode mode) {
        const string family = "CFX Horizontal Gauge Numeric Carlito";
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        FontRegistry.Register(family, Path.Combine(fonts, "Carlito-Regular.ttf"), 400);
        FontRegistry.Register(family, Path.Combine(fonts, "Carlito-Bold.ttf"), 700);
        var targets = new[] { new ChartGaugeTarget(1800, "Baseline"), new ChartGaugeTarget(9500, "Expected"),
            new ChartGaugeTarget(10500, "Goal"), new ChartGaugeTarget(19000, "Limit") };
        var chart = Chart.Create().AddSeries(ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Capacity",
                new ChartGaugeValue(12500.125, 0, 20000, targets)))
            .WithValueFormat(ChartValueFormat.Number("0.000", CultureInfo.InvariantCulture))
            .ConfigureGauge(options => { options.Form = ChartGaugeForm.Linear; options.TickCount = 11; options.Caption = "Processed requests"; });
        var height = width == 390 ? 300 : 440;
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height), 14),
            VisualTheme.Graphite(), mode, new VisualFrame("Measured capacity", "Native generated scale", showLegend: false), FontSpec.FromFamily(family)));
        ScatterBubbleColorRenderingTests.Capture(prepared, $"linear-gauge-complete-{width}-{mode}");
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role is "gauge-min-label" or "gauge-max-label"
            || text.Role?.StartsWith("gauge-tick-label-", StringComparison.Ordinal) == true).OrderBy(text => text.X).ToArray();
        Assert.Contains(labels, text => Caption(text) == "0.000");
        Assert.Contains(labels, text => Caption(text) == "20000.000");
        foreach (var label in labels) {
            var value = double.Parse(Caption(label), CultureInfo.InvariantCulture);
            Assert.Equal(value.ToString("0.000", CultureInfo.InvariantCulture), Caption(label));
            Assert.True(label.Text.Size >= 12);
        }
        for (var index = 1; index < labels.Length; index++)
            Assert.True(labels[index - 1].X + labels[index - 1].Text.Metrics.Width / 2 <= labels[index].X - labels[index].Text.Metrics.Width / 2);
        var ticks = prepared.Scene.Nodes.Count(node => node.Role == "gauge-tick");
        Assert.Equal(width == 390 ? 6 : 11, ticks);
        Assert.Equal(ticks, prepared.Regions.Count(region => region.Role is "gauge-min-label" or "gauge-max-label"
            || region.Role.StartsWith("gauge-tick-label-", StringComparison.Ordinal)));
        var source = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "gauge");
        Assert.Equal("12500.125", source.Metadata["data-cfx-value"]);
        Assert.Equal("0", source.Metadata["data-cfx-min"]); Assert.Equal("20000", source.Metadata["data-cfx-max"]);
        Assert.Equal(4, prepared.Scene.Nodes.Count(node => node.Role == "gauge-target"));
        for (var index = 0; index < targets.Length; index++)
            Assert.Equal(targets[index].Value.ToString("R", CultureInfo.InvariantCulture), source.Metadata[$"data-cfx-gauge-target-{index}-value"]);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "gauge.label-overflow");
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    private static string Caption(VisualSceneText text) => string.Join(" ", text.Text.Lines.Select(line => line.Text));
}
