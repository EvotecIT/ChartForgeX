using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid kanban board.
/// </summary>
public sealed class MermaidKanbanDocument : MermaidDocument {
    /// <summary>Gets columns in source order.</summary>
    public List<MermaidKanbanColumn> Columns { get; } = new();
}

/// <summary>
/// Describes one Mermaid kanban column.
/// </summary>
public sealed class MermaidKanbanColumn : MermaidAstNode {
    /// <summary>Initializes a kanban column.</summary>
    public MermaidKanbanColumn(string id, string title, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Kanban column id must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Kanban column title must not be empty.", nameof(title));
        Id = id;
        Title = title;
    }

    /// <summary>Gets the column id.</summary>
    public string Id { get; }

    /// <summary>Gets the column title.</summary>
    public string Title { get; }

    /// <summary>Gets tasks in source order.</summary>
    public List<MermaidKanbanTask> Tasks { get; } = new();
}

/// <summary>
/// Describes one Mermaid kanban task.
/// </summary>
public sealed class MermaidKanbanTask : MermaidAstNode {
    /// <summary>Initializes a kanban task.</summary>
    public MermaidKanbanTask(string id, string title, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Kanban task id must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Kanban task title must not be empty.", nameof(title));
        Id = id;
        Title = title;
    }

    /// <summary>Gets the task id.</summary>
    public string Id { get; }

    /// <summary>Gets the task title.</summary>
    public string Title { get; }

    /// <summary>Gets parsed task metadata.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);
}
