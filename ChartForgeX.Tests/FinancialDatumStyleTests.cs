using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FinancialDatumStyleTests {
    private static readonly ChartColor Blue = ChartColor.FromHex("#3377CC");
    private static readonly ChartColor Orange = ChartColor.FromHex("#CC7733");
    private static readonly ChartColor Green = ChartColor.FromHex("#22AA66");

    [Fact]
    public void DatumStylesAreAtomicDetachedAndUseDirectionFallbackAndObservationColorPrecedence() {
        var chart = FinancialMarkOptionsTests.Prices(ChartSeriesKind.Candlestick).WithLegend(true).WithPointLegend();
        var series = chart.Series[0];
        series.ConfigureFinancial(options => {
            options.Rising.Fill = Green; options.Rising.FillOpacity = .5;
            options.Rising.Wick.Stroke = Orange; options.Rising.Wick.StrokeWidth = 3;
            options.Falling.Stroke = Green;
        });
        ChartFinancialItemOptions? captured = null;
        series.ConfigureFinancialValues((value, index, options) => {
            if (index != 0) return;
            Assert.True(value.Close >= value.Open);
            options.Stroke = Blue; options.StrokeWidth = 5;
            captured = options;
        });
        captured!.Stroke = Orange;
        var prepared = Prepare(chart);
        var body = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().First(node => node.Role == "candlestick-body");
        var wick = prepared.Scene.Nodes.OfType<VisualSceneLine>().First(node => node.Role == "candlestick-wick");
        Assert.Equal(ChartColorMath.WithOpacity(Green, .5), body.Fill);
        Assert.Equal(Blue, body.Stroke); Assert.Equal(5, body.StrokeWidth);
        Assert.Equal(Orange, wick.Stroke); Assert.Equal(3, wick.StrokeWidth);
        Assert.Equal(Blue, prepared.Scene.Nodes.OfType<VisualSceneRectangle>().First(node => node.Role == "legend-financial-body").Stroke);
        var originalSvg = prepared.ToSvg(); var originalPng = prepared.ToPng();
        Assert.Throws<InvalidOperationException>(() => series.ConfigureFinancialValues((_, index, options) => {
            options.Stroke = Orange;
            if (index == 1) throw new InvalidOperationException("Unusable source style.");
        }));
        Assert.Equal(originalSvg, Prepare(chart).ToSvg());
        series.WithPointColor(0, Orange);
        var changed = Prepare(chart);
        body = changed.Scene.Nodes.OfType<VisualSceneRectangle>().First(node => node.Role == "candlestick-body");
        wick = changed.Scene.Nodes.OfType<VisualSceneLine>().First(node => node.Role == "candlestick-wick");
        Assert.Equal(ChartColorMath.WithOpacity(Orange, .5), body.Fill);
        Assert.Equal(Orange, body.Stroke); Assert.Equal(Orange, wick.Stroke);
        Assert.Equal(originalSvg, prepared.ToSvg()); Assert.Equal(originalPng, prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick)]
    [InlineData(ChartSeriesKind.Ohlc)]
    public void TypedUpdatePreservesDatumStyleAndReplacementClearsIt(ChartSeriesKind kind) {
        var chart = FinancialMarkOptionsTests.Prices(kind);
        var series = chart.Series[0];
        series.ConfigureFinancial(options => options.Rising.Stroke = Green);
        series.ConfigureFinancialValue(0, options => { options.Stroke = Blue; options.StrokeWidth = 7; });
        series.WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("second") });
        series.UpdateFinancialValue(0, new ChartCandlestick(1, 10, 16, 6, 13));
        Assert.Equal("first", series.ObservationIds[0].Value);
        var role = kind == ChartSeriesKind.Candlestick ? "candlestick-body" : "ohlc-stem";
        var prepared = Prepare(chart);
        var mark = prepared.Scene.Nodes.OfType<VisualSceneMark>().First(node => node.Role == role);
        Assert.Equal(Blue, mark.Stroke); Assert.Equal(7, mark.StrokeWidth);
        series.UseSeriesFinancialStyle(0);
        mark = Prepare(chart).Scene.Nodes.OfType<VisualSceneMark>().First(node => node.Role == role);
        Assert.Equal(Green, mark.Stroke);
        series.ConfigureFinancialValue(0, options => options.Stroke = Blue);
        series.ReplaceFinancialValues(series.FinancialValues);
        Assert.Empty(series.ObservationIds);
        mark = Prepare(chart).Scene.Nodes.OfType<VisualSceneMark>().First(node => node.Role == role);
        Assert.Equal(Green, mark.Stroke);
    }

    [Fact]
    public void DatumConfigurationRejectsWrongFamilyAndOhlcFillWithoutInstallingPartialStyles() {
        var line = Chart.Create().AddLine("Samples", new[] { new ChartPoint(1, 2) }).Series[0];
        Assert.Throws<InvalidOperationException>(() => line.ConfigureFinancialValue(0, options => options.Stroke = Blue));
        var chart = FinancialMarkOptionsTests.Prices(ChartSeriesKind.Ohlc);
        var series = chart.Series[0]; var before = Prepare(chart).ToSvg();
        Assert.Throws<InvalidOperationException>(() => series.ConfigureFinancialValue(0, options => options.FillOpacity = 0));
        Assert.Throws<InvalidOperationException>(() => series.ConfigureFinancialValues((_, index, options) => {
            if (index == 0) options.Stroke = Blue;
            else options.Wick.StrokeWidth = 2;
        }));
        Assert.Equal(before, Prepare(chart).ToSvg());
        Assert.Throws<ArgumentOutOfRangeException>(() => series.ConfigureFinancialValue(2, options => options.Stroke = Blue));
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
