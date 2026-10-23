using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

/// <summary>A deterministic circular Venn fit, with quantitative residuals rather than an exactness claim for arbitrary three-set inputs.</summary>
public sealed class VennDiagramLayout {
    internal VennDiagramLayout(IReadOnlyList<VennCirclePlacement> circles, IReadOnlyList<VennRegionPlacement> regions, double maximumRelativeAreaError) {
        Circles = new List<VennCirclePlacement>(circles).AsReadOnly(); Regions = new List<VennRegionPlacement>(regions).AsReadOnly(); MaximumRelativeAreaError = maximumRelativeAreaError;
    }
    /// <summary>Gets circles whose areas preserve the source set-size ratios.</summary>
    public IReadOnlyList<VennCirclePlacement> Circles { get; }
    /// <summary>Gets requested and achieved areas for declared intersections.</summary>
    public IReadOnlyList<VennRegionPlacement> Regions { get; }
    /// <summary>Gets the largest absolute intersection-area residual divided by the largest source set size.</summary>
    public double MaximumRelativeAreaError { get; }
    /// <summary>Gets whether every declared region is within half a percent of the largest source set size.</summary>
    public bool IsAreaAccurate => MaximumRelativeAreaError <= .005;
}

/// <summary>A placed set circle in the block's content coordinate system.</summary>
public sealed class VennCirclePlacement {
    internal VennCirclePlacement(string setId, ChartPoint center, double radius) { SetId = setId; Center = center; Radius = radius; }
    /// <summary>Gets the source set identity.</summary>
    public string SetId { get; }
    /// <summary>Gets the circle center.</summary>
    public ChartPoint Center { get; }
    /// <summary>Gets the circle radius.</summary>
    public double Radius { get; }
}

/// <summary>The actual circular intersection and its cardinality fit.</summary>
public sealed class VennRegionPlacement {
    internal VennRegionPlacement(IReadOnlyList<string> setIds, double requestedArea, double actualArea, string path, ChartPoint labelAnchor) {
        SetIds = new List<string>(setIds).AsReadOnly(); RequestedArea = requestedArea; ActualArea = actualArea; Path = path; LabelAnchor = labelAnchor;
    }
    /// <summary>Gets the participating set identities.</summary>
    public IReadOnlyList<string> SetIds { get; }
    /// <summary>Gets the authored intersection cardinality.</summary>
    public double RequestedArea { get; }
    /// <summary>Gets the achieved intersection area expressed in source cardinality units.</summary>
    public double ActualArea { get; }
    /// <summary>Gets the real circle-intersection SVG path, or an empty string for an empty intersection.</summary>
    public string Path { get; }
    /// <summary>Gets an interior label anchor when the region has positive area.</summary>
    public ChartPoint LabelAnchor { get; }
}
