using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class ProgressTransitionDefaultOutputControls {
    [Theory]
    [InlineData(390, false)] [InlineData(390, true)]
    [InlineData(760, false)] [InlineData(760, true)]
    public void ExistingIndependentProgressRecipeKeepsOrdinaryNativeAndAssetFreeOutputs(int width, bool dark) {
        var chart = IndependentProgressRecipe.Create(width == 390 ? "compact" : "wide", dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithFontFamily(BrowserProofFonts.Family);
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart);
        Assert.DoesNotContain("native-motion", section.Html); Assert.DoesNotContain("update-frame", section.Html);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Capture output must be absolute.");
        Directory.CreateDirectory(output); var path = Path.Combine(output, "progress-motion-default-" + width + "-" + dark);
        File.WriteAllText(path + ".html", section.Html); File.WriteAllText(path + ".svg", section.PreparedVisual.ToSvg());
        File.WriteAllBytes(path + ".png", section.PreparedVisual.ToPng());
    }
}
