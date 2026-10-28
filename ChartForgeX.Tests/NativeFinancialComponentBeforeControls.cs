using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeFinancialComponentBeforeControls {
    [Theory] [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void ExistingNativeEntryCapturesActualFinancialPricesAndDirectionCrossing(bool ohlc, bool labels) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = Model(ohlc, labels); var endChart = Model(ohlc, labels, 60, 80, 10, 40);
        var middleChart = Model(ohlc, labels, ChartMath.InterpolateRange(40, 60, .5), ChartMath.InterpolateRange(70, 80, .5), ChartMath.InterpolateRange(20, 10, .5), ChartMath.InterpolateRange(60, 40, .5));
        var ordinary = renderer.PrepareSection(chart); var end = renderer.PrepareSection(endChart); var middle = renderer.PrepareSection(middleChart); var name = "financial-before-" + ohlc + "-" + labels;
        NativeRadialMotionBeforeControls.Capture(ordinary, name + "-from"); NativeRadialMotionBeforeControls.Capture(end, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-middle");
        string? payload = null; var error = Record.Exception(() => payload = renderer.PrepareNativeTransitionJson(Section(renderer, chart), Section(renderer, endChart), new HtmlNativeTransitionOptions(frameCount: 3)));
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { payload, error = error?.ToString(), prices = chart.Series[0].FinancialValues.Select(item => new { item.X, item.Open, item.High, item.Low, item.Close }), middle = middleChart.Series[0].FinancialValues.Select(item => new { item.X, item.Open, item.High, item.Low, item.Close }) }));
        Assert.Null(error); Assert.NotNull(payload); using var json = JsonDocument.Parse(payload!); Assert.Equal(new[] { "open", "high", "low", "close" }, json.RootElement.GetProperty("readings").EnumerateArray().Take(4).Select(row => row.GetProperty("component").GetString()));
    }
    [Theory] [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void OrdinaryFinancialDefaultAndAuthoredDirectionStylesKeepNativeBytes(bool ohlc, bool styled) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = Model(ohlc, true, styled: styled); var first = renderer.PrepareSection(chart); var repeat = renderer.PrepareSection(chart);
        NativeRadialMotionBeforeControls.Capture(first, "financial-ordinary-" + ohlc + "-" + styled); Assert.Equal(first.Html, repeat.Html); Assert.Equal(first.PreparedVisual.ToSvg(), repeat.PreparedVisual.ToSvg()); Assert.Equal(first.PreparedVisual.ToPng(), repeat.PreparedVisual.ToPng());
        Assert.False(NativeRawValueSourceMotionTests.Metadata(first).TryGetProperty("nativeValueSource", out _)); Assert.Equal(2, chart.Series[0].FinancialValues.Count);
    }
    internal static Chart Model(bool ohlc, bool labels, double open = 40, double high = 70, double low = 20, double close = 60, int width = 760, bool dark = false, bool styled = true) {
        var chart = Chart.Create().WithTitle("Actual financial components").WithSize(width, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithLegend(true).WithDataLabels(labels);
        foreach (var key in new[] { "delivery", "comparison" }) {
            var changed = key == "delivery"; var values = new[] { new ChartCandlestick(1, changed ? open : 40, changed ? high : 70, changed ? low : 20, changed ? close : 60), new ChartCandlestick(3, 50, 75, 25, 45) };
            var series = (ohlc ? ChartSeries.CreateOhlc(new ChartSeriesId(key), key, values) : ChartSeries.CreateCandlestick(new ChartSeriesId(key), key, values))
                .WithObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later") });
            if (styled) {
                series.ConfigureFinancial(options => {
                    options.Rising.Stroke = ChartColor.FromHex("#218C74"); options.Rising.StrokeWidth = 2.5; options.Rising.DashPattern = new[] { 3d, 2d }; options.Rising.DashOffset = -1;
                    options.Falling.Stroke = ChartColor.FromHex("#CF6D45"); options.Falling.StrokeWidth = 3; options.Falling.DashPattern = new[] { 2d, 1d }; options.Falling.StrokeOpacity = .8;
                    if (!ohlc) { options.Rising.FillOpacity = .25; options.Falling.FillOpacity = 0; options.Rising.Wick.StrokeWidth = 1.5; options.Falling.Wick.DashPattern = new[] { 4d, 2d }; }
                });
                series.ConfigureFinancialValue(0, options => { options.StrokeWidth = 4; if (!ohlc) options.Wick.Stroke = ChartColor.FromHex("#3987E5"); });
                series.WithPointColor(1, ChartColor.FromHex("#729BB7"));
            }
            chart.AddSeries(series);
        }
        chart.ConfigureXAxis(axis => { axis.Minimum = 0; axis.Maximum = 4; }); chart.ConfigureYAxis(axis => { axis.Minimum = 0; axis.Maximum = 100; }); return chart;
    }
    internal static HtmlChartSection Section(HtmlInteractiveChartRenderer renderer, Chart chart) => renderer.PrepareSection(chart, configure: options => { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-financial-components"; });
}
