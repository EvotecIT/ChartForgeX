using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2GanttDependencyRoutingTests {
    [Theory]
    [InlineData(false, false, 2, 4, false)]
    [InlineData(false, false, 3, 3, false)]
    [InlineData(false, false, 5, 2, false)]
    [InlineData(false, true, 3, 3, false)]
    [InlineData(true, false, 3, 3, false)]
    [InlineData(true, true, 2, 4, false)]
    [InlineData(false, true, 3, 3, true)]
    public void FinishToStartLinksClearPaintedBarsAndDiamondsAndPointIntoTheStart(bool sourceMilestone,
        bool targetMilestone, double finish, double start, bool upward) {
        foreach (var width in new[] { 400, 960 }) {
            var source = sourceMilestone ? ChartSeries.CreateGanttMilestone(new ChartSeriesId("source"), "Source", finish)
                : ChartSeries.CreateGanttTask(new ChartSeriesId("source"), "Source", 1, finish);
            var target = targetMilestone ? ChartSeries.CreateGanttMilestone(new ChartSeriesId("target"), "Target", start)
                : ChartSeries.CreateGanttTask(new ChartSeriesId("target"), "Target", start, start + 2);
            var chart = Chart.Create().WithSize(width, 240).WithHeader(false).WithLegend(false).WithDataLabels(false);
            if (upward) chart.AddSeries(target).AddSeries(source); else chart.AddSeries(source).AddSeries(target);
            chart.AddGanttDependency(source, target);
            var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 240)))); var scene = prepared.Scene;
            var sourceBounds = scene.Regions.Single(region => region.Id == "series-" + (upward ? 1 : 0) + "-point-0").Bounds;
            var targetBounds = scene.Regions.Single(region => region.Id == "series-" + (upward ? 0 : 1) + "-point-0").Bounds;
            var route = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-dependency-line");
            var arrow = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-dependency-arrow");
            var first = route.Commands[0]; var last = route.Commands[route.Commands.Count - 1];
            Assert.True(first.X - route.StrokeWidth / 2 > sourceBounds.Right, "The painted connector must start beyond the source finish.");
            Assert.True(last.X + arrow.StrokeWidth / 2 < targetBounds.Left, "The painted arrow must stop before the target start, including a diamond's left vertex.");
            Assert.Equal(sourceBounds.Top + sourceBounds.Height / 2, first.Y, 7);
            Assert.Equal(targetBounds.Top + targetBounds.Height / 2, last.Y, 7);
            Assert.True(route.Commands[route.Commands.Count - 2].X < last.X);
            Assert.All(new[] { arrow.Commands[0], arrow.Commands[2] }, wing => Assert.True(wing.X < last.X));
            foreach (var bounds in new[] { sourceBounds, targetBounds }) {
                AssertClear(route, bounds); AssertClear(arrow, bounds);
            }
            var plot = scene.Nodes.OfType<VisualSceneGroup>().First(group => group.Clip.HasValue).Clip!.Value;
            foreach (var command in route.Commands.Concat(arrow.Commands)) {
                Assert.InRange(command.X, plot.Left + route.StrokeWidth / 2, plot.Right - route.StrokeWidth / 2);
                Assert.InRange(command.Y, plot.Top + route.StrokeWidth / 2, plot.Bottom - route.StrokeWidth / 2);
            }
            Assert.Contains("gantt-dependency-arrow", prepared.ToSvg());
        }
    }

    [Fact]
    public void ExplicitWindowsKeepAuthoredDatesAndClipConnectorsAtThePlot() {
        var chart = Chart.Create().WithSize(400, 240).WithHeader(false).WithLegend(false)
            .AddGanttTask("Source", 1, 5).AddGanttTask("Target", 3, 7);
        chart.AddGanttDependency(chart.Series[0], chart.Series[1]).WithXAxisBounds(2, 4);
        var scene = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400, 240)))).Scene;
        var schedule = Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "gantt-chart");
        Assert.Equal("2", schedule.Metadata["data-cfx-min"]); Assert.Equal("4", schedule.Metadata["data-cfx-max"]);
        var target = Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Id == "series-1-point-0");
        Assert.Equal("3", target.Metadata["data-cfx-start"]); Assert.Equal("7", target.Metadata["data-cfx-end"]);
        var activeGroups = new Stack<VisualSceneGroup>();
        ChartRect? markClip = null;
        var checkedRoute = false;
        foreach (var node in scene.Nodes) {
            if (node is VisualSceneGroup group) activeGroups.Push(group);
            else if (node is VisualSceneEndGroup) activeGroups.Pop();
            else if (node.Role == "gantt-task-shape") markClip = activeGroups.First(ancestor => ancestor.Clip.HasValue).Clip;
            else if (node is VisualScenePath route && route.Role == "gantt-dependency-line") {
                Assert.True(markClip.HasValue);
                Assert.Contains(activeGroups, ancestor => ancestor.Clip.HasValue && ancestor.Clip.Value.Equals(markClip!.Value));
                // A finish beyond the authored maximum must retain its off-plot stub and let this active clip contain it.
                Assert.Contains(route.Commands, command => command.X > markClip!.Value.Right);
                checkedRoute = true;
            }
        }
        Assert.True(checkedRoute);
    }

    private static void AssertClear(VisualScenePath path, ChartRect bounds) {
        // The bounds are the actual rendered surfaces, expanded by the connector's painted radius.
        var radius = path.StrokeWidth / 2;
        for (var index = 1; index < path.Commands.Count; index++) {
            var a = path.Commands[index - 1]; var b = path.Commands[index];
            var left = Math.Min(a.X, b.X) - radius; var right = Math.Max(a.X, b.X) + radius;
            var top = Math.Min(a.Y, b.Y) - radius; var bottom = Math.Max(a.Y, b.Y) + radius;
            Assert.False(left < bounds.Right && right > bounds.Left && top < bounds.Bottom && bottom > bounds.Top,
                "Dependency paint must not overlap its source or target surface.");
        }
    }
}
