using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class RequirementThemeRecipeTests {
    [Theory]
    [InlineData("compact", false)] [InlineData("compact", true)]
    [InlineData("wide", false)] [InlineData("wide", true)]
    public void CompletePublicProgramAndFencedDrawingUseTheSameNativeRoleRecipe(string variant, bool dark) {
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var artifact = RequirementThemeRecipe.Create(variant, mode, BrowserProofFonts.Family);
        var program = ExampleRecipeSource.EmitRequirementTheme(variant, mode);
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); var json = artifact.ToInterchangeJson(); var html = artifact.ToHtmlPage();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "requirement-program-" + variant + "-" + dark);
            File.WriteAllText(path + ".Program.cs", program); File.WriteAllText(path + ".expected.svg", svg);
            File.WriteAllBytes(path + ".expected.png", png); File.WriteAllText(path + ".expected.json", json); File.WriteAllText(path + ".expected.html", html);
        }
        Assert.Contains("var artifact=RequirementThemeRecipe.Create", program, StringComparison.Ordinal);
        Assert.DoesNotContain("ChartForgeX.Examples", program, StringComparison.Ordinal);
        Assert.Contains("ToInterchangeJson", program, StringComparison.Ordinal);
        var chart = Assert.IsType<TopologyChart>(artifact.Model); var node = chart.Nodes.Single(item => item.Id == "review");
        Assert.Equal(dark ? "#382535" : "#F5DBE4", node.BackgroundColor);
        Assert.Equal(dark ? "#F9EAF5" : "#28132B", node.LabelColor);
        var imported = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        Assert.Equal(node.BackgroundColor, imported.Nodes.Single(item => item.Id == "review").BackgroundColor);
        Assert.Equal(chart.Edges.Single().LabelColor, imported.Edges.Single().LabelColor);
        Assert.Equal(dark ? "#3B2755" : "#E7F2E9", chart.Edges.Single().LabelBackgroundColor);
        Assert.Equal(chart.Edges.Single().LabelBackgroundColor, imported.Edges.Single().LabelBackgroundColor);
        var parser = new MermaidVisualMarkupParser(new ChartForgeX.Mermaid.MermaidRenderOptions {
            Presentation = new ChartForgeX.Mermaid.MermaidPresentationOptions { ThemeMode = mode, FontFamily = BrowserProofFonts.Family },
            Requirement = new ChartForgeX.Mermaid.MermaidTopologyRenderOptions { Width = variant == "compact" ? 390 : 1100, Height = 640 }
        });
        var parsed = parser.Parse("```mermaid\n" + RequirementThemeRecipe.Source(mode) + "\n```");
        Assert.False(parsed.HasErrors); var fenced = Assert.Single(parsed.Artifacts);
        Assert.Equal(svg, fenced.ToSvg()); Assert.Equal(png, fenced.ToPng());
    }
}
