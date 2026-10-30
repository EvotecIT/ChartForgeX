using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ScheduleEmbeddingTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OpenTaskFadesTowardTheUnknownEndInSvgAndRaster(bool reversed) {
        var series = ChartSeries.CreateGanttTask(new ChartSeriesId("support"), "Support", 2, 3);
        series.ReplaceGanttItems(new[] { ChartGanttItem.OpenEnded(2) });
        var chart = Chart.Create().AddSeries(series).WithXAxisBounds(0, 10)
            .WithGanttOpenEndStyle(ChartGanttOpenEndStyle.Fade);
        var scale = new ChartStripScale(new ChartAxis { Minimum = 0, Maximum = 10, Reversed = reversed });
        var prepared = chart.PrepareStrip(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(600, 40), 0)), scale);
        var svg = XDocument.Parse(prepared.ToSvg("support"));
        var task = Assert.Single(svg.Descendants(), element => Role(element) == "gantt-task");
        Assert.Equal("true", (string?)task.Attribute("data-cfx-open"));
        Assert.Equal("10", (string?)task.Attribute("data-cfx-end"));
        var gradient = Assert.Single(svg.Descendants(), element => element.Name.LocalName == "linearGradient");
        var x1 = Number(gradient, "x1"); var x2 = Number(gradient, "x2");
        Assert.True(reversed ? x1 > x2 : x1 < x2);
        var stops = gradient.Elements().ToArray();
        Assert.Equal(3, stops.Length);
        Assert.Equal("1", (string?)stops[2].Attribute("offset"));
        Assert.Equal("0", (string?)stops[2].Attribute("stop-opacity"));
        Assert.Equal(stops[0].Attribute("stop-color")!.Value, stops[2].Attribute("stop-color")!.Value);
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        byte Alpha(int x) => image.Pixels[(20 * image.Width + x) * 4 + 3];
        Assert.True(Alpha(reversed ? 10 : 590) < Alpha(reversed ? 450 : 150));
        Assert.Equal(2, series.GanttItems[0].End);
        Assert.True(series.GanttItems[0].IsOpenEnded);
        Assert.Equal(prepared.ToSvg("support"), prepared.ToSvg("support"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScheduleBandsPaintBehindMarksAndTodayRemainsVisible(bool lanes) {
        var day = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var chart = Chart.Create().WithSize(600, 200)
            .WithStateCategories(new ChartStateCategory("supported", "Supported", ChartColor.FromHex("#2468AC")));
        if (lanes) chart.AddGanttLane("Version", new[] { new ChartGanttLaneItem(day, day.AddDays(8), "supported") });
        else chart.AddGanttTask("Version", day, day.AddDays(8));
        chart.WithXAxisBounds(day.ToOADate(), day.AddDays(10).ToOADate())
            .WithGanttToday(day.AddDays(5))
            .AddVerticalBand(day.AddDays(2).ToOADate(), day.AddDays(4).ToOADate(), color: ChartColor.FromHex("#2468AC"));
        var svg = XDocument.Parse(chart.ToSvg());
        var nodes = svg.Descendants().ToArray();
        var band = Assert.Single(nodes, element => Role(element) == "annotation-band");
        var mark = Assert.Single(nodes, element => Role(element) == (lanes ? "gantt-lane-item-shape" : "gantt-task-shape"));
        Assert.True(Array.IndexOf(nodes, band) < Array.IndexOf(nodes, mark));
        Assert.True(Number(band, "width") > 0);
        Assert.Single(nodes, element => Role(element) == "gantt-now-line");
    }

    [Fact]
    public void ClassicOpenTaskRequiresAnExplicitDisplayEnd() {
        var series = ChartSeries.CreateGanttTask(new ChartSeriesId("support"), "Support", 2, 3)
            .ReplaceGanttItems(new[] { ChartGanttItem.OpenEnded(2) });
        var error = Assert.Throws<InvalidOperationException>(() => Chart.Create().AddSeries(series).ToSvg());
        Assert.Contains("explicit X-axis maximum", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ScheduleLinksAndStateHitAreasStayInsideTheSharedDisplayWindow() {
        var chart = Chart.Create().AddGanttTask("Partly visible", -5, 2).AddGanttTask("Outside", -5, -2);
        for (var index = 0; index < chart.Series.Count; index++) {
            chart.Series[index].WithMarkOptions(new ChartMarkOptions(href: "/task/" + index, current: true));
            chart.Series[index].WithRowOptions(new ChartMarkOptions(href: "/row/" + index, current: true));
        }
        var scale = new ChartStripScale(new ChartAxis { Minimum = 0, Maximum = 10 });
        var prepared = chart.PrepareStrip(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(600, 80), 0)), scale);
        var svg = XDocument.Parse(prepared.ToSvg("window"));
        Assert.DoesNotContain(svg.Descendants(), element => Role(element) == ChartDomRoles.ScheduleRow);
        var link = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("href") == "/task/0");
        Assert.DoesNotContain(svg.Descendants(), element => (string?)element.Attribute("href") == "/task/1");
        var hit = Assert.Single(link.Descendants(), element => Role(element) == "mark-link-hit-area");
        Assert.Equal(0, Number(hit, "x"));
        Assert.Equal(120, Number(hit, "width"), 6);
    }

    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
