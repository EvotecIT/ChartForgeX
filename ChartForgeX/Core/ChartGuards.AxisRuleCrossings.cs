using System;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

internal static partial class ChartGuards {
    /// <summary>Crossing rules use the same opposite primary physical domain as Cartesian marks.</summary>
    private static void ValidateAxisRuleCrossings(Chart chart) {
        var x = chart.Options.XAxis; var y = chart.Options.YAxis;
        var secondaryX = chart.Options.SecondaryXAxis; var secondaryY = chart.Options.SecondaryYAxis;
        if (!x.CrossingCoordinate.HasValue && !y.CrossingCoordinate.HasValue && !secondaryX.CrossingCoordinate.HasValue && !secondaryY.CrossingCoordinate.HasValue) return;
        if (chart.Options.IsSparkline || chart.Series.Any(series => !VisualCartesianCompiler.Supports(series.Kind)))
            throw new NotSupportedException("Axis crossings require a non-sparkline Cartesian chart.");
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart);
        if (secondaryX.CrossingCoordinate.HasValue && (!horizontal || !chart.Series.Any(series => series.ValueAxis == ChartAxisSide.Secondary)))
            throw new NotSupportedException("A secondary X crossing requires a horizontal series on the secondary value axis.");
        if (secondaryY.CrossingCoordinate.HasValue && (horizontal || !chart.Series.Any(series => series.ValueAxis == ChartAxisSide.Secondary)))
            throw new NotSupportedException("A secondary Y crossing requires a vertical series on the secondary value axis.");
        Validate(x.CrossingCoordinate, y, horizontal); Validate(secondaryX.CrossingCoordinate, y, horizontal);
        Validate(y.CrossingCoordinate, x, false); Validate(secondaryY.CrossingCoordinate, x, false);
    }

    private static void Validate(double? value, ChartAxis opposite, bool linearCategory) {
        if (!value.HasValue) return;
        // Horizontal category slots follow ChartMapper.ForHorizontalBars, including its time exception.
        if (linearCategory && opposite.Scale != ChartScaleKind.Time) return;
        if (opposite.Scale == ChartScaleKind.Logarithmic && !opposite.IsInLogarithmicDomain(value.Value))
            throw new InvalidOperationException("An axis crossing must belong to the opposite primary axis's " + opposite.LogarithmicDomainName + " logarithmic domain.");
        if (opposite.Scale == ChartScaleKind.Time && opposite.TimeSessions?.IsClosed(value.Value) == true)
            throw new InvalidOperationException("An axis crossing must not identify a closed interval of the opposite primary time axis.");
    }
}
