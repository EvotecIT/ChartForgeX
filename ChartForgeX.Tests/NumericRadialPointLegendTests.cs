using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NumericRadialPointLegendTests {
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task CategoryAxisPointLegendAndNativeFactsShareOneFormatterResult(bool bars, bool authored) {
        var calls = new Dictionary<double, int>();
        var chart = Create(bars, calls, authored);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var legends = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "legend-label")
            .Select(node => string.Join("\n", node.Text.Lines.Select(line => line.Text))).ToArray();
        var categories = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").Select(group => group.Metadata["data-cfx-category"]).ToArray();
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var name = $"category-legend-{(bars ? "bar" : "column")}-{(authored ? "authored" : "formatter")}";
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, name + ".native.png"), prepared.ToPng());
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new { legends, categories, calls }, new JsonSerializerOptions { WriteIndented = true }));
        }
        Assert.Equal(categories, legends);
        Assert.Equal(authored ? 2 : 3, calls.Count);
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        if (authored) Assert.Equal("Authored", legends[0]);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Options.XAxis.WithLabelFormatter(_ => throw new InvalidOperationException("Prepared output must not format again."));
        chart.Options.XAxis.Labels.Clear(); chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RepeatedCategoriesDoNotReinvokeAStatefulFormatter(bool bars) {
        var calls = new Dictionary<double, int>(); var chart = Create(bars, calls, false);
        chart.Series[0].Points[2] = new ChartPoint(2, 70);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var categories = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").Select(group => group.Metadata["data-cfx-category"]).ToArray();
        Assert.Equal(categories[1], categories[2]);
        Assert.Equal(2, calls.Count); Assert.All(calls.Values, count => Assert.Equal(1, count));
    }

    internal static Chart Create(bool bars, Dictionary<double, int> calls, bool authored) {
        var chart = Chart.Create().WithSize(760, 480).WithHeader(false).WithLegend().WithPointLegend().WithDataLabels(false)
            .WithGrid(false).WithYAxisBounds(0, 100);
        NumericRadialSeriesTests.Add(chart, bars, "Observations", new[] { new ChartPoint(1, 30), new ChartPoint(2, 50), new ChartPoint(3, 70) });
        chart.Options.YAxis.Visible = false;
        chart.Options.XAxis.WithLabelFormatter(value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return $"Group {value} pass {calls[value]}";
        });
        if (authored) chart.Options.XAxis.Labels.Add(new ChartAxisLabel(1, "Authored"));
        return chart;
    }
}
