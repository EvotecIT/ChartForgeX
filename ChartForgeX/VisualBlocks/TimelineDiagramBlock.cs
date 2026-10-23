using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualBlocks;

/// <summary>Defines the reading direction of a categorical timeline.</summary>
public enum TimelineDiagramDirection {
    /// <summary>Periods progress from left to right; events stack below each period.</summary>
    LeftToRight,
    /// <summary>Periods progress from top to bottom; events follow to their right.</summary>
    TopToBottom
}

/// <summary>An ordered categorical timeline. Period labels need not be dates or quantities.</summary>
public sealed class TimelineDiagramBlock : VisualBlock<TimelineDiagramBlock> {
    private readonly List<TimelineSection> _sections = new();
    private readonly List<TimelinePeriod> _periods = new();
    private TimelineDiagramDirection _direction;
    private readonly IReadOnlyList<TimelineSection> _sectionView;
    private readonly IReadOnlyList<TimelinePeriod> _periodView;
    /// <summary>Initializes an empty native diagram.</summary>
    public TimelineDiagramBlock() { _sectionView = _sections.AsReadOnly(); _periodView = _periods.AsReadOnly(); }
    /// <summary>Gets a concise accessible diagram name.</summary>
    public override string AccessibleName => Options.Accessibility.Name ?? (Title.Length == 0 ? "Timeline diagram" : Title);
    /// <summary>Gets sections in authored order.</summary>
    public IReadOnlyList<TimelineSection> Sections => _sectionView;
    /// <summary>Gets periods and their ordered events.</summary>
    public IReadOnlyList<TimelinePeriod> Periods => _periodView;
    /// <summary>Gets or sets the reading direction.</summary>
    public TimelineDiagramDirection Direction { get => _direction; set { VisualBlockGuards.EnumDefined(value, nameof(value)); _direction = value; } }
    /// <summary>Creates an empty categorical timeline.</summary>
    public static TimelineDiagramBlock Create() => new();
    /// <summary>Adds a phase with a stable identity independent of its display label.</summary>
    public TimelineDiagramBlock AddSection(string id, string label) { _sections.Add(new TimelineSection(id, label)); return this; }
    /// <summary>Adds a period and its ordered events, without inventing a numeric duration.</summary>
    public TimelineDiagramBlock AddPeriod(string id, string label, IEnumerable<string> events, string? sectionId = null) {
        _periods.Add(new TimelinePeriod(id, label, events, sectionId)); return this;
    }
}

/// <summary>A named chronological phase.</summary>
public sealed class TimelineSection {
    /// <summary>Initializes a phase.</summary>
    public TimelineSection(string id, string label) { Id = id ?? throw new ArgumentNullException(nameof(id)); Label = label ?? throw new ArgumentNullException(nameof(label)); }
    /// <summary>Gets the stable phase identity.</summary>
    public string Id { get; }
    /// <summary>Gets the display label.</summary>
    public string Label { get; }
}

/// <summary>A categorical period and its events; the order conveys chronology.</summary>
public sealed class TimelinePeriod {
    private readonly IReadOnlyList<string> _events;
    /// <summary>Initializes a period.</summary>
    public TimelinePeriod(string id, string label, IEnumerable<string> events, string? sectionId = null) {
        Id = id ?? throw new ArgumentNullException(nameof(id)); Label = label ?? throw new ArgumentNullException(nameof(label));
        _events = new List<string>(events ?? throw new ArgumentNullException(nameof(events))).AsReadOnly(); SectionId = sectionId;
    }
    /// <summary>Gets the stable period identity.</summary>
    public string Id { get; }
    /// <summary>Gets the period label, with no numeric or calendar inference.</summary>
    public string Label { get; }
    /// <summary>Gets events in authored order.</summary>
    public IReadOnlyList<string> Events => _events;
    /// <summary>Gets the containing phase, or null for an unsectioned period.</summary>
    public string? SectionId { get; }
}
