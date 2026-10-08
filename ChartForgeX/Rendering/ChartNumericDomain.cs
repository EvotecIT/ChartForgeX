using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Rounds continuous, automatically bounded numeric domains before either renderer builds its mapper.</summary>
internal static class ChartNumericDomain {
    internal static void RoundX(Chart chart, ChartRange range) {
        var axis = chart.Options.XAxis;
        if (axis.Scale != ChartScaleKind.Linear || chart.Options.XAxisLabels.Count > 0) return;
        var ticks = ChartTicks.Generate(range.MinX, range.MaxX, axis.TickCount);
        range.SetXBounds(axis.Minimum ?? ticks[0], axis.Maximum ?? ticks[ticks.Count - 1]);
    }
}
