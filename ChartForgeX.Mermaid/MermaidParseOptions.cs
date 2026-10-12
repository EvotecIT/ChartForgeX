using System;

namespace ChartForgeX.Mermaid;

/// <summary>Supplies explicit context needed to resolve Mermaid source.</summary>
public sealed class MermaidParseOptions {
    /// <summary>Gets or sets the calendar date used by Gantt input formats that omit the year. Time-of-day and Kind are ignored.</summary>
    public DateTime? GanttDateAnchor { get; set; }
}
