using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PictorialImagePaintMetadataTests {
    [Fact]
    public void CapturedSparseAlphaExtentSurvivesCallerEditsAndIsSharedByEverySymbolPlacement() {
        var source = Sparse(); var artwork = ChartPictorialArtwork.FromImage(source); Array.Clear(source.Pixels);
        Assert.Equal(new ChartRect(1, 1, 2, 2), artwork.Resource.AlphaBounds);
        var chart = PictorialArtworkTests.Model(2.5).WithPictorialColumns(4).WithPictorialArtwork(artwork);
        var prepared = PictorialArtworkTests.Prepare(chart); var images = prepared.Scene.Nodes.OfType<VisualSceneImage>().ToArray(); Assert.NotEmpty(images);
        Assert.All(images, image => { Assert.Equal(artwork.Resource.AlphaBounds, image.AlphaBounds); Assert.Same(artwork.Resource.Image.Pixels, image.Image.Pixels); });
        var xml = XDocument.Parse(prepared.ToSvg());
        Assert.All(xml.Descendants().Where(node => node.Name.LocalName == "image"), node => Assert.Equal("1 1 2 2 4 3", (string?)node.Attribute("data-cfx-image-alpha-bounds")));
        Assert.Single(images.Select(image => image.DataUri).Distinct());
        var capturedSvg = prepared.ToSvg(); var capturedPng = prepared.ToPng();
        chart.WithPictorialArtwork(null); Assert.Equal(capturedSvg, prepared.ToSvg()); Assert.Equal(capturedPng, prepared.ToPng());
        PictorialArtworkTests.Save(prepared, "alpha-extent-shared-capture");
    }

    [Fact]
    public void FullyTransparentCapturedPixelsHaveNoInventedPaintExtent() {
        var artwork = ChartPictorialArtwork.FromImage(new RgbaImage(4, 3, new byte[48])); Assert.Null(artwork.Resource.AlphaBounds);
        var prepared = PictorialArtworkTests.Prepare(PictorialArtworkTests.Model(.5).WithPictorialArtwork(artwork));
        var images = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => node.Name.LocalName == "image").ToArray(); Assert.NotEmpty(images);
        Assert.All(images, node => Assert.Equal("0 0 0 0 4 3", (string?)node.Attribute("data-cfx-image-alpha-bounds")));
        PictorialArtworkTests.Save(prepared, "transparent-native-alpha");
    }

    [Theory]
    [InlineData(ChartPictorialFillDirection.LeftToRight)] [InlineData(ChartPictorialFillDirection.RightToLeft)]
    [InlineData(ChartPictorialFillDirection.BottomToTop)] [InlineData(ChartPictorialFillDirection.TopToBottom)]
    public void PrivatePaintMetadataDoesNotChangeCapturedSourcePixelsOrNativeSvgPartialClipGeometry(ChartPictorialFillDirection direction) {
        var chart = PictorialArtworkTests.Model(.25).WithPictorialArtwork(ChartPictorialArtwork.FromImage(PictorialArtworkTests.Image())).WithPictorialFillDirection(direction);
        var prepared = PictorialArtworkTests.Prepare(chart); var svg = prepared.ToSvg(); var xml = XDocument.Parse(svg);
        var native = prepared.ToRgba(); var fromSvg = SvgRasterizer.Rasterize(svg, 480, 220, strict: true).Image;
        var shape = prepared.Scene.Nodes.OfType<VisualSceneImage>().Single(image => image.Role == "pictorial-fill");
        var vertical = direction is ChartPictorialFillDirection.BottomToTop or ChartPictorialFillDirection.TopToBottom;
        var near = direction is ChartPictorialFillDirection.LeftToRight or ChartPictorialFillDirection.TopToBottom ? .125 : .875;
        var x = (int)(shape.Bounds.X + shape.Bounds.Width * (vertical ? .5 : near)); var y = (int)(shape.Bounds.Y + shape.Bounds.Height * (vertical ? near : .5)); var offset = (y * native.Width + x) * 4;
        Assert.Equal(new byte[] { 51, 102, 153, 255 }, native.Pixels.AsSpan(offset, 4).ToArray());
        Assert.Equal(native.Pixels.AsSpan(offset, 4).ToArray(), fromSvg.Pixels.AsSpan(offset, 4).ToArray());
        Assert.All(xml.Descendants().Where(node => node.Name.LocalName == "image"), node => Assert.Equal("0 0 8 4 8 4", node.Attribute("data-cfx-image-alpha-bounds")!.Value));
        Assert.DoesNotContain(xml.Descendants(), node => node.Name.LocalName is "script" or "foreignObject"); PictorialArtworkTests.Save(prepared, "alpha-partial-" + direction);
    }
    private static RgbaImage Sparse() {
        var pixels = new byte[4 * 3 * 4]; pixels[(1 * 4 + 1) * 4 + 3] = 1; pixels[(2 * 4 + 2) * 4 + 3] = 255;
        return new RgbaImage(4, 3, pixels);
    }
}
