using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects authored marker outlines through existing native stroke geometry and prepared snapshots.</summary>
public sealed class DashedMarkerOutlineTests {
    [Theory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Scatter)]
    public void DashedOutlinesUseTheNativePathAndKeepSeriesLinePaintIndependent(ChartSeriesKind kind) {
        var chart = Bare(); var points = new[] { new ChartPoint(3, 5), new ChartPoint(7, 5) };
        if (kind == ChartSeriesKind.Line) chart.AddLine("Series", points); else chart.AddScatter("Series", points);
        var dash = new[] { 5d, 3d };
        var series = chart.Series[0];
        series.ConfigureMarkers(options => {
            options.Enabled = true; options.Radius = 20; options.Stroke = ChartColor.FromRgb(255, 0, 0); options.StrokeWidth = 1;
            options.OutlineStroke = new ChartStrokeStyle(ChartColor.Black, 4, .5, dash); options.StrokeDashOffset = 2;
        });
        dash[0] = 100;
        var prepared = Prepare(chart);
        var marks = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "marker").ToArray();
        Assert.Equal(2, marks.Length);
        Assert.All(marks, mark => { Assert.Equal(new[] { 5d, 3d }, mark.Dash); Assert.Equal(2, mark.DashOffset); Assert.Equal(4, mark.StrokeWidth); Assert.Equal(128, mark.Stroke!.Value.A); });
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var elements = XDocument.Parse(svg).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "marker").ToArray();
        Assert.All(elements, element => { Assert.Equal("5 3", (string?)element.Attribute("stroke-dasharray")); Assert.Equal("2", (string?)element.Attribute("stroke-dashoffset")); });
        if (kind == ChartSeriesKind.Line) {
            var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "line");
            Assert.Null(line.Dash); Assert.Equal(chart.Options.Theme.SeriesStrokeWidth, line.StrokeWidth);
        }
        series.Markers.OutlineStroke = new ChartStrokeStyle(width: 0);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        var withoutOutline = Prepare(chart).Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "marker").ToArray();
        Assert.Equal(2, withoutOutline.Length); Assert.All(withoutOutline, mark => Assert.Null(mark.Stroke));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Markers.StrokeDashOffset = double.NaN);
    }

    [Fact]
    public void RasterOutlinesContainActualDashGapsRatherThanOnlySvgAttributes() {
        var chart = Bare().AddScatter("Glyph", new[] { new ChartPoint(5, 5) });
        chart.Series[0].ConfigureMarkers(options => {
            options.Radius = 35; options.Fill = ChartColor.Transparent;
            options.OutlineStroke = new ChartStrokeStyle(ChartColor.Black, 4, dash: new[] { 6d, 6d });
        });
        var dashed = Ink(Prepare(chart).ToPng());
        chart.Series[0].Markers.OutlineStroke = new ChartStrokeStyle(ChartColor.Black, 4);
        var solid = Ink(Prepare(chart).ToPng());
        Assert.True(dashed > 0 && dashed < solid * .8, "Native raster must preserve visible gaps in the authored marker outline.");
    }

    private static double Ink(byte[] png) => RasterImageDecoder.Decode(png).Pixels.Where((_, index) => index % 4 == 3).Sum(alpha => (double)alpha);
    private static Chart Bare() {
        var chart = Chart.Create().WithSize(400, 300).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithAxes(false).WithGrid(false).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false; chart.Options.TransparentBackground = true;
        return chart;
    }
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
