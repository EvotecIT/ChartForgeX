using System.Security.Cryptography;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects native specialty geometry, fixed-viewport layout and detached source facts.</summary>
public sealed class V2SpecialtyTests {
    [Fact]
    public void EveryBuiltInPictorialShapeHasDistinctNativePixelsAndTheSameSharedPathInSvg() {
        var hashes = new HashSet<string>();
        foreach (var shape in Enum.GetValues<ChartPictorialShape>()) {
            var scene = Compile(Pictorial(shape, 1));
            Assert.Single(scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "pictorial-fill");
            var pixels = VisualSceneRasterRenderer.Render(scene).Pixels;
            Assert.Contains(pixels.Where((_, index) => index % 4 == 3), alpha => alpha > 0);
            Assert.True(hashes.Add(Convert.ToHexString(SHA256.HashData(pixels))), "Distinct pictorial shape lost: " + shape);
            Assert.Contains("data-cfx-shape=\"" + shape + "\"", VisualSceneSvgRenderer.Render(scene));
        }
    }

    [Fact]
    public void CustomPathUsesItsViewBoxInBothPaintersAndIgnoresTheLegacyPngFallback() {
        var custom = Pictorial(ChartPictorialShape.Circle, .5)
            .WithPictorialSvgPath("M110 100 L120 120 L100 120 Z", new ChartRect(100, 100, 20, 20), ChartPictorialShape.Circle);
        var customScene = Compile(custom);
        var triangle = Compile(Pictorial(ChartPictorialShape.Triangle, .5));
        var circle = Compile(Pictorial(ChartPictorialShape.Circle, .5));
        Assert.Equal(VisualSceneRasterRenderer.Render(triangle).Pixels, VisualSceneRasterRenderer.Render(customScene).Pixels);
        Assert.False(VisualSceneRasterRenderer.Render(circle).Pixels.SequenceEqual(VisualSceneRasterRenderer.Render(customScene).Pixels));
        var symbol = Assert.Single(customScene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "pictorial-symbol");
        Assert.Equal("0.5", symbol.Metadata["data-cfx-fill"]);
        Assert.Contains(customScene.Nodes.OfType<VisualSceneGroup>(), group => group.Clip.HasValue && group.Clip.Value.Width > 0);
        Assert.Contains("clip-path=", VisualSceneSvgRenderer.Render(customScene));
    }

    [Fact]
    public void CustomCompoundContoursKeepTheirHoleInTheNativeRaster() {
        var scene = Compile(Pictorial(ChartPictorialShape.Circle, 1)
            .WithPictorialSvgPath("M0 0 H24 V24 H0 Z M6 6 H18 V18 H6 Z"));
        var path = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "pictorial-fill");
        var left = path.Commands.Min(command => command.X); var right = path.Commands.Max(command => command.X);
        var top = path.Commands.Min(command => command.Y); var bottom = path.Commands.Max(command => command.Y);
        var image = VisualSceneRasterRenderer.Render(scene);
        byte Alpha(double x, double y) => image.Pixels[((int)y * image.Width + (int)x) * 4 + 3];
        Assert.Equal(0, Alpha((left + right) / 2, (top + bottom) / 2));
        Assert.True(Alpha(left + (right - left) * .1, (top + bottom) / 2) > 0);
        Assert.Contains("fill-rule=\"evenodd\"", VisualSceneSvgRenderer.Render(scene));
    }

    [Fact]
    public void PictorialWrapsUnitsRetainsStylesAndReportsFixedViewportOverflowWithoutLosingValues() {
        var color = ChartColor.FromHex("#2468AC");
        var chart = Chart.Create().WithPictorialColumns(2).WithPictorialValuePerSymbol(1).WithPictorialEmptyOpacity(.3)
            .AddPictorial("People", new[] { new ChartPictorialItem("A", 3.5, color) }, ChartPictorialShape.Person);
        chart.Series[0].WithPointFillPattern(0, ChartFillPattern.DiagonalForward);
        chart.Series[0].WithPointDataLabelStyle(0, style => style.WithColor("#AB1234"));
        var scene = Compile(chart);
        var symbols = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "pictorial-symbol").ToArray();
        Assert.Equal(4, symbols.Length); Assert.Equal("0.5", symbols[3].Metadata["data-cfx-fill"]);
        Assert.All(scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "pictorial-fill"), path => Assert.Equal(color, path.Fill));
        Assert.Contains(scene.Nodes, node => node.Role == "fill-pattern");
        Assert.Equal(ChartColor.FromHex("#AB1234"), Assert.Single(scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "pictorial-value").Color);
        chart.Options.ShowPictorialValues = false;
        Assert.DoesNotContain(Compile(chart).Nodes, node => node.Role == "pictorial-value");
        chart.Series[0].Points[0] = new ChartPoint(1, 1_000_000);
        var compact = Compile(chart, new ChartRect(0, 0, 100, 40));
        Assert.Contains(compact.Diagnostics, diagnostic => diagnostic.Code == "pictorial.overflow");
        Assert.Contains(compact.Regions, region => region.Role == "pictorial-item" && region.Label?.Contains("1000000") == true);
        Assert.True(compact.Nodes.Count(node => node.Role == "pictorial-symbol") <= 20);
    }

    [Fact]
    public void RotatedWordCloudUsesActualStyledMetricsAndRetainsLimitedAndZeroWeightTerms() {
        var chart = Chart.Create().WithWordCloudAngles(90).WithWordCloudMaximumTerms(1).WithWordCloudFontRange(16, 40)
            .AddWordCloud("Words", new[] { new ChartWordCloudItem("office ffi", 80), new ChartWordCloudItem("Limited", 40), new ChartWordCloudItem("Zero", 0) });
        chart.Series[0].WithPointDataLabelStyle(0, style => style.WithWeight("900").WithItalic().WithTextCase(TextCaseTransform.Uppercase));
        var scene = Compile(chart);
        var text = Assert.Single(scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "word-cloud-text");
        Assert.Equal("OFFICE FFI", Assert.Single(text.Text.Lines).Text);
        var region = Assert.Single(scene.Regions, region => region.Role == "word-cloud-term");
        Assert.Equal(text.Text.Metrics.Height, region.Bounds.Width, 7);
        Assert.Equal(text.Text.Metrics.Width, region.Bounds.Height, 7);
        Assert.True(region.Bounds.Left >= 0 && region.Bounds.Right <= 320 && region.Bounds.Top >= 0 && region.Bounds.Bottom <= 180);
        Assert.Equal(3, scene.Nodes.Count(node => node.Role == "word-cloud-source"));
        Assert.Contains(scene.Regions, region => region.Label == "Zero: 0");
        Assert.Contains(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Rotation?.Degrees == 90);
        Assert.Contains("rotate(90", VisualSceneSvgRenderer.Render(scene));
        Assert.Contains(VisualSceneRasterRenderer.Render(scene).Pixels.Where((_, index) => index % 4 == 3), alpha => alpha > 0);
    }

    [Fact]
    public void CompactWordCloudReportsUnplacedSourceAndPreparedExportsDetachFromMutableInputs() {
        var full = new string('W', 120);
        var chart = Chart.Create().WithWordCloudFontRange(12, 18).AddWordCloud("Words", new[] { new ChartWordCloudItem(full, 8) });
        var scene = Compile(chart, new ChartRect(0, 0, 60, 40));
        Assert.Empty(scene.Nodes.OfType<VisualSceneText>());
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "word-cloud.overflow");
        Assert.Contains(scene.Regions, region => region.Label == full + ": 8");
        var artifact = new PreparedVisual(scene).ToArtifact("words", VisualArtifactKind.Chart);
        Assert.Contains(artifact.Regions, region => region.Label?.Contains(full) == true);
        var pictorial = Pictorial(ChartPictorialShape.Heart, .5).WithPictorialSvgPath("M0 0 H24 V24 H0 Z");
        var prepared = new PreparedVisual(Compile(pictorial)); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        pictorial.Series[0].Points.Clear(); pictorial.Options.PictorialSvgPathData = "M0 0 L24 0 L0 24 Z";
        pictorial.Options.PictorialColumns = 20;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void FunnelZeroSlotsAndRatiosRetainTheirSourceSemanticsAndExplicitStyle() {
        var chart = Chart.Create().WithDataLabels().WithXLabels("Initial", "Empty", "Reopened")
            .AddFunnel("Stages", new[] { new ChartPoint(1, 100), new ChartPoint(2, 0), new ChartPoint(3, 40) });
        var color = ChartColor.FromHex("#2468AC"); chart.Series[0].WithPointColor(0, color);
        var scene = Compile(chart);
        var stages = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "funnel-stage").ToArray();
        Assert.Equal(3, stages.Length); Assert.Equal("0", stages[1].Metadata["data-cfx-value"]);
        Assert.Equal("1", stages[1].Metadata["data-cfx-dropoff"]); Assert.Equal("0.4", stages[2].Metadata["data-cfx-retention"]);
        Assert.Equal("false", stages[2].Metadata["data-cfx-dropoff-defined"]);
        Assert.False(stages[2].Metadata.ContainsKey("data-cfx-dropoff"));
        Assert.Single(scene.Nodes, node => node.Role == "funnel-zero");
        Assert.Equal(color, scene.Nodes.OfType<VisualScenePath>().First(path => path.Role == "funnel-segment").Fill);
        Assert.Contains(scene.Regions, region => region.Role == "funnel-label" && region.Label?.Contains("Empty: 0") == true);
        chart.Series[0].Points[0] = new ChartPoint(1, 0);
        var noBaseline = Compile(chart, new ChartRect(0, 0, 120, 80));
        Assert.All(noBaseline.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "funnel-stage"),
            group => Assert.Equal("false", group.Metadata["data-cfx-retention-defined"]));
        chart.Series[0].ShowDataLabels = false;
        Assert.DoesNotContain(Compile(chart).Nodes, node => node.Role is "funnel-label" or "funnel-ratio");
    }

    private static Chart Pictorial(ChartPictorialShape shape, double value) => Chart.Create().WithPictorialColumns(1)
        .WithPictorialMaximum(1).WithPictorialValues(false).WithPictorialEmptyOpacity(0)
        .AddPictorial("Symbols", new[] { new ChartPictorialItem("", value, ChartColor.FromHex("#DE2738")) }, shape);

    private static VisualScene Compile(Chart chart, ChartRect? bounds = null) {
        var plot = bounds ?? new ChartRect(0, 0, 320, 180); var context = new VisualRenderContext();
        var builder = new VisualSceneBuilder(new VisualSize(plot.Right, plot.Bottom), context.Font);
        VisualSpecialtyCompiler.Build(chart, context, builder, plot); return builder.Build();
    }
}
