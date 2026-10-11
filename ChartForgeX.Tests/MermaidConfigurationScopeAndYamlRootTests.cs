using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationScopeAndYamlRootTests {
    [Theory]
    [InlineData("default")]
    [InlineData("42")]
    [InlineData("{nested: value}")]
    public void TreemapSettingsStayInactiveAndPreserveGlobalAppearance(string theme) {
        var source = "---\nconfig:\n  theme: dark\n  layout: elk\n  look: classic\n  treemap:\n    theme: " + theme + "\n    layout: dagre\n    look: handDrawn\n---\ntreemap-beta\n\"Storage\"\n  \"Documents\": 1\n";
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        Assert.NotNull(rendered.Artifact);
        Assert.Equal("dark", rendered.Document!.Configuration.Theme);
        Assert.Equal("elk", rendered.Document.Configuration.Layout);
        Assert.Equal("classic", rendered.Document.Configuration.Look);
        foreach (var key in new[] { "theme", "layout", "look" }) {
            Assert.Contains(rendered.Document.Configuration.Settings, setting => setting.Path == "treemap." + key);
            Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Message.Contains("treemap." + key));
        }
        Assert.Equal(source, rendered.Document.SourceText);
        Assert.True(rendered.Artifact.ToPng().Length > 64);
    }

    [Theory]
    [InlineData("*defaults", "defaults: &defaults\n  theme: dark\n")]
    [InlineData("&defaults", "")]
    [InlineData("&defaults {theme: dark}", "")]
    [InlineData("!custom", "")]
    [InlineData("[dark, default]", "")]
    [InlineData("|", "")]
    [InlineData(">", "")]
    public void UnsupportedRootYamlFormsRetainSourceAndAllowLaterSupportedDirectives(string value, string defaults) {
        var nested = value is "&defaults" or "!custom" or "|" or ">" ? "  theme: default\n" : string.Empty;
        var source = "---\n" + defaults + "config: " + value + "\n" + nested + "title: Retained YAML\n---\n%%{init: {\"theme\":\"dark\"}}%%\nflowchart LR\nA --> B\n";
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        Assert.NotNull(rendered.Artifact);
        Assert.Equal("dark", rendered.Document!.Configuration.Theme);
        Assert.Equal(source, rendered.Document.SourceText);
        Assert.Single(rendered.Document.Configuration.Settings, setting => setting.Path == "theme");
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.UnsupportedConfiguration && diagnostic.Span.Line >= 2);
        Assert.DoesNotContain(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidConfiguration);
        Assert.True(rendered.Artifact.ToPng().Length > 64);
    }
}
