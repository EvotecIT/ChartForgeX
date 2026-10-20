using System.Reflection;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartPathFlatteningBoundsTests {
    [Theory]
    [InlineData(1e12)]
    [InlineData(1e200)]
    public void HugeFiniteCubicRetainsEndpointsWithoutAnUnboundedSubdivisionPlan(double extent) {
        RequireBoundedCurvePlan(extent);
        var path = new ChartPath(new[] {
            ChartPathCommand.MoveTo(0, 0),
            ChartPathCommand.CubicTo(extent, extent, -extent, extent, 1, 2)
        });
        var points = path.Flatten(12);
        Assert.Equal(2049, points.Count);
        Assert.Equal(new ChartPoint(0, 0), points[0]);
        Assert.Equal(new ChartPoint(1, 2), points[points.Count - 1]);
        Assert.All(points, point => { Assert.True(double.IsFinite(point.X)); Assert.True(double.IsFinite(point.Y)); });
    }

    [Theory]
    [InlineData(25, 12)]
    [InlineData(1000, 6)]
    public void OrdinaryLengthSubdivisionKeepsItsGeometryAndDisconnectedSubpaths(double segmentLength, int segments) {
        var path = new ChartPath(new[] {
            ChartPathCommand.MoveTo(0, 0), ChartPathCommand.CubicTo(0, 100, 100, 100, 100, 0),
            ChartPathCommand.MoveTo(200, 50), ChartPathCommand.LineTo(220, 50)
        });
        var points = path.Flatten(segmentLength);
        Assert.Equal(segments + 3, points.Count);
        Assert.Equal(new ChartPoint(0, 0), points[0]);
        Assert.Equal(new ChartPoint(50, 75), points[segments / 2]);
        Assert.Equal(new ChartPoint(100, 0), points[segments]);
        Assert.Equal(new ChartPoint(200, 50, true), points[segments + 1]);
        Assert.Equal(new ChartPoint(220, 50), points[segments + 2]);
    }

    // Inspect the existing private planner first so a regression fails before allocating billions of points.
    internal static void RequireBoundedCurvePlan(double extent = 1e18) {
        var planner = typeof(ChartPath).GetMethod("CubicSteps", BindingFlags.NonPublic | BindingFlags.Static)!;
        var count = (int)planner.Invoke(null, new object[] { new ChartPoint(0, 0), new ChartPoint(extent, extent),
            new ChartPoint(-extent, extent), new ChartPoint(1, 2), 12d })!;
        Assert.Equal(2048, count);
    }
}
