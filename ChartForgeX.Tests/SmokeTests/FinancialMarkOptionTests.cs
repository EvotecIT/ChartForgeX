using System;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void FinancialMarkOptionsRenderNativeStyles() {
        foreach (var kind in new[] { ChartSeriesKind.Candlestick, ChartSeriesKind.Ohlc }) foreach (var variant in new[] { "compact-options", "compact-dashed" }) {
            var chart = V2GalleryModels.Create(kind, variant).WithSize(360, 360).WithLegend(true);
            var svg = chart.ToSvg();
            Assert(svg.Contains("data-cfx-role=\"legend-financial-", StringComparison.Ordinal), "Financial options need native financial legend glyphs.");
            Assert(svg.Contains(kind == ChartSeriesKind.Candlestick ? "data-cfx-role=\"candlestick-body\"" : "data-cfx-role=\"ohlc-stem\"", StringComparison.Ordinal),
                "Financial options must preserve the chart family and source observations.");
            Assert(chart.ToPng().Length > 64, "Configured compact financial marks must render native PNG.");
            if (variant == "compact-dashed") Assert(svg.Contains("stroke-dasharray=\"7 5 2\"", StringComparison.Ordinal)
                && svg.Contains("stroke-dashoffset=\"-3\"", StringComparison.Ordinal), "Financial dashes must preserve the authored pattern and phase.");
        }
    }
}
