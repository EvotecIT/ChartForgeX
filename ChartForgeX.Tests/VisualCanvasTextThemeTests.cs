using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VisualCanvasTextThemeTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UncoloredTextUsesCanonicalForegroundInSvgAndPng(bool dark) {
        var tokens = dark ? VisualDesignTokens.GraphiteDark() : VisualDesignTokens.GraphiteLight();
        var canvas = VisualCanvas.Create(320, 100).WithDesignTokens(tokens)
            .AddText(12, 12, 296, "Theme text", 36);
        var control = VisualCanvas.Create(320, 100).WithDesignTokens(tokens)
            .AddText(12, 12, 296, "Theme text", 36, tokens.Foreground);

        Assert.Equal(tokens.Foreground.ToCss(), TextFill(canvas.ToSvg()));
        var raster = canvas.RenderRgba();
        Assert.Equal(control.RenderRgba().Pixels, raster.Pixels);
        Assert.True(ContainsOpaqueColor(raster, tokens.Foreground), "The native raster must draw the selected foreground.");
    }

    [Fact]
    public void BorrowedCanvasArtifactKeepsInheritedTextWhenItsThemeChanges() {
        var canvas = VisualCanvas.Create(320, 100).WithDesignTokens(VisualDesignTokens.GraphiteLight())
            .AddLayer(new VisualCanvasTextLayer(12, 12, 296, "Theme text", 36));
        var borrowed = canvas.ToVisualArtifact().Clone();
        var dark = VisualDesignTokens.GraphiteDark();
        canvas.WithDesignTokens(dark);
        var control = VisualCanvas.Create(320, 100).WithDesignTokens(dark)
            .AddText(12, 12, 296, "Theme text", 36, dark.Foreground);

        Assert.Equal(dark.Foreground.ToCss(), TextFill(borrowed.ToSvg()));
        Assert.Equal(control.RenderRgba().Pixels, borrowed.ToRgbaImage().Pixels);
        Assert.Contains("fill=\"" + dark.Foreground.ToCss() + "\"", borrowed.ToHtmlPage());
    }

    [Theory]
    [InlineData("white", false)]
    [InlineData("transparent", false)]
    [InlineData("cyan", false)]
    [InlineData("custom", false)]
    [InlineData("white", true)]
    [InlineData("transparent", true)]
    [InlineData("cyan", true)]
    [InlineData("custom", true)]
    public void ExplicitConstructorAndPropertyColorsSurviveThemeChanges(string name, bool assignProperty) {
        var color = name switch {
            "white" => ChartColor.White,
            "transparent" => ChartColor.Transparent,
            "cyan" => ChartColor.FromHex("#00FFFF"),
            _ => ChartColor.FromHex("#9F1239")
        };
        var layer = assignProperty
            ? new VisualCanvasTextLayer(12, 12, 296, "Explicit text", 36) { Color = color }
            : new VisualCanvasTextLayer(12, 12, 296, "Explicit text", 36, color);
        var canvas = VisualCanvas.Create(320, 100).WithDesignTokens(VisualDesignTokens.GraphiteLight())
            .WithBackdrop(VisualCanvasBackdropStyle.Transparent).AddLayer(layer);
        foreach (var tokens in new[] { VisualDesignTokens.GraphiteLight(), VisualDesignTokens.GraphiteDark() }) {
            canvas.WithDesignTokens(tokens);
            Assert.Equal(color.ToCss(), TextFill(canvas.ToSvg()));
            var raster = canvas.RenderRgba();
            if (color.A == 0) {
                Assert.All(raster.Pixels.Where((_, index) => index % 4 == 3), alpha => Assert.Equal((byte)0, alpha));
            } else {
                Assert.True(ContainsOpaqueColor(raster, color), "An explicit color must reach the native raster unchanged.");
            }
        }
    }

    private static string TextFill(string svg) => XDocument.Parse(svg).Descendants()
        .Single(element => (string?)element.Attribute("data-cfx-role") == "visual-canvas-text")
        .Attribute("fill")!.Value;

    private static bool ContainsOpaqueColor(RgbaImage image, ChartColor color) {
        for (var offset = 0; offset < image.Pixels.Length; offset += 4) {
            if (image.Pixels[offset] == color.R && image.Pixels[offset + 1] == color.G &&
                image.Pixels[offset + 2] == color.B && image.Pixels[offset + 3] == 255) return true;
        }
        return false;
    }
}
