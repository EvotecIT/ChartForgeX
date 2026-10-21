using System;

namespace ChartForgeX.Mermaid;

/// <summary>Supplies explicit context needed to resolve Mermaid source.</summary>
public sealed class MermaidParseOptions {
    private MermaidResourceLimits _resourceLimits = new();
    /// <summary>Gets or sets resource bounds captured independently for each parse request.</summary>
    public MermaidResourceLimits ResourceLimits { get => _resourceLimits; set => _resourceLimits = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets the calendar date used by Gantt input formats that omit the year. Time-of-day and Kind are ignored.</summary>
    public DateTime? GanttDateAnchor { get; set; }

    /// <summary>Captures calendar context and independent resource limits for one operation.</summary>
    public MermaidParseOptions Clone() => new() { GanttDateAnchor = GanttDateAnchor, ResourceLimits = ResourceLimits.Clone() };
}
