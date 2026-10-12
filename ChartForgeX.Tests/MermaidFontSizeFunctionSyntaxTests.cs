using System.Text.Json;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFontSizeFunctionSyntaxTests {
    [Theory]
    [InlineData("calc(18px))", false)]
    [InlineData("calc((18px)", false)]
    [InlineData("calc([18px)", false)]
    [InlineData("calc({18px)", false)]
    [InlineData("calc(18px])", false)]
    [InlineData("calc(18px})", false)]
    [InlineData("calc([{18px]})", false)]
    [InlineData("var(--size, [calc(18px])", false)]
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
    [InlineData("var(--size, [{18px}])", true)]
    [InlineData("var(--size, \"[{)]}\" /* [{)]} */)", true)]
    [InlineData("var(--size, \\[18px)", true)]
    [InlineData("round(1em, 1px)", true)]
    [InlineData("mod(1em, 3px)", true)]
    [InlineData("rem(1em, 3px)", true)]
    [InlineData("hypot(1em, 2px)", true)]
    [InlineData("abs(-1em)", true)]
    [InlineData("attr(data-size px, 18px)", true)]
    [InlineData("attr(data-size type(<length>), 18px)", true)]
    [InlineData("attr(data-size type(<length>), 18px))", false)]
    [InlineData("var(--size, \"before\\\r\nafter\")", true)]
    [InlineData("var(--size, \"before\\\rafter\")", true)]
    [InlineData("var(--size, \"before\\\nafter\")", true)]
    [InlineData("var(--size, \"before\r\nafter\")", false)]
    [InlineData("calc(1px\\\n + 2px)", false)]
    [InlineData("calc(1px\\\r + 2px)", false)]
    [InlineData("calc(1px\\\r\n + 2px)", false)]
    [InlineData("calc(1px\\\f + 2px)", false)]
    [InlineData("var(--size, \"before\\\fafter\")", true)]
    [InlineData("calc(1px /* before\\\nafter */ + 2px)", true)]
    [InlineData("var(--size, \"\\61\n\")", true)]
    [InlineData("var(--size, \"\\61\r\")", true)]
    [InlineData("var(--size, \"\\61\r\n\")", true)]
    [InlineData("var(--size, \"\\61\f\")", true)]
    [InlineData("var(--size, \"\\000061\n\")", true)]
    [InlineData("var(--size, \"\\0000611\n\")", false)]
    [InlineData("var(--size, \"\\61\n\n\")", false)]
    [InlineData("calc(\\31\npx + 2px)", true)]
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

    [Theory]
    [InlineData("18px", false)]
    [InlineData("abs(-1em)", true)]
    public void LaterScalarSizeSuppressesDiagnosticsForEarlierObjectDescendants(string value, bool warning) {
        var result = MermaidRenderer.Render("---\nconfig:\n  themeVariables:\n    fontSize:\n      nested: 18\n---\n"
            + "%%{init: {\"themeVariables\":{\"fontSize\":" + JsonSerializer.Serialize(value) + "}}}%%\nflowchart LR\nA --> B");
        Assert.False(result.HasErrors);
        Assert.NotNull(result.Artifact);
        Assert.Contains(result.Document!.Configuration.Settings, setting => setting.Path == "themeVariables.fontSize.nested");
        if (warning) {
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, diagnostic.Code);
            Assert.Equal(7, diagnostic.Span.Line);
        } else Assert.Empty(result.Diagnostics);
    }
}
