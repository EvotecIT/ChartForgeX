using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeErrorComponentBeforeControls {
    [Theory] [InlineData("x", false)] [InlineData("x", true)] [InlineData("y", false)] [InlineData("y", true)] [InlineData("xy", false)] [InlineData("xy", true)]
    public void ExistingNativeEntryCapturesTrueEstimateAndOnlyAuthoredErrorBounds(string form, bool labels) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = Model(form, labels); var target = Model(form, labels, 50, .25, 1.75, 40, 60);
        var middle = Model(form, labels, ChartMath.InterpolateRange(30, 50, .5), ChartMath.InterpolateRange(.5, .25, .5), ChartMath.InterpolateRange(1.5, 1.75, .5), ChartMath.InterpolateRange(20, 40, .5), ChartMath.InterpolateRange(40, 60, .5));
        var ordinary = renderer.PrepareSection(chart); var end = renderer.PrepareSection(target); var nativeMiddle = renderer.PrepareSection(middle); var name = "error-before-" + form + "-" + labels;
        NativeRadialMotionBeforeControls.Capture(ordinary, name + "-from"); NativeRadialMotionBeforeControls.Capture(end, name + "-to"); NativeRadialMotionBeforeControls.Capture(nativeMiddle, name + "-middle");
        string? payload = null; var error = Record.Exception(() => { var from = Section(renderer, chart); var to = Section(renderer, target); payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); });
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { payload, error = error?.ToString(), form, xBounds = chart.Series[0].ErrorBars[0].XBounds.HasValue, yBounds = chart.Series[0].ErrorBars[0].YBounds.HasValue }));
        Assert.Null(error); Assert.NotNull(payload); using var json = JsonDocument.Parse(payload!); Assert.Equal("value", json.RootElement.GetProperty("readings")[0].GetProperty("component").GetString());
    }
    [Theory] [InlineData("x")] [InlineData("y")] [InlineData("xy")]
    public void OrdinaryNativeErrorFormsKeepTheirRealNullableIntervalsAndDefaultBytes(string form) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = Model(form, true); var first = renderer.PrepareSection(chart); var repeat = renderer.PrepareSection(chart);
        NativeRadialMotionBeforeControls.Capture(first, "error-ordinary-" + form); Assert.Equal(first.Html, repeat.Html); Assert.Equal(first.PreparedVisual.ToSvg(), repeat.PreparedVisual.ToSvg()); Assert.Equal(first.PreparedVisual.ToPng(), repeat.PreparedVisual.ToPng());
        Assert.Equal(form.Contains('x'), chart.Series[0].ErrorBars[0].XBounds.HasValue); Assert.Equal(form.Contains('y'), chart.Series[0].ErrorBars[0].YBounds.HasValue); Assert.False(NativeRawValueSourceMotionTests.Metadata(first).TryGetProperty("nativeValueSource", out _));
    }
    internal static Chart Model(string form, bool labels, double y = 30, double xLower = .5, double xUpper = 1.5, double yLower = 20, double yUpper = 40, int width = 760, bool dark = false) {
        var chart = Chart.Create().WithTitle("Actual uncertainty components").WithSize(width, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithLegend(true).WithDataLabels(labels);
        foreach (var key in new[] { "delivery", "comparison" }) {
            var first = new ChartErrorBar(1, key == "delivery" ? y : 30, form.Contains('x') ? new ChartErrorBounds(key == "delivery" ? xLower : .5, key == "delivery" ? xUpper : 1.5) : null,
                form.Contains('y') ? new ChartErrorBounds(key == "delivery" ? yLower : 20, key == "delivery" ? yUpper : 40) : null);
            var later = new ChartErrorBar(3, 45, form.Contains('x') ? new ChartErrorBounds(2.5, 3.5) : null, form.Contains('y') ? new ChartErrorBounds(35, 55) : null);
            chart.AddSeries(ChartSeries.CreateErrorBar(new ChartSeriesId(key), key, new[] { first, later }).WithObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later") }));
        }
        chart.ConfigureXAxis(axis => { axis.Minimum = 0; axis.Maximum = 4; }); chart.ConfigureYAxis(axis => { axis.Minimum = 0; axis.Maximum = 100; }); return chart;
    }
    internal static HtmlChartSection Section(HtmlInteractiveChartRenderer renderer, Chart chart) => renderer.PrepareSection(chart, configure: options => { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-error-components"; });
}
