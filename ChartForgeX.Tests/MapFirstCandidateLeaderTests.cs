using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MapFirstCandidateLeaderTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstOffsetCalloutReservesItsLeaderWhileIncumbentFirstCandidateRemainsUnchanged(bool mapCallout) {
        var style = new TextStyle { FontSize = 10, Font = FontSpec.FromFamily("Arial, sans-serif") };
        var source = new LabelPlacementRequest("Source", new ChartPoint(20, 30), style, new[] { new LabelCandidate(30, 0, 0, .5) }, 100) {
            HasLeaderLine = true, LeaderOnFirstCandidate = mapCallout, MeasuredSize = new TextMetrics(10, 8, 8), Fallback = LabelFallbackRule.Drop
        };
        var neighbor = new LabelPlacementRequest("Neighbor", new ChartPoint(35, 30), style, new[] { new LabelCandidate(0, 0, .5, .5), new LabelCandidate(0, 20, .5, .5) }) {
            MeasuredSize = new TextMetrics(8, 8, 8), Fallback = LabelFallbackRule.Drop
        };
        var service = new LabelPlacementService();
        var placed = service.Place(new[] { source, neighbor }, new ChartRect(0, 0, 100, 80), Array.Empty<LabelObstacle>(), 0, service.Measure, avoidLeaderLabels: true);
        Assert.False(placed[0].IsDropped); Assert.False(placed[1].IsDropped);
        Assert.Equal(mapCallout, placed[0].HasLeaderLine); Assert.Equal(mapCallout ? 1 : 0, placed[1].CandidateIndex);
        if (mapCallout) {
            var leader = new LabelMarkShape(new[] { new List<ChartPoint> { source.Anchor, placed[0].LeaderEnd } }, false, 1);
            Assert.False(leader.Intersects(placed[1].Bounds));
        }
    }
}
