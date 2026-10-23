using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SharedYAxisHandlesSecondaryOnlyCharts() {
        var secondaryOnlyA = Chart.Create().WithSize(300, 200).WithSecondaryYAxis("Rate").AddLine("Rate A", Points(82, 88, 94));
        var secondaryOnlyB = Chart.Create().WithSize(300, 200).WithSecondaryYAxis("Rate").AddLine("Rate B", Points(70, 76, 81));
        secondaryOnlyA.Series[0].UseSecondaryValueAxis();
        secondaryOnlyB.Series[0].UseSecondaryValueAxis();

        ChartGrid.Create().Add(secondaryOnlyA).Add(secondaryOnlyB).WithSharedYAxis();

        Assert(!secondaryOnlyA.Options.YAxisMinimum.HasValue && !secondaryOnlyB.Options.YAxisMinimum.HasValue, "Secondary-only shared y-axis grids should not apply fallback primary-axis bounds.");
        Assert(secondaryOnlyA.Options.SecondaryYAxisMinimum == secondaryOnlyB.Options.SecondaryYAxisMinimum && secondaryOnlyA.Options.SecondaryYAxisMaximum == secondaryOnlyB.Options.SecondaryYAxisMaximum, "Secondary-only shared y-axis grids should share secondary-axis bounds.");
        Assert(secondaryOnlyA.Options.SecondaryYAxisMaximum >= 94, "Secondary-only shared y-axis grids should include secondary-series values instead of the primary fallback range.");
    }

    private static void SharedAxesIgnoreProgressBars() {
        var cartesian = Chart.Create()
            .WithSize(300, 200)
            .AddLine("Values", Points(1, 2, 3));
        var progress = Chart.Create()
            .WithSize(300, 200)
            .AddProgressBars("Completion", new[] {
                new ChartProgressItem("Build", 90),
                new ChartProgressItem("Test", 80)
            });

        ChartGrid.Create().Add(cartesian).Add(progress).WithSharedAxes();

        Assert(cartesian.Options.XAxisMaximum < 10, "Shared x-axis grids should not include progress row indexes.");
        Assert(cartesian.Options.YAxisMaximum < 10, "Shared y-axis grids should not include progress-bar values.");
        Assert(!progress.Options.XAxisMaximum.HasValue && !progress.Options.YAxisMaximum.HasValue, "Progress bars should not receive cartesian shared-axis bounds.");
    }
}
