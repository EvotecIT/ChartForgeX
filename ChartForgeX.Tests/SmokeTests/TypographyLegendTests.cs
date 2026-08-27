using System;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void LegendCasingDrivesAllocationBeforeSerialization() {
        var chart = Chart.Create().WithSize(320, 240)
            .AddLine("iiii", Points(1, 2, 3))
            .AddLine("iiii", Points(2, 3, 4))
            .AddLine("iiii", Points(3, 4, 5));
        var preservedRows = CountOccurrences(chart.ToSvg(), "data-cfx-role=\"legend-row\"");

        chart.WithLegendStyle(style => style.WithTextCase(TextCaseTransform.Uppercase));
        var expandedSvg = chart.ToSvg();

        Assert(expandedSvg.Contains("data-cfx-label=\"IIII\"", StringComparison.Ordinal) && expandedSvg.Contains(">IIII</text>", StringComparison.Ordinal), "SVG legends should materialize casing before serialization.");
        Assert(CountOccurrences(expandedSvg, "data-cfx-role=\"legend-row\"") > preservedRows, "SVG legend allocation should measure case-expanded labels before deciding row wraps.");
    }
}
