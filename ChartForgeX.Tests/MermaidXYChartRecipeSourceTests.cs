using ChartForgeX.Themes;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidXYChartRecipeSourceTests {
    [Theory]
    [InlineData("wide-vertical", ThemeMode.Light)] [InlineData("wide-vertical", ThemeMode.Dark)]
    [InlineData("compact-vertical", ThemeMode.Light)] [InlineData("compact-vertical", ThemeMode.Dark)]
    [InlineData("wide-horizontal", ThemeMode.Light)] [InlineData("wide-horizontal", ThemeMode.Dark)]
    [InlineData("compact-horizontal", ThemeMode.Light)] [InlineData("compact-horizontal", ThemeMode.Dark)]
    public void DownloadPackagesTheExactCompiledSourceFactoryAsAnOrdinaryPublicProgram(string variant, ThemeMode mode) {
        var source = ExampleRecipeSource.EmitXYChart(variant, mode); var chart = MermaidXYChartDepthRecipe.Create(variant, mode);
        Assert.Contains("var chart = MermaidXYChartDepthRecipe.Create(\"" + variant + "\", ThemeMode." + mode + ");", source);
        Assert.Contains("public static class MermaidXYChartDepthRecipe", source); Assert.Contains("chart.AddLine(\"Completed\"", source);
        Assert.Contains("chart.SaveSvg", source); Assert.Contains("chart.SavePng", source); Assert.Contains("chart.SaveHtml", source);
        Assert.DoesNotContain("ChartForgeX.Mermaid", source); Assert.DoesNotContain("ParseXYChart", source);
        Assert.DoesNotContain("ChartForgeX.Examples", source); Assert.DoesNotContain("{{", source); Assert.DoesNotContain("FontRegistry.Register", source);
        Assert.Equal(variant.StartsWith("compact", StringComparison.Ordinal) ? 390 : 1100, chart.Options.Size.Width);
        Assert.Equal(new[] { "Opened", "", "Reviewed, ready", "Released" }, chart.Series[1].PointLabels);
        var horizontal = variant.EndsWith("-horizontal", StringComparison.Ordinal);
        var parsed = new MermaidParser().ParseXYChart(XYChartSourceDepthFixture.Read(horizontal));
        Assert.Empty(parsed.Diagnostics);
        var sourceChart = parsed.Document!.ToChart(new MermaidXYChartRenderOptions {
            Width = variant.StartsWith("compact-", StringComparison.Ordinal) ? 390 : 1100, Height = 520,
            Presentation = new MermaidPresentationOptions { ThemeMode = mode }
        });
        // The independent Core builder and actual source lowering must paint identical native scenes.
        Assert.Equal(sourceChart.ToSvg(), chart.ToSvg());
        Assert.Equal(sourceChart.ToPng(), chart.ToPng());
    }
}
