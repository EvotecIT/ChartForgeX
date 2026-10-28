using System;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

public sealed partial class TopologyNode {
    private double? _maximumTextWidth;
    private double? _contentPadding;
    /// <summary>Gets or sets base typography for this node's captions. Native role sizes remain relative to this base; heading emphasis is the fallback when FontWeight is omitted.</summary>
    /// <remarks>Preparation clones this override before measuring or capturing the immutable native text scene.</remarks>
    public TextStyleOverride? TextStyle { get; set; }
    /// <summary>Gets or sets typography for the node subtitle, overriding the base caption style only for that role.</summary>
    /// <remarks>Null preserves ordinary sizing and paint. Preparation and interchange detach this mutable override.</remarks>
    public TextStyleOverride? SubtitleTextStyle { get; set; }
    /// <summary>Gets or sets additional authored padding inside the shared native shape-content allocation; null retains incumbent geometry.</summary>
    public double? ContentPadding {
        get => _contentPadding;
        set { if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < 0)) throw new ArgumentOutOfRangeException(nameof(value)); _contentPadding = value; }
    }
    /// <summary>Gets or sets a measured text wrapping width in logical units. Null retains incumbent natural-width sizing.</summary>
    /// <remarks>FitLabelToContent grows the node vertically at this width. The shared text owner retains grapheme-safe wrapping and explicit row ceilings.</remarks>
    public double? MaximumTextWidth {
        get => _maximumTextWidth;
        set { if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value <= 0)) throw new ArgumentOutOfRangeException(nameof(value)); _maximumTextWidth = value; }
    }
}

public sealed partial class TopologyGroup {
    /// <summary>Gets or sets base typography for this region's measured native header and routing reservation.</summary>
    public TextStyleOverride? TextStyle { get; set; }
}

public sealed partial class TopologyEdge {
    /// <summary>Gets or sets base typography for this relationship's measured native label and placement obstacle.</summary>
    public TextStyleOverride? TextStyle { get; set; }
}
