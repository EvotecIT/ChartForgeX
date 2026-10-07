using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2SceneGradientStrokeTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void GradientPathRetainsTheSameDashCapAndJoinAsItsOrdinaryStroke(bool rotated, bool square) {
        var builder = new VisualSceneBuilder(new VisualSize(40, 40), FontSpec.FromFamily("Missing stroke test font"));
        var dash = new[] { 4d, 8d };
        using (rotated ? builder.PushRotation(90, 20, 20) : null)
            builder.Path(new ChartPath(new[] {
                ChartPathCommand.MoveTo(5, 6), ChartPathCommand.LineTo(35, 6),
                ChartPathCommand.MoveTo(5, 30), ChartPathCommand.LineTo(20, 10), ChartPathCommand.LineTo(35, 30)
            }), stroke: ChartColor.Black, strokeWidth: 4, role: "authored-stroke", dash: dash,
                cap: square ? VisualStrokeCap.Square : VisualStrokeCap.Butt,
                join: square ? VisualStrokeJoin.Miter : VisualStrokeJoin.Bevel);
        var ordinary = builder.Build();
        // Transparent stops isolate the optional stroke contract. The ordinary path is the existing
        // native owner for dash, cap and join behavior; gradient fill is covered by V2ScenePaintTests.
        var nodes = ordinary.Nodes.Select(node => node is VisualScenePath path
            ? (VisualSceneNode)new VisualSceneGradient(path, new ChartPoint(0, 0), new ChartPoint(40, 0), new[] {
                new VisualGradientStop(0, ChartColor.Transparent), new VisualGradientStop(1, ChartColor.Transparent)
            }) : node).ToArray();
        var gradient = new VisualScene(ordinary.Size, nodes, ordinary.Diagnostics, ordinary.Regions);
        dash[0] = 20;
        var reference = VisualSceneRasterRenderer.Render(ordinary, supersampling: 1);
        var actual = VisualSceneRasterRenderer.Render(gradient, supersampling: 1);
        Assert.Equal(reference.Pixels, actual.Pixels);
        Assert.Contains(actual.Pixels, channel => channel > 0);
        if (!rotated) {
            Assert.Equal(0, Alpha(actual, 13, 6));
            Assert.Equal(square ? 255 : 0, Alpha(actual, 4, 6));
        }
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(gradient));
        var element = svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "authored-stroke");
        Assert.StartsWith("url(#", element.Attribute("fill")!.Value);
        Assert.Equal("4 8", element.Attribute("stroke-dasharray")!.Value);
        Assert.Equal(square ? "square" : "butt", element.Attribute("stroke-linecap")!.Value);
        Assert.Equal(square ? "miter" : "bevel", element.Attribute("stroke-linejoin")!.Value);
    }

    private static byte Alpha(ChartForgeX.Raster.RgbaImage image, int x, int y) => image.Pixels[(y * image.Width + x) * 4 + 3];
}
