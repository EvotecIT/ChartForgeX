using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartLookV2RegressionTests {
    [Fact]
    public void FittedPictorialRetainsAllRequestedUnitSymbols() {
        var chart = Chart.Create().WithSize(800, 560).WithLegend(false)
            .AddPictorial("Capacity", new[] { new ChartPictorialItem("North", 100) })
            .WithPictorialColumns(5).WithPictorialValuePerSymbol(1);
        var prepared = Fit(chart);
        var symbols = XDocument.Parse(prepared.ToSvg()).Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == "pictorial-symbol");
        Assert.Equal(100, symbols.Count());
        Assert.DoesNotContain(prepared.Diagnostics, item => item.Code == "pictorial.overflow");
    }

    [Fact]
    public void FittedBottomLegendPreservesRowPitchAndItsTitle() {
        var chart = Chart.Create().WithSize(600, 560).WithTitle("Delivery")
            .AddProgressBars("Delivery", new[] { new ChartProgressItem("North", 72), new ChartProgressItem("South", 58) });
        chart.Options.LegendPosition = ChartLegendPosition.Bottom;
        var prepared = Fit(chart, new VisualFrame(showLegend: true, legendPosition: ChartLegendPosition.Bottom, legendTitle: "Current delivery"));
        var rows = prepared.Regions.Where(item => item.Role == "progress-row").ToArray();
        Assert.Equal(2, rows.Length);
        Assert.InRange(rows[1].Bounds.Top - rows[0].Bounds.Top, 28, 40);
        Assert.Contains(prepared.Regions, item => item.Role == "legend-title" && item.Label == "Current delivery");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FinancialPricesFitTheirOwnAxisBesideNonFinancialObservations(bool secondary) {
        var chart = Chart.Create().WithSize(800, 440).WithLegend(false)
            .AddCandlestick("Price", new[] { new ChartCandlestick(1, 102, 110, 98, 108), new ChartCandlestick(2, 108, 114, 104, 106) });
        if (secondary) chart.Series[0].UseSecondaryValueAxis();
        chart.AddLine("Count", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) });
        if (!secondary) chart.Series[1].UseSecondaryValueAxis();
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var role = secondary ? "axis-secondary-y-label" : "axis-y-label";
        var ticks = prepared.Regions.Where(item => item.Role == role).Select(item => double.Parse(item.Label!.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        Assert.NotEmpty(ticks);
        Assert.InRange(ticks.Min(), 80, 98);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void SourceContourCannotRejectItsOwnOutwardLeader(double stroke) {
        var shape = new LabelMarkShape(new[] { new List<ChartPoint> { new(10, 10), new(30, 10), new(30, 30), new(10, 30) } }, true, stroke);
        var request = new LabelPlacementRequest("Caption", new ChartPoint(20, 20), new() { FontSize = 12 }, new[] { new LabelCandidate(70, 0) }) {
            MeasuredSize = new(20, 12, 10), AssociatedMarkId = "source", HasLeaderLine = true, LeaderOnFirstCandidate = true, Fallback = LabelFallbackRule.Drop
        };
        var result = Assert.Single(new LabelPlacementService().Place(new[] { request }, new ChartRect(0, 0, 160, 80),
            new[] { new LabelObstacle("source", shape) }, 2, (_, _) => new(20, 12, 10), avoidLeaderLabels: true));
        Assert.False(result.IsDropped);
        Assert.Equal(30, result.LeaderStart.X, 6);
    }

    [Fact]
    public void LongTargetCaptionDoesNotRemoveTheGaugeArc() {
        var chart = Chart.Create().WithSize(360, 360).WithLegend(false)
            .AddSeries(ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Capacity",
                new ChartGaugeValue(76, targets: new[] { new ChartGaugeTarget(85, new string('W', 100)) })));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.DoesNotContain(prepared.Diagnostics, item => item.Code == "gauge.insufficient-space");
        Assert.Contains(prepared.Scene.Nodes, item => item.Role == "gauge-value");
    }

    [Fact]
    public void TransparentDifferenceStrokeRemainsTransparentOnBothBoundaries() {
        var chart = Chart.Create().WithSize(600, 380).WithLegend(false)
            .AddDifferenceArea("Pair", new[] { new ChartDifferencePoint(0, 8, 2), new ChartDifferencePoint(1, 2, 8) });
        chart.Series[0].WithColor(ChartColor.Transparent).ConfigureDifferenceArea(options => options.NegativeFill = ChartColor.FromHex("#305CDF"));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var boundaries = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(item => item.Role is "difference-first" or "difference-second").ToArray();
        Assert.Equal(2, boundaries.Length);
        Assert.All(boundaries, item => Assert.Equal(0, item.Stroke!.Value.A));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(20)]
    public void DottedMapCalloutStartsAtItsResolvedHaloWithoutASecondRadiusInset(double radius) {
        var chart = Chart.Create().WithSize(800, 440).WithLegend(false).WithDataLabels()
            .AddDottedMap("Sites", new[] { new ChartMapPoint("Headquarters", 0, 0) });
        chart.Series[0].WithMarkerRadius(radius);
        chart.Options.DataLabelPlacement = ChartDataLabelPlacement.Right;
        chart.Options.MapCallouts = new ChartMapCallouts(100);
        RoundedRadialSeriesTests.UseFixtureFont(chart);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var halo = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneEllipse>(), item => item.Role == "dotted-map-point-halo");
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), item => item.Role == "dotted-map-label-leader");
        Assert.Equal(halo.Cx + halo.Rx, line.Start.X, 5);
        Assert.Equal(halo.Cy, line.Start.Y, 5);
        Assert.True(line.End.X > line.Start.X);
    }

    [Fact]
    public void AForeignRectangleOverlappingTheSourceExitStillBlocksTheLeader() {
        var request = new LabelPlacementRequest("Caption", new ChartPoint(20, 50), new() { FontSize = 12 }, new[] { new LabelCandidate(120, 0) }) {
            MeasuredSize = new(10, 10, 10), AssociatedMarkId = "source", HasLeaderLine = true, LeaderOnFirstCandidate = true, Fallback = LabelFallbackRule.Drop
        };
        var result = Assert.Single(new LabelPlacementService().Place(new[] { request }, new ChartRect(0, 0, 200, 100),
            new[] { new LabelObstacle("source", new ChartRect(10, 40, 20, 20)), new LabelObstacle("foreign", new ChartRect(25, 40, 20, 20)) },
            2, (_, _) => new(10, 10, 10), avoidLeaderLabels: true));
        Assert.True(result.IsDropped);
    }

    private static PreparedVisual Fit(Chart chart, VisualFrame? frame = null) {
        RoundedRadialSeriesTests.UseFixtureFont(chart);
        var context = VisualExportRequest.ForChart(chart).Context;
        return chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(context.Layout.Size, context.Layout.PaddingEdges, fitHeightToContent: true),
            context.Theme, context.ThemeMode, frame ?? context.Frame, context.Font));
    }
}
