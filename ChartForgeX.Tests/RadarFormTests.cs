using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects explicit radar forms without changing source category alignment or missing-value policy.</summary>
public sealed class RadarFormTests {
    [Fact]
    public void RadarHelpersUseOneSeriesKindAndValidateFormAndAreaOnlyPaint() {
        var points = new[] { new ChartPoint(1, 50), new ChartPoint(2, 50), new ChartPoint(3, 50) };
        var chart = Chart.Create().AddRadar("Default", points).AddRadarLine("Line", points).AddRadarArea("Area", points);
        Assert.All(chart.Series, series => Assert.Equal(ChartSeriesKind.Radar, series.Kind));
        Assert.Equal(new[] { ChartRadarForm.Area, ChartRadarForm.Line, ChartRadarForm.Area }, chart.Series.Select(series => series.Radar.Form));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Series[0].Radar.Form = (ChartRadarForm)99);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Series[0].Radar.FillOpacity = double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Series[0].Radar.FillOpacity = 1.1);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Series[0].Radar.FillOpacity = -.1);
        Assert.Throws<ArgumentNullException>(() => chart.Series[0].WithRadar(null!));
        chart.Series[1].Radar.FillOpacity = .4;
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        var scatter = Chart.Create().AddScatter("Samples", points);
        scatter.Series[0].Radar.Form = ChartRadarForm.Line;
        Assert.Throws<InvalidOperationException>(() => Prepare(scatter));
    }

    [Theory]
    [InlineData(ChartRadarForm.Line, 0)]
    [InlineData(ChartRadarForm.Area, 102)]
    public void ExplicitFormsPaintTheSameClosedOutlineButOnlyAreaFillsItsNativeInterior(ChartRadarForm form, int alpha) {
        var chart = Bare().AddRadar("Signal", SixValues(50), ChartColor.FromHex("#2468AC"));
        chart.Series[0].Radar.Form = form;
        chart.Series[0].Markers.Enabled = false;
        if (form == ChartRadarForm.Area) chart.Series[0].Radar.FillOpacity = .4;
        var prepared = Prepare(chart);
        var outline = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "radar-outline");
        Assert.True(outline.Close);
        Assert.Equal(form == ChartRadarForm.Area ? 1 : 0, prepared.Scene.Nodes.Count(node => node.Role == "radar-area"));
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "radar-point");
        Assert.Equal(6, prepared.Regions.Count(region => region.Role == "radar-point"));
        var centre = new ChartPoint((outline.Commands.Min(command => command.X) + outline.Commands.Max(command => command.X)) / 2,
            (outline.Commands.Min(command => command.Y) + outline.Commands.Max(command => command.Y)) / 2);
        var image = RasterImageDecoder.Decode(prepared.ToPng());
        var pixel = image.Pixels.Skip(((int)centre.Y * image.Width + (int)centre.X) * 4).Take(4).ToArray();
        Assert.Equal(alpha, pixel[3]);
        if (alpha > 0) {
            var fill = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "radar-area").Fill!.Value;
            Assert.Equal(ChartColor.FromArgb(102, 36, 104, 172), fill);
            // Native transparent buffers quantize premultiplied RGB before PNG unpremultiplies it.
            var expected = new byte[] { fill.R, fill.G, fill.B };
            for (var index = 0; index < expected.Length; index++) Assert.InRange(Math.Abs(pixel[index] - expected[index]), 0, 1);
        }
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(form == ChartRadarForm.Area ? 1 : 0, svg.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "radar-area"));
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-form") == form.ToString().ToLowerInvariant());
    }

    [Fact]
    public void MixedFormsShareCategoryAndValueGeometryAndKeepMissingZeroDistinctFromAuthoredZero() {
        var color = ChartColor.FromHex("#2468AC");
        var chart = Bare().AddRadarArea("Area", new[] { new ChartPoint(3, 80), new ChartPoint(1, 0), new ChartPoint(2, 50) }, color)
            .AddRadarLine("Line", new[] { new ChartPoint(1, 0), new ChartPoint(3, 80), new ChartPoint(4, 20) }, color);
        chart.Series[0].WithMarkers(marker => { marker.Shape = ChartMarkerShape.Star; marker.Radius = 6; });
        chart.Series[0].WithPointColor(0, ChartColor.FromHex("#AB1234")).WithPointLabel(0, "Complete source label");
        var prepared = Prepare(chart);
        Assert.Single(prepared.Scene.Nodes, node => node.Role == "radar-area");
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "radar-outline"));
        var source = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Id == "series-0-point-0");
        Assert.Equal("3", source.Metadata["data-cfx-category"]); Assert.Equal("80", source.Metadata["data-cfx-value"]);
        Assert.Equal("Complete source label", source.Metadata["data-cfx-full-label"]);
        var authoredZero = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Id == "series-1-point-0");
        Assert.Equal("false", authoredZero.Metadata["data-cfx-missing"]); Assert.Equal("0", authoredZero.Metadata["data-cfx-value"]);
        var missing = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Id == "series-1-missing-category-1");
        Assert.Equal("-1", missing.Metadata["data-cfx-point"]); Assert.Equal("true", missing.Metadata["data-cfx-missing"]);
        Assert.Equal("0", missing.Metadata["data-cfx-value"]);
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "radar-series"), group => Assert.Equal("zero", group.Metadata["data-cfx-missing-policy"]));
        var sameAreaPoint = Assert.Single(prepared.Regions, region => region.Id == "series-0-point-0").Bounds;
        var sameLinePoint = Assert.Single(prepared.Regions, region => region.Id == "series-1-point-1").Bounds;
        Assert.Equal(sameAreaPoint.Left + sameAreaPoint.Width / 2, sameLinePoint.Left + sameLinePoint.Width / 2, 7);
        Assert.Equal(sameAreaPoint.Top + sameAreaPoint.Height / 2, sameLinePoint.Top + sameLinePoint.Height / 2, 7);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Series[0].Radar.Form = ChartRadarForm.Line; chart.Series[0].Points.Clear();
        chart.Series[1].Markers.Shape = ChartMarkerShape.Pin;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void ZeroOpacityRemovesAreaInkIncludingPatternWhileKeepingOutlineAndSourceFacts() {
        var chart = Bare().AddRadarArea("Signal", SixValues(50));
        chart.Series[0].WithRadar(radar => radar.FillOpacity = 0);
        chart.Series[0].FillPattern = ChartFillPattern.Crosshatch;
        chart.Series[0].Markers.Enabled = false;
        var prepared = Prepare(chart);
        Assert.Equal(0, Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "radar-area").Fill!.Value.A);
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "radar-pattern");
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "radar-outline");
        Assert.Equal(6, prepared.Regions.Count(region => region.Role == "radar-point"));
        Assert.NotEmpty(prepared.ToPng());
    }

    private static Chart Bare() {
        var chart = Chart.Create().WithSize(320, 220).WithHeader(false).WithLegend(false).WithDataLabels(false).WithYAxisBounds(0, 100);
        chart.Options.ShowAxes = false; chart.Options.ShowGrid = false; return chart;
    }

    private static ChartPoint[] SixValues(double value) => Enumerable.Range(1, 6).Select(index => new ChartPoint(index, value)).ToArray();

    private static PreparedVisual Prepare(Chart chart) {
        var request = VisualExportRequest.ForChart(chart).Context;
        return chart.Prepare(new VisualRenderContext(request.Layout, request.Theme, request.ThemeMode,
            new VisualFrame(showLegend: chart.Options.ShowLegend, transparentBackground: true), request.Font));
    }
}
