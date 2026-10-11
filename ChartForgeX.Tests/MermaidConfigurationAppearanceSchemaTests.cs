using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationAppearanceSchemaTests {
    [Theory]
    [InlineData("neo")]
    [InlineData("neo-dark")]
    [InlineData("redux")]
    [InlineData("redux-dark")]
    [InlineData("redux-color")]
    [InlineData("redux-dark-color")]
    public void PinnedModernScopedThemesKeepPrecedenceAndExplicitNativeApproximation(string theme) {
        const string body = "flowchart LR\nA --> B";
        foreach (var legacy in new[] { false, true }) {
            var configuration = legacy ? "%%{init: {'theme':'dark','flowchart':{'theme':'" + theme + "'}}}%%\n"
                : "---\nconfig:\n  theme: dark\n  flowchart:\n    theme: " + theme + "\n---\n";
            var result = MermaidRenderer.Render(configuration + body);
            Assert.False(result.HasErrors);
            Assert.Equal(theme, result.Document!.Configuration.Theme);
            Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration && item.Message.Contains(theme, StringComparison.Ordinal));
            Assert.Equal(MermaidRenderer.Render("%%{init: {'theme':'default'}}%%\n" + body).Artifact!.ToPng(), result.Artifact!.ToPng());
            var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + configuration + body + "\n```");
            Assert.False(fenced.HasErrors);
            Assert.Equal(result.Artifact.ToPng(), Assert.Single(fenced.Artifacts).ToPng());
        }
    }

    [Theory]
    [InlineData("theme", "dark", "false")]
    [InlineData("look", "classic", "false")]
    [InlineData("layout", "dagre", "false")]
    [InlineData("theme", "dark", "''")]
    [InlineData("look", "classic", "null")]
    public void InvalidGlobalAppearanceIsDiagnosedEvenWhenTheActiveFamilyOverridesIt(string key, string scoped, string global) {
        var source = "---\nconfig:\n  " + key + ": " + global + "\n  flowchart:\n    " + key + ": " + scoped + "\n---\nflowchart LR\nA --> B";
        var result = MermaidRenderer.Render(source);
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        var diagnostic = Assert.Single(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
        Assert.Equal(3, diagnostic.Span.Line);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors);
        Assert.Empty(fenced.Artifacts);
    }

    [Fact]
    public void EarlierValuesAtTheSamePathCanStillBeReplacedByLaterDeclarations() {
        var result = MermaidRenderer.Render("---\nconfig:\n  theme: false\n---\n%%{init: {'theme':'dark','flowchart':{'theme':'default'}}}%%\nflowchart LR\nA --> B");
        Assert.False(result.HasErrors);
        Assert.Equal("default", result.Document!.Configuration.Theme);
        Assert.Equal(3, result.Document.Configuration.Settings.Count);
    }

    [Fact]
    public void AppearanceInAnInactiveDiagramScopeRemainsRetainedWithItsDiagnostic() {
        var result = MermaidRenderer.Render("---\nconfig:\n  theme: dark\n  sequence:\n    theme: false\n---\nflowchart LR\nA --> B");
        Assert.False(result.HasErrors);
        Assert.Equal("dark", result.Document!.Configuration.Theme);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration && item.Span.Line == 5);
    }
}
