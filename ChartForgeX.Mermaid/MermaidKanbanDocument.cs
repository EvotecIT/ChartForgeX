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

    /// <summary>Gets authored metadata, including fields without native behavior.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the original metadata block; host edits to values do not rewrite source facts.</summary>
    public string? MetadataSource { get; internal set; }

    /// <summary>Gets or sets the assignee through the same metadata dictionary. Null removes the field.</summary>
    public string? Assigned { get => MermaidKanbanMetadata.Get(Metadata, "assigned"); set => MermaidKanbanMetadata.Set(Metadata, "assigned", value); }

    /// <summary>Gets or sets the ticket text through the same metadata dictionary. Null removes the field.</summary>
    public string? Ticket { get => MermaidKanbanMetadata.Get(Metadata, "ticket"); set => MermaidKanbanMetadata.Set(Metadata, "ticket", value); }

    /// <summary>Gets or sets the authored priority text through the same metadata dictionary. Null removes the field.</summary>
    public string? Priority { get => MermaidKanbanMetadata.Get(Metadata, "priority"); set => MermaidKanbanMetadata.Set(Metadata, "priority", value); }

    /// <summary>Gets or sets the optional display label override through the same metadata dictionary. Null removes the field.</summary>
    public string? Label { get => MermaidKanbanMetadata.Get(Metadata, "label"); set => MermaidKanbanMetadata.Set(Metadata, "label", value); }

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

    /// <summary>Gets the original metadata block; host edits to values do not rewrite source facts.</summary>
    public string? MetadataSource { get; internal set; }

    /// <summary>Gets or sets the assignee through the same metadata dictionary. Null removes the field.</summary>
    public string? Assigned { get => MermaidKanbanMetadata.Get(Metadata, "assigned"); set => MermaidKanbanMetadata.Set(Metadata, "assigned", value); }

    /// <summary>Gets or sets the ticket text through the same metadata dictionary. Null removes the field.</summary>
    public string? Ticket { get => MermaidKanbanMetadata.Get(Metadata, "ticket"); set => MermaidKanbanMetadata.Set(Metadata, "ticket", value); }

    /// <summary>Gets or sets the authored priority text through the same metadata dictionary. Null removes the field.</summary>
    public string? Priority { get => MermaidKanbanMetadata.Get(Metadata, "priority"); set => MermaidKanbanMetadata.Set(Metadata, "priority", value); }

    /// <summary>Gets or sets the optional display label override through the same metadata dictionary. Null removes the field.</summary>
    public string? Label { get => MermaidKanbanMetadata.Get(Metadata, "label"); set => MermaidKanbanMetadata.Set(Metadata, "label", value); }

    /// <summary>Gets parsed task metadata.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);
}
