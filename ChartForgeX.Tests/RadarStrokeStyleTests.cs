using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects shared plot/legend paint, alpha, hidden-ink geometry and detached exports.</summary>
public sealed class RadarStrokeStyleTests {
    [Fact]
    public void OutlineCallbacksDetachRetainedOptionsAndPreserveCommittedPaintAfterFailure() {
        var chart = Create(false);
        var series = chart.Series[0];
        var dash = new[] { 5d, 2 };
        var stroke = new ChartStrokeStyle(ChartColor.FromArgb(128, 220, 38, 38), 4, .5, dash);
        ChartRadarOptions? retained = null;
        series.ConfigureRadar(options => { retained = options; options.OutlineStroke = stroke; });
        var committed = Prepare(chart);
        dash[0] = 99;
        retained!.OutlineStroke = new ChartStrokeStyle(width: 0);
        Assert.Same(stroke, series.Radar.OutlineStroke);
        Assert.Equal(new[] { 5d, 2 }, series.Radar.OutlineStroke!.Dash);
        Assert.Throws<InvalidOperationException>(() => series.ConfigureRadar(options => {
            options.OutlineStroke = new ChartStrokeStyle(width: 0);
            throw new InvalidOperationException();
        }));
        Assert.Equal(committed.ToSvg(), Prepare(chart).ToSvg());
        Assert.Equal(committed.ToPng(), Prepare(chart).ToPng());
        series.ConfigureRadar(options => options.OutlineStroke = null);
        Assert.Null(series.Radar.OutlineStroke);
        Assert.NotEqual(committed.ToPng(), Prepare(chart).ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ZeroOutlineRetainsAreaMarkersAndSourceObservationsInBothProjections(bool mixed) {
        var chart = Create(mixed);
        var series = chart.Series.Last();
        series.Radar.FillOpacity = .4;
        series.Radar.OutlineStroke = new ChartStrokeStyle(width: 0);
        var hidden = Prepare(chart);
        Assert.Single(hidden.Scene.Nodes, node => node.Role == "radar-area");
        Assert.DoesNotContain(hidden.Scene.Nodes, node => node.Role == "radar-outline" || node.Role == "legend-line");
        Assert.Equal(4, hidden.Regions.Count(region => region.Role == "radar-point"));
        Assert.Equal(4, series.Points.Count);
        series.Radar.OutlineStroke = new ChartStrokeStyle(width: 1024, opacity: 0);
        var transparent = Prepare(chart);
        Assert.Equal(hidden.Regions.Where(region => region.Role == "radar-point").Select(region => region.Bounds),
            transparent.Regions.Where(region => region.Role == "radar-point").Select(region => region.Bounds));
        Assert.Equal(hidden.ToPng(), transparent.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CustomOutlinePaintAndLegendShareAlphaAndDashWithoutRecoloringTheFill(bool mixed) {
        var chart = Create(mixed);
        var series = chart.Series.Last();
        var fill = ChartColor.FromHex("#16A34A"); series.Color = fill;
        series.Radar.FillOpacity = .4;
        series.Radar.OutlineStroke = new ChartStrokeStyle(ChartColor.FromArgb(128, 220, 38, 38), 12, .5, new[] { 6d, 3 });
        var prepared = Prepare(chart);
        var outline = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "radar-outline");
        Assert.Equal(ChartColor.FromArgb(64, 220, 38, 38), outline.Stroke);
        Assert.Equal(12, outline.StrokeWidth); Assert.Equal(new[] { 6d, 3 }, outline.Dash);
        var legend = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "legend-line");
        Assert.Equal(outline.Stroke, legend.Stroke); Assert.Equal(outline.Dash, legend.Dash);
        Assert.Equal(fill.WithAlpha(102), Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "radar-area").Fill);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        series.Radar.OutlineStroke = new ChartStrokeStyle(width: 0);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.NotEqual(png, Prepare(chart).ToPng());
    }

    [Theory]
    [InlineData(ChartPolarGridShape.Circle)]
    [InlineData(ChartPolarGridShape.Polygon)]
    public void GuideFillAndDashedOutlinesRetainIndependentAlphaAndZeroStroke(ChartPolarGridShape shape) {
        var chart = Create(false).WithLegend(false).WithPolarGridShape(shape);
        chart.Options.PolarGridFill = ChartColor.FromArgb(128, 37, 99, 235);
        chart.Options.PolarGridFillOpacity = .5;
        chart.Options.PolarGridStroke = new ChartStrokeStyle(ChartColor.FromHex("#DC2626"), 0, dash: new[] { 4d, 2 });
        chart.Options.PolarAxisStroke = new ChartStrokeStyle(width: 0);
        chart.Options.PolarGridRingCount = 2;
        chart.Series[0].Radar.FillOpacity = 0;
        var prepared = Prepare(chart);
        var rings = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "radar-ring").ToArray();
        Assert.Equal(2, rings.Length);
        Assert.All(rings, ring => {
            Assert.Equal(ChartColor.FromArgb(64, 37, 99, 235), ring.Fill);
            Assert.Equal(0, ring.StrokeWidth); Assert.Equal(new[] { 4d, 2 }, ring.Dash);
        });
        var filled = prepared.ToPng(); chart.Options.PolarGridFillOpacity = 0;
        Assert.NotEqual(filled, Prepare(chart).ToPng());
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.PolarGridFillOpacity = double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.PolarGridFillOpacity = 1.1);
    }

    private static Chart Create(bool mixed) {
        var values = Enumerable.Range(1, 4).Select(index => new ChartPoint(index, 65)).ToArray();
        var chart = Chart.Create().WithSize(520, 380).WithHeader(false).WithLegend(true).WithDataLabels(false).WithYAxisBounds(0, 100);
        if (mixed) chart.AddRadialColumn("Count", values);
        chart.AddRadarArea("Target", values); chart.Series.Last().Radar.CurveTension = .25;
        return chart;
    }
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
