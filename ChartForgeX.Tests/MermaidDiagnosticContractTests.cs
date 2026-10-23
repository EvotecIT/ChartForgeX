using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidDiagnosticContractTests {
    [Theory]
    [InlineData("")]
    [InlineData("flowchart LR\nA -->")]
    [InlineData("pie\n\"A\": nope")]
    [InlineData("timeline\n: orphan")]
    [InlineData("xychart-beta\nline [nope]")]
    [InlineData("sankey-beta\nA,B,nope")]
    [InlineData("radar-beta\ninvalid statement")]
    [InlineData("treemap-beta\n\"A\": nope")]
    [InlineData("gantt\nTask : not-a-date, 1d")]
    public void MalformedNativeSourceHasStableCodesAndCannotBecomeAnArtifact(string source) {
        var parsed = new MermaidParser().Parse(source);
        Assert.True(parsed.HasErrors);
        Assert.All(parsed.Diagnostics, item => Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, item.Code));
        Assert.All(parsed.Diagnostics, item => Assert.True(item.Span.Line > 0 && item.Span.Column > 0));

        var rendered = MermaidRenderer.Render(source);
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        Assert.Equal(parsed.Diagnostics.Select(item => item.Code), rendered.Diagnostics.Select(item => item.Code));
    }

    [Theory]
    [InlineData("C4Context\nPerson(user, \"User\")\nSystem(app, \"Application\")\nRel(user, app, \"Uses\")", "topology")]
    public void DeliberateMappingsRemainRenderableAndExposeTheSameLocatedLimitThroughMarkdown(string source, string limitation) {
        var parsed = new MermaidParser().Parse(source);
        var rendered = MermaidRenderer.Render(source);
        Assert.False(parsed.HasErrors);
        Assert.False(rendered.HasErrors);
        Assert.NotNull(rendered.Artifact);
        var diagnostic = Assert.Single(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ApproximateMapping);
        Assert.Equal(MermaidDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(1, diagnostic.Span.Line);
        Assert.Contains(limitation, diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(parsed.Diagnostics.Select(item => item.Code), rendered.Diagnostics.Select(item => item.Code));

        var fenced = new MermaidVisualMarkupParser().Parse("Before\n\n```mermaid\n" + source + "\n```\n");
        Assert.False(fenced.HasErrors);
        Assert.Single(fenced.Artifacts);
        var mapped = Assert.Single(fenced.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ApproximateMapping);
        Assert.Equal(4, mapped.Line);
        Assert.Equal(diagnostic.Message, mapped.Message);
    }
}
