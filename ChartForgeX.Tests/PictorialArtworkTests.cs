using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PictorialArtworkTests {
    internal const string ArtworkSvg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 8 4'><rect width='8' height='4' fill='#336699'/></svg>";
    [Theory]
    [InlineData(ChartPictorialFillDirection.LeftToRight, false)]
    [InlineData(ChartPictorialFillDirection.RightToLeft, false)]
    [InlineData(ChartPictorialFillDirection.BottomToTop, false)]
    [InlineData(ChartPictorialFillDirection.TopToBottom, false)]
    [InlineData(ChartPictorialFillDirection.LeftToRight, true)]
    [InlineData(ChartPictorialFillDirection.RightToLeft, true)]
    [InlineData(ChartPictorialFillDirection.BottomToTop, true)]
    [InlineData(ChartPictorialFillDirection.TopToBottom, true)]
    public void ImagePartialDirectionsRetainSourceAspectAndMatchSvgNativePixels(ChartPictorialFillDirection direction, bool fromSvg) {
        var artwork = fromSvg ? ChartPictorialArtwork.FromSvg(ArtworkSvg, 8, 4) : ChartPictorialArtwork.FromImage(Image());
        var chart = Model(.25).Pictorial(p => p.Artwork(artwork).FillDirection(direction));
        var prepared = Prepare(chart, fromSvg ? ThemeMode.Dark : ThemeMode.Light);
        var image = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneImage>(), node => node.Role == "pictorial-fill");
        Assert.Equal(2, image.Bounds.Width / image.Bounds.Height, 10);
        var index = prepared.Scene.Nodes.ToList().IndexOf(image);
        var clip = prepared.Scene.Nodes.Take(index).OfType<VisualSceneGroup>().Last(node => node.Clip.HasValue).Clip!.Value;
        var vertical = direction is ChartPictorialFillDirection.BottomToTop or ChartPictorialFillDirection.TopToBottom;
        Assert.Equal(image.Bounds.Width * (vertical ? 1 : .25), clip.Width, 10);
        Assert.Equal(image.Bounds.Height * (vertical ? .25 : 1), clip.Height, 10);
        Assert.Equal(direction == ChartPictorialFillDirection.RightToLeft ? image.Bounds.Right - clip.Width : image.Bounds.Left, clip.Left, 10);
        Assert.Equal(direction == ChartPictorialFillDirection.BottomToTop ? image.Bounds.Bottom - clip.Height : image.Bounds.Top, clip.Top, 10);
        var native = VisualSceneRasterRenderer.Render(prepared.Scene);
        var rasterSvg = SvgRasterizer.Rasterize(prepared.ToSvg(), 480, 220, strict: true).Image;
        var near = direction is ChartPictorialFillDirection.LeftToRight or ChartPictorialFillDirection.TopToBottom ? .125 : .875;
        var inside = new ChartPoint(image.Bounds.X + image.Bounds.Width * (vertical ? .5 : near), image.Bounds.Y + image.Bounds.Height * (vertical ? near : .5));
        var outside = new ChartPoint(image.Bounds.X + image.Bounds.Width * (vertical ? .5 : 1 - near), image.Bounds.Y + image.Bounds.Height * (vertical ? 1 - near : .5));
        Assert.Equal(new byte[] { 51, 102, 153, 255 }, Pixel(native, inside));
        Assert.Equal(Pixel(native, inside), Pixel(rasterSvg, inside));
        Assert.Equal(0, Pixel(native, outside)[3]); Assert.Equal(0, Pixel(rasterSvg, outside)[3]);
        var item = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "pictorial-item");
        Assert.Equal("0.25", item.Metadata["data-cfx-value"]); Assert.Equal("assessments", item.Metadata["data-cfx-series-key"]);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.All(svg.Descendants().Where(node => node.Name.LocalName == "image"), node => Assert.StartsWith("data:image/png;base64,", node.Attribute("href")!.Value));
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName is "script" or "foreignObject");
        Save(prepared, "image-" + direction + "-" + fromSvg);
    }

    [Theory]
    [InlineData(ChartPictorialFillDirection.LeftToRight)]
    [InlineData(ChartPictorialFillDirection.RightToLeft)]
    [InlineData(ChartPictorialFillDirection.BottomToTop)]
    [InlineData(ChartPictorialFillDirection.TopToBottom)]
    public void BuiltInAndCustomHoleContoursShareTheSamePartialRectangle(ChartPictorialFillDirection direction) {
        var chart = Model(.5).Pictorial(p => p.Shape(ChartPictorialShape.Square).SvgPathData(null).FillDirection(direction));
        var square = Prepare(chart); var squareClip = FillClip(square.Scene);
        chart.Pictorial(p => p.SvgPath("M0 0H24V24H0Z M8 8V16H16V8Z")); var hole = Prepare(chart); var contourClip = FillClip(hole.Scene);
        Assert.Equal(squareClip, contourClip);
        Assert.Equal(2, VisualSceneGeometry.Flatten(Assert.Single(hole.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "pictorial-fill"), 1).Count);
        Save(hole, "contour-" + direction);
    }

    [Fact]
    public void CapturedImageAndRepeatedExportsRemainDetachedFromSourceEdits() {
        var source = Image(); var artwork = ChartPictorialArtwork.FromImage(source); source.Pixels[0] = 255;
        var chart = Model(2.5).Pictorial(p => p.Columns(4).Artwork(artwork));
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Equal(8, artwork.Width); Assert.Equal(4, artwork.Height);
        var uris = XDocument.Parse(svg).Descendants().Where(node => node.Name.LocalName == "image").Select(node => (string)node.Attribute("href")!).Distinct().ToArray();
        var uri = Assert.Single(uris); var decoded = RasterImageDecoder.Decode(Convert.FromBase64String(uri.Substring(uri.IndexOf(',') + 1)));
        Assert.Equal((byte)51, decoded.Pixels[0]); Assert.Equal((byte)102, decoded.Pixels[1]);
        chart.Pictorial(p => p.Artwork((ChartPictorialArtwork?)null).FillDirection(ChartPictorialFillDirection.BottomToTop));
        chart.Series[0].Points[0] = new ChartPoint(1, 0);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.DoesNotContain("data-cfx-image-artwork", chart.ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void NullArtworkAndDefaultDirectionRestoreExactBuiltInAndCustomContourBytes() {
        var chart = Model(.5); var builtInSvg = chart.ToSvg(); var builtInPng = chart.ToPng();
        chart.Pictorial(p => p.Artwork((ChartPictorialArtwork?)null).FillDirection(ChartPictorialFillDirection.LeftToRight));
        Assert.Equal(builtInSvg, chart.ToSvg()); Assert.Equal(builtInPng, chart.ToPng());
        chart.Pictorial(p => p.SvgPath("M0 0H24V24H0Z M8 8V16H16V8Z")); var contourSvg = chart.ToSvg(); var contourPng = chart.ToPng();
        chart.Pictorial(p => p.Artwork(ChartPictorialArtwork.FromImage(Image()))); Assert.Contains("data-cfx-image-artwork", chart.ToSvg());
        chart.Pictorial(p => p.Artwork((ChartPictorialArtwork?)null)); Assert.Equal(contourSvg, chart.ToSvg()); Assert.Equal(contourPng, chart.ToPng());
        chart.Pictorial(p => p.Shape(ChartPictorialShape.Square).SvgPathData(null)); chart.Pictorial(p => p.SvgPath("M"));
        chart.Pictorial(p => p.Artwork(ChartPictorialArtwork.FromImage(Image()))); Assert.NotEmpty(chart.ToPng());
        Assert.Equal("M", chart.Options.Pictorial.SvgPathData); // Artwork precedence does not rewrite fallback configuration.
    }

    internal static Chart Model(double value) => Chart.Create().Size(480, 220).ShowHeader(false).Legend(false).Axes(false).ShowGrid(false)
        .Pictorial(p => p.Columns(1).ValuePerSymbol(1).ShowValues(false).EmptyOpacity(0).Shape(ChartPictorialShape.Square).SvgPathData(null)).XLabels("Team")
        .AddSeries(new ChartSeries(new ChartSeriesId("assessments"), "Assessments", ChartSeriesKind.Pictorial,
            new[] { new ChartPoint(1, value) }).PointColor(0, ChartColor.FromHex("#336699")));
    internal static PreparedVisual Prepare(Chart chart, ThemeMode mode = ThemeMode.Light) => chart.Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(480, 220)), VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode,
        new VisualFrame(showLegend: false, transparentBackground: true)));
    internal static RgbaImage Image() {
        var pixels = new byte[8 * 4 * 4]; for (var index = 0; index < pixels.Length; index += 4) {
            pixels[index] = 51; pixels[index + 1] = 102; pixels[index + 2] = 153; pixels[index + 3] = 255;
        }
        return new RgbaImage(8, 4, pixels);
    }
    private static ChartRect FillClip(VisualScene scene) {
        var nodes = scene.Nodes.ToList(); var fill = nodes.FindIndex(node => node.Role == "pictorial-fill");
        return nodes.Take(fill).OfType<VisualSceneGroup>().Last(node => node.Clip.HasValue).Clip!.Value;
    }
    private static byte[] Pixel(RgbaImage image, ChartPoint point) {
        var offset = ((int)point.Y * image.Width + (int)point.X) * 4; return image.Pixels.Skip(offset).Take(4).ToArray();
    }
    internal static void Save(PreparedVisual prepared, string name) {
        var root = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(root)) return;
        Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, "pictorial-artwork-" + name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(root, "pictorial-artwork-" + name + ".png"), prepared.ToPng());
    }
}
