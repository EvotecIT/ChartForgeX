using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class DatelineRouteRectangleTests {
    [Fact]
    public void SharedVisibilityOwnerFindsCurveInteriorAndTangencyWithoutControlHullFalsePositives() {
        var cubic = new ChartPath(new[] { ChartPathCommand.MoveTo(-2, 2), ChartPathCommand.CubicTo(-2, -2, 2, -2, 2, 2) });
        Assert.True(cubic.IntersectsRectangle(new ChartRect(-.1, -1.1, .2, .2)));
        Assert.True(cubic.IntersectsRectangle(new ChartRect(-.1, -1, .2, .1)));
        Assert.False(cubic.IntersectsRectangle(new ChartRect(-.1, -2.1, .2, .2)));
        var line = new ChartPath(new[] { ChartPathCommand.MoveTo(-2, 0), ChartPathCommand.LineTo(2, 0) });
        Assert.True(line.IntersectsRectangle(new ChartRect(-1, -1, 2, 2))); Assert.False(line.IntersectsRectangle(new ChartRect(-1, 1, 2, 2)));
        Assert.Equal(ChartPathCommandKind.CubicTo, cubic.Commands[1].Kind); // Visibility does not replace or filter the actual exported native curve.
    }
}
