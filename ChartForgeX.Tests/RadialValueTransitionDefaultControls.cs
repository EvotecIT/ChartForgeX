using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class RadialValueTransitionDefaultControls {
    [Theory]
    [InlineData(390, false)] [InlineData(390, true)]
    [InlineData(760, false)] [InlineData(760, true)]
    public void LayeredCircleAndProgressRingRecipesKeepExistingNativeAndAssetFreeOutputs(int width, bool dark) {
        var renderer = new HtmlInteractiveChartRenderer();
        foreach (var sample in new[] { ("layered", LayeredRadialRecipe.Create()), ("circle", CircleRecipe.Create()), ("rings", ProgressRingRecipe.Create()) }) {
            var chart = sample.Item2.WithSize(width, 400).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
                .WithFontFamily(BrowserProofFonts.Family);
            var section = renderer.PrepareSection(chart);
            Assert.DoesNotContain("native-motion", section.Html); Assert.DoesNotContain("update-frame", section.Html);
            var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) continue;
            if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Capture output must be absolute.");
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "radial-value-default-" + sample.Item1 + "-" + width + "-" + dark);
            File.WriteAllText(stem + ".html", section.Html); File.WriteAllText(stem + ".svg", section.PreparedVisual.ToSvg());
            File.WriteAllBytes(stem + ".png", section.PreparedVisual.ToPng());
        }
    }
}
