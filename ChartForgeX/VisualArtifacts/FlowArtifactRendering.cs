using System;
using System.Globalization;
using ChartForgeX.Topology;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

/// <summary>
/// Provides static preview rendering helpers for flow artifacts.
/// </summary>
public static class FlowArtifactRendering {
    /// <summary>
    /// Converts a flow artifact into a topology chart for deterministic static previews.
    /// </summary>
    /// <param name="flow">The flow artifact.</param>
    /// <returns>A topology chart preview model.</returns>
    public static TopologyChart ToTopologyChart(this FlowArtifact flow) {
        if (flow == null) throw new ArgumentNullException(nameof(flow));
        var chart = TopologyChart.Create()
            .WithId(flow.Id)
            .WithTitle(flow.Title)
            .WithSubtitle(flow.Subtitle)
            .WithViewport(flow.Width, flow.Height, flow.Padding)
            .WithLayout(ToTopologyLayout(flow.LayoutMode), ToVisualLinkDirection(flow.Direction));

        for (var i = 0; i < flow.Lanes.Count; i++) {
            var lane = flow.Lanes[i];
            chart.AddGroup(lane.Id, lane.Label, 0, 0, 280, 180, ToTopologyStatus(lane.Status), color: lane.Color);
        }

        for (var i = 0; i < flow.Steps.Count; i++) {
            var step = flow.Steps[i];
            chart.AddAutoNode(step.Id, step.Label, ToTopologyKind(step.Kind), ToTopologyStatus(step.Status), step.LaneId, step.Subtitle, width: step.Width, height: step.Height, symbol: step.Symbol, color: step.Color, iconId: step.Icon);
            chart.WithNodeDisplay(step.Id, ToDisplay(step.Kind));
            if (!string.IsNullOrWhiteSpace(step.Badge)) chart.WithNodeBadge(step.Id, step.Badge);
        }

        for (var i = 0; i < flow.Connectors.Count; i++) {
            var connector = flow.Connectors[i];
            chart.AddEdge(connector.Id, connector.SourceId, connector.TargetId, connector.Label, ToTopologyKind(connector.Kind), ToTopologyStatus(connector.Status), ToVisualLinkDirection(connector.Direction), TopologyEdgeRouting.Orthogonal, color: connector.Color);
        }

        return chart;
    }

    /// <summary>
    /// Wraps a flow artifact in a product-neutral visual artifact envelope.
    /// </summary>
    /// <param name="flow">The flow artifact.</param>
    /// <param name="sourceLanguage">The source language that produced the flow.</param>
    /// <returns>A visual artifact envelope.</returns>
    public static VisualArtifact ToVisualArtifact(this FlowArtifact flow, VisualArtifactSourceLanguage sourceLanguage = VisualArtifactSourceLanguage.Native) {
        if (flow == null) throw new ArgumentNullException(nameof(flow));
        var artifact = VisualArtifact.Create(flow.Id, VisualArtifactKind.Flow, flow);
        artifact.SourceLanguage = sourceLanguage;
        artifact.Title = flow.Title;
        artifact.Subtitle = flow.Subtitle;
        artifact.NaturalSize = new VisualArtifactSize(flow.Width, flow.Height);
        artifact.ExportFormats = flow.ExportFormats;
        artifact.Metadata["render.model"] = nameof(FlowArtifact);
        artifact.Metadata["render.previewModel"] = nameof(TopologyChart);
        artifact.Metadata["flow.lanes"] = flow.Lanes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["flow.steps"] = flow.Steps.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["flow.connectors"] = flow.Connectors.Count.ToString(CultureInfo.InvariantCulture);
        return artifact;
    }

    /// <summary>
    /// Renders a flow artifact static preview to SVG.
    /// </summary>
    /// <param name="flow">The flow artifact.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this FlowArtifact flow) => flow.ToTopologyChart().ToSvg();

    /// <summary>
    /// Renders a flow artifact static preview to a standalone HTML page.
    /// </summary>
    /// <param name="flow">The flow artifact.</param>
    /// <returns>HTML markup.</returns>
    public static string ToHtmlPage(this FlowArtifact flow) => flow.ToTopologyChart().ToHtmlPage();

    /// <summary>
    /// Renders a flow artifact static preview to PNG.
    /// </summary>
    /// <param name="flow">The flow artifact.</param>
    /// <returns>PNG bytes.</returns>
    public static byte[] ToPng(this FlowArtifact flow) => flow.ToTopologyChart().ToPng();

    private static TopologyLayoutMode ToTopologyLayout(FlowArtifactLayoutMode mode) {
        switch (mode) {
            case FlowArtifactLayoutMode.Dense:
                return TopologyLayoutMode.DenseGrouped;
            case FlowArtifactLayoutMode.Force:
                return TopologyLayoutMode.ForceDirected;
            default:
                return TopologyLayoutMode.Layered;
        }
    }

    private static TopologyLayoutDirection ToVisualLinkDirection(FlowArtifactDirection direction) {
        switch (direction) {
            case FlowArtifactDirection.TopToBottom:
                return TopologyLayoutDirection.TopToBottom;
            case FlowArtifactDirection.RightToLeft:
                return TopologyLayoutDirection.RightToLeft;
            case FlowArtifactDirection.BottomToTop:
                return TopologyLayoutDirection.BottomToTop;
            default:
                return TopologyLayoutDirection.LeftToRight;
        }
    }

    private static VisualLinkDirection ToVisualLinkDirection(VisualLinkDirection direction) {
        switch (direction) {
            case VisualLinkDirection.None:
                return VisualLinkDirection.None;
            case VisualLinkDirection.Backward:
                return VisualLinkDirection.Backward;
            case VisualLinkDirection.Bidirectional:
                return VisualLinkDirection.Bidirectional;
            default:
                return VisualLinkDirection.Forward;
        }
    }

    private static TopologyHealthStatus ToTopologyStatus(VisualStatus status) {
        switch (status) {
            case VisualStatus.Positive:
                return TopologyHealthStatus.Healthy;
            case VisualStatus.Warning:
                return TopologyHealthStatus.Warning;
            case VisualStatus.Negative:
                return TopologyHealthStatus.Critical;
            case VisualStatus.Neutral:
                return TopologyHealthStatus.Disabled;
            default:
                return TopologyHealthStatus.Unknown;
        }
    }

    private static TopologyNodeKind ToTopologyKind(FlowArtifactStepKind kind) {
        switch (kind) {
            case FlowArtifactStepKind.Process:
            case FlowArtifactStepKind.Manual:
                return TopologyNodeKind.Process;
            case FlowArtifactStepKind.Data:
                return TopologyNodeKind.Database;
            case FlowArtifactStepKind.External:
                return TopologyNodeKind.Application;
            case FlowArtifactStepKind.Input:
            case FlowArtifactStepKind.Output:
                return TopologyNodeKind.Queue;
            case FlowArtifactStepKind.Decision:
            case FlowArtifactStepKind.Start:
            case FlowArtifactStepKind.End:
            case FlowArtifactStepKind.Delay:
            case FlowArtifactStepKind.Event:
            case FlowArtifactStepKind.Document:
            default:
                return TopologyNodeKind.Generic;
        }
    }

    private static TopologyEdgeKind ToTopologyKind(FlowArtifactConnectorKind kind) {
        switch (kind) {
            case FlowArtifactConnectorKind.Dependency:
                return TopologyEdgeKind.Dependency;
            case FlowArtifactConnectorKind.Data:
                return TopologyEdgeKind.DataFlow;
            case FlowArtifactConnectorKind.Rejection:
            case FlowArtifactConnectorKind.Retry:
            case FlowArtifactConnectorKind.Error:
            case FlowArtifactConnectorKind.Async:
            default:
                return TopologyEdgeKind.Link;
        }
    }

    private static TopologyNodeDisplayMode ToDisplay(FlowArtifactStepKind kind) {
        switch (kind) {
            case FlowArtifactStepKind.Start:
            case FlowArtifactStepKind.End:
            case FlowArtifactStepKind.Decision:
                return TopologyNodeDisplayMode.Pill;
            default:
                return TopologyNodeDisplayMode.Card;
        }
    }
}
