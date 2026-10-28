using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

/// <summary>A reusable static directory tree with source-ordered rows and shared SVG/native PNG geometry.</summary>
/// <remarks>Rows use contiguous preorder and parent-before-child IDs. Native limits are 10000 rows,
/// 128 levels and an 8192-pixel frame per axis. Call WithNaturalSize after changing data, width,
/// typography or padding. An undersized explicit frame rejects rather than reducing text to unreadable sizes.</remarks>
public sealed class TreeViewBlock : VisualBlock<TreeViewBlock> {
    private readonly List<TreeViewItem> _items = new();
    private readonly IReadOnlyList<TreeViewItem> _view;
    private double _rowIndent = 18;
    private double _rowPadding = 5;
    /// <summary>Initializes an empty directory tree.</summary>
    public TreeViewBlock() { _view = _items.AsReadOnly(); }
    /// <summary>Gets a concise accessible name.</summary>
    public override string AccessibleName => Options.Accessibility.Name ?? (Title.Length == 0 ? "Directory tree" : Title);
    /// <summary>Gets immutable rows in authored preorder.</summary>
    public IReadOnlyList<TreeViewItem> Items => _view;
    /// <summary>Gets or sets whether Auto icons display native files and folders. Explicit icons are independent.</summary>
    public bool ShowIcons { get; set; }
    /// <summary>Gets or sets indentation per level in logical pixels, from 4 through 256.</summary>
    public double RowIndent {
        get => _rowIndent;
        set { Bounded(value, 4, 256, nameof(value)); _rowIndent = value; }
    }
    /// <summary>Gets or sets vertical padding on each side of a row, from zero through 64 logical pixels.</summary>
    public double RowPadding {
        get => _rowPadding;
        set { Bounded(value, 0, 64, nameof(value)); _rowPadding = value; }
    }
    /// <summary>Creates an empty directory tree.</summary>
    public static TreeViewBlock Create() => new();
    /// <summary>Adds one complete immutable row.</summary>
    public TreeViewBlock AddItem(TreeViewItem item) {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (_items.Count >= VisualBlockRendering.MaximumTreeViewItems) throw new InvalidOperationException("Tree views support at most 10000 rows.");
        _items.Add(item); return this;
    }
    /// <summary>Adds a row with automatic directory and icon policies.</summary>
    public TreeViewBlock AddItem(string id, string label, string? parentId = null, string? description = null) =>
        AddItem(new TreeViewItem(id, label, parentId, description));
    /// <summary>Measures a complete fixed-width frame with bounded natural height.</summary>
    public ChartSize MeasureNaturalSize(int width = 520, int minimumHeight = 0) => VisualBlockRendering.TreeViewNaturalSize(this, width, minimumHeight);
    /// <summary>Captures the measured fixed-width natural frame without silently dropping rows.</summary>
    public TreeViewBlock WithNaturalSize(int width = 520, int minimumHeight = 0) {
        Options.Size = MeasureNaturalSize(width, minimumHeight); return this;
    }
    private static void Bounded(double value, double minimum, double maximum, string parameter) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(parameter, value, "Value is outside the supported tree-row range.");
    }
}
