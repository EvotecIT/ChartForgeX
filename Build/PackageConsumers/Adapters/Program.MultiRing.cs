using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;

internal static partial class Program {
    private static void MultiRingCalloutAdapterContract() {
        var chart = Chart.Create().Size(700, 500).ShowHeader(false).Legend(false).DataLabels()
            .Pie(p => p.ShowCenterLabel(false)).XLabels("Alpha", "Beta").Pie(p => p.SliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent))
            .RadialBandAllocation(new ChartRadialBandAllocation(.2, .1));
        for (var index = 0; index < 2; index++) {
            var series = new ChartSeries(new ChartSeriesId("ring-" + index), "Period " + index, ChartSeriesKind.Donut,
                new[] { new ChartPoint(1, 2 + index), new ChartPoint(2, 1) }).PointSliceOffset(0, .12)
                .ObservationIds(new[] { new ChartObservationId("alpha"), new ChartObservationId("beta") });
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
