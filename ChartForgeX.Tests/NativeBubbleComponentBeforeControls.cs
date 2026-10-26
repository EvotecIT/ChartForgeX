using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeBubbleComponentBeforeControls {
    [Theory] [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void ExistingOptInAndNativeTransitionApiPrepareAuthoredBubbleYAndSize(bool area, bool reversed) {
        var renderer = new HtmlInteractiveChartRenderer();
        var ordinary = renderer.PrepareSection(Model(30, 10, area, reversed));
        var target = renderer.PrepareSection(Model(50, 70, area, reversed));
        var middle = renderer.PrepareSection(Model(ChartMath.InterpolateRange(30, 50, .5), ChartMath.InterpolateRange(10, 70, .5), area, reversed));
        var name = "bubble-before-" + area + "-" + reversed;
        NativeRadialMotionBeforeControls.Capture(ordinary, name + "-from"); NativeRadialMotionBeforeControls.Capture(target, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-native-middle");
        string? payload = null; Exception? error = null;
        try { var from = Section(renderer, Model(30, 10, area, reversed)); var to = Section(renderer, Model(50, 70, area, reversed)); payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); }
        catch (Exception exception) { error = exception; }
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { area, reversed, payload, error = error?.ToString() }));
        Assert.Null(error); Assert.NotNull(payload);
        using var json = JsonDocument.Parse(payload!); Assert.Equal("cartesian-component-value", json.RootElement.GetProperty("sourceKind").GetString());
    }
    [Theory] [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void OrdinaryTypedBubbleDefaultsKeepDeterministicOutputAndNoRawHeader(bool area, bool reversed) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = Model(30, 10, area, reversed);
        var first = renderer.PrepareSection(chart); var repeated = renderer.PrepareSection(chart);
        NativeRadialMotionBeforeControls.Capture(first, "bubble-ordinary-" + area + "-" + reversed);
        Assert.Equal(first.Html, repeated.Html); Assert.Equal(first.PreparedVisual.ToSvg(), repeated.PreparedVisual.ToSvg()); Assert.Equal(first.PreparedVisual.ToPng(), repeated.PreparedVisual.ToPng());
        Assert.False(NativeRawValueSourceMotionTests.Metadata(first).TryGetProperty("nativeValueSource", out _));
        Assert.Equal(2, chart.Series[0].Bubbles.Count); Assert.Equal("current", chart.Series[1].ObservationIds[0].Value);
    }
    internal static Chart Model(double y, double size, bool area = false, bool reversed = false, int width = 760, bool dark = false, bool labels = false) {
        var chart = Chart.Create().WithTitle("Authored Bubble components").WithSize(width, 420)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithLegend(true).WithDataLabels(labels);
        foreach (var key in new[] { "delivery", "comparison" }) {
            var series = ChartSeries.CreateBubble(new ChartSeriesId(key), key, new[] {
                new ChartBubble(1, key == "delivery" ? y : 65, key == "delivery" ? size : 35).WithColorValue(7),
                new ChartBubble(3, 45, 25).WithColorValue(11) })
                .WithObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later") });
            series.ConfigureMarkers(markers => { markers.Shape = area ? ChartMarkerShape.Diamond : ChartMarkerShape.Circle; }); chart.AddSeries(series);
        }
        chart.ConfigureXAxis(axis => { axis.Minimum = 0; axis.Maximum = 4; });
        chart.ConfigureYAxis(axis => { axis.Minimum = 0; axis.Maximum = 100; });
        chart.ConfigureBubble(options => { options.WithSizeDomain(0, 100); options.MinimumRadius = 6; options.MaximumRadius = 18;
            options.SizeEncoding = area ? ChartBubbleSizeEncoding.Area : ChartBubbleSizeEncoding.SquareRootRadius; options.Reversed = reversed;
            options.SizeDimension.Name = "Effort"; options.SizeDimension.ShowLegend = false; });
        return chart;
    }
    internal static HtmlChartSection Section(HtmlInteractiveChartRenderer renderer, Chart chart) => renderer.PrepareSection(chart,
        configure: options => { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-bubble-components"; });
}
