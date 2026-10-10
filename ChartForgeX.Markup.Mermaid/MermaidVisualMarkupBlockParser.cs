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
    private readonly MermaidFlowchartRenderOptions _swimlaneRenderOptions;
    private readonly MermaidFlowchartRenderOptions _useCaseRenderOptions;
    private readonly MermaidTopologyRenderOptions _cynefinRenderOptions;
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
    public MermaidVisualMarkupBlockParser() : this(new MermaidRenderOptions()) {
    }

    /// <summary>
    /// Initializes a Mermaid visual block parser with rendering defaults.
    /// </summary>
    /// <param name="renderOptions">Optional rendering defaults by Mermaid diagram kind.</param>
    public MermaidVisualMarkupBlockParser(MermaidRenderOptions renderOptions) {
        if (renderOptions == null) throw new ArgumentNullException(nameof(renderOptions));

        _renderOptions = renderOptions.Flowchart == null ? new MermaidFlowchartRenderOptions() : Clone(renderOptions.Flowchart);
        _swimlaneRenderOptions = Clone(renderOptions.Swimlane ?? renderOptions.Flowchart ?? new MermaidFlowchartRenderOptions());
        _useCaseRenderOptions = Clone(renderOptions.UseCase ?? renderOptions.Flowchart ?? new MermaidFlowchartRenderOptions());
        _cynefinRenderOptions = Clone(renderOptions.Cynefin ?? new MermaidTopologyRenderOptions());
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
                Message = diagnostic.Message,
                Code = diagnostic.Code
            });
        }

        if (mermaidResult.HasErrors || mermaidResult.Document == null || mermaidResult.Document.IsDiagnosticOnly) return;
        try {
            var options = BuildRenderOptions(block, mermaidResult.Document.Kind);
            var artifact = mermaidResult.Document.ToVisualArtifact(options);
            artifact.Metadata["fence"] = block.FenceName;
            artifact.Metadata["sourceLine"] = block.FenceLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadStartLine"] = block.StartLine.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata["payloadEndLine"] = block.EndLine.ToString(CultureInfo.InvariantCulture);
            result.Artifacts.Add(artifact);
        } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is OverflowException) {
            result.Diagnostics.Add(new MarkupDiagnostic {
                Line = block.FenceLine,
                Severity = MarkupDiagnosticSeverity.Error,
                Code = MermaidDiagnosticCodes.ConversionFailed,
                Message = ex.Message
            });
        }
    }

    private MermaidRenderOptions BuildRenderOptions(VisualMarkupBlock block, MermaidDiagramKind kind) => kind switch {
        MermaidDiagramKind.Swimlane => new MermaidRenderOptions { Swimlane = BuildOptions(block, _swimlaneRenderOptions) },
        MermaidDiagramKind.UseCase => new MermaidRenderOptions { UseCase = BuildOptions(block, _useCaseRenderOptions) },
        MermaidDiagramKind.Cynefin => new MermaidRenderOptions { Cynefin = BuildTopologyOptions(block, _cynefinRenderOptions) },
        MermaidDiagramKind.Class => new MermaidRenderOptions { Class = BuildTopologyOptions(block, _classRenderOptions) },
        MermaidDiagramKind.State => new MermaidRenderOptions { State = BuildTopologyOptions(block, _stateRenderOptions) },
        MermaidDiagramKind.EntityRelationship => new MermaidRenderOptions { EntityRelationship = BuildTopologyOptions(block, _entityRelationshipRenderOptions) },
        MermaidDiagramKind.Requirement => new MermaidRenderOptions { Requirement = BuildTopologyOptions(block, _requirementRenderOptions) },
        MermaidDiagramKind.Architecture => new MermaidRenderOptions { Architecture = BuildTopologyOptions(block, _architectureRenderOptions) },
        MermaidDiagramKind.C4 => new MermaidRenderOptions { C4 = BuildTopologyOptions(block, _c4RenderOptions) },
        MermaidDiagramKind.MindMap => new MermaidRenderOptions { MindMap = BuildTopologyOptions(block, _mindMapRenderOptions) },
        MermaidDiagramKind.TreeView => new MermaidRenderOptions { TreeView = BuildTopologyOptions(block, _treeViewRenderOptions) },
        MermaidDiagramKind.EventModeling => new MermaidRenderOptions { EventModeling = BuildTopologyOptions(block, _eventModelingRenderOptions) },
        MermaidDiagramKind.Kanban => new MermaidRenderOptions { Kanban = BuildTopologyOptions(block, _kanbanRenderOptions) },
        MermaidDiagramKind.Flowchart => new MermaidRenderOptions { Flowchart = BuildOptions(block) },
        MermaidDiagramKind.Sequence => new MermaidRenderOptions { Sequence = BuildSequenceOptions(block) },
        MermaidDiagramKind.Pie => new MermaidRenderOptions { Pie = BuildPieOptions(block) },
        MermaidDiagramKind.Journey => new MermaidRenderOptions { Journey = BuildJourneyOptions(block) },
        MermaidDiagramKind.GitGraph => new MermaidRenderOptions { GitGraph = BuildGitGraphOptions(block) },
        MermaidDiagramKind.Timeline => new MermaidRenderOptions { Timeline = BuildTimelineOptions(block) },
        MermaidDiagramKind.Quadrant => new MermaidRenderOptions { Quadrant = BuildQuadrantOptions(block) },
        MermaidDiagramKind.XYChart => new MermaidRenderOptions { XYChart = BuildXYChartOptions(block) },
        MermaidDiagramKind.Sankey => new MermaidRenderOptions { Sankey = BuildSankeyOptions(block) },
        MermaidDiagramKind.Radar => new MermaidRenderOptions { Radar = BuildRadarOptions(block) },
        MermaidDiagramKind.Treemap => new MermaidRenderOptions { Treemap = BuildTreemapOptions(block) },
        MermaidDiagramKind.Gantt => new MermaidRenderOptions { Gantt = BuildGanttOptions(block) },
        MermaidDiagramKind.Packet => new MermaidRenderOptions { Packet = BuildPacketOptions(block) },
        MermaidDiagramKind.Block => new MermaidRenderOptions { Block = BuildBlockOptions(block) },
        MermaidDiagramKind.Venn => new MermaidRenderOptions { Venn = BuildVennOptions(block) },
        MermaidDiagramKind.Ishikawa => new MermaidRenderOptions { Ishikawa = BuildIshikawaOptions(block) },
        MermaidDiagramKind.Wardley => new MermaidRenderOptions { Wardley = BuildWardleyOptions(block) },
        _ => new MermaidRenderOptions()
    };

    private MermaidFlowchartRenderOptions BuildOptions(VisualMarkupBlock block, MermaidFlowchartRenderOptions? defaults = null) {
        var options = Clone(defaults ?? _renderOptions);
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        var hasExplicitWidth = TryReadDoubleAttribute(block, "width", out var parsedWidth);
        var hasExplicitHeight = TryReadDoubleAttribute(block, "height", out var parsedHeight);
        if (hasExplicitWidth) options.Width = parsedWidth;
        if (hasExplicitHeight) options.Height = parsedHeight;
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
