using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Finite large geometry uses the bounded spatial fallback, including saturated cells and queries.</summary>
public sealed class LabelSpatialIndexBoundsTests {
    [Theory]
    [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void SaturatedObstacleSpansRetainCollisionSemanticsWithoutEnumeratingTheirCells(bool horizontal, bool vertical) {
        var bounds = Huge(horizontal, vertical); var index = new LabelSpatialIndex(2);
        index.Add(bounds, "large-mark");
        Assert.True(index.Intersects(new ChartRect(0, 0, 2, 2), null));
        Assert.False(index.Intersects(new ChartRect(0, 0, 2, 2), "large-mark"));
    }

    [Theory]
    [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void SaturatedQueriesSearchExistingEntriesAndStillHonorLabelOverlap(bool horizontal, bool vertical) {
        var index = new LabelSpatialIndex(2); index.Add(new ChartRect(0, 0, 2, 2), null);
        Assert.True(index.Intersects(Huge(horizontal, vertical), null));
        Assert.False(index.Intersects(Huge(horizontal, vertical), null, allowLabelOverlap: true));
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void SaturatedLeaderQueriesRetainTheirActualSmallCaptionCollision(bool horizontal) {
        var index = new LabelSpatialIndex(2); index.Add(new ChartRect(-2, -2, 4, 4), null);
        var first = horizontal ? new ChartPoint(-1e200, 0) : new ChartPoint(0, -1e200);
        var second = horizontal ? new ChartPoint(1e200, 0) : new ChartPoint(0, 1e200);
        Assert.True(index.LeaderIntersectsLabel(first, second));
        Assert.False(index.LeaderIntersectsLabel(new ChartPoint(-1e200, 100), new ChartPoint(1e200, 100)));
    }

    [Fact]
    public void PublicPlacementWithNoRequestsStillIndexesFiniteOversizedObstaclesSafely() {
        var service = new LabelPlacementService(); var obstacle = new LabelObstacle("large", Huge(true, true));
        Assert.Empty(service.Place(Array.Empty<LabelPlacementRequest>(), new ChartRect(0, 0, 100, 100), new[] { obstacle }));
        var request = new LabelPlacementRequest("Caption", new ChartPoint(10, 10), new TextStyle { FontSize = 12 }, new[] { new LabelCandidate(0, 0) });
        Assert.True(Assert.Single(service.Place(new[] { request }, new ChartRect(0, 0, 100, 100), new[] { obstacle })).IsDropped);
    }

    private static ChartRect Huge(bool horizontal, bool vertical) => new(horizontal ? -1e200 : -4, vertical ? -1e200 : -4,
        horizontal ? 2e200 : 8, vertical ? 2e200 : 8);
}
