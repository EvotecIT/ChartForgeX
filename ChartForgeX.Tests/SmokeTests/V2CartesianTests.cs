using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects Cartesian data geometry, explicit paint and immutable source identity.</summary>
public sealed class V2CartesianTests {
    [Fact]
    public void EmptySeries_ReportsNoData_AndSinglePointKeepsItsIdentity() {
        var empty = Compile(Chart.Create().AddLine("Empty", Array.Empty<ChartPoint>()));
        Assert.Contains(empty.Diagnostics, diagnostic => diagnostic.Code == "cartesian.no-data");
        var single = Compile(Chart.Create().AddLine("Single", new[] { new ChartPoint(2, 3) }));
        Assert.Single(single.Regions);
        var line = Assert.Single(single.Nodes.OfType<VisualScenePath>());
        Assert.Equal(2, line.Commands.Count);
        Assert.All(line.Commands, command => { Assert.True(double.IsFinite(command.X)); Assert.True(double.IsFinite(command.Y)); });
    }

    [Fact]
    public void MeasuredAxisGutters_StayInsideTheFrame_AndHiddenAxesRecoverTheViewport() {
        var chart = Chart.Create().AddScatter("Measured", new[] { new ChartPoint(0, 0), new ChartPoint(10, 10) })
            .WithXAxis("Observed time").WithYAxis("Measured value");
        chart.Options.XAxis.WithBounds(0, 10);
        chart.Options.YAxis.WithBounds(0, 10);
        var viewport = new ChartRect(150, 80, 330, 230);
        var context = new VisualRenderContext();
        var builder = new VisualSceneBuilder(new VisualSize(640, 400), context.Font);
        VisualCartesianCompiler.BuildInViewport(chart, context, builder, viewport);
        var visible = builder.Build();
        Assert.All(visible.Nodes.OfType<VisualSceneText>(), text => {
            Assert.True(text.X >= viewport.Left - .000001);
            Assert.True(text.X + text.Text.Metrics.Width <= viewport.Right + .000001);
            Assert.True(text.Baseline - text.Text.Ascent >= viewport.Top - .000001);
            Assert.True(text.Baseline - text.Text.Ascent + text.Text.Metrics.Height <= viewport.Bottom + .000001);
        });
        chart.WithAxes(false);
        builder = new VisualSceneBuilder(new VisualSize(640, 400), context.Font);
        VisualCartesianCompiler.BuildInViewport(chart, context, builder, viewport);
        var hidden = builder.Build().Nodes.OfType<VisualSceneEllipse>().ToArray();
        Assert.Equal(viewport.Left, hidden[0].Cx, 10);
        Assert.Equal(viewport.Bottom, hidden[0].Cy, 10);
        Assert.Equal(viewport.Right, hidden[1].Cx, 10);
        Assert.Equal(viewport.Top, hidden[1].Cy, 10);
    }

    [Fact]
    public void GroupedBars_OccupyAdjacentSlots_WhileStacksSeparatePositiveAndNegativeTotals() {
        var chart = Chart.Create().AddBar("A", Points(4, -3)).AddBar("B", Points(6, -7));
        chart.Options.YAxis.WithBounds(-10, 10);
        var grouped = Compile(chart).Nodes.OfType<VisualSceneRectangle>().Where(mark => mark.Role == "bar").ToArray();
        Assert.Equal(4, grouped.Length);
        Assert.True(grouped[0].Bounds.Right < grouped[2].Bounds.Left);
        chart.Options.BarMode = ChartBarMode.Stacked;
        var stacked = Compile(chart).Nodes.OfType<VisualSceneRectangle>().Where(mark => mark.Role == "bar").ToArray();
        Assert.Equal(stacked[0].Bounds.Left, stacked[2].Bounds.Left, 10);
        Assert.Equal(stacked[1].Bounds.Left, stacked[3].Bounds.Left, 10);
        Assert.Equal(stacked[2].Bounds.Bottom, stacked[0].Bounds.Top, 10);
        Assert.Equal(stacked[1].Bounds.Bottom, stacked[3].Bounds.Top, 10);
        Assert.Equal(4d / 6, stacked[0].Bounds.Height / stacked[2].Bounds.Height, 10);
        Assert.Equal(3d / 7, stacked[1].Bounds.Height / stacked[3].Bounds.Height, 10);
    }

    [Fact]
    public void TimeAndSecondaryLogAxes_MapRealValues_AndUseConfiguredFormatting() {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var chart = Chart.Create().AddLine("Primary", new[] { new ChartPoint(start, 0), new ChartPoint(start.AddDays(2), 100) })
            .AddScatter("Secondary", new[] { new ChartPoint(start, 1), new ChartPoint(start.AddDays(1), 10), new ChartPoint(start.AddDays(2), 100) });
        chart.Series[1].UseSecondaryYAxis();
        chart.Options.XAxis.WithTimeScale().WithBounds(start.ToOADate(), start.AddDays(2).ToOADate());
        chart.Options.YAxis.WithBounds(0, 100);
        chart.Options.SecondaryYAxis.WithScale(ChartScaleKind.Logarithmic).WithBounds(1, 100).WithLabelFormatter(value => "L" + value);
        var scene = Compile(chart);
        var markers = scene.Nodes.OfType<VisualSceneEllipse>().Where(mark => mark.Role == "marker").TakeLast(3).ToArray();
        Assert.Equal(3, markers.Length);
        Assert.True(Math.Abs((markers[0].Cx + markers[2].Cx) / 2 - markers[1].Cx) < 0.000001);
        Assert.Equal((markers[0].Cy + markers[2].Cy) / 2, markers[1].Cy, 10);
        Assert.Contains(scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "axis-secondary-y-label" && text.Text.Lines.Any(line => line.Text == "L10"));
    }

    [Fact]
    public void DisconnectedAreas_CloseEachSegmentWithoutFillingAcrossTheGap() {
        var chart = Chart.Create().AddArea("Observed", new[] {
            new ChartPoint(0, 3), new ChartPoint(1, 4), new ChartPoint(3, 6, true), new ChartPoint(4, 2)
        });
        var scene = Compile(chart);
        var areas = scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "area").ToArray();
        Assert.Equal(2, areas.Length);
        Assert.All(areas, area => Assert.True(area.Close));
        var left = areas[0].Commands.Max(command => command.X);
        var right = areas[1].Commands.Min(command => command.X);
        Assert.True(left < right);
        var line = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "line");
        Assert.Equal(2, line.Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo));
    }

    [Fact]
    public void CompiledScene_KeepsExplicitPointColorAndSourceIdentity_AfterModelMutation() {
        var color = ChartColor.FromHex("#F17A42");
        var chart = Chart.Create().AddScatter("Measured", Points(5, 8));
        chart.Series[0].WithInteractionKey("measurements").WithPointColor(0, color);
        var scene = Compile(chart);
        var svg = VisualSceneSvgRenderer.Render(scene);
        chart.Series[0].Points.Clear();
        chart.Series[0].PointColors.Clear();
        chart.Series[0].InteractionKey = "changed";
        Assert.Equal(svg, VisualSceneSvgRenderer.Render(scene));
        Assert.Equal(color, scene.Nodes.OfType<VisualSceneEllipse>().First().Fill);
        Assert.Contains(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Metadata.TryGetValue("data-cfx-series-key", out var key) && key == "measurements");
        Assert.Equal("series-0-point-0", scene.Regions[0].Id);
        Assert.Contains(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Metadata.TryGetValue("data-cfx-source-point", out var point) && point == "0");
    }

    [Fact]
    public void CrowdedDataLabels_AvoidOverlap_AndReportOmissions() {
        var chart = Chart.Create().AddScatter("Dense", Enumerable.Range(0, 30).Select(index => new ChartPoint(index, 1))).WithDataLabels();
        var scene = Compile(chart, new ChartRect(70, 30, 120, 55));
        var labels = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "data-label").ToArray();
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        for (var index = 0; index < labels.Length; index++) {
            var first = labels[index];
            var box = new ChartRect(first.X, first.Baseline - first.Text.Ascent, first.Text.Metrics.Width, first.Text.Metrics.Height);
            for (var other = index + 1; other < labels.Length; other++) {
                var second = labels[other];
                var otherBox = new ChartRect(second.X, second.Baseline - second.Text.Ascent, second.Text.Metrics.Width, second.Text.Metrics.Height);
                Assert.False(box.Left < otherBox.Right && box.Right > otherBox.Left && box.Top < otherBox.Bottom && box.Bottom > otherBox.Top);
            }
        }
    }

    [Fact]
    public void UnsupportedPreparedFeatures_ProduceActionableErrors_WithoutChangingLegacyModels() {
        var chart = Chart.Create().AddBar("Secondary stack", Points(1, 2));
        chart.Options.BarMode = ChartBarMode.Stacked;
        chart.Series[0].UseSecondaryYAxis();
        var exception = Assert.Throws<NotSupportedException>(() => Compile(chart));
        Assert.Contains("secondary-axis stacks", exception.Message);
        chart.Series[0].UsePrimaryYAxis().WithFillPattern(ChartFillPattern.DiagonalForward);
        Assert.Contains("fill patterns", Assert.Throws<NotSupportedException>(() => Compile(chart)).Message);
    }

    private static VisualScene Compile(Chart chart, ChartRect? bounds = null) {
        var context = new VisualRenderContext();
        var builder = new VisualSceneBuilder(new VisualSize(640, 400), context.Font);
        VisualCartesianCompiler.Build(chart, context, builder, bounds ?? new ChartRect(70, 40, 490, 290));
        return builder.Build();
    }

    private static IEnumerable<ChartPoint> Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value));
}
