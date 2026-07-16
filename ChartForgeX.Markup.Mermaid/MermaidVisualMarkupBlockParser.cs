using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Markup.Mermaid;

/// <summary>
/// Parses Mermaid Markdown fences into ChartForgeX visual artifacts.
/// </summary>
public sealed partial class MermaidVisualMarkupBlockParser : IVisualMarkupBlockParser {
    private readonly MermaidFlowchartRenderOptions _renderOptions;
    private readonly MermaidSequenceRenderOptions _sequenceRenderOptions;
    private readonly MermaidPieRenderOptions _pieRenderOptions;
    private readonly MermaidJourneyRenderOptions _journeyRenderOptions;
    private readonly MermaidGitGraphRenderOptions _gitGraphRenderOptions;
    private readonly MermaidTimelineRenderOptions _timelineRenderOptions;
    private readonly MermaidQuadrantRenderOptions _quadrantRenderOptions;
    private readonly MermaidXYChartRenderOptions _xyChartRenderOptions;
    private readonly MermaidSankeyRenderOptions _sankeyRenderOptions;
    private readonly MermaidRadarRenderOptions _radarRenderOptions;
    private readonly MermaidTreemapRenderOptions _treemapRenderOptions;
    private readonly MermaidGanttRenderOptions _ganttRenderOptions;
    private readonly MermaidPacketRenderOptions _packetRenderOptions;
    private readonly MermaidBlockRenderOptions _blockRenderOptions;
    private readonly MermaidVennRenderOptions _vennRenderOptions;
    private readonly MermaidIshikawaRenderOptions _ishikawaRenderOptions;
    private readonly MermaidWardleyRenderOptions _wardleyRenderOptions;
    private readonly MermaidTopologyRenderOptions _classRenderOptions;
    private readonly MermaidTopologyRenderOptions _stateRenderOptions;
    private readonly MermaidTopologyRenderOptions _entityRelationshipRenderOptions;
    private readonly MermaidTopologyRenderOptions _requirementRenderOptions;
    private readonly MermaidTopologyRenderOptions _architectureRenderOptions;
    private readonly MermaidTopologyRenderOptions _c4RenderOptions;
    private readonly MermaidTopologyRenderOptions _mindMapRenderOptions;
    private readonly MermaidTopologyRenderOptions _treeViewRenderOptions;
    private readonly MermaidTopologyRenderOptions _eventModelingRenderOptions;
    private readonly MermaidTopologyRenderOptions _kanbanRenderOptions;

    /// <summary>
    /// Initializes a Mermaid visual block parser.
    /// </summary>
    public MermaidVisualMarkupBlockParser() : this(new MermaidVisualMarkupRenderOptions()) {
    }

    /// <summary>
    /// Initializes a Mermaid visual block parser with rendering defaults.
    /// </summary>
    /// <param name="renderOptions">Optional rendering defaults by Mermaid diagram kind.</param>
    public MermaidVisualMarkupBlockParser(MermaidVisualMarkupRenderOptions renderOptions) {
        if (renderOptions == null) throw new ArgumentNullException(nameof(renderOptions));

        _renderOptions = renderOptions.Flowchart == null ? new MermaidFlowchartRenderOptions() : Clone(renderOptions.Flowchart);
        _sequenceRenderOptions = renderOptions.Sequence == null ? new MermaidSequenceRenderOptions() : Clone(renderOptions.Sequence);
        _pieRenderOptions = renderOptions.Pie == null ? new MermaidPieRenderOptions() : Clone(renderOptions.Pie);
        _journeyRenderOptions = renderOptions.Journey == null ? new MermaidJourneyRenderOptions() : Clone(renderOptions.Journey);
        _gitGraphRenderOptions = renderOptions.GitGraph == null ? new MermaidGitGraphRenderOptions() : Clone(renderOptions.GitGraph);
        _timelineRenderOptions = renderOptions.Timeline == null ? new MermaidTimelineRenderOptions() : Clone(renderOptions.Timeline);
        _quadrantRenderOptions = renderOptions.Quadrant == null ? new MermaidQuadrantRenderOptions() : Clone(renderOptions.Quadrant);
        _xyChartRenderOptions = renderOptions.XYChart == null ? new MermaidXYChartRenderOptions() : Clone(renderOptions.XYChart);
        _sankeyRenderOptions = renderOptions.Sankey == null ? new MermaidSankeyRenderOptions() : Clone(renderOptions.Sankey);
        _radarRenderOptions = renderOptions.Radar == null ? new MermaidRadarRenderOptions() : Clone(renderOptions.Radar);
        _treemapRenderOptions = renderOptions.Treemap == null ? new MermaidTreemapRenderOptions() : Clone(renderOptions.Treemap);
        _ganttRenderOptions = renderOptions.Gantt == null ? new MermaidGanttRenderOptions() : Clone(renderOptions.Gantt);
        _packetRenderOptions = renderOptions.Packet == null ? new MermaidPacketRenderOptions() : Clone(renderOptions.Packet);
        _blockRenderOptions = renderOptions.Block == null ? new MermaidBlockRenderOptions() : Clone(renderOptions.Block);
        _vennRenderOptions = renderOptions.Venn == null ? new MermaidVennRenderOptions() : Clone(renderOptions.Venn);
        _ishikawaRenderOptions = renderOptions.Ishikawa == null ? new MermaidIshikawaRenderOptions() : Clone(renderOptions.Ishikawa);
        _wardleyRenderOptions = renderOptions.Wardley == null ? new MermaidWardleyRenderOptions() : Clone(renderOptions.Wardley);
        _classRenderOptions = renderOptions.Class == null ? new MermaidTopologyRenderOptions() : Clone(renderOptions.Class);
        _stateRenderOptions = renderOptions.State == null ? new MermaidTopologyRenderOptions() : Clone(renderOptions.State);
        _entityRelationshipRenderOptions = renderOptions.EntityRelationship == null ? new MermaidTopologyRenderOptions() : Clone(renderOptions.EntityRelationship);
        _requirementRenderOptions = renderOptions.Requirement == null ? new MermaidTopologyRenderOptions() : Clone(renderOptions.Requirement);
        _architectureRenderOptions = renderOptions.Architecture == null ? new MermaidTopologyRenderOptions() : Clone(renderOptions.Architecture);
        _c4RenderOptions = renderOptions.C4 == null ? new MermaidTopologyRenderOptions() : Clone(renderOptions.C4);
        _mindMapRenderOptions = renderOptions.MindMap == null ? new MermaidTopologyRenderOptions() : Clone(renderOptions.MindMap);
        _treeViewRenderOptions = renderOptions.TreeView == null ? new MermaidTopologyRenderOptions() : Clone(renderOptions.TreeView);
        _eventModelingRenderOptions = renderOptions.EventModeling == null ? new MermaidTopologyRenderOptions() : Clone(renderOptions.EventModeling);
        _kanbanRenderOptions = renderOptions.Kanban == null ? new MermaidTopologyRenderOptions() : Clone(renderOptions.Kanban);
    }

    /// <inheritdoc />
    public bool CanParse(VisualMarkupBlock block) => block != null && block.Kind == VisualMarkupKind.Mermaid;

    /// <inheritdoc />
    public void Parse(VisualMarkupBlock block, VisualMarkupParseResult result) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        if (result == null) throw new ArgumentNullException(nameof(result));

        var mermaidResult = new MermaidParser().Parse(block.Payload);
        foreach (var diagnostic in mermaidResult.Diagnostics) {
            result.Diagnostics.Add(new MarkupDiagnostic {
                Line = diagnostic.Span.Line <= 0 ? block.FenceLine : block.StartLine + diagnostic.Span.Line - 1,
                Severity = diagnostic.Severity,
                Message = diagnostic.Message
            });
        }

        if (mermaidResult.HasErrors || mermaidResult.Document == null) return;
        try {
        if (mermaidResult.Document is MermaidClassDocument classDiagram) {
            AddTopologyArtifact(result, block, classDiagram.ToVisualArtifact(BuildTopologyOptions(block, _classRenderOptions)));
            return;
        }

        if (mermaidResult.Document is MermaidStateDocument stateDiagram) {
            AddTopologyArtifact(result, block, stateDiagram.ToVisualArtifact(BuildTopologyOptions(block, _stateRenderOptions)));
            return;
        }

        if (mermaidResult.Document is MermaidEntityRelationshipDocument erDiagram) {
            AddTopologyArtifact(result, block, erDiagram.ToVisualArtifact(BuildTopologyOptions(block, _entityRelationshipRenderOptions)));
            return;
        }

        if (mermaidResult.Document is MermaidRequirementDocument requirement) {
            AddTopologyArtifact(result, block, requirement.ToVisualArtifact(BuildTopologyOptions(block, _requirementRenderOptions)));
            return;
        }

        if (mermaidResult.Document is MermaidArchitectureDocument architecture) {
            AddTopologyArtifact(result, block, architecture.ToVisualArtifact(BuildTopologyOptions(block, _architectureRenderOptions)));
            return;
        }

        if (mermaidResult.Document is MermaidC4Document c4) {
            AddTopologyArtifact(result, block, c4.ToVisualArtifact(BuildTopologyOptions(block, _c4RenderOptions)));
            return;
        }

        if (mermaidResult.Document is MermaidMindMapDocument mindMap) {
            AddTopologyArtifact(result, block, mindMap.ToVisualArtifact(BuildTopologyOptions(block, _mindMapRenderOptions)));
            return;
        }

        if (mermaidResult.Document is MermaidTreeViewDocument treeView) {
            AddTopologyArtifact(result, block, treeView.ToVisualArtifact(BuildTopologyOptions(block, _treeViewRenderOptions)));
            return;
        }

        if (mermaidResult.Document is MermaidEventModelingDocument eventModeling) {
            AddTopologyArtifact(result, block, eventModeling.ToVisualArtifact(BuildTopologyOptions(block, _eventModelingRenderOptions)));
            return;
        }

        if (mermaidResult.Document is MermaidKanbanDocument kanban) {
            AddTopologyArtifact(result, block, kanban.ToVisualArtifact(BuildTopologyOptions(block, _kanbanRenderOptions)));
            return;
        }

        if (mermaidResult.Document is MermaidFlowchartDocument flowchart) {
            var options = BuildOptions(block);
            var artifact = flowchart.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(TopologyChart);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidSequenceDocument sequence) {
            var options = BuildSequenceOptions(block);
            var artifact = sequence.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(SequenceArtifact);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidPieDocument pie) {
            var options = BuildPieOptions(block);
            var artifact = pie.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(Chart);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidJourneyDocument journey) {
            var options = BuildJourneyOptions(block);
            var artifact = journey.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(Chart);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidGitGraphDocument gitGraph) {
            var options = BuildGitGraphOptions(block);
            var artifact = gitGraph.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(GitGraphBlock);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidTimelineDocument timeline) {
            var options = BuildTimelineOptions(block);
            var artifact = timeline.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(Chart);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidQuadrantDocument quadrant) {
            var options = BuildQuadrantOptions(block);
            var artifact = quadrant.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(Chart);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidXYChartDocument xyChart) {
            var options = BuildXYChartOptions(block);
            var artifact = xyChart.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(Chart);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidSankeyDocument sankey) {
            var options = BuildSankeyOptions(block);
            var artifact = sankey.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(Chart);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidRadarDocument radar) {
            var options = BuildRadarOptions(block);
            var artifact = radar.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(Chart);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidTreemapDocument treemap) {
            var options = BuildTreemapOptions(block);
            var artifact = treemap.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(Chart);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidGanttDocument gantt) {
            var options = BuildGanttOptions(block);
            var artifact = gantt.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(Chart);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidPacketDocument packet) {
            var options = BuildPacketOptions(block);
            var artifact = packet.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(PacketLayoutBlock);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidBlockDocument blockDiagram) {
            var options = BuildBlockOptions(block);
            var artifact = blockDiagram.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(BlockLayoutBlock);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidVennDocument venn) {
            var options = BuildVennOptions(block);
            var artifact = venn.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(VennDiagramBlock);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidIshikawaDocument ishikawa) {
            var options = BuildIshikawaOptions(block);
            var artifact = ishikawa.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(FishboneDiagramBlock);
            result.Artifacts.Add(artifact);
            return;
        }

        if (mermaidResult.Document is MermaidWardleyDocument wardley) {
            var options = BuildWardleyOptions(block);
            var artifact = wardley.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["render.model"] = nameof(WardleyMapBlock);
            result.Artifacts.Add(artifact);
            return;
        }

        result.Diagnostics.Add(new MarkupDiagnostic {
            Line = block.FenceLine,
            Severity = MarkupDiagnosticSeverity.Warning,
            Message = "Mermaid diagram kind '" + mermaidResult.Document.Kind + "' is recognized but cannot produce a ChartForgeX visual artifact yet."
        });
        } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is OverflowException) {
            result.Diagnostics.Add(new MarkupDiagnostic {
                Line = block.FenceLine,
                Severity = MarkupDiagnosticSeverity.Error,
                Message = ex.Message
            });
        }
    }

    private static void AddTopologyArtifact(VisualMarkupParseResult result, VisualMarkupBlock block, VisualArtifact artifact) {
        artifact.Metadata["fence"] = block.FenceName;
        artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(TopologyChart);
        result.Artifacts.Add(artifact);
    }

    private MermaidFlowchartRenderOptions BuildOptions(VisualMarkupBlock block) {
        var options = Clone(_renderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryReadDoubleAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadDoubleAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        if (TryReadDoubleAttribute(block, "padding", out var parsedPadding)) options.Padding = parsedPadding;
        return options;
    }

    private MermaidSequenceRenderOptions BuildSequenceOptions(VisualMarkupBlock block) {
        var options = Clone(_sequenceRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryReadDoubleAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadDoubleAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        if (TryReadDoubleAttribute(block, "padding", out var parsedPadding)) options.Padding = parsedPadding;
        return options;
    }

    private MermaidPieRenderOptions BuildPieOptions(VisualMarkupBlock block) {
        var options = Clone(_pieRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryGetAttribute(block, "series", out var series) && !string.IsNullOrWhiteSpace(series)) options.SeriesName = series;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        return options;
    }

    private MermaidTimelineRenderOptions BuildTimelineOptions(VisualMarkupBlock block) {
        var options = Clone(_timelineRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        return options;
    }

    private MermaidJourneyRenderOptions BuildJourneyOptions(VisualMarkupBlock block) {
        var options = Clone(_journeyRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryGetAttribute(block, "series", out var series) && !string.IsNullOrWhiteSpace(series)) options.SeriesName = series;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        return options;
    }

    private MermaidGitGraphRenderOptions BuildGitGraphOptions(VisualMarkupBlock block) {
        var options = Clone(_gitGraphRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        if (TryReadDoubleAttribute(block, "padding", out var parsedPadding)) options.Padding = parsedPadding;
        if (TryReadBooleanAttribute(block, "branchLabels", out var showBranchLabels)) options.ShowBranchLabels = showBranchLabels;
        if (TryReadBooleanAttribute(block, "commitLabels", out var showCommitLabels)) options.ShowCommitLabels = showCommitLabels;
        return options;
    }

    private MermaidXYChartRenderOptions BuildXYChartOptions(VisualMarkupBlock block) {
        var options = Clone(_xyChartRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        if (TryReadBooleanAttribute(block, "dataLabels", out var showDataLabels)) options.ShowDataLabels = showDataLabels;
        return options;
    }

    private MermaidQuadrantRenderOptions BuildQuadrantOptions(VisualMarkupBlock block) {
        var options = Clone(_quadrantRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryGetAttribute(block, "series", out var series) && !string.IsNullOrWhiteSpace(series)) options.SeriesName = series;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        return options;
    }

    private MermaidSankeyRenderOptions BuildSankeyOptions(VisualMarkupBlock block) {
        var options = Clone(_sankeyRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryGetAttribute(block, "series", out var series) && !string.IsNullOrWhiteSpace(series)) options.SeriesName = series;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        return options;
    }

    private MermaidRadarRenderOptions BuildRadarOptions(VisualMarkupBlock block) {
        var options = Clone(_radarRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        return options;
    }

    private MermaidTreemapRenderOptions BuildTreemapOptions(VisualMarkupBlock block) {
        var options = Clone(_treemapRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryGetAttribute(block, "series", out var series) && !string.IsNullOrWhiteSpace(series)) options.SeriesName = series;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        return options;
    }

    private MermaidGanttRenderOptions BuildGanttOptions(VisualMarkupBlock block) {
        var options = Clone(_ganttRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        if (TryReadDateAttribute(block, "today", out var parsedToday)) options.Today = parsedToday;
        return options;
    }

    private MermaidPacketRenderOptions BuildPacketOptions(VisualMarkupBlock block) {
        var options = Clone(_packetRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        if (TryReadDoubleAttribute(block, "padding", out var parsedPadding)) options.Padding = parsedPadding;
        if (TryReadIntAttribute(block, "bitsPerRow", out var parsedBitsPerRow)) options.BitsPerRow = parsedBitsPerRow;
        if (TryReadBooleanAttribute(block, "bitNumbers", out var showBitNumbers)) options.ShowBitNumbers = showBitNumbers;
        return options;
    }

    private MermaidBlockRenderOptions BuildBlockOptions(VisualMarkupBlock block) {
        var options = Clone(_blockRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        if (TryReadDoubleAttribute(block, "padding", out var parsedPadding)) options.Padding = parsedPadding;
        if (TryReadIntAttribute(block, "columns", out var parsedColumns)) options.Columns = parsedColumns;
        if (TryReadBooleanAttribute(block, "edges", out var showEdges)) options.ShowEdges = showEdges;
        return options;
    }

    private MermaidVennRenderOptions BuildVennOptions(VisualMarkupBlock block) {
        var options = Clone(_vennRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        if (TryReadDoubleAttribute(block, "padding", out var parsedPadding)) options.Padding = parsedPadding;
        return options;
    }

    private MermaidIshikawaRenderOptions BuildIshikawaOptions(VisualMarkupBlock block) {
        var options = Clone(_ishikawaRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        if (TryReadDoubleAttribute(block, "padding", out var parsedPadding)) options.Padding = parsedPadding;
        return options;
    }

    private MermaidWardleyRenderOptions BuildWardleyOptions(VisualMarkupBlock block) {
        var options = Clone(_wardleyRenderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryReadIntAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadIntAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        if (TryReadDoubleAttribute(block, "padding", out var parsedPadding)) options.Padding = parsedPadding;
        return options;
    }

    private MermaidTopologyRenderOptions BuildTopologyOptions(VisualMarkupBlock block, MermaidTopologyRenderOptions defaults) {
        var options = Clone(defaults);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryReadDoubleAttribute(block, "width", out var parsedWidth)) options.Width = parsedWidth;
        if (TryReadDoubleAttribute(block, "height", out var parsedHeight)) options.Height = parsedHeight;
        if (TryReadDoubleAttribute(block, "padding", out var parsedPadding)) options.Padding = parsedPadding;
        return options;
    }

    private static bool TryGetAttribute(VisualMarkupBlock block, string key, out string value) =>
        VisualMarkupFenceOptions.TryGetAttribute(block, key, out value);

    private static bool TryReadIntAttribute(VisualMarkupBlock block, string key, out int value) {
        if (!TryGetAttribute(block, key, out var text) || string.IsNullOrWhiteSpace(text)) {
            value = 0;
            return false;
        }

        value = VisualMarkupFenceOptions.ParseInt32(text, key);
        return true;
    }

    private static bool TryReadDoubleAttribute(VisualMarkupBlock block, string key, out double value) {
        if (!TryGetAttribute(block, key, out var text) || string.IsNullOrWhiteSpace(text)) {
            value = 0;
            return false;
        }

        value = VisualMarkupFenceOptions.ParseDouble(text, key);
        return true;
    }

    private static bool TryReadBooleanAttribute(VisualMarkupBlock block, string key, out bool value) {
        if (!TryGetAttribute(block, key, out var text) || string.IsNullOrWhiteSpace(text)) {
            value = false;
            return false;
        }

        value = VisualMarkupFenceOptions.ParseBoolean(text, key);
        return true;
    }

    private static bool TryReadDateAttribute(VisualMarkupBlock block, string key, out DateTime value) {
        if (!TryGetAttribute(block, key, out var text) || string.IsNullOrWhiteSpace(text)) {
            value = default;
            return false;
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out value)) return true;
        throw new ArgumentException("Option '" + key + "' requires a date value.");
    }
}
