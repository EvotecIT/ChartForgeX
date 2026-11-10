using System;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Names the mark drawn at an edge's source, middle or target arrow position.
/// </summary>
public enum GraphEdgeArrowShape {
    /// <summary>Use the default filled arrowhead.</summary>
    Arrow,

    /// <summary>Use a filled arrowhead with a notched back.</summary>
    Vee,

    /// <summary>Use an open, outlined triangle such as a generalization marker.</summary>
    Triangle,

    /// <summary>Use a short bar across the edge.</summary>
    Bar,

    /// <summary>Use a filled circle.</summary>
    Circle,

    /// <summary>Use a filled diamond.</summary>
    Diamond
}

/// <summary>
/// Maps <see cref="GraphEdgeStyle.Value"/> onto a stroke width range across a scene.
/// </summary>
public sealed class GraphEdgeWidthScale {
    /// <summary>Gets or sets the stroke width used for the smallest edge value.</summary>
    public double MinimumWidth { get; set; } = 1;

    /// <summary>Gets or sets the stroke width used for the largest edge value.</summary>
    public double MaximumWidth { get; set; } = 8;

    internal void Validate() {
        if (!IsPositiveFinite(MinimumWidth) || !IsPositiveFinite(MaximumWidth)) throw new InvalidOperationException("Graph scene edge width scale bounds must be finite and greater than zero.");
        if (MaximumWidth < MinimumWidth) throw new InvalidOperationException("Graph scene edge width scale maximum must not be smaller than its minimum.");
    }

    private static bool IsPositiveFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
}
