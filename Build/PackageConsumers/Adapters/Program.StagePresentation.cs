using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;

internal static partial class Program {
    private static void NativeStagePresentationAdapterContract() {
        var chart = Chart.Create().WithSize(640, 480).WithDataLabels().WithXLabels("Services", "Support")
            .AddPyramid("Allocation", new[] { new ChartPoint(1, 60), new ChartPoint(2, 40) })
            .ConfigurePyramid(options => { options.LabelContent = ChartStageLabelContent.Separate; options.PartitionOutline = new ChartStrokeStyle(width: 2, dash: new[] { 4d, 2d }); });
        var html = chart.ToInteractiveHtmlFragment(options => options.Interaction.Enable(ChartInteractionFeatures.Export));
        PackageAssertions.Require(PackageAssertions.Contains(html, "pyramid-stage") && PackageAssertions.Contains(html, "pyramid-category-label")
            && PackageAssertions.Contains(html, "pyramid-value-label") && PackageAssertions.Contains(html, "stroke-dasharray"),
            "Packed native stage adapter lost source captions or outline paint.");
    }
}
