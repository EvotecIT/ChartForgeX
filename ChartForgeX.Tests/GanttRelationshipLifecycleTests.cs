using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttRelationshipLifecycleTests {
    [Fact]
    public void MovingDuplicateNamesPreservesStableRelationshipsAndRemovalPrunesOnlyIncidentLinks() {
        var first = Task("first", 1, 3); var other = Task("other", 2, 4); var last = Task("last", 4, 6);
        var chart = Chart.Create().WithLegend(false).AddSeries(first).AddSeries(other).AddSeries(last).AddGanttDependency(first, last);
        var original = Prepare(chart); var svg = original.ToSvg(); var png = original.ToPng(new VisualRenderOptions(supersampling: 1));
        var links = chart.GanttDependencies;
        chart.MoveSeries(first, 1);
        Assert.Equal(links, chart.GanttDependencies);
        var reordered = Prepare(chart);
        AssertLink(reordered, "first", "last", "series-1", "series-2");
        Assert.Equal("1", TaskGroup(reordered, "last").Metadata["data-cfx-dependency"]);
        Assert.Equal(1, first.GanttItems[0].Start); Assert.Equal(6, last.GanttItems[0].End);
        Assert.Equal(svg, original.ToSvg()); Assert.Equal(png, original.ToPng(new VisualRenderOptions(supersampling: 1)));
        chart.MoveSeries(last, 0);
        AssertLink(Prepare(chart), "first", "last", "series-2", "series-0");
        chart.RemoveSeries(other);
        Assert.Equal(links, chart.GanttDependencies);
        AssertLink(Prepare(chart), "first", "last", "series-1", "series-0");
        chart.RemoveSeries(first);
        Assert.Empty(chart.GanttDependencies);
        var remaining = Prepare(chart);
        Assert.DoesNotContain(remaining.Scene.Nodes, node => node.Role == "gantt-dependency");
        Assert.NotEmpty(remaining.ToPng(new VisualRenderOptions(supersampling: 1)));
        chart.AddSeries(first);
        Assert.Empty(chart.GanttDependencies);
        chart.AddGanttDependency(first, last).ClearSeries().AddLine("Reused", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) });
        Assert.Empty(chart.GanttDependencies); Assert.NotEmpty(chart.ToSvg()); Assert.NotEmpty(chart.ToPng());
        Assert.Equal("first", Assert.Single(links).PredecessorId.Value);
    }

    [Fact]
    public void EmptyAttachedPeersKeepDormantRelationshipsAndRestoreCorrectSparseOrdinalArrowProjection() {
        var emptyBefore = Task("before", 0, 1).ReplaceGanttItems(Array.Empty<ChartGanttItem>());
        var first = Task("first", 1, 3);
        var emptyBetween = Task("between", 3, 5).ReplaceGanttItems(Array.Empty<ChartGanttItem>());
        var last = Task("last", 5, 7);
        var chart = Chart.Create().WithLegend(false).AddSeries(emptyBefore).AddSeries(first).AddSeries(emptyBetween).AddSeries(last)
            .AddGanttDependency(first, last).AddGanttDependency(first, emptyBetween).AddGanttDependency(emptyBetween, last);
        var links = chart.GanttDependencies;
        var sparse = Prepare(chart); var svg = sparse.ToSvg(); var png = sparse.ToPng(new VisualRenderOptions(supersampling: 1));
        Assert.Equal(new[] { "1", "3" }, sparse.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "gantt-task").Select(group => group.Metadata["data-cfx-series"]));
        AssertLink(sparse, "first", "last", "series-1", "series-3");
        var firstBounds = sparse.Regions.Single(region => region.Id == "series-1-point-0").Bounds;
        var lastBounds = sparse.Regions.Single(region => region.Id == "series-3-point-0").Bounds;
        Assert.True(lastBounds.Top > firstBounds.Top);
        first.ReplaceGanttItems(Array.Empty<ChartGanttItem>());
        var cleared = Prepare(chart);
        Assert.Equal(links, chart.GanttDependencies);
        Assert.Single(cleared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "gantt-task");
        Assert.DoesNotContain(cleared.Scene.Nodes, node => node.Role == "gantt-dependency");
        Assert.NotEmpty(cleared.ToPng(new VisualRenderOptions(supersampling: 1)));
        first.ReplaceGanttItems(new[] { new ChartGanttItem(1, 4, .7) });
        var restored = Prepare(chart);
        AssertLink(restored, "first", "last", "series-1", "series-3");
        Assert.Equal("0", TaskGroup(restored, "first").Metadata["data-cfx-source-point"]);
        Assert.Equal("1", TaskGroup(restored, "first").Metadata["data-cfx-source-count"]);
        emptyBetween.ReplaceGanttItems(new[] { new ChartGanttItem(3, 5) });
        var allActive = Prepare(chart);
        Assert.Equal(3, allActive.Scene.Nodes.Count(node => node.Role == "gantt-dependency"));
        Assert.Equal(links, chart.GanttDependencies);
        Assert.NotEmpty(allActive.ToPng(new VisualRenderOptions(supersampling: 1)));
        first.ReplaceGanttItems(Array.Empty<ChartGanttItem>()); emptyBetween.ReplaceGanttItems(Array.Empty<ChartGanttItem>()); last.ReplaceGanttItems(Array.Empty<ChartGanttItem>());
        var empty = Prepare(chart);
        Assert.Contains(empty.Diagnostics, diagnostic => diagnostic.Code == "schedule.no-data");
        Assert.Equal(links, chart.GanttDependencies);
        Assert.Equal(svg, sparse.ToSvg()); Assert.Equal(png, sparse.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    private static ChartSeries Task(string id, double start, double end) => ChartSeries.CreateGanttTask(new ChartSeriesId(id), "Repeated label", start, end, .5);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 360)), frame: new VisualFrame(showLegend: false)));
    private static VisualSceneGroup TaskGroup(PreparedVisual prepared, string id) => Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "gantt-task" && group.Metadata["data-cfx-series-key"] == id);
    private static void AssertLink(PreparedVisual prepared, string predecessor, string successor, string source, string target) {
        var link = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "gantt-dependency");
        Assert.Equal(predecessor, link.Metadata["data-cfx-predecessor-id"]); Assert.Equal(successor, link.Metadata["data-cfx-successor-id"]);
        Assert.Equal(source, link.Metadata["data-cfx-source"]); Assert.Equal(target, link.Metadata["data-cfx-target"]);
        Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-dependency-arrow");
    }
}
