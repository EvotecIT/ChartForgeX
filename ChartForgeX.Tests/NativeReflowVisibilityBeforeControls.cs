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
public sealed class NativeReflowVisibilityBeforeControls {
    [Theory]
    [InlineData("line", false)] [InlineData("line", true)] [InlineData("bar", false)] [InlineData("bar", true)]
    [InlineData("boxes", false)] [InlineData("boxes", true)] [InlineData("raw", false)] [InlineData("raw", true)]
    public void OrdinaryNativeSourcePreparationRemainsRepeatable(string kind, bool dark) {
        var chart = Model(kind).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var renderer = new HtmlInteractiveChartRenderer(); var first = renderer.PrepareSection(chart); var second = renderer.PrepareSection(chart);
        Capture(first, "reflow-before-ordinary-" + kind + "-" + dark);
        Assert.Equal(first.Html, second.Html); Assert.Equal(first.PreparedVisual.ToSvg(), second.PreparedVisual.ToSvg()); Assert.Equal(first.PreparedVisual.ToPng(), second.PreparedVisual.ToPng());
    }
    [Theory] [InlineData("raw")] [InlineData("line")]
    public void FullAuthoredFactsCanDifferWhileCompletedNativePaintIsUnchanged(string kind) {
        var first = Model(kind, changedUnpainted: false); var last = Model(kind, changedUnpainted: true);
        var renderer = new HtmlInteractiveChartRenderer(); var a = renderer.PrepareSection(first); var b = renderer.PrepareSection(last);
        Capture(a, "reflow-before-unpainted-" + kind + "-a"); Capture(b, "reflow-before-unpainted-" + kind + "-b");
        var facts = new { kind, beforeSamples = first.Series[0].BoxPlotSourceSamples, afterSamples = last.Series[0].BoxPlotSourceSamples,
            beforeColors = first.Series[0].PointColors.Select(color => color?.ToHexRgba()), afterColors = last.Series[0].PointColors.Select(color => color?.ToHexRgba()) };
        Save("reflow-before-unpainted-" + kind + ".facts.json", JsonSerializer.Serialize(facts));
        Assert.Equal(a.PreparedVisual.ToSvg("reflow-paint-control"), b.PreparedVisual.ToSvg("reflow-paint-control")); Assert.Equal(a.PreparedVisual.ToPng(), b.PreparedVisual.ToPng());
        if (kind == "raw") Assert.NotEqual(first.Series[0].BoxPlotSourceSamples[3], last.Series[0].BoxPlotSourceSamples[3]);
        else Assert.NotEqual(first.Series[0].PointColors[1], last.Series[0].PointColors[1]);
    }
    internal static Chart Model(string kind, bool changedUnpainted = false) {
        var id = new ChartSeriesId("delivery"); ChartSeries series;
        if (kind == "raw") {
            var samples = Enumerable.Range(0, 9).Select(index => index == 3 && changedUnpainted ? 3.25 : (double)index).ToArray();
            series = ChartSeries.CreateBoxPlot(id, "Delivery", new[] { new ChartBoxPlotSampleGroup(1, samples), new ChartBoxPlotSampleGroup(2, new double[] { 2, 3, 4, 5, 6 }), new ChartBoxPlotSampleGroup(3, new double[] { 4, 5, 6, 7, 8 }) });
            series.WithBoxPlotGroupIds(new[] { new ChartObservationId("earlier"), new ChartObservationId("current"), new ChartObservationId("later") });
            series.WithObservationIds(Enumerable.Range(0, series.BoxPlotSourceSamples.Count).Select(index => new ChartObservationId("sample-" + index)));
        } else if (kind == "boxes") series = ChartSeries.CreateBoxPlot(id, "Delivery", new[] { new ChartBoxPlot(1, 0, 2, 4, 6, 8), new ChartBoxPlot(2, 2, 3, 4, 5, 6), new ChartBoxPlot(3, 4, 5, 6, 7, 8) });
        else {
            var points = new[] { new ChartPoint(1, 20), new ChartPoint(2, 80), new ChartPoint(3, 40) };
            series = kind == "bar" ? ChartSeries.CreateBar(id, "Delivery", points) : ChartSeries.CreateLine(id, "Delivery", points);
            series.WithPointColor(1, changedUnpainted ? ChartColor.Black : ChartColor.White);
        }
        if (kind != "raw") series.WithObservationIds(new[] { new ChartObservationId("earlier"), new ChartObservationId("current"), new ChartObservationId("later") });
        return Chart.Create().WithSize(760, 420).WithLegend(false).WithDataLabels(false).WithLineMarkers(ChartLineMarkerMode.None).AddSeries(series);
    }
    internal static void Capture(HtmlChartSection section, string name) {
        Save(name + ".html", section.Html); Save(name + ".svg", section.PreparedVisual.ToSvg());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".native.png"), section.PreparedVisual.ToPng()); }
    }
    internal static void Save(string name, string value) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name), value);
    }
}
