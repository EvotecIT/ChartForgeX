using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>A process graph whose top-level subgraphs represent responsibility lanes.</summary>
public sealed class MermaidSwimlaneDocument : MermaidFlowchartDocument { }

/// <summary>A UML use case graph with actors, use cases, and system boundaries.</summary>
public sealed class MermaidUseCaseDocument : MermaidFlowchartDocument {
    /// <summary>Gets visible attached notes, separate from semantic actors, use cases and associations.</summary>
    public List<MermaidDiagramNote> Notes { get; } = new();
    internal List<MermaidRawStatement> SelectorStatements { get; } = new();
}

/// <summary>A source-preserving map of the five Cynefin complexity domains.</summary>
public sealed class MermaidCynefinDocument : MermaidDocument {
    /// <summary>Gets or sets the authored title.</summary>
    public string? Title { get; set; }
    /// <summary>Gets item labels by first domain declaration order. A repeated domain block replaces that domain's items; the original blocks remain in SourceText and RawStatements.</summary>
    public Dictionary<string, List<string>> Domains { get; } = new();
    /// <summary>Gets domain transitions in source order.</summary>
    public List<MermaidFlowchartEdge> Transitions { get; } = new();
}
