using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HorizontalLineTests {
    [Theory]
    [InlineData(ChartStepPosition.Start, false)]
    [InlineData(ChartStepPosition.Middle, false)]
    [InlineData(ChartStepPosition.End, false)]
    [InlineData(ChartStepPosition.Start, true)]
    [InlineData(ChartStepPosition.Middle, true)]
    [InlineData(ChartStepPosition.End, true)]
    public void StepsChangeValueAtTheDeclaredCategoryPosition(ChartStepPosition position, bool reversed) {
        var chart = Chart.Create().WithSize(640, 400).WithLegend(false)
            .AddStepLine("Steps", new[] { new ChartPoint(1, 20), new ChartPoint(3, 80) });
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        chart.Options.YAxis.Reversed = reversed;
        chart.Series[0].StepPosition = position;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "line");
        var start = line.Commands[0]; var end = line.Commands[^1];
        var transition = position == ChartStepPosition.Start ? start.Y : position == ChartStepPosition.End ? end.Y : (start.Y + end.Y) / 2;
        var horizontal = Enumerable.Range(1, line.Commands.Count - 1).Single(index => line.Commands[index].X != line.Commands[index - 1].X);
        Assert.Equal(transition, line.Commands[horizontal - 1].Y, 8);
        Assert.Equal(transition, line.Commands[horizontal].Y, 8);
        Assert.Equal(end.X, line.Commands[horizontal].X, 8);
        Assert.True(reversed ? start.Y < end.Y : start.Y > end.Y);
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(ChartScaleKind.Linear, 320, 240)]
    [InlineData(ChartScaleKind.Linear, 800, 460)]
    [InlineData(ChartScaleKind.Logarithmic, 320, 240)]
    public void AutomaticPhysicalRangesContainMarkersAndRetainSourceValues(ChartScaleKind scale, int width, int height) {
        var chart = Chart.Create().WithSize(width, height).WithLegend(false).WithLineMarkers(ChartLineMarkerMode.All)
            .AddLine("Readings", new[] { new ChartPoint(1, 20), new ChartPoint(2, 200), new ChartPoint(3, 2000) });
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        chart.Options.XAxis.Scale = scale;
        chart.Series[0].MarkerRadius = 18;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var x = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "axis-x");
        var y = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "axis-y");
        var markers = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "marker").ToArray();
        Assert.Equal(3, markers.Length);
        foreach (var marker in markers) {
            var stroke = marker.Stroke.HasValue ? marker.StrokeWidth / 2 : 0;
            Assert.True(marker.Cx - marker.Rx - stroke >= x.Start.X - 1e-7);
            Assert.True(marker.Cx + marker.Rx + stroke <= x.End.X + 1e-7);
            Assert.True(marker.Cy - marker.Ry - stroke >= y.Start.Y - 1e-7);
            Assert.True(marker.Cy + marker.Ry + stroke <= y.End.Y + 1e-7);
        }
        Assert.Equal(new[] { 20d, 200d, 2000d }, chart.Series[0].Points.Select(point => point.Y));
        Assert.Contains("data-cfx-x=\"1\"", prepared.ToSvg());
        Assert.Contains("data-cfx-y=\"20\"", prepared.ToSvg());
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LogarithmicValuesAcceptZeroAndNegativeLinearCategories(bool step) {
        var chart = Chart.Create().WithSize(540, 340).WithLegend(false);
        var points = new[] { new ChartPoint(-1, 20), new ChartPoint(0, 200), new ChartPoint(1, 2000) };
        if (step) chart.AddStepLine("Readings", points);
        else chart.AddLine("Readings", points);
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        chart.Options.XAxis.Scale = ChartScaleKind.Logarithmic;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "line");
        Assert.Contains("data-cfx-x=\"0\"", prepared.ToSvg());
        Assert.Contains("data-cfx-y=\"200\"", prepared.ToSvg());
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Fact]
    public void AddingALineDoesNotReserveASecondBarSlot() {
        Chart Model(bool mixed) {
            var chart = Chart.Create().WithSize(640, 400).WithLegend(false).WithHeader(false)
                .AddHorizontalBar("Bars", new[] { new ChartPoint(1, 20), new ChartPoint(2, 50) });
            chart.WithXAxisBounds(0, 100).WithYAxisBounds(.5, 2.5);
            if (mixed) chart.AddLine("Line", new[] { new ChartPoint(1, 30), new ChartPoint(2, 60) });
            chart.Options.LineOrientation = ChartOrientation.Horizontal;
            return chart;
        }
        var single = Model(false); var mixed = Model(true);
        var first = single.Prepare(VisualExportRequest.ForChart(single).Context).Regions.Where(region => region.Role == "point").ToArray();
        var second = mixed.Prepare(VisualExportRequest.ForChart(mixed).Context).Regions.Where(region => region.Role == "point").Take(2).ToArray();
        Assert.Equal(first.Select(region => region.Bounds), second.Select(region => region.Bounds));
    }

    [Fact]
    public void HorizontalLineConstraintsFailBeforeRenderingAnIncompatibleMix() {
        var chart = Chart.Create().AddLine("Line", new[] { new ChartPoint(1, 20) });
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.LineOrientation = (ChartOrientation)99);
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        chart.Options.YAxis.Scale = ChartScaleKind.Logarithmic;
        Assert.Throws<NotSupportedException>(() => chart.Prepare(VisualExportRequest.ForChart(chart).Context));
        chart.Options.YAxis.Scale = ChartScaleKind.Linear;
        chart.AddBar("Vertical", new[] { new ChartPoint(1, 40) });
        Assert.Throws<NotSupportedException>(() => chart.Prepare(VisualExportRequest.ForChart(chart).Context));
    }
}
