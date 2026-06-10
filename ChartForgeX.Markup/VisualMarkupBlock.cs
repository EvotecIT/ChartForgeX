using System;
using System.Collections.Generic;

namespace ChartForgeX.Markup;

/// <summary>
/// Identifies the visual kind declared by a supported markup fence.
/// </summary>
public enum VisualMarkupKind {
    /// <summary>The fence declares a topology diagram.</summary>
    Topology,
    /// <summary>The fence declares a flow diagram.</summary>
    Flow,
    /// <summary>The fence declares a table artifact.</summary>
    Table,
    /// <summary>The fence declares a chart artifact.</summary>
    Chart,
    /// <summary>The fence declares a timeline artifact.</summary>
    Timeline,
    /// <summary>The fence declares a Mermaid diagram.</summary>
    Mermaid
}

/// <summary>
/// Describes a supported visual fenced block extracted from Markdown.
/// </summary>
public sealed class VisualMarkupBlock {
    /// <summary>Initializes a visual markup block.</summary>
    public VisualMarkupBlock(VisualMarkupKind kind, string fenceName, string fenceInfo, string payload, int fenceLine, int startLine, int endLine, IReadOnlyDictionary<string, string> attributes) {
        Kind = kind;
        FenceName = fenceName ?? throw new ArgumentNullException(nameof(fenceName));
        FenceInfo = fenceInfo ?? throw new ArgumentNullException(nameof(fenceInfo));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        FenceLine = fenceLine < 1 ? 1 : fenceLine;
        StartLine = startLine < 1 ? 1 : startLine;
        EndLine = endLine < StartLine ? StartLine : endLine;
        Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
    }

    /// <summary>Gets the visual kind.</summary>
    public VisualMarkupKind Kind { get; }

    /// <summary>Gets the normalized fence name, such as <c>chartforgex topology</c> or <c>mermaid</c>.</summary>
    public string FenceName { get; }

    /// <summary>Gets the full fence info string after the opening fence marker.</summary>
    public string FenceInfo { get; }

    /// <summary>Gets the extracted fence payload.</summary>
    public string Payload { get; }

    /// <summary>Gets the one-based source line that contains the opening fence.</summary>
    public int FenceLine { get; }

    /// <summary>Gets the one-based source line where the payload starts.</summary>
    public int StartLine { get; }

    /// <summary>Gets the one-based source line where the payload ends.</summary>
    public int EndLine { get; }

    /// <summary>Gets attributes parsed from trailing fence metadata when present.</summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }
}

/// <summary>
/// Describes the result of scanning Markdown for visual fenced blocks.
/// </summary>
public sealed class VisualMarkupScanResult {
    /// <summary>Gets supported visual blocks.</summary>
    public List<VisualMarkupBlock> Blocks { get; } = new();

    /// <summary>Gets scanner diagnostics.</summary>
    public List<MarkupDiagnostic> Diagnostics { get; } = new();
}
