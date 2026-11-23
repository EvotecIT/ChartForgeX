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

/// <summary>The icon keys a tree row draws natively. <see cref="Icon.None"/> paints nothing and any other key paints a
/// question mark, without loading an asset or icon library.</summary>
public static class TreeViewIcons {
    /// <summary>Document outline: <c>file</c>.</summary>
    public static Icon File { get; } = Icon.Named("file");
    /// <summary>Folder outline: <c>folder</c>.</summary>
    public static Icon Folder { get; } = Icon.Named("folder");

    internal static TreeViewIcon Glyph(Icon? icon, bool showIcons, bool directory) {
        if (icon == null) return showIcons ? directory ? TreeViewIcon.Folder : TreeViewIcon.File : TreeViewIcon.None;
        if (icon.Value.IsEmpty) return TreeViewIcon.None;
        return icon.Value.Equals(File) ? TreeViewIcon.File : icon.Value.Equals(Folder) ? TreeViewIcon.Folder : TreeViewIcon.Unknown;
    }
}

internal enum TreeViewIcon { None, File, Folder, Unknown }

/// <summary>An immutable row with stable identity, parent identity and complete authored text.</summary>
public sealed class TreeViewItem {
    private readonly IReadOnlyList<string> _classes;
    /// <summary>Initializes a row. Parent rows precede their descendants in contiguous preorder.
    /// IDs contain at most 4096 characters. Class names and icon references are inert metadata; only Highlighted and Icon affect paint.</summary>
    public TreeViewItem(string id, string label, string? parentId = null, string? description = null,
        TreeViewItemKind kind = TreeViewItemKind.Auto, Icon? icon = null,
        bool highlighted = false, string? iconReference = null, IEnumerable<string>? classes = null) {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 4096) throw new ArgumentException("A tree item ID must contain 1 through 4096 characters.", nameof(id));
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("A tree item label is required.", nameof(label));
        if (parentId != null && (string.IsNullOrWhiteSpace(parentId) || parentId.Length > 4096)) throw new ArgumentException("Parent ID must be null or contain 1 through 4096 characters.", nameof(parentId));
        VisualBlockGuards.EnumDefined(kind, nameof(kind));
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
    /// <summary>Gets the row icon: null follows the item kind when the block shows icons, <see cref="Icon.None"/> paints
    /// nothing, and <see cref="TreeViewIcons"/> lists the keys drawn natively.</summary>
    public Icon? Icon { get; }
    /// <summary>Gets whether the row has an explicit theme-aware highlight.</summary>
    public bool Highlighted { get; }
    /// <summary>Gets an inert original icon name, without resolving or fetching assets.</summary>
    public string? IconReference { get; }
    /// <summary>Gets a copied, read-only list of inert source class names.</summary>
    public IReadOnlyList<string> Classes => _classes;
}
