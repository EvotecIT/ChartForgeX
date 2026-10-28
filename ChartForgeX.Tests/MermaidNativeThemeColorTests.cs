using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
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
    [InlineData("xychart-beta\nx-axis [Jan, Feb]\ny-axis 0 --> 10\nbar [3, 7]")]
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
            // Pinned topology headings and label fallbacks use textColor; generic primary text remains independent.
            Assert.Equal(ChartColor.FromHex(artifact.Model is TopologyChart ? "#EEDDCC" : "#FFEEDD"), colors.Foreground);
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
        if (artifact.Model is Chart chart) {
            Assert.False(chart.Options.TransparentBackground);
            var pixels = RasterImageDecoder.Decode(png).Pixels;
            Assert.Equal(new byte[] { 0x11, 0x22, 0x33, 0xff }, pixels.Take(4));
        }
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + Colors + body + "\n```");
        Assert.False(fence.HasErrors);
        var fenced = Assert.Single(fence.Artifacts);
        Assert.Equal(svg, fenced.ToSvg());
        Assert.Equal(png, fenced.ToPng());
    }

    [Theory]
    [InlineData("pie\n\"Accepted\" : 3\n\"Declined\" : 1")]
    [InlineData("xychart-beta\nx-axis [Jan, Feb]\ny-axis 0 --> 10\nbar [3, 7]")]
    public void UnauthoredChartBackgroundStaysTransparentAndExplicitHostColorsKeepPrecedence(string body) {
        var ordinary = MermaidRenderer.Render(body);
        Assert.Empty(ordinary.Diagnostics);
        Assert.True(Assert.IsType<Chart>(ordinary.Artifact!.Model).Options.TransparentBackground);
        Assert.Equal(0, RasterImageDecoder.Decode(ordinary.Artifact.ToPng()).Pixels[3]);

        var host = VisualTheme.Graphite().Configure(builder => builder.Dark.Background = ChartColor.FromHex("#010203"));
        var overridden = MermaidRenderer.Render(Colors + body,
            new MermaidRenderOptions { Presentation = new MermaidPresentationOptions { Theme = host } });
        Assert.Empty(overridden.Diagnostics);
        var chart = Assert.IsType<Chart>(overridden.Artifact!.Model);
        Assert.False(chart.Options.TransparentBackground);
        Assert.Equal(ChartColor.FromHex("#010203"), chart.Options.Theme.Resolve(chart.Options.ThemeMode).Background);
        Assert.Equal(new byte[] { 1, 2, 3, 255 }, RasterImageDecoder.Decode(overridden.Artifact.ToPng()).Pixels.Take(4));
        Assert.Equal(ChartColor.FromHex("#010203"), host.Resolve(VisualThemeMode.Dark).Background);
        var fenced = new MermaidVisualMarkupParser(new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { Theme = host }
        }).Parse("```mermaid\n" + Colors + body + "\n```");
        Assert.False(fenced.HasErrors); Assert.Equal(overridden.Artifact.ToPng(), Assert.Single(fenced.Artifacts).ToPng());
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
        Assert.All(Assert.IsType<TopologyChart>(rendered.Artifact.Model).Nodes,
            node => Assert.Equal(ChartColor.FromHex("#EEDDCC"), ChartColor.Parse(node.LabelColor!)));
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + Colors + "flowchart LR\nA --> B\n```");
        Assert.False(fenced.HasErrors); Assert.Equal(rendered.Artifact.ToPng(), Assert.Single(fenced.Artifacts).ToPng());
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
        Assert.Equal(ChartColor.FromHex("#EEDDCC"), colors.Foreground);
        Assert.All(Assert.IsType<TopologyChart>(rendered.Artifact.Model).Nodes,
            node => Assert.Equal(ChartColor.FromHex("#EEDDCC"), ChartColor.Parse(node.LabelColor!)));
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

    [Theory]
    [InlineData("pie\n\"Accepted\" : 3\n\"Declined\" : 1")]
    [InlineData("packet-beta\n0-7: \"Header\"")]
    [InlineData("radar-beta\naxis A,B,C\ncurve current{10,20,30}")]
    public void UnqualifiedDarkModeRemainsLocatedAndUnappliedOutsideSequenceAndTopology(string body) {
        foreach (var preset in new string?[] { null, "base" }) {
            var declaration = preset == null ? "" : "'theme':'" + preset + "',";
            var ordinarySource = preset == null ? body : "%%{init: {" + declaration.TrimEnd(',') + "}}%%\n" + body;
            var source = "%%{init: {" + declaration + "'themeVariables':{'darkMode':true}}}%%\n" + body;
            var ordinary = MermaidRenderer.Render(ordinarySource); var rendered = MermaidRenderer.Render(source);
            Assert.False(ordinary.HasErrors); Assert.False(rendered.HasErrors);
            var unsupported = Assert.Single(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration
                && item.Message.Contains("themeVariables.darkMode", StringComparison.Ordinal));
            Assert.Equal(1, unsupported.Span.Line);
            var retained = Assert.Single(rendered.Document!.Configuration.Settings, item => item.Path == "themeVariables.darkMode");
            Assert.False(retained.IsString); Assert.Equal("true", retained.Value);
            Assert.Equal(ordinary.Artifact!.ToPng(), rendered.Artifact!.ToPng());
            Assert.Equal(ordinary.Diagnostics.Select(item => item.Code + ":" + item.Message),
                rendered.Diagnostics.Where(item => !ReferenceEquals(item, unsupported)).Select(item => item.Code + ":" + item.Message));
            if (preset == null) {
                var rejected = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
                Assert.True(rejected.HasErrors); Assert.Null(rejected.Artifact);
                Assert.Contains(rejected.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
            }
        }
    }

    private static VisualTheme Theme(object model) => model switch {
        Chart chart => chart.Options.Theme,
        TopologyChart topology => topology.Theme,
        SequenceArtifact sequence => sequence.Theme,
        IVisualBlock block => block.Options.Theme,
        _ => throw new InvalidOperationException("The source must use an existing native presentation owner.")
    };
}
