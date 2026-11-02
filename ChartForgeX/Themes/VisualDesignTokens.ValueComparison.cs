using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

public sealed partial class VisualDesignTokens {
    // Compare owned arrays directly; native capture must not materialize theme JSON or clone palettes for equality.
    internal bool SameValues(VisualDesignTokens other) =>
        Background.Equals(other.Background)
        && Surface.Equals(other.Surface)
        && ElevatedSurface.Equals(other.ElevatedSurface)
        && Foreground.Equals(other.Foreground)
        && MutedForeground.Equals(other.MutedForeground)
        && Border.Equals(other.Border)
        && Accent.Equals(other.Accent)
        && SecondaryAccent.Equals(other.SecondaryAccent)
        && Positive.Equals(other.Positive)
        && Warning.Equals(other.Warning)
        && Negative.Equals(other.Negative)
        && Disabled.Equals(other.Disabled)
        && PlotBorder.Equals(other.PlotBorder)
        && Muted.Equals(other.Muted)
        && Grid.Equals(other.Grid)
        && Axis.Equals(other.Axis)
        && Info.Equals(other.Info)
        && Quiet.Equals(other.Quiet)
        && QuietLine.Equals(other.QuietLine)
        && Neutral.Equals(other.Neutral)
        && Neutral2.Equals(other.Neutral2)
        && Neutral3.Equals(other.Neutral3)
        && SameColors(_palette, other._palette) && SameColors(_sequentialRamp, other._sequentialRamp)
        && SameRamp(DivergingRamp, other.DivergingRamp)
        && SamePair(_status.Critical, other._status.Critical)
        && SamePair(_status.High, other._status.High)
        && SamePair(_status.Medium, other._status.Medium)
        && SamePair(_status.Low, other._status.Low)
        && SamePair(_status.Info, other._status.Info)
        && SamePair(_status.Pass, other._status.Pass)
        && SamePair(_status.Neutral, other._status.Neutral)
        && SamePair(_status.Maintenance, other._status.Maintenance);

    private static bool SamePair(VisualTokenColor a, VisualTokenColor b) => a.Fill.Equals(b.Fill) && a.Ink.Equals(b.Ink);
    private static bool SameColors(IReadOnlyList<ChartColor>? a, IReadOnlyList<ChartColor>? b) {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null || a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++) if (!a[i].Equals(b[i])) return false;
        return true;
    }
    private static bool SameRamp(VisualDivergingRamp? a, VisualDivergingRamp? b) => ReferenceEquals(a, b)
        || a != null && b != null && a.Neutral.Equals(b.Neutral) && SameColors(a.Negative, b.Negative) && SameColors(a.Positive, b.Positive);
}
