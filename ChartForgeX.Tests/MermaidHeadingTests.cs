using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidHeadingTests {
    [Theory]
    [InlineData("title: \"Report #1\" # weekly", "Report #1")]
    [InlineData("title: 'Editor''s #1' # weekly", "Editor's #1")]
    [InlineData("title: \"Caf\\u00E9\"", "Café")]
    [InlineData("'title': 'Report #1' # monthly", "Report #1")]
    [InlineData("  title: \"Report #1\"\n  config:\n    theme: dark", "Report #1")]
    [InlineData("title: Report#1 # weekly", "Report#1")]
    public void FrontMatterTitlesUseSharedYamlScalarAndRootRules(string frontMatter, string title) {
        foreach (var body in new[] { "flowchart LR\nA --> B", "stateDiagram-v2\n[*] --> Ready",
            "pie\n\"SVG\": 45", "sankey-beta\nData,Charts,10" }) {
            var source = "---\n" + frontMatter + "\n---\n" + body;
            var rendered = MermaidRenderer.Render(source);
            Assert.Empty(rendered.Diagnostics);
            Assert.Equal(title, rendered.Artifact!.Title);
            Assert.Contains(title, Texts(rendered.Artifact.ToSvg()));
            Assert.Equal(source, rendered.Document!.SourceText);
            Assert.Equal(frontMatter, rendered.Document.FrontMatter);
        }
    }

    [Theory]
    [InlineData("Caller title", "Source title", "Front title", "Caller title")]
    [InlineData(null, "Source title", "Front title", "Source title")]
    [InlineData(null, null, "Front title", "Front title")]
    [InlineData(null, null, null, "Mermaid pie")]
    public void HeadingPrecedenceRemainsCallerThenSourceThenFrontMatterThenFamily(string? caller, string? sourceTitle, string? frontMatter, string expected) {
        var source = (frontMatter == null ? "" : "---\ntitle: \"" + frontMatter + "\" # heading\n---\n")
            + "pie" + (sourceTitle == null ? "" : " title " + sourceTitle) + "\n\"SVG\": 45";
        var options = new MermaidRenderOptions { Pie = new MermaidPieRenderOptions { Title = caller } };
        var rendered = MermaidRenderer.Render(source, options);
        Assert.Empty(rendered.Diagnostics);
        Assert.Equal(expected, rendered.Artifact!.Title);
        Assert.Contains(expected, Texts(rendered.Artifact.ToSvg()));
        Assert.Equal(source, rendered.Document!.SourceText);
    }

    [Theory]
    [InlineData("config:\n  theme: dark\n  title: Nested title")]
    [InlineData("config: {\n\"title\": \"Nested title\"\n}")]
    [InlineData("metadata:\n  title: Nested title\nconfig:\n  theme: dark")]
    public void NestedMappingsCannotBecomeTheDiagramHeading(string frontMatter) {
        var source = "---\n" + frontMatter + "\n---\nflowchart LR\nA --> B";
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        Assert.Equal("Mermaid flowchart", rendered.Artifact!.Title);
        Assert.DoesNotContain("Nested title", Texts(rendered.Artifact.ToSvg()));
        Assert.Equal(source, rendered.Document!.SourceText);
        Assert.Equal(frontMatter, rendered.Document.FrontMatter);
    }

    private static IEnumerable<string> Texts(string svg) => XDocument.Parse(svg).Descendants()
        .Where(element => element.Name.LocalName == "text").Select(element => element.Value);
}
