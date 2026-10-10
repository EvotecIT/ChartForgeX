using System;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void FinancialMarkOptionsRenderNativeStyles() {
        foreach (var kind in new[] { ChartSeriesKind.Candlestick, ChartSeriesKind.Ohlc }) {
            var chart = V2GalleryModels.Create(kind, "compact-options").WithSize(360, 360).WithLegend(true);
            var svg = chart.ToSvg();
            Assert(svg.Contains("data-cfx-role=\"legend-financial-", StringComparison.Ordinal), "Financial options need native financial legend glyphs.");
            Assert(svg.Contains(kind == ChartSeriesKind.Candlestick ? "data-cfx-role=\"candlestick-body\"" : "data-cfx-role=\"ohlc-stem\"", StringComparison.Ordinal),
                "Financial options must preserve the chart family and source observations.");
            Assert(chart.ToPng().Length > 64, "Configured compact financial marks must render native PNG.");
        }
    }
}
