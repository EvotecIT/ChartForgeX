using PowerForge.Fluent;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ChartForgeX.Topology;

/// <summary>Hand-written fluent methods of <see cref="TopologyIconPack"/> that do more than set one property.</summary>
public static partial class TopologyIconPackFluent {
    /// <summary>
    /// Adds or replaces pack metadata and returns the current pack.
    /// </summary>
    public static global::ChartForgeX.Topology.TopologyIconPack Metadata(this global::ChartForgeX.Topology.TopologyIconPack topologyIconPack, string key, string value)
        => (topologyIconPack ?? throw new System.ArgumentNullException(nameof(topologyIconPack))).SetMetadata(key, value);
}
