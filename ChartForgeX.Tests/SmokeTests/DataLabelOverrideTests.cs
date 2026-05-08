using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TimelineAndGanttSeriesDataLabelOverridesAreHonored() {
        var timelineVisible = Chart.Create()
            .WithSize(640, 360)
            .AddTimelineRange("Visible", 1, 5)
            .AddTimelineRange("Hidden", 1, 4);
        timelineVisible.Series[0].WithDataLabels();
        var timelineVisibleSvg = timelineVisible.ToSvg();
        Assert(CountOccurrences(timelineVisibleSvg, "data-cfx-role=\"data-label\"") == 1, "Timeline series overrides should enable labels for one item without enabling every timeline row.");

        var timelineHidden = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .AddTimelineRange("Hidden", 1, 5);
        timelineHidden.Series[0].WithDataLabels(false);
        Assert(!timelineHidden.ToSvg().Contains("data-cfx-role=\"data-label\"", System.StringComparison.Ordinal), "Timeline series overrides should hide labels even when chart-level labels are enabled.");

        var ganttVisible = Chart.Create()
            .WithSize(640, 360)
            .AddGanttTask("Visible", 1, 5, 0.5);
        ganttVisible.Series[0].WithDataLabels();
        Assert(ganttVisible.ToSvg().Contains("data-cfx-role=\"gantt-progress-label\"", System.StringComparison.Ordinal), "Gantt series overrides should enable progress labels.");

        var ganttHidden = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .AddGanttTask("Hidden", 1, 5, 0.5);
        ganttHidden.Series[0].WithDataLabels(false);
        Assert(!ganttHidden.ToSvg().Contains("data-cfx-role=\"gantt-progress-label\"", System.StringComparison.Ordinal), "Gantt series overrides should hide progress labels even when chart-level labels are enabled.");
        Assert(timelineVisible.ToPng().Length > 64 && ganttVisible.ToPng().Length > 64, "Timeline and Gantt label overrides should render valid PNG output.");
    }

    private static void SpecializedSeriesDataLabelOverridesAreHonored() {
        var pie = Chart.Create().AddPie("Pie", Points(60, 40));
        pie.Series[0].WithDataLabels();
        Assert(CountOccurrences(pie.ToSvg(), "data-cfx-role=\"data-label\"") == 2, "Pie series overrides should enable slice labels without chart-level labels.");

        var polar = Chart.Create().WithDataLabels().AddPolarArea("Polar", Points(30, 50, 80));
        polar.Series[0].WithDataLabels(false);
        Assert(!polar.ToSvg().Contains("data-cfx-role=\"polar-area-label\"", System.StringComparison.Ordinal), "Polar-area series overrides should hide labels when chart-level labels are enabled.");

        var heatmap = Chart.Create().WithDataLabels().AddHeatmapRow("A", Points(10, 20)).AddHeatmapRow("B", Points(30, 40));
        heatmap.Series[1].WithDataLabels(false);
        Assert(CountOccurrences(heatmap.ToSvg(), "data-cfx-role=\"data-label\"") == 2, "Heatmap row overrides should hide labels for one row only.");

        var sankey = Chart.Create().AddSankey("Flow", new[] { new ChartSankeyLink("A", "B", 10) });
        sankey.Series[0].WithDataLabels();
        Assert(sankey.ToSvg().Contains("data-cfx-role=\"sankey-node-label\"", System.StringComparison.Ordinal), "Sankey series overrides should enable node labels without chart-level labels.");
        Assert(pie.ToPng().Length > 64 && polar.ToPng().Length > 64 && heatmap.ToPng().Length > 64 && sankey.ToPng().Length > 64, "Specialized label overrides should render valid PNG output.");
    }

    private static void IntrinsicSpecializedLabelsCanBeSuppressed() {
        var gauge = Chart.Create().AddGauge("Score", 87);
        gauge.Series[0].WithDataLabels(false);
        var gaugeSvg = gauge.ToSvg();
        Assert(!gaugeSvg.Contains("data-cfx-role=\"gauge-label\"", System.StringComparison.Ordinal), "Gauge series overrides should hide intrinsic value labels.");
        Assert(!gaugeSvg.Contains("data-cfx-role=\"gauge-status-label\"", System.StringComparison.Ordinal), "Gauge series overrides should hide intrinsic status labels.");
        Assert(gaugeSvg.Contains("data-cfx-role=\"gauge-min-label\"", System.StringComparison.Ordinal), "Gauge axis labels should remain visible by default.");
        Assert(!Chart.Create().WithAxes(false).AddGauge("Score", 87).ToSvg().Contains("data-cfx-role=\"gauge-min-label\"", System.StringComparison.Ordinal), "Gauge axis labels should honor axes visibility.");

        var circle = Chart.Create().AddCircle("Progress", 72);
        circle.Series[0].WithDataLabels(false);
        Assert(!circle.ToSvg().Contains("data-cfx-role=\"circle-label\"", System.StringComparison.Ordinal), "Circle series overrides should hide intrinsic labels.");

        var radial = Chart.Create().WithLegend(false).AddRadialBar("Coverage", Points(90, 75, 66));
        radial.Series[0].WithDataLabels(false);
        Assert(!radial.ToSvg().Contains("data-cfx-role=\"radial-bar-total\"", System.StringComparison.Ordinal), "Radial bar series overrides should hide center labels.");

        var funnel = Chart.Create().AddFunnel("Pipeline", Points(100, 74, 51));
        funnel.Series[0].WithDataLabels(false);
        var funnelSvg = funnel.ToSvg();
        Assert(!funnelSvg.Contains("data-cfx-role=\"funnel-label\"", System.StringComparison.Ordinal), "Funnel series overrides should hide segment labels.");
        Assert(!funnelSvg.Contains("data-cfx-role=\"funnel-retention\"", System.StringComparison.Ordinal), "Funnel series overrides should hide retention metrics.");

        var donut = Chart.Create().AddDonut("Checks", Points(70, 30));
        donut.Series[0].WithDataLabels(false);
        var donutSvg = donut.ToSvg();
        Assert(donutSvg.Contains("data-cfx-role=\"donut-slice\"", System.StringComparison.Ordinal), "Donut slices should still render when labels are suppressed.");
        Assert(!donutSvg.Contains("data-cfx-role=\"donut-total-label\"", System.StringComparison.Ordinal), "Donut series overrides should hide center totals.");
        Assert(!donutSvg.Contains("data-cfx-role=\"donut-title\"", System.StringComparison.Ordinal), "Donut series overrides should hide center titles.");
        Assert(gauge.ToPng().Length > 64 && circle.ToPng().Length > 64 && radial.ToPng().Length > 64 && funnel.ToPng().Length > 64 && donut.ToPng().Length > 64, "Suppressed intrinsic labels should still render valid PNG output.");
    }

    private static void HierarchyLabelsCanBeSuppressed() {
        var treemap = Chart.Create().AddTreemap("Findings", new[] {
            new ChartTreemapItem("Spoofing", 42),
            new ChartTreemapItem("Policy gaps", 28)
        });
        treemap.Series[0].WithDataLabels(false);
        var treemapSvg = treemap.ToSvg();
        Assert(treemapSvg.Contains("data-cfx-role=\"treemap-tile\"", System.StringComparison.Ordinal), "Treemap tiles should still render when labels are suppressed.");
        Assert(!treemapSvg.Contains("data-cfx-role=\"treemap-label\"", System.StringComparison.Ordinal), "Treemap series overrides should hide tile labels.");
        Assert(!treemapSvg.Contains("data-cfx-role=\"treemap-value\"", System.StringComparison.Ordinal), "Treemap series overrides should hide tile values.");

        var tree = Chart.Create().AddTree("Hierarchy", new[] {
            new ChartTreeLink("Root", "A"),
            new ChartTreeLink("Root", "B")
        });
        tree.Series[0].WithDataLabels(false);
        var treeSvg = tree.ToSvg();
        Assert(treeSvg.Contains("data-cfx-role=\"tree-node\"", System.StringComparison.Ordinal), "Tree nodes should still render when labels are suppressed.");
        Assert(!treeSvg.Contains("data-cfx-role=\"tree-node-label\"", System.StringComparison.Ordinal), "Tree series overrides should hide node labels.");
        Assert(treemap.ToPng().Length > 64 && tree.ToPng().Length > 64, "Suppressed hierarchy labels should still render valid PNG output.");
    }

    private static void BulletLabelsCanBeSuppressed() {
        var full = Chart.Create()
            .WithSize(560, 260)
            .AddBullet("Long control label", 82, 90);
        var compact = Chart.Create()
            .WithSize(560, 260)
            .AddBullet("Long control label", 82, 90);
        compact.Series[0].WithDataLabels(false);

        var fullSvg = full.ToSvg();
        var compactSvg = compact.ToSvg();
        Assert(compactSvg.Contains("data-cfx-role=\"bullet-value\"", System.StringComparison.Ordinal), "Bullet values should still render when labels are suppressed.");
        Assert(compactSvg.Contains("data-cfx-role=\"bullet-target\"", System.StringComparison.Ordinal), "Bullet targets should still render when labels are suppressed.");
        Assert(!compactSvg.Contains("data-cfx-role=\"bullet-row-label\"", System.StringComparison.Ordinal), "Bullet label overrides should hide row labels.");
        Assert(!compactSvg.Contains("data-cfx-role=\"bullet-target-label\"", System.StringComparison.Ordinal), "Bullet label overrides should hide target labels.");
        Assert(!compactSvg.Contains("data-cfx-role=\"bullet-value-label\"", System.StringComparison.Ordinal), "Bullet label overrides should hide value labels.");
        Assert(!compactSvg.Contains("data-cfx-role=\"bullet-status-marker\"", System.StringComparison.Ordinal), "Bullet label overrides should hide status markers.");
        Assert(GetAttribute(compactSvg, "data-cfx-role=\"bullet-value\"", "x") < GetAttribute(fullSvg, "data-cfx-role=\"bullet-value\"", "x"), "Label-free bullets should reclaim left label space.");
        Assert(compact.ToPng().Length > 64, "Suppressed bullet labels should still render valid PNG output.");
    }

    private static void HorizontalSeriesDataLabelOverridesReserveLayout() {
        var noLabelsSvg = Chart.Create()
            .WithSize(420, 260)
            .AddHorizontalBar("No labels", Points(98, 72))
            .ToSvg();
        var labels = Chart.Create()
            .WithSize(420, 260)
            .AddHorizontalBar("Labels", Points(98, 72));
        labels.Series[0].WithDataLabels();

        var labelsSvg = labels.ToSvg();
        Assert(CountOccurrences(labelsSvg, "data-cfx-role=\"data-label\"") == 2, "Horizontal bar series overrides should enable value labels.");
        Assert(GetAttribute(labelsSvg, "<clipPath", "width") < GetAttribute(noLabelsSvg, "<clipPath", "width"), "Horizontal bar label overrides should reserve right-side label space.");
        Assert(labels.ToPng().Length > 64, "Horizontal bar label override layout should render valid PNG output.");
    }

    private static void PointLabelOverridesReachSpecializedDataLabels() {
        var vertical = Chart.Create().WithDataLabels().AddBar("Vertical", Points(12, 24));
        vertical.Series[0].WithPointLabel(1, "Custom vertical");
        Assert(vertical.ToSvg().Contains(">Custom vertical</text>", System.StringComparison.Ordinal), "SVG vertical bar labels should honor point-label overrides.");

        var horizontal = Chart.Create().WithDataLabels().AddHorizontalBar("Horizontal", Points(12, 24));
        horizontal.Series[0].WithPointLabel(1, "Custom horizontal");
        Assert(horizontal.ToSvg().Contains(">Custom horizontal</text>", System.StringComparison.Ordinal), "SVG horizontal bar labels should honor point-label overrides.");

        var heatmap = Chart.Create().WithDataLabels().AddHeatmapRow("Heat", Points(12, 24));
        heatmap.Series[0].WithPointLabel(1, "Custom heat");
        Assert(heatmap.ToSvg().Contains(">Custom heat</text>", System.StringComparison.Ordinal), "SVG heatmap labels should honor point-label overrides.");

        var range = Chart.Create().WithDataLabels().AddRangeBar("Window", new[] { new ChartInterval(1, 10, 25) });
        range.Series[0].WithPointLabel(0, "Custom range");
        Assert(range.ToSvg().Contains(">Custom range</text>", System.StringComparison.Ordinal), "SVG range-bar labels should honor interval point-label overrides.");
        Assert(vertical.ToPng().Length > 64 && horizontal.ToPng().Length > 64 && heatmap.ToPng().Length > 64 && range.ToPng().Length > 64, "Point-label override chart variants should render valid PNG output.");
    }
}
