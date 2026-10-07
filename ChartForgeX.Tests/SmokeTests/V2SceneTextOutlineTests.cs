using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Glyph-following native text outlines retain prepared faces and obey scene density and clipping.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class V2SceneTextOutlineTests : IDisposable {
    private const string Family = "CFX Scene Outline";
    private static readonly ChartColor Ink = ChartColor.FromRgb(20, 40, 200);
    private static readonly ChartColor Halo = ChartColor.FromRgb(220, 30, 40);

    [Fact]
    public void SvgOutlineUsesTypedPaintAndParticipatesInDetachedIdentity() {
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed());
        var plain = Builder(); plain.Text("H H", 20, 60, 40, Ink, role: "label");
        var outlined = Builder(); outlined.Text("H H", 20, 60, 40, Ink, role: "label", stroke: Halo, strokeWidth: 4,
            strokePaint: SvgPaint.Of(Halo, SvgColorRole.Surface));
        var scene = outlined.Build();
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        var text = svg.Descendants().Single(element => element.Name.LocalName == "text");
        Assert.Equal(Halo.ToCss(), text.Attribute("stroke")!.Value);
        Assert.Equal("4", text.Attribute("stroke-width")!.Value);
        Assert.Equal("round", text.Attribute("stroke-linejoin")!.Value);
        Assert.Equal("stroke", text.Attribute("paint-order")!.Value);
        Assert.NotEqual(VisualSceneSvgRenderer.Identity(plain.Build(), null, null, null, false), VisualSceneSvgRenderer.Identity(scene, null, null, null, false));
        var variables = new SvgColorVariables().Add("--outline", Halo, SvgColorRole.Surface);
        var options = new VisualSvgOptions(colorVariables: variables);
        var bound = VisualSceneSvgRenderer.Render(scene, options: options);
        Assert.Contains("var(--outline,", bound);
        var image = VisualSceneRasterRenderer.Render(scene, supersampling: 1);
        Assert.Equal(0, image.Pixels[(45 * image.Width + 55) * 4 + 3]); // Between glyphs, inside the label's bounding box.
        Assert.True(Count(image, Halo) > 0 && Count(image, Ink) > 0);
    }

    [Theory]
    [InlineData(1, 0)] [InlineData(2, 0)]
    [InlineData(1, 90)] [InlineData(2, 90)]
    public void NativeOutlineRetainsGlyphsAcrossRegistryChangesAndHonorsDensityRotationAndClip(int scale, double angle) {
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed());
        var builder = Builder();
        using (builder.PushClip(new ChartRect(20, 20, 60, 60)))
        using (builder.PushRotation(angle, 60, 50))
            builder.Text("H H", 20, 60, 40, Ink, stroke: Halo, strokeWidth: 4);
        var scene = builder.Build();
        var image = VisualSceneRasterRenderer.Render(scene, scale, supersampling: 2);
        Assert.Equal(120 * scale, image.Width);
        Assert.True(Count(image, Halo) > 0 && Count(image, Ink) > 0);
        for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) {
            if (x >= 20 * scale && x < 80 * scale && y >= 20 * scale && y < 80 * scale) continue;
            Assert.Equal(0, image.Pixels[(y * image.Width + x) * 4 + 3]);
        }
        FontRegistry.Clear();
        FontRegistry.Register(Family, OpenTypeTestFonts.CidKeyed());
        Assert.Equal(image.Pixels, VisualSceneRasterRenderer.Render(scene, scale, supersampling: 2).Pixels);
    }

    [Fact]
    public void OutlineRejectsInvalidWidthsAndBoundsIntermediateAllocation() {
        var builder = Builder();
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Text("H", 0, 20, 20, Ink, stroke: Halo, strokeWidth: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Text("H", 0, 20, 20, Ink, stroke: Halo, strokeWidth: double.NaN));
        builder.Text("H", 0, 20, 20, Ink, stroke: Halo, strokeWidth: 10_000);
        Assert.Throws<ArgumentOutOfRangeException>(() => VisualSceneRasterRenderer.Render(builder.Build()));
    }

    private static VisualSceneBuilder Builder() => new(new VisualSize(120, 100), FontSpec.FromFamily(Family));
    private static int Count(RgbaImage image, ChartColor color) => image.Pixels.Chunk(4)
        .Count(pixel => pixel[3] > 128 && Math.Abs(pixel[0] - color.R) < 10 && Math.Abs(pixel[1] - color.G) < 10 && Math.Abs(pixel[2] - color.B) < 10);
    public void Dispose() => FontRegistry.Clear();
}
