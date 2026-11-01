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

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void FinancialValueAxesFitPricesWhileStackedColumnsKeepTheirBandWidth(VisualThemeMode mode) {
        var candles = Chart.Create().WithSize(920, 520).WithLegend(false).WithTheme(VisualTheme.Graphite(), mode)
            .AddCandlestick("Price", new[] { new ChartCandlestick(1, 102, 110, 98, 108), new ChartCandlestick(2, 108, 114, 104, 106), new ChartCandlestick(3, 112, 120, 108, 118) });
        var prices = candles.Prepare(VisualExportRequest.ForChart(candles).Context);
        var ticks = prices.Regions.Where(region => region.Role == "axis-y-label").Select(region => double.Parse(region.Label!.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        Assert.NotEmpty(ticks);
        Assert.InRange(ticks.Min(), 80, 98);

        var stacked = Chart.Create().WithSize(920, 520).WithTheme(VisualTheme.Graphite(), mode).WithStackedBars()
            .AddBar("Ready", new[] { new ChartPoint(1, 42), new ChartPoint(2, 58), new ChartPoint(3, 72), new ChartPoint(4, 86) })
            .AddBar("In review", new[] { new ChartPoint(1, 18), new ChartPoint(2, 22), new ChartPoint(3, 16), new ChartPoint(4, 10) });
        var columns = stacked.Prepare(VisualExportRequest.ForChart(stacked).Context);
        var widths = columns.Regions.Where(region => region.Role == "point").Select(region => region.Bounds.Width).ToArray();
        Assert.Equal(8, widths.Length);
        Assert.All(widths, width => Assert.InRange(width, 40, VisualTheme.Graphite().MaximumBarWidth + .001));
    }

    [Theory]
    [InlineData(360, VisualThemeMode.Light)]
    [InlineData(360, VisualThemeMode.Dark)]
    [InlineData(800, VisualThemeMode.Light)]
    public void GaugeTargetCaptionSitsOnItsRayClearOfTheTick(int width, VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(width, width == 360 ? 360 : 440).WithLegend(false).WithTheme(VisualTheme.Graphite(), mode)
            .AddSeries(ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Available capacity",
                new ChartGaugeValue(76, targets: new[] { new ChartGaugeTarget(85) })));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var tick = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "gauge-target");
        var caption = Assert.Single(NumericRadialAxisTextTests.Footprints(prepared.Scene), item => item.Text.Role == "gauge-target-label").Bounds;
        Assert.Equal("85", Assert.Single(prepared.Regions, region => region.Role == "gauge-target-label").Label);
        Assert.True(caption.Left >= 0 && caption.Right <= prepared.Size.Width, "The caption stays inside the canvas.");
        // Every point of the tick keeps at least a small gap from the caption box.
        for (var step = 0; step <= 20; step++) {
            var x = tick.Start.X + (tick.End.X - tick.Start.X) * step / 20; var y = tick.Start.Y + (tick.End.Y - tick.Start.Y) * step / 20;
            var dx = Math.Max(Math.Max(caption.Left - x, 0), x - caption.Right); var dy = Math.Max(Math.Max(caption.Top - y, 0), y - caption.Bottom);
            Assert.True(Math.Sqrt(dx * dx + dy * dy) >= 2, "The target tick must not touch its caption.");
        }
        // The caption centre lies on the tick's ray, beyond its outer end.
        var cx = caption.Left + caption.Width / 2; var cy = caption.Top + caption.Height / 2;
        var rx = tick.End.X - tick.Start.X; var ry = tick.End.Y - tick.Start.Y; var length = Math.Sqrt(rx * rx + ry * ry);
        Assert.InRange(Math.Abs((cx - tick.Start.X) * ry - (cy - tick.Start.Y) * rx) / length, 0, 2);
        Assert.True((cx - tick.End.X) * rx + (cy - tick.End.Y) * ry > 0);
    }
}