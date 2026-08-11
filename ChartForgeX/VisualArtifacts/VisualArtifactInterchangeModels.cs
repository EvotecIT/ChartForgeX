using System;
using System.Collections.Generic;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

/// <summary>
/// Represents the versioned, renderer-independent semantic form of a visual artifact.
/// </summary>
/// <remarks>
/// The interchange envelope is intended for package, process, and PowerShell assembly-load-context
/// boundaries. It carries diagram semantics while rendered SVG remains an independent fallback.
/// </remarks>
public sealed class VisualArtifactInterchangeEnvelope {
    /// <summary>The schema identifier emitted by this ChartForgeX version.</summary>
    public const string SchemaId = "chartforgex.visual-artifact";

    /// <summary>The schema version emitted by this ChartForgeX version.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Maximum accepted UTF-8 payload size for one interchange envelope.</summary>
    public const int MaximumJsonUtf8Bytes = 8 * 1024 * 1024;

    /// <summary>Maximum accepted JSON text length for one interchange envelope.</summary>
    public const int MaximumJsonCharacters = 8 * 1024 * 1024;

    /// <summary>Maximum accepted materialized JSON values for one interchange envelope.</summary>
    public const int MaximumJsonValues = 600000;

    private string _id = string.Empty;
    private string _title = string.Empty;
    private string _subtitle = string.Empty;

    /// <summary>Gets the schema identifier.</summary>
    public string Schema => SchemaId;

    /// <summary>Gets the schema version.</summary>
    public int Version => CurrentVersion;

    /// <summary>Gets or sets the visual artifact kind.</summary>
    public VisualArtifactKind Kind { get; set; }

    /// <summary>Gets or sets the structured semantic family, independently from the authoring kind.</summary>
    public VisualArtifactInterchangeFamily Family { get; set; }

    /// <summary>Gets or sets the source language used to author the artifact.</summary>
    public VisualArtifactSourceLanguage SourceLanguage { get; set; }

    /// <summary>Gets or sets the stable artifact identifier.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the artifact title.</summary>
    public string Title { get => _title; set => _title = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string Subtitle { get => _subtitle; set => _subtitle = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets topology-wide semantics for a topology envelope.</summary>
    public VisualArtifactInterchangeTopologyArtifact? Topology { get; set; }

    /// <summary>Gets or sets flow-wide semantics for a flow envelope.</summary>
    public VisualArtifactInterchangeFlowArtifact? Flow { get; set; }

    /// <summary>Gets or sets sequence-wide semantics for a sequence envelope.</summary>
    public VisualArtifactInterchangeSequenceArtifact? Sequence { get; set; }

    /// <summary>Gets or sets the natural width in pixels when known.</summary>
    public double? Width { get; set; }

    /// <summary>Gets or sets the natural height in pixels when known.</summary>
    public double? Height { get; set; }

    /// <summary>Gets or sets the accessible name.</summary>
    public string? AccessibleName { get; set; }

    /// <summary>Gets or sets the accessible description.</summary>
    public string? AccessibleDescription { get; set; }

    /// <summary>Gets or sets the BCP 47 language tag.</summary>
    public string? Language { get; set; }

    /// <summary>Gets or sets whether the artifact is decorative.</summary>
    public bool IsDecorative { get; set; }

    /// <summary>Gets or sets typed artifact presentation semantics.</summary>
    public VisualArtifactInterchangePresentation? Presentation { get; set; }

    /// <summary>Gets opaque caller-defined extension data.</summary>
    /// <remarks>Stable ChartForgeX semantics are represented by typed properties, never reserved extension keys.</remarks>
    public Dictionary<string, string> Extensions { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets logical groups, lanes, or containers.</summary>
    public List<VisualArtifactInterchangeGroup> Groups { get; } = new();

    /// <summary>Gets semantic nodes or participants.</summary>
    public List<VisualArtifactInterchangeNode> Nodes { get; } = new();

    /// <summary>Gets semantic edges or messages.</summary>
    public List<VisualArtifactInterchangeEdge> Edges { get; } = new();

    /// <summary>Gets reusable ordered scenarios or guided paths.</summary>
    public List<VisualArtifactInterchangeScenario> Scenarios { get; } = new();

    /// <summary>Gets notes, blocks, and other diagram annotations.</summary>
    public List<VisualArtifactInterchangeAnnotation> Annotations { get; } = new();

    /// <summary>Serializes the envelope to deterministic UTF-8 JSON text.</summary>
    public string ToJson() => VisualArtifactInterchangeJson.Serialize(this);

    /// <summary>Serializes the envelope to deterministic UTF-8 JSON bytes.</summary>
    public byte[] ToUtf8Json() {
        string json = ToJson();
        var encoding = new System.Text.UTF8Encoding(false, true);
        int byteCount = encoding.GetByteCount(json);
        if (byteCount > MaximumJsonUtf8Bytes) {
            throw new ArgumentOutOfRangeException(nameof(json), byteCount, "Interchange UTF-8 JSON must not exceed " + MaximumJsonUtf8Bytes + " bytes.");
        }
        return encoding.GetBytes(json);
    }

    /// <summary>Parses and validates an interchange envelope from JSON text.</summary>
    public static VisualArtifactInterchangeEnvelope FromJson(string json) => VisualArtifactInterchangeJson.Deserialize(json);

    /// <summary>Validates this typed snapshot without serializing it.</summary>
    public void Validate() => VisualArtifactInterchangeValidation.Validate(this);

    /// <summary>Parses and validates an interchange envelope from UTF-8 JSON bytes.</summary>
    public static VisualArtifactInterchangeEnvelope FromUtf8Json(byte[] json) {
        if (json == null) throw new ArgumentNullException(nameof(json));
        if (json.Length > MaximumJsonUtf8Bytes) {
            throw new ArgumentOutOfRangeException(nameof(json), json.Length, "Interchange UTF-8 JSON must not exceed " + MaximumJsonUtf8Bytes + " bytes.");
        }
        return FromJson(new System.Text.UTF8Encoding(false, true).GetString(json));
    }
}

/// <summary>Represents one logical group, lane, or container.</summary>
public sealed class VisualArtifactInterchangeGroup {
    /// <summary>Gets or sets the stable group id.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the well-known group role.</summary>
    public VisualArtifactInterchangeGroupRole Role { get; set; }
    /// <summary>Gets or sets the semantic group kind token.</summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Gets or sets the visible group label.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional subtitle.</summary>
    public string? Subtitle { get; set; }
    /// <summary>Gets or sets the semantic status token.</summary>
    public string? Status { get; set; }
    /// <summary>Gets or sets the optional accent color.</summary>
    public string? Color { get; set; }
    /// <summary>Gets or sets an optional navigation target.</summary>
    public string? Href { get; set; }
    /// <summary>Gets or sets optional tooltip text.</summary>
    public string? Tooltip { get; set; }
    /// <summary>Gets or sets the prepared x-coordinate.</summary>
    public double? X { get; set; }
    /// <summary>Gets or sets the prepared y-coordinate.</summary>
    public double? Y { get; set; }
    /// <summary>Gets or sets the prepared width.</summary>
    public double? Width { get; set; }
    /// <summary>Gets or sets the prepared height.</summary>
    public double? Height { get; set; }
    /// <summary>Gets or sets topology-specific group semantics.</summary>
    public VisualArtifactInterchangeTopologyGroup? Topology { get; set; }
    /// <summary>Gets opaque caller-defined extension data.</summary>
    public Dictionary<string, string> Extensions { get; } = new(StringComparer.Ordinal);
}

/// <summary>Represents one semantic diagram node or participant.</summary>
public sealed class VisualArtifactInterchangeNode {
    /// <summary>Gets or sets the stable node id.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the well-known node role.</summary>
    public VisualArtifactInterchangeNodeRole Role { get; set; }
    /// <summary>Gets or sets the semantic node kind token.</summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Gets or sets the visible node label.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional subtitle.</summary>
    public string? Subtitle { get; set; }
    /// <summary>Gets or sets the parent group id.</summary>
    public string? GroupId { get; set; }
    /// <summary>Gets or sets the semantic status token.</summary>
    public string? Status { get; set; }
    /// <summary>Gets or sets the reusable icon id.</summary>
    public string? IconId { get; set; }
    /// <summary>Gets or sets the short visual symbol.</summary>
    public string? Symbol { get; set; }
    /// <summary>Gets or sets optional badge text.</summary>
    public string? Badge { get; set; }
    /// <summary>Gets or sets the optional accent color.</summary>
    public string? Color { get; set; }
    /// <summary>Gets or sets the optional background color.</summary>
    public string? BackgroundColor { get; set; }
    /// <summary>Gets or sets an optional navigation target.</summary>
    public string? Href { get; set; }
    /// <summary>Gets or sets optional tooltip text.</summary>
    public string? Tooltip { get; set; }
    /// <summary>Gets or sets the prepared x-coordinate.</summary>
    public double? X { get; set; }
    /// <summary>Gets or sets the prepared y-coordinate.</summary>
    public double? Y { get; set; }
    /// <summary>Gets or sets the prepared width.</summary>
    public double? Width { get; set; }
    /// <summary>Gets or sets the prepared height.</summary>
    public double? Height { get; set; }
    /// <summary>Gets or sets topology-specific node semantics.</summary>
    public VisualArtifactInterchangeTopologyNode? Topology { get; set; }
    /// <summary>Gets or sets flow-specific node semantics.</summary>
    public VisualArtifactInterchangeFlowNode? Flow { get; set; }
    /// <summary>Gets or sets sequence-specific node semantics.</summary>
    public VisualArtifactInterchangeSequenceNode? Sequence { get; set; }
    /// <summary>Gets opaque caller-defined extension data.</summary>
    public Dictionary<string, string> Extensions { get; } = new(StringComparer.Ordinal);
    /// <summary>Gets named attachment ports.</summary>
    public List<VisualArtifactInterchangePort> Ports { get; } = new();
    /// <summary>Gets typed label/value detail rows.</summary>
    public List<VisualArtifactInterchangeDetail> Details { get; } = new();
    /// <summary>Gets typed node metrics without encoding them as reserved extension keys.</summary>
    public List<VisualArtifactInterchangeMetric> Metrics { get; } = new();
}

/// <summary>Represents one named node attachment port.</summary>
public sealed class VisualArtifactInterchangePort {
    /// <summary>Gets or sets the node-local stable port id.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the exact attachment side.</summary>
    public TopologyEdgePort Side { get; set; }
    /// <summary>Gets or sets the normalized position along the side.</summary>
    public double Offset { get; set; } = 0.5;
    /// <summary>Gets or sets the optional visible label.</summary>
    public string? Label { get; set; }
    /// <summary>Gets opaque caller-defined extension data.</summary>
    public Dictionary<string, string> Extensions { get; } = new(StringComparer.Ordinal);
}

/// <summary>Represents one typed node detail row.</summary>
public sealed class VisualArtifactInterchangeDetail {
    /// <summary>Gets or sets the row label.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Gets or sets the row value.</summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional icon id.</summary>
    public string? IconId { get; set; }
    /// <summary>Gets or sets an optional semantic status token.</summary>
    public string? Status { get; set; }
    /// <summary>Gets or sets an optional accent color.</summary>
    public string? Color { get; set; }
    /// <summary>Gets opaque caller-defined extension data.</summary>
    public Dictionary<string, string> Extensions { get; } = new(StringComparer.Ordinal);
}

/// <summary>Represents one semantic edge, connector, or sequence message.</summary>
public sealed class VisualArtifactInterchangeEdge {
    /// <summary>Gets or sets the stable edge id.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the well-known edge role.</summary>
    public VisualArtifactInterchangeEdgeRole Role { get; set; }
    /// <summary>Gets or sets the semantic edge kind token.</summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Gets or sets the source node id.</summary>
    public string SourceId { get; set; } = string.Empty;
    /// <summary>Gets or sets the target node id.</summary>
    public string TargetId { get; set; } = string.Empty;
    /// <summary>Gets or sets the primary label.</summary>
    public string? Label { get; set; }
    /// <summary>Gets or sets the secondary label.</summary>
    public string? SecondaryLabel { get; set; }
    /// <summary>Gets or sets the tertiary label.</summary>
    public string? TertiaryLabel { get; set; }
    /// <summary>Gets or sets the source endpoint label.</summary>
    public string? SourceLabel { get; set; }
    /// <summary>Gets or sets the target endpoint label.</summary>
    public string? TargetLabel { get; set; }
    /// <summary>Gets or sets the semantic status token.</summary>
    public string? Status { get; set; }
    /// <summary>Gets or sets the named source port id.</summary>
    public string? SourcePortId { get; set; }
    /// <summary>Gets or sets the named target port id.</summary>
    public string? TargetPortId { get; set; }
    /// <summary>Gets or sets an optional accent color.</summary>
    public string? Color { get; set; }
    /// <summary>Gets or sets an optional navigation target.</summary>
    public string? Href { get; set; }
    /// <summary>Gets or sets optional tooltip text.</summary>
    public string? Tooltip { get; set; }
    /// <summary>Gets or sets the semantic order of the edge.</summary>
    public int Order { get; set; }
    /// <summary>Gets or sets topology-specific edge semantics.</summary>
    public VisualArtifactInterchangeTopologyEdge? Topology { get; set; }
    /// <summary>Gets or sets flow-specific edge semantics.</summary>
    public VisualArtifactInterchangeFlowEdge? Flow { get; set; }
    /// <summary>Gets or sets sequence-specific edge semantics.</summary>
    public VisualArtifactInterchangeSequenceEdge? Sequence { get; set; }
    /// <summary>Gets opaque caller-defined extension data.</summary>
    public Dictionary<string, string> Extensions { get; } = new(StringComparer.Ordinal);
    /// <summary>Gets typed edge metrics without encoding them as reserved extension keys.</summary>
    public List<VisualArtifactInterchangeMetric> Metrics { get; } = new();
}

/// <summary>Represents one named semantic metric.</summary>
public sealed class VisualArtifactInterchangeMetric {
    private string _name = string.Empty;
    private string _value = string.Empty;

    /// <summary>Gets or sets the metric name.</summary>
    public string Name { get => _name; set => _name = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets the formatted metric value.</summary>
    public string Value { get => _value; set => _value = value ?? throw new ArgumentNullException(nameof(value)); }
}

/// <summary>Represents one reusable ordered scenario or guided path.</summary>
public sealed class VisualArtifactInterchangeScenario {
    /// <summary>Gets or sets the stable scenario id.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the visible scenario label.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional scenario description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets an optional accent color.</summary>
    public string? Color { get; set; }
    /// <summary>Gets or sets the default step duration in milliseconds.</summary>
    public int PlaybackDelayMilliseconds { get; set; } = 900;
    /// <summary>Gets or sets whether playback loops.</summary>
    public bool LoopPlayback { get; set; }
    /// <summary>Gets or sets whether a capable host may start playback automatically.</summary>
    public bool AutoPlay { get; set; }
    /// <summary>Gets or sets whether non-path members should be visually de-emphasized.</summary>
    public bool Spotlight { get; set; }
    /// <summary>Gets ordered scenario steps.</summary>
    public List<VisualArtifactInterchangeScenarioStep> Steps { get; } = new();
    /// <summary>Gets opaque caller-defined extension data.</summary>
    public Dictionary<string, string> Extensions { get; } = new(StringComparer.Ordinal);
}

/// <summary>Represents one ordered node or edge reference in a scenario.</summary>
public sealed class VisualArtifactInterchangeScenarioStep {
    /// <summary>Gets or sets the referenced node or edge id.</summary>
    public string TargetId { get; set; } = string.Empty;
    /// <summary>Gets or sets the exact target kind.</summary>
    public TopologyScenarioStepKind Kind { get; set; }
    /// <summary>Gets or sets an optional step label.</summary>
    public string? Label { get; set; }
    /// <summary>Gets or sets an optional step description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets an optional duration override in milliseconds.</summary>
    public int? DurationMilliseconds { get; set; }
    /// <summary>Gets opaque caller-defined extension data.</summary>
    public Dictionary<string, string> Extensions { get; } = new(StringComparer.Ordinal);
}

/// <summary>Represents one semantic diagram note, block, or annotation.</summary>
public sealed class VisualArtifactInterchangeAnnotation {
    /// <summary>Gets or sets the stable annotation id.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the well-known annotation role.</summary>
    public VisualArtifactInterchangeAnnotationRole Role { get; set; }
    /// <summary>Gets or sets the semantic annotation kind token.</summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Gets or sets the annotation text.</summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional placement token.</summary>
    public string? Placement { get; set; }
    /// <summary>Gets or sets the first covered semantic step index.</summary>
    public int? StartIndex { get; set; }
    /// <summary>Gets or sets the last covered semantic step index.</summary>
    public int? EndIndex { get; set; }
    /// <summary>Gets referenced node ids.</summary>
    public List<string> TargetIds { get; } = new();
    /// <summary>Gets or sets sequence-specific annotation semantics.</summary>
    public VisualArtifactInterchangeSequenceAnnotation? Sequence { get; set; }
    /// <summary>Gets opaque caller-defined extension data.</summary>
    public Dictionary<string, string> Extensions { get; } = new(StringComparer.Ordinal);
}
