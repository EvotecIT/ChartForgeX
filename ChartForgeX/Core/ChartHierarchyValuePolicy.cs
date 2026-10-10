namespace ChartForgeX.Core;

/// <summary>Determines how authored group sizes contribute to an area hierarchy.</summary>
public enum ChartHierarchyValuePolicy {
    /// <summary>Groups sum descendant leaf sizes; authored group values remain facts without affecting geometry.</summary>
    LeafAggregate,
    /// <summary>Authored group values include their children. Null group values sum resolved children; unused capacity remains a gap.</summary>
    AuthoredTotal
}
