using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Prepared native geometry, clipping and text resources have a detached export lifetime.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class V2SceneTests : IDisposable {
    [Fact]
    public void PreparingCopiesCallerGeometryMetadataAndDashes() {
        var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(5, 5), ChartPathCommand.LineTo(35, 35) };
        var metadata = new Dictionary<string, string> { ["data-source-id"] = "original" };
        var dashes = new[] { 3d, 2d };
        var builder = Builder(48, 48);
        using (builder.PushGroup("series", "series", metadata)) {
            builder.Path(new ChartPath(commands), stroke: ChartColor.Black, strokeWidth: 2);
            builder.Line(5, 40, 35, 40, ChartColor.Black, dash: dashes);
        }
        var scene = builder.Build();
        var expectedSvg = VisualSceneSvgRenderer.Render(scene);
        var expectedPixels = VisualSceneRasterRenderer.Render(scene).Pixels;
        commands.Clear(); metadata["data-source-id"] = "changed"; dashes[0] = 20;
        builder.Rect(new ChartRect(0, 0, 48, 48), ChartColor.White);
        Assert.Equal(expectedSvg, VisualSceneSvgRenderer.Render(scene));
        Assert.Equal(expectedPixels, VisualSceneRasterRenderer.Render(scene).Pixels);
        Assert.Contains("data-source-id=\"original\"", expectedSvg);
    }

    [Fact]
    public void NestedClipScopesIntersectAndRestoreTheirParent() {
        var builder = Builder(40, 40);
        using (builder.PushClip(new ChartRect(5, 5, 30, 30))) {
            builder.Rect(new ChartRect(0, 0, 40, 40), ChartColor.FromRgb(200, 20, 20));
            using (builder.PushClip(new ChartRect(10, 10, 10, 10)))
                builder.Rect(new ChartRect(0, 0, 40, 40), ChartColor.FromRgb(20, 200, 20));
            builder.Rect(new ChartRect(25, 25, 15, 15), ChartColor.FromRgb(20, 20, 200));
        }
        var scene = builder.Build();
        var image = VisualSceneRasterRenderer.Render(scene);
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, Pixel(image, 1, 1));
        Assert.Equal(new byte[] { 200, 20, 20, 255 }, Pixel(image, 7, 7));
        Assert.Equal(new byte[] { 20, 200, 20, 255 }, Pixel(image, 15, 15));
        Assert.Equal(new byte[] { 20, 20, 200, 255 }, Pixel(image, 30, 30));
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, Pixel(image, 37, 30));
        var svg = VisualSceneSvgRenderer.Render(scene);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(svg, "<clipPath").Count);
    }

    [Fact]
    public void FullDonutPreservesTransparentHoleAndHasNoRadialStroke() {
        var builder = Builder(80, 80);
        var blue = ChartColor.FromRgb(20, 50, 200);
        builder.Slice(40, 40, 30, 15, 0, Math.PI * 2, blue, ChartColor.Black, 2, "donut-slice", "slice");
        var scene = builder.Build();
        var image = VisualSceneRasterRenderer.Render(scene, supersampling: 3);
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, Pixel(image, 40, 40));
        Assert.Equal(new byte[] { 20, 50, 200, 255 }, Pixel(image, 60, 40));
        Assert.Equal(new byte[] { 20, 50, 200, 255 }, Pixel(image, 40, 60));
        var svg = VisualSceneSvgRenderer.Render(scene);
        Assert.Contains("fill-rule=\"evenodd\"", svg);
        Assert.Contains("data-cfx-role=\"donut-slice\"", svg);
        Assert.Equal(2, svg.Count(c => c == 'M')); // Separate closed inner and outer contours.
    }

    [Fact]
    public void PreparedShapedRunsRetainPrimaryAndFallbackFacesAfterRegistryChanges() {
        const string primary = "CFX V2 Scene Primary", fallback = "CFX V2 Scene Fallback";
        FontRegistry.Register(primary, OpenTypeTestFonts.NameKeyed(includePrivateUse: false));
        FontRegistry.Register(fallback, OpenTypeTestFonts.NameKeyed());
        var style = new TextStyle { Font = FontSpec.FromFamily(primary + ", " + fallback), FontSize = 20, Color = ChartColor.Black };
        var text = "H" + char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter) + "\nO";
        var builder = Builder(100, 70);
        builder.Text(text, 8, 25, style, role: "probe-label");
        var scene = builder.Build();
        var node = Assert.IsType<VisualSceneText>(Assert.Single(scene.Nodes));
        Assert.Equal(2, node.Text.Lines.Count);
        Assert.NotSame(node.Text.Lines[0].Glyphs[0].Face.Root, node.Text.Lines[0].Glyphs[1].Face.Root);
        var svg = VisualSceneSvgRenderer.Render(scene);
        var pixels = VisualSceneRasterRenderer.Render(scene).Pixels;
        style.Font.Family = "unavailable"; style.Color = ChartColor.White; style.FontSize = 40;
        FontRegistry.Clear();
        FontRegistry.Register(primary, OpenTypeTestFonts.CidKeyed());
        Assert.Equal(svg, VisualSceneSvgRenderer.Render(scene));
        Assert.Equal(pixels, VisualSceneRasterRenderer.Render(scene).Pixels);
        Assert.Contains(pixels, b => b != 0);
    }

    [Fact]
    public void RasterBudgetIncludesScaleAndSupersamplingBeforeAllocation() {
        var scene = Builder(40, 20).Build();
        Assert.Throws<ArgumentOutOfRangeException>(() => VisualSceneRasterRenderer.Render(scene, scale: 2, supersampling: 3, pixelBudget: 28799));
        var image = VisualSceneRasterRenderer.Render(scene, scale: 2, supersampling: 3, pixelBudget: 28800);
        Assert.Equal(80, image.Width);
        Assert.Equal(40, image.Height);
        Assert.Throws<ArgumentOutOfRangeException>(() => VisualSceneRasterRenderer.Render(scene, scale: int.MaxValue));
    }

    private static VisualSceneBuilder Builder(double width, double height) => new(new VisualSize(width, height), FontSpec.SystemSans());
    private static byte[] Pixel(RgbaImage image, int x, int y) => image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();
    public void Dispose() => FontRegistry.Clear();
}
