using System;
using System.Collections.Generic;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid block diagram.
/// </summary>
public sealed class MermaidBlockDocument : MermaidDocument {
    /// <summary>Gets block statements retained in source order.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets parsed block items in layout order.</summary>
    public List<MermaidBlockItem> Items { get; } = new();

    /// <summary>Gets parsed block edges.</summary>
    public List<MermaidBlockEdge> Edges { get; } = new();

    /// <summary>Gets style and class statements retained with their source locations.</summary>
    public List<MermaidRawStatement> StyleStatements { get; } = new();

    /// <summary>Gets named styles in declaration order.</summary>
    public List<MermaidStyleDefinition> ClassDefinitions { get; } = new();
    /// <summary>Gets or sets whether source explicitly selects automatic single-row columns.</summary>
    public bool AutoColumns { get; set; }

    /// <summary>Gets or sets the optional column count.</summary>
    public int? Columns { get; set; }

    /// <summary>Gets or sets the optional title statement.</summary>
    public string? Title { get; set; }
}

/// <summary>
/// Describes one Mermaid block item.
/// </summary>
public sealed class MermaidBlockItem {
    private string _id;
    private string _label;

    /// <summary>Initializes a block item.</summary>
    public MermaidBlockItem(string id, string label, int columnSpan, BlockLayoutShape shape, bool isSpace, MermaidSourceSpan span) {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _label = label ?? throw new ArgumentNullException(nameof(label));
        ColumnSpan = columnSpan;
        Shape = shape;
        IsSpace = isSpace;
        Span = span;
    }

    /// <summary>Gets or sets the item id.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets how many columns this item spans.</summary>
    public int ColumnSpan { get; }

    /// <summary>Gets the preferred item shape.</summary>
    public BlockLayoutShape Shape { get; }

    /// <summary>Gets whether this item is an invisible spacer.</summary>
    public bool IsSpace { get; }

    /// <summary>Gets or sets the containing composite ID.</summary>
    public string? ParentId { get; set; }
    /// <summary>Gets or sets whether this source declaration is a composite.</summary>
    public bool IsComposite { get; set; }
    /// <summary>Gets or sets local columns for this composite. Null inherits its enclosing grid.</summary>
    public int? Columns { get; set; }
    /// <summary>Gets or sets whether this composite explicitly uses automatic columns.</summary>
    public bool AutoColumns { get; set; }
    /// <summary>Gets or sets native directional arrow heads.</summary>
    public BlockLayoutArrowDirections ArrowDirections { get; set; } = BlockLayoutArrowDirections.Right;
    /// <summary>Gets class names in authored order.</summary>
    public List<string> Classes { get; } = new();
    /// <summary>Gets captured style declarations after the default/class/direct cascade.</summary>
    public Dictionary<string, string> Styles { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the source span for the item statement.</summary>
    public MermaidSourceSpan Span { get; }
}

/// <summary>
/// Describes one Mermaid block edge.
/// </summary>
public sealed class MermaidBlockEdge {
    private string _sourceId;
    private string _targetId;
    private string _label;

    /// <summary>Initializes a block edge.</summary>
    public MermaidBlockEdge(string sourceId, string targetId, string label, bool directed, MermaidSourceSpan span) {
        _sourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
        _targetId = targetId ?? throw new ArgumentNullException(nameof(targetId));
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Directed = directed;
        Span = span;
    }

    /// <summary>Gets or sets the source item id.</summary>
    public string SourceId { get => _sourceId; set => _sourceId = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the target item id.</summary>
    public string TargetId { get => _targetId; set => _targetId = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the optional edge label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets whether the edge is directed.</summary>
    public bool Directed { get; }

    /// <summary>Gets or sets whether the source connector is painted.</summary>
    public bool Visible { get; set; } = true;
    /// <summary>Gets or sets captured native connector paint; null retains the default.</summary>
    public ChartForgeX.Core.ChartStrokeStyle? Stroke { get; set; }

    /// <summary>Gets the source span for the edge statement.</summary>
    public MermaidSourceSpan Span { get; }
}
