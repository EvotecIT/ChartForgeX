using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRangeComponentBeforeControls {
    [Theory] [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void ExistingNativeEntryCapturesTrueLowerUpperGeometryForRangeBandAndRangeArea(bool area, bool labels) {
        var renderer = new HtmlInteractiveChartRenderer(); var ordinary = renderer.PrepareSection(Model(10, 35, area, labels)); var target = renderer.PrepareSection(Model(20, 60, area, labels));
        var middle = renderer.PrepareSection(Model(ChartMath.InterpolateRange(10, 20, .5), ChartMath.InterpolateRange(35, 60, .5), area, labels));
        var name = "range-before-" + area + "-" + labels;
        NativeRadialMotionBeforeControls.Capture(ordinary, name + "-from"); NativeRadialMotionBeforeControls.Capture(target, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-middle");
        string? payload = null; var error = Record.Exception(() => { var from = Section(renderer, Model(10, 35, area, labels)); var to = Section(renderer, Model(20, 60, area, labels)); payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); });
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { payload, error = error?.ToString() }));
        Assert.Null(error); Assert.NotNull(payload); using var json = JsonDocument.Parse(payload!); Assert.Equal("lower", json.RootElement.GetProperty("readings")[0].GetProperty("component").GetString());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void OrdinaryNativeRangeDefaultsKeepDeterministicOutputAndNoComponentHeader(bool area) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = Model(10, 35, area, true); var first = renderer.PrepareSection(chart); var repeat = renderer.PrepareSection(chart);
        NativeRadialMotionBeforeControls.Capture(first, "range-ordinary-" + area); Assert.Equal(first.Html, repeat.Html); Assert.Equal(first.PreparedVisual.ToSvg(), repeat.PreparedVisual.ToSvg()); Assert.Equal(first.PreparedVisual.ToPng(), repeat.PreparedVisual.ToPng());
        Assert.False(NativeRawValueSourceMotionTests.Metadata(first).TryGetProperty("nativeValueSource", out _));
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ExistingSelectedBubbleComponentWireAndNativeMidpointRemainARealBeforeControl(bool area) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeBubbleComponentBeforeControls.Section(renderer, NativeBubbleComponentBeforeControls.Model(30, 10, area)); var to = NativeBubbleComponentBeforeControls.Section(renderer, NativeBubbleComponentBeforeControls.Model(50, 70, area));
        var middle = NativeBubbleComponentBeforeControls.Section(renderer, NativeBubbleComponentBeforeControls.Model(ChartMath.InterpolateRange(30, 50, .5), ChartMath.InterpolateRange(10, 70, .5), area));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3));
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, "range-bubble-control-" + area); NativeRadialMotionBeforeControls.Capture(middle, "range-bubble-control-" + area + "-middle");
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng());
        Assert.Equal("bubble-components", NativeRawValueSourceMotionTests.Metadata(from).GetProperty("nativeValueSource").GetProperty("kind").GetString());
    }
    internal static Chart Model(double lower, double upper, bool area, bool labels, int width = 760, bool dark = false) {
        var chart = Chart.Create().WithTitle("Native interval components").WithSize(width, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithLegend(true).WithDataLabels(labels);
        foreach (var key in new[] { "delivery", "comparison" }) {
            var values = new[] { new ChartRangeBand(.5, key == "delivery" ? lower : 35, key == "delivery" ? upper : 70), new ChartRangeBand(1.5, 25, 55), new ChartRangeBand(3, 30, 65, true) };
            var series = area ? ChartSeries.CreateRangeArea(new ChartSeriesId(key), key, values) : ChartSeries.CreateRangeBand(new ChartSeriesId(key), key, values);
            series.WithObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later"), new ChartObservationId("disconnected") }); chart.AddSeries(series);
        }
        chart.ConfigureXAxis(axis => { axis.Minimum = 0; axis.Maximum = 4; }); chart.ConfigureYAxis(axis => { axis.Minimum = 0; axis.Maximum = 100; }); return chart;
    }
    internal static HtmlChartSection Section(HtmlInteractiveChartRenderer renderer, Chart chart) => renderer.PrepareSection(chart, configure: options => { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-range-components"; });
}
