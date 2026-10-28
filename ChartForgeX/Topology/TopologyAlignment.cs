using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

/// <summary>Declares one shared body-center coordinate and the global order along its perpendicular axis.</summary>
public enum TopologyAlignmentKind {
    /// <summary>Equal body center-Y; members advance from left to right.</summary>
    Row = 0,
    /// <summary>Equal body center-X; members advance from top to bottom.</summary>
    Column = 1
}

/// <summary>One ordered, identified native placement declaration. It never changes source node order or identity.</summary>
public sealed class TopologyAlignment {
    private string _id;
    private TopologyAlignmentKind _kind;

    /// <summary>Creates a row or column with detached ordered member IDs.</summary>
    /// <param name="id">Exact stable occurrence identity.</param>
    /// <param name="kind">Global row or column semantics.</param>
    /// <param name="nodeIds">At least two distinct existing node IDs, validated with the chart.</param>
    public TopologyAlignment(string id, TopologyAlignmentKind kind, IEnumerable<string> nodeIds) {
        _id = Required(id); Kind = kind;
        if (nodeIds == null) throw new ArgumentNullException(nameof(nodeIds));
        var budget = new TopologyAlignmentBudget(id);
        foreach (var nodeId in nodeIds) {
            budget.Work();
            const int maximum = ChartForgeX.VisualArtifacts.VisualArtifactInterchangeValidation.MaximumNodes;
            if (NodeIds.Count == maximum) throw new TopologyAlignmentLimitException("Interchange.MaximumNodes", maximum, (long)maximum + 1, id);
            NodeIds.Add(nodeId);
        }
    }

    /// <summary>Gets or sets the exact stable declaration ID.</summary>
    public string Id { get => _id; set => _id = Required(value); }
    /// <summary>Gets or sets the global row or column semantics.</summary>
    public TopologyAlignmentKind Kind {
        get => _kind;
        set { TopologyModelGuards.EnumDefined(value, nameof(value)); _kind = value; }
    }
    /// <summary>Gets exact node identities in authored order. Equalities may overlap other declarations.</summary>
    public List<string> NodeIds { get; } = new();
    /// <summary>Gets inert source and host facts retained through cloning and portable interchange.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);

    internal TopologyAlignment Clone() {
        var copy = new TopologyAlignment(Id, Kind, NodeIds);
        foreach (var item in Metadata) copy.Metadata.Add(item.Key, item.Value);
        return copy;
    }
    private static string Required(string value) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("Alignment ID is required.", nameof(value)) : value;
}

public sealed partial class TopologyChart {
    /// <summary>Gets ordered row/column declarations for Layered placement or supplied Manual geometry validation.</summary>
    /// <remarks>Automatic placement uses native spacing. Manual validation checks centers, order/non-overlap and
    /// actual containment without reconstructing an unrecorded typography gap. Other layout modes reject declarations.</remarks>
    public List<TopologyAlignment> Alignments { get; } = new();
}
