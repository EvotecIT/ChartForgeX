using System;
using System.Collections.Generic;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid Wardley map.
/// </summary>
public sealed class MermaidWardleyDocument : MermaidDocument {
    /// <summary>Gets Wardley statements retained in source order.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets or sets the optional Mermaid title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the optional Mermaid canvas width.</summary>
    public int? CanvasWidth { get; set; }

    /// <summary>Gets or sets the optional Mermaid canvas height.</summary>
    public int? CanvasHeight { get; set; }

    /// <summary>Gets evolution stage labels.</summary>
    public List<string> Stages { get; } = new();

    /// <summary>Gets parsed map nodes.</summary>
    public List<MermaidWardleyNode> Nodes { get; } = new();

    /// <summary>Gets parsed dependency links.</summary>
    public List<MermaidWardleyLink> Links { get; } = new();

    /// <summary>Gets parsed evolution trends.</summary>
    public List<MermaidWardleyEvolution> Evolutions { get; } = new();

    /// <summary>Gets parsed notes.</summary>
    public List<MermaidWardleyNote> Notes { get; } = new();

    /// <summary>Gets parsed numbered annotations.</summary>
    public List<MermaidWardleyAnnotation> Annotations { get; } = new();

    /// <summary>Gets parsed accelerators and deaccelerators.</summary>
    public List<MermaidWardleyMarker> Markers { get; } = new();

    /// <summary>Gets parsed pipelines.</summary>
    public List<MermaidWardleyPipeline> Pipelines { get; } = new();
}

/// <summary>Describes a parsed Wardley node.</summary>
public sealed class MermaidWardleyNode {
    /// <summary>Initializes a parsed Wardley map node.</summary>
    public MermaidWardleyNode(string id, double visibility, double evolution, WardleyMapNodeKind kind, MermaidSourceSpan span) {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Visibility = visibility;
        Evolution = evolution;
        Kind = kind;
        Span = span;
    }

    /// <summary>Gets the Mermaid node name.</summary>
    public string Id { get; }

    /// <summary>Gets the normalized visibility coordinate from zero to one.</summary>
    public double Visibility { get; }

    /// <summary>Gets the normalized evolution coordinate from zero to one.</summary>
    public double Evolution { get; }

    /// <summary>Gets the mapped node kind.</summary>
    public WardleyMapNodeKind Kind { get; }

    /// <summary>Gets or sets whether the node declares inertia.</summary>
    public bool Inertia { get; set; }

    /// <summary>Gets or sets the optional Mermaid sourcing strategy.</summary>
    public string? Strategy { get; set; }

    /// <summary>Gets or sets the optional label X offset.</summary>
    public double? LabelOffsetX { get; set; }

    /// <summary>Gets or sets the optional label Y offset.</summary>
    public double? LabelOffsetY { get; set; }

    /// <summary>Gets the source span for the node statement.</summary>
    public MermaidSourceSpan Span { get; }
}

/// <summary>Describes a parsed Wardley dependency link.</summary>
public sealed class MermaidWardleyLink {
    /// <summary>Initializes a parsed Wardley dependency link.</summary>
    public MermaidWardleyLink(string from, string to, string label, bool dashed, WardleyMapFlow flow, MermaidSourceSpan span) {
        From = from ?? throw new ArgumentNullException(nameof(from));
        To = to ?? throw new ArgumentNullException(nameof(to));
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Dashed = dashed;
        Flow = flow;
        Span = span;
    }

    /// <summary>Gets the source node name.</summary>
    public string From { get; }

    /// <summary>Gets the target node name.</summary>
    public string To { get; }

    /// <summary>Gets the optional Mermaid link or flow label.</summary>
    public string Label { get; }

    /// <summary>Gets whether the dependency was declared as dashed.</summary>
    public bool Dashed { get; }

    /// <summary>Gets the optional Mermaid flow hint.</summary>
    public WardleyMapFlow Flow { get; }

    /// <summary>Gets the source span for the link statement.</summary>
    public MermaidSourceSpan Span { get; }
}

/// <summary>Describes a parsed Wardley evolution trend.</summary>
public sealed class MermaidWardleyEvolution {
    /// <summary>Initializes a parsed Wardley evolution trend.</summary>
    public MermaidWardleyEvolution(string nodeId, double targetEvolution, MermaidSourceSpan span) {
        NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        TargetEvolution = targetEvolution;
        Span = span;
    }

    /// <summary>Gets the source node name.</summary>
    public string NodeId { get; }

    /// <summary>Gets the normalized target evolution coordinate from zero to one.</summary>
    public double TargetEvolution { get; }

    /// <summary>Gets the source span for the evolve statement.</summary>
    public MermaidSourceSpan Span { get; }
}

/// <summary>Describes a parsed Wardley note.</summary>
public sealed class MermaidWardleyNote {
    /// <summary>Initializes a parsed Wardley note.</summary>
    public MermaidWardleyNote(string text, double visibility, double evolution, MermaidSourceSpan span) {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Visibility = visibility;
        Evolution = evolution;
        Span = span;
    }

    /// <summary>Gets the note text.</summary>
    public string Text { get; }

    /// <summary>Gets the normalized visibility coordinate from zero to one.</summary>
    public double Visibility { get; }

    /// <summary>Gets the normalized evolution coordinate from zero to one.</summary>
    public double Evolution { get; }

    /// <summary>Gets the source span for the note statement.</summary>
    public MermaidSourceSpan Span { get; }
}

/// <summary>Describes a parsed Wardley annotation.</summary>
public sealed class MermaidWardleyAnnotation {
    /// <summary>Initializes a parsed Wardley annotation.</summary>
    public MermaidWardleyAnnotation(int number, string text, double visibility, double evolution, MermaidSourceSpan span) {
        Number = number;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Visibility = visibility;
        Evolution = evolution;
        Span = span;
    }

    /// <summary>Gets the annotation number.</summary>
    public int Number { get; }

    /// <summary>Gets the annotation text.</summary>
    public string Text { get; }

    /// <summary>Gets the normalized visibility coordinate from zero to one.</summary>
    public double Visibility { get; }

    /// <summary>Gets the normalized evolution coordinate from zero to one.</summary>
    public double Evolution { get; }

    /// <summary>Gets the source span for the annotation statement.</summary>
    public MermaidSourceSpan Span { get; }
}

/// <summary>Describes a parsed Wardley accelerator or deaccelerator.</summary>
public sealed class MermaidWardleyMarker {
    /// <summary>Initializes a parsed Wardley acceleration marker.</summary>
    public MermaidWardleyMarker(string label, double visibility, double evolution, WardleyMapMarkerKind kind, MermaidSourceSpan span) {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Visibility = visibility;
        Evolution = evolution;
        Kind = kind;
        Span = span;
    }

    /// <summary>Gets the marker label.</summary>
    public string Label { get; }

    /// <summary>Gets the normalized visibility coordinate from zero to one.</summary>
    public double Visibility { get; }

    /// <summary>Gets the normalized evolution coordinate from zero to one.</summary>
    public double Evolution { get; }

    /// <summary>Gets the marker kind.</summary>
    public WardleyMapMarkerKind Kind { get; }

    /// <summary>Gets the source span for the marker statement.</summary>
    public MermaidSourceSpan Span { get; }
}

/// <summary>Describes a parsed Wardley pipeline.</summary>
public sealed class MermaidWardleyPipeline {
    private readonly List<MermaidWardleyPipelineComponent> _components = new();

    /// <summary>Initializes a parsed Wardley pipeline.</summary>
    public MermaidWardleyPipeline(string parentId, MermaidSourceSpan span) {
        ParentId = parentId ?? throw new ArgumentNullException(nameof(parentId));
        Span = span;
    }

    /// <summary>Gets the parent node name.</summary>
    public string ParentId { get; }

    /// <summary>Gets the source span for the pipeline statement.</summary>
    public MermaidSourceSpan Span { get; }

    /// <summary>Gets pipeline child components.</summary>
    public IReadOnlyList<MermaidWardleyPipelineComponent> Components => _components;

    /// <summary>Adds a parsed pipeline child component.</summary>
    public void AddComponent(MermaidWardleyPipelineComponent component) {
        if (component == null) throw new ArgumentNullException(nameof(component));
        _components.Add(component);
    }
}

/// <summary>Describes a parsed Wardley pipeline component.</summary>
public sealed class MermaidWardleyPipelineComponent {
    /// <summary>Initializes a parsed Wardley pipeline child component.</summary>
    public MermaidWardleyPipelineComponent(string label, double evolution, MermaidSourceSpan span) {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Evolution = evolution;
        Span = span;
    }

    /// <summary>Gets the child component label.</summary>
    public string Label { get; }

    /// <summary>Gets the normalized evolution coordinate from zero to one.</summary>
    public double Evolution { get; }

    /// <summary>Gets the source span for the child component statement.</summary>
    public MermaidSourceSpan Span { get; }
}
