using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FinancialRangeCombinationLifecycleTests {
    [Theory]
    [InlineData(ChartSeriesKind.Candlestick, 390, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Ohlc, 800, VisualThemeMode.Dark)]
    public void FinancialAndRangeCompanionsRetainIndependentExtremaStylesAndIdentityThroughReplacement(
        ChartSeriesKind kind, int width, VisualThemeMode mode) {
        var prices = new[] { new ChartCandlestick(1, 30, 90, 10, 60), new ChartCandlestick(4, 60, 80, 20, 40) };
        var financial = kind == ChartSeriesKind.Candlestick
            ? ChartSeries.CreateCandlestick(new ChartSeriesId("prices"), "Prices", prices)
            : ChartSeries.CreateOhlc(new ChartSeriesId("prices"), "Prices", prices);
        var range = ChartSeries.CreateRangeBand(new ChartSeriesId("forecast"), "Forecast", new[] {
            new ChartRangeBand(1, -200, -100), new ChartRangeBand(4, 100, 300)
        }).UseSecondaryValueAxis();
        financial.WithObservationIds(new[] { new ChartObservationId("opening"), new ChartObservationId("closing") });
        range.WithObservationIds(new[] { new ChartObservationId("low"), new ChartObservationId("high") });
        financial.ConfigureFinancialValues((value, _, style) => {
            if (value.High >= 90) { style.Stroke = ChartColor.FromHex("#CC7733"); style.StrokeWidth = 4; }
        });
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(width, 420)
            .WithTitle("Price and forecast").WithLegend(true).AddSeries(range).AddSeries(financial);
        AssertDomains(chart, 10, 90, -200, 300);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        AssertSource(prepared, "prices", "opening", "high", 90);
        AssertSource(prepared, "forecast", "low", "lower", -200);
        financial.UpdateFinancialValue(0, new ChartCandlestick(1, 30, 120, 5, 80));
        range.ReplaceRanges(new[] { new ChartRangeBand(1, -500, -250), new ChartRangeBand(4, 200, 400) });
        Assert.Empty(range.ObservationIds);
        range.WithObservationIds(new[] { new ChartObservationId("low"), new ChartObservationId("high") });
        chart.MoveSeries(financial, 0);
        AssertDomains(chart, 5, 120, -500, 400);
        var updated = Prepare(chart);
        AssertSource(updated, "prices", "opening", "high", 120);
        AssertSource(updated, "forecast", "low", "lower", -500);
        var mark = updated.Scene.Nodes.OfType<VisualSceneMark>().First(node => node.Role == (kind == ChartSeriesKind.Candlestick ? "candlestick-body" : "ohlc-stem"));
        Assert.Equal(ChartColor.FromHex("#CC7733"), mark.Stroke); Assert.Equal(4, mark.StrokeWidth);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        var image = RasterImageDecoder.Decode(updated.ToPng());
        Assert.Equal(width, image.Width); Assert.Equal(420, image.Height);
        Capture(prepared, "financial-range-" + kind + "-before");
        Capture(updated, "financial-range-" + kind + "-after");
    }

    private static void AssertDomains(Chart chart, double low, double high, double secondaryLow, double secondaryHigh) {
        var primary = ChartRange.FromChart(chart); var secondary = ChartRange.FromSecondaryValueAxis(chart, primary);
        Assert.True(primary.MinY <= low && primary.MaxY >= high);
        Assert.True(primary.MinY > secondaryLow && primary.MaxY < secondaryHigh);
        Assert.True(secondary.MinY <= secondaryLow && secondary.MaxY >= secondaryHigh);
    }
    private static void AssertSource(PreparedVisual prepared, string series, string observation, string fact, double value) {
        var source = Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), node =>
            (string?)node.Attribute("data-cfx-series-key") == series && (string?)node.Attribute("data-cfx-observation-id") == observation
            && node.Attribute("data-cfx-label-for") == null);
        Assert.Equal(value, double.Parse(source.Attribute("data-cfx-" + fact)!.Value, CultureInfo.InvariantCulture));
    }
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task path.");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + ".native.png"), prepared.ToPng());
    }
}
