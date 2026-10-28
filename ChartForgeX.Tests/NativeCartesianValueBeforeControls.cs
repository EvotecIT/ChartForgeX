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
public sealed class NativeCartesianValueBeforeControls {
    [Theory]
    [InlineData(false, "linear")] [InlineData(true, "linear")]
    [InlineData(false, "log-positive")] [InlineData(true, "log-positive")]
    [InlineData(false, "log-negative")] [InlineData(true, "log-negative")]
    [InlineData(false, "elapsed-time") ] [InlineData(true, "elapsed-time")]
    public void ExistingOneEditApiCapturesRealLineAndBarValuesOnFixedNativeAxes(bool bar, string scale) {
        var renderer = new HtmlInteractiveChartRenderer(); var first = scale == "log-negative" ? -20d : 20d; var last = scale == "log-negative" ? -80d : 80d;
        var from = renderer.PrepareSection(Model(bar, scale, first)); var to = renderer.PrepareSection(Model(bar, scale, last));
        var expected = renderer.PrepareSection(Model(bar, scale, (first + last) / 2));
        var name = "cartesian-value-before-" + (bar ? "bar-" : "line-") + scale;
        Capture(from, name + "-from"); Capture(to, name + "-to"); Capture(expected, name + "-independent-middle");
        string? payload = null; Exception? failure = null;
        try { payload = renderer.PrepareValueTransitionJson(from, to, 3); } catch (Exception exception) { failure = exception; }
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            File.WriteAllText(Path.Combine(output, name + ".capture.json"), JsonSerializer.Serialize(new { payload, errorType = failure?.GetType().FullName, error = failure?.Message }));
        }
        Assert.Null(failure); Assert.NotNull(payload); using var json = JsonDocument.Parse(payload);
        Assert.Equal("cartesian-value", json.RootElement.GetProperty("kind").GetString());
        Assert.Equal("delivery", json.RootElement.GetProperty("seriesKey").GetString()); Assert.Equal("current", json.RootElement.GetProperty("observationId").GetString());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void OrdinaryNativeDefaultOutputsKeepExistingRepeatabilityWithoutAValueCapture(bool bar) {
        var renderer = new HtmlInteractiveChartRenderer(); var a = renderer.PrepareSection(Model(bar, "linear", 20)); var b = renderer.PrepareSection(Model(bar, "linear", 20));
        Capture(a, "cartesian-value-ordinary-" + bar);
        Assert.Equal(a.Html, b.Html); Assert.Equal(a.PreparedVisual.ToSvg(), b.PreparedVisual.ToSvg()); Assert.Equal(a.PreparedVisual.ToPng(), b.PreparedVisual.ToPng());
    }
    [Theory]
    [InlineData(ChartSeriesKind.Scatter)] [InlineData(ChartSeriesKind.Bubble)] [InlineData(ChartSeriesKind.Radar)]
    [InlineData(ChartSeriesKind.Polar)] [InlineData(ChartSeriesKind.ErrorBar)] [InlineData(ChartSeriesKind.Lollipop)]
    public void ExistingSharedMarkerFamiliesKeepOrdinaryNativePaintBytes(ChartSeriesKind kind) {
        var renderer = new HtmlInteractiveChartRenderer();
        var first = renderer.PrepareSection(Assert.IsType<Chart>(ExampleRecipes.ForKind(kind).Create()));
        var second = renderer.PrepareSection(Assert.IsType<Chart>(ExampleRecipes.ForKind(kind).Create()));
        Capture(first, "cartesian-marker-ordinary-" + kind);
        Assert.Equal(first.Html, second.Html); Assert.Equal(first.PreparedVisual.ToSvg(), second.PreparedVisual.ToSvg()); Assert.Equal(first.PreparedVisual.ToPng(), second.PreparedVisual.ToPng());
    }
    internal static Chart Model(bool bar, string scale, double value) {
        var first = scale == "elapsed-time" ? new DateTime(2026, 1, 1).ToOADate() : 0;
        var points = new[] { new ChartPoint(first, scale == "log-negative" ? -30 : 30), new ChartPoint(first + 1, value), new ChartPoint(first + 2, scale == "log-negative" ? -60 : 60) };
        var series = bar ? ChartSeries.CreateBar(new ChartSeriesId("delivery"), "Delivery", points) : ChartSeries.CreateLine(new ChartSeriesId("delivery"), "Delivery", points);
        series.WithObservationId(0, new ChartObservationId("earlier")).WithObservationId(1, new ChartObservationId("current")).WithObservationId(2, new ChartObservationId("later"));
        var chart = Chart.Create().WithSize(760, 400).WithTheme(VisualTheme.Graphite()).WithLegend(false).WithDataLabels(false).WithLineMarkers(ChartLineMarkerMode.None).AddSeries(series);
        chart.ConfigureXAxis(axis => { axis.Minimum = first - .5; axis.Maximum = first + 2.5; if (scale == "elapsed-time") axis.Scale = ChartScaleKind.Time; });
        chart.ConfigureYAxis(axis => {
            if (scale.StartsWith("log-", StringComparison.Ordinal)) { axis.LogarithmicDomain = scale == "log-negative" ? ChartLogarithmicDomain.Negative : ChartLogarithmicDomain.Positive; axis.Scale = ChartScaleKind.Logarithmic; }
            axis.Minimum = scale == "log-negative" ? -100 : scale == "log-positive" ? 1 : 0; axis.Maximum = scale == "log-negative" ? -1 : 100;
        }); return chart;
    }
    private static void Capture(HtmlChartSection section, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, name); File.WriteAllText(path + ".html", section.Html);
        File.WriteAllText(path + ".svg", section.PreparedVisual.ToSvg()); File.WriteAllBytes(path + ".native.png", section.PreparedVisual.ToPng());
    }
}
