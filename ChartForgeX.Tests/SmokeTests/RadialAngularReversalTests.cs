using System;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void RadialAngularReversalExportsSupportedFamilies() {
        var points = new[] { new ChartPoint(1, 60), new ChartPoint(2, 80), new ChartPoint(3, 40) };
        var charts = new[] {
            (Chart.Create().AddRadarArea("Observed", points).AddRadarLine("Target", points), "radar-outline"),
            (Chart.Create().AddPolar("Observed", new[] { new ChartPoint(0, 60), new ChartPoint(.7, 80), new ChartPoint(2.8, 40) }), "polar-line"),
            (Chart.Create().AddPolarArea("Observed", points), "polar-area-segment"),
            (Chart.Create().AddRadialColumn("Observed", points).AddRadarLine("Target", points), "radial-column")
        };
        foreach (var (chart, role) in charts) {
            chart.ConfigureXAxis(axis => axis.WithReversal());
            Assert(chart.ToSvg().Contains("data-cfx-role=\"" + role + "\"", StringComparison.Ordinal), "Angular reversal should render its native family geometry.");
            Assert(chart.ToPng().Length > 64, "Angular reversal should export native PNG through the same prepared projection.");
        }
    }
}
