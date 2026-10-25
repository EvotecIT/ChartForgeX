using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Whole-coordinate rotation preserves source direction/reversal and never repurposes Cartesian physical windows.</summary>
public sealed class PolarOrientationDepthTests {
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void RadarAndPolarMarksAndSpokesRotateTogetherWithoutChangingSourceFacts(bool radar, bool reverse) {
        var chart = Chart.Create().WithSize(600, 420).WithHeader(false).WithLegend(false).WithDataLabels(false).WithAxes(false).WithYAxisBounds(0, 100);
        var points = Enumerable.Range(0, 4).Select(index => new ChartPoint(radar ? index + 1 : index * Math.PI / 2, 80)).ToArray();
        if (radar) chart.AddRadarLine("Signal", points); else chart.AddPolar("Signal", points);
        chart.Options.XAxis.Reversed = reverse;
        var before = PolarDepthContractTests.Prepare(chart); var old = Centres(before); var cx = old.Average(point => point.X); var cy = old.Average(point => point.Y);
        chart.Options.PolarRotationDegrees = 90; var after = PolarDepthContractTests.Prepare(chart); var rotated = Centres(after);
        for (var index = 0; index < old.Length; index++) {
            Assert.Equal(cx - (old[index].Y - cy), rotated[index].X, 7); Assert.Equal(cy + (old[index].X - cx), rotated[index].Y, 7);
        }
        var spokes = after.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role is "radar-spoke" or "polar-spoke").ToArray();
        Assert.NotEmpty(spokes);
        Assert.All(spokes, line => { Assert.Equal(cx, line.Start.X, 7); Assert.Equal(cy, line.Start.Y, 7); });
        Assert.Equal(points, chart.Series[0].Points); Assert.Equal(4, rotated.Length);
        Assert.Equal(before.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role is "radar-point-source" or "polar-point-source").Select(group => group.Metadata["data-cfx-value"]),
            after.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role is "radar-point-source" or "polar-point-source").Select(group => group.Metadata["data-cfx-value"]));
    }

    [Fact]
    public void PolarAreaSlotsUseTheSameRotationWhileAllRawValuesRemainExact() {
        var chart = Chart.Create().WithHeader(false).WithLegend(false).WithDataLabels(false)
            .AddPolarArea("Parts", new[] { new ChartPoint(1, 20), new ChartPoint(2, 0), new ChartPoint(3, 80) });
        var before = PolarDepthContractTests.Prepare(chart); chart.Options.PolarRotationDegrees = 90;
        var after = PolarDepthContractTests.Prepare(chart);
        var old = before.Scene.Nodes.OfType<VisualSceneSlice>().ToArray(); var actual = after.Scene.Nodes.OfType<VisualSceneSlice>().ToArray();
        Assert.Equal(3, actual.Length);
        for (var index = 0; index < old.Length; index++) { Assert.Equal(old[index].Start + Math.PI / 2, actual[index].Start, 12); Assert.Equal(old[index].Sweep, actual[index].Sweep); }
        Assert.Equal(new[] { "20", "0", "80" }, after.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "polar-area-point-source").Select(group => group.Metadata["data-cfx-value"]));
    }

    [Fact]
    public void FiniteRotationRetainsAuthoredDegreesAndRejectsForeignPhysicalOwners() {
        var chart = PolarDepthContractTests.Fixture();
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.PolarRotationDegrees = double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.PolarRotationDegrees = double.PositiveInfinity);
        chart.Options.PolarRotationDegrees = 450; var fullTurn = PolarDepthContractTests.Prepare(chart);
        Assert.Equal(450, chart.Options.PolarRotationDegrees); chart.Options.PolarRotationDegrees = 90;
        Assert.Equal(fullTurn.ToPng(), PolarDepthContractTests.Prepare(chart).ToPng());
        var foreign = Chart.Create().AddLine("Signal", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2) }); foreign.Options.PolarRotationDegrees = 90;
        Assert.Throws<NotSupportedException>(() => PolarDepthContractTests.Prepare(foreign));
        var mixed = Chart.Create().AddRadialColumn("Counts", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2), new ChartPoint(3, 3) })
            .AddRadarLine("Target", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2), new ChartPoint(3, 3) });
        mixed.Options.PolarRotationDegrees = 90; Assert.Throws<NotSupportedException>(() => PolarDepthContractTests.Prepare(mixed));
    }

    private static ChartPoint[] Centres(PreparedVisual prepared) => prepared.Regions.Where(region => region.Role is "radar-point" or "polar-point")
        .Select(region => new ChartPoint(region.Bounds.Left + region.Bounds.Width / 2, region.Bounds.Top + region.Bounds.Height / 2)).ToArray();
}
