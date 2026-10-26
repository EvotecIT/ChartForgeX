using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidImplicitTopologyPaletteTests {
    [Theory]
    [InlineData("flowchart", false)] [InlineData("flowchart", true)]
    [InlineData("swimlane", false)] [InlineData("swimlane", true)]
    [InlineData("agentflow", false)] [InlineData("agentflow", true)]
    public void AbsentThemeUsesPinnedDefaultUnlessAnExplicitHostThemeOwnsThePalette(string family, bool hostTheme) {
        var source = MermaidTopologyPaletteRoleTests.Source(family, false, false); var options = MermaidTopologyPaletteRoleTests.Options();
        if (hostTheme) options.Presentation!.Theme = VisualTheme.Graphite().Configure(builder => {
            builder.Light.Surface = ChartColor.FromHex("#102030"); builder.Light.Border = ChartColor.FromHex("#203040");
            builder.Light.Foreground = ChartColor.FromHex("#304050");
        });
        var result = MermaidRenderer.Render(source, options); Capture(family + "-host-" + hostTheme, source, result); Assert.Empty(result.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(result.Artifact); var chart = Assert.IsType<TopologyChart>(artifact.Model);
        var work = Assert.Single(chart.Nodes, node => node.Id == "work"); var group = Assert.Single(chart.Groups); var edge = Assert.Single(chart.Edges);
        Assert.Null(result.Document!.Configuration.Theme); Assert.DoesNotContain(artifact.Metadata, pair => pair.Key == "mermaid.config.theme");
        if (hostTheme) {
            Assert.Equal(ChartColor.FromHex("#102030"), chart.Theme.Resolve(chart.ThemeMode).Surface);
            Assert.Equal(ChartColor.FromHex("#203040"), chart.Theme.Resolve(chart.ThemeMode).Border);
            Assert.Equal(ChartColor.FromHex("#304050"), chart.Theme.Resolve(chart.ThemeMode).Foreground);
            Assert.Null(work.BackgroundColor); Assert.Null(work.BorderColor); Assert.Null(work.LabelColor); Assert.Null(edge.Color);
        } else {
            Color("#ececff", work.BackgroundColor); Color("#9370db", work.BorderColor); Color("#333333", work.LabelColor);
            Color("#aaaa33", group.BorderColor); Color("#333333", group.LabelColor); Color("#333333", edge.Color);
            Assert.Equal(family == "agentflow" ? ChartColor.Transparent : ChartColor.FromHex("#ffffde"), ChartColor.Parse(group.BackgroundColor!));
            Assert.Equal(ChartColor.FromRgba(232, 232, 232, 102), ChartColor.Parse(edge.LabelBackgroundColor!));
        }
        var saved = artifact.ToInterchangeEnvelope(); var restoredChart = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart(); var restored = restoredChart.Prepare(); CaptureArtifact(family + "-host-" + hostTheme + "-restored", restored.Visual.ToArtifact());
        Assert.Equal(artifact.ToPng(), restored.ToPng()); Assert.Equal(work.Label, Assert.Single(restoredChart.Nodes, node => node.Id == "work").Label);
    }
    [Theory] [InlineData("flowchart")] [InlineData("swimlane")] [InlineData("agentflow")]
    public void AbsentAndExplicitDefaultHaveEqualPaintWithoutInventingAnAuthoredThemeFact(string family) {
        var source = MermaidTopologyPaletteRoleTests.Source(family, false, false); var options = MermaidTopologyPaletteRoleTests.Options();
        var implicitResult = MermaidRenderer.Render(source, options); var explicitSource = "%%{init: {\"theme\":\"default\"}}%%\n" + source; var explicitResult = MermaidRenderer.Render(explicitSource, options);
        Capture(family + "-implicit", source, implicitResult); Capture(family + "-explicit-default", explicitSource, explicitResult);
        Assert.Empty(implicitResult.Diagnostics); Assert.Empty(explicitResult.Diagnostics);
        var implicitArtifact = Assert.IsType<VisualArtifact>(implicitResult.Artifact); var explicitArtifact = Assert.IsType<VisualArtifact>(explicitResult.Artifact);
        Assert.Equal(implicitArtifact.ToPng(), explicitArtifact.ToPng()); Assert.Null(implicitResult.Document!.Configuration.Theme); Assert.Equal("default", explicitResult.Document!.Configuration.Theme);
        Assert.DoesNotContain(implicitArtifact.Metadata, pair => pair.Key == "mermaid.config.theme"); Assert.Equal("default", explicitArtifact.Metadata["mermaid.config.theme"]);
    }
    [Theory] [InlineData("flowchart")] [InlineData("swimlane")] [InlineData("agentflow")]
    public void ExplicitRolesAndDirectStylesWinOverTheImplicitSourceSeed(string family) {
        var source = "%%{init: {\"themeVariables\":{\"nodeBorder\":\"#ff0000\",\"mainBkg\":\"#ffff00\"}}}%%\n" + MermaidTopologyPaletteRoleTests.Source(family, false, false)
            + "\nstyle work fill:#102030,stroke:#203040,color:#304050\nlinkStyle 0 stroke:#405060";
        var result = MermaidRenderer.Render(source, MermaidTopologyPaletteRoleTests.Options()); Capture(family + "-precedence", source, result); Assert.Empty(result.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(result.Artifact); var chart = Assert.IsType<TopologyChart>(artifact.Model);
        var work = Assert.Single(chart.Nodes, node => node.Id == "work"); var done = Assert.Single(chart.Nodes, node => node.Id == "done");
        Color("#102030", work.BackgroundColor); Color("#203040", work.BorderColor); Color("#304050", work.LabelColor); Color("#ffff00", done.BackgroundColor); Color("#ff0000", done.BorderColor); Color("#405060", Assert.Single(chart.Edges).Color);
        var restored = artifact.ToInterchangeEnvelope().ToTopologyChart().Prepare(); CaptureArtifact(family + "-precedence-restored", restored.Visual.ToArtifact()); Assert.Equal(artifact.ToPng(), restored.ToPng());
    }
    [Fact]
    public void NativeGraphiteNullPaintFieldsKeepTheirOrdinaryContract() {
        var chart = TopologyChart.Create().WithId("implicit-palette-native-control").WithViewport(400, 300, 24).WithTitle("").WithLegend(null)
            .AddNode("work", "Work", 60, 60, width: 140, height: 70);
        var artifact = chart.ToVisualArtifact(); CaptureArtifact("native-null", artifact); var saved = artifact.ToInterchangeEnvelope(); var restored = saved.ToTopologyChart().Prepare(); CaptureArtifact("native-null-restored", restored.Visual.ToArtifact());
        Assert.Equal(VisualTheme.Graphite().ToJson(), chart.Theme.ToJson()); Assert.Null(chart.Nodes[0].BackgroundColor); Assert.Null(chart.Nodes[0].BorderColor); Assert.Null(chart.Nodes[0].LabelColor); Assert.Equal(artifact.ToPng(), restored.ToPng());
    }
    [Theory]
    [InlineData("flowchart", "compact", VisualThemeMode.Light)] [InlineData("flowchart", "wide", VisualThemeMode.Dark)]
    [InlineData("swimlane", "compact", VisualThemeMode.Light)] [InlineData("swimlane", "wide", VisualThemeMode.Dark)]
    [InlineData("agentflow", "compact", VisualThemeMode.Light)] [InlineData("agentflow", "wide", VisualThemeMode.Dark)]
    public void OrdinaryPaletteRecipeExportsTheSameCapturedGraphAndConsumerSource(string family, string variant, VisualThemeMode mode) {
        var artifact = TopologySourcePaletteRecipe.Create(family, variant, mode, BrowserProofFonts.Family); CaptureArtifact("recipe-" + family + "-" + variant + "-" + mode, artifact);
        var source = ExampleRecipeSource.EmitTopologySourcePalette(family, variant, mode);
        Assert.Contains("TopologySourcePaletteRecipe.Create", source, StringComparison.Ordinal); Assert.Contains("ToInterchangeJson", source, StringComparison.Ordinal);
        var saved = artifact.ToInterchangeEnvelope(); var restored = saved.ToTopologyChart().Prepare(); CaptureArtifact("recipe-" + family + "-" + variant + "-" + mode + "-restored", restored.Visual.ToArtifact());
        Assert.Equal(artifact.ToPng(), restored.ToPng()); Assert.Equal(3, saved.Nodes.Count); Assert.Equal(2, saved.Edges.Count);
    }
    private static void Color(string expected, string? actual) => Assert.Equal(ChartColor.FromHex(expected), ChartColor.Parse(actual!));
    private static void Capture(string name, string source, MermaidRenderResult result) {
        if (Environment.GetEnvironmentVariable("CFX_SOURCE_CONTOUR_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "implicit-source-palette-" + name); File.WriteAllText(stem + ".mmd", source); File.WriteAllText(stem + ".diagnostics.json", JsonSerializer.Serialize(result.Diagnostics));
        }
        if (result.Artifact != null) CaptureArtifact(name, result.Artifact);
    }
    private static void CaptureArtifact(string name, VisualArtifact artifact) {
        if (Environment.GetEnvironmentVariable("CFX_SOURCE_CONTOUR_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, "implicit-source-palette-" + name); File.WriteAllText(stem + ".svg", artifact.ToSvg()); File.WriteAllBytes(stem + ".png", artifact.ToPng()); File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
    }
}
