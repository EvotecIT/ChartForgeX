using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

public static partial class V2GalleryModels {
    /// <summary>Creates a fixed comparison grid with shared Cartesian axes.</summary>
    public static ChartGrid CreateGrid() => ChartGrid.Create().WithColumns(2).WithGap(16).WithPanelSize(640, 400)
        .Add(Create(ChartSeriesKind.Line).WithTitle("Observed requests"))
        .Add(Create(ChartSeriesKind.Bar).WithTitle("Reviewed requests"))
        .Add(Create(ChartSeriesKind.Area).WithTitle("Capacity used"), 2).WithSharedAxes();

    /// <summary>Creates groups, status, icon shapes, details and routed links in a native topology.</summary>
    public static TopologyChart CreateTopology(bool atlas = false) {
        var chart = TopologyChart.Create(); chart.Id = atlas ? "shape-atlas" : "service-topology";
        chart.DefaultRenderOptions = new TopologyRenderOptions { FitContentToViewport = true, WrapNodeLabels = true, IncludeLegend = true, IncludeGroupStatusDots = true };
        if (atlas) {
            var shapes = Enum.GetValues<TopologyNodeShape>();
            for (var index = 0; index < shapes.Length; index++) chart.Nodes.Add(new TopologyNode {
                Id = "shape-" + index, Label = shapes[index].ToString(), X = 24 + index % 5 * 160, Y = 24 + index / 5 * 120,
                Width = 132, Height = 86, Shape = shapes[index], PreserveDisplayModeSize = true, ShowStatusBadge = false
            });
            return chart;
        }
        chart.Groups.Add(new TopologyGroup { Id = "primary", Label = "Primary region", X = 0, Y = 0, Width = 340, Height = 300, Status = TopologyHealthStatus.Healthy });
        chart.Groups.Add(new TopologyGroup { Id = "secondary", Label = "Secondary region", X = 390, Y = 0, Width = 340, Height = 300, Status = TopologyHealthStatus.Warning });
        var source = new TopologyNode { Id = "client", Label = "Client service", Subtitle = "Requests", X = 30, Y = 65, Width = 240, Height = 85,
            Kind = TopologyNodeKind.Server, GroupId = "primary", Status = TopologyHealthStatus.Healthy };
        source.Details.Add(new TopologyNodeDetail { Label = "State", Value = "Available", Status = TopologyHealthStatus.Healthy }); chart.Nodes.Add(source);
        chart.Nodes.Add(new TopologyNode { Id = "cache", Label = "Cache", X = 70, Y = 210, Width = 150, Height = 65, Shape = TopologyNodeShape.Cylinder, GroupId = "primary" });
        chart.Nodes.Add(new TopologyNode { Id = "worker", Label = "Processing service", X = 430, Y = 65, Width = 240, Height = 85, Kind = TopologyNodeKind.Server,
            GroupId = "secondary", Status = TopologyHealthStatus.Warning });
        chart.Nodes.Add(new TopologyNode { Id = "store", Label = "Result store", X = 470, Y = 210, Width = 150, Height = 65, Shape = TopologyNodeShape.Cylinder, GroupId = "secondary" });
        chart.Edges.Add(new TopologyEdge { Id = "request", SourceNodeId = "client", TargetNodeId = "worker", Label = "Requests", SecondaryLabel = "Active",
            Routing = TopologyEdgeRouting.ObstacleAvoidingOrthogonal, Direction = VisualLinkDirection.Forward });
        chart.Edges.Add(new TopologyEdge { Id = "cache-read", SourceNodeId = "client", TargetNodeId = "cache", Label = "Lookup", Routing = TopologyEdgeRouting.Curved,
            LineStyle = TopologyEdgeLineStyle.Dashed, Direction = VisualLinkDirection.Bidirectional });
        chart.Edges.Add(new TopologyEdge { Id = "persist", SourceNodeId = "worker", TargetNodeId = "store", Label = "Persist", Routing = TopologyEdgeRouting.Orthogonal });
        chart.Legend = TopologyLegend.Create().AddStatus("Available", TopologyHealthStatus.Healthy).AddStatus("Review", TopologyHealthStatus.Warning)
            .AddNodeKind("Service", TopologyNodeKind.Server).AddEdgeKind("Dependency", TopologyEdgeKind.Dependency, lineStyle: TopologyEdgeLineStyle.Dashed);
        return chart;
    }

    /// <summary>Creates a branched process with typed lanes, decisions and semantic statuses.</summary>
    public static FlowArtifact CreateFlow() => FlowArtifact.Create("assessment-flow").WithSize(900, 600)
        .AddLane("service", "Service").AddLane("review", "Review")
        .AddStep("received", "Request received", FlowArtifactStepKind.Start, "service")
        .AddStep("decision", "Automatic checks pass?", FlowArtifactStepKind.Decision, "service")
        .AddStep("manual", "Review observations", FlowArtifactStepKind.Process, "review", VisualStatus.Warning)
        .AddStep("complete", "Assessment complete", FlowArtifactStepKind.End, "service", VisualStatus.Positive)
        .AddConnector("received", "decision", "Evaluate").AddConnector("decision", "manual", "Review required")
        .AddConnector("decision", "complete", "Passed").AddConnector("manual", "complete", "Accepted");

    /// <summary>Creates actor, control, database and queue notation with nested fragments, notes and activation.</summary>
    public static SequenceArtifact CreateSequence() {
        var model = SequenceArtifact.Create("request-sequence").WithSize(960, 760)
            .AddParticipant("client", "Client", SequenceArtifactParticipantKind.Actor)
            .AddParticipant("service", "Service", SequenceArtifactParticipantKind.Control)
            .AddParticipant("store", "Store", SequenceArtifactParticipantKind.Database)
            .AddParticipant("queue", "Queue", SequenceArtifactParticipantKind.Queue)
            .AddMessage("client", "service", "Submit request")
            .AddMessage("service", "service", "Validate payload")
            .AddMessage("service", "store", "Persist result")
            .AddMessage("service", "queue", "Publish event", kind: SequenceArtifactMessageKind.Async)
            .AddMessage("service", "client", "Return result", SequenceArtifactMessageLineStyle.Dashed, SequenceArtifactMessageKind.Return)
            .AddBlock(SequenceArtifactBlockKind.Alt, "Validation outcome", 1, 4)
            .AddBlock(SequenceArtifactBlockKind.Loop, "Retry storage", 2, 2, depth: 1)
            .AddActivation("service", true, 0).AddActivation("service", false, 5)
            .AddNote(SequenceArtifactNotePlacement.Over, new[] { "service", "store" }, "Durable result");
        model.Notes[0].StepIndex = 2; return model;
    }
}
