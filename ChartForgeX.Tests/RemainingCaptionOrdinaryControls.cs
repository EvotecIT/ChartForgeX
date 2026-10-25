using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Install and execute before the product patch; compare these exact ordinary bytes after it.</summary>
public sealed class RemainingCaptionOrdinaryControls {
    public static IEnumerable<object[]> Poses() {
        foreach (var family in new[] { "class", "er", "flowchart", "requirement", "native" })
            foreach (var width in new[] { 390, 1100 }) foreach (var dark in new[] { false, true }) yield return new object[] { family, width, dark };
    }
    [Theory]
    [MemberData(nameof(Poses))]
    public void OrdinaryPlainFamilyAndNativeRolesProvideExactSvgPngPortableControls(string family, int width, bool dark) {
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        PreparedVisual visual;
        double? sourcePadding = null;
        if (family == "native") {
            var chart = TopologyChart.Create().WithViewport(width, 620).WithTheme(VisualTheme.Report().WithFontFamily(BrowserProofFonts.Family), mode)
                .AddAutoGroup("scope", "Review scope").AddAutoNode("a", "Review", groupId: "scope")
                .AddAutoNode("b", "Ready", groupId: "scope").AddEdge("link", "a", "b", "Accept work");
            visual = chart.Prepare().Visual;
        } else {
            var source = family switch {
                "class" => "classDiagram\nnamespace Domain {\nclass A[Review]\nclass B[Ready]\nA --> B : Accept work\n}",
                "er" => "erDiagram\nsubgraph Domain[Review scope]\nA[Review]\nB[Ready]\nA ||--o{ B : Accept work\nend",
                "flowchart" => "flowchart TB\nsubgraph Domain[Review scope]\nA[Review] -->|Accept work| B[Ready]\nend",
                _ => "requirementDiagram\nrequirement Review {\nid: R1\ntext: Accept work\nrisk: low\nverifymethod: test\n}\nelement Ready {\ntype: document\n}\nReady - satisfies -> Review"
            };
            var topology = new MermaidTopologyRenderOptions { Width = width, Height = 620 };
            var result = MermaidRenderer.Render(source, new MermaidRenderOptions {
                Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, ThemeMode = mode, TopologySizing = MermaidTopologySizing.Natural },
                Class = topology, EntityRelationship = topology, Requirement = topology,
                Flowchart = new MermaidFlowchartRenderOptions { Width = width, Height = 620 }
            });
            Assert.False(result.HasErrors); Assert.Empty(result.Diagnostics);
            var sourceChart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
            sourcePadding = sourceChart.Viewport.Padding;
            visual = sourceChart.Prepare().Visual;
        }
        var svg = visual.ToSvg(); var png = visual.ToPng(); var json = visual.SemanticInterchange!.ToJson();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "remaining-caption-default-" + family + "-" + width + "-" + dark);
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".png", png); File.WriteAllText(stem + ".json", json);
        }
        if (family == "native") {
            Assert.Equal(2, visual.SemanticInterchange.Version);
            Assert.Null(visual.SemanticInterchange.Topology!.ViewportPadding);
        } else {
            Assert.NotEqual(24, sourcePadding!.Value);
            TopologyViewportPaddingFixtureContract.RequireCapturedInsets(visual.SemanticInterchange, sourcePadding.Value);
        }
        if (family == "native") Assert.Equal((double?)VisualTheme.Report().Typography.TitleSize,
            visual.SemanticInterchange.Presentation!.Theme!.TitleSize);
        Assert.DoesNotContain("labelRuns", json, StringComparison.Ordinal);
    }
}
