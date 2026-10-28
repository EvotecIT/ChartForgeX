using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FunnelConnectorLifecycleTests {
    [Fact]
    public void InsertRemoveAndKeyedReplaceCarryOutgoingPaintWithItsSourceIdentity() {
        var chart = StagePresentationOptionsTests.Create(false, ChartFunnelForm.Cone, ChartOrientation.Vertical);
        var series = chart.Series[0];
        var retained = new ChartFunnelConnectorStyle(fill: ChartColor.FromHex("#B35618"), fillOpacity: .7);
        series.WithFunnelConnectorStyle(1, retained);
        series.Points.Insert(0, new ChartPoint(0, 120)); series.WithObservationId(0, new ChartObservationId("new"));
        Assert.Same(retained, series.FunnelConnectorStyles[2]); Assert.Equal("review", series.ObservationIds[2].Value);
        series.Points.RemoveAt(0);
        Assert.Same(retained, series.FunnelConnectorStyles[1]); Assert.Equal("review", series.ObservationIds[1].Value);
        series.ApplyPointChanges(new[] {
            ChartObservationChange<ChartPoint>.Remove(new ChartObservationId("intake")),
            ChartObservationChange<ChartPoint>.Replace(new ChartObservationId("review"), new ChartPoint(2, 60)),
            ChartObservationChange<ChartPoint>.Append(new ChartObservationId("next"), new ChartPoint(4, 10))
        });
        Assert.Same(retained, series.FunnelConnectorStyles[0]); Assert.Equal("review", series.ObservationIds[0].Value);
        var prepared = StagePresentationOptionsTests.Prepare(chart);
        var connection = prepared.Scene.Nodes.OfType<VisualScenePath>().First(path => path.Role == "funnel-connection-area");
        Assert.Equal(ChartColorMath.WithOpacity(retained.Fill!.Value, .7), connection.Fill);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        series.Points.Clear(); Assert.Empty(series.FunnelConnectorStyles); Assert.Empty(series.PointStrokeColors);
        series.Points.Add(new ChartPoint(1, 10)); series.Points.Add(new ChartPoint(2, 5));
        Assert.Empty(series.FunnelConnectorStyles);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void LastSourceStyleBecomesOutgoingOnAppendAndVisibilityNeverDeletesSourceFacts() {
        var chart = StagePresentationOptionsTests.Create(false, ChartFunnelForm.StageBars, ChartOrientation.Vertical);
        var series = chart.Series[0];
        series.WithFunnelConnectorStyle(2, new ChartFunnelConnectorStyle(visible: false));
        series.Points.Add(new ChartPoint(4, 0)); series.WithObservationId(3, new ChartObservationId("zero"));
        var prepared = StagePresentationOptionsTests.Prepare(chart);
        Assert.Equal(3, prepared.Scene.Nodes.OfType<VisualSceneGroup>().Count(group => group.Role == "funnel-connection"));
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualScenePath>().Count(path => path.Role == "funnel-dropoff"));
        Assert.Equal(4, prepared.Scene.Regions.Count(region => region.Role == "funnel-stage"));
        Assert.Contains("data-cfx-observation-id=\"zero\"", prepared.ToSvg());
        var last = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Last(group => group.Role == "funnel-connection");
        Assert.Equal("25", last.Metadata["data-cfx-from-value"]); Assert.Equal("0", last.Metadata["data-cfx-to-value"]);
        series.WithFunnelConnectorStyle(2, null);
        Assert.Equal(3, StagePresentationOptionsTests.Prepare(chart).Scene.Nodes.OfType<VisualScenePath>().Count(path => path.Role == "funnel-dropoff"));
    }
}
