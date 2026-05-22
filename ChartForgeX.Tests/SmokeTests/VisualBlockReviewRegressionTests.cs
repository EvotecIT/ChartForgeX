using System;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisualBlockReviewRegressionsStayCovered() {
        var zeroFunnel = SegmentedMetricBlock.Create(SegmentedMetricStyle.FunnelColumns)
            .WithTitle("Zero Funnel")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(360, 180)
            .AddItem("Reached", 0, segments: 4)
            .AddItem("Converted", 0, segments: 3);
        var zeroFunnelSvg = zeroFunnel.ToSvg("visual-block-segmented-zero-funnel");
        Assert(zeroFunnelSvg.Contains("data-cfx-role=\"segmented-metric-funnel-stage\"", StringComparison.Ordinal), "SegmentedMetricBlock funnel columns should render zero-total stage layouts from explicit segment counts.");
        Assert(CountOccurrences(zeroFunnelSvg, "data-cfx-role=\"segmented-metric-funnel-bar\"") == 7, "SegmentedMetricBlock zero-total funnel columns should preserve caller supplied segment counts.");
        Assert(zeroFunnel.ToPng().Length > 64, "SegmentedMetricBlock zero-total funnel columns should render PNG output.");

        var zeroSliceCapsule = SegmentedMetricBlock.Create(SegmentedMetricStyle.CapsuleLoop).WithTheme(ChartTheme.ReportLight()).WithSize(420, 220).AddItem("Used", 10).AddItem("Empty", 0);
        var zeroSliceCapsuleSvg = zeroSliceCapsule.ToSvg("visual-block-segmented-zero-slice-capsule");
        Assert(!zeroSliceCapsuleSvg.Contains("data-cfx-label=\"Empty\"", StringComparison.Ordinal), "SegmentedMetricBlock capsule loops should not render visible segments for zero-share items.");
        Assert(zeroSliceCapsule.ToPng().Length > 64, "SegmentedMetricBlock capsule loops with zero-share items should still render PNG output.");

        var compactCompositionLegend = SegmentedMetricBlock.Create(SegmentedMetricStyle.CompositionStrip)
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(360, 190)
            .WithMetric("Tiny", 12)
            .WithAction("Open")
            .AddItem("One", 1)
            .AddItem("Two", 1)
            .AddItem("Three", 1)
            .AddItem("Four", 1)
            .AddItem("Five", 1)
            .AddItem("Six", 1)
            .AddItem("Seven", 1)
            .AddItem("Eight", 1)
            .AddItem("Nine", 1)
            .AddItem("Ten", 1)
            .AddItem("Eleven", 1)
            .AddItem("Twelve", 1);
        var compactCompositionSvg = compactCompositionLegend.ToSvg("visual-block-composition-compact-legend");
        var compactLegendRows = CountOccurrences(compactCompositionSvg, "data-cfx-role=\"segmented-metric-legend-swatch\"");
        var compactLegendY = GetAttribute(compactCompositionSvg, "data-cfx-role=\"segmented-metric-composition\"", "data-cfx-legend-y");
        var compactLegendRowHeight = GetAttribute(compactCompositionSvg, "data-cfx-role=\"segmented-metric-composition\"", "data-cfx-row-height");
        var compactLegendBottom = GetAttribute(compactCompositionSvg, "data-cfx-role=\"segmented-metric-composition\"", "data-cfx-legend-bottom");
        Assert(compactLegendRows < 12, "SegmentedMetricBlock compact composition legends should clip overflowing rows before the footer.");
        Assert(compactLegendY + compactLegendRows * compactLegendRowHeight <= compactLegendBottom + 1.1, "SegmentedMetricBlock compact composition legends should reserve the full row height before drawing.");
        Assert(compactCompositionLegend.ToPng().Length > 64, "SegmentedMetricBlock compact composition legends should render PNG output.");
    }
}
