using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid Venn diagram.
/// </summary>
public sealed class MermaidVennDocument : MermaidDocument {
    /// <summary>Gets Venn statements retained in source order.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets parsed Venn sets.</summary>
    public List<MermaidVennSet> Sets { get; } = new();

    /// <summary>Gets parsed Venn intersections.</summary>
    public List<MermaidVennIntersection> Intersections { get; } = new();

    /// <summary>Gets parsed Venn text nodes.</summary>
    public List<MermaidVennTextNode> TextNodes { get; } = new();

    /// <summary>Gets style statements retained in source order.</summary>
    public List<MermaidRawStatement> StyleStatements { get; } = new();

    /// <summary>Gets or sets the optional title statement.</summary>
    public string? Title { get; set; }
}

/// <summary>
/// Describes one Mermaid Venn set.
/// </summary>
public sealed class MermaidVennSet {
    private string _id;
    private string _label;

    /// <summary>Initializes a Venn set.</summary>
    public MermaidVennSet(string id, string label, double size, MermaidSourceSpan span) {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Size = size;
        Span = span;
    }

    /// <summary>Gets or sets the set id.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the source size value.</summary>
    public double Size { get; set; }

    /// <summary>Gets the source span.</summary>
    public MermaidSourceSpan Span { get; }

    /// <summary>Gets or sets the optional fill color.</summary>
    public ChartColor? Fill { get; set; }

    /// <summary>Gets or sets the optional stroke color.</summary>
    public ChartColor? Stroke { get; set; }

    /// <summary>Gets or sets the optional text color.</summary>
    public ChartColor? TextColor { get; set; }
}

/// <summary>
/// Describes one Mermaid Venn intersection.
/// </summary>
public sealed class MermaidVennIntersection {
    private readonly List<string> _setIds;
    private string _label;

    /// <summary>Initializes a Venn intersection.</summary>
    public MermaidVennIntersection(IEnumerable<string> setIds, string label, double size, MermaidSourceSpan span) {
        if (setIds == null) throw new ArgumentNullException(nameof(setIds));
        _setIds = new List<string>(setIds);
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Size = size;
        Span = span;
    }

    /// <summary>Gets participating set ids.</summary>
    public IReadOnlyList<string> SetIds => _setIds;

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the source size value.</summary>
    public double Size { get; set; }

    /// <summary>Gets the source span.</summary>
    public MermaidSourceSpan Span { get; }

    /// <summary>Gets or sets the optional fill color.</summary>
    public ChartColor? Fill { get; set; }

    /// <summary>Gets or sets the optional stroke color.</summary>
    public ChartColor? Stroke { get; set; }

    /// <summary>Gets or sets the optional text color.</summary>
    public ChartColor? TextColor { get; set; }
}

/// <summary>
/// Describes one Mermaid Venn text node.
/// </summary>
public sealed class MermaidVennTextNode {
    private readonly List<string> _setIds;
    private string _id;
    private string _label;

    /// <summary>Initializes a Venn text node.</summary>
    public MermaidVennTextNode(IEnumerable<string> setIds, string id, string label, MermaidSourceSpan span) {
        if (setIds == null) throw new ArgumentNullException(nameof(setIds));
        _setIds = new List<string>(setIds);
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Span = span;
    }

    /// <summary>Gets participating set ids.</summary>
    public IReadOnlyList<string> SetIds => _setIds;

    /// <summary>Gets or sets the text node id.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets the source span.</summary>
    public MermaidSourceSpan Span { get; }

    /// <summary>Gets or sets the optional text color.</summary>
    public ChartColor? TextColor { get; set; }
}
