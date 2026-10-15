using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

// Captures the completed renderer map for host gestures. It does not enable native viewport re-preparation.
internal sealed class ChartDataProjection {
    internal ChartDataProjection(ChartRect plot, ChartRange range, ChartAxis x, ChartAxis y, bool horizontal) {
        Plot = plot;
        X = new ChartDataAxisProjection(range.MinX, range.MaxX, x);
        Y = new ChartDataAxisProjection(range.MinY, range.MaxY, horizontal
            ? new ChartAxis { Reversed = y.Reversed, Scale = y.Scale == ChartScaleKind.Time ? ChartScaleKind.Time : ChartScaleKind.Linear } : y);
        Horizontal = horizontal;
    }
    internal ChartDataProjection(ChartRect plot, double minimum, double maximum, ChartAxis x, bool linearOnly = false) {
        Plot = plot;
        X = new ChartDataAxisProjection(minimum, maximum, linearOnly
            ? new ChartAxis { Scale = x.Scale == ChartScaleKind.Time ? ChartScaleKind.Time : ChartScaleKind.Linear } : x);
    }
    internal ChartRect Plot { get; }
    internal ChartDataAxisProjection X { get; }
    internal ChartDataAxisProjection? Y { get; }
    internal bool Horizontal { get; }
}

internal sealed class ChartDataAxisProjection {
    internal ChartDataAxisProjection(double minimum, double maximum, ChartAxis axis) {
        Minimum = minimum; Maximum = maximum; Scale = axis.Scale;
        Threshold = axis.SymmetricLogarithmThreshold; Reversed = axis.Reversed;
        TransformedMinimum = ChartScaleTransform.Forward(minimum, axis);
        TransformedMaximum = ChartScaleTransform.Forward(maximum, axis);
    }
    internal double Minimum { get; }
    internal double Maximum { get; }
    internal double TransformedMinimum { get; }
    internal double TransformedMaximum { get; }
    internal ChartScaleKind Scale { get; }
    internal double Threshold { get; }
    internal bool Reversed { get; }
}
