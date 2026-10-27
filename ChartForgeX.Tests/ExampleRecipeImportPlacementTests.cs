using System.Text.RegularExpressions;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Downloadable top-level programs keep the compiled recipe imports before executable statements.</summary>
public sealed class ExampleRecipeImportPlacementTests {
    [Theory]
    [InlineData("number-culture")]
    [InlineData("quadrant")]
    [InlineData("class-caption")]
    [InlineData("initial-fade")]
    [InlineData("appearance")]
    public void DownloadedProgramsKeepRecipeImportsBeforeTopLevelStatements(string recipe) {
        var source = recipe switch {
            "number-culture" => ExampleRecipeSource.EmitNativeNumberCultureCase("pie", "fr-number", 390, VisualThemeMode.Light),
            "quadrant" => ExampleRecipeSource.Emit(ExampleRecipes.ForKind(ChartForgeX.Core.ChartSeriesKind.Scatter, "interior-quadrants"), "interior-quadrants", "Regions", "Measured axes", VisualThemeMode.Light, 800, 440),
            "initial-fade" => ExampleRecipeSource.EmitInitialFade(ChartForgeX.Core.ChartSeriesKind.Line, 390, VisualThemeMode.Light),
            "appearance" => ExampleRecipeSource.EmitNativeAppearance("bloom", "wide", VisualThemeMode.Light),
            _ => ExampleRecipeSource.EmitRemainingCaptionRoles("class", "wide", VisualThemeMode.Light)
        };
        Assert.Contains("using System.Globalization;", source, StringComparison.Ordinal);
        var firstStatement = source.IndexOf("\nvar ", StringComparison.Ordinal);
        Assert.True(firstStatement >= 0, "The complete downloaded program must retain its executable entry point.");
        Assert.DoesNotMatch(new Regex(@"^using [A-Za-z_][\w.]*;\r?$", RegexOptions.Multiline), source.Substring(firstStatement));
    }
}
