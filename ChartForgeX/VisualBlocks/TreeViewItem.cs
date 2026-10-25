using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualBlocks;

/// <summary>Defines the meaning of a directory-tree row independently of its icon.</summary>
public enum TreeViewItemKind {
    /// <summary>A trailing slash or children identifies a directory; other rows are files.</summary>
    Auto,
    /// <summary>A file. Files cannot contain child rows.</summary>
    File,
    /// <summary>A directory, including an empty authored directory.</summary>
    Directory
}

/// <summary>Selects a built-in native icon without loading an asset or icon library.</summary>
public enum TreeViewIcon {
    /// <summary>Uses the resolved item kind when the block enables default icons.</summary>
    Auto,
    /// <summary>Does not paint an icon.</summary>
    None,
    /// <summary>Paints the native document outline.</summary>
    File,
    /// <summary>Paints the native folder outline.</summary>
    Folder,
    /// <summary>Paints a local question mark for an unresolved external icon reference.</summary>
    Unknown
}

/// <summary>An immutable row with stable identity, parent identity and complete authored text.</summary>
public sealed class TreeViewItem {
    private readonly IReadOnlyList<string> _classes;
    /// <summary>Initializes a row. Parent rows precede their descendants in contiguous preorder.
    /// IDs contain at most 4096 characters. Class names and icon references are inert metadata; only Highlighted and the built-in Icon affect paint.</summary>
    public TreeViewItem(string id, string label, string? parentId = null, string? description = null,
        TreeViewItemKind kind = TreeViewItemKind.Auto, TreeViewIcon icon = TreeViewIcon.Auto,
        bool highlighted = false, string? iconReference = null, IEnumerable<string>? classes = null) {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 4096) throw new ArgumentException("A tree item ID must contain 1 through 4096 characters.", nameof(id));
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("A tree item label is required.", nameof(label));
        if (parentId != null && (string.IsNullOrWhiteSpace(parentId) || parentId.Length > 4096)) throw new ArgumentException("Parent ID must be null or contain 1 through 4096 characters.", nameof(parentId));
        VisualBlockGuards.EnumDefined(kind, nameof(kind)); VisualBlockGuards.EnumDefined(icon, nameof(icon));
        Id = id; Label = label; ParentId = parentId; Description = description ?? string.Empty;
        Kind = kind; Icon = icon; Highlighted = highlighted; IconReference = iconReference;
        var copy = new List<string>();
        if (classes != null) foreach (var value in classes) {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Tree item classes must be non-empty.", nameof(classes));
            if (copy.Count >= 64) throw new ArgumentException("A tree item supports at most 64 class names.", nameof(classes));
            copy.Add(value);
        }
        _classes = copy.AsReadOnly();
    }
    /// <summary>Gets the stable row identity.</summary>
    public string Id { get; }
    /// <summary>Gets the complete source label, even when its ink is shortened.</summary>
    public string Label { get; }
    /// <summary>Gets the parent identity, or null for a root row.</summary>
    public string? ParentId { get; }
    /// <summary>Gets the complete description, painted beside or below the label.</summary>
    public string Description { get; }
    /// <summary>Gets the authored file/directory meaning.</summary>
    public TreeViewItemKind Kind { get; }
    /// <summary>Gets the native icon policy.</summary>
    public TreeViewIcon Icon { get; }
    /// <summary>Gets whether the row has an explicit theme-aware highlight.</summary>
    public bool Highlighted { get; }
    /// <summary>Gets an inert original icon name, without resolving or fetching assets.</summary>
    public string? IconReference { get; }
    /// <summary>Gets a copied, read-only list of inert source class names.</summary>
    public IReadOnlyList<string> Classes => _classes;
}
