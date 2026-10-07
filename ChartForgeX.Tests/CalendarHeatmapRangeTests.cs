using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CalendarHeatmapRangeTests {
    [Fact]
    public void TinyObservedRange_MapsTheMaximumToHighIntensity() {
        var chart = Chart.Create().AddCalendarHeatmap("Tiny", new[] {
            new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 0),
            new ChartCalendarHeatmapItem(new DateTime(2026, 1, 6), 1e-9)
        });
        var svg = XDocument.Parse(chart.ToSvg());
        var cells = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "calendar-cell" && (string?)element.Attribute("data-cfx-empty") == "false").ToArray();
        Assert.Equal(2, cells.Length);
        Assert.Null(cells[0].Attribute("data-cfx-status"));
        Assert.Null(cells[1].Attribute("data-cfx-status"));
        Assert.Equal("0", (string)cells[0].Attribute("data-cfx-level")!);
        Assert.Equal("4", (string)cells[1].Attribute("data-cfx-level")!);
        Assert.NotEqual((string)cells[0].RenderedAttribute("fill")!, (string)cells[1].RenderedAttribute("fill")!);
        var scale = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "calendar-scale-step").ToArray();
        Assert.Equal("4", (string)scale.Last().Attribute("data-cfx-level")!);
        Assert.All(scale, step => Assert.Null(step.Attribute("data-cfx-status")));
        Assert.True(chart.ToPng().Length > 200);
    }
}
