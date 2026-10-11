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

    [Theory]
    [InlineData("config:\n  - theme: default\n    look: classic\n", "config")]
    [InlineData("config:\n- theme: default\n  look: classic\n- theme: forest\n", "config")]
    [InlineData("config: &defaults\n- theme: default\n  look: classic\n", "config")]
    [InlineData("config:\n  other:\n    - theme: forest\n      look: classic\n  theme: default\n", "other")]
    [InlineData("config:\n  other:\n  - theme: forest\n    look: classic\n  theme: default\n", "other")]
    public void OpaqueBlockSequencesResumeAtTheNextMappingEntry(string yaml, string retainedPath) {
        var source = "---\n" + yaml + "title: Retained sequence\n---\n%%{init: {\"theme\":\"dark\"}}%%\nflowchart LR\nA --> B\n";
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        Assert.NotNull(rendered.Artifact);
        Assert.Equal("dark", rendered.Document!.Configuration.Theme);
        Assert.Equal(source, rendered.Document.SourceText);
        Assert.Single(rendered.Document.Configuration.Settings, setting => setting.Path == retainedPath);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
        Assert.DoesNotContain(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidConfiguration);
        if (retainedPath == "other")
            Assert.Contains(rendered.Document.Configuration.Settings, setting => setting.Path == "theme" && setting.Value == "default");
        Assert.True(rendered.Artifact.ToPng().Length > 64);
    }

    [Theory]
    [InlineData("config:\n-")]
    [InlineData("config: &defaults\n-")]
    public void IndentlessSequenceContentCountsTowardTheConfigurationBudget(string declaration) {
        var source = "---\n" + declaration + " " + new string('x', 70000) + "\n---\nflowchart LR\nA --> B\n";
        var rendered = MermaidRenderer.Render(source);
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidConfiguration && diagnostic.Message.Contains("65536"));
    }

    [Theory]
    [InlineData("metadata")]
    [InlineData("\"quoted metadata\"")]
    public void UnrelatedRootMetadataDoesNotExtendAnOpaqueSequenceBudget(string key) {
        var source = "---\nconfig:\n- theme: forest\n" + key + ": " + new string('x', 70000) + "\n---\n%%{init: {\"theme\":\"dark\"}}%%\nflowchart LR\nA --> B\n";
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        Assert.NotNull(rendered.Artifact);
        Assert.Equal("dark", rendered.Document!.Configuration.Theme);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
    }
}
