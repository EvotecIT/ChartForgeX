using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// A compact horizontal date or period selector strip for dashboards and report snapshots.
/// </summary>
public sealed class DateStripBlock : VisualBlock<DateStripBlock> {
    private readonly List<DateStripItem> _items = new();
    private string _header = string.Empty;
    private string _previousSymbol = "<";
    private string _nextSymbol = ">";

    /// <summary>Gets date strip items.</summary>
    public IReadOnlyList<DateStripItem> Items => _items;

    /// <summary>Gets or sets the header text rendered above the strip.</summary>
    public string Header { get => _header; set => _header = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets whether previous and next controls are rendered.</summary>
    public bool ShowNavigation { get; set; }

    /// <summary>Gets or sets the previous control symbol.</summary>
    public string PreviousSymbol { get => _previousSymbol; set => _previousSymbol = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the next control symbol.</summary>
    public string NextSymbol { get => _nextSymbol; set => _nextSymbol = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Creates a new date strip block.</summary>
    public static DateStripBlock Create() => new();

    /// <summary>Sets the header text rendered above the strip.</summary>
    public DateStripBlock WithHeader(string header) { Header = header ?? throw new ArgumentNullException(nameof(header)); return this; }

    /// <summary>Sets whether previous and next controls are rendered.</summary>
    public DateStripBlock WithNavigation(bool enabled = true) { ShowNavigation = enabled; return this; }

    /// <summary>Sets previous and next control symbols.</summary>
    public DateStripBlock WithNavigationSymbols(string previous, string next) {
        PreviousSymbol = previous ?? throw new ArgumentNullException(nameof(previous));
        NextSymbol = next ?? throw new ArgumentNullException(nameof(next));
        return this;
    }

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
public sealed class EntityStripBlock : VisualBlock<EntityStripBlock> {
    private readonly List<EntityStripItem> _items = new();
    private string _actionLabel = string.Empty;
    private string _actionSymbol = "+";
    private string _actionUrl = string.Empty;

    /// <summary>Gets entity strip items.</summary>
    public IReadOnlyList<EntityStripItem> Items => _items;

    /// <summary>Gets or sets optional action text.</summary>
    public string ActionLabel { get => _actionLabel; set => _actionLabel = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional action symbol.</summary>
    public string ActionSymbol { get => _actionSymbol; set => _actionSymbol = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets an optional URL for the action in SVG/HTML outputs.</summary>
    public string ActionUrl { get => _actionUrl; set => _actionUrl = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Creates a new entity strip block.</summary>
    public static EntityStripBlock Create() => new();

    /// <summary>Adds one entity item.</summary>
    public EntityStripBlock AddItem(string label, string? avatarText = null, ChartColor? color = null, VisualStatus status = VisualStatus.None) {
        _items.Add(new EntityStripItem(label, avatarText) { Color = color, Status = status });
        return this;
    }

    /// <summary>Sets optional action text rendered in the strip header.</summary>
    public EntityStripBlock WithAction(string label, string? symbol = null, string? url = null) {
        ActionLabel = label ?? throw new ArgumentNullException(nameof(label));
        ActionSymbol = symbol ?? "+";
        ActionUrl = url ?? string.Empty;
        return this;
    }
}

/// <summary>
/// Describes one entity in an entity strip.
/// </summary>
public sealed class EntityStripItem {
    private string _label;
    private string _avatarText;
    private VisualStatus _status;

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
    public VisualStatus Status {
        get => _status;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }
}

/// <summary>
/// A compact dashboard section header for separating groups inside visual grids.
/// </summary>
public sealed class SectionHeaderBlock : VisualBlock<SectionHeaderBlock> {
    private string _actionLabel = string.Empty;
    private string _actionSymbol = string.Empty;
    private string _actionUrl = string.Empty;

    /// <summary>Gets or sets optional action text.</summary>
    public string ActionLabel { get => _actionLabel; set => _actionLabel = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional action symbol.</summary>
    public string ActionSymbol { get => _actionSymbol; set => _actionSymbol = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets an optional URL for the action in SVG/HTML outputs.</summary>
    public string ActionUrl { get => _actionUrl; set => _actionUrl = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Creates a new section header block.</summary>
    public static SectionHeaderBlock Create() => new();

    /// <summary>Sets optional action text rendered on the trailing side.</summary>
    public SectionHeaderBlock WithAction(string label, string? symbol = null, string? url = null) {
        ActionLabel = label ?? throw new ArgumentNullException(nameof(label));
        ActionSymbol = symbol ?? string.Empty;
        ActionUrl = url ?? string.Empty;
        return this;
    }
}
