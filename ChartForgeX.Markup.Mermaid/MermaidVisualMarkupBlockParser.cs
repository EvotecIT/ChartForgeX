using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Markup.Mermaid;

/// <summary>
/// Parses Mermaid Markdown fences into ChartForgeX visual artifacts.
/// </summary>
public sealed class MermaidVisualMarkupBlockParser : IVisualMarkupBlockParser {
    private readonly MermaidFlowchartRenderOptions _renderOptions;
    private readonly MermaidSequenceRenderOptions _sequenceRenderOptions;
    private readonly MermaidPieRenderOptions _pieRenderOptions;
    private readonly MermaidTimelineRenderOptions _timelineRenderOptions;
    private readonly MermaidXYChartRenderOptions _xyChartRenderOptions;
    private readonly MermaidSankeyRenderOptions _sankeyRenderOptions;
    private readonly MermaidRadarRenderOptions _radarRenderOptions;
    private readonly MermaidTreemapRenderOptions _treemapRenderOptions;
    private readonly MermaidGanttRenderOptions _ganttRenderOptions;
    private readonly MermaidTopologyRenderOptions _classRenderOptions;
    private readonly MermaidTopologyRenderOptions _stateRenderOptions;
    private readonly MermaidTopologyRenderOptions _entityRelationshipRenderOptions;
    private readonly MermaidTopologyRenderOptions _mindMapRenderOptions;
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
        _timelineRenderOptions = renderOptions.Timeline == null ? new MermaidTimelineRenderOptions() : Clone(renderOptions.Timeline);
        _xyChartRenderOptions = renderOptions.XYChart == null ? new MermaidXYChartRenderOptions() : Clone(renderOptions.XYChart);
        _sankeyRenderOptions = renderOptions.Sankey == null ? new MermaidSankeyRenderOptions() : Clone(renderOptions.Sankey);
        _radarRenderOptions = renderOptions.Radar == null ? new MermaidRadarRenderOptions() : Clone(renderOptions.Radar);
        _treemapRenderOptions = renderOptions.Treemap == null ? new MermaidTreemapRenderOptions() : Clone(renderOptions.Treemap);
        _ganttRenderOptions = renderOptions.Gantt == null ? new MermaidGanttRenderOptions() : Clone(renderOptions.Gantt);
        _classRenderOptions = renderOptions.Class == null ? new MermaidTopologyRenderOptions() : Clone(renderOptions.Class);
        _stateRenderOptions = renderOptions.State == null ? new MermaidTopologyRenderOptions() : Clone(renderOptions.State);
        _entityRelationshipRenderOptions = renderOptions.EntityRelationship == null ? new MermaidTopologyRenderOptions() : Clone(renderOptions.EntityRelationship);
        _mindMapRenderOptions = renderOptions.MindMap == null ? new MermaidTopologyRenderOptions() : Clone(renderOptions.MindMap);
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
                Severity = diagnostic.Severity == MermaidDiagnosticSeverity.Error ? MarkupDiagnosticSeverity.Error : MarkupDiagnosticSeverity.Warning,
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

        if (mermaidResult.Document is MermaidMindMapDocument mindMap) {
            AddTopologyArtifact(result, block, mindMap.ToVisualArtifact(BuildTopologyOptions(block, _mindMapRenderOptions)));
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
        if (block.Attributes.TryGetValue("id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (block.Attributes.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (block.Attributes.TryGetValue("subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (block.Attributes.TryGetValue("width", out var width) && double.TryParse(width, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedWidth)) options.Width = parsedWidth;
        if (block.Attributes.TryGetValue("height", out var height) && double.TryParse(height, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedHeight)) options.Height = parsedHeight;
        if (block.Attributes.TryGetValue("padding", out var padding) && double.TryParse(padding, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedPadding)) options.Padding = parsedPadding;
        return options;
    }

    private MermaidSequenceRenderOptions BuildSequenceOptions(VisualMarkupBlock block) {
        var options = Clone(_sequenceRenderOptions);
        if (block.Attributes.TryGetValue("id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (block.Attributes.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (block.Attributes.TryGetValue("subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (block.Attributes.TryGetValue("width", out var width) && double.TryParse(width, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedWidth)) options.Width = parsedWidth;
        if (block.Attributes.TryGetValue("height", out var height) && double.TryParse(height, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedHeight)) options.Height = parsedHeight;
        if (block.Attributes.TryGetValue("padding", out var padding) && double.TryParse(padding, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedPadding)) options.Padding = parsedPadding;
        return options;
    }

    private MermaidPieRenderOptions BuildPieOptions(VisualMarkupBlock block) {
        var options = Clone(_pieRenderOptions);
        if (block.Attributes.TryGetValue("id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (block.Attributes.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (block.Attributes.TryGetValue("subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (block.Attributes.TryGetValue("series", out var series) && !string.IsNullOrWhiteSpace(series)) options.SeriesName = series;
        if (block.Attributes.TryGetValue("width", out var width) && int.TryParse(width, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedWidth)) options.Width = parsedWidth;
        if (block.Attributes.TryGetValue("height", out var height) && int.TryParse(height, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedHeight)) options.Height = parsedHeight;
        return options;
    }

    private MermaidTimelineRenderOptions BuildTimelineOptions(VisualMarkupBlock block) {
        var options = Clone(_timelineRenderOptions);
        if (block.Attributes.TryGetValue("id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (block.Attributes.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (block.Attributes.TryGetValue("subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (block.Attributes.TryGetValue("width", out var width) && int.TryParse(width, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedWidth)) options.Width = parsedWidth;
        if (block.Attributes.TryGetValue("height", out var height) && int.TryParse(height, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedHeight)) options.Height = parsedHeight;
        return options;
    }

    private MermaidXYChartRenderOptions BuildXYChartOptions(VisualMarkupBlock block) {
        var options = Clone(_xyChartRenderOptions);
        if (block.Attributes.TryGetValue("id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (block.Attributes.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (block.Attributes.TryGetValue("subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (block.Attributes.TryGetValue("width", out var width) && int.TryParse(width, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedWidth)) options.Width = parsedWidth;
        if (block.Attributes.TryGetValue("height", out var height) && int.TryParse(height, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedHeight)) options.Height = parsedHeight;
        if (block.Attributes.TryGetValue("dataLabels", out var dataLabels) && TryParseBoolean(dataLabels, out var showDataLabels)) options.ShowDataLabels = showDataLabels;
        return options;
    }

    private MermaidSankeyRenderOptions BuildSankeyOptions(VisualMarkupBlock block) {
        var options = Clone(_sankeyRenderOptions);
        if (block.Attributes.TryGetValue("id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (block.Attributes.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (block.Attributes.TryGetValue("subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (block.Attributes.TryGetValue("series", out var series) && !string.IsNullOrWhiteSpace(series)) options.SeriesName = series;
        if (block.Attributes.TryGetValue("width", out var width) && int.TryParse(width, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedWidth)) options.Width = parsedWidth;
        if (block.Attributes.TryGetValue("height", out var height) && int.TryParse(height, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedHeight)) options.Height = parsedHeight;
        return options;
    }

    private MermaidRadarRenderOptions BuildRadarOptions(VisualMarkupBlock block) {
        var options = Clone(_radarRenderOptions);
        if (block.Attributes.TryGetValue("id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (block.Attributes.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (block.Attributes.TryGetValue("subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (block.Attributes.TryGetValue("width", out var width) && int.TryParse(width, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedWidth)) options.Width = parsedWidth;
        if (block.Attributes.TryGetValue("height", out var height) && int.TryParse(height, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedHeight)) options.Height = parsedHeight;
        return options;
    }

    private MermaidTreemapRenderOptions BuildTreemapOptions(VisualMarkupBlock block) {
        var options = Clone(_treemapRenderOptions);
        if (block.Attributes.TryGetValue("id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (block.Attributes.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (block.Attributes.TryGetValue("subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (block.Attributes.TryGetValue("series", out var series) && !string.IsNullOrWhiteSpace(series)) options.SeriesName = series;
        if (block.Attributes.TryGetValue("width", out var width) && int.TryParse(width, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedWidth)) options.Width = parsedWidth;
        if (block.Attributes.TryGetValue("height", out var height) && int.TryParse(height, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedHeight)) options.Height = parsedHeight;
        return options;
    }

    private MermaidGanttRenderOptions BuildGanttOptions(VisualMarkupBlock block) {
        var options = Clone(_ganttRenderOptions);
        if (block.Attributes.TryGetValue("id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (block.Attributes.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (block.Attributes.TryGetValue("subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (block.Attributes.TryGetValue("width", out var width) && int.TryParse(width, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedWidth)) options.Width = parsedWidth;
        if (block.Attributes.TryGetValue("height", out var height) && int.TryParse(height, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedHeight)) options.Height = parsedHeight;
        if (block.Attributes.TryGetValue("today", out var today) && DateTime.TryParse(today, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedToday)) options.Today = parsedToday;
        return options;
    }

    private MermaidTopologyRenderOptions BuildTopologyOptions(VisualMarkupBlock block, MermaidTopologyRenderOptions defaults) {
        var options = Clone(defaults);
        if (block.Attributes.TryGetValue("id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (block.Attributes.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (block.Attributes.TryGetValue("subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (block.Attributes.TryGetValue("width", out var width) && double.TryParse(width, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedWidth)) options.Width = parsedWidth;
        if (block.Attributes.TryGetValue("height", out var height) && double.TryParse(height, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedHeight)) options.Height = parsedHeight;
        if (block.Attributes.TryGetValue("padding", out var padding) && double.TryParse(padding, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedPadding)) options.Padding = parsedPadding;
        return options;
    }

    private static MermaidFlowchartRenderOptions Clone(MermaidFlowchartRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            Padding = options.Padding
        };

    private static MermaidSequenceRenderOptions Clone(MermaidSequenceRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            Padding = options.Padding
        };

    private static MermaidPieRenderOptions Clone(MermaidPieRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            SeriesName = options.SeriesName,
            Width = options.Width,
            Height = options.Height
        };

    private static MermaidTimelineRenderOptions Clone(MermaidTimelineRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            ShowEventDurations = options.ShowEventDurations
        };

    private static MermaidXYChartRenderOptions Clone(MermaidXYChartRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            ShowDataLabels = options.ShowDataLabels
        };

    private static MermaidSankeyRenderOptions Clone(MermaidSankeyRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            SeriesName = options.SeriesName,
            Width = options.Width,
            Height = options.Height
        };

    private static MermaidRadarRenderOptions Clone(MermaidRadarRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height
        };

    private static MermaidTreemapRenderOptions Clone(MermaidTreemapRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            SeriesName = options.SeriesName,
            Width = options.Width,
            Height = options.Height
        };

    private static MermaidGanttRenderOptions Clone(MermaidGanttRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            Today = options.Today
        };

    private static MermaidTopologyRenderOptions Clone(MermaidTopologyRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            Padding = options.Padding
        };

    private static bool TryParseBoolean(string value, out bool result) {
        if (bool.TryParse(value, out result)) return true;
        if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase)) {
            result = true;
            return true;
        }

        if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "off", StringComparison.OrdinalIgnoreCase)) {
            result = false;
            return true;
        }

        result = false;
        return false;
    }
}
