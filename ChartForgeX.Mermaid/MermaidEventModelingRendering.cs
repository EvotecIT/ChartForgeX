using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid Event Modeling diagrams.
/// </summary>
public static class MermaidEventModelingRendering {
    /// <summary>
    /// Converts an Event Modeling document into a renderer-independent ChartForgeX topology chart.
    /// </summary>
    public static TopologyChart ToTopologyChart(this MermaidEventModelingDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var chart = TopologyChart.Create()
            .WithId(string.IsNullOrWhiteSpace(options.Id) ? "mermaid-event-modeling" : options.Id!.Trim())
            .WithTitle(string.IsNullOrWhiteSpace(options.Title) ? "Mermaid Event Modeling" : options.Title!)
            .WithSubtitle(string.IsNullOrWhiteSpace(options.Subtitle) ? document.Header : options.Subtitle!)
            .WithViewport(options.Width, options.Height, options.Padding)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight);

        var groups = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var frame in document.Frames) {
            var lane = LaneTitle(frame);
            if (!groups.ContainsKey(lane)) {
                var groupId = MermaidParserUtilities.StableId("eventmodeling-lane", groups.Count);
                groups.Add(lane, groupId);
                chart.AddAutoGroup(groupId, lane, TopologyHealthStatus.Unknown, subtitle: "swimlane", cssClass: "cfx-mermaid-eventmodeling-lane", symbol: "L");
                chart.Groups[chart.Groups.Count - 1].LayoutPolicy = TopologyGroupLayoutPolicy.Grid;
            }

            chart.AddAutoNode(frame.Number, frame.Name, ToNodeKind(frame), TopologyHealthStatus.Unknown, groupId: groups[lane], subtitle: FrameSubtitle(frame), width: 144, height: 64, symbol: FrameSymbol(frame), cssClass: "cfx-mermaid-eventmodeling-frame");
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.Metadata["mermaid.frame"] = frame.Number;
            node.Metadata["mermaid.entity"] = frame.EntityIdentifier;
            node.Metadata["mermaid.entityKind"] = frame.EntityKind.ToString();
            node.Metadata["mermaid.reset"] = frame.IsReset ? "true" : "false";
            if (!string.IsNullOrWhiteSpace(frame.Namespace)) node.Metadata["mermaid.namespace"] = frame.Namespace!;
            if (!string.IsNullOrWhiteSpace(frame.DataReference)) node.Metadata["mermaid.dataRef"] = frame.DataReference!;
            if (!string.IsNullOrWhiteSpace(frame.DataType)) node.Metadata["mermaid.dataType"] = frame.DataType!;
            if (!string.IsNullOrWhiteSpace(frame.InlineData)) node.Metadata["mermaid.inlineData"] = frame.InlineData!;
        }

        for (var index = 0; index < document.Relations.Count; index++) {
            var relation = document.Relations[index];
            chart.AddEdge("eventmodeling-edge-" + index.ToString(CultureInfo.InvariantCulture), relation.SourceNumber, relation.TargetNumber, relation.IsInferred ? null : "explicit", TopologyEdgeKind.Dependency, TopologyHealthStatus.Unknown, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal);
            chart.Edges[chart.Edges.Count - 1].Metadata["mermaid.inferred"] = relation.IsInferred ? "true" : "false";
        }

        return chart;
    }

    /// <summary>
    /// Wraps an Event Modeling document in a visual artifact envelope backed by a ChartForgeX topology chart.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidEventModelingDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        var topology = document.ToTopologyChart(options);
        var artifact = VisualArtifact.Create(topology.Id ?? "mermaid-event-modeling", VisualArtifactKind.Mermaid, topology);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.Title = topology.Title ?? string.Empty;
        artifact.Subtitle = topology.Subtitle ?? string.Empty;
        artifact.NaturalSize = new VisualArtifactSize(topology.Viewport.Width, topology.Viewport.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.frames"] = document.Frames.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.relations"] = document.Relations.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.dataBlocks"] = document.DataBlocks.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(TopologyChart);
        return artifact;
    }

    /// <summary>Renders an Event Modeling document to static SVG.</summary>
    public static string ToSvg(this MermaidEventModelingDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToSvg();

    /// <summary>Renders an Event Modeling document to static PNG.</summary>
    public static byte[] ToPng(this MermaidEventModelingDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToPng();

    private static string LaneTitle(MermaidEventModelingFrame frame) {
        var lane = BaseLane(frame.EntityKind);
        return string.IsNullOrWhiteSpace(frame.Namespace) ? lane : frame.Namespace + " - " + lane;
    }

    private static string BaseLane(MermaidEventModelingEntityKind kind) {
        if (kind == MermaidEventModelingEntityKind.Command || kind == MermaidEventModelingEntityKind.ReadModel) return "Command/Read Model";
        if (kind == MermaidEventModelingEntityKind.Event) return "Events";
        return "UI/Automation";
    }

    private static TopologyNodeKind ToNodeKind(MermaidEventModelingFrame frame) {
        if (frame.IsReset) return TopologyNodeKind.Gateway;
        if (frame.EntityKind == MermaidEventModelingEntityKind.Command) return TopologyNodeKind.Process;
        if (frame.EntityKind == MermaidEventModelingEntityKind.Event) return TopologyNodeKind.Queue;
        if (frame.EntityKind == MermaidEventModelingEntityKind.ReadModel) return TopologyNodeKind.Database;
        if (frame.EntityKind == MermaidEventModelingEntityKind.Processor) return TopologyNodeKind.Service;
        return TopologyNodeKind.Application;
    }

    private static string? FrameSubtitle(MermaidEventModelingFrame frame) {
        var type = frame.IsReset ? "reset " + frame.EntityKind : frame.EntityKind.ToString();
        if (!string.IsNullOrWhiteSpace(frame.DataReference)) return type + " - [[" + frame.DataReference + "]]";
        if (!string.IsNullOrWhiteSpace(frame.InlineData)) return type + " - data";
        return type;
    }

    private static string? FrameSymbol(MermaidEventModelingFrame frame) {
        if (frame.IsReset) return "R";
        if (frame.EntityKind == MermaidEventModelingEntityKind.Command) return "C";
        if (frame.EntityKind == MermaidEventModelingEntityKind.Event) return "E";
        if (frame.EntityKind == MermaidEventModelingEntityKind.ReadModel) return "V";
        if (frame.EntityKind == MermaidEventModelingEntityKind.Processor) return "P";
        return "UI";
    }
}
