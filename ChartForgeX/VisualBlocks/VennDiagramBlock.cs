using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// A deterministic static Venn diagram for one to three sets.
/// </summary>
public sealed class VennDiagramBlock : VisualBlock<VennDiagramBlock> {
    private readonly List<VennSet> _sets = new();
    private readonly List<VennIntersection> _intersections = new();
    private readonly List<VennTextNode> _textNodes = new();

    /// <summary>Gets Venn sets in source/render order.</summary>
    public IReadOnlyList<VennSet> Sets => _sets;

    /// <summary>Gets declared set intersections.</summary>
    public IReadOnlyList<VennIntersection> Intersections => _intersections;

    /// <summary>Gets free-form labels attached to sets or intersections.</summary>
    public IReadOnlyList<VennTextNode> TextNodes => _textNodes;

    /// <summary>Gets a concise accessibility label.</summary>
    public override string AccessibleName => Title.Length == 0 ? "Venn diagram" : Title;

    /// <summary>Creates a Venn diagram block.</summary>
    public static VennDiagramBlock Create() => new();

    /// <summary>Adds a Venn set.</summary>
    public VennDiagramBlock AddSet(string id, string? label = null, double size = 10) {
        _sets.Add(new VennSet(id, label ?? id, size));
        return this;
    }

    /// <summary>Adds an intersection between two or three sets.</summary>
    public VennDiagramBlock AddIntersection(IEnumerable<string> setIds, string? label = null, double size = 1) {
        _intersections.Add(new VennIntersection(setIds, label ?? string.Empty, size));
        return this;
    }

    /// <summary>Adds an extra text label for a set or intersection.</summary>
    public VennDiagramBlock AddText(IEnumerable<string> setIds, string id, string? label = null) {
        _textNodes.Add(new VennTextNode(setIds, id, label ?? id));
        return this;
    }
}

/// <summary>
/// Describes a Venn set.
/// </summary>
public sealed class VennSet {
    private string _id;
    private string _label;

    /// <summary>Initializes a Venn set.</summary>
    public VennSet(string id, string label, double size) {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Size = size;
    }

    /// <summary>Gets or sets the stable set id.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the source size value.</summary>
    public double Size { get; set; }

    /// <summary>Gets or sets the optional fill color.</summary>
    public ChartColor? Fill { get; set; }

    /// <summary>Gets or sets the optional stroke color.</summary>
    public ChartColor? Stroke { get; set; }

    /// <summary>Gets or sets the optional text color.</summary>
    public ChartColor? TextColor { get; set; }
}

/// <summary>
/// Describes a Venn set intersection.
/// </summary>
public sealed class VennIntersection {
    private readonly List<string> _setIds;
    private string _label;

    /// <summary>Initializes a Venn intersection.</summary>
    public VennIntersection(IEnumerable<string> setIds, string label, double size) {
        if (setIds == null) throw new ArgumentNullException(nameof(setIds));
        _setIds = new List<string>(setIds);
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Size = size;
    }

    /// <summary>Gets participating set ids.</summary>
    public IReadOnlyList<string> SetIds => _setIds;

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the source size value.</summary>
    public double Size { get; set; }

    /// <summary>Gets or sets the optional fill color.</summary>
    public ChartColor? Fill { get; set; }

    /// <summary>Gets or sets the optional stroke color.</summary>
    public ChartColor? Stroke { get; set; }

    /// <summary>Gets or sets the optional text color.</summary>
    public ChartColor? TextColor { get; set; }
}

/// <summary>
/// Describes an extra Venn text label.
/// </summary>
public sealed class VennTextNode {
    private readonly List<string> _setIds;
    private string _id;
    private string _label;

    /// <summary>Initializes a Venn text node.</summary>
    public VennTextNode(IEnumerable<string> setIds, string id, string label) {
        if (setIds == null) throw new ArgumentNullException(nameof(setIds));
        _setIds = new List<string>(setIds);
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _label = label ?? throw new ArgumentNullException(nameof(label));
    }

    /// <summary>Gets participating set ids.</summary>
    public IReadOnlyList<string> SetIds => _setIds;

    /// <summary>Gets or sets the stable text node id.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the optional text color.</summary>
    public ChartColor? TextColor { get; set; }
}
