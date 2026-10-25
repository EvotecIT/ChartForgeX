using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

/// <summary>Optional native surface styling for an existing topology detail row.</summary>
/// <remarks>The detail's text, identity, status and TextStyle remain the canonical facts.
/// Null BadgeStyle retains ordinary detail layout and paint. Card, CompactCard and Pill bodies support these surfaces; other visible display roles reject them explicitly. All lengths use topology coordinates.</remarks>
public sealed class TopologyDetailBadgeStyle {
    private double _strokeWidth = 1, _cornerRadius = 4, _horizontalPadding = 10, _minimumHeight = 26, _gap = 4;
    /// <summary>Gets or sets the fill. Null paints no fill.</summary>
    public ChartColor? Fill { get; set; }
    /// <summary>Gets or sets the stroke. Null paints no stroke.</summary>
    public ChartColor? Stroke { get; set; }
    /// <summary>Gets or sets a finite stroke width from zero through 4096.</summary>
    public double StrokeWidth { get => _strokeWidth; set => _strokeWidth = Length(value, nameof(StrokeWidth)); }
    /// <summary>Gets or sets a finite corner radius from zero through 4096.</summary>
    public double CornerRadius { get => _cornerRadius; set => _cornerRadius = Length(value, nameof(CornerRadius)); }
    /// <summary>Gets or sets the padding on each horizontal side, from zero through 4096.</summary>
    public double HorizontalPadding { get => _horizontalPadding; set => _horizontalPadding = Length(value, nameof(HorizontalPadding)); }
    /// <summary>Gets or sets the positive minimum badge height, no greater than 4096.</summary>
    public double MinimumHeight { get => _minimumHeight; set { TopologyModelGuards.PositiveFinite(value, nameof(MinimumHeight)); _minimumHeight = Length(value, nameof(MinimumHeight)); } }
    /// <summary>Gets or sets the following row gap, from zero through 4096.</summary>
    public double Gap { get => _gap; set => _gap = Length(value, nameof(Gap)); }
    /// <summary>Creates an independent mutable style copy.</summary>
    public TopologyDetailBadgeStyle Clone() => (TopologyDetailBadgeStyle)MemberwiseClone();
    internal TopologyDetailBadgeStyle Scale(double scale) {
        var copy = Clone(); copy.StrokeWidth *= scale; copy.CornerRadius *= scale;
        copy.HorizontalPadding *= scale; copy.MinimumHeight *= scale; copy.Gap *= scale; return copy;
    }
    private static double Length(double value, string name) {
        TopologyModelGuards.NonNegativeFinite(value, name);
        if (value > 4096) throw new ArgumentOutOfRangeException(name, value, "Badge lengths cannot exceed 4096.");
        return value;
    }
}

public sealed partial class TopologyNodeDetail {
    /// <summary>Gets or sets an optional surface around this detail's existing text. Non-null requires portable version 2.</summary>
    public TopologyDetailBadgeStyle? BadgeStyle { get; set; }
}
