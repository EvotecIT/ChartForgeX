using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeComponentTimeBeforeControls {
    [Theory]
    [InlineData("candlestick", "elapsed")] [InlineData("candlestick", "sessions")] [InlineData("candlestick", "ordinal")] [InlineData("candlestick", "calendar")]
    [InlineData("ohlc", "elapsed")] [InlineData("ohlc", "sessions")] [InlineData("ohlc", "ordinal")] [InlineData("ohlc", "calendar")]
    [InlineData("bubble", "ordinal")] [InlineData("range", "calendar")] [InlineData("error", "elapsed")]
    public void ExistingNativeEntryRetainsRealFixedTimeXAndNativeNumericComponents(string family, string policy) {
        var renderer = new HtmlInteractiveChartRenderer(); var fromChart = Model(family, policy, 40); var toChart = Model(family, policy, 60); var middleChart = Model(family, policy, ChartMath.InterpolateRange(40, 60, .5));
        var ordinary = renderer.PrepareSection(fromChart); var to = renderer.PrepareSection(toChart); var middle = renderer.PrepareSection(middleChart); var name = "time-component-before-" + family + "-" + policy;
        NativeRadialMotionBeforeControls.Capture(ordinary, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-middle");
        string? payload = null; var error = Record.Exception(() => payload = renderer.PrepareNativeTransitionJson(Section(renderer, fromChart), Section(renderer, toChart), new HtmlNativeTransitionOptions(frameCount: 3)));
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { payload, error = error?.ToString(), family, policy, minimum = ordinary.PreparedVisual.XProjection?.Minimum, maximum = ordinary.PreparedVisual.XProjection?.Maximum, instants = ordinary.PreparedVisual.XProjection?.TimeDomain?.Instants, sessions = ordinary.PreparedVisual.XProjection?.TimeSessions?.Sessions.Select(row => new { row.Minimum, row.Maximum }) })); Assert.Null(error); Assert.NotNull(payload);
    }
    [Theory] [InlineData("elapsed")] [InlineData("sessions")] [InlineData("ordinal")] [InlineData("calendar")]
    public void OrdinaryTimeOwnersAndAuthoredAxesStayDetachedAndDeterministic(string policy) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = Model("candlestick", policy, 40); var first = renderer.PrepareSection(chart); var repeated = renderer.PrepareSection(chart);
        NativeRadialMotionBeforeControls.Capture(first, "time-component-ordinary-" + policy); Assert.Equal(first.Html, repeated.Html); Assert.Equal(first.PreparedVisual.ToSvg(), repeated.PreparedVisual.ToSvg()); Assert.Equal(first.PreparedVisual.ToPng(), repeated.PreparedVisual.ToPng());
        Assert.Equal(ChartScaleKind.Time, first.PreparedVisual.XProjection!.Scale); Assert.Null(chart.Options.XAxis.PreparedTimeDomain); Assert.False(NativeRawValueSourceMotionTests.Metadata(first).TryGetProperty("nativeValueSource", out _));
    }
    internal static Chart Model(string family, string policy, double value, int width = 760, bool dark = false) {
        var first = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc); var later = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
        var chart = Chart.Create().WithTitle("Captured time categories").WithSize(width, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithLegend(true).WithDataLabels(true);
        var id = new ChartSeriesId("delivery"); ChartSeries series;
        if (family == "bubble") {
            series = ChartSeries.CreateBubble(id, family, new[] { new ChartBubble(first, value, 20), new ChartBubble(later, 45, 30) });
            chart.ConfigureBubble(options => { options.WithSizeDomain(0, 100); options.SizeDimension.ShowLegend = false; options.MinimumRadius = 6; options.MaximumRadius = 18; });
        } else if (family == "range") series = ChartSeries.CreateRangeArea(id, family, new[] { new ChartRangeBand(first, 10, value), new ChartRangeBand(later, 20, 55) });
        else if (family == "error") series = ChartSeries.CreateErrorBar(id, family, new[] { new ChartErrorBar(first, value, yBounds: new ChartErrorBounds(value - 10, value + 10)), new ChartErrorBar(later, 45, yBounds: new ChartErrorBounds(35, 55)) });
        else {
            var prices = new[] { new ChartCandlestick(first, 40, 80, 10, value), new ChartCandlestick(later, 45, 70, 20, 50) };
            series = family == "ohlc" ? ChartSeries.CreateOhlc(id, family, prices) : ChartSeries.CreateCandlestick(id, family, prices);
        }
        chart.AddSeries(series.WithObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later") })); chart.ConfigureYAxis(axis => { axis.Minimum = 0; axis.Maximum = 100; });
        chart.WithXAxisTimeScale(TimeZoneInfo.Utc).WithXAxisBounds(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToOADate(), new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc).ToOADate());
        chart.ConfigureXAxis(axis => {
            if (policy == "sessions") axis.WithTimeSessions(new ChartTimeSessionDomain(new[] { new ChartTimeSession(first.AddDays(-1), first.AddDays(1)), new ChartTimeSession(later.AddDays(-1), later.AddDays(1)) }));
            else if (policy == "ordinal") axis.WithTimeSpacing(ChartTimeSpacing.Ordinal);
            else if (policy == "calendar") axis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month)));
            else if (policy != "elapsed") throw new ArgumentOutOfRangeException(nameof(policy));
        });
        return chart;
    }
    internal static HtmlChartSection Section(HtmlInteractiveChartRenderer renderer, Chart chart) => renderer.PrepareSection(chart, configure: options => { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-time-components"; });
}
