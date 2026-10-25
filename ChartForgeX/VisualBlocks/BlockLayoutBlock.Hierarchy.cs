using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

public sealed partial class BlockLayoutBlock {
    /// <summary>Gets or sets whether direct children occupy one source-ordered row. Explicit Columns is retained for restoring fixed columns.</summary>
    public bool AutoColumns { get; set; }

    /// <summary>Adds a visible item inside a named composite. Parent references may be declared later and are validated before rendering.</summary>
    public BlockLayoutBlock AddItem(string id, string label, string parentId, int columnSpan = 1, BlockLayoutShape shape = BlockLayoutShape.Rectangle) {
        _items.Add(new BlockLayoutItem(id, label, columnSpan, shape) { ParentId = parentId });
        return this;
    }

    /// <summary>Adds a composite without replacing or aggregating its source children. Null local columns inherits the parent grid.</summary>
    public BlockLayoutBlock AddComposite(string id, string label = "", int columnSpan = 1, int? columns = null, string? parentId = null, bool autoColumns = false) {
        _items.Add(new BlockLayoutItem(id, label, columnSpan) { ParentId = parentId, IsComposite = true, Columns = columns, AutoColumns = autoColumns });
        return this;
    }

    /// <summary>Adds a source-ordered invisible spacer inside a composite.</summary>
    public BlockLayoutBlock AddSpace(string parentId, int columnSpan = 1) {
        _items.Add(new BlockLayoutItem("space-" + _items.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), "", columnSpan, isSpace: true) { ParentId = parentId });
        return this;
    }
}

public sealed partial class BlockLayoutItem {
    private string? _parentId;
    private int? _columns;
    /// <summary>Gets or sets the containing composite ID. Null or empty places the item in the root grid.</summary>
    public string? ParentId { get => _parentId; set => _parentId = string.IsNullOrEmpty(value) ? null : value; }
    /// <summary>Gets or sets whether this item contains its own source-ordered grid.</summary>
    public bool IsComposite { get; set; }
    /// <summary>Gets or sets a composite's local fixed column count. Null inherits the enclosing grid; explicit auto takes precedence.</summary>
    public int? Columns {
        get => _columns;
        set {
            if (value.HasValue && (value.Value < 1 || value.Value > 24)) throw new ArgumentOutOfRangeException(nameof(value));
            _columns = value;
        }
    }
    /// <summary>Gets or sets whether this composite's children occupy one row in authored order.</summary>
    public bool AutoColumns { get; set; }
    /// <summary>Gets or sets filled arrow directions. Used only by Arrow; the default is a right-pointing arrow.</summary>
    public BlockLayoutArrowDirections ArrowDirections { get; set; } = BlockLayoutArrowDirections.Right;
    /// <summary>Gets or sets the optional captured fill; null retains the theme palette.</summary>
    public ChartColor? Fill { get; set; }
    /// <summary>Gets or sets immutable outline overrides.</summary>
    public ChartStrokeStyle? Stroke { get; set; }
    /// <summary>Gets measured font, color, case, language, variation and palette overrides. Decorated/scripted baselines are rejected before paint.</summary>
    public TextStyleOverride TextStyle { get; private set; } = new();
    /// <summary>Gets retained source facts. Metadata is not interpreted as CSS or markup.</summary>
    public Dictionary<string, string> Metadata { get; private set; } = new(StringComparer.Ordinal);
    /// <summary>Creates an independent item copy, including mutable caption and source metadata values.</summary>
    public BlockLayoutItem Clone() {
        var copy = (BlockLayoutItem)MemberwiseClone();
        copy.TextStyle = TextStyle.Clone();
        copy.Metadata = new Dictionary<string, string>(Metadata, StringComparer.Ordinal);
        return copy;
    }
}

/// <summary>Identifies the filled head directions of a native block arrow.</summary>
[Flags]
public enum BlockLayoutArrowDirections {
    /// <summary>A right-facing head.</summary>
    Right = 1,
    /// <summary>A left-facing head.</summary>
    Left = 2,
    /// <summary>An upward-facing head.</summary>
    Up = 4,
    /// <summary>A downward-facing head.</summary>
    Down = 8,
    /// <summary>Left- and right-facing heads.</summary>
    Horizontal = Right | Left,
    /// <summary>Up- and down-facing heads.</summary>
    Vertical = Up | Down
}

public sealed partial class BlockLayoutEdge {
    /// <summary>Gets or sets whether this source edge is painted. A hidden link retains its source identity and does not add a fake line.</summary>
    public bool Visible { get; set; } = true;
    /// <summary>Gets or sets immutable connector outline overrides. Null retains the incumbent axis paint.</summary>
    public ChartStrokeStyle? Stroke { get; set; }
}
