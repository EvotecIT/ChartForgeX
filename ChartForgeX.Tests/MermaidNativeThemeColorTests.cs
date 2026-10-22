using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidNativeThemeColorTests {
    private const string Colors = "%%{init: {'theme':'dark','themeVariables':{'background':'#112233','textColor':'#EEDDCC',"
        + "'primaryColor':'#334455','primaryTextColor':'#FFEEDD','primaryBorderColor':'#778899',"
        + "'secondaryColor':'#223344','tertiaryColor':'#445566','lineColor':'#CCDDEE'}}}%%\n";

    [Theory]
    [InlineData("flowchart LR\nA[Start] --> B[Finish]")]
    [InlineData("sequenceDiagram\nA->>B: Request\nB-->>A: Response")]
    [InlineData("pie\n\"Accepted\" : 3\n\"Declined\" : 1")]
    [InlineData("packet-beta\n0-7: \"Header\"\n8-15: \"Data\"")]
    public void SourceColorsUseOneNativeThemeForDirectAndFencedSvgAndPng(string body) {
        var rendered = MermaidRenderer.Render(Colors + body);
        Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!;
        var theme = Theme(artifact.Model!);
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var colors = theme.Resolve(mode);
            Assert.Equal(ChartColor.FromHex("#112233"), colors.Background);
            Assert.Equal(ChartColor.FromHex("#334455"), colors.Surface);
            Assert.Equal(ChartColor.FromHex("#FFEEDD"), colors.Foreground);
            Assert.Equal(ChartColor.FromHex("#EEDDCC"), colors.MutedForeground);
            Assert.Equal(ChartColor.FromHex("#778899"), colors.Border);
            Assert.Equal(ChartColor.FromHex("#223344"), colors.ElevatedSurface);
            Assert.Equal(ChartColor.FromHex("#445566"), colors.Neutral2);
            Assert.Equal(ChartColor.FromHex("#CCDDEE"), colors.Axis);
            Assert.Equal(ChartColor.FromHex("#CCDDEE"), colors.Status.Neutral.Fill);
        }
        var svg = artifact.ToSvg();
        Assert.Contains("#112233", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", svg, StringComparison.OrdinalIgnoreCase);
        var png = artifact.ToPng();
        Assert.NotEmpty(png);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + Colors + body + "\n```");
        Assert.False(fence.HasErrors);
        var fenced = Assert.Single(fence.Artifacts);
        Assert.Equal(svg, fenced.ToSvg());
        Assert.Equal(png, fenced.ToPng());
    }

    [Fact]
    public void ExplicitHostAndFamilyThemesOverrideSourceDefaultsWithoutMutatingTheSharedTheme() {
        var shared = VisualTheme.Graphite();
        var host = shared.Configure(builder => builder.Dark.Background = ChartColor.FromHex("#010203"));
        var family = shared.Configure(builder => builder.Dark.Background = ChartColor.FromHex("#040506"));
        var options = new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { Theme = host },
            Flowchart = new MermaidFlowchartRenderOptions { Presentation = new MermaidPresentationOptions { Theme = family } }
        };
        var rendered = MermaidRenderer.Render(Colors + "flowchart LR\nA --> B", options);
        Assert.Empty(rendered.Diagnostics);
        Assert.Equal(ChartColor.FromHex("#040506"), Theme(rendered.Artifact!.Model!).Resolve(VisualThemeMode.Dark).Background);
        Assert.Equal(ChartColor.FromHex("#010203"), host.Resolve(VisualThemeMode.Dark).Background);
        Assert.NotEqual(ChartColor.FromHex("#112233"), shared.Resolve(VisualThemeMode.Dark).Background);
    }

    [Theory]
    [InlineData("'#GGHHII'")]
    [InlineData("'url(https://example.invalid/paint)'")]
    [InlineData("false")]
    [InlineData("{ nested: red }")]
    public void InvalidSupportedColorValuesHaveLocatedErrorsAndNoArtifact(string value) {
        var rendered = MermaidRenderer.Render("---\nconfig:\n  themeVariables:\n    primaryColor: " + value + "\n---\nflowchart LR\nA --> B");
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        var error = Assert.Single(rendered.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, error.Code);
        Assert.Equal(4, error.Span.Line);
        Assert.Contains("primaryColor", error.Message);
    }

    [Fact]
    public void SharedReferenceSourceUsesTheLastDeclarationWithoutDiscardingOtherColorRoles() {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "shared-native-colors.mmd"));
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var colors = Theme(rendered.Artifact!.Model!).Resolve(VisualThemeMode.Light);
        Assert.Equal(ChartColor.FromHex("#112233"), colors.Background);
        Assert.Equal(ChartColor.FromHex("#334455"), colors.Surface);
        Assert.Equal(ChartColor.FromHex("#FFEEDD"), colors.Foreground);
        Assert.Equal(ChartColor.FromHex("#CCDDEE"), colors.Status.Neutral.Fill);
    }

    [Fact]
    public void UnsupportedFamilyColorPropertiesRemainDiagnosedInsteadOfBeingSilentlyConsumed() {
        var rendered = MermaidRenderer.Render("%%{init: {'themeVariables':{'primaryColor':'#123456','unknownPaint':'red'}}}%%\nflowchart LR\nA --> B");
        Assert.False(rendered.HasErrors);
        var warning = Assert.Single(rendered.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, warning.Code);
        Assert.Contains("unknownPaint", warning.Message);
    }

    private static VisualTheme Theme(object model) => model switch {
        Chart chart => chart.Options.Theme,
        TopologyChart topology => topology.Theme,
        SequenceArtifact sequence => sequence.Theme,
        IVisualBlock block => block.Options.Theme,
        _ => throw new InvalidOperationException("The source must use an existing native presentation owner.")
    };
}
