using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Parses Mermaid source and renders every supported family through the canonical static engine.</summary>
public static class MermaidRenderer {
    /// <summary>Produces a reusable artifact and located diagnostics. Source with errors never produces an artifact.</summary>
    public static MermaidRenderResult Render(string source, MermaidRenderOptions? options = null) => Render(source, options, default);

    /// <summary>Parses and converts a source request with cooperative cancellation.</summary>
    /// <remarks>Cancellation is checked during parsing and before/after native export; it cannot interrupt
    /// a native layout operation already executing. Output limits are enforced when the artifact is exported.</remarks>
    public static MermaidRenderResult Render(string source, MermaidRenderOptions? options, CancellationToken cancellationToken) {
        var captured = (options ?? new MermaidRenderOptions()).Clone();
        return RenderParsed(new MermaidParser().Parse(source, captured.ParseOptions, cancellationToken), captured, cancellationToken);
    }

    /// <summary>Converts an existing parse outcome without reparsing or losing its located diagnostics.</summary>
    /// <remarks>Markup hosts use this boundary to apply fence options after family detection.
    /// Null options retain the resource limits captured by parsing.</remarks>
    public static MermaidRenderResult RenderParsed(MermaidParseResult<MermaidDocument> parsed,
        MermaidRenderOptions? options = null, CancellationToken cancellationToken = default) {
        if (parsed == null) throw new ArgumentNullException(nameof(parsed));
        cancellationToken.ThrowIfCancellationRequested();
        var captured = (options ?? new MermaidRenderOptions()).Clone();
        if (!Enum.IsDefined(typeof(MermaidWarningPolicy), captured.WarningPolicy)) throw new ArgumentOutOfRangeException(nameof(options));
        var result = new MermaidRenderResult { Document = parsed.Document };
        result.Diagnostics.AddRange(parsed.Diagnostics);
        if (parsed.HasErrors || parsed.Document == null || parsed.Document.IsDiagnosticOnly) return result;
        if (captured.WarningPolicy == MermaidWarningPolicy.Reject && parsed.Diagnostics.Any(item => item.Severity == MermaidDiagnosticSeverity.Warning)) {
            result.Diagnostics.Add(new MermaidDiagnostic { Code = MermaidDiagnosticCodes.RejectedWarning,
                Severity = MermaidDiagnosticSeverity.Error, Span = parsed.Document.HeaderSpan,
                Message = "The host warning policy rejects source with unsupported or approximate notation." });
            return result;
        }
        try {
            var limits = captured.ParseOptions?.ResourceLimits ?? parsed.Document.CapturedResourceLimits ?? new MermaidResourceLimits();
            using var scope = MermaidResourceGuard.Begin(limits, cancellationToken);
            MermaidResourceGuard.ValidateDocument(parsed.Document);
            result.Artifact = MermaidArtifactPolicy.Attach(parsed.Document.ToVisualArtifact(captured), parsed.Document, limits, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        } catch (MermaidResourceLimitException exception) {
            result.Diagnostics.Add(new MermaidDiagnostic { Code = MermaidDiagnosticCodes.ResourceLimitExceeded,
                Severity = MermaidDiagnosticSeverity.Error, Span = exception.Span, Message = exception.Message });
        } catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is OverflowException) {
            result.Diagnostics.Add(new MermaidDiagnostic { Code = MermaidDiagnosticCodes.ConversionFailed,
                Severity = MermaidDiagnosticSeverity.Error, Span = parsed.Document.HeaderSpan, Message = exception.Message });
        }
        return result;
    }

    /// <summary>Converts a parsed document through its native family using shared host defaults.</summary>
    /// <remarks>Use <see cref="RenderParsed"/> for a diagnostic result when conversion is unsupported.</remarks>
    public static VisualArtifact ToVisualArtifact(this MermaidDocument document, MermaidRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidRenderOptions();
        return document switch {
            MermaidZenUmlDocument item => item.ToVisualArtifact(options.Resolve(options.ZenUml)),
            MermaidRailroadDocument item => item.ToVisualArtifact(options.Resolve(options.Railroad)),
            MermaidAgentflowDocument item => item.ToVisualArtifact(options.Resolve(options.Agentflow)),
            MermaidClassDocument item => item.ToVisualArtifact(options.Resolve(options.Class)),
            MermaidStateDocument item => item.ToVisualArtifact(options.Resolve(options.State)),
            MermaidEntityRelationshipDocument item => item.ToVisualArtifact(options.Resolve(options.EntityRelationship)),
            MermaidRequirementDocument item => item.ToVisualArtifact(options.Resolve(options.Requirement)),
            MermaidArchitectureDocument item => item.ToVisualArtifact(options.Resolve(options.Architecture)),
            MermaidC4Document item => item.ToVisualArtifact(options.Resolve(options.C4)),
            MermaidMindMapDocument item => item.ToVisualArtifact(options.Resolve(options.MindMap)),
            MermaidTreeViewDocument item => item.ToVisualArtifact(options.Resolve(options.TreeView)),
            MermaidEventModelingDocument item => item.ToVisualArtifact(options.Resolve(options.EventModeling)),
            MermaidKanbanDocument item => item.ToVisualArtifact(options.Resolve(options.Kanban)),
            MermaidSwimlaneDocument item => item.ToVisualArtifact(options.Resolve(options.Swimlane)),
            MermaidUseCaseDocument item => item.ToVisualArtifact(options.Resolve(options.UseCase)),
            MermaidCynefinDocument item => item.ToVisualArtifact(options.Resolve(options.Cynefin)),
            MermaidFlowchartDocument item => item.ToVisualArtifact(options.Resolve(options.Flowchart)),
            MermaidSequenceDocument item => item.ToVisualArtifact(options.Resolve(options.Sequence)),
            MermaidPieDocument item => item.ToVisualArtifact(options.Resolve(options.Pie)),
            MermaidJourneyDocument item => item.ToVisualArtifact(options.Resolve(options.Journey)),
            MermaidGitGraphDocument item => item.ToVisualArtifact(options.Resolve(options.GitGraph)),
            MermaidTimelineDocument item => item.ToVisualArtifact(options.Resolve(options.Timeline)),
            MermaidQuadrantDocument item => item.ToVisualArtifact(options.Resolve(options.Quadrant)),
            MermaidXYChartDocument item => item.ToVisualArtifact(options.Resolve(options.XYChart)),
            MermaidSankeyDocument item => item.ToVisualArtifact(options.Resolve(options.Sankey)),
            MermaidRadarDocument item => item.ToVisualArtifact(options.Resolve(options.Radar)),
            MermaidTreemapDocument item => item.ToVisualArtifact(options.Resolve(options.Treemap)),
            MermaidGanttDocument item => item.ToVisualArtifact(options.Resolve(options.Gantt)),
            MermaidPacketDocument item => item.ToVisualArtifact(options.Resolve(options.Packet)),
            MermaidBlockDocument item => item.ToVisualArtifact(options.Resolve(options.Block)),
            MermaidVennDocument item => item.ToVisualArtifact(options.Resolve(options.Venn)),
            MermaidIshikawaDocument item => item.ToVisualArtifact(options.Resolve(options.Ishikawa)),
            MermaidWardleyDocument item => item.ToVisualArtifact(options.Resolve(options.Wardley)),
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
