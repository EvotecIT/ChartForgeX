using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// A dashboard card for people, workload, or ranked rows with avatar slots and progress rails.
/// </summary>
public sealed class WorkloadListBlock : VisualBlock<WorkloadListBlock> {
    private readonly List<WorkloadListRow> _rows = new();
    private string _actionLabel = string.Empty;
    private string _actionSymbol = ">";
    private string _actionUrl = string.Empty;

    /// <summary>Gets workload rows.</summary>
    public IReadOnlyList<WorkloadListRow> Rows => _rows;

    /// <summary>Gets or sets whether progress rails are shown.</summary>
    public bool ShowProgressRails { get; set; } = true;

    /// <summary>Gets or sets whether checkbox-style selection controls are shown.</summary>
    public bool ShowSelectionControls { get; set; }

    /// <summary>Gets or sets whether row dividers are shown.</summary>
    public bool ShowDividers { get; set; } = true;

    /// <summary>Gets optional footer action text.</summary>
    public string ActionLabel { get => _actionLabel; set => _actionLabel = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets optional footer action symbol.</summary>
    public string ActionSymbol { get => _actionSymbol; set => _actionSymbol = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets optional footer action URL for SVG/HTML outputs.</summary>
    public string ActionUrl { get => _actionUrl; set => _actionUrl = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Creates a new workload list block.</summary>
    public static WorkloadListBlock Create() => new();

    /// <summary>Adds a person workload row.</summary>
    public WorkloadListBlock AddPerson(string name, string? subtitle, double value, double maximum = 100, VisualStatus status = VisualStatus.Neutral, string? avatarText = null, string? displayValue = null, ChartColor? color = null, string? note = null, bool selected = false) {
        _rows.Add(new WorkloadListRow(name, subtitle, value, maximum, status, avatarText, displayValue, color, note, selected));
        return this;
    }

    /// <summary>Sets whether progress rails are shown.</summary>
    public WorkloadListBlock WithProgressRails(bool enabled = true) { ShowProgressRails = enabled; return this; }

    /// <summary>Sets whether checkbox-style selection controls are shown.</summary>
    public WorkloadListBlock WithSelectionControls(bool enabled = true) { ShowSelectionControls = enabled; return this; }

    /// <summary>Sets whether row dividers are shown.</summary>
    public WorkloadListBlock WithDividers(bool enabled = true) { ShowDividers = enabled; return this; }

    /// <summary>Sets optional footer action text.</summary>
    public WorkloadListBlock WithAction(string label, string? symbol = null, string? url = null) {
        ActionLabel = label ?? throw new ArgumentNullException(nameof(label));
        ActionSymbol = symbol ?? ">";
        ActionUrl = url ?? string.Empty;
        return this;
    }
}

/// <summary>
/// One row in a workload list.
/// </summary>
public sealed class WorkloadListRow {
    private string _label;
    private string _subtitle = string.Empty;
    private string _avatarText = string.Empty;
    private string _displayValue = string.Empty;
    private string _note = string.Empty;
    private VisualStatus _status;

    /// <summary>Initializes a workload row.</summary>
    public WorkloadListRow(string label, string? subtitle, double value, double maximum = 100, VisualStatus status = VisualStatus.Neutral, string? avatarText = null, string? displayValue = null, ChartColor? color = null, string? note = null, bool selected = false) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _subtitle = subtitle ?? string.Empty;
        Value = value;
        Maximum = maximum;
        Status = status;
        _avatarText = avatarText ?? string.Empty;
        _displayValue = displayValue ?? string.Empty;
        Color = color;
        _note = note ?? string.Empty;
        Selected = selected;
    }

    /// <summary>Gets or sets the primary row label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets secondary row text.</summary>
    public string Subtitle { get => _subtitle; set => _subtitle = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the progress value.</summary>
    public double Value { get; set; }

    /// <summary>Gets or sets the progress maximum.</summary>
    public double Maximum { get; set; }

    /// <summary>Gets or sets the semantic row status.</summary>
    public VisualStatus Status {
        get => _status;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }

    /// <summary>Gets or sets avatar initials or a compact symbol.</summary>
    public string AvatarText { get => _avatarText; set => _avatarText = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets a right-aligned value override.</summary>
    public string DisplayValue { get => _displayValue; set => _displayValue = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets an optional row accent color.</summary>
    public ChartColor? Color { get; set; }

    /// <summary>Gets or sets optional compact status note text.</summary>
    public string Note { get => _note; set => _note = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets whether the row is selected.</summary>
    public bool Selected { get; set; }
}

/// <summary>
/// Item kind used by activity timeline blocks.
/// </summary>
public enum ActivityTimelineItemKind {
    /// <summary>A section label in the feed.</summary>
    Section,
    /// <summary>A primary timeline event.</summary>
    Event,
    /// <summary>A nested checklist item below the previous event.</summary>
    ChecklistItem,
    /// <summary>A compact collapsed-items summary.</summary>
    HiddenSummary
}

/// <summary>
/// A dashboard activity feed with section labels, status nodes, connectors, and nested checklist rows.
/// </summary>
public sealed class ActivityTimelineBlock : VisualBlock<ActivityTimelineBlock> {
    private readonly List<ActivityTimelineItem> _items = new();

    /// <summary>Gets timeline items.</summary>
    public IReadOnlyList<ActivityTimelineItem> Items => _items;

    /// <summary>Gets or sets whether event rows render a contained surface behind their content.</summary>
    public bool ShowEventSurfaces { get; set; } = true;

    /// <summary>Creates a new activity timeline block.</summary>
    public static ActivityTimelineBlock Create() => new();

    /// <summary>Sets whether primary events render as card-like surfaces or compact inline rows.</summary>
    public ActivityTimelineBlock WithEventSurfaces(bool enabled = true) {
        ShowEventSurfaces = enabled;
        return this;
    }

    /// <summary>Adds a section label.</summary>
    public ActivityTimelineBlock AddSection(string label) {
        _items.Add(ActivityTimelineItem.Section(label));
        return this;
    }

    /// <summary>Adds a primary timeline event.</summary>
    public ActivityTimelineBlock AddEvent(string title, string? timestamp = null, VisualStatus status = VisualStatus.Neutral, string? badge = null, string? detail = null, string? symbol = null) {
        _items.Add(ActivityTimelineItem.Event(title, timestamp, status, badge, detail, symbol));
        return this;
    }

    /// <summary>Adds a nested checklist item.</summary>
    public ActivityTimelineBlock AddChecklistItem(string text, bool completed, bool muted = false) {
        _items.Add(ActivityTimelineItem.Checklist(text, completed, muted));
        return this;
    }

    /// <summary>Adds a compact hidden-items summary.</summary>
    public ActivityTimelineBlock AddHiddenSummary(int count, string label) {
        _items.Add(ActivityTimelineItem.Hidden(count, label));
        return this;
    }
}

/// <summary>
/// One item in an activity timeline.
/// </summary>
public sealed class ActivityTimelineItem {
    private string _title;
    private string _timestamp = string.Empty;
    private string _badge = string.Empty;
    private string _detail = string.Empty;
    private string _symbol = string.Empty;
    private VisualStatus _status;
    private ActivityTimelineItemKind _kind;

    private ActivityTimelineItem(ActivityTimelineItemKind kind, string title) {
        Kind = kind;
        _title = title ?? throw new ArgumentNullException(nameof(title));
    }

    /// <summary>Gets or sets the item kind.</summary>
    public ActivityTimelineItemKind Kind {
        get => _kind;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _kind = value;
        }
    }

    /// <summary>Gets or sets the item title.</summary>
    public string Title { get => _title; set => _title = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional timestamp text.</summary>
    public string Timestamp { get => _timestamp; set => _timestamp = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional compact badge text.</summary>
    public string Badge { get => _badge; set => _badge = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional detail text.</summary>
    public string Detail { get => _detail; set => _detail = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional compact symbol text rendered inside the event node.</summary>
    public string Symbol { get => _symbol; set => _symbol = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets semantic item status.</summary>
    public VisualStatus Status {
        get => _status;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }

    /// <summary>Gets or sets whether checklist text should render completed.</summary>
    public bool Completed { get; set; }

    /// <summary>Gets or sets whether text should render muted.</summary>
    public bool Muted { get; set; }

    /// <summary>Gets or sets hidden item count for summaries.</summary>
    public int HiddenCount { get; set; }

    /// <summary>Creates a section item.</summary>
    public static ActivityTimelineItem Section(string label) => new(ActivityTimelineItemKind.Section, label);

    /// <summary>Creates an event item.</summary>
    public static ActivityTimelineItem Event(string title, string? timestamp, VisualStatus status, string? badge, string? detail, string? symbol = null) {
        return new ActivityTimelineItem(ActivityTimelineItemKind.Event, title) {
            Timestamp = timestamp ?? string.Empty,
            Status = status,
            Badge = badge ?? string.Empty,
            Detail = detail ?? string.Empty,
            Symbol = symbol ?? string.Empty
        };
    }

    /// <summary>Creates a checklist item.</summary>
    public static ActivityTimelineItem Checklist(string text, bool completed, bool muted) {
        return new ActivityTimelineItem(ActivityTimelineItemKind.ChecklistItem, text) {
            Completed = completed,
            Muted = muted,
            Status = completed ? VisualStatus.Positive : VisualStatus.Info
        };
    }

    /// <summary>Creates a hidden summary item.</summary>
    public static ActivityTimelineItem Hidden(int count, string label) {
        return new ActivityTimelineItem(ActivityTimelineItemKind.HiddenSummary, label) {
            HiddenCount = count,
            Status = VisualStatus.Info
        };
    }
}
