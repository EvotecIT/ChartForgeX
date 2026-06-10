using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Defines Mermaid flowchart layout direction tokens.
/// </summary>
public enum MermaidFlowchartDirection {
    /// <summary>No direction was declared.</summary>
    None,
    /// <summary>Top-to-bottom direction.</summary>
    TopToBottom,
    /// <summary>Top-down direction alias.</summary>
    TopDown,
    /// <summary>Bottom-to-top direction.</summary>
    BottomToTop,
    /// <summary>Left-to-right direction.</summary>
    LeftToRight,
    /// <summary>Right-to-left direction.</summary>
    RightToLeft
}

/// <summary>
/// Describes a Mermaid flowchart or graph document.
/// </summary>
public sealed class MermaidFlowchartDocument : MermaidDocument {
    /// <summary>Gets or sets the declared flowchart direction.</summary>
    public MermaidFlowchartDirection Direction { get; set; }

    /// <summary>Gets raw flowchart statements retained with source spans.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets parsed flowchart node definitions and references.</summary>
    public List<MermaidFlowchartNode> Nodes { get; } = new();

    /// <summary>Gets parsed flowchart edges.</summary>
    public List<MermaidFlowchartEdge> Edges { get; } = new();

    /// <summary>Gets parsed flowchart subgraphs.</summary>
    public List<MermaidFlowchartSubgraph> Subgraphs { get; } = new();

    /// <summary>Gets parsed flowchart class definitions.</summary>
    public List<MermaidFlowchartClassDefinition> ClassDefinitions { get; } = new();

    /// <summary>Gets parsed flowchart link style declarations.</summary>
    public List<MermaidFlowchartLinkStyle> LinkStyles { get; } = new();
}

/// <summary>
/// Defines Mermaid flowchart node shape families.
/// </summary>
public enum MermaidFlowchartNodeShape {
    /// <summary>No explicit shape was declared.</summary>
    Default,
    /// <summary>Rectangle shape, such as <c>A[Text]</c>.</summary>
    Rectangle,
    /// <summary>Rounded rectangle shape, such as <c>A(Text)</c>.</summary>
    Rounded,
    /// <summary>Stadium shape, such as <c>A([Text])</c>.</summary>
    Stadium,
    /// <summary>Subroutine shape, such as <c>A[[Text]]</c>.</summary>
    Subroutine,
    /// <summary>Database/cylinder shape, such as <c>A[(Text)]</c>.</summary>
    Cylinder,
    /// <summary>Circle shape, such as <c>A((Text))</c>.</summary>
    Circle,
    /// <summary>Double circle shape, such as <c>A(((Text)))</c>.</summary>
    DoubleCircle,
    /// <summary>Asymmetric shape, such as <c>A&gt;Text]</c>.</summary>
    Asymmetric,
    /// <summary>Rhombus shape, such as <c>A{Text}</c>.</summary>
    Rhombus,
    /// <summary>Hexagon shape, such as <c>A{{Text}}</c>.</summary>
    Hexagon,
    /// <summary>Parallelogram shape, such as <c>A[/Text/]</c>.</summary>
    Parallelogram,
    /// <summary>Alternate parallelogram shape, such as <c>A[\Text\]</c>.</summary>
    ParallelogramAlt,
    /// <summary>Trapezoid shape, such as <c>A[/Text\]</c>.</summary>
    Trapezoid,
    /// <summary>Alternate trapezoid shape, such as <c>A[\Text/]</c>.</summary>
    TrapezoidAlt
}

/// <summary>
/// Describes a Mermaid flowchart node reference or definition.
/// </summary>
public sealed class MermaidFlowchartNode : MermaidAstNode {
    private string _id;

    /// <summary>Initializes a parsed flowchart node.</summary>
    public MermaidFlowchartNode(string id, MermaidSourceSpan span) : base(span) {
        _id = string.IsNullOrWhiteSpace(id) ? throw new System.ArgumentException("Node id is required.", nameof(id)) : id;
    }

    /// <summary>Gets or sets the Mermaid node identifier.</summary>
    public string Id { get => _id; set => _id = string.IsNullOrWhiteSpace(value) ? throw new System.ArgumentException("Node id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the optional node label text.</summary>
    public string? Text { get; set; }

    /// <summary>Gets or sets the explicit Mermaid node shape.</summary>
    public MermaidFlowchartNodeShape Shape { get; set; }

    /// <summary>Gets class names assigned through Mermaid class syntax.</summary>
    public List<string> Classes { get; } = new();

    /// <summary>Gets style declarations assigned directly or through class definitions.</summary>
    public Dictionary<string, string> Styles { get; } = new();

    /// <summary>Gets or sets an optional Mermaid click link target.</summary>
    public string? Href { get; set; }

    /// <summary>Gets or sets an optional Mermaid click tooltip.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Gets or sets an optional parent Mermaid subgraph id.</summary>
    public string? SubgraphId { get; set; }
}

/// <summary>
/// Describes a Mermaid flowchart edge.
/// </summary>
public sealed class MermaidFlowchartEdge : MermaidAstNode {
    private string _sourceId;
    private string _targetId;
    private string _operator;

    /// <summary>Initializes a parsed flowchart edge.</summary>
    public MermaidFlowchartEdge(string sourceId, string targetId, string edgeOperator, MermaidSourceSpan span) : base(span) {
        _sourceId = string.IsNullOrWhiteSpace(sourceId) ? throw new System.ArgumentException("Source node id is required.", nameof(sourceId)) : sourceId;
        _targetId = string.IsNullOrWhiteSpace(targetId) ? throw new System.ArgumentException("Target node id is required.", nameof(targetId)) : targetId;
        _operator = string.IsNullOrWhiteSpace(edgeOperator) ? throw new System.ArgumentException("Edge operator is required.", nameof(edgeOperator)) : edgeOperator;
    }

    /// <summary>Gets or sets the source node identifier.</summary>
    public string SourceId { get => _sourceId; set => _sourceId = string.IsNullOrWhiteSpace(value) ? throw new System.ArgumentException("Source node id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the target node identifier.</summary>
    public string TargetId { get => _targetId; set => _targetId = string.IsNullOrWhiteSpace(value) ? throw new System.ArgumentException("Target node id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the raw Mermaid edge operator segment.</summary>
    public string Operator { get => _operator; set => _operator = string.IsNullOrWhiteSpace(value) ? throw new System.ArgumentException("Edge operator is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the optional edge label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets style declarations assigned through linkStyle syntax.</summary>
    public Dictionary<string, string> Styles { get; } = new();
}

/// <summary>
/// Describes a Mermaid flowchart subgraph.
/// </summary>
public sealed class MermaidFlowchartSubgraph : MermaidAstNode {
    private string _id;
    private string _title;

    /// <summary>Initializes a parsed flowchart subgraph.</summary>
    public MermaidFlowchartSubgraph(string id, string title, MermaidSourceSpan span) : base(span) {
        _id = string.IsNullOrWhiteSpace(id) ? throw new System.ArgumentException("Subgraph id is required.", nameof(id)) : id;
        _title = title ?? throw new System.ArgumentNullException(nameof(title));
    }

    /// <summary>Gets or sets the Mermaid subgraph identifier.</summary>
    public string Id { get => _id; set => _id = string.IsNullOrWhiteSpace(value) ? throw new System.ArgumentException("Subgraph id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the Mermaid subgraph title.</summary>
    public string Title { get => _title; set => _title = value ?? throw new System.ArgumentNullException(nameof(value)); }

    /// <summary>Gets node ids declared inside the subgraph.</summary>
    public List<string> NodeIds { get; } = new();
}

/// <summary>
/// Describes a Mermaid flowchart class definition.
/// </summary>
public sealed class MermaidFlowchartClassDefinition : MermaidAstNode {
    private string _name;

    /// <summary>Initializes a parsed class definition.</summary>
    public MermaidFlowchartClassDefinition(string name, MermaidSourceSpan span) : base(span) {
        _name = string.IsNullOrWhiteSpace(name) ? throw new System.ArgumentException("Class name is required.", nameof(name)) : name;
    }

    /// <summary>Gets or sets the Mermaid class name.</summary>
    public string Name { get => _name; set => _name = string.IsNullOrWhiteSpace(value) ? throw new System.ArgumentException("Class name is required.", nameof(value)) : value; }

    /// <summary>Gets style declarations for the class.</summary>
    public Dictionary<string, string> Styles { get; } = new();
}

/// <summary>
/// Describes a Mermaid flowchart linkStyle declaration.
/// </summary>
public sealed class MermaidFlowchartLinkStyle : MermaidAstNode {
    private string _selector;

    /// <summary>Initializes a parsed link style declaration.</summary>
    public MermaidFlowchartLinkStyle(string selector, MermaidSourceSpan span) : base(span) {
        _selector = string.IsNullOrWhiteSpace(selector) ? throw new System.ArgumentException("Link style selector is required.", nameof(selector)) : selector;
    }

    /// <summary>Gets or sets the raw Mermaid linkStyle selector, such as an edge index or <c>default</c>.</summary>
    public string Selector { get => _selector; set => _selector = string.IsNullOrWhiteSpace(value) ? throw new System.ArgumentException("Link style selector is required.", nameof(value)) : value; }

    /// <summary>Gets style declarations for selected links.</summary>
    public Dictionary<string, string> Styles { get; } = new();
}
