using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid pie chart document.
/// </summary>
public sealed class MermaidPieDocument : MermaidDocument {
    private string? _title;

    /// <summary>Gets raw semantic body statements retained for future syntax growth.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets or sets whether Mermaid's optional showData flag was present.</summary>
    public bool ShowData { get; set; }

    /// <summary>Gets or sets the optional Mermaid pie title.</summary>
    public string? Title {
        get => _title;
        set => _title = value;
    }

    /// <summary>Gets parsed pie slices in source order.</summary>
    public List<MermaidPieSlice> Slices { get; } = new();
}

/// <summary>
/// Describes one Mermaid pie slice.
/// </summary>
public sealed class MermaidPieSlice {
    private string _label;

    /// <summary>Initializes a pie slice.</summary>
    public MermaidPieSlice(string label, double value, MermaidSourceSpan span) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Value = value;
        Span = span;
    }

    /// <summary>Gets or sets the slice label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the positive numeric slice value.</summary>
    public double Value { get; set; }

    /// <summary>Gets or sets the source span.</summary>
    public MermaidSourceSpan Span { get; set; }
}
