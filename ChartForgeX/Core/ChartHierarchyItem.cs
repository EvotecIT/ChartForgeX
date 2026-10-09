using System;

namespace ChartForgeX.Core;

/// <summary>An area-hierarchy node with explicit identity, optional parent, authored size and independent numeric color.</summary>
public readonly struct ChartHierarchyItem {
    private readonly ChartNode _node;
    /// <summary>Gets the authored ID used by parents and interactive targets.</summary>
    public string Id => _node.Id;
    /// <summary>Gets the display label, which may repeat on other nodes.</summary>
    public string Label => _node.Label;
    /// <summary>Gets the parent ID, or null for a root or standalone leaf.</summary>
    public string? ParentId { get; }
    /// <summary>Gets the authored size. Leaves require a value; the chart's parent-value policy determines group sizing.</summary>
    public double? Value { get; }
    /// <summary>Gets the independent numeric color value, or null when no color measurement is supplied.</summary>
    public double? ColorValue { get; }
    internal ChartNode Node => _node;

    /// <summary>Creates a node. Leaf/group size requirements are validated with the complete item collection and chart policy.</summary>
    public ChartHierarchyItem(string id, string label, string? parentId = null, double? value = null, double? colorValue = null) {
        _node = new ChartNode(id, label);
        if (parentId != null && string.IsNullOrWhiteSpace(parentId)) throw new ArgumentException("Parent ID must not be empty.", nameof(parentId));
        if (value.HasValue) {
            ChartGuards.Finite(value.Value, nameof(value));
            if (value.Value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Hierarchy sizes must be zero or greater.");
        }
        if (colorValue.HasValue) ChartGuards.Finite(colorValue.Value, nameof(colorValue));
        ParentId = parentId;
        Value = value;
        ColorValue = colorValue;
    }
}
