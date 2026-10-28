using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Converts source-ordered Event Modeling facts through native topology swimlanes and measured factual cards.</summary>
public static partial class MermaidEventModelingRendering {
    /// <summary>Converts a timeline to shared swimlane ranks without changing source identities or inferring domain calculations.</summary>
    public static TopologyChart ToTopologyChart(this MermaidEventModelingDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        using var scope = MermaidResourceGuard.BeginIfAbsent(document.CapturedResourceLimits);
        ValidateModel(document); options ??= new MermaidTopologyRenderOptions();
        var chart = TopologyChart.Create().WithId(string.IsNullOrWhiteSpace(options.Id) ? "mermaid-event-modeling" : options.Id!.Trim())
            .WithTitle(MermaidHeading.Title(document, options.Title, document.Title, "Mermaid Event Modeling"))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle)).WithViewport(options.Width, options.Height, options.Padding)
            .WithLayout(TopologyLayoutMode.Swimlane, TopologyLayoutDirection.LeftToRight);
        var render = new TopologyRenderOptions { IncludeLegend = false, IncludeStatusBadges = false, IncludeGroupStatusDots = false,
            WrapNodeLabels = true, AllowMultilineNodeLabels = true, CardSubtitleMode = TopologyCardSubtitleMode.Text,
            CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.Plain, NodeSurfaceStyle = TopologyNodeSurfaceStyle.Card };
        chart.DefaultRenderOptions = render;
        var lanes = document.Frames.Select(frame => new { Frame = frame, Key = MermaidEventModelingParser.LaneKey(frame), Band = MermaidEventModelingParser.Band(frame.EntityKind) })
            .GroupBy(item => item.Key, StringComparer.Ordinal).OrderBy(group => group.First().Band);
        var groups = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var lane in lanes) {
            var frame = lane.First().Frame; var band = lane.First().Band;
            var id = "e" + band.ToString(CultureInfo.InvariantCulture) + (frame.Namespace ?? "");
            var title = band == 0 ? "UI/Automation" : band == 1 ? "Command/Read Model" : "Events";
            if (frame.Namespace != null) title = frame.Namespace + " - " + title;
            chart.AddAutoGroup(id, title, TopologyHealthStatus.Unknown, cssClass: "cfx-mermaid-eventmodeling-lane");
            groups.Add(lane.Key, id); var group = chart.Groups[chart.Groups.Count - 1];
            group.Metadata["mermaid.lane.band"] = band.ToString(CultureInfo.InvariantCulture);
            if (frame.Namespace != null) group.Metadata["mermaid.namespace"] = frame.Namespace;
        }
        for (var index = 0; index < document.Frames.Count; index++) {
            MermaidResourceGuard.Checkpoint(); var frame = document.Frames[index];
            var label = string.IsNullOrEmpty(frame.Name) ? frame.EntityIdentifier.Substring(frame.EntityIdentifier.IndexOf('.') + 1) : frame.Name;
            chart.AddAutoNode(frame.Number, MermaidLabelText.DisplayCaption(label), TopologyNodeKind.Generic, TopologyHealthStatus.Unknown,
                groupId: groups[MermaidEventModelingParser.LaneKey(frame)], subtitle: frame.Number + " · " + frame.EntityKind,
                width: 144, height: 64, cssClass: "cfx-mermaid-eventmodeling-frame");
            var node = chart.Nodes[chart.Nodes.Count - 1]; node.Shape = TopologyNodeShape.Rectangle;
            node.DisplayMode = TopologyNodeDisplayMode.Card; node.FitLabelToContent = true; node.PreserveDisplayModeSize = true;
            node.MaximumLabelCharacters = label.Length; node.ShowStatusBadge = false; node.PaletteIndex = (int)frame.EntityKind;
            if (frame.IsReset) node.Badge = "reset";
            node.Metadata["layer"] = index.ToString(CultureInfo.InvariantCulture);
            node.Metadata["mermaid.frame"] = frame.Number; node.Metadata["mermaid.order"] = index.ToString(CultureInfo.InvariantCulture);
            node.Metadata["mermaid.entity"] = frame.EntityIdentifier; node.Metadata["mermaid.entityKind"] = frame.EntityKind.ToString();
            node.Metadata["mermaid.reset"] = frame.IsReset ? "true" : "false";
            node.Metadata["mermaid.source.label"] = label;
            Source(node.Metadata, frame.Span);
            if (frame.Namespace != null) node.Metadata["mermaid.namespace"] = frame.Namespace;
            if (frame.DataReference != null) node.Metadata["mermaid.dataRef"] = frame.DataReference;
            if (frame.DataType != null) node.Metadata["mermaid.dataType"] = frame.DataType;
            if (frame.InlineData != null) node.Metadata["mermaid.inlineData"] = frame.InlineData;
            var table = FrameData(document, frame);
            foreach (var pair in table.Metadata) node.Metadata[pair.Key] = pair.Value;
            if (table.Rows.Count > VisualArtifactInterchangeValidation.MaximumDetailsPerNode)
                throw new MermaidResourceLimitException("Topology.MaximumDetailsPerNode", VisualArtifactInterchangeValidation.MaximumDetailsPerNode, table.Rows.Count, frame.Span);
            MermaidResourceGuard.ReserveElements(table.Rows.Count, frame.Span);
            foreach (var row in table.Rows) {
                MermaidEventModelingData.CheckPortableFactText(row.Cells[0].DisplayText, frame.Span);
                MermaidEventModelingData.CheckPortableFactLength((long)row.Cells[1].DisplayText.Length + 2 + row.Cells[2].DisplayText.Length, frame.Span);
                var detail = new TopologyNodeDetail { Label = row.Cells[0].DisplayText,
                    Value = row.Cells[1].DisplayText + ": " + row.Cells[2].DisplayText };
                foreach (var pair in row.Metadata) detail.Metadata[pair.Key] = pair.Value;
                detail.Metadata["mermaid.data.kind"] = row.Cells[1].DisplayText; node.Details.Add(detail);
            }
        }
        foreach (var relation in document.Relations) {
            var id = MermaidEventModelingParser.RelationId(relation.SourceNumber, relation.TargetNumber);
            chart.AddEdge(id, relation.SourceNumber, relation.TargetNumber, null, TopologyEdgeKind.Dependency,
                TopologyHealthStatus.Unknown, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal);
            var edge = chart.Edges[chart.Edges.Count - 1]; edge.Metadata["mermaid.inferred"] = relation.IsInferred ? "true" : "false";
            edge.Metadata["mermaid.source.frame"] = relation.SourceNumber; edge.Metadata["mermaid.target.frame"] = relation.TargetNumber;
            Source(edge.Metadata, relation.Span);
        }
        return MermaidPresentation.Apply(chart, document, options.Presentation);
    }

    /// <summary>Creates a portable native topology artifact and preserves captured source export limits.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidEventModelingDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        var topology = document.ToTopologyChart(options); var artifact = topology.ToVisualArtifact(VisualArtifactSourceLanguage.Mermaid);
        artifact.Kind = VisualArtifactKind.Mermaid; artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html | VisualArtifactExportFormat.Json;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString(); artifact.Metadata["mermaid.header"] = document.Header;
        // The complete document remains the source owner; optional portable text respects the common bound.
        VisualArtifactInterchangeMapping.TrySetBoundedExtension(artifact.Metadata, "mermaid.source", document.SourceText);
        artifact.Metadata["mermaid.frames"] = document.Frames.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.relations"] = document.Relations.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.dataBlocks"] = document.DataBlocks.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(TopologyChart);
        return MermaidArtifactPolicy.Attach(MermaidPresentation.Apply(artifact, document), document);
    }
    /// <summary>Renders an Event Modeling document to static SVG.</summary>
    public static string ToSvg(this MermaidEventModelingDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToSvg();
    /// <summary>Renders an Event Modeling document to static PNG.</summary>
    public static byte[] ToPng(this MermaidEventModelingDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToPng();

    private static void ValidateModel(MermaidEventModelingDocument document) {
        if (document.Frames.Any(item => item == null) || document.DataBlocks.Any(item => item == null) || document.Relations.Any(item => item == null))
            throw new ArgumentException("Event Modeling source collections cannot contain null entries.");
        MermaidResourceGuard.ValidateDocument(document);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var frame in document.Frames) {
            MermaidResourceGuard.Checkpoint();
            if (frame == null || !MermaidEventModelingParser.FrameId(frame.Number) || !ids.Add(frame.Number)) throw new ArgumentException("Event Modeling requires unique decimal timeframe identities.");
            if (!Enum.IsDefined(typeof(MermaidEventModelingEntityKind), frame.EntityKind) || frame.EntityKind == MermaidEventModelingEntityKind.Unknown
                || !MermaidEventModelingParser.Identifier(frame.EntityIdentifier, true) || frame.Namespace != null && (!MermaidEventModelingParser.Identifier(frame.Namespace) || frame.Namespace.Length > 510))
                throw new ArgumentException("Event Modeling frames require a supported entity kind and qualified identifier.");
        }
        if (ids.Count == 0) throw new ArgumentException("Event Modeling requires at least one timeframe.");
        var dataIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var block in document.DataBlocks)
            if (block == null || !MermaidEventModelingParser.Identifier(block.Id) || !dataIds.Add(block.Id)) throw new ArgumentException("Event Modeling data-block identities must be valid and unique.");
        foreach (var frame in document.Frames)
            if (frame.DataReference != null && !dataIds.Contains(frame.DataReference)) throw new ArgumentException("Event Modeling data references require existing blocks.");
        var pairs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var relation in document.Relations) {
            if (relation == null || !ids.Contains(relation.SourceNumber) || !ids.Contains(relation.TargetNumber)
                || relation.SourceNumber == relation.TargetNumber || !pairs.Add(relation.SourceNumber + ":" + relation.TargetNumber))
                throw new ArgumentException("Event Modeling relations require unique existing source/target frame pairs.");
        }
    }
    private static void Source(IDictionary<string, string> metadata, MermaidSourceSpan span) {
        metadata["mermaid.source.line"] = span.Line.ToString(CultureInfo.InvariantCulture);
        metadata["mermaid.source.column"] = span.Column.ToString(CultureInfo.InvariantCulture);
        metadata["mermaid.source.length"] = span.Length.ToString(CultureInfo.InvariantCulture);
    }
}
