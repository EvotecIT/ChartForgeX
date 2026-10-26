using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeComponentClipBeforeControls {
    [Theory] [InlineData("bubble")] [InlineData("range")] [InlineData("error")] [InlineData("candlestick")] [InlineData("ohlc")]
    public void ExistingNativeEntryCapturesRawValuesThatEnterTheirFixedNativeClip(string family) {
        var renderer = new HtmlInteractiveChartRenderer(); var fromChart = Model(family, 120); var toChart = Model(family, 40); var middleChart = Model(family, ChartMath.InterpolateRange(120, 40, .5));
        var from = renderer.PrepareSection(fromChart); var to = renderer.PrepareSection(toChart); var middle = renderer.PrepareSection(middleChart); var name = "clip-before-" + family;
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-middle");
        string? payload = null; var error = Record.Exception(() => payload = renderer.PrepareNativeTransitionJson(Section(renderer, fromChart), Section(renderer, toChart), new HtmlNativeTransitionOptions(frameCount: 3)));
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { payload, error = error?.ToString(), family, source = 120, middle = ChartMath.InterpolateRange(120, 40, .5), destination = 40 })); Assert.Null(error); Assert.NotNull(payload);
    }
    [Fact]
    public void ExistingNativeEntryKeepsRealClippedErrorXIntervalsAndTheirAuthoredEstimate() {
        var renderer = new HtmlInteractiveChartRenderer(); var fromChart = ErrorX(-1, 2); var toChart = ErrorX(.5, 1.5); var middleChart = ErrorX(ChartMath.InterpolateRange(-1, .5, .5), ChartMath.InterpolateRange(2, 1.5, .5));
        NativeRadialMotionBeforeControls.Capture(renderer.PrepareSection(fromChart), "clip-before-error-x-from"); NativeRadialMotionBeforeControls.Capture(renderer.PrepareSection(toChart), "clip-before-error-x-to"); NativeRadialMotionBeforeControls.Capture(renderer.PrepareSection(middleChart), "clip-before-error-x-middle");
        string? payload = null; var error = Record.Exception(() => payload = renderer.PrepareNativeTransitionJson(Section(renderer, fromChart), Section(renderer, toChart), new HtmlNativeTransitionOptions(frameCount: 3)));
        NativeRadialMotionBeforeControls.Save("clip-before-error-x.facts.json", JsonSerializer.Serialize(new { payload, error = error?.ToString() })); Assert.Null(error); Assert.NotNull(payload);
    }
    [Theory] [InlineData("bubble")] [InlineData("candlestick")]
    public void OrdinaryNativeClippingIsDeterministicAndDoesNotPublishAnOptionalSource(string family) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = Model(family, 120); var from = renderer.PrepareSection(chart); var repeat = renderer.PrepareSection(chart);
        NativeRadialMotionBeforeControls.Capture(from, "clip-ordinary-" + family); Assert.Equal(from.Html, repeat.Html); Assert.Equal(from.PreparedVisual.ToSvg(), repeat.PreparedVisual.ToSvg()); Assert.Equal(from.PreparedVisual.ToPng(), repeat.PreparedVisual.ToPng()); Assert.False(NativeRawValueSourceMotionTests.Metadata(from).TryGetProperty("nativeValueSource", out _));
    }
    internal static Chart Model(string family, double value, int width = 760, bool dark = false) {
        var chart = Chart.Create().WithTitle("Raw values through native clip").WithSize(width, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithLegend(true).WithDataLabels(false);
        var id = new ChartSeriesId("delivery"); ChartSeries series;
        if (family == "bubble") {
            series = ChartSeries.CreateBubble(id, family, new[] { new ChartBubble(1, value, 20), new ChartBubble(3, 45, 30) });
            chart.ConfigureBubble(options => { options.WithSizeDomain(0, 100); options.SizeDimension.ShowLegend = false; options.MinimumRadius = 6; options.MaximumRadius = 18; });
        } else if (family == "range") series = ChartSeries.CreateRangeArea(id, family, new[] { new ChartRangeBand(1, value - 10, value + 10), new ChartRangeBand(3, 35, 55) });
        else if (family == "error") series = ChartSeries.CreateErrorBar(id, family, new[] { new ChartErrorBar(1, value, yBounds: new ChartErrorBounds(value - 10, value + 10)), new ChartErrorBar(3, 45, yBounds: new ChartErrorBounds(35, 55)) });
        else {
            var prices = new[] { new ChartCandlestick(1, value, value + 20, value - 10, value + 10), new ChartCandlestick(3, 45, 70, 20, 50) };
            series = family == "ohlc" ? ChartSeries.CreateOhlc(id, family, prices) : ChartSeries.CreateCandlestick(id, family, prices);
        }
        chart.AddSeries(series.WithObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later") })); chart.ConfigureXAxis(axis => { axis.Minimum = 0; axis.Maximum = 4; }); chart.ConfigureYAxis(axis => { axis.Minimum = 0; axis.Maximum = 100; }); return chart;
    }
    internal static Chart ErrorX(double lower, double upper) { var chart = Model("error", 40); var item = chart.Series[0].ErrorBars[0]; chart.Series[0].UpdateErrorBar(0, new ChartErrorBar(item.X, item.Y, new ChartErrorBounds(lower, upper), item.YBounds)); return chart; }
    internal static HtmlChartSection Section(HtmlInteractiveChartRenderer renderer, Chart chart) => renderer.PrepareSection(chart, configure: options => { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-clipped-components"; });
}
