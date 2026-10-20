using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Financial tuples and paired boundaries keep source facts while a native X window changes their paint.</summary>
public sealed class ChartXViewportExtendedFamilyTests {
    [Theory]
    [InlineData(ChartSeriesKind.Candlestick, false)]
    [InlineData(ChartSeriesKind.Ohlc, true)]
    [InlineData(ChartSeriesKind.RangeBand, true)]
    [InlineData(ChartSeriesKind.RangeArea, false)]
    [InlineData(ChartSeriesKind.DifferenceArea, true)]
    public void NumericAndTimeWindowsKeepTupleIdentityAndFullValueDomain(ChartSeriesKind kind, bool reversed) {
        foreach (var time in new[] { false, true }) {
            var origin = time ? new DateTime(2026, 1, 28, 0, 0, 0, DateTimeKind.Utc).ToOADate() : 0;
            var chart = Create(kind, origin).WithXAxisBounds(origin, origin + 10);
            if (time) chart.WithXAxisScale(ChartScaleKind.Time);
            chart.Options.XAxis.Reversed = reversed;
            var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 380)), VisualTheme.Graphite());
            var full = chart.Prepare(context);
            var window = chart.Prepare(context, new ChartXViewport(origin + 4, origin + 6));
            var projection = Assert.IsType<ChartXProjection>(window.XProjection);
            Assert.Equal(origin, projection.FullMinimum); Assert.Equal(origin + 10, projection.FullMaximum);
            Assert.Equal(origin + 4, projection.Minimum); Assert.Equal(origin + 6, projection.Maximum);
            Assert.Equal(reversed, projection.Reversed);
            Assert.Equal(time ? ChartScaleKind.Time : ChartScaleKind.Linear, projection.Scale);
            Assert.Equal(AxisText(full, "axis-y-label"), AxisText(window, "axis-y-label"));
            Assert.NotEqual(AxisText(full, "axis-x-label"), AxisText(window, "axis-x-label"));
            var observations = XDocument.Parse(window.ToSvg()).Descendants()
                .Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
            Assert.Equal(3, observations.Length);
            Assert.Equal(new[] { "tuple-0", "tuple-1", "tuple-2" }, observations.Select(node => (string?)node.Attribute("data-cfx-observation-id")));
            Assert.Equal(new[] { "false", null, "false" }, observations.Select(node => (string?)node.Attribute("data-cfx-visible")));
            Assert.Empty(observations[0].Elements()); Assert.Empty(observations[2].Elements());
            Assert.NotEmpty(observations[1].Elements());
            Assert.Equal(kind is ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc ? "open" : kind == ChartSeriesKind.DifferenceArea ? "first" : "lower",
                observations[1].Attributes().First(attribute => attribute.Name.LocalName is "data-cfx-open" or "data-cfx-first" or "data-cfx-lower").Name.LocalName.Substring(9));
            Assert.NotEmpty(window.ToPng());
            Assert.Equal(origin, chart.Options.XAxis.Minimum); Assert.Equal(origin + 10, chart.Options.XAxis.Maximum);
            Assert.Equal(3, chart.Series[0].SourcePointCount);
        }
    }

    [Theory]
    [InlineData(ChartSeriesKind.RangeBand, "range-band")]
    [InlineData(ChartSeriesKind.RangeArea, "range-area")]
    [InlineData(ChartSeriesKind.DifferenceArea, "difference-positive")]
    public void WindowBetweenPairsKeepsNativeCrossingGeometryWithoutInteractiveEndpoints(ChartSeriesKind kind, string role) {
        var chart = Create(kind, 0).WithXAxisBounds(0, 10);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context, new ChartXViewport(1, 2));
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == role && path.Commands.Count > 3);
        var observations = XDocument.Parse(prepared.ToSvg()).Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(3, observations.Length);
        Assert.All(observations, node => { Assert.Equal("false", (string?)node.Attribute("data-cfx-visible")); Assert.Empty(node.Elements()); });
        Assert.NotEmpty(prepared.ToRgba().Pixels);
    }

    private static Chart Create(ChartSeriesKind kind, double origin) {
        var coordinates = new[] { origin, origin + 5, origin + 10 };
        var id = new ChartSeriesId("tuples");
        var series = kind switch {
            ChartSeriesKind.Candlestick => ChartSeries.CreateCandlestick(id, "Tuples", coordinates.Select((x, index) => new ChartCandlestick(x, 20 + index, 50 + index, -20 - index, 35 + index))),
            ChartSeriesKind.Ohlc => ChartSeries.CreateOhlc(id, "Tuples", coordinates.Select((x, index) => new ChartCandlestick(x, 20 + index, 50 + index, -20 - index, 35 + index))),
            ChartSeriesKind.RangeBand => ChartSeries.CreateRangeBand(id, "Tuples", coordinates.Select((x, index) => new ChartRangeBand(x, -20 - index, 50 + index))),
            ChartSeriesKind.RangeArea => ChartSeries.CreateRangeArea(id, "Tuples", coordinates.Select((x, index) => new ChartRangeBand(x, -20 - index, 50 + index))),
            _ => ChartSeries.CreateDifferenceArea(id, "Tuples", coordinates.Select((x, index) => new ChartDifferencePoint(x, 50 + index, -20 - index)))
        };
        if (kind is not (ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc)) series.ConfigureMarkers(markers => markers.Enabled = true);
        for (var index = 0; index < 3; index++) series.WithObservationId(index, new ChartObservationId("tuple-" + index));
        return Chart.Create().WithLegend(false).AddSeries(series);
    }

    [Fact]
    public void DenseFinancialWindowUsesVisibleNativeSlotsForReadableBodies() {
        var chart = Chart.Create().WithLegend(false).WithXAxisBounds(0, 99).AddCandlestick("Daily",
            Enumerable.Range(0, 100).Select(index => new ChartCandlestick(index, 20, 40, 10, 30)));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 380)), VisualTheme.Graphite());
        var full = chart.Prepare(context);
        var window = chart.Prepare(context, new ChartXViewport(40, 42));
        var fullBodies = XDocument.Parse(full.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "candlestick-body").ToArray();
        var bodies = XDocument.Parse(window.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "candlestick-body").ToArray();
        Assert.Equal(100, fullBodies.Length); Assert.Equal(3, bodies.Length);
        double Width(XElement body) => double.Parse(body.Attribute("width")!.Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.All(bodies, body => Assert.True(Width(body) > Width(fullBodies[40])));
        Assert.All(bodies, body => Assert.InRange(Width(body), 8, 22));
        Assert.Equal(100, chart.Series[0].SourcePointCount);
        Assert.Equal(AxisText(full, "axis-y-label"), AxisText(window, "axis-y-label"));
    }

    private static string?[] AxisText(PreparedVisual prepared, string role) => prepared.Regions
        .Where(region => region.Role == role).Select(region => region.Label).ToArray();
}
