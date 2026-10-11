using System.Text;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationUnicodeExportTests {
    [Theory]
    [InlineData("fontFamily", "\\uD800")]
    [InlineData("fontFamily", "\\uDFFF")]
    [InlineData("theme", "\\uD800")]
    [InlineData("theme", "\\uDFFF")]
    [InlineData("layout", "\\uD800")]
    [InlineData("layout", "\\uDFFF")]
    [InlineData("look", "\\uD800")]
    [InlineData("look", "\\uDFFF")]
    public void EffectiveAppearanceRejectsIncompleteUnicodeBeforeCreatingAnExportableArtifact(string key, string escape) {
        foreach (var prefix in Declarations(key, "Arial" + escape + "serif")) {
            var source = prefix + "sequenceDiagram\nA->>B: Hello";
            var parsed = new MermaidParser().ParseSequence(source);
            Assert.True(parsed.HasErrors);
            var error = Assert.Single(parsed.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
            Assert.True(error.Span.Line > 0 && error.Span.Column > 0);
            var rendered = MermaidRenderer.Render(source);
            Assert.True(rendered.HasErrors);
            Assert.Null(rendered.Artifact);
            var markdown = new MermaidVisualMarkupParser().Parse(Fence(source));
            Assert.True(markdown.HasErrors);
            Assert.Empty(markdown.Artifacts);
        }
    }

    [Theory]
    [InlineData("fontFamily", "\\uD83D\\uDE00", "\uD83D\uDE00")]
    [InlineData("layout", "\\uFFFE", "\uFFFE")]
    [InlineData("layout", "\\uFFFF", "\uFFFF")]
    public void CompleteUnicodeAndQuotedNoncharactersRetainStrictJsonAndXmlExports(string key, string escaped, string decoded) {
        foreach (var prefix in Declarations(key, "Arial" + escaped + "serif")) {
            var source = prefix + "sequenceDiagram\nA->>B: Hello";
            var rendered = MermaidRenderer.Render(source);
            Assert.False(rendered.HasErrors);
            Assert.Equal("Arial" + decoded + "serif", key == "fontFamily" ? rendered.Document!.Configuration.FontFamily : rendered.Document!.Configuration.Layout);
            var artifact = Assert.IsType<VisualArtifact>(rendered.Artifact);
            Assert.True((artifact.ExportFormats & VisualArtifactExportFormat.Json) != 0);
            var json = artifact.ToInterchangeJson();
            Assert.Equal(json, new UTF8Encoding(false, true).GetString(artifact.ToInterchangeUtf8Json()));
            Assert.NotNull(XDocument.Parse(artifact.ToSvg()).Root);
            var markdown = new MermaidVisualMarkupParser().Parse(Fence(source));
            Assert.False(markdown.HasErrors);
            var fencedArtifact = Assert.Single(markdown.Artifacts);
            Assert.Equal(fencedArtifact.ToInterchangeJson(), new UTF8Encoding(false, true).GetString(fencedArtifact.ToInterchangeUtf8Json()));
            Assert.NotNull(XDocument.Parse(fencedArtifact.ToSvg()).Root);
        }
    }

    [Fact]
    public void RetainedUnknownAndReplacedValuesDoNotRestrictSourcePreservation() {
        const string source = "---\nconfig:\n  unknown: \"\\uD800\"\n  fontFamily: \"\\uDFFF\"\n---\n"
            + "%%{init: {'fontFamily':'Arial'}}%%\nsequenceDiagram\nA->>B: Hello";
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        Assert.Equal(source, rendered.Document!.SourceText);
        Assert.Equal("Arial", rendered.Document.Configuration.FontFamily);
        var artifact = Assert.IsType<VisualArtifact>(rendered.Artifact);
        Assert.NotEmpty(artifact.ToInterchangeJson());
        Assert.NotEmpty(artifact.ToInterchangeUtf8Json());
    }

    private static string Fence(string source) => new string((char)96, 3) + "mermaid\n" + source + "\n" + new string((char)96, 3);

    private static IEnumerable<string> Declarations(string key, string value) {
        yield return "---\nconfig:\n  " + key + ": \"" + value + "\"\n---\n";
        yield return "---\nconfig: {\"" + key + "\":\"" + value + "\"}\n---\n";
        yield return "%%{init: {\"" + key + "\":\"" + value + "\"}}%%\n";
    }
}
