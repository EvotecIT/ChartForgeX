using ChartForgeX.Core;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteColourVisionTests {
    // Machado, Oliveira and Fernandes (2009), full-severity linear-sRGB matrices.
    // https://www.inf.ufrgs.br/~oliveira/pubs_files/CVD_Simulation/CVD_Simulation.html
    private static readonly double[][] Protan = { new[] { .152286, 1.052583, -.204868 }, new[] { .114503, .786281, .099216 }, new[] { -.003882, -.048116, 1.051998 } };
    private static readonly double[][] Deuter = { new[] { .367322, .860646, -.227968 }, new[] { .280085, .672501, .047413 }, new[] { -.011820, .042940, .968881 } };

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void AdjacentCategoricalColoursRemainDistinct(bool dark, bool protanopia) {
        var tokens = dark ? VisualDesignTokens.GraphiteDark() : VisualDesignTokens.GraphiteLight();
        var labs = tokens.Palette.Select(colour => Lab(colour, protanopia ? Protan : Deuter)).ToArray();
        for (var i = 1; i < labs.Length; i++) {
            var distance = Math.Sqrt(labs[i].Zip(labs[i - 1], (a, b) => (a - b) * (a - b)).Sum());
            Assert.True(distance >= (i <= 2 ? 20 : 12), $"s{i} to s{i + 1}: CIE76 Delta E {distance:0.00}");
        }
    }

    private static double[] Lab(ChartColor colour, double[][] matrix) {
        double Linear(byte b) { var v = b / 255.0; return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4); }
        var rgb = new[] { Linear(colour.R), Linear(colour.G), Linear(colour.B) };
        var transformed = matrix.Select(row => Math.Clamp(row.Zip(rgb, (a, b) => a * b).Sum(), 0, 1)).ToArray();
        var r = transformed[0]; var g = transformed[1]; var b = transformed[2];
        double F(double v) => v > 216.0 / 24389 ? Math.Cbrt(v) : (24389.0 / 27 * v + 16) / 116;
        var x = F((.4124564 * r + .3575761 * g + .1804375 * b) / .95047);
        var y = F(.2126729 * r + .7151522 * g + .072175 * b);
        var z = F((.0193339 * r + .119192 * g + .9503041 * b) / 1.08883);
        return new[] { 116 * y - 16, 500 * (x - y), 200 * (y - z) };
    }
}
