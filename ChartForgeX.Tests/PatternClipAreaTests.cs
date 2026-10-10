using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PatternClipAreaTests {
    [Theory]
    [InlineData("bowtie", true)]
    [InlineData("hole", true)]
    [InlineData("duplicate-contours", false)]
    [InlineData("segmented-retrace", false)]
    public void PatternProducerUsesEvenOddFillRatherThanBoundsOrSignedArea(string shape, bool filled) {
        var commands = shape switch {
            "bowtie" => Contour((10, 10), (90, 90), (10, 90), (90, 10)),
            "segmented-retrace" => Contour((10, 10), (50, 90), (90, 50), (70, 70), (50, 90), (30, 50)),
            _ => Contour((10, 10), (90, 10), (90, 90), (10, 90))
        };
        if (shape == "hole") commands.AddRange(Contour((30, 30), (70, 30), (70, 70), (30, 70)));
        if (shape == "duplicate-contours") commands.AddRange(commands.ToArray());
        var builder = new VisualSceneBuilder(new VisualSize(100, 100), FontSpec.SystemSans());
        builder.Pattern(new ChartPath(commands), ChartFillPattern.Crosshatch, ChartColor.Black, role: "test-pattern");
        var prepared = new PreparedVisual(builder.Build());
        Assert.Equal(filled, prepared.Scene.Nodes.OfType<VisualSceneLine>().Any());
        var rgba = prepared.ToRgba();
        Assert.Equal(filled, Enumerable.Range(0, rgba.Width * rgba.Height).Any(pixel => rgba.Pixels[pixel * 4 + 3] != 0));
        if (shape == "hole") Assert.Equal(0, rgba.Pixels[(50 * rgba.Width + 50) * 4 + 3]);
    }

    private static List<ChartPathCommand> Contour(params (double X, double Y)[] points) => points.Select((point, index) =>
        index == 0 ? ChartPathCommand.MoveTo(point.X, point.Y) : ChartPathCommand.LineTo(point.X, point.Y)).ToList();
}
