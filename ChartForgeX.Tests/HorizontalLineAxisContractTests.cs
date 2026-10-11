using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HorizontalLineAxisContractTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CloselySpacedContinuousCategoriesRetainTheirDomainAndVisibleSeparation(bool time, bool step) {
        var first = time ? new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc).ToOADate() : 1d;
        var last = time ? first + 1d / 86400000 : first + 1e-8;
        var chart = Model(new[] { new ChartPoint(first, 20), new ChartPoint(last, 40) }, step);
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        chart.Options.YAxis.Scale = time ? ChartScaleKind.Time : ChartScaleKind.Linear;
        var range = ChartRange.FromChart(chart);
        Assert.Equal(first, range.MinY);
        Assert.Equal(last, range.MaxY);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var points = prepared.Regions.Where(region => region.Role == "point").ToArray();
        Assert.Equal(2, points.Length);
        Assert.True(Math.Abs(points[0].Bounds.Y - points[1].Bounds.Y) > 150);
    }

    [Theory]
    [InlineData(ChartScaleKind.Linear)]
    [InlineData(ChartScaleKind.Time)]
    [InlineData(ChartScaleKind.Logarithmic)]
    public void AutomaticValueBoundsTransposeTheVerticalLineContract(ChartScaleKind scale) {
        var first = scale == ChartScaleKind.Time ? new DateTime(2026, 10, 11).ToOADate() : 20;
        var vertical = Model(new[] { new ChartPoint(2, first), new ChartPoint(8, first + 40) });
        var horizontal = Model(vertical.Series[0].Points.ToArray());
        vertical.Options.YAxis.Scale = scale;
        horizontal.Options.XAxis.Scale = scale;
        horizontal.Options.LineOrientation = ChartOrientation.Horizontal;
        var source = ChartRange.FromChart(vertical);
        var transposed = ChartRange.FromChart(horizontal);
        Assert.Equal(source.MinY, transposed.MinX);
        Assert.Equal(source.MaxY, transposed.MaxX);
        Assert.Equal(source.MinX, transposed.MinY);
        Assert.Equal(source.MaxX, transposed.MaxY);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void BarCategoryPaddingUsesDistinctRepresentableCoordinates(bool horizontal, bool time) {
        var first = time ? new DateTime(2026, 10, 11).ToOADate() : 1d;
        var last = first + (time ? 1d / 86400000 : 1e-8);
        var points = new[] { new ChartPoint(first, 20), new ChartPoint(last, 40) };
        var chart = horizontal ? Chart.Create().AddHorizontalBar("Readings", points) : Chart.Create().AddBar("Readings", points);
        (horizontal ? chart.Options.YAxis : chart.Options.XAxis).Scale = time ? ChartScaleKind.Time : ChartScaleKind.Linear;
        var range = ChartRange.FromChart(chart);
        var minimum = horizontal ? range.MinY : range.MinX; var maximum = horizontal ? range.MaxY : range.MaxX;
        Assert.True(minimum < first && maximum > last);
        Assert.True(maximum - minimum < (last - first) * 3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContinuousCategoryTicksUseTheAuthoredScaleAndTickCount(bool time) {
        var first = time ? new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc).ToOADate() : 2;
        var last = first + 6;
        var chart = Model(new[] { new ChartPoint(first + .25, 20) });
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        chart.WithYAxisBounds(first, last);
        var axis = chart.Options.YAxis;
        axis.Scale = time ? ChartScaleKind.Time : ChartScaleKind.Linear;
        axis.TickCount = 4;
        axis.WithLabelFormatter(value => value.ToString("R", CultureInfo.InvariantCulture));
        var expected = ChartTicks.ForAxis(axis, first, last).Select(value => {
            var text = value.ToString("R", CultureInfo.InvariantCulture);
            return text + " (" + text + ")";
        });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(expected, prepared.Regions.Where(region => region.Role == "axis-y-label").Select(region => region.Label));
    }

    [Theory]
    [InlineData(ChartDataLabelPlacement.Auto, 20)]
    [InlineData(ChartDataLabelPlacement.Auto, -20)]
    [InlineData(ChartDataLabelPlacement.Above, 20)]
    [InlineData(ChartDataLabelPlacement.Above, -20)]
    public void AutomaticLabelsFollowTheValueDirectionAndExplicitLabelsKeepTheirSide(ChartDataLabelPlacement placement, double value) {
        var chart = Model(new[] { new ChartPoint(1, value) }).WithXAxisBounds(-100, 100).WithYAxisBounds(0, 2);
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        chart.Options.ShowDataLabels = true;
        chart.Options.DataLabelPlacement = placement;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var point = Assert.Single(prepared.Regions, region => region.Role == "point").Bounds;
        var label = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "data-label");
        var centerX = point.X + point.Width / 2; var centerY = point.Y + point.Height / 2;
        if (placement == ChartDataLabelPlacement.Above) Assert.True(label.Baseline < centerY);
        else if (value > 0) Assert.True(label.X > centerX);
        else Assert.True(label.X + label.Text.Metrics.Width < centerX);
    }

    private static Chart Model(ChartPoint[] points, bool step = false) {
        var chart = Chart.Create().WithSize(700, 450).WithLegend(false).WithHeader(false).WithLineMarkers(ChartLineMarkerMode.None);
        return step ? chart.AddStepLine("Readings", points) : chart.AddLine("Readings", points);
    }
}
