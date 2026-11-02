using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BulletNumericCaptionTests {
    private const string ProofFont = "CFX Bullet Numeric Captions Carlito";
    private static readonly double[] Values = { 0, 2611.111, 3722.222, 4833.333, 5944.444, 7055.555, 8166.666, 12345.6789 };

    [Theory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(760, VisualThemeMode.Light)]
    [InlineData(760, VisualThemeMode.Dark)]
    public void DenseBulletNumbersStayCompleteAndScaleCaptionsDoNotOverlap(int width, VisualThemeMode mode) {
        var chart = Create();
        var height = width == 390 ? 300 : 440;
        var prepared = chart.Prepare(Context(width, height, mode));
        ScatterBubbleColorRenderingTests.Capture(prepared, $"bullet-numeric-{width}-{mode}");
        var numeric = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => NumericRole(node.Role)).ToArray();
        Assert.NotEmpty(numeric);
        foreach (var text in numeric) {
            var displayed = string.Join(" ", text.Text.Lines.Select(line => line.Text));
            var complete = text.Role == "bullet-value-label" ? Values.Select(Format)
                : text.Role == "bullet-target-label" ? new[] { "/ " + Format(9500.125) }
                : new[] { 0d, 2500, 5000, 7500, 10000 }.Select(Format);
            Assert.Contains(displayed, complete);
            Assert.True(text.Text.Size >= 12, "Ordinary bullet numbers must not shrink to fit a dense row.");
        }
        var axis = numeric.Where(node => node.Role == "bullet-axis-label").OrderBy(node => node.X).ToArray();
        Assert.Contains(axis, text => string.Join(" ", text.Text.Lines.Select(line => line.Text)) == Format(0));
        Assert.Contains(axis, text => string.Join(" ", text.Text.Lines.Select(line => line.Text)) == Format(10000));
        for (var index = 1; index < axis.Length; index++) {
            var prior = axis[index - 1]; var current = axis[index];
            Assert.True(prior.X + prior.Text.Metrics.Width / 2 + 5.9 <= current.X - current.Text.Metrics.Width / 2);
        }
        var rows = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "bullet-row").ToArray();
        Assert.Equal(Values.Length, rows.Length);
        for (var index = 0; index < rows.Length; index++) {
            Assert.Equal(Values[index].ToString("R", CultureInfo.InvariantCulture), rows[index].Metadata["data-cfx-value"]);
            Assert.Equal("9500.125", rows[index].Metadata["data-cfx-target"]);
            Assert.Contains(prepared.Regions, region => region.Id == $"series-{index}-value" && region.Label == Format(Values[index]));
            Assert.Contains(prepared.Regions, region => region.Id == $"series-{index}-target" && region.Label == "/ " + Format(9500.125));
        }
        Assert.Equal(8, prepared.Scene.Nodes.Count(node => node.Role == "bullet-value"));
        Assert.Equal(8, prepared.Scene.Nodes.Count(node => node.Role == "bullet-target"));
        Assert.Equal(5, prepared.Scene.Nodes.Count(node => node.Role == "bullet-axis-tick"));
        if (width == 390) Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code is "bullet.label-overflow" or "radial.text-overflow");
        else {
            Assert.Equal(8, numeric.Count(node => node.Role == "bullet-target-label"));
            Assert.Equal(5, axis.Length);
        }
        var image = PngReader.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(width, image.Width); Assert.Equal(height, image.Height);
    }

    [Fact]
    public void HeightConstrainedBulletOmitsWholeNumbersInsteadOfPaintingMicroscopicFacts() {
        var chart = Create().WithAxes(false);
        var prepared = chart.Prepare(Context(390, 80, VisualThemeMode.Light, frame: false));
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node is VisualSceneText && NumericRole(node.Role));
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "radial.text-overflow");
        Assert.Equal(Values.Length, prepared.Regions.Count(region => region.Role == "bullet-value-label"));
        Assert.Equal(Values.Length, prepared.Scene.Nodes.Count(node => node.Role == "bullet-value"));
    }

    [Fact]
    public void AuthoredBulletNumberSizeRemainsIntactWhenCompleteCaptionsFit() {
        var chart = Create().ConfigureDataLabelStyle(style => style.WithFontSize(22));
        var prepared = chart.Prepare(Context(760, 440, VisualThemeMode.Light));
        var values = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "bullet-value-label").ToArray();
        Assert.NotEmpty(values);
        Assert.All(values, text => Assert.Equal(22, text.Text.Size));
        Assert.All(values, text => Assert.Contains(string.Join(" ", text.Text.Lines.Select(line => line.Text)), Values.Select(Format)));
    }

    private static bool NumericRole(string? role) => role is "bullet-value-label" or "bullet-target-label" or "bullet-axis-label";
    private static string Format(double value) => value.ToString("0.0000", CultureInfo.InvariantCulture);

    private static Chart Create() {
        var chart = Chart.Create().WithDataLabels().WithValueFormat(ChartValueFormat.Number("0.0000", CultureInfo.InvariantCulture));
        for (var index = 0; index < Values.Length; index++)
            chart.AddBullet("Regional service " + (index + 1), Values[index], 9500.125, 0, 10000, new[] { 4000d, 8000 });
        return chart;
    }

    private static VisualRenderContext Context(int width, int height, VisualThemeMode mode, bool frame = true) {
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        FontRegistry.Register(ProofFont, Path.Combine(fonts, "Carlito-Regular.ttf"), 400);
        FontRegistry.Register(ProofFont, Path.Combine(fonts, "Carlito-Bold.ttf"), 700);
        return new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height), frame ? 14 : 0), VisualTheme.Graphite(), mode,
            frame ? new VisualFrame("Dense comparison", "Native numeric captions", showLegend: false) : new VisualFrame(showLegend: false),
            FontSpec.FromFamily(ProofFont));
    }
}
