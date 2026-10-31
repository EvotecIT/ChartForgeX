using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StripRenderingTests {
    private static VisualRenderContext Context(double width = 600, double height = 40) =>
        new(new VisualLayoutOptions(new VisualSize(width, height), 0));

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ScheduleStripsUseTheEntireSharedDomainWithoutFrameGutters(bool reversed, bool time) {
        var origin = time ? new DateTime(2026, 1, 1).ToOADate() : 0;
        var axis = new ChartAxis { Minimum = origin, Maximum = origin + 10,
            Scale = time ? ChartScaleKind.Time : ChartScaleKind.Linear, Reversed = reversed };
        var scale = new ChartStripScale(axis);
        var chart = Chart.Create().WithTitle("Host title").WithSubtitle("Host subtitle")
            .AddGanttTask("A long row label belongs in HTML", origin + 2, origin + 6);
        chart.Options.ShowDataLabels = false;

        var prepared = chart.PrepareStrip(Context(), scale);
        var svg = XDocument.Parse(prepared.ToSvg("shared-row"));
        var shape = Assert.Single(svg.Descendants(), element => Role(element) == "gantt-task-shape");
        Assert.Equal(reversed ? 240 : 120, Number(shape, "x"), 6);
        Assert.Equal(240, Number(shape, "width"), 6);
        Assert.Equal(600, Number(svg.Root!, "width"));
        Assert.Equal(40, Number(svg.Root!, "height"));
        Assert.DoesNotContain(svg.Descendants(), element => Role(element) is "legend-entry" or "schedule-axis" or "schedule-row-label" or "background");
        Assert.DoesNotContain(svg.Descendants(), element => element.Name.LocalName == "text");
        Assert.Equal("Host title", chart.Title);
        Assert.True(chart.Options.ShowAxes);
        Assert.Null(chart.Options.XAxis.Minimum);
        axis.Maximum = origin + 20;
        Assert.Equal(origin + 10, scale.Maximum);
    }

    [Fact]
    public void NumericStripKeepsTheExplicitValueDomainAcrossRows() {
        var scale = new ChartStripScale(new ChartAxis { Minimum = 0, Maximum = 100 });
        foreach (var value in new[] { 20d, 60d }) {
            var chart = Chart.Create().AddHorizontalBar("Capacity", new[] { new ChartPoint(1, value) });
            var prepared = chart.PrepareStrip(Context(), scale);
            var plot = Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds;
            Assert.Equal(0, plot.Left);
            Assert.Equal(0, plot.Top);
            Assert.Equal(600, plot.Width);
            Assert.Equal(40, plot.Height);
            var mark = Assert.Single(prepared.Regions, region => region.Role == "point").Bounds;
            Assert.Equal(0, mark.Left, 6);
            Assert.Equal(value * 6, mark.Width, 6);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AxisStripSharesPositionsAndKeepsBoundaryCaptionsInside(bool reversed) {
        var axis = new ChartAxis { Minimum = 0, Maximum = 100, Reversed = reversed };
        axis.Labels.Add(new ChartAxisLabel(0, "None"));
        axis.Labels.Add(new ChartAxisLabel(50, "Half"));
        axis.Labels.Add(new ChartAxisLabel(100, "All"));
        var scale = new ChartStripScale(axis);
        var prepared = scale.PrepareAxis(Context(height: 32));
        var svg = XDocument.Parse(prepared.ToSvg("capacity-axis"));
        var ticks = svg.Descendants().Where(element => Role(element) == "strip-axis-tick").ToArray();
        Assert.Equal(3, ticks.Length);
        Assert.Equal(reversed ? new[] { 600d, 300d, 0d } : new[] { 0d, 300d, 600d }, ticks.Select(tick => Number(tick, "x1")));
        Assert.Equal(reversed ? 480 : 120, prepared.XProjection!.Project(20), 6);
        var labels = svg.Descendants().Where(element => element.Name.LocalName == "text").ToArray();
        Assert.Equal(new[] { "None", "Half", "All" }, labels.Select(label => label.Value));
        Assert.Equal(0, Number(labels[reversed ? 2 : 0], "x"));
        Assert.InRange(Number(labels[reversed ? 0 : 2], "x"), 500, 599);
        Assert.DoesNotContain(svg.Descendants(), element => Role(element) == "point");
    }

    [Fact]
    public void StripScaleRejectsMissingOrCollapsedDomainsAndUnsupportedFamilies() {
        Assert.Throws<ArgumentException>(() => new ChartStripScale(new ChartAxis()));
        var scale = new ChartStripScale(new ChartAxis { Minimum = 0, Maximum = 100 });
        var pie = Chart.Create().AddPie("Share", new[] { new ChartPoint(1, 20) });
        Assert.Throws<NotSupportedException>(() => pie.PrepareStrip(Context(), scale));
    }

    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
