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
        var cells = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "calendar-heatmap-cell" && (string?)element.Attribute("data-cfx-empty") == "false").ToArray();
        Assert.Equal(2, cells.Length);
        Assert.Equal("negative", (string)cells[0].Attribute("data-cfx-status")!);
        Assert.Equal("positive", (string)cells[1].Attribute("data-cfx-status")!);
        Assert.NotEqual((string)cells[0].Attribute("fill")!, (string)cells[1].Attribute("fill")!);
        var scale = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "calendar-heatmap-scale-step").ToArray();
        Assert.Equal("positive", (string)scale.Last().Attribute("data-cfx-status")!);
        Assert.True(chart.ToPng().Length > 200);
    }
}
