using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects public numeric paths through the shared native mark, legend, pattern and bubble-scale owners.</summary>
public sealed class CustomMarkerPathTests {
    [Fact]
    public void UnitPathsRejectNonFiniteAndOutOfBoundsCoordinatesAtThePublicBoundary() {
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartMarkerPath.Start(double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartMarkerPath.Start(0, 1.01));
        var path = ChartMarkerPath.Start(-1, -1).LineTo(1, -1).LineTo(0, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => path.LineTo(double.PositiveInfinity, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => path.CubicTo(0, 0, -1.1, 0, 0, 1));
        var bounded = ChartMarkerPath.Start(0, 0);
        for (var index = 1; index < 128; index++) bounded = bounded.LineTo(0, 0);
        Assert.Throws<InvalidOperationException>(() => bounded.LineTo(0, 0));
    }

    [Fact]
    public void TypedCubicsReachMarksAndLegendsWhilePreparationCapturesTheOriginalShape() {
        var shape = ChartMarkerPath.Start(0, -1).CubicTo(1, -1, 1, 1, 0, 1).CubicTo(-1, 1, -1, -1, 0, -1);
        var chart = Bare().WithLegend().AddScatter("Glyph", new[] { new ChartPoint(5, 5) });
        ChartMarkerOptions? captured = null;
        chart.Series[0].ConfigureMarkers(options => { captured = options; options.CustomPath = shape; options.Radius = 20; });
        captured!.CustomPath = null;
        chart.Series[0].WithPointFillPattern(0, ChartFillPattern.DiagonalForward);
        var prepared = Prepare(chart);
        var mark = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "marker");
        var legend = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "legend-swatch");
        Assert.Equal(2, mark.Commands.Count(command => command.Kind == ChartPathCommandKind.CubicTo));
        Assert.Equal(mark.Commands.Select(command => command.Kind), legend.Commands.Select(command => command.Kind));
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "marker-pattern");
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.PathClip != null
            && group.PathClip.Commands.Count == mark.Commands.Count && group.PathClip.Commands[0].X == mark.Commands[0].X
            && group.PathClip.Commands[0].Y == mark.Commands[0].Y);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Equal("path", Assert.Single(Roles(XDocument.Parse(svg), "marker")).Name.LocalName);
        Assert.Contains(RasterImageDecoder.Decode(png).Pixels.Where((_, index) => index % 4 == 3), alpha => alpha > 0);
        chart.Series[0].Markers.CustomPath = shape.LineTo(1, 0);
        chart.Series[0].Markers.Enabled = false;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.DoesNotContain(Prepare(chart).Scene.Nodes, node => node.Role == "marker");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CustomBubbleReferenceAreaUsesEvenOddHolesAndSelfIntersectionsWithoutChangingRawSizes(bool crossing) {
        var shape = crossing
            ? ChartMarkerPath.Start(-1, -1).LineTo(1, 1).LineTo(-1, 1).LineTo(1, -1)
            : ChartMarkerPath.Start(-1, -1).LineTo(1, -1).LineTo(1, 1).LineTo(-1, 1)
                .MoveTo(-.5, -.5).LineTo(.5, -.5).LineTo(.5, .5).LineTo(-.5, .5);
        var chart = Bare().AddBubble("Areas", new[] { new ChartBubble(5, 5, 100) }).ConfigureBubble(options => {
            options.WithSizeDomain(0, 100); options.MinimumRadius = 0; options.MaximumRadius = 20;
            options.SizeEncoding = ChartBubbleSizeEncoding.Area;
        });
        chart.Series[0].ConfigureMarkers(options => { options.CustomPath = shape; options.StrokeWidth = 0; options.Fill = ChartColor.Black; });
        var prepared = Prepare(chart);
        var mark = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "bubble");
        var expectedRadius = 20 * Math.Sqrt(Math.PI / (crossing ? 2 : 3));
        Assert.Equal(expectedRadius * 2, mark.Commands.Max(command => command.X) - mark.Commands.Min(command => command.X), 7);
        var group = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "point");
        Assert.Equal("100", group.Metadata["data-cfx-size"]);
        Assert.Equal(100, chart.Series[0].Bubbles[0].Size);
        Assert.Contains("fill-rule=\"evenodd\"", prepared.ToSvg());
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 2)));
        var area = image.Pixels.Where((_, index) => index % 4 == 3).Sum(alpha => (double)alpha) / 255;
        Assert.InRange(area, Math.PI * 400 * .975, Math.PI * 400 * 1.025);
    }

    [Fact]
    public void BubbleRadiusBoundsRemainUnitExtentsWhileAreaModeRejectsAnEmptyContour() {
        var shape = ChartMarkerPath.Start(-1, -1).LineTo(1, -1).LineTo(0, 1);
        var chart = Bare().AddBubble("Sizes", new[] { new ChartBubble(2, 5, 1), new ChartBubble(8, 5, 100) })
            .ConfigureBubble(options => { options.WithSizeDomain(1, 100); options.MinimumRadius = 3; options.MaximumRadius = 20; });
        chart.Series[0].ConfigureMarkers(options => { options.CustomPath = shape; options.StrokeWidth = 0; });
        var marks = Prepare(chart).Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "bubble").ToArray();
        Assert.Equal(new[] { 6d, 40d }, marks.Select(mark => mark.Commands.Max(command => command.X) - mark.Commands.Min(command => command.X)));
        chart.ConfigureBubble(options => options.SizeEncoding = ChartBubbleSizeEncoding.Area);
        chart.Series[0].Markers.CustomPath = ChartMarkerPath.Start(-1, 0).LineTo(1, 0);
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
    }

    private static Chart Bare() {
        var chart = Chart.Create().WithSize(400, 300).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithAxes(false).WithGrid(false).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false; chart.Options.TransparentBackground = true;
        return chart;
    }
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static IEnumerable<XElement> Roles(XDocument document, string role) => document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role);
}
