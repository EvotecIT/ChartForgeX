using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

internal static class HorizontalRasterAssertions {
    internal static readonly ChartColor Guide = ChartColor.FromHex("#D017CB");
    internal static readonly ChartColor Band = ChartColor.FromHex("#17BC43");

    internal static void Statistics(PreparedVisual prepared, ChartRect line, ChartRect band) {
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        var start = (int)Math.Ceiling(line.Top + line.Height * .2);
        var end = (int)Math.Floor(line.Bottom - line.Height * .2);
        var paintedRows = Enumerable.Range(start, end - start).Count(y => HasColor(image, line.Left, y, Guide, Band));
        Assert.True(paintedRows > (end - start) * .3, "The native mean/median guide must extend vertically at its physical X position.");
        foreach (var fraction in new[] { .2, .8 }) {
            var y = band.Top + band.Height * fraction;
            Assert.True(HasColor(image, band.Left + band.Width * .25, y, Band), "The native deviation band must fill its physical X interval.");
            Assert.False(HasColor(image, band.Right + 6, y, Band), "The native deviation band must stop at its physical X boundary.");
        }
    }

    internal static bool HasColor(RgbaImage image, double x, double y, ChartColor color, ChartColor? background = null) {
        // A small neighbourhood and channel tolerance permit antialiasing and rounding.
        for (var row = Math.Max(0, (int)Math.Round(y) - 2); row <= Math.Min(image.Height - 1, (int)Math.Round(y) + 2); row++)
            for (var column = Math.Max(0, (int)Math.Round(x) - 2); column <= Math.Min(image.Width - 1, (int)Math.Round(x) + 2); column++) {
                var index = (row * image.Width + column) * 4;
                if (Math.Abs(image.Pixels[index] - color.R) <= 16 && Math.Abs(image.Pixels[index + 1] - color.G) <= 16 && Math.Abs(image.Pixels[index + 2] - color.B) <= 16) return true;
                if (background is ChartColor under) {
                    var red = color.R - under.R; var green = color.G - under.G; var blue = color.B - under.B;
                    var coverage = ((image.Pixels[index] - under.R) * red + (image.Pixels[index + 1] - under.G) * green + (image.Pixels[index + 2] - under.B) * blue)
                        / (double)(red * red + green * green + blue * blue);
                    if (coverage >= .25 && coverage <= 1 && Math.Abs(image.Pixels[index] - under.R - coverage * red) <= 16
                        && Math.Abs(image.Pixels[index + 1] - under.G - coverage * green) <= 16 && Math.Abs(image.Pixels[index + 2] - under.B - coverage * blue) <= 16) return true;
                }
            }
        return false;
    }
}
