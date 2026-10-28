using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BoxPlotStatisticLabelTests {
    [Fact]
    public void DefaultMedianOutputAndLazyCustomHeadingRemainByteIdenticalWhenStatisticControlsAreUnused() {
        var chart = Fixture(); chart.Options.BoxPlot.LabelContent = ChartBoxPlotLabelContent.Median;
        chart.Series[0].WithPointLabel(0, "Authored heading"); chart.WithValueFormatter(_ => throw new InvalidOperationException("Lazy custom heading"));
        var original = Prepare(chart); var svg = original.ToSvg(); var png = original.ToPng();
        chart.ConfigureBoxPlot(options => {
            options.StatisticLabelFormatter = _ => throw new InvalidOperationException("Median mode ignores statistic formatting");
            options.MedianLabel.Visible = false; options.MedianLabel.Style.WithFontSize(42);
        });
        var configured = Prepare(chart); Assert.Equal(svg, configured.ToSvg()); Assert.Equal(png, configured.ToPng());
        Assert.Equal("Authored heading", Assert.Single(Labels(configured)).Text.Lines[0].Text);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.BoxPlot.LabelContent = (ChartBoxPlotLabelContent)99);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void IndependentStatisticsFollowPhysicalValueAxesWithoutChangingRawFacts(bool horizontal, bool reversed, bool secondary) {
        var chart = Fixture(horizontal); var series = chart.Series[0];
        if (secondary) series.UseSecondaryValueAxis();
        var axis = horizontal ? secondary ? chart.Options.SecondaryXAxis : chart.Options.XAxis
            : secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
        axis.WithBounds(-50, 70).WithReversal(reversed);
        chart.ConfigureBoxPlot(options => {
            options.LabelContent = ChartBoxPlotLabelContent.Statistics;
            options.StatisticLabelFormatter = context => context.Statistic.ToString();
            foreach (var component in Components(options)) component.Placement = horizontal ? ChartDataLabelPlacement.Above : ChartDataLabelPlacement.Right;
            options.MinimumLabel.Style.WithColor("#aa2200").WithFontSize(10);
            options.MedianLabel.Style.WithColor("#0044bb").WithFontSize(13);
        });
        var prepared = Prepare(chart); Capture(prepared, "stat-axis-" + horizontal + "-" + reversed + "-" + secondary);
        var labels = Labels(prepared); Assert.Equal(5, labels.Length);
        var names = new[] { "minimum", "q1", "median", "q3", "maximum" };
        var ordered = names.Select(name => Assert.Single(labels, label => label.Id == "series-0-point-0-" + name + "-label")).ToArray();
        var coordinates = ordered.Select(label => horizontal ? label.X : label.Baseline).ToArray();
        Assert.Equal(horizontal ? reversed : !reversed, coordinates[0] > coordinates[4]);
        Assert.Equal(ChartColor.FromHex("#aa2200"), ordered[0].Text.Style.Color); Assert.Equal(10, ordered[0].Text.Style.FontSize);
        Assert.Equal(ChartColor.FromHex("#0044bb"), ordered[2].Text.Style.Color); Assert.Equal(13, ordered[2].Text.Style.FontSize);
        var point = Assert.Single(Summaries(prepared));
        Assert.Equal(new[] { "-30", "-10", "10", "30", "50" }, names.Select(name => point.Metadata["data-cfx-" + name]));
        Assert.Equal(secondary ? "secondary" : "primary", point.Metadata["data-cfx-axis"]);
        Assert.Equal("0", point.Metadata["data-cfx-source-point"]); Assert.Equal("1", point.Metadata["data-cfx-source-count"]);
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "data-label-source"),
            group => Assert.Equal(point.Id, group.Metadata["data-cfx-label-for"]));
        var ids = XDocument.Parse(prepared.ToSvg()).Descendants().Attributes("id").Select(attribute => attribute.Value).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count()); Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void IqrFormatterCapturesRenderedWhiskersOnceAndPreservesSourceExtremaHeadingsAndDetachedExports() {
        var series = ChartSeries.CreateBoxPlot(new ChartSeriesId("latency"), "Latency", 1, new[] { 0d, 100, 101, 102 })
            .ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr).WithPointLabel(0, "Raw measurements");
        var chart = Chart.Create().WithSize(850, 480).WithHeader(false).WithLegend(false).WithAxes(false).WithDataLabels()
            .WithXLabels("Service A").WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture)).AddSeries(series);
        var calls = new List<ChartBoxPlotStatisticLabelContext>();
        chart.ConfigureBoxPlot(options => {
            options.LabelContent = ChartBoxPlotLabelContent.Statistics; options.MinimumLabel.Visible = false;
            options.StatisticLabelFormatter = context => { calls.Add(context); return context.Statistic == ChartBoxPlotStatistic.Q1 ? null : context.Statistic + ": " + context.FormattedValue; };
        });
        RoundedRadialSeriesTests.UseFixtureFont(chart); var prepared = Prepare(chart); Capture(prepared, "stat-iqr-captured");
        Assert.Equal(Enum.GetValues<ChartBoxPlotStatistic>(), calls.Select(context => context.Statistic));
        Assert.All(calls, context => { Assert.Equal("Latency", context.SeriesName); Assert.Equal("Service A", context.CategoryLabel); Assert.Equal(0, context.PointIndex); });
        Assert.Equal(75, calls[0].Value); Assert.Equal(100.5, calls[2].Value);
        var point = Assert.Single(Summaries(prepared)); Assert.Equal("0", point.Metadata["data-cfx-source-minimum"]);
        Assert.Equal("75", point.Metadata["data-cfx-minimum"]); Assert.Equal("Raw measurements", point.Metadata["data-cfx-label"]);
        Assert.Equal("Minimum: 75", point.Metadata["data-cfx-minimum-label"]); Assert.False(point.Metadata.ContainsKey("data-cfx-q1-label"));
        Assert.DoesNotContain(Labels(prepared), label => label.Id!.Contains("-minimum-") || label.Id.Contains("-q1-"));
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "data-label-source"),
            group => Assert.Equal(point.Id, group.Metadata["data-cfx-label-for"]));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Options.BoxPlot.StatisticLabelFormatter = _ => throw new InvalidOperationException("Detached formatter");
        chart.Options.BoxPlot.MaximumLabel.Style.WithFontSize(90); chart.ClearSeries();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.Equal(5, calls.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StatisticAnchorsUseTheExistingLogarithmicProjectionInsteadOfLinearValueSpacing(bool horizontal) {
        var chart = Fixture(horizontal); chart.Series[0].ReplaceBoxPlots(new[] { new ChartBoxPlot(1, 1, 10, 100, 1000, 10000) });
        (horizontal ? chart.Options.XAxis : chart.Options.YAxis).WithBounds(.1, 100000).WithScale(ChartScaleKind.Logarithmic);
        chart.ConfigureBoxPlot(options => {
            options.LabelContent = ChartBoxPlotLabelContent.Statistics;
            foreach (var label in Components(options)) label.Placement = horizontal ? ChartDataLabelPlacement.Above : ChartDataLabelPlacement.Left;
        });
        var prepared = Prepare(chart); Capture(prepared, "stat-log-" + horizontal); var labels = Labels(prepared); Assert.Equal(5, labels.Length);
        var centers = labels.Select(label => RangeBarEndpointLabelTests.Bounds(label))
            .Select(bounds => horizontal ? bounds.Left + bounds.Width / 2 : bounds.Top + bounds.Height / 2).ToArray();
        var step = centers[1] - centers[0];
        for (var index = 1; index < centers.Length - 1; index++) Assert.Equal(step, centers[index + 1] - centers[index], 6);
        Assert.Equal(new[] { "1", "10", "100", "1000", "10000" }, new[] { "minimum", "q1", "median", "q3", "maximum" }
            .Select(name => Assert.Single(Summaries(prepared)).Metadata["data-cfx-" + name]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrowdedClippedAndEqualStatisticsKeepFullFactsAndOnlyCompleteNumericCaptions(bool horizontal) {
        var chart = Fixture(horizontal).WithSize(240, 180);
        chart.Series[0].ReplaceBoxPlots(new[] { new ChartBoxPlot(1, 4, 4, 4, 4, 4) });
        const string completeNumber = "123456789012345678901234567890";
        chart.WithValueFormatter(_ => completeNumber).ConfigureBoxPlot(options => options.LabelContent = ChartBoxPlotLabelContent.Statistics);
        var context = VisualExportRequest.ForChart(chart).Context;
        var viewport = horizontal ? new ChartCartesianViewport(x: new ChartAxisWindow(0, 5))
            : new ChartCartesianViewport(y: new ChartAxisWindow(0, 5));
        var prepared = chart.Prepare(context, viewport); Capture(prepared, "stat-crowded-clipped-" + horizontal); var point = Assert.Single(Summaries(prepared));
        Assert.Equal(new[] { "4", "4", "4", "4", "4" }, new[] { "minimum", "q1", "median", "q3", "maximum" }.Select(name => point.Metadata["data-cfx-" + name]));
        Assert.All(Labels(prepared), label => Assert.Equal(completeNumber, Assert.Single(label.Text.Lines).Text));
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        Assert.Equal(-50, (horizontal ? chart.Options.XAxis : chart.Options.YAxis).Minimum);
        Assert.All(Labels(prepared), label => Assert.True(LabelPlacementService.Contains(new ChartRect(0, 0, 240, 180), RangeBarEndpointLabelTests.Bounds(label))));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void NestedConfigurationAndPreparationSnapshotsDoNotLeakStylesOrSuppressions() {
        var chart = Fixture(); ChartBoxPlotOptions? leaked = null;
        chart.ConfigureBoxPlot(options => {
            leaked = options; options.LabelContent = ChartBoxPlotLabelContent.Statistics; options.MinimumLabel.Style.WithColor("#aa2200");
            options.StatisticLabelFormatter = context => {
                chart.Options.BoxPlot.MinimumLabel.Style.WithColor("#00ff00"); chart.Options.BoxPlot.MaximumLabel.Visible = false;
                return context.Statistic.ToString();
            };
        });
        leaked!.MinimumLabel.Style.WithColor("#ff0000"); leaked.MaximumLabel.Visible = false;
        var prepared = Prepare(chart); var labels = Labels(prepared);
        Assert.Equal(5, labels.Length); Assert.Equal(ChartColor.FromHex("#aa2200"), labels.Single(label => label.Id!.Contains("-minimum-")).Text.Style.Color);
        Assert.Throws<InvalidOperationException>(() => chart.ConfigureBoxPlot(options => {
            options.MedianLabel.Style.WithFontSize(99); throw new InvalidOperationException("Reject nested changes");
        }));
        Assert.Null(chart.Options.BoxPlot.MedianLabel.Style.FontSize);
    }

    [Fact]
    public void HiddenStatisticCaptionsStillCaptureEveryMemberAndFormatterFailuresRemainVisible() {
        var chart = Fixture().WithDataLabels(false); var calls = new List<ChartBoxPlotStatistic>();
        chart.ConfigureBoxPlot(options => {
            options.LabelContent = ChartBoxPlotLabelContent.Statistics;
            options.StatisticLabelFormatter = context => { calls.Add(context.Statistic); return context.FormattedValue; };
        });
        var prepared = Prepare(chart); Assert.Empty(Labels(prepared));
        Assert.Equal(Enum.GetValues<ChartBoxPlotStatistic>(), calls);
        Assert.Equal("50", Assert.Single(Summaries(prepared)).Metadata["data-cfx-maximum-label"]);
        chart.Options.BoxPlot.StatisticLabelFormatter = _ => throw new FormatException("Authored statistic failure");
        Assert.Equal("Authored statistic failure", Assert.Throws<FormatException>(() => Prepare(chart)).Message);
    }

    internal static Chart Fixture(bool horizontal = false) {
        var chart = Chart.Create().WithSize(850, 480).WithHeader(false).WithLegend(false).WithAxes(false).WithDataLabels()
            .WithXLabels("Service A").WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture))
            .ConfigureBoxPlot(options => options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical)
            .AddSeries(ChartSeries.CreateBoxPlot(new ChartSeriesId("summary"), "Delivery", new[] { new ChartBoxPlot(1, -30, -10, 10, 30, 50) }));
        chart.Options.ShowGrid = false; (horizontal ? chart.Options.XAxis : chart.Options.YAxis).WithBounds(-50, 70);
        (horizontal ? chart.Options.YAxis : chart.Options.XAxis).WithBounds(0, 2); RoundedRadialSeriesTests.UseFixtureFont(chart); return chart;
    }

    private static ChartValueLabelOptions[] Components(ChartBoxPlotOptions options) => new[] { options.MinimumLabel, options.Q1Label, options.MedianLabel, options.Q3Label, options.MaximumLabel };
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneText[] Labels(PreparedVisual prepared) => RangeBarEndpointLabelTests.Labels(prepared);
    private static VisualSceneGroup[] Summaries(PreparedVisual prepared) => RangeBarEndpointLabelTests.Points(prepared).Where(point => point.Metadata.ContainsKey("data-cfx-median")).ToArray();
    private static void Capture(PreparedVisual prepared, string name) => ScatterBubbleColorRenderingTests.Capture(prepared, "boxplot-" + name);
}
