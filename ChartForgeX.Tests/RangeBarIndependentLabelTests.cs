using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RangeBarIndependentLabelTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EndpointStyleAndPhysicalPlacementAreIndependentWhileRawOrderAndSourceRemainExact(bool horizontal) {
        var chart = RangeBarEndpointLabelTests.Fixture(horizontal); chart.RemoveSeries(chart.Series[1]);
        chart.Series[0].WithPointLabel(0, "Promised interval").ConfigurePointDataLabelStyle(0, style => style.WithFontSize(9).WithColor("#884488"));
        chart.ConfigureRangeBar(options => {
            options.EndpointLabelFormatter = context => context.Endpoint == ChartRangeBarEndpoint.Start ? "S" : "E";
            options.StartLabel.Placement = horizontal ? ChartDataLabelPlacement.Above : ChartDataLabelPlacement.Left;
            options.EndLabel.Placement = horizontal ? ChartDataLabelPlacement.Below : ChartDataLabelPlacement.Right;
            options.StartLabel.Style.WithFontSize(11).WithColor("#aa2200");
            options.EndLabel.Style.WithFontSize(15).WithColor("#0044bb");
        });
        var prepared = RangeBarEndpointLabelTests.Prepare(chart);
        ScatterBubbleColorRenderingTests.Capture(prepared, "range-independent-" + (horizontal ? "horizontal" : "vertical"));
        var labels = RangeBarEndpointLabelTests.Labels(prepared); Assert.Equal(2, labels.Length);
        var start = Assert.Single(labels, label => label.Id == "series-0-point-0-start-label");
        var end = Assert.Single(labels, label => label.Id == "series-0-point-0-end-label");
        Assert.Equal(11, start.Text.Style.FontSize); Assert.Equal(15, end.Text.Style.FontSize);
        Assert.Equal(ChartColor.FromHex("#aa2200"), start.Text.Style.Color); Assert.Equal(ChartColor.FromHex("#0044bb"), end.Text.Style.Color);
        var mark = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "range-bar").Bounds;
        var first = RangeBarEndpointLabelTests.Bounds(start); var last = RangeBarEndpointLabelTests.Bounds(end);
        Assert.True(horizontal ? first.Bottom < mark.Top && last.Top > mark.Bottom : first.Right < mark.Left && last.Left > mark.Right);
        var point = Assert.Single(RangeBarEndpointLabelTests.Points(prepared));
        Assert.Equal("30", point.Metadata["data-cfx-start"]); Assert.Equal("-10", point.Metadata["data-cfx-end"]);
        Assert.Equal("Promised interval", point.Metadata["data-cfx-label"]);
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "data-label-source"),
            group => Assert.Equal(point.Id, group.Metadata["data-cfx-label-for"]));
        Assert.Equal(2, labels.Select(label => label.Id).Distinct().Count());
    }

    [Fact]
    public void HiddenEndpointStillCapturesItsTextAndConfigurationDoesNotLeakNestedStyleReferences() {
        var chart = RangeBarEndpointLabelTests.Fixture(); chart.RemoveSeries(chart.Series[1]);
        ChartRangeBarOptions? callbackOptions = null; var calls = new List<ChartRangeBarEndpoint>();
        chart.ConfigureRangeBar(options => {
            callbackOptions = options; options.StartLabel.Visible = false; options.EndLabel.Style.WithColor("#0044bb");
            options.EndpointLabelFormatter = context => { calls.Add(context.Endpoint); return context.Endpoint.ToString(); };
        });
        callbackOptions!.StartLabel.Visible = true; callbackOptions.EndLabel.Style.WithColor("#ff0000");
        var prepared = RangeBarEndpointLabelTests.Prepare(chart); var label = Assert.Single(RangeBarEndpointLabelTests.Labels(prepared));
        Assert.Equal("series-0-point-0-end-label", label.Id); Assert.Equal(ChartColor.FromHex("#0044bb"), label.Text.Style.Color);
        Assert.Equal(new[] { ChartRangeBarEndpoint.Start, ChartRangeBarEndpoint.End }, calls);
        var point = Assert.Single(RangeBarEndpointLabelTests.Points(prepared)); Assert.Equal("Start", point.Metadata["data-cfx-start-label"]);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Throws<InvalidOperationException>(() => chart.ConfigureRangeBar(options => {
            options.EndLabel.Style.WithColor("#ff0000"); options.EndLabel.Visible = false; throw new InvalidOperationException("Rejected configuration");
        }));
        Assert.True(chart.Options.RangeBar.EndLabel.Visible); Assert.Equal(ChartColor.FromHex("#0044bb"), chart.Options.RangeBar.EndLabel.Style.Color);
        chart.Options.RangeBar.EndpointLabelFormatter = _ => throw new InvalidOperationException("Detached export");
        chart.Options.RangeBar.EndLabel.Style.WithFontSize(90); chart.ClearSeries();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.Equal(2, calls.Count);
    }

    [Fact]
    public void FormatterMutationCannotChangeTheCapturedTypographyOfLaterSeries() {
        var chart = RangeBarEndpointLabelTests.Fixture();
        chart.ConfigureRangeBar(options => {
            options.StartLabel.Style.WithColor("#aa2200");
            options.EndpointLabelFormatter = context => {
                chart.Options.RangeBar.StartLabel.Style.WithColor("#00ff00");
                return context.Endpoint == ChartRangeBarEndpoint.Start ? "S" : "E";
            };
        });
        var prepared = RangeBarEndpointLabelTests.Prepare(chart);
        Assert.All(RangeBarEndpointLabelTests.Labels(prepared).Where(label => label.Id!.Contains("-start-")),
            label => Assert.Equal(ChartColor.FromHex("#aa2200"), label.Text.Style.Color));
        Assert.Equal(2, RangeBarEndpointLabelTests.Labels(prepared).Count(label => label.Id!.Contains("-start-")));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.RangeBar.StartLabel.Placement = (ChartDataLabelPlacement)99);
    }
}
