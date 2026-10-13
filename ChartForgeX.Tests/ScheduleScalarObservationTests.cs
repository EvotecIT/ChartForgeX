using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ScheduleScalarObservationTests {
    [Theory]
    [InlineData(ChartSeriesKind.Gantt, "gantt-task")]
    [InlineData(ChartSeriesKind.Gauge, "gauge")]
    [InlineData(ChartSeriesKind.Circle, "circle-chart")]
    [InlineData(ChartSeriesKind.Bullet, "bullet-row")]
    [InlineData(ChartSeriesKind.LayeredRadial, "layered-radial-point")]
    public void TypedScheduleAndScalarFactsHaveOneLogicalObservationAndStableExportIdentity(ChartSeriesKind kind, string role) {
        var series = Create(kind).WithObservationId(0, new ChartObservationId("reading"));
        Assert.Equal(1, series.ObservationCount); Assert.Equal(1, series.SourcePointCount);
        Assert.Empty(series.Points); Assert.True(series.Points.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => series.Points.Add(new ChartPoint(1, 2)));
        Assert.Throws<ArgumentException>(() => new ChartSeries(new ChartSeriesId("raw"), "Raw", kind, new[] { new ChartPoint(1, 2) }));
        var chart = Chart.Create().AddSeries(series);
        var prepared = chart.Prepare(Context());
        var group = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == role);
        Assert.Equal("1", group.Metadata["data-cfx-source-count"]);
        Assert.Equal("0", group.Metadata["data-cfx-source-point"]);
        Assert.Equal("typed", group.Metadata["data-cfx-series-key"]);
        Assert.Equal("reading", group.Metadata["data-cfx-observation-id"]);
        var artifact = chart.ToVisualArtifact();
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.Clone().ToInterchangeJson());
        Assert.Equal("typed", envelope.Extensions["chart.series.0.id"]);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        series.WithPointLabel(0, "Custom").WithPointColor(0, ChartColor.FromHex("#B34C89"));
        Update(series);
        Assert.Equal("reading", series.ObservationIds[0].Value); Assert.Equal("Custom", series.PointLabels[0]); Assert.NotNull(series.PointColors[0]);
        Assert.NotEqual(svg, chart.Prepare(Context()).ToSvg());
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Clear(series);
        Assert.Equal(0, series.ObservationCount); Assert.Equal(0, series.SourcePointCount);
        Assert.Empty(series.ObservationIds); Assert.Empty(series.PointLabels); Assert.Empty(series.PointColors);
        Assert.NotEmpty(chart.Prepare(Context()).Diagnostics);
    }

    [Fact]
    public void BulletBoundariesAndRadialLayersAreDetachedFromAuthoredInputAndCountRealItems() {
        var boundaries = new[] { 80d, 20d, 120d };
        var bullet = ChartSeries.CreateBullet(new ChartSeriesId("bullet"), "Reading", new ChartBulletValue(130, 105, 0, 100, boundaries));
        boundaries[0] = 1;
        var value = Assert.Single(bullet.BulletValues);
        Assert.Equal(new[] { 80d, 20d, 120d }, value.RangeEnds);
        Assert.Throws<NotSupportedException>(() => ((IList<double>)value.RangeEnds)[0] = 1);
        var metadata = Assert.Single(Chart.Create().AddSeries(bullet).Prepare(Context()).Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "bullet-row").Metadata;
        Assert.Equal("130", metadata["data-cfx-value"]); Assert.Equal("105", metadata["data-cfx-target"]);
        Assert.Equal("80,20,120", metadata["data-cfx-source-range-ends"]);
        var first = new ChartRadialLayer("First", 20); var second = new ChartRadialLayer("Second", 80);
        var layers = new[] { first, second };
        var radial = ChartSeries.CreateLayeredRadial(new ChartSeriesId("radial"), "Layers", layers);
        first.Value = 99; layers[1] = first;
        Assert.Equal(20, radial.RadialLayers[0].Value); Assert.Equal(80, radial.RadialLayers[1].Value);
        Assert.Equal(2, radial.ObservationCount); Assert.Equal(2, radial.SourcePointCount);
        Assert.Throws<NotSupportedException>(() => ((IList<ChartRadialLayer>)radial.RadialLayers).Add(first));
        Assert.Equal(2, Chart.Create().AddSeries(radial).Prepare(Context()).Scene.Nodes.Count(node => node.Role == "layered-radial-point"));
    }

    [Fact]
    public void InvalidReplacementIsAtomicAndGanttPreservesAuthoredSchedulingFacts() {
        var series = Create(ChartSeriesKind.Gauge).WithObservationId(0, new ChartObservationId("stable"));
        Assert.Throws<ArgumentException>(() => series.ReplaceGaugeValues(new[] { new ChartGaugeValue(1), new ChartGaugeValue(2) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.ReplaceGaugeValues(new[] { default(ChartGaugeValue) }));
        Assert.Equal(40, series.GaugeValues[0].Value); Assert.Equal("stable", series.ObservationIds[0].Value);
        var start = new DateTime(2026, 3, 1, 15, 45, 12, DateTimeKind.Local);
        var task = ChartSeries.CreateGanttTask(new ChartSeriesId("task"), "Task", start, start.AddDays(2), .4);
        var marker = ChartSeries.CreateGanttMarker(new ChartSeriesId("marker"), "Marker", start, start.AddDays(3));
        var milestone = ChartSeries.CreateGanttMilestone(new ChartSeriesId("milestone"), "Milestone", start.AddDays(2));
        Assert.Equal(start.ToOADate(), task.GanttItems[0].Start); Assert.Equal(.4, task.GanttItems[0].Progress);
        Assert.Equal(start.AddDays(3).ToOADate(), marker.GanttItems[0].End); Assert.True(marker.GanttItems[0].IsVerticalMarker);
        Assert.True(milestone.GanttItems[0].IsMilestone);
        var chart = Chart.Create().AddSeries(task).AddSeries(marker).AddSeries(milestone).AddGanttDependency(task, milestone);
        Assert.Single(chart.GanttDependencies); Assert.NotEmpty(chart.Prepare(Context()).ToSvg());
        milestone.UpdateGanttItem(0, new ChartGanttItem(start.ToOADate(), start.ToOADate(), 1, isMilestone: true));
        Assert.Equal(task.Id, Assert.Single(chart.GanttDependencies).PredecessorId);
        Assert.NotEmpty(chart.Prepare(Context()).ToSvg());
    }

    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(320, 240)), VisualTheme.Graphite(), frame: new VisualFrame(showLegend: false));
    private static ChartSeries Create(ChartSeriesKind kind) {
        var id = new ChartSeriesId("typed");
        return kind switch {
            ChartSeriesKind.Gantt => ChartSeries.CreateGanttTask(id, "Task", 1, 5, .4),
            ChartSeriesKind.Gauge => ChartSeries.CreateGauge(id, "Value", 40),
            ChartSeriesKind.Circle => ChartSeries.CreateCircle(id, "Value", 40),
            ChartSeriesKind.Bullet => ChartSeries.CreateBullet(id, "Value", 40, 60, rangeEnds: new[] { 20d, 80d }),
            _ => ChartSeries.CreateLayeredRadial(id, "Layer", new[] { new ChartRadialLayer("Value", 40) })
        };
    }
    private static void Update(ChartSeries series) {
        switch (series.Kind) {
            case ChartSeriesKind.Gantt: series.UpdateGanttItem(0, new ChartGanttItem(1, 4, .8)); break;
            case ChartSeriesKind.Gauge: case ChartSeriesKind.Circle: series.UpdateGaugeValue(0, new ChartGaugeValue(75)); break;
            case ChartSeriesKind.Bullet: series.UpdateBulletValue(0, new ChartBulletValue(75, 90, rangeEnds: new[] { 60d, 80d })); break;
            default: series.UpdateRadialLayer(0, new ChartRadialLayer("Value", 75)); break;
        }
    }
    private static void Clear(ChartSeries series) {
        switch (series.Kind) {
            case ChartSeriesKind.Gantt: series.ReplaceGanttItems(Array.Empty<ChartGanttItem>()); break;
            case ChartSeriesKind.Gauge: case ChartSeriesKind.Circle: series.ReplaceGaugeValues(Array.Empty<ChartGaugeValue>()); break;
            case ChartSeriesKind.Bullet: series.ReplaceBulletValues(Array.Empty<ChartBulletValue>()); break;
            default: series.ReplaceRadialLayers(Array.Empty<ChartRadialLayer>()); break;
        }
    }
}
