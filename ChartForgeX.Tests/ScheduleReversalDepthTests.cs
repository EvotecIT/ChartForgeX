using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ScheduleReversalDepthTests {
    public static IEnumerable<object[]> Poses() {
        foreach (var kind in new[] { ChartSeriesKind.Timeline, ChartSeriesKind.Gantt, ChartSeriesKind.StateTimeline, ChartSeriesKind.GanttLane })
            foreach (var width in new[] { 390, 900 }) foreach (var dark in new[] { false, true }) yield return new object[] { kind, width, dark };
    }

    [Theory]
    [MemberData(nameof(Poses))]
    public void BothPhysicalReversalsMirrorSourceMarksWithoutRewritingRawOrderDatesOrDetachedExports(ChartSeriesKind kind, int width, bool dark) {
        var chart = Fixture(kind, width, dark); var before = Prepare(chart); var svg = before.ToSvg(); var png = before.ToPng();
        var original = SourceRegions(before); chart.Options.XAxis.Reversed = true; chart.Options.YAxis.Reversed = true;
        var reversed = Prepare(chart); var projection = reversed.Scene.DataProjection!;
        Assert.True(projection.X.Reversed); Assert.Null(projection.Y);
        Assert.Equal(before.Scene.DataProjection!.X.Minimum, projection.X.Minimum); Assert.Equal(before.Scene.DataProjection.X.Maximum, projection.X.Maximum);
        var horizontalSum = before.Scene.DataProjection.Plot.Left + projection.Plot.Right;
        var verticalSum = before.Scene.DataProjection.Plot.Top + before.Scene.DataProjection.Plot.Bottom;
        foreach (var region in SourceRegions(reversed)) {
            var source = original[region.Key]; Assert.Equal(source.Width, region.Value.Width, 7); Assert.Equal(source.Height, region.Value.Height, 7);
            Assert.Equal(horizontalSum - source.Right, region.Value.Left, 7); Assert.Equal(verticalSum - source.Bottom, region.Value.Top, 7);
        }
        var normalFacts = XDocument.Parse(svg).Descendants().Where(node => node.Attribute("data-cfx-start") != null)
            .Select(node => ((string?)node.Attribute("data-cfx-series-key"), (string?)node.Attribute("data-cfx-start"), (string?)node.Attribute("data-cfx-end"))).ToArray();
        Assert.Equal(normalFacts, XDocument.Parse(reversed.ToSvg()).Descendants().Where(node => node.Attribute("data-cfx-start") != null)
            .Select(node => ((string?)node.Attribute("data-cfx-series-key"), (string?)node.Attribute("data-cfx-start"), (string?)node.Attribute("data-cfx-end"))));
        Assert.NotEmpty(reversed.ToPng()); Assert.Equal(svg, before.ToSvg()); Assert.Equal(png, before.ToPng());
        NumericRadialEncodedGeometryTests.Capture(chart, "schedule-reversed-" + kind + "-" + width + (dark ? "-dark" : "-light"));
        chart.Options.XAxis.Reversed = chart.Options.YAxis.Reversed = false;
        Assert.Equal(svg, Prepare(chart).ToSvg()); Assert.Equal(png, Prepare(chart).ToPng());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Timeline)]
    [InlineData(ChartSeriesKind.Gantt)]
    [InlineData(ChartSeriesKind.StateTimeline)]
    [InlineData(ChartSeriesKind.GanttLane)]
    public void PhysicalFlagsChangeOnlyTheirOwnDimensionAndCanResetIndependently(ChartSeriesKind kind) {
        var chart = Fixture(kind, 800, false); var normal = Prepare(chart); var original = SourceRegions(normal);
        chart.Options.XAxis.Reversed = true; var timeOnly = Prepare(chart);
        foreach (var region in SourceRegions(timeOnly)) {
            Assert.Equal(original[region.Key].Top, region.Value.Top, 8); Assert.Equal(original[region.Key].Height, region.Value.Height, 8);
        }
        chart.Options.XAxis.Reversed = false; chart.Options.YAxis.Reversed = true; var laneOnly = Prepare(chart);
        foreach (var region in SourceRegions(laneOnly)) {
            Assert.Equal(original[region.Key].Left, region.Value.Left, 8); Assert.Equal(original[region.Key].Width, region.Value.Width, 8);
        }
        chart.Options.YAxis.Reversed = false; Assert.Equal(normal.ToSvg(), Prepare(chart).ToSvg()); Assert.Equal(normal.ToPng(), Prepare(chart).ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReversedProgressAndDependencyArrowStayAttachedToActualStartFinishAndSuccessor(bool milestone) {
        var chart = Fixture(ChartSeriesKind.Gantt, 900, false); chart.Options.XAxis.Reversed = true;
        if (milestone) chart.Series[1].ReplaceGanttItems(new[] { new ChartGanttItem(8, 8, 1, true) });
        chart.AddGanttDependency(chart.Series[0], chart.Series[1]); var prepared = Prepare(chart);
        var task = prepared.Scene.Regions.Single(region => region.Id == "series-0-point-0").Bounds;
        var progress = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "gantt-progress");
        Assert.Equal(task.Right, progress.Bounds.Right, 8); Assert.Equal(task.Width / 2, progress.Bounds.Width, 8);
        var successor = prepared.Scene.Regions.Single(region => region.Id == "series-1-point-0").Bounds;
        var arrow = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-dependency-arrow");
        Assert.True(arrow.Commands[1].X > successor.Right); Assert.True(arrow.Commands[0].X > arrow.Commands[1].X);
        var actual = prepared.ToRgba(); var scene = prepared.Scene;
        var control = VisualSceneRasterRenderer.Render(new VisualScene(scene.Size, scene.Nodes.Where(node => node.Role != "gantt-dependency-arrow").ToArray(), scene.Diagnostics, scene.Regions));
        Assert.True(actual.Pixels.Where((value, index) => value != control.Pixels[index]).Count() > 4, "The reversed arrow must paint actual native pixels, not only metadata.");
        Assert.DoesNotContain(scene.Diagnostics, item => item.Code == "gantt.dependency-insufficient-space");
    }

    [Fact]
    public void ExplicitWindowEdgesKeepTheSameInstantFloorAndClippingUnderReversal() {
        var chart = Chart.Create().WithSize(800, 420).WithHeader(false).WithLegend(false).WithAxes(false).WithDataLabels(false)
            .WithXAxisBounds(0, 10).AddTimelineRange("Minimum instant", 0, 0).AddTimelineRange("Maximum instant", 10, 10)
            .AddTimelineRange("Clipped start", -2, 3).AddTimelineRange("Clipped end", 7, 12);
        var normal = Prepare(chart); var before = SourceRegions(normal); chart.Options.XAxis.Reversed = true;
        var reverse = Prepare(chart); var sum = normal.Scene.DataProjection!.Plot.Left + reverse.Scene.DataProjection!.Plot.Right;
        foreach (var region in SourceRegions(reverse)) {
            Assert.Equal(before[region.Key].Width, region.Value.Width, 8); Assert.Equal(sum - before[region.Key].Right, region.Value.Left, 8);
        }
        Assert.Equal(1, before["series-0-point-0"].Width, 8); Assert.Equal(0, before["series-1-point-0"].Width, 8);
        Assert.Equal(new ChartPoint(10, 10), chart.Series[1].Points[0]); Assert.NotEmpty(reverse.ToPng());
    }

    [Fact]
    public void ReversedOpenLaneEndsPointTowardTheCapturedCurrentInstantAndPackingRemainsSourceStable() {
        var chart = Fixture(ChartSeriesKind.GanttLane, 800, false); chart.WithGanttToday(7);
        var before = ChartGanttLaneModel.Build(chart, 500); chart.Options.XAxis.Reversed = true; chart.Options.YAxis.Reversed = true;
        var after = ChartGanttLaneModel.Build(chart, 500);
        Assert.Equal(before.Rows.SelectMany(row => row.Items).Select(item => (item.PointIndex, item.SubRow, item.End)), after.Rows.SelectMany(row => row.Items).Select(item => (item.PointIndex, item.SubRow, item.End)));
        var prepared = Prepare(chart); var arrow = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "gantt-lane-open-end");
        Assert.True(arrow.Commands[1].X < arrow.Commands[0].X); Assert.Equal("true", XDocument.Parse(prepared.ToSvg()).Descendants().Single(node => (string?)node.Attribute("data-cfx-open") == "true").Attribute("data-cfx-open")!.Value);
    }

    internal static Chart Fixture(ChartSeriesKind kind, int width, bool dark) {
        var chart = Chart.Create().WithSize(width, 460).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithXAxisBounds(0, 10).WithStateCategories(new ChartStateCategory("up", "Up", ChartColor.FromHex("#218A63")));
        if (kind == ChartSeriesKind.Timeline) chart.AddTimelineRange("First", 1, 4).AddTimelineRange("Second", 6, 9);
        else if (kind == ChartSeriesKind.Gantt) chart.AddGanttTask("First", 1, 4, .5).AddGanttTask("Second", 6, 9);
        else if (kind == ChartSeriesKind.StateTimeline) chart.AddStateTimelineLane("First", new[] { new ChartStateTimelineSegment(1, 4, "up") })
            .AddStateTimelineLane("Second", new[] { new ChartStateTimelineSegment(6, 9, "up") });
        else chart.AddGanttLane("First", new[] { new ChartGanttLaneItem(1, 4, "up"), new ChartGanttLaneItem(2, 5, "up") })
            .AddGanttLane("Second", new[] { new ChartGanttLaneItem(6, null, "up") }).WithGanttToday(9);
        for (var index = 0; index < chart.Series.Count; index++) chart.Series[index].WithObservationId(0, new ChartObservationId("source-" + index));
        return chart;
    }

    internal static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static Dictionary<string, ChartRect> SourceRegions(PreparedVisual visual) => visual.Scene.Regions
        .Where(region => region.Id.StartsWith("series-", StringComparison.Ordinal) && region.Role is "timeline-item" or "gantt-task" or "state-timeline-segment" or "gantt-lane-item")
        .ToDictionary(region => region.Id, region => region.Bounds);
}
