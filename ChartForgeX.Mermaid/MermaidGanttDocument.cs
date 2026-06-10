using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid Gantt diagram.
/// </summary>
public sealed class MermaidGanttDocument : MermaidDocument {
    /// <summary>Gets retained non-empty Gantt statements.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets or sets the optional Mermaid title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the Mermaid dateFormat directive.</summary>
    public string DateFormat { get; set; } = "YYYY-MM-DD";

    /// <summary>Gets or sets the optional Mermaid axisFormat directive.</summary>
    public string? AxisFormat { get; set; }

    /// <summary>Gets or sets the optional Mermaid tickInterval directive.</summary>
    public string? TickInterval { get; set; }

    /// <summary>Gets or sets the optional Mermaid excludes directive.</summary>
    public string? Excludes { get; set; }

    /// <summary>Gets or sets the optional Mermaid todayMarker directive.</summary>
    public string? TodayMarker { get; set; }

    /// <summary>Gets Gantt sections in source order.</summary>
    public List<MermaidGanttSection> Sections { get; } = new();

    /// <summary>Gets Gantt tasks in source order.</summary>
    public List<MermaidGanttTask> Tasks { get; } = new();
}

/// <summary>
/// Describes one Mermaid Gantt section.
/// </summary>
public sealed class MermaidGanttSection : MermaidAstNode {
    /// <summary>Initializes a Gantt section.</summary>
    public MermaidGanttSection(string name, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Gantt section name must not be empty.", nameof(name));
        Name = name;
    }

    /// <summary>Gets the section name.</summary>
    public string Name { get; }
}

/// <summary>
/// Describes one Mermaid Gantt task or milestone.
/// </summary>
public sealed class MermaidGanttTask : MermaidAstNode {
    /// <summary>Initializes a Gantt task.</summary>
    public MermaidGanttTask(string title, string? id, string? section, DateTime start, DateTime end, double progress, bool milestone, IReadOnlyList<string> tags, IReadOnlyList<string> dependencies, string rawMetadata, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Gantt task title must not be empty.", nameof(title));
        Title = title;
        Id = id;
        Section = section;
        Start = start;
        End = end;
        Progress = progress;
        IsMilestone = milestone;
        Tags = new List<string>(tags ?? throw new ArgumentNullException(nameof(tags)));
        DependencyIds = new List<string>(dependencies ?? throw new ArgumentNullException(nameof(dependencies)));
        RawMetadata = rawMetadata ?? string.Empty;
    }

    /// <summary>Gets the display title.</summary>
    public string Title { get; }

    /// <summary>Gets the optional task id.</summary>
    public string? Id { get; }

    /// <summary>Gets the optional section name.</summary>
    public string? Section { get; }

    /// <summary>Gets the task start date/time.</summary>
    public DateTime Start { get; }

    /// <summary>Gets the task end date/time.</summary>
    public DateTime End { get; }

    /// <summary>Gets the rendered progress value from zero to one.</summary>
    public double Progress { get; }

    /// <summary>Gets whether the task is a milestone.</summary>
    public bool IsMilestone { get; }

    /// <summary>Gets Mermaid tags such as active, done, crit, and milestone.</summary>
    public List<string> Tags { get; }

    /// <summary>Gets referenced dependency ids from after clauses.</summary>
    public List<string> DependencyIds { get; }

    /// <summary>Gets the resolved zero-based dependency index, or -1 when none is rendered.</summary>
    public int DependencyIndex { get; internal set; } = -1;

    /// <summary>Gets raw Mermaid metadata after the task colon.</summary>
    public string RawMetadata { get; }
}
