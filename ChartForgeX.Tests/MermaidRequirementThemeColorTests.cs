using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed partial class MermaidRequirementThemeColorTests {
    [Theory]
    [InlineData("requirementBackground")] [InlineData("requirementBorderColor")]
    [InlineData("requirementTextColor")] [InlineData("relationColor")] [InlineData("relationLabelColor")]
    public void FiniteLiteralRoleColorsReachTheNativeRequirementPaint(string key) {
        var source = "%%{init: {\"themeVariables\":{\"" + key + "\":\"#B71D62\"}}}%%\n" + Body;
        var result = MermaidRenderer.Render(source, Options(1100, false));
        Capture("requirement-color-" + key, result);
        Assert.False(result.HasErrors); Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var node = chart.Nodes.Single(item => item.Id == "R"); var edge = Assert.Single(chart.Edges);
        var paint = key switch { "requirementBackground" => node.BackgroundColor, "requirementBorderColor" => node.Color,
            "requirementTextColor" => node.LabelColor, "relationColor" => edge.Color, _ => edge.LabelColor };
        Assert.Equal("#B71D62", paint);
    }
    [Theory]
    [InlineData(390, false)] [InlineData(390, true)] [InlineData(1100, false)] [InlineData(1100, true)]
    public void UnconfiguredRequirementSourceKeepsItsOrdinaryNativeArtifacts(int width, bool dark) {
        var result = MermaidRenderer.Render(Body, Options(width, dark));
        Capture("requirement-unconfigured-" + width + "-" + dark, result);
        Assert.False(result.HasErrors); Assert.Empty(result.Diagnostics); Assert.NotNull(result.Artifact);
    }
    private static MermaidRenderOptions Options(int width, bool dark) => new() {
        Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light },
        Requirement = new MermaidTopologyRenderOptions { Width = width, Height = 640 }
    };
    private static void Capture(string stem, MermaidRenderResult result) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, stem + ".diagnostics.json"), JsonSerializer.Serialize(result.Diagnostics.Select(item => new { item.Code, item.Message, Line = item.Span.Line, Column = item.Span.Column })));
        if (result.Artifact is { } artifact) {
            File.WriteAllText(Path.Combine(output, stem + ".svg"), artifact.ToSvg());
            File.WriteAllBytes(Path.Combine(output, stem + ".png"), artifact.ToPng());
            File.WriteAllText(Path.Combine(output, stem + ".json"), artifact.ToInterchangeJson());
        }
    }
    private const string Body = "requirementDiagram\nrequirement R {\nid: R1\ntext: Review work\nrisk: low\nverifymethod: test\n}\nelement E {\ntype: document\n}\nE - satisfies -> R";
}
