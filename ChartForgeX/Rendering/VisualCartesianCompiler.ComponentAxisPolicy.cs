using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Only fixed category X may be temporal. Animated tuple members keep numeric native axes.</summary>
    private static bool ComponentValueAxesSupported(Chart chart) {
        var x = chart.Options.XAxis;
        if (!x.Minimum.HasValue || !x.Maximum.HasValue || x.PreparedViewportWindow != null) return false;
        if (x.Scale == ChartScaleKind.Time && chart.Series.Any(series => series.Kind == ChartSeriesKind.ErrorBar && series.ErrorBars.Any(item => item.XBounds.HasValue))) return false;
        foreach (var axis in chart.Series.Select(series => series.ValueAxis).Distinct()
            .Select(side => side == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis))
            if (!axis.Minimum.HasValue || !axis.Maximum.HasValue || axis.Scale == ChartScaleKind.Time) return false;
        return true;
    }
}
