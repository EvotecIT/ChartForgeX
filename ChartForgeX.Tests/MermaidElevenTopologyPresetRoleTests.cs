using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidElevenTopologyPresetRoleTests {
    [Theory]
    [InlineData("base", "#fff4dd", null, "#333333", null, null)]
    [InlineData("default", "#ececff", "#9370db", "#333333", "#ffffde", "#aaaa33")]
    [InlineData("dark", "#1f2020", "#cccccc", "#cccccc", "#474949", "#ffffff40")]
    [InlineData("forest", "#cde498", "#13540c", "#000000", "#cdffb2", "#6eaa49")]
    [InlineData("neutral", "#eeeeee", "#999999", "#000000", "#fcfcfc", "#707070")]
    [InlineData("neo", "#ffffff", "#000000", "#333333", "#ffffff", null)]
    [InlineData("neo-dark", "#2a2020", "#cccccc", "#e0dfdf", null, null)]
    [InlineData("redux", "#ffffff", "#28253d", "#28253d", "#f9f9fb", "#bdbccc")]
    [InlineData("redux-dark", "#111113", "#ffffff", "#e0dfdf", "#1e1a2e", "#bdbccc")]
    [InlineData("redux-color", "#ffffff", "#28253d", "#28253d", "#f9f9fb", "#bdbccc")]
    [InlineData("redux-dark-color", "#111113", "#ffffff", "#e0dfdf", "#1e1a2e", "#bdbccc")]
    public void EveryNamedPresetResolvesItsLiteralTopologyRolesWithoutClaimingEffectsOrPaletteSlotPaint(string preset, string fill, string? border, string text, string? cluster, string? clusterBorder) {
        var result = Render(preset); Capture(preset + "-direct", result); Assert.False(result.HasErrors);
        var artifact = Assert.IsType<VisualArtifact>(result.Artifact); var chart = Assert.IsType<TopologyChart>(artifact.Model); var node = Assert.Single(chart.Nodes, item => item.Id == "work"); var group = Assert.Single(chart.Groups); var edge = Assert.Single(chart.Edges);
        Color(fill, node.BackgroundColor); Assert.Equal(border == null ? chart.Theme.Resolve(chart.ThemeMode).Border : ChartColor.FromHex(border), ChartColor.Parse(node.BorderColor!)); Color(text, node.LabelColor); Color(text, edge.LabelColor);
        Assert.Equal(cluster == null ? chart.Theme.Resolve(chart.ThemeMode).Neutral2 : ChartColor.FromHex(cluster), ChartColor.Parse(group.BackgroundColor!));
        Assert.NotNull(group.BorderColor); if (clusterBorder != null) Color(clusterBorder, group.BorderColor); Assert.NotNull(group.LabelColor); Assert.NotNull(edge.LabelBackgroundColor); Assert.NotNull(edge.Color);
        var saved = artifact.ToInterchangeEnvelope(); var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart().Prepare(); CaptureArtifact(preset + "-restored", restored.Visual.ToArtifact());
        Assert.Equal(TopologyStaticPaintObservation.Paint(artifact.ToSvg()), TopologyStaticPaintObservation.Paint(restored.ToSvg())); Assert.Equal(artifact.ToPng(), restored.ToPng()); Assert.Equal(preset, result.Document!.Configuration.Theme); Assert.Equal(preset, artifact.Metadata["mermaid.config.theme"]);
        // Literal colors do not complete the named gradient/effect/font/radius policy.
        if (preset is not ("default" or "dark")) Assert.Contains(result.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Warning && item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
    }
    [Theory] [InlineData("flowchart")] [InlineData("swimlane")] [InlineData("agentflow")]
    public void ExplicitHostThemeSuppressesNamedSeedsWhileAuthoredLiteralRolesSurvive(string family) {
        var options = Options(); options.Presentation!.Theme = VisualTheme.Graphite().Configure(builder => { builder.Light.Surface = ChartColor.FromHex("#102030"); builder.Light.Border = ChartColor.FromHex("#203040"); });
        var plain = Render("redux", family: family, options: options); var authored = Render("redux", "\"nodeBorder\":\"#345678\",\"mainBkg\":\"#456789\",\"nodeTextColor\":\"#56789a\"", family, options);
        Capture(family + "-host", plain); Capture(family + "-host-literals", authored); Assert.False(plain.HasErrors); Assert.False(authored.HasErrors);
        var host = Assert.IsType<TopologyChart>(plain.Artifact!.Model); var node = Assert.Single(host.Nodes, item => item.Id == "work"); Assert.Null(node.BorderColor); Assert.Null(node.BackgroundColor); Assert.Null(node.LabelColor); Color("#102030", host.Theme.Resolve(host.ThemeMode).Surface.ToHexRgba());
        var literal = Assert.Single(Assert.IsType<TopologyChart>(authored.Artifact!.Model).Nodes, item => item.Id == "work"); Color("#345678", literal.BorderColor); Color("#456789", literal.BackgroundColor); Color("#56789a", literal.LabelColor);
        if (family == "agentflow") Assert.Equal(ChartColor.Transparent, ChartColor.Parse(Assert.Single(host.Groups).BackgroundColor!));
    }
    [Theory] [InlineData("base")] [InlineData("dark")] [InlineData("redux-dark-color")]
    public void AuthoredRolesAndDirectStylesReplayAfterDerivedNamedAssignments(string preset) {
        var result = Render(preset, "\"nodeBorder\":\"#123456\",\"clusterBorder\":\"#234567\",\"nodeTextColor\":\"#345678\",\"edgeLabelBackground\":\"#45678980\"", style: "\nstyle work fill:#56789a,stroke:#6789ab,color:#789abc\nlinkStyle 0 stroke:#89abcd,color:#9abcde");
        Capture(preset + "-precedence", result); Assert.False(result.HasErrors); var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var work = Assert.Single(chart.Nodes, node => node.Id == "work"); var done = Assert.Single(chart.Nodes, node => node.Id == "done"); Color("#56789a", work.BackgroundColor); Color("#6789ab", work.BorderColor); Color("#789abc", work.LabelColor); Color("#123456", done.BorderColor); Color("#345678", done.LabelColor); Color("#234567", Assert.Single(chart.Groups).BorderColor);
        var edge = Assert.Single(chart.Edges); Color("#89abcd", edge.Color); Color("#9abcde", edge.LabelColor); Color("#45678940", edge.LabelBackgroundColor);
    }
    [Fact]
    public void ExplicitBaseDarkModeDerivesItsLabelBackplateWithoutChangingHostModeOrNodeOverrides() {
        var result = Render("base", "\"secondaryColor\":\"#808080\",\"darkMode\":true,\"nodeBorder\":\"#123456\""); Capture("base-dark-mode", result); Assert.False(result.HasErrors);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); Color("#34343480", Assert.Single(chart.Edges).LabelBackgroundColor); Color("#123456", chart.Nodes[0].BorderColor); Assert.Equal(VisualThemeMode.Light, chart.ThemeMode);
    }
    private static MermaidRenderOptions Options() => new() { Flowchart = new MermaidFlowchartRenderOptions { Width = 640, Height = 440 }, Swimlane = new MermaidFlowchartRenderOptions { Width = 640, Height = 440 }, Agentflow = new MermaidTopologyRenderOptions { Width = 640, Height = 440 }, Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family } };
    private static MermaidRenderResult Render(string preset, string? roles = null, string family = "flowchart", MermaidRenderOptions? options = null, string style = "") {
        var header = family == "flowchart" ? "flowchart LR\nsubgraph scope[Scope]" : family == "swimlane" ? "swimlane-beta LR\nsubgraph scope[Scope]" : "agentflow-beta LR\nflow scope[Scope]";
        return MermaidRenderer.Render("%%{init: {\"theme\":\"" + preset + "\",\"themeVariables\":{" + roles + "}}}%%\n" + header + "\nwork[Work] -->|Route| done[Done]\nend" + style, options ?? Options());
    }
    private static void Color(string expected, string? actual) => Assert.Equal(ChartColor.FromHex(expected), ChartColor.Parse(actual!));
    private static void Capture(string name, MermaidRenderResult result) {
        if (Environment.GetEnvironmentVariable("CFX_SOURCE_CONTOUR_CAPTURE_DIRECTORY") is { Length: > 0 } output) { Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "named-topology-" + name + ".diagnostics.json"), JsonSerializer.Serialize(result.Diagnostics)); }
        if (result.Artifact != null) CaptureArtifact(name, result.Artifact);
    }
    private static void CaptureArtifact(string name, VisualArtifact artifact) {
        if (Environment.GetEnvironmentVariable("CFX_SOURCE_CONTOUR_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, "named-topology-" + name); File.WriteAllText(path + ".svg", artifact.ToSvg()); File.WriteAllBytes(path + ".png", artifact.ToPng()); if (artifact.Model is TopologyChart chart) TopologyPortableFontObservation.Capture("named-topology-" + name, chart.Prepare().Visual); else if (artifact.Model is ChartForgeX.Rendering.PreparedVisual prepared) TopologyPortableFontObservation.Capture("named-topology-" + name, prepared); File.WriteAllText(path + ".json", artifact.ToInterchangeJson());
    }
}
