using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;

internal static partial class Program {
    private static void AlignedDifferenceAdapterContract() {
        var first = ChartSeries.CreateLine(new ChartSeriesId("first"), "Actual", new[] { new ChartPoint(1, 1), new ChartPoint(2, 100) });
        var second = ChartSeries.CreateLine(new ChartSeriesId("second"), "Forecast", new[] { new ChartPoint(1, 10), new ChartPoint(2, 2) });
        var chart = Chart.Create().AddAlignedDifferenceArea("Comparison", ChartDifferencePoints.Align(first, second));
        chart.Options.YAxis.WithLogarithmicScale();
        var html = chart.ToInteractiveHtmlFragment(options => options.Interaction.Enable(ChartInteractionFeatures.Export));
        PackageAssertions.Require(PackageAssertions.Contains(html, "data-cfx-first-source-series-id=\"first\"")
            && PackageAssertions.Contains(html, "data-cfx-second-source-series-id=\"second\"")
            && PackageAssertions.Contains(html, "difference-negative"), "Packed HTML adapter dropped aligned native source/crossing facts.");
    }
}
