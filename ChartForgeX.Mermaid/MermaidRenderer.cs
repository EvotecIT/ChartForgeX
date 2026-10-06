using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Parses Mermaid source and renders every supported family through the canonical static engine.</summary>
public static class MermaidRenderer {
    /// <summary>Produces a reusable artifact and located diagnostics. Source with errors never produces an artifact.</summary>
    public static MermaidRenderResult Render(string source, MermaidRenderOptions? options = null) {
        var parsed = new MermaidParser().Parse(source);
        var result = new MermaidRenderResult { Document = parsed.Document };
        result.Diagnostics.AddRange(parsed.Diagnostics);
        if (parsed.HasErrors || parsed.Document == null || parsed.Document.Kind == MermaidDiagramKind.ZenUml) return result;
        try { result.Artifact = parsed.Document.ToVisualArtifact(options); }
        catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is OverflowException) {
            result.Diagnostics.Add(new MermaidDiagnostic { Severity = MermaidDiagnosticSeverity.Error, Span = parsed.Document.HeaderSpan, Message = exception.Message });
        }
        return result;
    }

    /// <summary>Converts an already parsed document into its typed artifact; unsupported families throw a located render error through Render.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidDocument document, MermaidRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidRenderOptions();
        return document switch {
            MermaidClassDocument item => item.ToVisualArtifact(options.Class),
            MermaidStateDocument item => item.ToVisualArtifact(options.State),
            MermaidEntityRelationshipDocument item => item.ToVisualArtifact(options.EntityRelationship),
            MermaidRequirementDocument item => item.ToVisualArtifact(options.Requirement),
            MermaidArchitectureDocument item => item.ToVisualArtifact(options.Architecture),
            MermaidC4Document item => item.ToVisualArtifact(options.C4),
            MermaidMindMapDocument item => item.ToVisualArtifact(options.MindMap),
            MermaidTreeViewDocument item => item.ToVisualArtifact(options.TreeView),
            MermaidEventModelingDocument item => item.ToVisualArtifact(options.EventModeling),
            MermaidKanbanDocument item => item.ToVisualArtifact(options.Kanban),
            MermaidSwimlaneDocument item => item.ToVisualArtifact(options.Swimlane),
            MermaidUseCaseDocument item => item.ToVisualArtifact(options.UseCase),
            MermaidCynefinDocument item => item.ToVisualArtifact(options.Cynefin),
            MermaidFlowchartDocument item => item.ToVisualArtifact(options.Flowchart),
            MermaidSequenceDocument item => item.ToVisualArtifact(options.Sequence),
            MermaidPieDocument item => item.ToVisualArtifact(options.Pie),
            MermaidJourneyDocument item => item.ToVisualArtifact(options.Journey),
            MermaidGitGraphDocument item => item.ToVisualArtifact(options.GitGraph),
            MermaidTimelineDocument item => item.ToVisualArtifact(options.Timeline),
            MermaidQuadrantDocument item => item.ToVisualArtifact(options.Quadrant),
            MermaidXYChartDocument item => item.ToVisualArtifact(options.XYChart),
            MermaidSankeyDocument item => item.ToVisualArtifact(options.Sankey),
            MermaidRadarDocument item => item.ToVisualArtifact(options.Radar),
            MermaidTreemapDocument item => item.ToVisualArtifact(options.Treemap),
            MermaidGanttDocument item => item.ToVisualArtifact(options.Gantt),
            MermaidPacketDocument item => item.ToVisualArtifact(options.Packet),
            MermaidBlockDocument item => item.ToVisualArtifact(options.Block),
            MermaidVennDocument item => item.ToVisualArtifact(options.Venn),
            MermaidIshikawaDocument item => item.ToVisualArtifact(options.Ishikawa),
            MermaidWardleyDocument item => item.ToVisualArtifact(options.Wardley),
            _ => throw new InvalidOperationException("Mermaid diagram kind '" + document.Kind + "' cannot produce a static artifact.")
        };
    }

    /// <summary>Renders an already parsed document to SVG.</summary>
    public static string ToSvg(this MermaidDocument document, MermaidRenderOptions? options = null) => document.ToVisualArtifact(options).ToSvg();

    /// <summary>Renders an already parsed document to PNG.</summary>
    public static byte[] ToPng(this MermaidDocument document, MermaidRenderOptions? options = null) => document.ToVisualArtifact(options).ToPng();
}

/// <summary>A parsed document, optional static artifact, and diagnostics from one render request.</summary>
public sealed class MermaidRenderResult {
    /// <summary>Gets the source-preserving parsed document when a family was recognized.</summary>
    public MermaidDocument? Document { get; internal set; }
    /// <summary>Gets the artifact when parsing and conversion succeeded.</summary>
    public VisualArtifact? Artifact { get; internal set; }
    /// <summary>Gets located parsing or conversion diagnostics.</summary>
    public List<MermaidDiagnostic> Diagnostics { get; } = new();
    /// <summary>Gets whether an error prevented rendering.</summary>
    public bool HasErrors => Diagnostics.Any(item => item.Severity == MermaidDiagnosticSeverity.Error);
}
