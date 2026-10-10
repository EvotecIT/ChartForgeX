using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>A candidate text-box position relative to a label's anchor, tried in list order.</summary>
public readonly struct LabelCandidate {
    /// <summary>Creates a candidate. Alignment fractions are zero at the leading edge and one at the trailing edge.</summary>
    public LabelCandidate(double offsetX, double offsetY, double horizontalAlignment = 0, double verticalAlignment = 0) {
        ChartGuards.Finite(offsetX, nameof(offsetX)); ChartGuards.Finite(offsetY, nameof(offsetY));
        ChartGuards.Finite(horizontalAlignment, nameof(horizontalAlignment)); ChartGuards.Finite(verticalAlignment, nameof(verticalAlignment));
        if (horizontalAlignment < 0 || horizontalAlignment > 1) throw new ArgumentOutOfRangeException(nameof(horizontalAlignment));
        if (verticalAlignment < 0 || verticalAlignment > 1) throw new ArgumentOutOfRangeException(nameof(verticalAlignment));
        OffsetX = offsetX; OffsetY = offsetY; HorizontalAlignment = horizontalAlignment; VerticalAlignment = verticalAlignment;
    }
    /// <summary>Gets the horizontal offset in logical pixels.</summary>
    public double OffsetX { get; }
    /// <summary>Gets the vertical offset in logical pixels.</summary>
    public double OffsetY { get; }
    /// <summary>Gets the horizontal alignment fraction.</summary>
    public double HorizontalAlignment { get; }
    /// <summary>Gets the vertical alignment fraction.</summary>
    public double VerticalAlignment { get; }
}

/// <summary>Defines what happens after all full-text candidate positions are blocked.</summary>
public enum LabelFallbackRule {
    /// <summary>Try successively shorter text with an ellipsis, then drop the label.</summary>
    EllipsisThenDrop,
    /// <summary>Drop the label without shortening it.</summary>
    Drop
}

/// <summary>A measured label with an anchor, priority and ordered candidate positions.</summary>
public sealed class LabelPlacementRequest {
    /// <summary>Creates a placement request. Higher priorities are placed first; equal priorities retain input order.</summary>
    public LabelPlacementRequest(string text, ChartPoint anchor, TextStyle style, IReadOnlyList<LabelCandidate> candidates, int priority = 0) {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        ChartGuards.Finite(anchor.X, nameof(anchor)); ChartGuards.Finite(anchor.Y, nameof(anchor));
        Anchor = anchor; Style = (style ?? throw new ArgumentNullException(nameof(style))).Clone();
        if (candidates == null) throw new ArgumentNullException(nameof(candidates));
        var copy = new LabelCandidate[candidates.Count];
        for (var i = 0; i < copy.Length; i++) copy[i] = candidates[i];
        Candidates = Array.AsReadOnly(copy); Priority = priority;
    }
    /// <summary>Gets the original text, retained even when its visible label is shortened or dropped.</summary>
    public string Text { get; }
    /// <summary>Gets the anchor in logical pixels.</summary>
    public ChartPoint Anchor { get; }
    /// <summary>Gets the resolved text style.</summary>
    public TextStyle Style { get; }
    /// <summary>Gets candidates in preference order.</summary>
    public IReadOnlyList<LabelCandidate> Candidates { get; }
    /// <summary>Gets placement priority.</summary>
    public int Priority { get; }
    /// <summary>Gets or sets the fallback rule.</summary>
    public LabelFallbackRule Fallback { get; set; } = LabelFallbackRule.EllipsisThenDrop;
    /// <summary>Gets or sets whether a displaced label should have a leader from its anchor.</summary>
    public bool HasLeaderLine { get; set; }
    /// <summary>Gets or sets the associated mark identity. A label fully contained in that mark is intentional; all other intersections are rejected.</summary>
    public string? AssociatedMarkId { get; set; }

    // Prepared compilers resolve ink when they choose a mark-relative placement, before fitting.
    internal Themes.SvgPaint? Paint { get; set; }
    /// <summary>Gets or sets optional bounds restricting this label, in addition to the scene bounds.</summary>
    public ChartRect? Bounds { get; set; }
    /// <summary>Gets or sets padding around text, such as its halo or badge inset, in logical pixels.</summary>
    public double Padding { get; set; }
    // Prepared axis requests retain their rotation so shortened strings use the same footprint.
    // MeasuredSize remains the unrotated text measurement for requests with this value set.
    internal double RotationDegrees { get; set; }
    internal TextMetrics? MeasuredSize { get; set; }
    internal TextMetrics? DecorationSize { get; set; }
}

/// <summary>An occupied mark rectangle that labels must avoid.</summary>
public readonly struct LabelObstacle {
    /// <summary>Creates a mark obstacle with a stable identity.</summary>
    public LabelObstacle(string id, ChartRect bounds) { Id = id ?? throw new ArgumentNullException(nameof(id)); Bounds = bounds; Shape = null; }
    internal LabelObstacle(string id, LabelMarkShape shape) { Id = id; Bounds = shape.Bounds; Shape = shape; }
    internal LabelMarkShape? Shape { get; }
    /// <summary>Gets the mark identity.</summary>
    public string Id { get; }
    /// <summary>Gets its bounds in logical pixels.</summary>
    public ChartRect Bounds { get; }
}

/// <summary>The deterministic placement result for one requested label.</summary>
public sealed class PlacedLabel {
    internal PlacedLabel(LabelPlacementRequest request, string text, ChartRect bounds, bool dropped, int candidateIndex, bool ellipsized = false) {
        Request = request; Text = text; Bounds = bounds; IsDropped = dropped; CandidateIndex = candidateIndex; IsEllipsized = ellipsized;
    }
    /// <summary>Gets the original request.</summary>
    public LabelPlacementRequest Request { get; }
    /// <summary>Gets the displayed text, or an empty string when dropped.</summary>
    public string Text { get; }
    /// <summary>Gets the measured text rectangle in logical pixels.</summary>
    public ChartRect Bounds { get; }
    /// <summary>Gets whether the label was dropped after its candidates were exhausted.</summary>
    public bool IsDropped { get; }
    /// <summary>Gets whether the visible text was shortened.</summary>
    public bool IsEllipsized { get; }
    /// <summary>Gets the chosen candidate index, or minus one when dropped.</summary>
    public int CandidateIndex { get; }
    /// <summary>Gets whether to draw a leader to the nearest point on the label box.</summary>
    public bool HasLeaderLine => !IsDropped && Request.HasLeaderLine && CandidateIndex > 0;
    /// <summary>Gets the leader's endpoint on the label box.</summary>
    public ChartPoint LeaderEnd => new(Math.Max(Bounds.Left, Math.Min(Bounds.Right, Request.Anchor.X)), Math.Max(Bounds.Top, Math.Min(Bounds.Bottom, Request.Anchor.Y)));
}
