using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FlowArtifactLayoutTests {
    [Theory]
    [InlineData(FlowArtifactDirection.LeftToRight)]
    [InlineData(FlowArtifactDirection.RightToLeft)]
    [InlineData(FlowArtifactDirection.TopToBottom)]
    [InlineData(FlowArtifactDirection.BottomToTop)]
    public void OrderedFlowLanesStayDisjointAndContainTheirStepsWithDirectionalBranchRoutes(FlowArtifactDirection direction) {
        var flow = FlowArtifact.Create("lane-flow").WithSize(900, 600).WithTitle("Assessment workflow")
            .AddLane("service", "Service").AddLane("review", "Review")
            .AddStep("z-start", "Request received", FlowArtifactStepKind.Start, "service")
            .AddStep("y-check", "Automatic checks pass?", FlowArtifactStepKind.Decision, "service")
            .AddStep("x-review", "Review observations", FlowArtifactStepKind.Process, "review")
            .AddStep("a-end", "Assessment complete", FlowArtifactStepKind.End, "service")
            .AddConnector("z-start", "y-check", "Evaluate")
            .AddConnector("y-check", "x-review", "Review required")
            .AddConnector("y-check", "a-end", "Passed")
            .AddConnector("x-review", "a-end", "Accepted")
            .AddConnector("x-review", "y-check", "Retry", FlowArtifactConnectorKind.Retry);
        flow.Direction = direction;
        flow.ConfigureStep("x-review", step => { step.Width = 150; step.Metadata["source.fact"] = "Retain source"; });
        var before = flow.ToVisualArtifact().ToInterchangeJson();
        Assert.Equal(TopologyLayoutMode.Swimlane, flow.ToTopologyChart().LayoutMode);
        var prepared = flow.Prepare(VisualExportRequest.ForFlow(flow).Context);
        var semantics = prepared.SemanticInterchange!;
        var lanes = semantics.Groups.Select(group => new ChartRect(group.X!.Value, group.Y!.Value, group.Width!.Value, group.Height!.Value)).ToArray();
        Assert.Equal(2, lanes.Length);
        Assert.True(lanes[0].Right <= lanes[1].Left || lanes[1].Right <= lanes[0].Left || lanes[0].Bottom <= lanes[1].Top || lanes[1].Bottom <= lanes[0].Top,
            "Separate flow lanes must never overlap or overprint their headers.");
        foreach (var step in semantics.Nodes) {
            var lane = semantics.Groups.Single(group => group.Id == step.GroupId);
            Assert.True(step.X >= lane.X && step.Y >= lane.Y && step.X + step.Width <= lane.X + lane.Width + .001 && step.Y + step.Height <= lane.Y + lane.Height + .001,
                "Every prepared step must remain inside its declared lane.");
        }
        var positions = semantics.Nodes.ToDictionary(node => node.Id, node => Projection(node.X!.Value + node.Width!.Value / 2, node.Y!.Value + node.Height!.Value / 2, direction));
        var ordered = flow.Steps.Select(step => positions[step.Id]).ToArray();
        Assert.True(ordered.Zip(ordered.Skip(1), (first, next) => next > first).All(increases => increases),
            "Source step order must advance in the authored direction instead of infrastructure-kind or lexical order.");
        Assert.Equal(flow.Connectors.Select(connector => (connector.Id, Label: (string?)connector.Label, connector.Direction)),
            semantics.Edges.Select(edge => (edge.Id, edge.Label, edge.Flow!.Direction)));
        foreach (var edge in semantics.Edges) {
            Assert.True(edge.ResolvedRoute.Count >= 2);
            var first = edge.ResolvedRoute[0]; var last = edge.ResolvedRoute[edge.ResolvedRoute.Count - 1];
            var progression = Projection(last.X, last.Y, direction) - Projection(first.X, first.Y, direction);
            Assert.Equal(edge.Label == "Retry", progression < 0);
        }
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "topology-group-label");
        Assert.Equal(before, flow.ToVisualArtifact().ToInterchangeJson());
        Assert.Equal(prepared.ToSvg(), flow.Prepare(VisualExportRequest.ForFlow(flow).Context).ToSvg());
    }

    [Fact]
    public void FlowWithoutLanesStillUsesDeclaredStepOrderAndExplicitAlternativeModesRemainAvailable() {
        var flow = FlowArtifact.Create("ordered-flow").WithSize(600, 260)
            .AddStep("z-start", "Start", FlowArtifactStepKind.Start).AddStep("a-end", "End", FlowArtifactStepKind.End)
            .AddConnector("z-start", "a-end");
        var prepared = flow.Prepare(VisualExportRequest.ForFlow(flow).Context);
        Assert.True(prepared.SemanticInterchange!.Nodes[0].X < prepared.SemanticInterchange.Nodes[1].X);
        flow.AddLane("lane", "Lane"); flow.LayoutMode = FlowArtifactLayoutMode.Dense;
        Assert.Equal(TopologyLayoutMode.DenseGrouped, flow.ToTopologyChart().LayoutMode);
        flow.LayoutMode = FlowArtifactLayoutMode.Force;
        Assert.Equal(TopologyLayoutMode.ForceDirected, flow.ToTopologyChart().LayoutMode);
    }

    private static double Projection(double x, double y, FlowArtifactDirection direction) => direction switch {
        FlowArtifactDirection.RightToLeft => -x,
        FlowArtifactDirection.TopToBottom => y,
        FlowArtifactDirection.BottomToTop => -y,
        _ => x
    };
}
