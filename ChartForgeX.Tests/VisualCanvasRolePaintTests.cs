using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VisualCanvasRolePaintTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void IndependentTileAndBadgePaintsReachSvgAndNativePngWithoutChangingSharedText(VisualThemeMode mode) {
        var canvas = CreateCanvas(mode);
        var colors = new[] { ChartColor.FromHex("#D01327"), ChartColor.FromHex("#19A64E"), ChartColor.FromHex("#2163D8"), ChartColor.FromHex("#A21FC3") };
        canvas.ConfigureStyle(style => {
            style.TileLabelColor = colors[0];
            style.TileValueColor = colors[1];
            style.TileDetailColor = colors[2];
            style.HeroBadgeTextColor = colors[3];
        });

        AssertRolePaints(canvas, colors);
        canvas.WithThemeMode(mode == VisualThemeMode.Light ? VisualThemeMode.Dark : VisualThemeMode.Light);
        AssertRolePaints(canvas, colors);
        Assert.Equal(canvas.Theme.Resolve(canvas.ThemeMode).Foreground.ToCss(), TextPaint(canvas.ToSvg(), "Shared text"));

        canvas.ConfigureStyle(style => {
            style.TileLabelColor = null;
            style.TileValueColor = null;
            style.TileDetailColor = null;
            style.HeroBadgeTextColor = null;
        });
        var inherited = canvas.Theme.Resolve(canvas.ThemeMode);
        AssertRolePaints(canvas, new[] { inherited.MutedForeground, inherited.Foreground, inherited.MutedForeground, inherited.Foreground });
    }

    [Fact]
    public void ConfiguredRolePaintsAreDetachedFromRetainedCallbackValuesAndFailedEdits() {
        var canvas = CreateCanvas(VisualThemeMode.Dark);
        VisualCanvasStyleOptions? retained = null;
        canvas.ConfigureStyle(style => {
            retained = style;
            style.TileLabelColor = ChartColors.Red;
            style.TileValueColor = ChartColors.Green;
            style.TileDetailColor = ChartColors.Blue;
            style.HeroBadgeTextColor = ChartColors.Magenta;
        });
        var artifact = canvas.ToVisualArtifact();
        var svg = artifact.ToSvg();
        var png = artifact.ToPng();

        retained!.TileLabelColor = retained.TileValueColor = retained.TileDetailColor = retained.HeroBadgeTextColor = ChartColors.Yellow;
        Assert.Throws<InvalidOperationException>(() => canvas.ConfigureStyle(style => {
            style.TileLabelColor = style.TileValueColor = style.TileDetailColor = style.HeroBadgeTextColor = ChartColors.Cyan;
            throw new InvalidOperationException("Rejected edit");
        }));

        Assert.Equal(svg, artifact.ToSvg());
        Assert.Equal(png, artifact.ToPng());
        AssertRolePaints(canvas, new[] { ChartColors.Red, ChartColors.Green, ChartColors.Blue, ChartColors.Magenta });
    }

    private static VisualCanvas CreateCanvas(VisualThemeMode mode) => VisualCanvas.Create(620, 220)
        .WithTheme(VisualTheme.Graphite(), mode)
        .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
        .AddInfoTile(12, 12, 360, 150, "", "Capacity label", "256 GB", "Calibrated detail")
        .AddHeroBadge(410, 20, 150, 110, "PB")
        .AddText(12, 180, 590, "Shared text", 24);

    private static void AssertRolePaints(VisualCanvas canvas, ChartColor[] colors) {
        var svg = canvas.ToSvg();
        var raster = canvas.ToRgbaImage();
        var labels = new[] { "Capacity label", "256 GB", "Calibrated detail", "PB" };
        for (var index = 0; index < labels.Length; index++) {
            Assert.Equal(colors[index].ToCss(), TextPaint(svg, labels[index]));
            Assert.True(ContainsOpaqueColor(raster, colors[index]), "Native canvas text omitted " + labels[index]);
        }
    }

    private static string TextPaint(string svg, string text) => XDocument.Parse(svg).Descendants()
        .Single(element => element.Name.LocalName == "text" && element.Value == text).Attribute("fill")!.Value;

    private static bool ContainsOpaqueColor(RgbaImage image, ChartColor color) {
        for (var offset = 0; offset < image.Pixels.Length; offset += 4)
            if (image.Pixels[offset] == color.R && image.Pixels[offset + 1] == color.G && image.Pixels[offset + 2] == color.B && image.Pixels[offset + 3] == 255) return true;
        return false;
    }
}
