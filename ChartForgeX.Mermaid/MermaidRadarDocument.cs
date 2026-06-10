using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid radar diagram document.
/// </summary>
public sealed class MermaidRadarDocument : MermaidDocument {
    /// <summary>Gets retained raw body statements.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets or sets the optional chart title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets parsed radar axes.</summary>
    public List<MermaidRadarAxis> Axes { get; } = new();

    /// <summary>Gets parsed radar curves.</summary>
    public List<MermaidRadarCurve> Curves { get; } = new();

    /// <summary>Gets or sets whether the legend should be shown.</summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>Gets or sets the optional minimum radar scale.</summary>
    public double? Minimum { get; set; }

    /// <summary>Gets or sets the optional maximum radar scale.</summary>
    public double? Maximum { get; set; }

    /// <summary>Gets or sets the optional tick count.</summary>
    public int? Ticks { get; set; }

    /// <summary>Gets or sets the optional Mermaid graticule style.</summary>
    public string? Graticule { get; set; }
}

/// <summary>
/// Describes a Mermaid radar axis.
/// </summary>
public sealed class MermaidRadarAxis : MermaidAstNode {
    private string _id;
    private string _label;

    /// <summary>Initializes a Mermaid radar axis.</summary>
    public MermaidRadarAxis(string id, string label, MermaidSourceSpan span) : base(span) {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _label = label ?? throw new ArgumentNullException(nameof(label));
    }

    /// <summary>Gets or sets the axis id.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the axis label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }
}

/// <summary>
/// Describes a Mermaid radar curve.
/// </summary>
public sealed class MermaidRadarCurve : MermaidAstNode {
    private string _id;
    private string _label;

    /// <summary>Initializes a Mermaid radar curve.</summary>
    public MermaidRadarCurve(string id, string label, MermaidSourceSpan span) : base(span) {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _label = label ?? throw new ArgumentNullException(nameof(label));
    }

    /// <summary>Gets or sets the curve id.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the curve label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets ordered curve values when Mermaid values were specified by position.</summary>
    public List<double> OrderedValues { get; } = new();

    /// <summary>Gets curve values keyed by axis id when Mermaid values were specified as key-value pairs.</summary>
    public Dictionary<string, double> ValuesByAxisId { get; } = new(StringComparer.Ordinal);
}
