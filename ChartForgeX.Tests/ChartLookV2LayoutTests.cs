using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartLookV2LayoutTests {
    private static readonly TextStyle Style = new() { FontSize = 12 };
    private static TextMetrics Measure(string text, TextStyle style) => new(10, 10, 10);

    [Fact]
    public void CrossingLeadersTryTheNextCandidateWithoutMovingEarlierLabels() {
        LabelPlacementRequest Request(ChartPoint anchor, params LabelCandidate[] candidates) => new("Complete caption", anchor, Style, candidates) {
            MeasuredSize = new TextMetrics(10, 10, 10), HasLeaderLine = true, LeaderOnFirstCandidate = true, Fallback = LabelFallbackRule.Drop
        };
        var first = Request(new ChartPoint(20, 20), new LabelCandidate(120, 70));
        var second = Request(new ChartPoint(20, 90), new LabelCandidate(120, -70), new LabelCandidate(120, 40));
        var results = new LabelPlacementService().Place(new[] { first, second }, new ChartRect(0, 0, 200, 180), null, LabelPlacementService.DefaultGap, Measure, avoidLeaderLabels: true);
        Assert.All(results, result => Assert.False(result.IsDropped));
        Assert.Equal(0, results[0].CandidateIndex); Assert.Equal(1, results[1].CandidateIndex);
        Assert.False(LabelLeaderGeometry.Intersects(results[0].LeaderStart, results[0].LeaderEnd, results[1].LeaderStart, results[1].LeaderEnd));
        Assert.Equal("Complete caption", results[1].Text);
    }

    [Fact]
    public void LeadersLeaveTheirSourceBoundaryAndAvoidOtherMarks() {
        var source = new ChartRect(10, 40, 20, 20);
        var request = new LabelPlacementRequest("Target caption", new ChartPoint(20, 50), Style,
            new[] { new LabelCandidate(120, 0), new LabelCandidate(120, -45) }) {
            MeasuredSize = new TextMetrics(10, 10, 10), AssociatedMarkId = "source", HasLeaderLine = true,
            LeaderOnFirstCandidate = true, Fallback = LabelFallbackRule.Drop
        };
        var result = Assert.Single(new LabelPlacementService().Place(new[] { request }, new ChartRect(0, 0, 200, 100),
            new[] { new LabelObstacle("source", source), new LabelObstacle("other", new LabelMarkShape(new[] { new List<ChartPoint> { new(70, 40), new(90, 40), new(90, 60), new(70, 60) } }, true, 0)) }, LabelPlacementService.DefaultGap, Measure, avoidLeaderLabels: true));
        Assert.False(result.IsDropped); Assert.Equal(1, result.CandidateIndex);
        Assert.Equal(source.Right, result.LeaderStart.X, 6);
        Assert.InRange(result.LeaderStart.Y, source.Top, source.Bottom);
        Assert.Equal(new ChartPoint(20, 50), request.Anchor);
    }

    [Theory]
    [InlineData("progress", 390, VisualThemeMode.Light)]
    [InlineData("progress", 960, VisualThemeMode.Dark)]
    [InlineData("bullet", 390, VisualThemeMode.Dark)]
    [InlineData("timeline", 960, VisualThemeMode.Light)]
    public void RowContextsFitTheirContentWhileFixedHostsKeepTheirViewport(string family, int width, VisualThemeMode mode) {
        var chart = Chart.Create().WithTitle("Quarterly delivery").WithLegend(false).WithSize(width, 560).WithTheme(VisualTheme.Graphite(), mode);
        if (family == "progress") chart.AddProgressBars("Delivery", new[] { new ChartProgressItem("North", 72), new ChartProgressItem("South", 58) });
        else if (family == "bullet") chart.AddBullet("North", 72, 80).AddBullet("South", 58, 65);
        else chart.AddTimelineRange("North", 0, 10).AddTimelineRange("South", 2, 8);
        RoundedRadialSeriesTests.UseFixtureFont(chart);
        var export = VisualExportRequest.ForChart(chart).Context;
        var context = new VisualRenderContext(new VisualLayoutOptions(export.Layout.Size, export.Layout.PaddingEdges, fitHeightToContent: true),
            export.Theme, export.ThemeMode, export.Frame, export.Font);
        var fitted = chart.Prepare(context);
        Assert.InRange(fitted.Size.Height, 100, 300);
        Assert.Equal(width, fitted.Size.Width);
        var image = PngReader.Decode(fitted.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal((int)fitted.Size.Height, image.Height); Assert.Equal(width, image.Width);
        var fixedHost = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(context.Layout.Size, context.Layout.PaddingEdges),
            context.Theme, context.ThemeMode, context.Frame, context.Font));
        Assert.Equal(560, fixedHost.Size.Height);
        var role = family == "progress" ? "progress-row" : family == "bullet" ? "bullet-row" : "timeline-item";
        var rows = fitted.Regions.Where(region => region.Role == role).ToArray();
        Assert.Equal(2, rows.Length);
        Assert.InRange(Math.Abs(rows[1].Bounds.Top - rows[0].Bounds.Top), 24, 48);
        Assert.Equal(560, chart.Options.Size.Height);
    }
}
