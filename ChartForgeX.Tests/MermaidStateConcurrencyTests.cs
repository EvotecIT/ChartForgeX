using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects state-region identity and native group ownership without inventing state or transition facts.</summary>
public sealed class MermaidStateConcurrencyTests {
    internal static string Source => File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "state-concurrent-regions.mmd"));

    [Fact]
    public void ParsedRegionsOwnIndependentControlsDirectionsAndNestedCompositeIdentity() {
        var parsed = new MermaidParser().ParseState(Source);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        var active = document.Regions.Where(region => region.CompositeStateId == "Active").ToArray();
        Assert.Equal(new[] { 0, 1 }, active.Select(region => region.Index));
        Assert.Equal(new[] { "TB", "LR" }, active.Select(region => region.Direction));
        Assert.Equal(9, active[1].Span.Line);
        Assert.Equal(active[0].Id, document.States.Single(state => state.Id == "On").RegionId);
        Assert.Equal(active[1].Id, document.States.Single(state => state.Id == "Waiting").RegionId);
        var waiting = Assert.Single(document.Regions, region => region.CompositeStateId == "Waiting");
        Assert.Equal("BT", waiting.Direction);
        Assert.Equal(waiting.Id, document.States.Single(state => state.Id == "Queued").RegionId);
        foreach (var region in document.Regions) {
            var controls = document.States.Where(state => state.RegionId == region.Id && state.Kind is "start" or "end").ToArray();
            Assert.Single(controls, state => state.Kind == "start");
            Assert.Single(controls, state => state.Kind == "end");
        }
        var chart = document.ToTopologyChart();
        Assert.Equal(2, chart.Groups.Count(group => group.Presentation == TopologyGroupPresentation.Compartment));
        Assert.Equal(active[1].Id, chart.Groups.Single(group => group.Id == "Waiting").ParentGroupId);
        Assert.Equal("Waiting", chart.Nodes.Single(node => node.Id == "Queued").GroupId);
        Assert.Equal(document.Transitions.Select(edge => (edge.SourceId, edge.TargetId)), chart.Edges.Select(edge => (edge.SourceNodeId, edge.TargetNodeId)));
        Assert.DoesNotContain(chart.Nodes, node => node.Id is "Active" or "Waiting");
        Assert.Equal("On", Assert.Single(chart.Annotations).TargetId);
    }

    [Fact]
    public void RepeatedInitialAndFinalReferencesShareTheirRegionControlWithoutCrossRegionMerging() {
        var parsed = new MermaidParser().ParseState("stateDiagram-v2\nstate Active {\n[*] --> A\n[*] --> B\nA --> [*]\nB --> [*]\n--\n[*] --> C\nC --> [*]\n}");
        Assert.Empty(parsed.Diagnostics);
        var states = parsed.Document!.States;
        Assert.Equal(2, states.Count(state => state.Kind == "start"));
        Assert.Equal(2, states.Count(state => state.Kind == "end"));
        var edges = parsed.Document.Transitions;
        Assert.Equal(edges[0].SourceId, edges[1].SourceId);
        Assert.Equal(edges[2].TargetId, edges[3].TargetId);
        Assert.NotEqual(edges[0].SourceId, edges[4].SourceId);
        Assert.NotEqual(edges[2].TargetId, edges[5].TargetId);
    }

    [Fact]
    public void EmptyLeadingAdjacentAndTrailingRegionsRemainOrderedWithoutPhantomNodes() {
        var parsed = new MermaidParser().ParseState("stateDiagram-v2\nstate Empty {\n--\n--\n}\nEmpty --> Done");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { 0, 1, 2 }, parsed.Document!.Regions.Select(region => region.Index));
        var chart = parsed.Document.ToTopologyChart();
        Assert.Equal("Done", Assert.Single(chart.Nodes).Id);
        var prepared = chart.Prepare();
        var regions = prepared.ToInterchangeEnvelope().Groups.Where(group => group.Topology!.Presentation == TopologyGroupPresentation.Compartment).ToArray();
        Assert.Equal(3, regions.Length);
        Assert.All(regions, region => { Assert.True(region.Width > 0); Assert.True(region.Height > 0); });
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData("stateDiagram-v2\n--\nA", 2)]
    [InlineData("stateDiagram-v2\nstate Active {\n----\nA\n}", 3)]
    public void InvalidBoundariesReturnLocatedErrorsRatherThanInventingHyphenStates(string source, int line) {
        var parsed = new MermaidParser().ParseState(source);
        Assert.True(parsed.HasErrors);
        Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error && diagnostic.Span.Line == line);
        Assert.DoesNotContain(parsed.Document?.States ?? new List<MermaidStateNode>(), state => state.Id.Trim('-').Length == 0);
    }

    [Fact]
    public void EmptyRegionsConsumeTheSemanticBudgetBeforeNativeGroupExpansion() {
        const string source = "stateDiagram-v2\nstate Empty {\n--\n--\n}";
        var accepted = new MermaidParser().Parse(source, new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 4 } });
        Assert.Empty(accepted.Diagnostics);
        Assert.NotNull(accepted.Document);
        var rejected = new MermaidParser().Parse(source, new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 3 } });
        Assert.Null(rejected.Document);
        Assert.Contains(rejected.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
    }
}
