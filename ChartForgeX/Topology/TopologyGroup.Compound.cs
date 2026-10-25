namespace ChartForgeX.Topology;

public sealed partial class TopologyGroup {
    private TopologyLayoutDirection? _layoutDirection;

    /// <summary>Gets or sets the containing group id, or null for a root region.</summary>
    /// <remarks>Automatic layered layout supports at most 128 nested regions and requires an acyclic parent tree.</remarks>
    public string? ParentGroupId { get; set; }

    /// <summary>Gets or sets a local automatic layered or swimlane composition direction, or null to inherit the containing region.</summary>
    public TopologyLayoutDirection? LayoutDirection {
        get => _layoutDirection;
        set {
            if (value.HasValue) TopologyModelGuards.EnumDefined(value.Value, nameof(value));
            _layoutDirection = value;
        }
    }
}
