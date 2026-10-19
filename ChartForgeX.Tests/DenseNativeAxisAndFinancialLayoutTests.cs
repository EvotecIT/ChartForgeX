using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects usable dense category allocation and automatic financial mark separation.</summary>
public sealed class DenseNativeAxisAndFinancialLayoutTests {
    [Theory]
    [InlineData(ChartSeriesKind.StepLine)]
    [InlineData(ChartSeriesKind.Area)]
    [InlineData(ChartSeriesKind.RangeArea)]
    public void LongCompleteCategoryCatalogKeepsReadableInkAndPlotInsteadOfReservingUnpaintedCaptions(ChartSeriesKind kind) {
        var chart = Named(kind);
        var source = chart.Series[0].Points.ToArray();
        var prepared = Prepare(chart);
        var plot = Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds;
        Assert.True(plot.Width > 200, "Dense named captions must not consume both capped endpoint gutters while all their ink is dropped.");
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "axis-x-label").ToArray();
        Assert.NotEmpty(labels);
        Assert.All(labels, text => { Assert.Equal(12, text.Text.Size); Assert.InRange(text.Text.Lines.Count, 1, 2); });
        Assert.Equal(labels.Length, labels.Select(text => string.Join("\n", text.Text.Lines.Select(line => line.Text))).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(48, prepared.Regions.Count(region => region.Role == "axis-x-label"));
        Assert.Equal(source, chart.Series[0].Points);
        var points = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(45, points.Length);
        Assert.Equal(Enumerable.Range(0, 48).Where(index => index is not (10 or 11 or 30)).Select(index => "observation-" + index),
            points.Select(point => (string?)point.Attribute("data-cfx-observation-id")));
        Assert.All(points, point => Assert.Contains("complete authored category description", (string?)point.Attribute("data-cfx-formatted-x")));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick, 390, 0)]
    [InlineData(ChartSeriesKind.Candlestick, 390, .5)]
    [InlineData(ChartSeriesKind.Candlestick, 390, 2)]
    [InlineData(ChartSeriesKind.Ohlc, 390, 2.2)]
    [InlineData(ChartSeriesKind.Candlestick, 960, 2)]
    public void AutomaticFinancialWidthsRespectNeighborPitchAndRetainAuthoredStroke(ChartSeriesKind kind, int width, double stroke) {
        var values = Enumerable.Range(0, 45).Select(index => new ChartCandlestick(index + 1.5, 4, 8, 2, 6)).ToArray();
        var series = kind == ChartSeriesKind.Candlestick ? ChartSeries.CreateCandlestick(new ChartSeriesId("observed"), "Observed", values)
            : ChartSeries.CreateOhlc(new ChartSeriesId("observed"), "Observed", values);
        series.ConfigureFinancial(options => {
            options.Rising.StrokeWidth = stroke; options.Falling.StrokeWidth = stroke;
            if (kind == ChartSeriesKind.Candlestick) {
                options.Rising.Wick.StrokeWidth = stroke; options.Falling.Wick.StrokeWidth = stroke;
            }
        });
        var chart = Chart.Create().WithSize(width, 360).WithHeader(false).WithLegend(false).AddSeries(series);
        var prepared = Prepare(chart);
        var marks = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
        Assert.Equal(values.Length, marks.Length);
        Assert.Equal(stroke, series.Financial.Rising.StrokeWidth);
        var bounds = prepared.Regions.Where(region => region.Role == "point").OrderBy(region => region.Bounds.Left).ToArray();
        for (var index = 1; index < bounds.Length; index++) Assert.True(bounds[index - 1].Bounds.Right < bounds[index].Bounds.Left,
            "Automatic financial bodies and strokes should leave a gap at distinct mapped observations.");
        if (width == 960) Assert.All(prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "candlestick-body"),
            body => Assert.Equal(8, body.Bounds.Width));
    }

    [Fact]
    public void AuthoredOversizedFinancialStrokeIsRetainedWhileAutomaticBodyIsBounded() {
        var chart = Chart.Create().WithSize(390, 360).WithHeader(false).WithLegend(false)
            .AddCandlestick("Observed", Enumerable.Range(0, 45).Select(index => new ChartCandlestick(index + 1.5, 4, 8, 2, 6)));
        chart.Series[0].WithStrokeWidth(8);
        var prepared = Prepare(chart);
        var bodies = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "candlestick-body").ToArray();
        Assert.Equal(45, bodies.Length);
        Assert.All(bodies, body => { Assert.Equal(8, body.StrokeWidth); Assert.InRange(body.Bounds.Width, .1, 1); });
        Assert.Equal(45, prepared.Regions.Count(region => region.Role == "point"));
    }

    private static Chart Named(ChartSeriesKind kind) {
        var rows = Enumerable.Range(0, 48).Where(index => index is not (10 or 11 or 30)).ToArray();
        var points = rows.Select(index => new ChartPoint(2.5 + index * .75, Math.Sin(index * .47) * 4 - 1.5,
            index is 12 or 31 or 39)).ToArray();
        var series = kind == ChartSeriesKind.RangeArea
            ? ChartSeries.CreateRangeArea(new ChartSeriesId("observed"), "Observed", points.Select(point =>
                new ChartRangeBand(point.X, point.Y - 1, point.Y + 1, point.BreakBefore)), smooth: false)
            : new ChartSeries(new ChartSeriesId("observed"), "Observed", kind, points);
        series.WithObservationIds(rows.Select(index => new ChartObservationId("observation-" + index)));
        series.ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 2; });
        return Chart.Create().WithSize(390, 360).WithHeader(false).WithLegend(false).WithTheme(VisualTheme.Graphite())
            .ConfigureTickLabelStyle(style => style.FontSize = 12).AddSeries(series)
            .WithXLabels(Enumerable.Range(0, 48).Select(index => new ChartAxisLabel(2.5 + index * .75,
                "Regional processing checkpoint " + index.ToString("00") + " / complete authored category description")));
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
