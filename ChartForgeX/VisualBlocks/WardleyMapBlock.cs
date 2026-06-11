using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// A deterministic static Wardley map visual.
/// </summary>
public sealed class WardleyMapBlock : VisualBlock<WardleyMapBlock> {
    private readonly List<string> _stages = new();
    private readonly List<WardleyMapNode> _nodes = new();
    private readonly List<WardleyMapLink> _links = new();
    private readonly List<WardleyMapEvolution> _evolutions = new();
    private readonly List<WardleyMapNote> _notes = new();
    private readonly List<WardleyMapAnnotation> _annotations = new();
    private readonly List<WardleyMapMarker> _markers = new();
    private readonly List<WardleyMapPipeline> _pipelines = new();

    /// <summary>Gets evolution stage labels from left to right.</summary>
    public IReadOnlyList<string> Stages => _stages;

    /// <summary>Gets map nodes in render order.</summary>
    public IReadOnlyList<WardleyMapNode> Nodes => _nodes;

    /// <summary>Gets dependency links between map nodes.</summary>
    public IReadOnlyList<WardleyMapLink> Links => _links;

    /// <summary>Gets evolution trends for existing map nodes.</summary>
    public IReadOnlyList<WardleyMapEvolution> Evolutions => _evolutions;

    /// <summary>Gets free-form notes positioned on the map.</summary>
    public IReadOnlyList<WardleyMapNote> Notes => _notes;

    /// <summary>Gets numbered annotations positioned on the map.</summary>
    public IReadOnlyList<WardleyMapAnnotation> Annotations => _annotations;

    /// <summary>Gets accelerator and deaccelerator markers.</summary>
    public IReadOnlyList<WardleyMapMarker> Markers => _markers;

    /// <summary>Gets pipeline groups attached to parent components.</summary>
    public IReadOnlyList<WardleyMapPipeline> Pipelines => _pipelines;

    /// <inheritdoc />
    public override string AccessibleName => Title.Length == 0 ? "Wardley map" : Title;

    /// <summary>Creates an empty Wardley map block.</summary>
    public static WardleyMapBlock Create() => new();

    /// <summary>Sets the left-to-right evolution stages.</summary>
    public WardleyMapBlock SetStages(IEnumerable<string> stages) {
        if (stages == null) throw new ArgumentNullException(nameof(stages));
        _stages.Clear();
        foreach (var stage in stages) {
            var value = stage == null ? string.Empty : stage.Trim();
            if (value.Length > 0) _stages.Add(value);
        }

        return this;
    }

    /// <summary>Adds a map node.</summary>
    public WardleyMapNode AddNode(string id, string label, double visibility, double evolution, WardleyMapNodeKind kind = WardleyMapNodeKind.Component) {
        var node = new WardleyMapNode(id, label, visibility, evolution, kind);
        _nodes.Add(node);
        return node;
    }

    /// <summary>Adds a dependency link between nodes.</summary>
    public WardleyMapLink AddLink(string fromId, string toId, string? label = null, bool dashed = false, WardleyMapFlow flow = WardleyMapFlow.None) {
        var link = new WardleyMapLink(fromId, toId, label ?? string.Empty, dashed, flow);
        _links.Add(link);
        return link;
    }

    /// <summary>Adds an evolution trend for a node.</summary>
    public WardleyMapEvolution AddEvolution(string nodeId, double targetEvolution) {
        var evolution = new WardleyMapEvolution(nodeId, targetEvolution);
        _evolutions.Add(evolution);
        return evolution;
    }

    /// <summary>Adds a note.</summary>
    public WardleyMapNote AddNote(string text, double visibility, double evolution) {
        var note = new WardleyMapNote(text, visibility, evolution);
        _notes.Add(note);
        return note;
    }

    /// <summary>Adds a numbered annotation.</summary>
    public WardleyMapAnnotation AddAnnotation(int number, string text, double visibility, double evolution) {
        var annotation = new WardleyMapAnnotation(number, text, visibility, evolution);
        _annotations.Add(annotation);
        return annotation;
    }

    /// <summary>Adds an accelerator or deaccelerator marker.</summary>
    public WardleyMapMarker AddMarker(string label, double visibility, double evolution, WardleyMapMarkerKind kind) {
        var marker = new WardleyMapMarker(label, visibility, evolution, kind);
        _markers.Add(marker);
        return marker;
    }

    /// <summary>Adds a pipeline group.</summary>
    public WardleyMapPipeline AddPipeline(string parentId) {
        var pipeline = new WardleyMapPipeline(parentId);
        _pipelines.Add(pipeline);
        return pipeline;
    }
}

/// <summary>Identifies a Wardley map node kind.</summary>
public enum WardleyMapNodeKind {
    /// <summary>A normal component on the map.</summary>
    Component,
    /// <summary>An anchor representing a user, need, or other high-level demand signal.</summary>
    Anchor,
    /// <summary>A component that belongs to a pipeline group.</summary>
    PipelineComponent
}

/// <summary>Identifies a Wardley link flow hint.</summary>
public enum WardleyMapFlow {
    /// <summary>No explicit flow hint was declared.</summary>
    None,
    /// <summary>Flow moves from the source toward the target.</summary>
    Forward,
    /// <summary>Flow moves from the target back toward the source.</summary>
    Backward,
    /// <summary>Flow is bidirectional.</summary>
    Bidirectional
}

/// <summary>Identifies a Wardley marker kind.</summary>
public enum WardleyMapMarkerKind {
    /// <summary>A force that accelerates evolution.</summary>
    Accelerator,
    /// <summary>A force that slows evolution.</summary>
    Deaccelerator
}

/// <summary>Describes one positioned Wardley map node.</summary>
public sealed class WardleyMapNode {
    private string _id;
    private string _label;

    /// <summary>Initializes a Wardley map node.</summary>
    public WardleyMapNode(string id, string label, double visibility, double evolution, WardleyMapNodeKind kind) {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Visibility = visibility;
        Evolution = evolution;
        Kind = kind;
    }

    /// <summary>Gets or sets the stable node id.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the vertical visibility coordinate from zero to one.</summary>
    public double Visibility { get; set; }

    /// <summary>Gets or sets the horizontal evolution coordinate from zero to one.</summary>
    public double Evolution { get; set; }

    /// <summary>Gets or sets the node kind.</summary>
    public WardleyMapNodeKind Kind { get; set; }

    /// <summary>Gets or sets whether the node has an inertia marker.</summary>
    public bool Inertia { get; set; }

    /// <summary>Gets or sets an optional sourcing strategy such as build, buy, outsource, or market.</summary>
    public string? Strategy { get; set; }

    /// <summary>Gets or sets an optional X label offset in rendered pixels.</summary>
    public double? LabelOffsetX { get; set; }

    /// <summary>Gets or sets an optional Y label offset in rendered pixels.</summary>
    public double? LabelOffsetY { get; set; }
}

/// <summary>Describes a dependency link between Wardley map nodes.</summary>
public sealed class WardleyMapLink {
    /// <summary>Initializes a Wardley map dependency link.</summary>
    public WardleyMapLink(string fromId, string toId, string label, bool dashed, WardleyMapFlow flow) {
        FromId = fromId ?? throw new ArgumentNullException(nameof(fromId));
        ToId = toId ?? throw new ArgumentNullException(nameof(toId));
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Dashed = dashed;
        Flow = flow;
    }

    /// <summary>Gets the source node id.</summary>
    public string FromId { get; }

    /// <summary>Gets the target node id.</summary>
    public string ToId { get; }

    /// <summary>Gets the optional link label.</summary>
    public string Label { get; }

    /// <summary>Gets whether the link should render as a dashed dependency.</summary>
    public bool Dashed { get; }

    /// <summary>Gets the optional flow hint.</summary>
    public WardleyMapFlow Flow { get; }
}

/// <summary>Describes a Wardley node evolution trend.</summary>
public sealed class WardleyMapEvolution {
    /// <summary>Initializes a Wardley evolution trend.</summary>
    public WardleyMapEvolution(string nodeId, double targetEvolution) {
        NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        TargetEvolution = targetEvolution;
    }

    /// <summary>Gets the source node id.</summary>
    public string NodeId { get; }

    /// <summary>Gets the target evolution coordinate from zero to one.</summary>
    public double TargetEvolution { get; }
}

/// <summary>Describes a positioned Wardley note.</summary>
public sealed class WardleyMapNote {
    /// <summary>Initializes a Wardley note.</summary>
    public WardleyMapNote(string text, double visibility, double evolution) {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Visibility = visibility;
        Evolution = evolution;
    }

    /// <summary>Gets the note text.</summary>
    public string Text { get; }

    /// <summary>Gets the vertical visibility coordinate from zero to one.</summary>
    public double Visibility { get; }

    /// <summary>Gets the horizontal evolution coordinate from zero to one.</summary>
    public double Evolution { get; }
}

/// <summary>Describes a numbered Wardley annotation.</summary>
public sealed class WardleyMapAnnotation {
    /// <summary>Initializes a numbered Wardley annotation.</summary>
    public WardleyMapAnnotation(int number, string text, double visibility, double evolution) {
        Number = number;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Visibility = visibility;
        Evolution = evolution;
    }

    /// <summary>Gets the annotation number.</summary>
    public int Number { get; }

    /// <summary>Gets the annotation text.</summary>
    public string Text { get; }

    /// <summary>Gets the vertical visibility coordinate from zero to one.</summary>
    public double Visibility { get; }

    /// <summary>Gets the horizontal evolution coordinate from zero to one.</summary>
    public double Evolution { get; }
}

/// <summary>Describes a positioned Wardley accelerator or deaccelerator.</summary>
public sealed class WardleyMapMarker {
    /// <summary>Initializes a Wardley acceleration marker.</summary>
    public WardleyMapMarker(string label, double visibility, double evolution, WardleyMapMarkerKind kind) {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Visibility = visibility;
        Evolution = evolution;
        Kind = kind;
    }

    /// <summary>Gets the marker label.</summary>
    public string Label { get; }

    /// <summary>Gets the vertical visibility coordinate from zero to one.</summary>
    public double Visibility { get; }

    /// <summary>Gets the horizontal evolution coordinate from zero to one.</summary>
    public double Evolution { get; }

    /// <summary>Gets the marker kind.</summary>
    public WardleyMapMarkerKind Kind { get; }
}

/// <summary>Describes a Wardley pipeline attached to a parent component.</summary>
public sealed class WardleyMapPipeline {
    private readonly List<WardleyMapPipelineComponent> _components = new();

    /// <summary>Initializes a pipeline attached to a parent node.</summary>
    public WardleyMapPipeline(string parentId) {
        ParentId = parentId ?? throw new ArgumentNullException(nameof(parentId));
    }

    /// <summary>Gets the parent node id.</summary>
    public string ParentId { get; }

    /// <summary>Gets child components in pipeline order.</summary>
    public IReadOnlyList<WardleyMapPipelineComponent> Components => _components;

    /// <summary>Adds a pipeline child component.</summary>
    public WardleyMapPipelineComponent AddComponent(string label, double evolution) {
        var component = new WardleyMapPipelineComponent(label, evolution);
        _components.Add(component);
        return component;
    }
}

/// <summary>Describes a child component inside a Wardley pipeline.</summary>
public sealed class WardleyMapPipelineComponent {
    /// <summary>Initializes a Wardley pipeline child component.</summary>
    public WardleyMapPipelineComponent(string label, double evolution) {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Evolution = evolution;
    }

    /// <summary>Gets the child component label.</summary>
    public string Label { get; }

    /// <summary>Gets the child component evolution coordinate from zero to one.</summary>
    public double Evolution { get; }
}
