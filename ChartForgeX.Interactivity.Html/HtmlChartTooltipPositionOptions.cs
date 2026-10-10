using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Configures HTML tooltip anchoring, ordered placement fallback, and screen offsets.</summary>
public sealed class HtmlChartTooltipPositionOptions {
    private HtmlChartTooltipAnchor _anchor;
    private ReadOnlyCollection<HtmlChartTooltipPlacement> _placements = Array.AsReadOnly(new[] { HtmlChartTooltipPlacement.BottomRight });
    private double _gap = 14;
    private double _offsetX;
    private double _offsetY;

    /// <summary>Gets or sets the anchor. The default is <see cref="HtmlChartTooltipAnchor.Pointer"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The anchor is not a defined enum value.</exception>
    public HtmlChartTooltipAnchor Anchor {
        get => _anchor;
        set {
            if (!Enum.IsDefined(typeof(HtmlChartTooltipAnchor), value)) throw new ArgumentOutOfRangeException(nameof(Anchor));
            _anchor = value;
        }
    }

    /// <summary>
    /// Gets or sets a copied, read-only sequence of directions, tried in order against the browser viewport
    /// with an 8 CSS pixel inset. If none fits, the first direction is clamped to that viewport.
    /// The default contains only <see cref="HtmlChartTooltipPlacement.BottomRight"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The sequence is null.</exception>
    /// <exception cref="ArgumentException">The sequence is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A direction is not a defined enum value.</exception>
    public IReadOnlyList<HtmlChartTooltipPlacement> Placements {
        get => _placements;
        set {
            if (value == null) throw new ArgumentNullException(nameof(Placements));
            if (value.Count == 0) throw new ArgumentException("Tooltip placements must contain at least one direction.", nameof(Placements));
            var copy = value.ToArray();
            foreach (var placement in copy)
                if (!Enum.IsDefined(typeof(HtmlChartTooltipPlacement), placement)) throw new ArgumentOutOfRangeException(nameof(Placements));
            _placements = Array.AsReadOnly(copy);
        }
    }

    /// <summary>
    /// Gets or sets the finite, non-negative gap in CSS pixels, applied on each directed axis.
    /// The default is 14; diagonal directions apply the gap both horizontally and vertically.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The gap is negative or non-finite.</exception>
    public double Gap {
        get => _gap;
        set => _gap = Finite(value, nameof(Gap), nonNegative: true);
    }

    /// <summary>Gets or sets a finite horizontal CSS pixel offset. Positive values move right. The default is zero.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The offset is non-finite.</exception>
    public double OffsetX {
        get => _offsetX;
        set => _offsetX = Finite(value, nameof(OffsetX));
    }

    /// <summary>Gets or sets a finite vertical CSS pixel offset. Positive values move down. The default is zero.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The offset is non-finite.</exception>
    public double OffsetY {
        get => _offsetY;
        set => _offsetY = Finite(value, nameof(OffsetY));
    }

    internal string SerializedPlacements => string.Join(",", _placements.Select(placement => placement switch {
        HtmlChartTooltipPlacement.TopRight => "top-right",
        HtmlChartTooltipPlacement.BottomRight => "bottom-right",
        HtmlChartTooltipPlacement.BottomLeft => "bottom-left",
        HtmlChartTooltipPlacement.TopLeft => "top-left",
        _ => placement.ToString().ToLowerInvariant()
    }));

    private static double Finite(double value, string propertyName, bool nonNegative = false) {
        if (double.IsNaN(value) || double.IsInfinity(value) || nonNegative && value < 0)
            throw new ArgumentOutOfRangeException(propertyName, "Tooltip position values must be finite" + (nonNegative ? " and non-negative." : "."));
        return value;
    }
}
