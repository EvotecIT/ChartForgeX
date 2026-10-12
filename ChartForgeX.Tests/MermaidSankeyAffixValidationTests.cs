using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSankeyAffixValidationTests {
    [Theory]
    [InlineData("json", "prefix", "\\u0000")]
    [InlineData("json", "suffix", "\\u0000")]
    [InlineData("yaml", "prefix", "\\u0000")]
    [InlineData("yaml", "suffix", "\\u0000")]
    [InlineData("legacy", "prefix", "\\u0000")]
    [InlineData("legacy", "suffix", "\\u0000")]
    [InlineData("json", "prefix", "\\u000b")]
    [InlineData("json", "suffix", "\\ufffe")]
    public void XmlInvalidAffixesFailBeforeAnExportableArtifactIsCreated(string syntax, string key, string encoded) {
        var source = Source(syntax, key, encoded);
        var result = MermaidRenderer.Render(source);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, diagnostic.Code);
        Assert.True(diagnostic.Span.Line > 0);
        Assert.Null(result.Artifact);
        var markup = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(markup.Artifacts);
        Assert.Contains(markup.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("yaml")]
    [InlineData("legacy")]
    public void CompleteUnicodeAndMarkupCharactersRemainLiteralAcrossExports(string syntax) {
        var result = MermaidRenderer.Render(Source(syntax, "prefix", "<b>&\\ud83d\\ude80\\t"));
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        Assert.NotNull(artifact);
        var svg = XDocument.Parse(artifact.ToSvg());
        Assert.Contains(svg.Descendants().Where(node => node.Name.LocalName == "text"),
            node => node.Value.Contains("<b>&🚀", StringComparison.Ordinal));
        Assert.NotEmpty(artifact.ToPng());
    }

    private static string Source(string syntax, string key, string encoded) {
        var configuration = syntax switch {
            "json" => "%%{init: {\"sankey\": {\"" + key + "\": \"" + encoded + "\"}}}%%\n",
            "yaml" => "---\nconfig:\n  sankey:\n    " + key + ": \"" + encoded + "\"\n---\n",
            _ => "%%{init: {'sankey': {'" + key + "': '" + encoded + "'}}}%%\n"
        };
        return configuration + "sankey-beta\nA,B,6";
    }
}
