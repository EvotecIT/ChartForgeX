using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttMarkerSemanticTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MarkerDescriptionAndValueDescribeTheVisiblePositionWhileSchedulingRangeRemainsSeparate(bool mermaid) {
        var chart = mermaid
            ? new MermaidParser().ParseGantt("gantt\nWork :2026-01-01,8d\nDeadline :vert,2026-01-03,2d\nDeadline :vert,2026-01-06,1d").Document!.ToChart()
            : Chart.Create().AddGanttTask("Work", 1, 9).AddGanttMarker("Deadline", 4, 6).AddGanttMarker("Deadline", 7, 8);
        var expected = mermaid ? "Deadline: 2026-01-03" : "Deadline: 4";
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var regions = prepared.Regions.Where(region => region.Role == "gantt-vertical-marker").ToArray();
        Assert.Equal(expected, regions[0].Label);
        Assert.NotEqual(regions[0].Label, regions[1].Label);
        var groups = XDocument.Parse(prepared.ToSvg()).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "gantt-vertical-marker").ToArray();
        Assert.Equal(expected, (string?)groups[0].Attribute("aria-label"));
        Assert.Equal(chart.Series[1].Points[0].X.ToString("R", CultureInfo.InvariantCulture), (string?)groups[0].Attribute("data-cfx-value"));
        Assert.Equal(chart.Series[1].Points[0].Y.ToString("R", CultureInfo.InvariantCulture), (string?)groups[0].Attribute("data-cfx-end"));
    }
}
