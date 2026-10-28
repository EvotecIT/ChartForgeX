namespace ChartForgeX.Topology;

/// <summary>Controls how a group participates in a containing group's presentation.</summary>
public enum TopologyGroupPresentation {
    /// <summary>A labeled group with its own enclosing surface.</summary>
    Container,
    /// <summary>An ordered, unlabeled compartment of its parent with a separator between siblings.</summary>
    /// <remarks>Automatic layered layout places compartments across their parent's flow direction.</remarks>
    Compartment
}

public sealed partial class TopologyGroup {
    private TopologyGroupPresentation _presentation;

    /// <summary>Gets or sets whether the group title and subtitle are painted. Semantic labels and icons are retained.</summary>
    public bool ShowLabel { get; set; } = true;

    /// <summary>Gets or sets whether this group is an independent container or a compartment of its parent.</summary>
    /// <remarks>Compartments retain identity and bounds, but do not paint a duplicate surface or header.</remarks>
    public TopologyGroupPresentation Presentation {
        get => _presentation;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _presentation = value;
        }
    }
}
