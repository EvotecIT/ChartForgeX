using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Identifies a Mermaid diagram family.
/// </summary>
public enum MermaidDiagramKind {
    /// <summary>The diagram family could not be identified.</summary>
    Unknown,
    /// <summary>A flowchart or graph diagram.</summary>
    Flowchart,
    /// <summary>A sequence diagram.</summary>
    Sequence,
    /// <summary>A class diagram.</summary>
    Class,
    /// <summary>A state diagram.</summary>
    State,
    /// <summary>An entity relationship diagram.</summary>
    EntityRelationship,
    /// <summary>A Gantt diagram.</summary>
    Gantt,
    /// <summary>A pie chart diagram.</summary>
    Pie,
    /// <summary>A journey diagram.</summary>
    Journey,
    /// <summary>A Git graph diagram.</summary>
    GitGraph,
    /// <summary>A mind map diagram.</summary>
    MindMap,
    /// <summary>A timeline diagram.</summary>
    Timeline,
    /// <summary>A requirement diagram.</summary>
    Requirement,
    /// <summary>A quadrant chart diagram.</summary>
    Quadrant,
    /// <summary>A Sankey diagram.</summary>
    Sankey,
    /// <summary>An XY chart diagram.</summary>
    XYChart,
    /// <summary>A block diagram.</summary>
    Block,
    /// <summary>A packet diagram.</summary>
    Packet,
    /// <summary>An architecture diagram.</summary>
    Architecture,
    /// <summary>A Kanban diagram.</summary>
    Kanban,
    /// <summary>A radar diagram.</summary>
    Radar,
    /// <summary>A treemap diagram.</summary>
    Treemap,
    /// <summary>A C4 diagram.</summary>
    C4,
    /// <summary>A Venn diagram.</summary>
    Venn,
    /// <summary>An Ishikawa diagram.</summary>
    Ishikawa,
    /// <summary>A Wardley map diagram.</summary>
    Wardley,
    /// <summary>An event modeling diagram.</summary>
    EventModeling,
    /// <summary>A tree view diagram.</summary>
    TreeView,
    /// <summary>A ZenUML diagram.</summary>
    ZenUml
}

/// <summary>
/// Describes a parsed Mermaid document.
/// </summary>
public class MermaidDocument {
    private string _sourceText = string.Empty;
    private string _header = string.Empty;

    /// <summary>Gets or sets the Mermaid diagram family.</summary>
    public MermaidDiagramKind Kind { get; set; }

    /// <summary>Gets or sets the full Mermaid source text.</summary>
    public string SourceText { get => _sourceText; set => _sourceText = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the source span for the diagram header line.</summary>
    public MermaidSourceSpan HeaderSpan { get; set; } = new(1, 1, 0);

    /// <summary>Gets or sets the diagram header line.</summary>
    public string Header { get => _header; set => _header = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional YAML frontmatter text.</summary>
    public string? FrontMatter { get; set; }

    /// <summary>Gets parsed Mermaid directives.</summary>
    public List<MermaidDirective> Directives { get; } = new();

    /// <summary>Gets unclassified body statements retained for recognized families that do not have a semantic parser yet.</summary>
    public List<MermaidRawStatement> RawStatements { get; } = new();
}

/// <summary>
/// Describes a Mermaid directive comment such as init configuration.
/// </summary>
public sealed class MermaidDirective {
    private string _text;

    /// <summary>Initializes a Mermaid directive.</summary>
    public MermaidDirective(string text, MermaidSourceSpan span) {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        Span = span;
    }

    /// <summary>Gets or sets the directive text.</summary>
    public string Text { get => _text; set => _text = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the directive source span.</summary>
    public MermaidSourceSpan Span { get; set; }
}
