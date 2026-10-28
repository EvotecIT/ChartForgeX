using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Markup;

public sealed partial class MarkupChartParser {
    /// <summary>Maps indexed Start/End pairs to the existing typed dumbbell source owner.</summary>
    private static ChartPoint[] BuildDumbbellPoints(IReadOnlyList<double> values) {
        if (values.Count % 2 != 0) throw new ArgumentException("Dumbbell chart values must alternate Start and End values for each category.");
        var points = new ChartPoint[values.Count];
        for (var index = 0; index < points.Length; index++) points[index] = new ChartPoint(index / 2 + 1, values[index]);
        return points;
    }

    private static void AddDumbbellMarkupSeries(Chart chart, string name, IEnumerable<ChartPoint> values, string? color) {
        var points = values.ToArray(); var pairs = new ChartDumbbell[points.Length / 2];
        for (var index = 0; index < pairs.Length; index++) pairs[index] = new ChartDumbbell(points[index * 2].X, points[index * 2].Y, points[index * 2 + 1].Y);
        chart.AddDumbbell(name, pairs, ParseColor(color));
    }

    private static bool TryParseComparisonOption(ChartState state, string key, string value) {
        switch (NormalizeKey(key)) {
            case "lollipoporientation": state.LollipopOrientation = VisualMarkupFenceOptions.ParseEnum<ChartOrientation>(value, key); return true;
            case "dumbbellorientation": state.DumbbellOrientation = VisualMarkupFenceOptions.ParseEnum<ChartOrientation>(value, key); return true;
            default: return false;
        }
    }

    private static void ApplyComparisonOptions(Chart chart, ChartState state) {
        if (state.LollipopOrientation.HasValue) chart.Options.LollipopOrientation = state.LollipopOrientation.Value;
        if (state.DumbbellOrientation.HasValue) chart.Options.DumbbellOrientation = state.DumbbellOrientation.Value;
    }
}
