using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the common default number policy independently of a host's language and authored overrides.</summary>
public sealed class SharedDefaultNumberFormattingTests {
    [Fact]
    public void GroupedNumericReadoutSemanticsCannotDependOnTheHostCulture() {
        var prior = CultureInfo.CurrentCulture;
        try {
            string Render(string culture) {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                return Chart.Create().WithXAxisBounds(1000, 2000)
                    .AddScatter("Samples", new[] { new ChartPoint(1234, 12500.125) })
                    .Prepare(new VisualRenderContext()).ToSvg();
            }
            var invariant = Render(string.Empty);
            Assert.Equal(invariant, Render("pl-PL"));
            var point = Assert.Single(XDocument.Parse(invariant).Descendants(), node => node.Attribute("data-cfx-point") != null);
            Assert.Equal("1,234", (string?)point.Attribute("data-cfx-formatted-x"));
            Assert.Equal("1234", (string?)point.Attribute("data-cfx-x"));
            Assert.Equal("Samples: x 1234, y 12.5k (12500.125)", (string?)point.Attribute("data-cfx-description"));
        } finally { CultureInfo.CurrentCulture = prior; }
    }

    [Theory]
    [InlineData(2611.111, "2,611.111")]
    [InlineData(1.23456, "1.235")]
    [InlineData(12500.125, "12.5k")]
    [InlineData(1000, "1,000")]
    public void AutomaticValuesAreInvariantGroupedAndTrimmedOnAPolishHost(double value, string expected) {
        var prior = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
            Assert.Equal(expected, ChartNumericFormatter.FormatValue(Chart.Create().Options, value));
        } finally { CultureInfo.CurrentCulture = prior; }
    }

    [Fact]
    public void FineDefaultTicksKeepTheirStepAndGroupingWithoutPaddingZeros() {
        Assert.Equal(new[] { "10,000", "10,010", "10,020" },
            ChartNumericFormatter.FormatCompactScaleValues(new[] { 10000d, 10010, 10020 }));
        Assert.Equal(new[] { "1,000", "1,000.5", "1,001" },
            ChartNumericFormatter.FormatCompactScaleValues(new[] { 1000d, 1000.5, 1001 }));
    }

    [Fact]
    public void DefaultScalarFactsShareOneCompactUnitWithoutPaddingZeros() {
        Assert.Equal(new[] { "0", "2.6k", "9.5k", "10k" }, ChartNumericFormatter.FormatScaleValues(Chart.Create().Options,
            new[] { 0d, 2611.111, 9500.125, 10000 }));
    }

    [Fact]
    public void ScalarTicksKeepTheirActualStepWhileReadingsUseCompactCaptions() {
        Assert.Equal(new[] { "0", "1,250", "2.6k", "10,000" }, ChartNumericFormatter.FormatScaleValues(Chart.Create().Options.ValueFormat,
            new[] { 0d, 1250, 2611.111, 10000 }, new[] { 0d, 1250, 2500, 10000 }));
    }

    [Fact]
    public void ScalarDisambiguationCannotMoveFractionalTicksWhenLargeReadingsRoundTogether() {
        var ticks = new[] { 1000000.75, 1000025.75, 1000050.75, 1000075.75, 1000100.75 };
        var facts = ticks.Concat(new[] { 1000006d, 1000016 }).ToArray();
        var captions = ChartNumericFormatter.FormatScaleValues(Chart.Create().Options.ValueFormat, facts, ticks);
        Assert.Equal(new[] { "1,000,000.75", "1,000,025.75", "1,000,050.75", "1,000,075.75", "1,000,100.75" }, captions.Take(5));
        Assert.Equal(new[] { "1,000,006", "1,000,016" }, captions.Skip(5));
    }

    [Fact]
    public void ScheduleAndColorKeyPositionsUseTheSameStepAwareDefaultTicks() {
        var chart = Chart.Create();
        var ticks = new[] { 10000d, 10250, 10500, 10750, 11000 };
        var expected = new[] { "10,000", "10,250", "10,500", "10,750", "11,000" };
        var schedule = ChartAxisValueFormatter.Create(chart.Options.XAxis, ticks, valueDefaults: chart.Options);
        Assert.Equal(expected, ticks.Select(schedule));
        var color = new ChartColorPresentation(chart.Options.HeatmapColorDimension, chart.Options.Labels, chart.Options.Labels.Value, chart.Options.ValueFormat);
        Assert.Equal(expected, color.ScaleValues(ticks));
    }

    [Fact]
    public void MapClusterDisplayPoliciesCannotReplaceTheRawCount() {
        var sites = ChartSeries.CreateDottedMap(new ChartSeriesId("sites"), "Sites", new[] {
            new ChartMapPoint("A", 10, 50), new ChartMapPoint("B", 10, 50), new ChartMapPoint("C", 10, 50)
        }).WithMapClustering(new ChartMapClustering(48, 3));
        var chart = Chart.Create().WithSize(800, 440).WithMapViewport(ChartMapViewport.Europe()).AddSeries(sites)
            .WithValueFormat(ChartValueFormat.Number("0 'items'"));
        var cluster = Assert.Single(XDocument.Parse(chart.ToSvg()).Descendants(), node =>
            (string?)node.Attribute("data-cfx-derived") == "map-grid-cluster");
        Assert.Equal("3", cluster.Attribute("data-cfx-value")!.Value);
        Assert.Equal("3", cluster.Attribute("data-cfx-source-count")!.Value);
        Assert.Equal("3 items", cluster.Attribute("data-cfx-formatted-value")!.Value);
    }

    [Fact]
    public void AnAuthoredCultureAndFixedPrecisionRemainAuthoritative() {
        var chart = Chart.Create().WithValueFormat(ChartValueFormat.Number("#,0.0000 '°C'", CultureInfo.GetCultureInfo("pl-PL")));
        var expected = 2611.111.ToString("#,0.0000 '°C'", CultureInfo.GetCultureInfo("pl-PL"));
        Assert.Equal(expected, ChartNumericFormatter.FormatValue(chart.Options, 2611.111));
        Assert.Equal(new[] { expected }, ChartNumericFormatter.FormatScaleValues(chart.Options, new[] { 2611.111 }));
    }

    [Fact]
    public void DerivedSharesKeepTheExplicitNumberCultureAndTheirPercentUnit() {
        var format = ChartValueFormat.Percentages(ChartValueFormat.Number("0.###", CultureInfo.GetCultureInfo("pl-PL")), new[] { .23, .196 });
        Assert.Equal("23,0%", format.Format(.23));
        Assert.Equal("19,6%", format.Format(.196));
    }

    [Fact]
    public void DefaultProgressRingAverageNamesItsPercentUnitAndKeepsTheSourceValues() {
        var values = new[] { 27d, 34, 41, 48, 26, 33 };
        var chart = Chart.Create().WithSize(600, 440).WithDataLabels().WithProgressRingCenterLabel()
            .AddSeries(new ChartSeries(new ChartSeriesId("completion"), "Completion", ChartSeriesKind.ProgressRing,
                values.Select((value, index) => new ChartPoint(index + 1, value))));
        var document = XDocument.Parse(chart.ToSvg());
        var center = Assert.Single(document.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "progress-ring-value");
        Assert.Equal("34.8%", center.Value);
        Assert.Equal(values, chart.Series[0].Points.Select(point => point.Y));
        chart.WithValueFormat(ChartValueFormat.Number("0.000 'points'"));
        document = XDocument.Parse(chart.ToSvg());
        center = Assert.Single(document.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "progress-ring-value");
        Assert.Equal("34.833 points", center.Value);
    }
}
