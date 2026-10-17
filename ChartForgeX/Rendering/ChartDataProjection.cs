using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

// Captures the completed renderer map for host gestures. It does not enable native viewport re-preparation.
internal sealed class ChartDataProjection {
    private ChartDataProjection(ChartRect plot, ChartDataAxisProjection x, ChartDataAxisProjection? y = null, bool horizontal = false,
        IReadOnlyList<ChartAxisLabel>? categoryLabels = null) {
        Plot = plot; X = x; Y = y; Horizontal = horizontal;
        CategoryLabels = categoryLabels == null ? null : System.Array.AsReadOnly(categoryLabels.ToArray());
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
            ? new ChartAxis { Reversed = x.Reversed, Scale = x.Scale == ChartScaleKind.Time ? ChartScaleKind.Time : ChartScaleKind.Linear } : x);
        return !UsablePlot(plot) || projectedX == null ? null : new ChartDataProjection(plot, projectedX);
    }

    private static bool UsablePlot(ChartRect plot) => ChartMath.IsFinite(plot.Left) && ChartMath.IsFinite(plot.Top)
        && ChartMath.IsFinite(plot.Right) && ChartMath.IsFinite(plot.Bottom) && plot.Width > 0 && plot.Height > 0;
    internal ChartRect Plot { get; }
    internal ChartDataAxisProjection X { get; }
    internal ChartDataAxisProjection? Y { get; }
    internal bool Horizontal { get; }
    internal IReadOnlyList<ChartAxisLabel>? CategoryLabels { get; }
    internal ChartDataProjection WithDisplayTicks(IReadOnlyList<ChartAxisLabel> x, IReadOnlyList<ChartAxisLabel>? y = null,
        System.Func<double, string>? formatX = null, System.Func<double, string>? formatY = null,
        IReadOnlyList<ChartAxisLabel>? categoryLabels = null) =>
        new(Plot, X.WithDisplayTicks(x, formatX), Y == null ? null : Y.WithDisplayTicks(y ?? System.Array.Empty<ChartAxisLabel>(), formatY), Horizontal, categoryLabels);
}

/// <summary>Captures one axis caption and its precise ordered-time identity independently of browser date support.</summary>
internal sealed class ChartDataAxisCaption {
    internal ChartDataAxisCaption(double value, string text, string? instant) { Value = value; Text = text; Instant = instant; }
    internal double Value { get; }
    internal string Text { get; }
    internal string? Instant { get; }
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
    internal IReadOnlyList<ChartDataAxisCaption> DisplayTicks { get; private set; } = System.Array.Empty<ChartDataAxisCaption>();
    internal IReadOnlyList<ChartDataAxisCaption> DisplayBounds { get; private set; } = System.Array.Empty<ChartDataAxisCaption>();
    internal ChartDataAxisProjection WithDisplayTicks(IReadOnlyList<ChartAxisLabel> ticks, System.Func<double, string>? format) {
        var copy = (ChartDataAxisProjection)MemberwiseClone();
        ChartDataAxisCaption Caption(ChartAxisLabel label) => new(label.Value, label.Text,
            Scale == ChartScaleKind.Time && Minimum >= 0 && ChartDateTime.TryFromOrderedOaDate(label.Value, out var instant)
                ? instant.ToString("O", CultureInfo.InvariantCulture) : null);
        copy.DisplayTicks = System.Array.AsReadOnly(ticks.Select(Caption).ToArray());
        // The plot may end between ticks. Reuse painted captions before evaluating the
        // canonical formatter for the at most two otherwise unrepresented boundaries.
        if (format != null) {
            ChartDataAxisCaption Boundary(double value) {
                foreach (var tick in copy.DisplayTicks) if (tick.Value == value) return tick;
                return Caption(new ChartAxisLabel(value, format(value)));
            }
            copy.DisplayBounds = System.Array.AsReadOnly(new[] { Boundary(Minimum), Boundary(Maximum) });
        }
        return copy;
    }
}
