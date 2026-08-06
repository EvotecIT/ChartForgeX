namespace ChartForgeX.Topology;

/// <summary>
/// Controls how the direct children of a hierarchy item are arranged inside their layered-layout branch.
/// Policies inherit through the subtree until another item overrides them.
/// </summary>
public enum TopologyHierarchyLayoutPolicy {
    /// <summary>Uses a single sibling band when it fits and balanced compact packing when it does not.</summary>
    Auto = 0,

    /// <summary>Keeps direct children in one sibling band, allowing the final layout normalizer to fit wide content.</summary>
    Standard = 1,

    /// <summary>Packs direct children into a balanced deterministic grid.</summary>
    Compact = 2,

    /// <summary>Stacks direct children in one vertical column.</summary>
    Vertical = 3
}
