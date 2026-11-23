using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SharedYAxisHandlesSecondaryOnlyCharts() {
        var secondaryOnlyA = Chart.Create().Size(300, 200).SecondaryYAxis(s => s.Title("Rate")).AddLine("Rate A", Points(82, 88, 94));
        var secondaryOnlyB = Chart.Create().Size(300, 200).SecondaryYAxis(s => s.Title("Rate")).AddLine("Rate B", Points(70, 76, 81));
        secondaryOnlyA.Series[0].SecondaryAxis();
        secondaryOnlyB.Series[0].SecondaryAxis();

        ChartGrid.Create().Add(secondaryOnlyA).Add(secondaryOnlyB).ShareYAxis();

        Assert(!secondaryOnlyA.Options.YAxis.Min.HasValue && !secondaryOnlyB.Options.YAxis.Min.HasValue, "Secondary-only shared y-axis grids should not apply fallback primary-axis bounds.");
        Assert(secondaryOnlyA.Options.SecondaryYAxis.Min == secondaryOnlyB.Options.SecondaryYAxis.Min && secondaryOnlyA.Options.SecondaryYAxis.Max == secondaryOnlyB.Options.SecondaryYAxis.Max, "Secondary-only shared y-axis grids should share secondary-axis bounds.");
        Assert(secondaryOnlyA.Options.SecondaryYAxis.Max >= 94, "Secondary-only shared y-axis grids should include secondary-series values instead of the primary fallback range.");
    }

    private static void SharedAxesIgnoreProgressBars() {
        var cartesian = Chart.Create()
            .Size(300, 200)
            .AddLine("Values", Points(1, 2, 3));
        var progress = Chart.Create()
            .Size(300, 200)
            .AddProgressBars("Completion", new[] {
                new ChartProgressItem("Build", 90),
                new ChartProgressItem("Test", 80)
            });

        ChartGrid.Create().Add(cartesian).Add(progress).ShareAxes();

        Assert(cartesian.Options.XAxis.Max < 10, "Shared x-axis grids should not include progress row indexes.");
        Assert(cartesian.Options.YAxis.Max < 10, "Shared y-axis grids should not include progress-bar values.");
        Assert(!progress.Options.XAxis.Max.HasValue && !progress.Options.YAxis.Max.HasValue, "Progress bars should not receive cartesian shared-axis bounds.");
    }
}
