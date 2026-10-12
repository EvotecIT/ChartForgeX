using System.Text.Json;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFontSizeFunctionSyntaxTests {
    [Theory]
    [InlineData("calc(18px))", false)]
    [InlineData("calc((18px)", false)]
    [InlineData("clamp(12px, min(2vw, 18px), 20px))", false)]
    [InlineData("var(--size) + (18px)", false)]
    [InlineData("env(safe-area-inset-top))", false)]
    [InlineData("min(12px/*)", false)]
    [InlineData("var(--size, \"unterminated)", false)]
    [InlineData("max(12px, 20px) trailing(16px)", false)]
    [InlineData("calc(1rem + 2px)", true)]
    [InlineData("var(--label-size, calc(1rem + 2px))", true)]
    [InlineData("clamp(12px, min(2vw, 18px), 20px)", true)]
    [InlineData("calc(18px /* ) */)", true)]
    public void UnsupportedFunctionsRequireBalancedDelimitersThroughBothSourceForms(string value, bool valid) {
        var quoted = JsonSerializer.Serialize(value);
        foreach (var directive in new[] { false, true }) {
            var source = directive
                ? "%%{init: {\"themeVariables\":{\"fontSize\":" + quoted + "}}}%%\nflowchart LR\nA --> B"
                : "---\nconfig:\n  themeVariables:\n    fontSize: " + quoted + "\n---\nflowchart LR\nA --> B";
            var result = MermaidRenderer.Render(source);
            Assert.Equal(!valid, result.HasErrors);
            Assert.Null(result.Document!.Configuration.FontSize);
            var setting = Assert.Single(result.Document.Configuration.Settings);
            Assert.Equal(value, setting.Value);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(setting.Span, diagnostic.Span);
            Assert.Equal(directive ? 1 : 4, diagnostic.Span.Line);
            Assert.Equal(valid ? MermaidDiagnosticCodes.UnsupportedConfiguration : MermaidDiagnosticCodes.InvalidConfiguration, diagnostic.Code);
            if (valid) Assert.NotNull(result.Artifact);
            else Assert.Null(result.Artifact);
        }
    }
}
