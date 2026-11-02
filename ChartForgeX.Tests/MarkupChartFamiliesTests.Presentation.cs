using ChartForgeX.Core;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MarkupChartFamiliesTests {
    [Theory]
    [InlineData("candlestick")]
    [InlineData("ohlc")]
    public void IsoDateCandleCommandsAndTablesRetainWeeklyInstantsAndNativeTimeAxis(string family) {
        var dates = new[] { new DateTime(2026, 6, 1), new DateTime(2026, 6, 8) };
        var candles = new[] { new ChartCandlestick(dates[0], 102, 110, 98, 108), new ChartCandlestick(dates[1], 108, 114, 104, 106) };
        foreach (var body in new[] {
            "candle 2026-06-01 102 110 98 108\ncandle 2026-06-08 108 114 104 106",
            "candles:\n| x | open | high | low | close |\n| --- | --- | --- | --- | --- |\n| 2026-06-01 | 102 | 110 | 98 | 108 |\n| 2026-06-08 | 108 | 114 | 104 | 106 |\nend"
        }) {
            var chart = ParseChart("type " + family + "\nseries Price\n" + body);
            Assert.Equal(ChartScaleKind.Time, chart.Options.XAxis.Scale);
            Assert.Equal(dates.Select(date => date.ToOADate()), Assert.Single(chart.Series).FinancialValues.Select(value => value.X));
            var expected = Chart.Create().WithXAxisTimeScale();
            if (family == "ohlc") expected.AddOhlc("Price", candles);
            else expected.AddCandlestick("Price", candles);
            AssertNativeArtifacts(expected, chart, 920, VisualThemeMode.Light, "dated-" + family);
        }
    }

    [Fact]
    public void AuthoredHeatmapMeasurementNameReachesTheNativeKeyWithoutChangingDefaultTheme() {
        foreach (var option in new[] { "option heatmapValueName \"Coverage %\"", "options:\n| key | value |\n| heatmapValueName | Coverage % |\nend" }) {
            var chart = ParseChart("type heatmap\nlabels Build Test\nseries Windows values 96 88\n" + option);
            var expected = Chart.Create().WithXLabels("Build", "Test").AddHeatmapRow("Windows", new[] { new ChartPoint(1, 96), new ChartPoint(2, 88) });
            expected.Options.HeatmapColorDimension.Name = "Coverage %";
            Assert.Equal("Coverage %", chart.Options.HeatmapColorDimension.Name);
            Assert.Equal(expected.Options.Theme.ToJson(), chart.Options.Theme.ToJson());
            AssertNativeArtifacts(expected, chart, 920, VisualThemeMode.Light, "named-heatmap");
        }
    }
}
