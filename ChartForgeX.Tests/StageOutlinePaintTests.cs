using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StageOutlinePaintTests {
    [Theory]
    [InlineData(false, ChartFunnelForm.StageBars, 390, VisualThemeMode.Light)]
    [InlineData(false, ChartFunnelForm.StageBars, 800, VisualThemeMode.Dark)]
    [InlineData(false, ChartFunnelForm.Cone, 390, VisualThemeMode.Dark)]
    [InlineData(false, ChartFunnelForm.Cone, 800, VisualThemeMode.Light)]
    [InlineData(true, ChartFunnelForm.StageBars, 390, VisualThemeMode.Light)]
    [InlineData(true, ChartFunnelForm.StageBars, 800, VisualThemeMode.Dark)]
    public void OutlinesUseExistingPointPaintAndPreserveExactStageGeometry(bool pyramid, ChartFunnelForm form, int width, VisualThemeMode mode) {
        var chart = StagePresentationOptionsTests.Create(pyramid, form, ChartOrientation.Vertical, width);
        chart.WithTheme(VisualTheme.Graphite(), mode).WithDataLabels(false);
        var original = StagePresentationOptionsTests.Prepare(chart);
        var role = pyramid ? "pyramid-segment" : form == ChartFunnelForm.Cone ? "funnel-stage-line" : "funnel-segment";
        var originalPaths = original.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == role).ToArray();
        var sourceColor = ChartColor.FromHex("#2476A8"); var border = ChartColor.FromHex("#DC4263");
        var dash = new[] { 6d, 3d };
        var outline = new ChartStrokeStyle(ChartColor.Black, 3, .75, dash); dash[0] = 100;
        chart.Series[0].WithPointColor(1, sourceColor).WithPointStrokeColor(1, border).WithPointFillPattern(1, ChartFillPattern.DiagonalForward);
        chart.ConfigureFunnel(options => { options.StageOutline = outline; options.StageOutlineDashOffset = 2; });
        chart.ConfigurePyramid(options => { options.PartitionOutline = outline; options.PartitionOutlineDashOffset = 2; });
        var prepared = StagePresentationOptionsTests.Prepare(chart);
        var paths = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == role).ToArray();
        Assert.Equal(originalPaths.Length, paths.Length);
        for (var index = 0; index < paths.Length; index++) Assert.Equal(originalPaths[index].Commands, paths[index].Commands);
        Assert.Equal(ChartColorMath.WithOpacity(border, .75), paths[1].Stroke);
        Assert.Equal(3, paths[1].StrokeWidth); Assert.Equal(new[] { 6d, 3d }, paths[1].Dash); Assert.Equal(2, paths[1].DashOffset);
        if (form != ChartFunnelForm.Cone || pyramid) { Assert.Equal(sourceColor, paths[1].Fill); Assert.Contains(prepared.Scene.Nodes, node => node.Role == "fill-pattern"); }
        var svg = XDocument.Parse(prepared.ToSvg());
        var painted = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role).ToArray();
        Assert.Equal(paths.Length, painted.Length);
        Assert.Contains(painted, node => node.Attribute("stroke-dasharray") != null);
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(width, image.Width); Assert.Equal(520, image.Height);
        Assert.False(original.ToPng(new VisualRenderOptions(supersampling: 1)).SequenceEqual(prepared.ToPng(new VisualRenderOptions(supersampling: 1))));
        ScatterBubbleColorRenderingTests.Capture(prepared, $"stage-outline-{pyramid}-{form}-{width}-{mode}");
        var preparedSvg = prepared.ToSvg(); var preparedPng = prepared.ToPng();
        chart.Series[0].Points.Clear();
        Assert.Equal(preparedSvg, prepared.ToSvg()); Assert.Equal(preparedPng, prepared.ToPng());
    }

    [Theory]
    [InlineData(false, ChartFunnelForm.StageBars)]
    [InlineData(false, ChartFunnelForm.Cone)]
    [InlineData(true, ChartFunnelForm.StageBars)]
    public void WidthZeroDisablesOnlyTheAuthoredOutlineWithoutChangingSourceOrPartitionPaths(bool pyramid, ChartFunnelForm form) {
        var chart = StagePresentationOptionsTests.Create(pyramid, form, ChartOrientation.Vertical).WithDataLabels(false);
        var role = pyramid ? "pyramid-segment" : form == ChartFunnelForm.Cone ? "funnel-stage-line" : "funnel-segment";
        var before = StagePresentationOptionsTests.Prepare(chart);
        chart.ConfigureFunnel(options => options.StageOutline = new ChartStrokeStyle(ChartColor.Black, 0));
        chart.ConfigurePyramid(options => options.PartitionOutline = new ChartStrokeStyle(ChartColor.Black, 0));
        var prepared = StagePresentationOptionsTests.Prepare(chart);
        var paths = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == role).ToArray();
        Assert.Equal(3, paths.Length); Assert.All(paths, path => Assert.Equal(0, path.StrokeWidth));
        var original = before.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == role).ToArray();
        for (var index = 0; index < paths.Length; index++) Assert.Equal(original[index].Commands, paths[index].Commands);
        Assert.Equal(3, prepared.Regions.Count(region => region.Role == (pyramid ? "pyramid-stage" : "funnel-stage")));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartFunnelForm.StageBars)]
    [InlineData(ChartFunnelForm.Cone)]
    public void ConnectionOverridesPreserveWeightsSlotsAndExplicitPatternStrokePhase(ChartFunnelForm form) {
        var chart = StagePresentationOptionsTests.Create(false, form, ChartOrientation.Horizontal).WithDataLabels(false);
        var before = StagePresentationOptionsTests.Prepare(chart);
        var paths = before.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role is "funnel-dropoff" or "funnel-connection-area").ToArray();
        var fill = ChartColor.FromHex("#4281B6"); var stroke = ChartColor.FromHex("#A83268");
        chart.ConfigureFunnel(options => options.ConnectorStyle = new ChartFunnelConnectorStyle(fillOpacity: .4));
        chart.Series[0].WithFunnelConnectorStyle(1, new ChartFunnelConnectorStyle(fill: fill, fillOpacity: .8,
            fillPattern: ChartFillPattern.Crosshatch, outlineStroke: new ChartStrokeStyle(stroke, 2, 1, new[] { 4d, 2d }), strokeDashOffset: -3));
        var prepared = StagePresentationOptionsTests.Prepare(chart);
        var styled = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role is "funnel-dropoff" or "funnel-connection-area").ToArray();
        for (var index = 0; index < paths.Length; index++) Assert.Equal(paths[index].Commands, styled[index].Commands);
        Assert.Equal(ChartColorMath.WithOpacity(fill, .8), styled[1].Fill); Assert.Equal(stroke, styled[1].Stroke);
        Assert.Equal(-3, styled[1].DashOffset); Assert.Equal(new[] { 4d, 2d }, styled[1].Dash);
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "fill-pattern");
        Assert.Equal(before.Scene.Regions.Where(region => region.Role == "funnel-stage").Select(region => region.Bounds),
            prepared.Scene.Regions.Where(region => region.Role == "funnel-stage").Select(region => region.Bounds));
        Assert.Contains("data-cfx-from-value=\"75\"", prepared.ToSvg()); Assert.NotEmpty(prepared.ToPng());
    }
}
