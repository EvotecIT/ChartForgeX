using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Public resource failure contracts for the shared named-style expansion owner.</summary>
public sealed class MermaidStyleBudgetRegressionTests {
    [Theory]
    [InlineData("flowchart LR", "A")]
    [InlineData("classDiagram", "class A")]
    [InlineData("erDiagram", "A")]
    [InlineData("stateDiagram-v2", "A")]
    [InlineData("requirementDiagram", "element A {\ntype: component\n}")]
    public void CapturedNamedStyleBudgetRejectsTheSameSourceInParserDirectAndFencedHosts(string header, string declaration) {
        // Deliberately bounded: 40 names x 40 inert properties, never the adversarial large review example.
        var names = string.Join(",", Enumerable.Range(0, 40).Select(index => "marked" + index));
        var properties = string.Join(",", Enumerable.Range(0, 40).Select(index => "--p" + index + ":1"));
        var source = header + "\n  classDef " + names + " " + properties + "\n" + declaration;
        var policy = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 2 } };
        var parsed = new MermaidParser().Parse(source, policy);
        Assert.Null(parsed.Document);
        var diagnostic = Assert.Single(parsed.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        Assert.Equal((2, 3), (diagnostic.Span.Line, diagnostic.Span.Column));
        Assert.Contains(nameof(MermaidResourceLimits.MaximumElements), diagnostic.Message);
        var options = new MermaidRenderOptions { ParseOptions = policy };
        var direct = MermaidRenderer.Render(source, options);
        Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts);
        Assert.Contains(fence.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && item.Line == 3 && item.Column == 3);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => new MermaidParser().Parse(source, policy, cancellation.Token));
        Assert.NotNull(new MermaidParser().Parse(header + "\n" + declaration).Document);
    }

    [Theory]
    [InlineData("classDiagram", "class A")]
    [InlineData("erDiagram", "A")]
    [InlineData("stateDiagram-v2", "A")]
    [InlineData("requirementDiagram", "element A {\ntype: component\n}")]
    public void ExactSmallDefinitionBudgetDoesNotCountTheSameSharedCopiesTwice(string header, string declaration) {
        var source = header + "\nclassDef first,second fill:#123456\n" + declaration;
        var parser = new MermaidParser();
        var accepted = parser.Parse(source, new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 6 } });
        Assert.Empty(accepted.Diagnostics); Assert.NotNull(accepted.Document);
        var rejected = parser.Parse(source, new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 5 } });
        Assert.Null(rejected.Document);
        Assert.Single(rejected.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
    }
    [Fact]
    public void FlowchartClassAssignmentFanoutUsesTheSameCapturedElementBudget() {
        var ids = string.Join(",", Enumerable.Range(0, 40).Select(index => "N" + index));
        var classes = string.Join(",", Enumerable.Range(0, 40).Select(index => "marked" + index));
        var source = "flowchart LR\n  class " + ids + " " + classes;
        var limits = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 50 } };
        var parsed = new MermaidParser().Parse(source, limits);
        Assert.Null(parsed.Document);
        var diagnostic = Assert.Single(parsed.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        Assert.Equal((2, 3), (diagnostic.Span.Line, diagnostic.Span.Column));
        var options = new MermaidRenderOptions { ParseOptions = limits };
        var direct = MermaidRenderer.Render(source, options);
        Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
        var accepted = new MermaidParser().Parse(source, new MermaidParseOptions {
            ResourceLimits = new MermaidResourceLimits { MaximumElements = 2000 } });
        Assert.Empty(accepted.Diagnostics);
        var document = Assert.IsType<MermaidFlowchartDocument>(accepted.Document);
        Assert.Equal(40, document.Nodes.Count);
        Assert.All(document.Nodes, node => Assert.Equal(40, node.Classes.Count));
    }

    [Theory]
    [InlineData("flowchart LR")]
    [InlineData("classDiagram")]
    [InlineData("erDiagram")]
    [InlineData("stateDiagram-v2")]
    [InlineData("requirementDiagram")]
    public void DefaultStyleExpansionSharesTheCapturedElementPolicyAcrossExistingNativeFamilies(string header) {
        var declarations = Enumerable.Range(0, 10).Select(index => header switch {
            "classDiagram" => "class N" + index,
            "requirementDiagram" => "element N" + index + " {\ntype: component\n}",
            _ => "N" + index
        });
        var source = header + "\nclassDef default fill:#123456,stroke:#654321,color:#abcdef\n" + string.Join("\n", declarations);
        var limited = new MermaidRenderOptions { ParseOptions = new MermaidParseOptions {
            ResourceLimits = new MermaidResourceLimits { MaximumElements = 20 } } };
        var rejected = MermaidRenderer.Render(source, limited);
        Assert.True(rejected.HasErrors); Assert.Null(rejected.Artifact);
        Assert.Contains(rejected.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && item.Span.Line >= 2);
        var fenced = new MermaidVisualMarkupParser(limited).Parse("```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
        Assert.Contains(fenced.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        var accepted = MermaidRenderer.Render(source, new MermaidRenderOptions { ParseOptions = new MermaidParseOptions {
            ResourceLimits = new MermaidResourceLimits { MaximumElements = 100 } } });
        Assert.Empty(accepted.Diagnostics); Assert.NotNull(accepted.Artifact);
    }

}
