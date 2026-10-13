using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static class CartesianCategoryExamples {
    internal static void Write(string output, ChartPngOutputScale pngOutputScale) {
        var peer = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(1.5) + 3);
        var observations = new[] { new ChartPoint(1.5, 2), new ChartPoint(3.5, 3) };
        var mixed = Chart.Create().WithTitle("Bars and interval observations")
            .WithSubtitle("Each mark family retains its category margin")
            .WithXAxis("Category").WithYAxis("Value").WithTheme(ChartTheme.ReportLight())
            .WithBarStyle(ChartBarStyle.Flat).WithDataLabels(false)
            .AddBar("Observed", observations)
            .AddRangeBar("Reference range", new[] { new ChartInterval(peer, 1, 4) });
        var stacked = Chart.Create().WithTitle("Horizontal category stacks")
            .WithSubtitle("Equivalent numeric categories share one slot")
            .WithXAxis("Value").WithYAxis("Category").WithTheme(ChartTheme.ReportLight())
            .WithBarStyle(ChartBarStyle.Flat).WithDataLabels(false).WithStackedBars()
            .AddHorizontalBar("First", observations)
            .AddHorizontalBar("Second", new[] { new ChartPoint(peer, 4), new ChartPoint(3.5, 5) });
        var grid = ChartGrid.Create().WithTitle("Automatic category margins")
            .WithSubtitle("Complete edge bars with mixed marks and equivalent source coordinates")
            .WithColumns(2).WithGap(20).WithPanelSize(520, 360)
            .Add(mixed).Add(stacked);
        ExampleArtifactWriter.SaveGrid(grid, output, "cartesian-automatic-category-margins", pngOutputScale);
    }
}
