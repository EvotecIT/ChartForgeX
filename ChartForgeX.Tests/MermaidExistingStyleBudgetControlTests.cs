using System.Text.Json;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Passing incumbent-API control, never a semantic before reproduction.</summary>
public sealed class MermaidExistingStyleBudgetControlTests {
    [Fact]
    public void IncumbentFlowchartEdgeStylesAlreadyCountAgainstDirectAstBudget() {
        const string source = "flowchart LR\na --> b";
        var parsed = new MermaidParser().Parse(source);
        CaptureInput(source, parsed, "existing-flowchart-edge-budget-control");
        var document = Assert.IsType<MermaidFlowchartDocument>(parsed.Document);
        for (var i = 0; i < 20; i++) document.Edges[0].Styles["paint-" + i] = "retained";
        var rendered = MermaidRenderer.RenderParsed(parsed, Limit());
        Capture(rendered, "existing-flowchart-edge-budget-control");
        Assert.True(rendered.HasErrors);
        Assert.Equal(MermaidDiagnosticCodes.ResourceLimitExceeded, Assert.Single(rendered.Diagnostics).Code);
        Assert.Null(rendered.Artifact);
    }

    internal static MermaidRenderOptions Limit() => new() {
        ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 10 } }
    };

    internal static void CaptureInput(string source, MermaidParseResult<MermaidDocument> parsed, string name) {
        var output = Output();
        if (output == null) return;
        File.WriteAllText(Path.Combine(output, name + ".input.mmd"), source);
        File.WriteAllText(Path.Combine(output, name + ".parse.json"), JsonSerializer.Serialize(parsed.Diagnostics));
    }

    internal static void Capture(MermaidRenderResult result, string name) {
        var output = Output();
        if (output == null) return;
        File.WriteAllText(Path.Combine(output, name + ".diagnostics.json"), JsonSerializer.Serialize(result.Diagnostics.Select(d =>
            new { d.Code, d.Message, d.Severity, d.Span.Line, d.Span.Column, d.Span.Length })));
    }

    private static string? Output() {
        var output = Environment.GetEnvironmentVariable("CFX_STYLE_BUDGET_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return null;
        Directory.CreateDirectory(output);
        return output;
    }
}
