using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;

internal static partial class Program {
    private static void MultiRingCalloutAdapterContract() {
        var chart = Chart.Create().WithSize(700, 500).WithHeader(false).WithLegend(false).WithDataLabels()
            .WithDonutCenterLabel(false).WithXLabels("Alpha", "Beta").WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent)
            .WithAutomaticRadialBands(new ChartRadialBandAllocation(.2, .1));
        for (var index = 0; index < 2; index++) {
            var series = new ChartSeries(new ChartSeriesId("ring-" + index), "Period " + index, ChartSeriesKind.Donut,
                new[] { new ChartPoint(1, 2 + index), new ChartPoint(2, 1) }).WithPointSliceOffset(0, .12)
                .WithObservationIds(new[] { new ChartObservationId("alpha"), new ChartObservationId("beta") });
            series.DataLabelPlacement = ChartDataLabelPlacement.Outside; chart.AddSeries(series);
        }
        var html = chart.ToInteractiveHtmlFragment(options => options.Interaction.Enable(ChartInteractionFeatures.Export));
        PackageAssertions.Require(PackageAssertions.Contains(html, "data-cfx-radial-callout=\"overlay\"")
            && PackageAssertions.Contains(html, "data-cfx-slice-offset-ratio=\"0.12\"")
            && PackageAssertions.Contains(html, "data-cfx-series-key=\"ring-0\"") && PackageAssertions.Contains(html, "data-cfx-observation-id=\"alpha\""),
            "Packed multiring adapter lost native callouts, exact source offsets or ring/observation identity.");
        PackageAssertions.Require(PackageAssertions.Contains(html, "data-cfx-export=\"svg\"") && PackageAssertions.Contains(html, "data-cfx-export=\"png\""),
            "Packed multiring output lost the existing download surface.");
    }
}
