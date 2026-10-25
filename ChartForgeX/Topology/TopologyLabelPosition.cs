using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

/// <summary>Defines which vertical text location is pinned by an explicit edge label position.</summary>
public enum TopologyLabelVerticalAnchor {
    /// <summary>Pins the center of the measured complete label allocation.</summary>
    Center,
    /// <summary>Pins the first painted text row's baseline, using its actual resolved native font.</summary>
    TextBaseline
}

/// <summary>An immutable explicit label location in the topology coordinate domain.</summary>
public sealed class TopologyLabelPosition {
    /// <summary>Captures finite coordinates and a supported native vertical anchor.</summary>
    public TopologyLabelPosition(ChartPoint point, TopologyLabelVerticalAnchor verticalAnchor = TopologyLabelVerticalAnchor.Center) {
        if (point.BreakBefore || point.ColorValue.HasValue) throw new ArgumentException("A label position carries coordinates only.", nameof(point));
        TopologyModelGuards.Finite(point.X, nameof(point)); TopologyModelGuards.Finite(point.Y, nameof(point));
        TopologyModelGuards.EnumDefined(verticalAnchor, nameof(verticalAnchor)); Point = point; VerticalAnchor = verticalAnchor;
    }
    /// <summary>Gets the captured location.</summary>
    public ChartPoint Point { get; }
    /// <summary>Gets whether the location pins the center or first text baseline.</summary>
    public TopologyLabelVerticalAnchor VerticalAnchor { get; }
    internal TopologyLabelPosition Transform(Func<ChartPoint, ChartPoint> transform) => new(transform(Point), VerticalAnchor);
}

public sealed partial class TopologyEdge {
    /// <summary>Gets or sets an explicit measured label location, independent of its leader anchor.</summary>
    /// <remarks>Null uses native label clearance. A non-null location owns placement and cannot be combined
    /// with LabelOffsetX/Y. Native measurement, fitting and clipping retain their ordinary contracts.</remarks>
    public TopologyLabelPosition? LabelPosition { get; set; }
}
