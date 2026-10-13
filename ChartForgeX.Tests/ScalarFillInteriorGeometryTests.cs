using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ScalarFillInteriorGeometryTests {
    [Fact]
    public void FoldedThinWedgeHasAStableInteriorAwayFromItsSourceCap() {
        var contour = new List<ChartPoint> { new(0, 0), new(-.405, 1.991), new(-.227, .988) };
        var box = Find(new[] { contour }, new ChartRect(-3, -3, 6, 6));
        Assert.True(box.HasValue);
        Assert.True(box.Value.Width >= .00025);
        foreach (var point in Corners(box.Value)) {
            Assert.True(Cross(contour[0], contour[1], point) > 0);
            Assert.True(Cross(contour[1], contour[2], point) > 0);
            Assert.True(Cross(contour[2], contour[0], point) > 0);
        }
    }

    [Fact]
    public void RetracedTerminalStemHasNoPositiveInterior() {
        var contour = new List<ChartPoint> { new(0, 0), new(0, 6), new(2, 6), new(2, 10), new(0, 10) };
        Assert.Null(Find(new[] { contour }, new ChartRect(-.5, -.5, 1, 1)));
    }

    [Fact]
    public void InteriorMustStayInsideTheOriginalFootprint() {
        var contour = new List<ChartPoint> { new(0, 0), new(10, 0), new(10, 10) };
        var footprint = new ChartRect(2, 1, 1, 1);
        var box = Find(new[] { contour }, footprint);
        Assert.True(box.HasValue);
        Assert.All(Corners(box.Value), point => {
            Assert.InRange(point.X, footprint.Left, footprint.Right);
            Assert.InRange(point.Y, footprint.Top, footprint.Bottom);
            Assert.True(point.Y < point.X);
        });
        Assert.Null(Find(new[] { contour }, new ChartRect(1, 3, 1, 1)));
    }

    [Fact]
    public void SelfCrossingBandDoesNotLoseBothPositiveLobesAtItsMidpoint() {
        var contour = new List<ChartPoint> { new(0, 0), new(2, 2), new(0, 2), new(2, 0) };
        var box = Find(new[] { contour }, new ChartRect(.5, .5, 1, 1));
        Assert.True(box.HasValue);
        Assert.All(Corners(box.Value), point => Assert.True(point.Y < Math.Min(point.X, 2 - point.X)
            || point.Y > Math.Max(point.X, 2 - point.X)));
    }

    [Fact]
    public void EvenOddHolesAndDuplicateContoursDoNotBecomeInterior() {
        var outer = new List<ChartPoint> { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        var inner = new List<ChartPoint> { new(2, 2), new(8, 2), new(8, 8), new(2, 8) };
        Assert.Null(Find(new[] { outer, inner }, new ChartRect(3, 3, 1, 1)));
        Assert.Null(Find(new[] { outer, outer }, new ChartRect(3, 3, 1, 1)));
        Assert.NotNull(Find(new[] { outer, inner }, new ChartRect(0, 3, 1, 1)));
    }

    [Fact]
    public void BulkSweepPreservesFootprintIdentityAcrossDisconnectedContours() {
        var first = new List<ChartPoint> { new(0, 0), new(2, 0), new(2, 2), new(0, 2) };
        var second = first.Select(point => new ChartPoint(point.X + 100, point.Y)).ToList();
        var boxes = VisualSceneGeometry.FindFillInteriors(new[] { first, second }, new[] {
            new ChartRect(100, 0, 2, 2), new ChartRect(50, 0, 2, 2), new ChartRect(0, 0, 2, 2)
        }, .000125);
        Assert.True(boxes[0].HasValue); Assert.InRange(boxes[0]!.Value.X, 100, 102);
        Assert.Null(boxes[1]);
        Assert.True(boxes[2].HasValue); Assert.InRange(boxes[2]!.Value.X, 0, 2);
    }

    private static ChartRect? Find(IReadOnlyList<List<ChartPoint>> contours, ChartRect footprint) =>
        VisualSceneGeometry.FindFillInteriors(contours, new[] { footprint }, .000125)[0];
    private static IEnumerable<ChartPoint> Corners(ChartRect box) => new[] {
        new ChartPoint(box.Left, box.Top), new ChartPoint(box.Right, box.Top),
        new ChartPoint(box.Left, box.Bottom), new ChartPoint(box.Right, box.Bottom)
    };
    private static double Cross(ChartPoint first, ChartPoint second, ChartPoint point) =>
        (second.X - first.X) * (point.Y - first.Y) - (second.Y - first.Y) * (point.X - first.X);
}
