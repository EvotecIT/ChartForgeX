using System;
using System.Globalization;
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

        var compactProgressRows = SegmentedMetricBlock.Create(SegmentedMetricStyle.ProgressRows)
            .WithTitle("Compact Progress")
            .WithSubtitle("Tight footer")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(420, 250)
            .WithAction("Open")
            .AddItem("One", 8, item => item.Segments = 12)
            .AddItem("Two", 7, item => item.Segments = 12)
            .AddItem("Three", 6, item => item.Segments = 12)
            .AddItem("Four", 5, item => item.Segments = 12)
            .AddItem("Five", 4, item => item.Segments = 12)
            .AddItem("Six", 3, item => item.Segments = 12);
        var compactProgressSvg = compactProgressRows.ToSvg("visual-block-compact-progress-rows");
        var renderedProgressRows = CountOccurrences(compactProgressSvg, "data-cfx-role=\"segmented-metric-progress-strip\"");
        var progressBottom = GetAttribute(compactProgressSvg, "data-cfx-role=\"segmented-metric-progress-rows\"", "data-cfx-bottom");
        var progressStripYs = ExtractAttributeValues(compactProgressSvg, "data-cfx-strip-y=\"");
        var progressStripHeights = ExtractAttributeValues(compactProgressSvg, "data-cfx-strip-height=\"");
        var lastStripY = double.Parse(progressStripYs[progressStripYs.Length - 1], CultureInfo.InvariantCulture);
        var lastStripHeight = double.Parse(progressStripHeights[progressStripHeights.Length - 1], CultureInfo.InvariantCulture);
        Assert(renderedProgressRows > 0 && renderedProgressRows < 6, "SegmentedMetricBlock compact progress rows should clip overflowing rows before footer actions.");
        Assert(lastStripY + lastStripHeight <= progressBottom + 1.1, "SegmentedMetricBlock compact progress rows should reserve the full strip height before drawing.");
        Assert(compactProgressRows.ToPng().Length > 64, "SegmentedMetricBlock compact progress rows should render PNG output.");

        var compactCapsuleLoop = SegmentedMetricBlock.Create(SegmentedMetricStyle.CapsuleLoop)
            .WithTitle("Compact Capsule")
            .WithSubtitle("Tight footer")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(360, 170)
            .WithAction("Open")
            .AddItem("First", 10)
            .AddItem("Second", 8)
            .AddItem("Third", 6);
        var compactCapsuleSvg = compactCapsuleLoop.ToSvg("visual-block-compact-capsule-loop");
        var capsuleBottom = GetAttribute(compactCapsuleSvg, "data-cfx-role=\"segmented-metric-capsule-loop\"", "data-cfx-bottom");
        var capsuleY = GetAttribute(compactCapsuleSvg, "data-cfx-role=\"segmented-metric-capsule-loop\"", "data-cfx-loop-y");
        var capsuleHeight = GetAttribute(compactCapsuleSvg, "data-cfx-role=\"segmented-metric-capsule-loop\"", "data-cfx-loop-height");
        Assert(capsuleY + capsuleHeight <= capsuleBottom + 1.1, "SegmentedMetricBlock compact capsule loops should clamp loop geometry before the footer.");
        Assert(compactCapsuleLoop.ToPng().Length > 64, "SegmentedMetricBlock compact capsule loops should render PNG output.");
    }
}
