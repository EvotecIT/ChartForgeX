using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
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

    [Theory]
    [InlineData(false, true, "none")]
    [InlineData(true, true, "none")]
    [InlineData(false, false, "none")]
    [InlineData(true, false, "none")]
    [InlineData(false, true, "middle")]
    [InlineData(true, false, "middle")]
    [InlineData(false, true, "source")]
    [InlineData(true, true, "target")]
    [InlineData(false, false, "both")]
    [InlineData(true, true, "instant-source")]
    [InlineData(false, true, "long-middle")]
    [InlineData(true, false, "long-middle")]
    public void SkipRowLinksKeepEveryPaintedSegmentClearOfInterveningTasks(bool upward, bool overlapping, string milestone) {
        var font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(font), "The existing pinned font fixture must be available before visual proof.");
        foreach (var width in new[] { 400, 960 }) {
            var finish = overlapping ? 5d : 2;
            var start = overlapping ? 3d : milestone == "long-middle" ? 4 : 5;
            var source = milestone is "source" or "both" ? ChartSeries.CreateGanttMilestone(new ChartSeriesId("source"), "Source", finish)
                : ChartSeries.CreateGanttTask(new ChartSeriesId("source"), "Source", milestone == "instant-source" ? finish : 1, finish);
            var target = milestone is "target" or "both" ? ChartSeries.CreateGanttMilestone(new ChartSeriesId("target"), "Target", start)
                : ChartSeries.CreateGanttTask(new ChartSeriesId("target"), "Target", start, start + 2);
            var middle = milestone == "middle" ? ChartSeries.CreateGanttMilestone(new ChartSeriesId("middle"), "Middle", overlapping ? 3 : 3.5)
                : ChartSeries.CreateGanttTask(new ChartSeriesId("middle"), "Middle", 2.5, milestone == "long-middle" ? 10 : 4);
            var chart = Chart.Create().WithHeader(false).WithLegend(false).WithDataLabels(false);
            chart.AddSeries(upward ? target : source).AddSeries(middle).AddSeries(upward ? source : target).AddGanttDependency(source, target);
            var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 240)),
                VisualTheme.Graphite(), upward ? VisualThemeMode.Dark : VisualThemeMode.Light, new VisualFrame("", "", showLegend: false), FontSpec.FromFile(font)));
            ScatterBubbleColorRenderingTests.Capture(prepared, $"gantt-skip-row-{upward}-{overlapping}-{milestone}-{width}");
            var scene = prepared.Scene;
            var surfaces = scene.Regions.Where(region => region.Role is "gantt-task" or "gantt-milestone").Select(region => region.Bounds).ToArray();
            Assert.Equal(3, surfaces.Length);
            var route = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "gantt-dependency-line");
            var arrow = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "gantt-dependency-arrow");
            Assert.All(surfaces, bounds => { AssertClear(route, bounds); AssertClear(arrow, bounds); });
            var sourceBounds = scene.Regions.Single(region => region.Id == "series-" + (upward ? 2 : 0) + "-point-0").Bounds;
            var targetBounds = scene.Regions.Single(region => region.Id == "series-" + (upward ? 0 : 2) + "-point-0").Bounds;
            var first = route.Commands[0]; var last = route.Commands[route.Commands.Count - 1];
            Assert.Equal(3, first.X - route.StrokeWidth / 2 - sourceBounds.Right, 6);
            Assert.Equal(3, targetBounds.Left - last.X - arrow.StrokeWidth / 2, 6);
            Assert.True(route.Commands[route.Commands.Count - 2].X < last.X);
            Assert.All(new[] { arrow.Commands[0], arrow.Commands[2] }, wing => Assert.True(wing.X < last.X));
            var plot = DependencyClip(scene);
            Assert.All(route.Commands.Concat(arrow.Commands), command => {
                Assert.InRange(command.X, plot.Left + route.StrokeWidth / 2, plot.Right - route.StrokeWidth / 2);
                Assert.InRange(command.Y, plot.Top + route.StrokeWidth / 2, plot.Bottom - route.StrokeWidth / 2);
            });
            Assert.Contains("gantt-dependency-line", prepared.ToSvg());
            AssertNativeDependencyInkClears(prepared, surfaces);
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

    [Theory]
    [InlineData(false, false, 4, false)]
    [InlineData(true, false, 4, false)]
    [InlineData(false, true, 4, false)]
    [InlineData(true, true, 4, true)]
    [InlineData(false, false, 1, false)]
    [InlineData(false, false, 0, false)]
    public void DenseLinkedRowsReserveTheDeclaredStrokeWithoutCrossingBarsOrMilestones(bool upward, bool milestones, double stroke, bool explicitWindow) {
        var prepared = DenseDependency(30, stroke, milestones, upward, explicitWindow);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"gantt-dense-{upward}-{milestones}-{stroke}-{explicitWindow}");
        var scene = prepared.Scene;
        var surfaces = scene.Regions.Where(region => region.Role is "gantt-task" or "gantt-milestone").Select(region => region.Bounds).ToArray();
        Assert.Equal(3, surfaces.Length);
        Assert.All(surfaces, bounds => Assert.Equal(Math.Min(6.5, 10 - stroke), bounds.Height, 6));
        var route = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-dependency-line");
        var arrow = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-dependency-arrow");
        Assert.All(surfaces, bounds => { AssertClear(route, bounds); AssertClear(arrow, bounds); });
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "gantt.dependency-insufficient-space");
        var target = surfaces[upward ? 0 : 2]; var last = route.Commands[route.Commands.Count - 1];
        Assert.Equal(3, target.Left - last.X - stroke / 2, 6);
        Assert.True(route.Commands[route.Commands.Count - 2].X < last.X);
        if (!explicitWindow) {
            var plot = DependencyClip(scene);
            Assert.All(route.Commands.Concat(arrow.Commands), command => {
                Assert.InRange(command.X, plot.Left + stroke / 2, plot.Right - stroke / 2);
                Assert.InRange(command.Y, plot.Top + stroke / 2, plot.Bottom - stroke / 2);
            });
        } else {
            var schedule = Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "gantt-chart");
            Assert.Equal("1", schedule.Metadata["data-cfx-min"]); Assert.Equal("5", schedule.Metadata["data-cfx-max"]);
        }
        AssertNativeDependencyInkClears(prepared, surfaces, stroke > 0);
    }

    [Theory]
    [InlineData(6, 4)]
    [InlineData(30, 10)]
    public void UncontainableDependencyStrokeRetainsLocatedEndpointFactsWithoutIntersectingPaint(int height, double stroke) {
        var prepared = DenseDependency(height, stroke);
        var scene = prepared.Scene;
        Assert.Equal(3, scene.Regions.Count(region => region.Role == "gantt-task"));
        var dependency = Assert.Single(scene.Regions, region => region.Role == "gantt-dependency");
        Assert.Equal("gantt-dependency-0-2", dependency.Id);
        Assert.Equal("Source → Target", dependency.Label);
        var metadata = Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "gantt-dependency").Metadata;
        Assert.Equal("source", metadata["data-cfx-predecessor-id"]); Assert.Equal("target", metadata["data-cfx-successor-id"]);
        Assert.Equal("insufficient-space", metadata["data-cfx-geometry-status"]);
        var diagnostic = Assert.Single(prepared.Diagnostics, value => value.Code == "gantt.dependency-insufficient-space");
        Assert.Contains(dependency.Id, diagnostic.Message); Assert.Contains("source → target", diagnostic.Message);
        Assert.DoesNotContain(scene.Nodes.OfType<VisualScenePath>(), path => path.Role is "gantt-dependency-line" or "gantt-dependency-arrow");
        Assert.Contains("data-cfx-predecessor-id=\"source\"", prepared.ToSvg());
        AssertNativeDependencyInkClears(prepared, scene.Regions.Where(region => region.Role == "gantt-task").Select(region => region.Bounds).ToArray(), false);
    }

    [Fact]
    public void ClearInteriorRowsRetainADeclaredStrokeWiderThanOneRow() {
        var source = ChartSeries.CreateGanttTask(new ChartSeriesId("source"), "Source", 1, 2);
        var target = ChartSeries.CreateGanttTask(new ChartSeriesId("target"), "Target", 5, 6);
        var chart = Chart.Create().WithAxes(false).WithHeader(false).WithLegend(false).WithGrid(false).WithDataLabels(false)
            .AddGanttTask("Other", 1, 1.5).AddSeries(source).AddGanttTask("Other", 1, 1.5).AddSeries(target).AddGanttTask("Other", 1, 1.5)
            .AddGanttDependency(source, target);
        var font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(font));
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(500, 25), 0),
            VisualTheme.Graphite().Configure(builder => builder.AxisStrokeWidth = 6), VisualThemeMode.Light,
            new VisualFrame("", "", showLegend: false), FontSpec.FromFile(font)));
        ScatterBubbleColorRenderingTests.Capture(prepared, "gantt-dense-clear-interior");
        var scene = prepared.Scene;
        var surfaces = scene.Regions.Where(region => region.Role == "gantt-task").Select(region => region.Bounds).ToArray();
        Assert.Equal(5, surfaces.Length);
        var route = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-dependency-line");
        var arrow = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-dependency-arrow");
        Assert.Equal(6, route.StrokeWidth); Assert.Equal(6, arrow.StrokeWidth);
        Assert.All(surfaces, bounds => { AssertClear(route, bounds); AssertClear(arrow, bounds); });
        var plot = DependencyClip(scene);
        Assert.All(route.Commands.Concat(arrow.Commands), command => Assert.InRange(command.Y, plot.Top + 3, plot.Bottom - 3));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "gantt.dependency-insufficient-space");
        AssertNativeDependencyInkClears(prepared, surfaces);
    }

    [Theory]
    [InlineData(6, false)]
    [InlineData(60, true)]
    public void DependenciesStraddlingAnEmptyAuthoredWindowRetainFactsAndPaintOnlyWhenTheyFit(int height, bool paints) {
        var source = ChartSeries.CreateGanttTask(new ChartSeriesId("source"), "Source", 1, 2);
        var target = ChartSeries.CreateGanttTask(new ChartSeriesId("target"), "Target", 5, 6);
        var chart = Chart.Create().WithAxes(false).WithHeader(false).WithLegend(false).WithGrid(false).WithDataLabels(false)
            .AddSeries(source).AddSeries(target).AddGanttDependency(source, target).WithXAxisBounds(3, 4);
        var font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(font));
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(500, height), 0),
            VisualTheme.Graphite().Configure(builder => builder.AxisStrokeWidth = 4), VisualThemeMode.Light,
            new VisualFrame("", "", showLegend: false), FontSpec.FromFile(font)));
        var scene = prepared.Scene;
        var schedule = Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "gantt-chart");
        Assert.Equal("3", schedule.Metadata["data-cfx-min"]); Assert.Equal("4", schedule.Metadata["data-cfx-max"]);
        Assert.DoesNotContain(scene.Nodes, node => node.Role is "gantt-task-shape" or "gantt-milestone-shape");
        var dependency = Assert.Single(scene.Regions, region => region.Role == "gantt-dependency");
        Assert.Equal("Source → Target", dependency.Label);
        var metadata = Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "gantt-dependency").Metadata;
        Assert.Equal("source", metadata["data-cfx-predecessor-id"]); Assert.Equal("target", metadata["data-cfx-successor-id"]);
        var paths = scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role is "gantt-dependency-line" or "gantt-dependency-arrow").ToArray();
        if (paints) {
            Assert.Equal(2, paths.Length);
            Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "gantt.dependency-insufficient-space");
            var plot = DependencyClip(scene);
            Assert.All(paths.SelectMany(path => path.Commands), command => Assert.InRange(command.Y, plot.Top + 2, plot.Bottom - 2));
        } else {
            Assert.Empty(paths);
            Assert.Equal("insufficient-space", metadata["data-cfx-geometry-status"]);
            var diagnostic = Assert.Single(prepared.Diagnostics, value => value.Code == "gantt.dependency-insufficient-space");
            Assert.Contains(dependency.Id, diagnostic.Message);
        }
        AssertNativeDependencyInkClears(prepared, Array.Empty<ChartRect>(), paints);
    }

    private static PreparedVisual DenseDependency(int height, double stroke, bool milestones = false, bool upward = false, bool explicitWindow = false) {
        var source = milestones ? ChartSeries.CreateGanttMilestone(new ChartSeriesId("source"), "Source", 5)
            : ChartSeries.CreateGanttTask(new ChartSeriesId("source"), "Source", 1, 5);
        var target = milestones ? ChartSeries.CreateGanttMilestone(new ChartSeriesId("target"), "Target", 3)
            : ChartSeries.CreateGanttTask(new ChartSeriesId("target"), "Target", 3, 5);
        var middle = milestones ? ChartSeries.CreateGanttMilestone(new ChartSeriesId("middle"), "Middle", 3.5)
            : ChartSeries.CreateGanttTask(new ChartSeriesId("middle"), "Middle", 2.5, 4);
        var chart = Chart.Create().WithAxes(false).WithHeader(false).WithLegend(false).WithGrid(false).WithDataLabels(false);
        chart.AddSeries(upward ? target : source).AddSeries(middle).AddSeries(upward ? source : target).AddGanttDependency(source, target);
        if (explicitWindow) chart.WithXAxisBounds(1, 5);
        var font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(font));
        return chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(500, height), 0),
            VisualTheme.Graphite().Configure(builder => builder.AxisStrokeWidth = stroke), upward ? VisualThemeMode.Dark : VisualThemeMode.Light,
            new VisualFrame("", "", showLegend: false), FontSpec.FromFile(font)));
    }

    private static void AssertClear(VisualScenePath path, ChartRect bounds) {
        // The bounds are the actual rendered surfaces, expanded by the connector's painted radius.
        var radius = path.StrokeWidth / 2;
        if (radius <= 0) return;
        for (var index = 1; index < path.Commands.Count; index++) {
            var a = path.Commands[index - 1]; var b = path.Commands[index];
            var left = Math.Min(a.X, b.X) - radius; var right = Math.Max(a.X, b.X) + radius;
            var top = Math.Min(a.Y, b.Y) - radius; var bottom = Math.Max(a.Y, b.Y) + radius;
            Assert.False(left < bounds.Right && right > bounds.Left && top < bounds.Bottom && bottom > bounds.Top,
                "Dependency paint must not overlap any task or milestone surface.");
        }
    }

    private static ChartRect DependencyClip(VisualScene scene) {
        var active = new Stack<VisualSceneGroup>();
        foreach (var node in scene.Nodes) {
            if (node is VisualSceneGroup group) active.Push(group);
            else if (node is VisualSceneEndGroup) active.Pop();
            else if (node.Role == "gantt-dependency-line") return active.First(group => group.Clip.HasValue).Clip!.Value;
        }
        throw new InvalidOperationException("The fixture must draw its dependency under the schedule's plot clip.");
    }

    private static void AssertNativeDependencyInkClears(PreparedVisual prepared, IReadOnlyList<ChartRect> surfaces, bool expectInk = true) {
        var actual = prepared.ToRgba();
        var scene = prepared.Scene;
        var control = VisualSceneRasterRenderer.Render(new VisualScene(scene.Size,
            scene.Nodes.Where(node => node.Role is not ("gantt-dependency-line" or "gantt-dependency-arrow")).ToArray(), scene.Diagnostics, scene.Regions));
        var changed = 0;
        for (var y = 0; y < actual.Height; y++)
            for (var x = 0; x < actual.Width; x++) {
                var offset = (y * actual.Width + x) * 4;
                if (Math.Abs(actual.Pixels[offset] - control.Pixels[offset]) <= 20
                    && Math.Abs(actual.Pixels[offset + 1] - control.Pixels[offset + 1]) <= 20
                    && Math.Abs(actual.Pixels[offset + 2] - control.Pixels[offset + 2]) <= 20) continue;
                changed++;
                Assert.DoesNotContain(surfaces, bounds => x + .5 > bounds.Left && x + .5 < bounds.Right && y + .5 > bounds.Top && y + .5 < bounds.Bottom);
            }
        if (expectInk) Assert.True(changed >= 10, "The native dependency must retain visible ink outside the task surfaces.");
        else Assert.Equal(0, changed);
    }
}
