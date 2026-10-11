using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationQuotedKeyErrorTests {
    [Theory]
    [InlineData("\"confi\\qg\"", false)]
    [InlineData("\"confi\\u12GGg\"", false)]
    [InlineData("\"confi\\u12\"", false)]
    [InlineData("\"theme\\q\"", true)]
    [InlineData("\"theme\\u12GG\"", true)]
    [InlineData("\"theme\\u12\"", true)]
    [InlineData("\"unknown\\q\"", true)]
    public void MalformedQuotedMappingKeysProduceLocatedErrorsAndNoArtifact(string key, bool nested) {
        var declaration = nested ? "config:\n  " + key + ": dark" : key + ": {theme: dark}";
        var result = MermaidRenderer.Render("---\n" + declaration + "\n---\nflowchart LR\nA --> B");
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        var error = Assert.Single(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
        Assert.Equal(nested ? 3 : 2, error.Span.Line);
        Assert.Equal(nested ? 3 : 1, error.Span.Column);
    }

    [Theory]
    [InlineData("\"th\\u0065me\"", "dark", null)]
    [InlineData("\"unknown quoted key\"", null, MermaidDiagnosticCodes.UnsupportedConfiguration)]
    public void ValidQuotedMappingKeysRetainSupportedAndUnsupportedBehavior(string key, string? expectedTheme, string? warning) {
        var result = MermaidRenderer.Render("---\nconfig:\n  " + key + ": dark\n---\nflowchart LR\nA --> B");
        Assert.False(result.HasErrors);
        Assert.NotNull(result.Artifact);
        Assert.Equal(expectedTheme, result.Document!.Configuration.Theme);
        if (warning != null) Assert.Contains(result.Diagnostics, item => item.Code == warning);
    }
}
