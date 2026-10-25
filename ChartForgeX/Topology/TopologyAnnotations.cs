using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

/// <summary>The side on which a diagram annotation is placed.</summary>
public enum TopologyAnnotationSide {
    /// <summary>The left side of the graph.</summary>
    Left,
    /// <summary>The right side of the graph.</summary>
    Right
}

/// <summary>A visible note that does not introduce a graph node or relationship.</summary>
public sealed class TopologyAnnotation {
    internal double PreparedDefaultUnits { get; set; } = 1;
    internal ChartForgeX.Primitives.ChartRect? PreparedRoutingViewport { get; set; }
    /// <summary>Creates a note with stable identity and complete visible text.</summary>
    public TopologyAnnotation(string id, string text, string? targetId = null, TopologyAnnotationSide side = TopologyAnnotationSide.Right) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Annotation ids must not be empty.", nameof(id));
        if (text == null) throw new ArgumentNullException(nameof(text));
        TopologyModelGuards.EnumDefined(side, nameof(side));
        Id = id;
        Text = text;
        TargetId = targetId;
        Side = side;
    }

    /// <summary>Gets the stable annotation id.</summary>
    public string Id { get; }

    /// <summary>Gets the note text, including authored line breaks.</summary>
    public string Text { get; }

    /// <summary>Gets the attached node or group id, or null for an unattached note.</summary>
    public string? TargetId { get; }

    /// <summary>Gets the graph side used for the note gutter.</summary>
    public TopologyAnnotationSide Side { get; }
}

public sealed partial class TopologyNode {
    /// <summary>Gets or sets whether the node's visible caption is painted; its semantic label remains available.</summary>
    public bool ShowLabel { get; set; } = true;
}

public sealed partial class TopologyChart {
    /// <summary>Gets diagram notes, separate from semantic nodes and edges.</summary>
    /// <remarks>Notes are measured in side gutters and included in natural-size and fitted exports.
    /// Attached notes use dashed leaders. Their ids must be unique and attachments must reference existing nodes or groups.</remarks>
    public List<TopologyAnnotation> Annotations { get; } = new();
}
