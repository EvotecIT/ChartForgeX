using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

/// <summary>Optional native endpoint marker layout over the edge's existing marker kind and paint.</summary>
/// <remarks>Scale multiplies the canonical marker's nominal geometry; Offset moves it along its outward
/// endpoint tangent in topology units. StrokeWidth uses topology units independently of Scale; null inherits
/// the axis stroke. Viewport fitting applies once to geometry, offset and stroke. A captured prepared style
/// stores that fitted presentation. Null style retains ordinary marker geometry and paint.</remarks>
public sealed class TopologyMarkerStyle {
    private double _scale = 1, _offset;
    private double? _strokeWidth;
    /// <summary>Gets or sets a finite positive nominal geometry multiplier, no greater than 4096.</summary>
    public double Scale { get => _scale; set { TopologyModelGuards.PositiveFinite(value, nameof(Scale)); if (value > 4096) throw new ArgumentOutOfRangeException(nameof(Scale)); _scale = value; } }
    /// <summary>Gets or sets a finite signed outward offset, bounded to 4096 topology units in either direction.</summary>
    public double Offset { get => _offset; set { TopologyModelGuards.Finite(value, nameof(Offset)); if (Math.Abs(value) > 4096) throw new ArgumentOutOfRangeException(nameof(Offset)); _offset = value; } }
    /// <summary>Gets or sets a finite stroke width from zero through 4096; null inherits the native axis stroke.</summary>
    public double? StrokeWidth { get => _strokeWidth; set { if (value.HasValue) { TopologyModelGuards.NonNegativeFinite(value.Value, nameof(StrokeWidth)); if (value.Value > 4096) throw new ArgumentOutOfRangeException(nameof(StrokeWidth)); } _strokeWidth = value; } }
    /// <summary>Creates an independent mutable copy.</summary>
    public TopologyMarkerStyle Clone() => (TopologyMarkerStyle)MemberwiseClone();
    internal TopologyMarkerStyle Capture(double viewportScale, double axisStroke) => new() { Scale = Scale * viewportScale, Offset = Offset * viewportScale, StrokeWidth = (StrokeWidth ?? axisStroke) * viewportScale };
    internal double Extent(double axisStroke) => 15 * Scale + Math.Abs(Offset) + (StrokeWidth ?? axisStroke) / 2;
}

public sealed partial class TopologyEdge {
    /// <summary>Gets or sets optional source marker layout. Marker kind, direction and paint remain on the edge.</summary>
    public TopologyMarkerStyle? SourceMarkerStyle { get; set; }
    /// <summary>Gets or sets optional target marker layout. Marker kind, direction and paint remain on the edge.</summary>
    public TopologyMarkerStyle? TargetMarkerStyle { get; set; }
}
