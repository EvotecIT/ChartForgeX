using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RangeBarEndpointLabelTests {
    [Fact]
    public void CombinedDefaultsKeepCustomCaptionsAndLazyNumericFormattingExact() {
        var chart = Fixture().WithDataLabels();
        Assert.Equal(ChartRangeBarLabelContent.Combined, new ChartRangeBarOptions().LabelContent);
        chart.Options.RangeBar.LabelContent = ChartRangeBarLabelContent.Combined;
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.RangeBar.LabelContent = (ChartRangeBarLabelContent)99);
        foreach (var series in chart.Series) series.WithPointLabel(0, "Authored caption");
        chart.WithValueFormatter(_ => throw new InvalidOperationException("Custom captions bypass numeric formatting"));
        var original = Prepare(chart); var svg = original.ToSvg(); var pixels = original.ToRgba().Pixels;
        chart.ConfigureRangeBar(options => {
            options.LabelContent = ChartRangeBarLabelContent.Combined;
            options.EndpointLabelFormatter = _ => throw new FormatException("Combined captions do not invoke endpoint formatting");
        });
        var configured = Prepare(chart);
        Assert.Equal(svg, configured.ToSvg()); Assert.Equal(pixels, configured.ToRgba().Pixels);
        Assert.All(Points(configured), point => Assert.Equal("Authored caption", point.Metadata["data-cfx-label"]));
        Assert.All(Labels(configured), label => Assert.Equal("Authored caption", label.Text.Lines[0].Text));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CapturedCallbacksKeepAuthoredOrderPointHeadingsAndDetachedExports(bool visible) {
        var chart = Fixture().WithDataLabels(visible);
        chart.Series[0].WithPointLabel(0, "Promised <&> delivery");
        chart.AddRangeBar("Reserve", new[] { new ChartInterval(1, 5, 5) });
        var calls = new List<ChartRangeBarEndpointLabelContext>(); var numericCalls = 0;
        chart.WithValueFormatter(value => { numericCalls++; return value.ToString("0.0", CultureInfo.GetCultureInfo("pl-PL")); });
        chart.ConfigureRangeBar(options => options.EndpointLabelFormatter = context => {
            calls.Add(context); return context.Endpoint + " <&> " + context.FormattedValue;
        });
        var prepared = Prepare(chart); var points = Points(prepared);
        Assert.Equal(6, numericCalls); Assert.Equal(6, calls.Count);
        Assert.Equal(new[] { "Expected:0:Start:30", "Expected:0:End:-10", "Observed:0:Start:20", "Observed:0:End:0", "Reserve:0:Start:5", "Reserve:0:End:5" },
            calls.Select(call => call.SeriesName + ":" + call.PointIndex + ":" + call.Endpoint + ":" + call.Value.ToString(CultureInfo.InvariantCulture)));
        Assert.All(calls, call => { Assert.Equal("Express", call.CategoryLabel); Assert.Equal(1, call.Interval.X); });
        Assert.Equal(30, calls[0].Interval.Start); Assert.Equal(-10, calls[0].Interval.End);
        Assert.Equal("30,0", calls[0].FormattedValue);
        Assert.Equal("Promised <&> delivery", points[0].Metadata["data-cfx-label"]);
        Assert.Equal("Start <&> 30,0", points[0].Metadata["data-cfx-start-label"]);
        Assert.Equal("End <&> -10,0", points[0].Metadata["data-cfx-end-label"]);
        Assert.Equal("30", points[0].Metadata["data-cfx-start"]);
        Assert.Equal("-10", points[0].Metadata["data-cfx-end"]);
        Assert.Equal("0", points[0].Metadata["data-cfx-source-point"]);
        Assert.Equal("1", points[0].Metadata["data-cfx-source-count"]);
        if (!visible) Assert.Empty(Labels(prepared));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.DoesNotContain("Start <&>", svg); Assert.Contains("Start &lt;&amp;&gt;", svg);
        chart.ConfigureRangeBar(options => {
            options.EndpointLabelFormatter = _ => throw new InvalidOperationException("Detached export must not call again");
            options.LabelContent = ChartRangeBarLabelContent.Combined;
        });
        chart.WithValueFormatter(_ => throw new InvalidOperationException("Detached numeric text"));
        chart.ClearSeries();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Equal(6, calls.Count); Assert.Equal(6, numericCalls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n\t")]
    public void EmptyEndpointOutputSuppressesOnlyThatCaptionWithoutReplacingTheHeading(string? output) {
        var chart = Fixture(); chart.RemoveSeries(chart.Series[1]); chart.Series[0].WithPointLabel(0, "Source heading");
        var calls = 0;
        chart.Options.RangeBar.EndpointLabelFormatter = context => { calls++; return context.Endpoint == ChartRangeBarEndpoint.Start ? output : "Finished"; };
        var prepared = Prepare(chart); var point = Assert.Single(Points(prepared)); var label = Assert.Single(Labels(prepared));
        Assert.Equal(2, calls); Assert.Equal("series-0-point-0-end-label", label.Id);
        Assert.Equal("Finished", label.Text.Lines[0].Text);
        Assert.Equal("Source heading", point.Metadata["data-cfx-label"]);
        Assert.False(point.Metadata.ContainsKey("data-cfx-start-label"));
        Assert.Equal("Finished", point.Metadata["data-cfx-end-label"]);
        Assert.Equal("30", point.Metadata["data-cfx-start"]); Assert.Equal("-10", point.Metadata["data-cfx-end"]);
    }

    [Fact]
    public void FormatterExceptionsSurfaceFromPreparationWithoutAnEmptyFallback() {
        var chart = Fixture(); var calls = 0;
        chart.Options.RangeBar.EndpointLabelFormatter = _ => { calls++; throw new FormatException("Authored endpoint failure"); };
        Assert.Equal("Authored endpoint failure", Assert.Throws<FormatException>(() => Prepare(chart)).Message);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void AuthoredEndpointCaptionsFollowMappedEndpointsAndKeepOneSourceAssociation(bool horizontal, bool reversed, bool logarithmic) {
        var chart = Fixture(horizontal);
        if (logarithmic) foreach (var series in chart.Series) {
            series.UpdateInterval(0, new ChartInterval(1, 90, 10));
        }
        var valueAxis = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        valueAxis.WithBounds(logarithmic ? 1 : -20, logarithmic ? 1000 : 40).WithReversal(reversed);
        if (logarithmic) valueAxis.WithScale(ChartScaleKind.Logarithmic);
        chart.Options.RangeBar.EndpointLabelFormatter = context => context.Endpoint == ChartRangeBarEndpoint.Start ? "S" : "E";
        var prepared = Prepare(chart); var labels = Labels(prepared);
        Assert.Equal(4, labels.Length);
        var start = labels.Single(label => label.Id == "series-0-point-0-start-label");
        var end = labels.Single(label => label.Id == "series-0-point-0-end-label");
        Assert.Equal(horizontal ? !reversed : reversed, horizontal ? start.X > end.X : start.Baseline > end.Baseline);
        Assert.All(labels, label => Assert.True(LabelPlacementService.Contains(new ChartRect(0, 0, 800, 440), Bounds(label))));
        var decorations = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "data-label-source").ToArray();
        Assert.Equal(4, decorations.Length);
        Assert.All(decorations, group => Assert.Contains(group.Metadata["data-cfx-label-for"], new[] { "series-0-point-0", "series-1-point-0" }));
        Assert.Equal(4, decorations.Select(group => group.Id).Distinct().Count());
        var xml = XDocument.Parse(prepared.ToSvg());
        var ids = xml.Descendants().Attributes("id").Select(attribute => attribute.Value).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.Equal(2, Points(prepared).Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CategoryReversalMovesBothCaptionsWithoutChangingEndpointValuesOrSourceAssociations(bool horizontal) {
        var chart = Fixture(horizontal); chart.RemoveSeries(chart.Series[1]);
        chart.Series[0].ReplaceIntervals(new[] { chart.Series[0].Intervals[0], new ChartInterval(3, 30, -10) });
        chart.Series[0].WithDataLabelPlacement(ChartDataLabelPlacement.Center);
        chart.Options.RangeBar.EndpointLabelFormatter = context => context.Endpoint == ChartRangeBarEndpoint.Start ? "S" : "E";
        var categoryAxis = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        categoryAxis.WithBounds(0, 4);
        var original = Prepare(chart); categoryAxis.WithReversal(); var reversed = Prepare(chart);
        var before = Labels(original); var after = Labels(reversed);
        double Category(VisualSceneText label) => horizontal ? Bounds(label).Top : Bounds(label).Left;
        double Value(VisualSceneText label) => horizontal ? label.X : label.Baseline;
        Assert.Equal(!horizontal, Category(before[0]) < Category(before[2]));
        Assert.Equal(horizontal, Category(after[0]) < Category(after[2]));
        for (var index = 0; index < before.Length; index++) {
            Assert.Equal(before[index].Id, after[index].Id);
            Assert.Equal(Value(before[index]), Value(after[index]), 8);
        }
        Assert.Equal(new[] { "1", "3" }, Points(reversed).Select(point => point.Metadata["data-cfx-x"]));
        var sources = reversed.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "data-label-source");
        Assert.Equal(new[] { "series-0-point-0", "series-0-point-0", "series-0-point-1", "series-0-point-1" },
            sources.Select(group => group.Metadata["data-cfx-label-for"]));
    }

    [Theory]
    [InlineData(false, ChartDataLabelPlacement.Inside)]
    [InlineData(false, ChartDataLabelPlacement.Center)]
    [InlineData(true, ChartDataLabelPlacement.Inside)]
    [InlineData(true, ChartDataLabelPlacement.Center)]
    public void InsideCaptionsUseSeparateIntervalHalvesAndSharedPointTypography(bool horizontal, ChartDataLabelPlacement placement) {
        var chart = Fixture(horizontal); chart.RemoveSeries(chart.Series[1]);
        chart.Series[0].WithDataLabelPlacement(placement).ConfigurePointDataLabelStyle(0, style => style.WithColor("#a033aa").WithFontSize(11));
        var prepared = Prepare(chart); var labels = Labels(prepared); var body = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), mark => mark.Role == "range-bar").Bounds;
        Assert.Equal(2, labels.Length);
        Assert.All(labels, label => {
            Assert.Equal(11, label.Text.Style.FontSize); Assert.Equal(ChartColor.FromHex("#a033aa"), label.Text.Style.Color);
            Assert.True(LabelPlacementService.Contains(body, Bounds(label)));
        });
        var first = Bounds(labels[0]); var second = Bounds(labels[1]);
        Assert.True(first.Right <= second.Left || second.Right <= first.Left || first.Bottom <= second.Top || second.Bottom <= first.Top);
    }

    [Fact]
    public void VerticalSecondaryIntervalsAndScalarMixturesKeepTheirExistingLabelOwners() {
        var chart = Fixture(); chart.Series[1].ValueAxis = ChartAxisSide.Secondary;
        chart.Series[1].UpdateInterval(0, new ChartInterval(1, 900, 100));
        chart.Options.SecondaryYAxis.WithBounds(0, 1000).WithLabelFormatter(_ => "Axis-only");
        chart.AddLine("Scalar", new[] { new ChartPoint(.25, 5), new ChartPoint(1.75, 5) });
        chart.Series[2].WithPointLabel(0, "Scalar label").WithPointLabel(1, "Scalar label");
        var prepared = Prepare(chart); var point = Points(prepared).Single(group => group.Metadata["data-cfx-series"] == "1");
        Assert.Equal("secondary", point.Metadata["data-cfx-axis"]);
        Assert.Equal("900", point.Metadata["data-cfx-start-label"]);
        Assert.Equal("100", point.Metadata["data-cfx-end-label"]);
        Assert.Contains(Labels(prepared), label => label.Id == "series-1-point-0-start-label");
        Assert.Contains(Labels(prepared), label => label.Id == "series-2-point-0-label");
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "data-label-source"), group => group.Metadata["data-cfx-full-label"] == "Axis-only");
    }

    [Fact]
    public void ExplicitClippingAndCrowdedLabelsRetainCompleteEndpointCaptionsAndRawPairs() {
        var chart = Fixture(true).WithSize(300, 160); chart.RemoveSeries(chart.Series[1]);
        chart.Options.XAxis.WithBounds(0, 20);
        chart.Options.RangeBar.EndpointLabelFormatter = context => "Complete authored " + context.Endpoint + " caption that cannot fit the clipped endpoint";
        var prepared = Prepare(chart); var point = Assert.Single(Points(prepared));
        Assert.Equal(0, chart.Options.XAxis.Minimum); Assert.Equal(20, chart.Options.XAxis.Maximum);
        Assert.Equal("30", point.Metadata["data-cfx-start"]); Assert.Equal("-10", point.Metadata["data-cfx-end"]);
        Assert.StartsWith("Complete authored Start", point.Metadata["data-cfx-start-label"]);
        Assert.StartsWith("Complete authored End", point.Metadata["data-cfx-end-label"]);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        Assert.All(Labels(prepared), label => Assert.True(LabelPlacementService.Contains(new ChartRect(0, 0, 300, 160), Bounds(label))));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CustomEndpointProseKeepsSingleLineShorteningAndCompleteSourceFacts(bool horizontal) {
        const string start = "Preparation complete in regional queue";
        const string end = "Delivery complete in regional queue";
        var chart = Fixture(horizontal).WithSize(220, 180); chart.RemoveSeries(chart.Series[1]);
        chart.Series[0].UpdateInterval(0, new ChartInterval(1, -10, 30));
        chart.Options.RangeBar.EndpointLabelFormatter = context => context.Endpoint == ChartRangeBarEndpoint.Start ? start : end;
        var prepared = Prepare(chart); var captions = Labels(prepared);
        Assert.Equal(2, captions.Length);
        Assert.All(captions, caption => {
            Assert.EndsWith("…", Assert.Single(caption.Text.Lines).Text);
            Assert.Equal(12, caption.Text.Size);
        });
        var point = Assert.Single(Points(prepared));
        Assert.Equal(start, point.Metadata["data-cfx-start-label"]); Assert.Equal(end, point.Metadata["data-cfx-end-label"]);
        Assert.Equal("-10", point.Metadata["data-cfx-start"]); Assert.Equal("30", point.Metadata["data-cfx-end"]);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        ScatterBubbleColorRenderingTests.Capture(prepared, "endpoint-prose-" + (horizontal ? "horizontal" : "vertical"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratedEndpointNumbersStillOmitWholeInsteadOfShortening(bool horizontal) {
        const string number = "123456789012345678901234567890.123456789";
        var chart = Fixture(horizontal).WithSize(220, 180); chart.RemoveSeries(chart.Series[1]);
        chart.WithValueFormatter(_ => number);
        var prepared = Prepare(chart);
        Assert.Empty(Labels(prepared));
        var point = Assert.Single(Points(prepared));
        Assert.Equal(number, point.Metadata["data-cfx-start-label"]); Assert.Equal(number, point.Metadata["data-cfx-end-label"]);
        Assert.Equal("30", point.Metadata["data-cfx-start"]); Assert.Equal("-10", point.Metadata["data-cfx-end"]);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        Assert.NotEmpty(prepared.ToPng());
    }

    internal static Chart Fixture(bool horizontal = false) {
        var chart = Chart.Create().WithSize(800, 440).WithHeader(false).WithLegend(false).WithAxes(false).WithDataLabels()
            .WithXLabels("Express").WithBarStyle(ChartBarStyle.Flat)
            .AddSeries(ChartSeries.CreateRangeBar(new ChartSeriesId("expected"), "Expected", new[] { new ChartInterval(1, 30, -10) }))
            .AddSeries(ChartSeries.CreateRangeBar(new ChartSeriesId("observed"), "Observed", new[] { new ChartInterval(1, 20, 0) }))
            .ConfigureRangeBar(options => {
                options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
                options.LabelContent = ChartRangeBarLabelContent.Endpoints;
            });
        chart.Options.ShowGrid = false;
        (horizontal ? chart.Options.XAxis : chart.Options.YAxis).WithBounds(-20, 40);
        (horizontal ? chart.Options.YAxis : chart.Options.XAxis).WithBounds(0, 2);
        RoundedRadialSeriesTests.UseFixtureFont(chart);
        return chart;
    }

    internal static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    internal static VisualSceneGroup[] Points(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
    internal static VisualSceneText[] Labels(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "data-label").ToArray();
    internal static ChartRect Bounds(VisualSceneText text) => new(text.X, text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height);
}
