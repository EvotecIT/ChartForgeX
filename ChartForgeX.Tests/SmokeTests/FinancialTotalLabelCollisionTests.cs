using System.Globalization;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void DenseFinancialAndTotalLabelsAvoidCollisions() {
        string CloseFormatter(double value) => "Close " + value.ToString("0", CultureInfo.InvariantCulture);
        var candles = Enumerable.Range(1, 16)
            .Select(value => new ChartCandlestick(value, 40 + value * 0.2, 50 + value * 0.2, 35 + value * 0.2, 45 + value * 0.2))
            .ToArray();

        var candlestick = Chart.Create()
            .WithSize(300, 220)
            .WithDataLabels()
            .WithValueFormatter(CloseFormatter)
            .AddCandlestick("Windows", candles);
        AssertUsefulSubset(candlestick.ToSvg(), candles.Length, "Dense candlestick labels should avoid collisions.");
        Assert(candlestick.ToPng().Length > 64, "PNG candlestick label collision avoidance should render valid output.");

        var ohlc = Chart.Create()
            .WithSize(300, 220)
            .WithDataLabels()
            .WithValueFormatter(CloseFormatter)
            .AddOhlc("Windows", candles);
        AssertUsefulSubset(ohlc.ToSvg(), candles.Length, "Dense OHLC labels should avoid collisions.");
        Assert(ohlc.ToPng().Length > 64, "PNG OHLC label collision avoidance should render valid output.");

        var deltas = Enumerable.Range(1, 16)
            .Select(value => new ChartPoint(value, value % 3 == 0 ? -3 : 5))
            .ToArray();
        var waterfall = Chart.Create()
            .WithSize(300, 220)
            .WithDataLabels()
            .WithValueFormatter(value => "Delta " + value.ToString("0", CultureInfo.InvariantCulture))
            .AddWaterfall("Delta", deltas);
        AssertUsefulSubset(waterfall.ToSvg(), deltas.Length + 1, "Dense waterfall labels should avoid collisions.");
        Assert(waterfall.ToPng().Length > 64, "PNG waterfall label collision avoidance should render valid output.");

        var stacked = Chart.Create()
            .WithSize(300, 220)
            .WithStackedBars()
            .WithStackTotals()
            .WithValueFormatter(value => "Total " + value.ToString("0", CultureInfo.InvariantCulture))
            .AddBar("Passed", Points(10, 12, 11, 13, 12, 11, 10, 12, 11, 13, 12, 11, 10, 12, 11, 13))
            .AddBar("Warnings", Points(2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3));
        AssertUsefulStackTotalSubset(stacked.ToSvg(), 16, "Dense stacked total labels should avoid collisions.");
        Assert(stacked.ToPng().Length > 64, "PNG stacked total label collision avoidance should render valid output.");

        var horizontalStacked = Chart.Create()
            .WithSize(300, 220)
            .WithStackedHorizontalBars()
            .WithStackTotals()
            .WithValueFormatter(value => "Total " + value.ToString("0", CultureInfo.InvariantCulture))
            .AddHorizontalBar("Passed", Points(10, 12, 11, 13, 12, 11, 10, 12, 11, 13, 12, 11, 10, 12, 11, 13))
            .AddHorizontalBar("Warnings", Points(2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3));
        AssertUsefulSubset(horizontalStacked.ToSvg(), 16, "Dense horizontal stack total labels should avoid collisions.");
        Assert(horizontalStacked.ToPng().Length > 64, "PNG horizontal stack total label collision avoidance should render valid output.");
    }

    private static void AssertUsefulStackTotalSubset(string svg, int itemCount, string message) {
        var labelCount = System.Xml.Linq.XDocument.Parse(svg).Descendants().Count(e => (string?)e.Attribute("data-cfx-role") == "stack-total-label" && !e.AncestorsAndSelf().Any(a => (string?)a.Attribute("display") == "none"));
        Assert(labelCount > 0 && labelCount <= itemCount, message);
        Assert(Rendering.ChartLabelScene.Inspect(svg, Typography.FontSpec.SystemSans()).LabelLabel == 0, message);
    }
}
