using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// A compact horizontal date or period selector strip for dashboards and report snapshots.
/// </summary>
[Fluent]
public sealed class DateStripBlock : FactualVisualBlock<DateStripBlock> {
    private readonly List<DateStripItem> _items = new();
    private string _header = string.Empty;



    /// <summary>Gets date strip items.</summary>
    public IReadOnlyList<DateStripItem> Items => _items;

    /// <summary>Gets or sets the header text rendered above the strip.</summary>
    public string Header { get => _header; set => _header = value ?? throw new ArgumentNullException(nameof(value)); }




    /// <summary>Creates a new date strip block.</summary>
    public static DateStripBlock Create() => new();



    /// <summary>Adds a date item.</summary>
    public DateStripBlock AddItem(string label, string value, bool selected = false, ChartColor? color = null) {
        _items.Add(new DateStripItem(label, value) { Selected = selected, Color = color });
        return this;
    }
}

/// <summary>
/// Describes one item in a date strip.
/// </summary>
public sealed class DateStripItem {
    private string _label;
    private string _value;

    /// <summary>Initializes a date strip item.</summary>
    public DateStripItem(string label, string value) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the upper label, such as a weekday initial.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the lower value, such as a day number.</summary>
    public string Value { get => _value; set => _value = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets whether the item is selected.</summary>
    public bool Selected { get; set; }

    /// <summary>Gets or sets an optional item accent color.</summary>
    public ChartColor? Color { get; set; }
}

/// <summary>
/// A compact horizontal entity strip for people, systems, teams, or owners.
/// </summary>
public sealed class EntityStripBlock : FactualVisualBlock<EntityStripBlock> {
    private readonly List<EntityStripItem> _items = new();




    /// <summary>Gets entity strip items.</summary>
    public IReadOnlyList<EntityStripItem> Items => _items;




    /// <summary>Creates a new entity strip block.</summary>
    public static EntityStripBlock Create() => new();

    /// <summary>Adds one entity item.</summary>
    public EntityStripBlock AddItem(string label, string? avatarText = null, ChartColor? color = null, Tone tone = default) {
        _items.Add(new EntityStripItem(label, avatarText) { Color = color, Tone = tone });
        return this;
    }

}

/// <summary>
/// Describes one entity in an entity strip.
/// </summary>
public sealed class EntityStripItem {
    private string _label;
    private string _avatarText;
    private Tone _tone;

    /// <summary>Initializes an entity strip item.</summary>
    public EntityStripItem(string label, string? avatarText = null) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _avatarText = avatarText ?? string.Empty;
    }

    /// <summary>Gets or sets the entity label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional avatar text.</summary>
    public string AvatarText { get => _avatarText; set => _avatarText = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets an optional accent color.</summary>
    public ChartColor? Color { get; set; }

    /// <summary>Gets or sets an optional semantic status.</summary>
    public Tone Tone { get => _tone; set => _tone = value; }
}

/// <summary>
/// A compact dashboard section header for separating groups inside visual grids.
/// </summary>
public sealed class SectionHeaderBlock : FactualVisualBlock<SectionHeaderBlock> {







    /// <summary>Creates a new section header block.</summary>
    public static SectionHeaderBlock Create() => new();

}
