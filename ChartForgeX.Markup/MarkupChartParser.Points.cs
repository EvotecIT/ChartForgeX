using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Markup;

public sealed partial class MarkupChartParser {
    private static ChartPoint[] BuildSeriesPoints(string type, IReadOnlyList<double> values) {
        if (NormalizeKey(type) != "polar") {
            var indexed = new ChartPoint[values.Count];
            for (var i = 0; i < indexed.Length; i++) indexed[i] = new ChartPoint(i + 1, values[i]);
            return indexed;
        }

        if (values.Count % 2 != 0) throw new ArgumentException("Polar chart values must alternate angle and radius coordinates.");
        var polar = new ChartPoint[values.Count / 2];
        for (var i = 0; i < polar.Length; i++) polar[i] = new ChartPoint(values[i * 2], values[i * 2 + 1]);
        return polar;
    }
}
