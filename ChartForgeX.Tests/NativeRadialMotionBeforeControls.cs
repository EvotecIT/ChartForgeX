using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
[Trait("BrowserSuite", "Compatibility")]
public sealed class NativeRadialMotionBeforeControls {
    [Theory]
    [InlineData(false, "hidden")] [InlineData(true, "hidden")] [InlineData(false, "inside")] [InlineData(true, "inside")] [InlineData(false, "outside")] [InlineData(true, "outside")]
    public void ExistingOneEditApiCapturesRealIdentifiedPieAndDonutValues(bool donut, string labels) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(Model(donut, false, labels, 60)); var to = renderer.PrepareSection(Model(donut, false, labels, 80));
        var middle = renderer.PrepareSection(Model(donut, false, labels, 70)); var name = "radial-motion-before-value-" + donut + "-" + labels;
        Capture(from, name + "-from"); Capture(to, name + "-to"); Capture(middle, name + "-native-middle");
        string? payload = null; Exception? error = null; try { payload = renderer.PrepareValueTransitionJson(from, to, 3); } catch (Exception exception) { error = exception; }
        Save(name + ".capture.json", JsonSerializer.Serialize(new { payload, errorType = error?.GetType().FullName, message = error?.Message }));
        Assert.Null(error); Assert.NotNull(payload); using var json = JsonDocument.Parse(payload); Assert.Equal("radial-value", json.RootElement.GetProperty("kind").GetString());
        Assert.Equal("parts", json.RootElement.GetProperty("seriesKey").GetString()); Assert.Equal("primary", json.RootElement.GetProperty("observationId").GetString());
    }
    [Theory]
    [InlineData(false, "default")] [InlineData(true, "default")] [InlineData(false, "other")] [InlineData(true, "other")] [InlineData(false, "zero")] [InlineData(true, "zero")] [InlineData(false, "multi")] [InlineData(true, "multi")]
    public void OrdinaryRadialDefaultsAggregationAndNoDataKeepExactNativeControls(bool donut, string state) {
        var chart = Model(donut, state == "other", "inside"); if (state == "zero") for (var index = 0; index < chart.Series[0].Points.Count; index++) chart.Series[0].Points[index] = new ChartPoint(index + 1, 0);
        if (state == "multi") {
            var comparisonSeries = new ChartSeries(new ChartSeriesId("comparison"), "Comparison", donut ? ChartSeriesKind.Donut : ChartSeriesKind.Pie, chart.Series[0].Points.ToArray()).WithDataLabels(false);
            comparisonSeries.WithObservationIds(new[] { new ChartObservationId("primary"), new ChartObservationId("part-1"), new ChartObservationId("part-2") });
            chart.WithAutomaticRadialBands(new ChartRadialBandAllocation(.15, .04)).AddSeries(comparisonSeries);
        }
        var renderer = new HtmlInteractiveChartRenderer(); var first = renderer.PrepareSection(chart); var second = renderer.PrepareSection(chart);
        Capture(first, "radial-motion-before-ordinary-" + donut + "-" + state);
        Save("radial-motion-before-ordinary-" + donut + "-" + state + ".source.json", JsonSerializer.Serialize(new { points = chart.Series[0].Points, ids = chart.Series[0].ObservationIds.Select(pair => new { pair.Key, id = pair.Value.Value }), chart.Options.MaximumPieSlices }));
        Assert.Equal(first.Html, second.Html); Assert.Equal(first.PreparedVisual.ToSvg(), second.PreparedVisual.ToSvg()); Assert.Equal(first.PreparedVisual.ToPng(), second.PreparedVisual.ToPng());
        if (state == "other") { var other = first.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().First(row => row.Role == "radial-point" && row.Metadata["data-cfx-point"] == "-1");
            Assert.Equal("2,3,4", other.Metadata["data-cfx-source-points"]); Assert.Equal("15", other.Metadata["data-cfx-value"]); }
        if (state == "zero") Assert.Contains(first.PreparedVisual.Diagnostics, diagnostic => diagnostic.Code == "radial.no-data");
    }
    internal static Chart Model(bool donut, bool other, string labels = "inside", double value = 60, int width = 760, bool dark = false) {
        var points = other ? new[] { new ChartPoint(1, value), new ChartPoint(2, 25), new ChartPoint(3, 10), new ChartPoint(4, 4), new ChartPoint(5, 1) }
            : new[] { new ChartPoint(1, value), new ChartPoint(2, 25), new ChartPoint(3, 10) };
        var series = new ChartSeries(new ChartSeriesId("parts"), "Parts", donut ? ChartSeriesKind.Donut : ChartSeriesKind.Pie, points);
        series.WithObservationIds(Enumerable.Range(0, points.Length).Select(index => new ChartObservationId(index == 0 ? "primary" : "part-" + index)));
        var chart = Chart.Create().WithSize(width, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithLegend(true).WithDataLabels(labels != "hidden")
            .WithDataLabelPlacement(labels == "outside" ? ChartDataLabelPlacement.Outside : ChartDataLabelPlacement.Inside).AddSeries(series);
        chart.Options.MaximumPieSlices = other ? 3 : 8; chart.WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent);
        chart.ConfigureXAxis(axis => { for (var index = 0; index < points.Length; index++) axis.Labels.Add(new ChartAxisLabel(index + 1, "Part " + (index + 1))); });
        return chart;
    }
    internal static void Capture(HtmlChartSection section, string name) {
        Save(name + ".html", section.Html); Save(name + ".svg", section.PreparedVisual.ToSvg());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".native.png"), section.PreparedVisual.ToPng()); }
    }
    internal static void Save(string name, string text) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name), text);
    }
}
