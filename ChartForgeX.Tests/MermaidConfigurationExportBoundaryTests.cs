using System.Text;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationExportBoundaryTests {
    [Theory]
    [InlineData("\\u0000")]
    [InlineData("\\b")]
    [InlineData("\\f")]
    [InlineData("\\uFFFE")]
    [InlineData("\\uFFFF")]
    public void EffectiveFontNamesRejectCharactersThatCannotBeExportedToXml(string escape) {
        foreach (var declaration in Declarations("fontFamily", "Arial" + escape + "serif")) {
            foreach (var diagram in Diagrams()) {
                var source = declaration + diagram;
                var rendered = MermaidRenderer.Render(source);
                Assert.True(rendered.HasErrors);
                Assert.Null(rendered.Artifact);
                var error = Assert.Single(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
                Assert.True(error.Span.Line > 0 && error.Span.Column > 0);
                var fenced = new MermaidVisualMarkupParser().Parse(Fence(source));
                Assert.True(fenced.HasErrors);
                Assert.Empty(fenced.Artifacts);
            }
        }
    }

    [Theory]
    [InlineData("\\t")]
    [InlineData("\\n")]
    [InlineData("\\r")]
    [InlineData("\\uD83D\\uDE00")]
    public void XmlCompatibleFontCharactersKeepEveryAdvertisedExport(string escape) {
        foreach (var declaration in Declarations("fontFamily", "Arial" + escape + "serif, " + BrowserProofFonts.Family)) {
            foreach (var diagram in Diagrams()) {
                var rendered = MermaidRenderer.Render(declaration + diagram);
                Assert.False(rendered.HasErrors);
                var artifact = Assert.IsType<VisualArtifact>(rendered.Artifact);
                Assert.NotNull(XDocument.Parse(artifact.ToSvg()).Root);
                Assert.NotEmpty(artifact.ToPng());
                Assert.Equal(artifact.ToInterchangeJson(), new UTF8Encoding(false, true).GetString(artifact.ToInterchangeUtf8Json()));
            }
        }
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    public void SpacedDirectiveOpenersRemainOrdinaryComments(string separator) {
        const string diagram = "flowchart LR\nA --> B";
        var source = "%%" + separator + "{init: {'theme':'dark'}}%%\n" + diagram;
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        Assert.Null(rendered.Document!.Configuration.Theme);
        Assert.Empty(rendered.Document.Configuration.Settings);
        Assert.Equal(source, rendered.Document.SourceText);
        var artifact = Assert.IsType<VisualArtifact>(rendered.Artifact);
        Assert.Equal(MermaidRenderer.Render(diagram).Artifact!.ToPng(), artifact.ToPng());
        var fenced = new MermaidVisualMarkupParser().Parse(Fence(source));
        Assert.False(fenced.HasErrors);
        Assert.Equal(artifact.ToPng(), Assert.Single(fenced.Artifacts).ToPng());
    }

    private static IEnumerable<string> Diagrams() {
        yield return "flowchart LR\nA --> B";
        yield return "xychart-beta\nx-axis [A, B]\ny-axis 0 --> 10\nbar [3, 7]";
        yield return "sequenceDiagram\nA->>B: Hello";
        yield return "packet-beta\n0-7: \"Header\"";
    }

    private static IEnumerable<string> Declarations(string key, string value) {
        yield return "---\nconfig:\n  " + key + ": \"" + value + "\"\n---\n";
        yield return "---\nconfig: {\"" + key + "\":\"" + value + "\"}\n---\n";
        yield return "%%{init: {\"" + key + "\":\"" + value + "\"}}%%\n";
        if (key == "fontFamily") {
            yield return "---\nconfig:\n  fontFamily: Arial\n  themeVariables:\n    fontFamily: \"" + value + "\"\n---\n";
            yield return "---\nconfig: {\"fontFamily\":\"Arial\",\"themeVariables\":{\"fontFamily\":\"" + value + "\"}}\n---\n";
            yield return "%%{init: {\"fontFamily\":\"Arial\",\"themeVariables\":{\"fontFamily\":\"" + value + "\"}}}%%\n";
        }
    }

    private static string Fence(string source) => new string((char)96, 3) + "mermaid\n" + source + "\n" + new string((char)96, 3);
}
