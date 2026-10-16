using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

// Captures the completed renderer map for host gestures. It does not enable native viewport re-preparation.
internal sealed class ChartDataProjection {
    private ChartDataProjection(ChartRect plot, ChartDataAxisProjection x, ChartDataAxisProjection? y = null, bool horizontal = false) {
        Plot = plot; X = x; Y = y; Horizontal = horizontal;
    }

    internal static ChartDataProjection? TryCreate(ChartRect plot, ChartRange range, ChartAxis x, ChartAxis y, bool horizontal, bool includeY = true) {
        var projectedX = ChartDataAxisProjection.TryCreate(range.MinX, range.MaxX, x);
        if (!UsablePlot(plot) || projectedX == null) return null;
        var projectedY = includeY ? ChartDataAxisProjection.TryCreate(range.MinY, range.MaxY, horizontal
            ? new ChartAxis { Reversed = y.Reversed, Scale = y.Scale == ChartScaleKind.Time ? ChartScaleKind.Time : ChartScaleKind.Linear } : y) : null;
        return new ChartDataProjection(plot, projectedX, projectedY, horizontal);
    }

    internal static ChartDataProjection? TryCreate(ChartRect plot, double minimum, double maximum, ChartAxis x, bool linearOnly = false) {
        var projectedX = ChartDataAxisProjection.TryCreate(minimum, maximum, linearOnly
            ? new ChartAxis { Scale = x.Scale == ChartScaleKind.Time ? ChartScaleKind.Time : ChartScaleKind.Linear } : x);
        return !UsablePlot(plot) || projectedX == null ? null : new ChartDataProjection(plot, projectedX);
    }

    private static bool UsablePlot(ChartRect plot) => ChartMath.IsFinite(plot.Left) && ChartMath.IsFinite(plot.Top)
        && ChartMath.IsFinite(plot.Right) && ChartMath.IsFinite(plot.Bottom) && plot.Width > 0 && plot.Height > 0;
    internal ChartRect Plot { get; }
    internal ChartDataAxisProjection X { get; }
    internal ChartDataAxisProjection? Y { get; }
    internal bool Horizontal { get; }
}

internal sealed class ChartDataAxisProjection {
    private ChartDataAxisProjection(double minimum, double maximum, ChartAxis axis, double transformedMinimum, double transformedMaximum) {
        Minimum = minimum; Maximum = maximum; Scale = axis.Scale;
        Threshold = axis.SymmetricLogarithmThreshold; Reversed = axis.Reversed;
        TransformedMinimum = transformedMinimum; TransformedMaximum = transformedMaximum;
    }

    internal static ChartDataAxisProjection? TryCreate(double minimum, double maximum, ChartAxis axis) {
        if (!ChartMath.IsFinite(minimum) || !ChartMath.IsFinite(maximum) || maximum <= minimum) return null;
        var transformedMinimum = ChartScaleTransform.Forward(minimum, axis);
        var transformedMaximum = ChartScaleTransform.Forward(maximum, axis);
        return ChartMath.IsFinite(transformedMinimum) && ChartMath.IsFinite(transformedMaximum) && transformedMaximum > transformedMinimum
            ? new ChartDataAxisProjection(minimum, maximum, axis, transformedMinimum, transformedMaximum) : null;
    }
    internal double Minimum { get; }
    internal double Maximum { get; }
    internal double TransformedMinimum { get; }
    internal double TransformedMaximum { get; }
    internal ChartScaleKind Scale { get; }
    internal double Threshold { get; }
    internal bool Reversed { get; }
}
