using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>Immutable per-mode tooltip and focus roles shared with report and grid hosts.</summary>
public sealed class VisualChromeTokens : IEquatable<VisualChromeTokens> {
    /// <summary>Creates tooltip colours, a CSS elevation and an independent focus ring.</summary>
    public VisualChromeTokens(ChartColor surface, ChartColor text, ChartColor muted, ChartColor line,
        ChartColor focusRing, string shadow, double tooltipRadius = 6, double controlRadius = 4) {
        if (string.IsNullOrWhiteSpace(shadow)) throw new ArgumentException("A tooltip shadow is required.", nameof(shadow));
        if (shadow.IndexOfAny(new[] { ';', '{', '}', '<', '>', '\"', '\'', '\r', '\n' }) >= 0)
            throw new ArgumentException("A tooltip shadow must be one CSS property value.", nameof(shadow));
        TooltipRadius = Radius(tooltipRadius, nameof(tooltipRadius)); ControlRadius = Radius(controlRadius, nameof(controlRadius));
        Surface = surface; Text = text; Muted = muted; Line = line; FocusRing = focusRing; Shadow = shadow;
    }
    /// <summary>Gets the tooltip surface.</summary>
    public ChartColor Surface { get; }
    /// <summary>Gets the tooltip text.</summary>
    public ChartColor Text { get; }
    /// <summary>Gets secondary tooltip text.</summary>
    public ChartColor Muted { get; }
    /// <summary>Gets the tooltip border.</summary>
    public ChartColor Line { get; }
    /// <summary>Gets the keyboard focus ring.</summary>
    public ChartColor FocusRing { get; }
    /// <summary>Gets the shared CSS tooltip shadow, including all elevation layers.</summary>
    public string Shadow { get; }
    /// <summary>Gets the overlay corner radius in logical pixels.</summary>
    public double TooltipRadius { get; }
    /// <summary>Gets the control corner radius in logical pixels.</summary>
    public double ControlRadius { get; }
    /// <summary>Compares the complete chrome contract.</summary>
    public bool Equals(VisualChromeTokens? other) => other != null && Surface.Equals(other.Surface)
        && Text.Equals(other.Text) && Muted.Equals(other.Muted) && Line.Equals(other.Line)
        && FocusRing.Equals(other.FocusRing) && Shadow == other.Shadow
        && TooltipRadius.Equals(other.TooltipRadius) && ControlRadius.Equals(other.ControlRadius);
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is VisualChromeTokens other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() => Surface.GetHashCode() ^ Text.GetHashCode() ^ Muted.GetHashCode()
        ^ Line.GetHashCode() ^ FocusRing.GetHashCode() ^ Shadow.GetHashCode() ^ TooltipRadius.GetHashCode() ^ ControlRadius.GetHashCode();
    private static double Radius(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(name);
        return value;
    }
}
