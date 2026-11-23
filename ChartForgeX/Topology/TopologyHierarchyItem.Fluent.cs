using PowerForge.Fluent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System;

namespace ChartForgeX.Topology;

/// <summary>Hand-written fluent methods of <see cref="TopologyHierarchyItem"/> that do more than set one property.</summary>
public static partial class TopologyHierarchyItemFluent {
    /// <summary>Adds metadata and returns the same item.</summary>
    public static global::ChartForgeX.Topology.TopologyHierarchyItem Metadata(this global::ChartForgeX.Topology.TopologyHierarchyItem topologyHierarchyItem, string key, string value)
        => (topologyHierarchyItem ?? throw new System.ArgumentNullException(nameof(topologyHierarchyItem))).SetMetadata(key, value);
}
