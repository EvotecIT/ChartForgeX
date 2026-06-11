using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// A deterministic grid-based block layout with optional directed edges.
/// </summary>
public sealed class BlockLayoutBlock : VisualBlock<BlockLayoutBlock> {
    private readonly List<BlockLayoutItem> _items = new();
    private readonly List<BlockLayoutEdge> _edges = new();
    private int _columns = 3;

    /// <summary>Gets block items in source/layout order.</summary>
    public IReadOnlyList<BlockLayoutItem> Items => _items;

    /// <summary>Gets edges between visible block items.</summary>
    public IReadOnlyList<BlockLayoutEdge> Edges => _edges;

    /// <summary>Gets or sets the number of layout columns.</summary>
    public int Columns {
        get => _columns;
        set {
            if (value < 1 || value > 24) throw new ArgumentOutOfRangeException(nameof(value), value, "Block layout columns must be between one and twenty-four.");
            _columns = value;
        }
    }

    /// <summary>Gets or sets whether edges are rendered when present.</summary>
    public bool ShowEdges { get; set; } = true;

    /// <summary>Gets a concise accessibility label.</summary>
    public override string AccessibleName => Title.Length == 0 ? "Block layout" : Title;

    /// <summary>Creates a block layout.</summary>
    public static BlockLayoutBlock Create() => new();

    /// <summary>Adds a visible block item.</summary>
    public BlockLayoutBlock AddItem(string id, string label, int columnSpan = 1, BlockLayoutShape shape = BlockLayoutShape.Rectangle) {
        _items.Add(new BlockLayoutItem(id, label, columnSpan, shape, false));
        return this;
    }

    /// <summary>Adds an invisible spacer that occupies one or more columns.</summary>
    public BlockLayoutBlock AddSpace(int columnSpan = 1) {
        _items.Add(new BlockLayoutItem("space-" + _items.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), string.Empty, columnSpan, BlockLayoutShape.Rectangle, true));
        return this;
    }

    /// <summary>Adds an edge between two visible block items.</summary>
    public BlockLayoutBlock AddEdge(string sourceId, string targetId, string label = "", bool directed = true) {
        _edges.Add(new BlockLayoutEdge(sourceId, targetId, label, directed));
        return this;
    }

    /// <summary>Sets the number of layout columns.</summary>
    public BlockLayoutBlock WithColumns(int columns) { Columns = columns; return this; }

    /// <summary>Sets whether edges are rendered when present.</summary>
    public BlockLayoutBlock WithEdges(bool visible = true) { ShowEdges = visible; return this; }
}

/// <summary>
/// Describes one item in a deterministic block layout.
/// </summary>
public sealed class BlockLayoutItem {
    private string _id;
    private string _label;
    private int _columnSpan;

    /// <summary>Initializes a block layout item.</summary>
    public BlockLayoutItem(string id, string label, int columnSpan = 1, BlockLayoutShape shape = BlockLayoutShape.Rectangle, bool isSpace = false) {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _label = label ?? throw new ArgumentNullException(nameof(label));
        ColumnSpan = columnSpan;
        Shape = shape;
        IsSpace = isSpace;
    }

    /// <summary>Gets or sets the stable block id.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets how many layout columns this item spans.</summary>
    public int ColumnSpan {
        get => _columnSpan;
        set {
            if (value < 1 || value > 24) throw new ArgumentOutOfRangeException(nameof(value), value, "Block layout item spans must be between one and twenty-four columns.");
            _columnSpan = value;
        }
    }

    /// <summary>Gets or sets the preferred item shape.</summary>
    public BlockLayoutShape Shape { get; set; }

    /// <summary>Gets whether this item is an invisible spacer.</summary>
    public bool IsSpace { get; }
}

/// <summary>
/// Describes a link between two block layout items.
/// </summary>
public sealed class BlockLayoutEdge {
    private string _sourceId;
    private string _targetId;
    private string _label;

    /// <summary>Initializes a block layout edge.</summary>
    public BlockLayoutEdge(string sourceId, string targetId, string label = "", bool directed = true) {
        _sourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
        _targetId = targetId ?? throw new ArgumentNullException(nameof(targetId));
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Directed = directed;
    }

    /// <summary>Gets or sets the source item id.</summary>
    public string SourceId { get => _sourceId; set => _sourceId = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the target item id.</summary>
    public string TargetId { get => _targetId; set => _targetId = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the optional edge label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets whether the edge is directed.</summary>
    public bool Directed { get; set; }
}

/// <summary>
/// Identifies the preferred block layout item shape.
/// </summary>
public enum BlockLayoutShape {
    /// <summary>A rectangular block.</summary>
    Rectangle,
    /// <summary>A rounded rectangle block.</summary>
    Rounded,
    /// <summary>A circular or pill-like block, depending on available space.</summary>
    Circle,
    /// <summary>A database/cylinder-like block.</summary>
    Database
}
