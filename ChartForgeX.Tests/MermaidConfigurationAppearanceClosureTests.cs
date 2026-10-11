using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationAppearanceClosureTests {
    [Theory]
    [InlineData("theme", "dark")]
    [InlineData("look", "classic")]
    public void UnusableScopedAppearanceFallsBackToTheGlobalValueInDirectAndFencedRendering(string key, string expected) {
        foreach (var directive in new[] { false, true }) {
            var good = directive ? "%%{init: {'" + key + "':'" + expected + "'}}%%\n"
                : "---\nconfig:\n  " + key + ": " + expected + "\n---\n";
            var fallback = directive ? "%%{init: {'" + key + "':'" + expected + "','flowchart':{'" + key + "':'unknown'}}}%%\n"
                : "---\nconfig:\n  " + key + ": " + expected + "\n  flowchart:\n    " + key + ": unknown\n---\n";
            const string body = "flowchart LR\nA[Start] --> B[Done]";
            var result = MermaidRenderer.Render(fallback + body);
            Assert.False(result.HasErrors);
            var artifact = Assert.IsType<VisualArtifact>(result.Artifact);
            Assert.Equal(expected, key == "theme" ? result.Document!.Configuration.Theme : result.Document!.Configuration.Look);
            Assert.Contains(result.Document.Configuration.Settings, setting => setting.Path == "flowchart." + key && setting.Value == "unknown");
            Assert.Contains(result.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Warning && item.Message.Contains("flowchart." + key, StringComparison.Ordinal));
            Assert.Equal(MermaidRenderer.Render(good + body).Artifact!.ToPng(), artifact.ToPng());
            var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + fallback + body + "\n```");
            Assert.False(fenced.HasErrors);
            Assert.Equal(artifact.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
            Assert.Equal(artifact.ToPng(), Assert.Single(fenced.Artifacts).ToPng());
        }
    }

    [Fact]
    public void RecognizedNonNativeScopedThemeRetainsItsWarningAndWinsOverGlobalDark() {
        var result = MermaidRenderer.Render("---\nconfig:\n  theme: dark\n  flowchart:\n    theme: forest\n---\nflowchart LR\nA --> B");
        Assert.False(result.HasErrors);
        Assert.Equal("forest", result.Document!.Configuration.Theme);
        Assert.Contains(result.Diagnostics, item => item.Message.Contains("forest", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("theme")]
    [InlineData("look")]
    public void InvalidScopedValueTypesRemainErrorsBesideAValidGlobalValue(string key) {
        var result = MermaidRenderer.Render("---\nconfig:\n  " + key + ": " + (key == "theme" ? "dark" : "classic") + "\n  flowchart:\n    " + key + ": false\n---\nflowchart LR\nA --> B");
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Span.Line == 5);
    }

    [Theory]
    [InlineData("plain")]
    [InlineData("single")]
    [InlineData("double")]
    [InlineData("flow")]
    public void LiteralNulCannotEnterAnySupportedYamlScalarProjection(string syntax) {
        var value = "Arial\0serif";
        var config = syntax == "flow" ? "config: {\"fontFamily\":\"" + value + "\"}"
            : "config:\n  fontFamily: " + (syntax == "single" ? "'" + value + "'" : syntax == "double" ? "\"" + value + "\"" : value);
        var result = MermaidRenderer.Render("---\n" + config + "\n---\nflowchart LR\nA --> B");
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Span.Line >= 2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void BlankFlowMappingLinesDoNotConsumeTheConfigurationBudget(string indentation) {
        var blankLines = string.Concat(Enumerable.Repeat(indentation + "\n", 65536));
        var source = "---\nconfig: {\n" + blankLines + "\"theme\":\"dark\"\n}\n---\nflowchart LR\nA --> B";
        var result = MermaidRenderer.Render(source);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("dark", result.Document!.Configuration.Theme);
        Assert.Contains(blankLines, result.Document.FrontMatter);
        Assert.NotNull(result.Artifact);
    }
}
