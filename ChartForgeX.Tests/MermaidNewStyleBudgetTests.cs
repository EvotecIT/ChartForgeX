using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Post-product mutable-AST contracts for the new style collections.</summary>
public sealed class MermaidNewStyleBudgetTests {
    [Theory]
    [InlineData("flowchart-group-styles")]
    [InlineData("flowchart-group-classes")]
    [InlineData("agent-node-styles")]
    [InlineData("agent-flow-styles")]
    [InlineData("agent-edge-styles")]
    [InlineData("agent-node-classes")]
    [InlineData("agent-flow-classes")]
    [InlineData("agent-edge-classes")]
    [InlineData("agent-definition")]
    public void NewStyleCollectionsCannotBypassTheExistingAggregateModelBudget(string target) {
        var source = target.StartsWith("flowchart", StringComparison.Ordinal)
            ? "flowchart LR\nsubgraph scope[Scope]\na --> b\nend"
            : "agentflow-beta LR\nflow scope[Scope]\na --> b\nend";
        var parsed = new MermaidParser().Parse(source);
        MermaidExistingStyleBudgetControlTests.CaptureInput(source, parsed, target);
        if (parsed.Document is MermaidFlowchartDocument flow) {
            for (var i = 0; i < 20; i++) {
                if (target.EndsWith("styles", StringComparison.Ordinal)) flow.Subgraphs[0].Styles["paint-" + i] = "retained";
                else flow.Subgraphs[0].Classes.Add("class-" + i);
            }
        } else {
            var agent = Assert.IsType<MermaidAgentflowDocument>(parsed.Document);
            if (target == "agent-definition") {
                var definition = new MermaidStyleDefinition("marked", agent.HeaderSpan);
                for (var i = 0; i < 20; i++) definition.Styles["paint-" + i] = "retained";
                agent.ClassDefinitions.Add(definition);
            } else {
                var styles = target.StartsWith("agent-node", StringComparison.Ordinal) ? agent.Nodes[0].Styles
                    : target.StartsWith("agent-flow", StringComparison.Ordinal) ? agent.Flows[0].Styles : agent.Edges[0].Styles;
                var classes = target.StartsWith("agent-node", StringComparison.Ordinal) ? agent.Nodes[0].Classes
                    : target.StartsWith("agent-flow", StringComparison.Ordinal) ? agent.Flows[0].Classes : agent.Edges[0].Classes;
                for (var i = 0; i < 20; i++) {
                    if (target.EndsWith("styles", StringComparison.Ordinal)) styles["paint-" + i] = "retained";
                    else classes.Add("class-" + i);
                }
            }
        }
        var rendered = MermaidRenderer.RenderParsed(parsed, MermaidExistingStyleBudgetControlTests.Limit());
        MermaidExistingStyleBudgetControlTests.Capture(rendered, target);
        Assert.True(rendered.HasErrors);
        Assert.Equal(MermaidDiagnosticCodes.ResourceLimitExceeded, Assert.Single(rendered.Diagnostics).Code);
        Assert.Null(rendered.Artifact);
    }
}
