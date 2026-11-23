using PowerForge.Fluent;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ChartForgeX.Topology;

/// <summary>Hand-written fluent methods of <see cref="TopologyIconDefinition"/> that do more than set one property.</summary>
public static partial class TopologyIconDefinitionFluent {
    /// <summary>
    /// Adds or replaces icon metadata and returns the current icon definition.
    /// </summary>
    public static global::ChartForgeX.Topology.TopologyIconDefinition Metadata(this global::ChartForgeX.Topology.TopologyIconDefinition topologyIconDefinition, string key, string value)
        => (topologyIconDefinition ?? throw new System.ArgumentNullException(nameof(topologyIconDefinition))).SetMetadata(key, value);
}
