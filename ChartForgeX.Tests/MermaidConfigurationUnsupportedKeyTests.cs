using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationUnsupportedKeyTests {
    [Theory]
    [InlineData("foo.bar:")]
    [InlineData("[foo, bar]:")]
    [InlineData("123:")]
    [InlineData("foo:bar:")]
    public void UnsupportedMappingKeySubtreesRemainNonfatalAndCannotApplyNestedSettings(string owner) {
        var result = MermaidRenderer.Render("---\nconfig:\n  " + owner + "\n    theme: dark\n    nested:\n      fontFamily: Hidden\n  theme: default\n---\nflowchart LR\nA --> B");
        Assert.False(result.HasErrors);
        Assert.NotNull(result.Artifact);
        Assert.Equal("default", result.Document!.Configuration.Theme);
        Assert.Null(result.Document.Configuration.FontFamily);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration && item.Span.Line == 3);
    }

    [Theory]
    [InlineData("[foo: bar]", "#")]
    [InlineData("[foo: bar]", "")]
    [InlineData("{foo: bar}", "#")]
    [InlineData("{foo: bar}", "")]
    [InlineData("[&name \"foo]: bar\"]", "#")]
    [InlineData("[&name \"foo]: bar\"]", "")]
    [InlineData("[!custom \"foo]: bar\"]", "#")]
    [InlineData("[!<tag:example.com,2026:test> \"foo]: bar\"]", "#")]
    [InlineData("foo[bar]", "#")]
    public void UnsupportedComplexKeyBlockScalarBodiesStillCountTowardThePhysicalBudget(string key, string prefix) {
        var result = MermaidRenderer.Render("---\nconfig:\n  " + key + ": |\n    " + prefix + new string(' ', 70000) +
            "\n  theme: dark\n---\nflowchart LR\nA --> B");
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Message.Contains("65536"));
    }
}
