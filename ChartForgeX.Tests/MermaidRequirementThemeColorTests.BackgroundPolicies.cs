using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidRequirementThemeColorTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void HostThemeKeepsExplicitSourceLabelSurfacePaint(bool dark) {
        var options = Options(800, dark);
        options.Presentation!.Theme = VisualTheme.Report().WithFontFamily(BrowserProofFonts.Family);
        var plain = MermaidRenderer.Render(Body, options);
        var configured = MermaidRenderer.Render(LabelBackgroundSource, options);
        Capture("requirement-label-background-host-" + dark, configured);
        Assert.Empty(plain.Diagnostics); Assert.Empty(configured.Diagnostics);
        var source = Assert.IsType<TopologyChart>(plain.Artifact!.Model);
        var mapped = Assert.IsType<TopologyChart>(configured.Artifact!.Model);
        Assert.Equal(source.Theme.ToJson(), mapped.Theme.ToJson());
        Assert.Equal(source.Edges.Single().Color, mapped.Edges.Single().Color);
        Assert.Equal(source.Edges.Single().LabelColor, mapped.Edges.Single().LabelColor);
        Assert.Equal("#193857", mapped.Edges.Single().LabelBackgroundColor);
    }

    [Theory]
    [InlineData("42")] [InlineData("null")] [InlineData("'not-a-color'")]
    public void MalformedLabelSurfaceColorsKeepLocatedConfigurationErrors(string value) {
        var source = "%%{init:{\"themeVariables\":{\"relationLabelBackground\":" + value + "}}}%%\n" + Body;
        var result = MermaidRenderer.Render(source, Options(800, false));
        Capture("requirement-label-background-invalid-" + value.Replace("'", ""), result);
        Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error && item.Span.Line == 1);
    }

    [Fact]
    public void SourceLabelSurfacePaintDetachesAndRemainsEditableAfterPortableRestore() {
        var result = MermaidRenderer.Render(LabelBackgroundSource, Options(800, false));
        Capture("requirement-label-background-source-portable", result);
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var frozen = chart.Prepare(); var svg = frozen.ToSvg(); var png = frozen.ToPng();
        var saved = frozen.ToInterchangeEnvelope(); var json = saved.ToJson();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        Assert.Equal("#193857", chart.Edges.Single().LabelBackgroundColor);
        Assert.Equal("#193857", restored.Edges.Single().LabelBackgroundColor);
        var restoredPrepared = restored.Prepare();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "requirement-label-background-source-portable-restored");
            File.WriteAllText(path + ".svg", restoredPrepared.ToSvg()); File.WriteAllBytes(path + ".png", restoredPrepared.ToPng());
            File.WriteAllText(path + ".json", restoredPrepared.ToInterchangeEnvelope().ToJson());
        }
        Assert.Equal(png, restoredPrepared.ToPng());
        chart.Edges.Single().LabelBackgroundColor = "#F2D8B5";
        saved.Edges.Single().Topology!.LabelBackgroundColor = "#F2D8B5";
        Assert.Equal(svg, frozen.ToSvg()); Assert.Equal(png, frozen.ToPng());
        Assert.Equal(json, frozen.ToInterchangeEnvelope().ToJson());
        restored.Edges.Single().LabelBackgroundColor = "#F2D8B5";
        Assert.NotEqual(svg, restored.Prepare().ToSvg());
    }

    private const string LabelBackgroundSource = "%%{init:{\"themeVariables\":{\"relationLabelBackground\":\"#193857\"}}}%%\n" + Body;
}
