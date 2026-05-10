using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisualBlocksRenderTablesListsAndMetricCards() {
        var table = ChartTable.Create()
            .WithTitle("Drive summary")
            .WithSubtitle("Exact facts in a visual block, not a chart series")
            .WithTheme(ChartTheme.TransparentOverlayDark())
            .WithTransparentBackground()
            .AddColumn("Drive")
            .AddColumn("Used", VisualTextAlignment.Right, format: "0%")
            .AddColumn("Free", VisualTextAlignment.Right)
            .AddColumn("Status")
            .AddRow("C:", 0.72, "128 GB", "OK")
            .AddRow("D:", 0.91, "34 GB", "Warning")
            .WithStatusColumn("Status")
            .WithDenseMode();
        table.WithRow(1, row => row.Cells[3].Status = VisualStatus.Warning);
        table.WithRow(0, row => row.Cells[1].WithBadge("72%", VisualStatus.Positive, ChartColor.FromHex("#22C55E")));
        table.WithRow(0, row => row.Cells[2].WithSparkline(new[] { 96d, 118d, 128d }, color: ChartColor.FromHex("#38BDF8")));
        table.WithRow(1, row => row.Cells[2].WithMiniBars(new[] { 46d, 39d, 34d }, color: ChartColor.FromHex("#F97316")));

        var tableSvg = table.ToSvg("visual-block-table");
        Assert(tableSvg.Contains("<svg", StringComparison.Ordinal), "ChartTable should render SVG.");
        Assert(tableSvg.Contains("-visualCard", StringComparison.Ordinal) && tableSvg.Contains("class=\"cfx-guide-stroke\"", StringComparison.Ordinal), "Visual block SVG cards should use premium gradient surfaces and crisp strokes.");
        Assert(tableSvg.Contains("data-cfx-role=\"table-status\"", StringComparison.Ordinal), "ChartTable should render status markers.");
        Assert(tableSvg.Contains("data-cfx-role=\"table-cell-microvisual\"", StringComparison.Ordinal), "ChartTable should render cell microvisual groups.");
        Assert(tableSvg.Contains("data-cfx-role=\"table-cell-sparkline\"", StringComparison.Ordinal), "ChartTable should render cell sparklines.");
        Assert(tableSvg.Contains("data-cfx-role=\"table-cell-mini-bar\"", StringComparison.Ordinal), "ChartTable should render cell mini bars.");
        Assert(tableSvg.Contains("data-cfx-role=\"visual-badge\"", StringComparison.Ordinal), "ChartTable should render shared visual badges.");
        Assert(tableSvg.Contains("data-cfx-style=\"Soft\"", StringComparison.Ordinal), "ChartTable badges should expose style metadata.");
        Assert(!tableSvg.Contains("<script", StringComparison.OrdinalIgnoreCase), "Visual block SVG should stay script-free.");
        Assert(table.ToHtmlPage().Contains("chartforgex-visual-block", StringComparison.Ordinal) && table.ToHtmlPage().Contains("linear-gradient(180deg", StringComparison.Ordinal), "ChartTable should render a polished static HTML page.");
        Assert(table.ToPng().Length > 64, "ChartTable should render PNG output.");

        var list = ChartList.Create()
            .WithTitle("Security checks")
            .WithMarker(VisualListMarker.Status)
            .AddStatusItem("Disk encryption", VisualStatus.Positive, "ready")
            .AddStatusItem("Patch window", VisualStatus.Warning, "due")
            .AddStatusItem("Backup age", VisualStatus.Negative, "stale");
        var listSvg = list.ToSvg("visual-block-list");
        Assert(listSvg.Contains("data-cfx-role=\"list-marker\"", StringComparison.Ordinal), "ChartList should render list markers.");
        Assert(list.ToPng().Length > 64, "ChartList should render PNG output.");

        var checklist = ChartList.Create()
            .WithTitle("Release checklist")
            .AddCheckItem("Static HTML", true)
            .AddCheckItem("External dependencies", false);
        Assert(checklist.ToSvg("visual-block-checklist").Contains("data-cfx-role=\"list-check\"", StringComparison.Ordinal), "Checklist lists should render check paths without relying on font glyphs.");

        var metric = MetricCard.Create()
            .WithMetric("Coverage", 0.982, "P1")
            .WithIcon(VisualIcon.Lightning)
            .WithBadgePlacement(MetricCardBadgePlacement.TopLeft)
            .WithTrend("+2.4 pp")
            .WithCaption("since previous run")
            .WithAction("Open details", url: "#coverage")
            .WithStatus(VisualStatus.Positive)
            .AddDetail("Ready", "84%", VisualStatus.Positive)
            .AddDetail("Risk", "6%", VisualStatus.Warning)
            .WithMiniBars(new[] { 72d, 78d, 83d, 80d, 98d }, maximum: 100)
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(360, 190);
        var metricSvg = metric.ToSvg("visual-block-metric");
        Assert(metricSvg.Contains("data-cfx-role=\"metric-status-bar\"", StringComparison.Ordinal), "MetricCard should render status styling.");
        Assert(metricSvg.Contains("data-cfx-role=\"metric-detail\"", StringComparison.Ordinal), "MetricCard should render reusable supporting details.");
        Assert(metricSvg.Contains("data-cfx-role=\"visual-icon\"", StringComparison.Ordinal), "MetricCard should render reusable built-in icons.");
        Assert(metricSvg.Contains("data-cfx-placement=\"top-left\"", StringComparison.Ordinal), "MetricCard should render configurable badge placement.");
        Assert(metricSvg.Contains("data-cfx-role=\"metric-action-label\"", StringComparison.Ordinal), "MetricCard should render optional footer action text.");
        Assert(metricSvg.Contains("data-cfx-role=\"metric-action-symbol\"", StringComparison.Ordinal), "MetricCard should render optional footer action symbols.");
        Assert(metricSvg.Contains("data-cfx-role=\"metric-action-link\"", StringComparison.Ordinal) && metricSvg.Contains("href=\"#coverage\"", StringComparison.Ordinal), "MetricCard should render safe action links in SVG/HTML outputs.");
        Assert(metricSvg.Contains("data-cfx-role=\"metric-mini-bars\"", StringComparison.Ordinal), "MetricCard should render compact mini bar groups.");
        Assert(metricSvg.Contains("data-cfx-role=\"metric-mini-bar-highlight\"", StringComparison.Ordinal), "MetricCard should emphasize one mini bar.");
        Assert(metric.ToHtmlFragment().Contains("chartforgex-visual-block", StringComparison.Ordinal), "MetricCard should render an embeddable HTML fragment.");
        Assert(metric.ToPng().Length > 64, "MetricCard should render PNG output.");

        var sparkMetric = MetricCard.Create()
            .WithMetric("Network", "842 Mbps")
            .WithStatus(VisualStatus.Info)
            .WithMiniSparkline(new[] { 42d, 36d, 31d, 28d, 24d, 18d });
        var sparkSvg = sparkMetric.ToSvg("visual-block-metric-sparkline");
        Assert(sparkSvg.Contains("data-cfx-role=\"metric-mini-sparkline\"", StringComparison.Ordinal), "MetricCard should render compact sparkline groups.");
        Assert(sparkSvg.Contains("data-cfx-role=\"metric-mini-sparkline-current\"", StringComparison.Ordinal), "MetricCard sparklines should mark the current value.");
        Assert(sparkSvg.Contains("842 Mbps", StringComparison.Ordinal), "MetricCard should shrink long values enough to fit beside micro visuals.");
        Assert(sparkMetric.ToPng().Length > 64, "MetricCard sparkline should render PNG output.");

        var radialMetric = RadialMetricCard.Create()
            .WithMetric("Capacity left", "42%")
            .WithIcon(VisualIcon.Flame)
            .AddLayer("Track", 100, color: ChartColor.FromHex("#E2E8F0"), configure: layer => layer.WithGeometry(1, 0.16).WithLineCap(ChartRadialLayerCap.Butt))
            .AddLayer("Current", 42, color: ChartColor.FromHex("#F97316"), configure: layer => layer.WithGeometry(1, 0.12));
        var radialSvg = radialMetric.ToSvg("visual-block-radial-metric");
        Assert(radialSvg.Contains("data-cfx-role=\"radial-metric-layer\"", StringComparison.Ordinal), "RadialMetricCard should render public radial layers.");
        Assert(radialSvg.Contains("data-cfx-icon=\"flame\"", StringComparison.Ordinal), "RadialMetricCard should render reusable built-in icons.");
        Assert(radialMetric.ToPng().Length > 64, "RadialMetricCard should render PNG output.");

        var segmented = SegmentedProgressCard.Create()
            .WithTitle("Project Progress")
            .WithSubtitle("Reusable fixed-count progress rows")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(620, 250)
            .WithHeaderSymbol("%")
            .WithMenu()
            .AddRow("Performing Progress", 89, segments: 44, color: ChartColor.FromHex("#34C77B"), delta: "+10.2%", status: VisualStatus.Positive)
            .AddRow("Target Sales", 67, segments: 44, color: ChartColor.FromHex("#5EA2F6"), delta: "+2.2%", status: VisualStatus.Info)
            .WithAction("Up by 6% compared to last week")
            .WithActionStyle(ChartColor.FromHex("#DCFCE7"), ChartColor.FromHex("#16A34A"));
        var segmentedSvg = segmented.ToSvg("visual-block-segmented-progress");
        Assert(segmentedSvg.Contains("data-cfx-role=\"segmented-progress-card\"", StringComparison.Ordinal), "SegmentedProgressCard should render a public card role.");
        Assert(segmentedSvg.Contains("data-cfx-role=\"segmented-progress-header-badge\"", StringComparison.Ordinal), "SegmentedProgressCard should render optional header badges.");
        Assert(segmentedSvg.Contains("data-cfx-role=\"segmented-progress-menu-dot\"", StringComparison.Ordinal), "SegmentedProgressCard should render optional menu dots.");
        Assert(segmentedSvg.Contains("data-cfx-role=\"segmented-progress-strip\"", StringComparison.Ordinal), "SegmentedProgressCard should render segmented strips.");
        Assert(segmentedSvg.Contains("data-cfx-role=\"segmented-progress-segment-shadow\"", StringComparison.Ordinal), "SegmentedProgressCard should render dimensional segment shadows.");
        Assert(segmentedSvg.Contains("data-cfx-role=\"segmented-progress-segment-highlight\"", StringComparison.Ordinal), "SegmentedProgressCard should render dimensional segment highlights.");
        Assert(segmentedSvg.Contains("data-cfx-segments=\"44\"", StringComparison.Ordinal), "SegmentedProgressCard should preserve fixed segment counts in SVG metadata.");
        Assert(segmentedSvg.Contains("data-cfx-filled=\"39\"", StringComparison.Ordinal), "SegmentedProgressCard should derive filled segment counts from value and maximum.");
        Assert(segmentedSvg.Contains("data-cfx-role=\"segmented-progress-delta-pill\"", StringComparison.Ordinal), "SegmentedProgressCard should render delta pills.");
        Assert(segmentedSvg.Contains("data-cfx-role=\"segmented-progress-action-band\"", StringComparison.Ordinal), "SegmentedProgressCard should render optional action bands.");
        Assert(segmented.ToPng().Length > 64, "SegmentedProgressCard should render PNG output.");

        var composition = CompositionStatusCard.Create()
            .WithTitle("Overall Tasks")
            .WithSubtitle("Spread across 6 projects.")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(620, 360)
            .WithMetric("Tasks", 23, "Task")
            .AddSegment("On Going", 12, ChartColor.FromHex("#5EA2F6"), VisualStatus.Info, ChartFillPattern.DiagonalForward)
            .AddSegment("Under Review", 6, ChartColor.FromHex("#FFB05C"), VisualStatus.Warning)
            .AddSegment("Finish", 4, ChartColor.FromHex("#34C77B"), VisualStatus.Positive)
            .WithAction("View details task");
        var compositionSvg = composition.ToSvg("visual-block-composition-status");
        Assert(compositionSvg.Contains("data-cfx-role=\"composition-status-card\"", StringComparison.Ordinal), "CompositionStatusCard should render a public card role.");
        Assert(compositionSvg.Contains("data-cfx-role=\"composition-strip\"", StringComparison.Ordinal), "CompositionStatusCard should render a stacked composition strip.");
        Assert(compositionSvg.Contains("data-cfx-pattern=\"DiagonalForward\"", StringComparison.Ordinal), "CompositionStatusCard should preserve segment pattern hints in SVG metadata.");
        Assert(compositionSvg.Contains("data-cfx-role=\"composition-legend-swatch\"", StringComparison.Ordinal), "CompositionStatusCard should render legend swatches.");
        Assert(composition.ToHtmlFragment().Contains("chartforgex-visual-block", StringComparison.Ordinal), "CompositionStatusCard should render an embeddable HTML fragment.");
        Assert(composition.ToPng().Length > 64, "CompositionStatusCard should render PNG output.");

        var distribution = DistributionStripCard.Create()
            .WithTitle("Net Earning")
            .WithSubtitle("Currency split")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(620, 360)
            .WithMetric("Net earning", "EUR 56,980.00", "Last month")
            .AddSegment("Russian Ruble (RUB)", 9.74, ChartColor.FromHex("#FF3B13"), "RUB", "EUR 12.23")
            .AddSegment("Euro (EUR)", 38.48, ChartColor.FromHex("#1389F2"), "EUR", "EUR 20.23")
            .AddSegment("United States Dollar (USD)", 14.11, ChartColor.FromHex("#24D47B"), "USD", "EUR 12.00");
        var distributionSvg = distribution.ToSvg("visual-block-distribution-strip");
        Assert(distributionSvg.Contains("data-cfx-role=\"distribution-strip-card\"", StringComparison.Ordinal), "DistributionStripCard should render a public card role.");
        Assert(distributionSvg.Contains("data-cfx-role=\"distribution-segment\"", StringComparison.Ordinal), "DistributionStripCard should render stacked strip segments.");
        Assert(distributionSvg.Contains("data-cfx-role=\"distribution-legend-chip\"", StringComparison.Ordinal), "DistributionStripCard should render legend chips.");
        Assert(distributionSvg.Contains("data-cfx-role=\"distribution-row\"", StringComparison.Ordinal), "DistributionStripCard should render detail rows.");
        Assert(distributionSvg.Contains("data-cfx-role=\"distribution-ring\"", StringComparison.Ordinal), "DistributionStripCard should render row share rings.");
        Assert(distribution.ToHtmlFragment().Contains("chartforgex-visual-block", StringComparison.Ordinal), "DistributionStripCard should render an embeddable HTML fragment.");
        Assert(distribution.ToPng().Length > 64, "DistributionStripCard should render PNG output.");

        var heatmap = HeatmapInsightCard.Create()
            .WithTitle("Appointment Volume")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(620, 320)
            .WithControls("Day", "Week", "Week 1")
            .WithColumns("S", "M", "T")
            .WithColorKey(0, 12, ChartColor.FromHex("#D7F5F7"), ChartColor.FromHex("#08798C"))
            .AddRow("9 AM", 9, 3, 12)
            .AddRow("10 AM", 11, 2, 9)
            .AddInsight("Fri, 5 PM - 6 PM", "16 appointments");
        var heatmapSvg = heatmap.ToSvg("visual-block-heatmap-insight");
        Assert(heatmapSvg.Contains("data-cfx-role=\"heatmap-insight-card\"", StringComparison.Ordinal), "HeatmapInsightCard should render a public card role.");
        Assert(heatmapSvg.Contains("data-cfx-role=\"heatmap-insight-cell\"", StringComparison.Ordinal), "HeatmapInsightCard should render heatmap cells.");
        Assert(heatmapSvg.Contains("data-cfx-role=\"heatmap-insight-rail\"", StringComparison.Ordinal), "HeatmapInsightCard should render the insight rail.");
        Assert(heatmapSvg.Contains("data-cfx-role=\"heatmap-color-key\"", StringComparison.Ordinal), "HeatmapInsightCard should render the color key.");
        Assert(heatmapSvg.Contains("16 appointments", StringComparison.Ordinal), "HeatmapInsightCard should render insight details.");
        Assert(heatmap.ToHtmlFragment().Contains("chartforgex-visual-block", StringComparison.Ordinal), "HeatmapInsightCard should render an embeddable HTML fragment.");
        Assert(heatmap.ToPng().Length > 64, "HeatmapInsightCard should render PNG output.");

        var workload = WorkloadListBlock.Create()
            .WithTitle("Today Staff Workload")
            .WithSubtitle("Capacity share by staff member")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(620, 320)
            .AddPerson("Panji Dwi", "Zumba Trainer", 4, 8, VisualStatus.Neutral, "PD", "4/8", ChartColor.FromHex("#0E7490"))
            .AddPerson("Raihan Fikri", "Aerobik Trainer", 10, 8, VisualStatus.Negative, "RF", "10/8", ChartColor.FromHex("#DC2626"), "Overload")
            .AddPerson("Mufti Hidayat", "Massage Specialist", 6, 8, VisualStatus.Positive, selected: true)
            .WithSelectionControls();
        var workloadSvg = workload.ToSvg("visual-block-workload-list");
        Assert(workloadSvg.Contains("data-cfx-role=\"workload-list-block\"", StringComparison.Ordinal), "WorkloadListBlock should render a public block role.");
        Assert(workloadSvg.Contains("data-cfx-role=\"workload-avatar\"", StringComparison.Ordinal), "WorkloadListBlock should render avatar slots.");
        Assert(workloadSvg.Contains("data-cfx-role=\"workload-progress-rail\"", StringComparison.Ordinal), "WorkloadListBlock should render progress rails.");
        Assert(workloadSvg.Contains("data-cfx-role=\"workload-progress-fill\"", StringComparison.Ordinal), "WorkloadListBlock should render progress fills.");
        Assert(workloadSvg.Contains("data-cfx-ratio=\"1\"", StringComparison.Ordinal), "WorkloadListBlock should clamp overloaded progress ratios for renderers.");
        Assert(workloadSvg.Contains("data-cfx-role=\"workload-selection-control\"", StringComparison.Ordinal), "WorkloadListBlock should render optional selection controls.");
        Assert(workload.ToPng().Length > 64, "WorkloadListBlock should render PNG output.");

        var activity = ActivityTimelineBlock.Create()
            .WithTitle("Shipment Timeline")
            .WithSubtitle("Shipment status feed")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(620, 620)
            .WithEventSurfaces(false)
            .AddSection("In-progress")
            .AddEvent("Shipment", "Just now", VisualStatus.Info, "In-Progress", "Delivery by Royal Mail Standard", "S")
            .AddEvent("Shipment 1", status: VisualStatus.Neutral)
            .AddChecklistItem("Carrier confirmed", completed: true, muted: true)
            .AddChecklistItem("Packing in progress", completed: false)
            .AddHiddenSummary(6, "items hidden")
            .AddSection("Completed")
            .AddEvent("Order created", "Mar 10, 2026 10:20 am", VisualStatus.Positive);
        var activitySvg = activity.ToSvg("visual-block-activity-timeline");
        Assert(activitySvg.Contains("data-cfx-role=\"activity-timeline-block\"", StringComparison.Ordinal), "ActivityTimelineBlock should render a public block role.");
        Assert(activitySvg.Contains("data-cfx-event-surfaces=\"false\"", StringComparison.Ordinal), "ActivityTimelineBlock should expose compact event surface mode.");
        Assert(activitySvg.Contains(">S</text>", StringComparison.Ordinal), "ActivityTimelineBlock should render compact event symbols.");
        Assert(activitySvg.Contains("data-cfx-role=\"activity-spine\"", StringComparison.Ordinal), "ActivityTimelineBlock should render the connector spine.");
        Assert(activitySvg.Contains("data-cfx-role=\"activity-event-node\"", StringComparison.Ordinal), "ActivityTimelineBlock should render event nodes.");
        Assert(activitySvg.Contains("data-cfx-role=\"activity-check-node\"", StringComparison.Ordinal), "ActivityTimelineBlock should render nested checklist nodes.");
        Assert(activitySvg.Contains("data-cfx-role=\"activity-hidden-node\"", StringComparison.Ordinal), "ActivityTimelineBlock should render hidden summary nodes.");
        Assert(activity.ToHtmlFragment().Contains("chartforgex-visual-block", StringComparison.Ordinal), "ActivityTimelineBlock should render an embeddable HTML fragment.");
        Assert(activity.ToPng().Length > 64, "ActivityTimelineBlock should render PNG output.");

        var schedule = ScheduleTimelineBlock.Create()
            .WithTitle("Project Timeline")
            .WithSubtitle("Dense schedule lanes")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(760, 360)
            .WithTimeRange(8, 17, 1)
            .WithCurrentTime(14.2)
            .WithHeaderActions("12/Feb/2025", "Filter", "+ Add Schedule")
            .AddEvent("Meeting Brief Project", 8, 10, 0, ChartColor.FromHex("#5EA2F6"), VisualStatus.Info, avatars: new[] { "AM", "RF", "PD", "MR" })
            .AddEvent("Research Analyze Content", 9, 11, 1, ChartColor.FromHex("#8B5CF6"), VisualStatus.Info, avatars: new[] { "SC", "MR" })
            .AddEvent("Report Review", 16, 17.2, 0, ChartColor.FromHex("#5EA2F6"), VisualStatus.Info, badge: "Report", avatars: new[] { "MR", "SC" });
        var scheduleSvg = schedule.ToSvg("visual-block-schedule-timeline");
        Assert(scheduleSvg.Contains("data-cfx-role=\"schedule-timeline-block\"", StringComparison.Ordinal), "ScheduleTimelineBlock should render a public block role.");
        Assert(scheduleSvg.Contains("data-cfx-role=\"schedule-header-action\"", StringComparison.Ordinal), "ScheduleTimelineBlock should render optional header actions.");
        Assert(scheduleSvg.Contains("data-cfx-role=\"schedule-grid-line\"", StringComparison.Ordinal), "ScheduleTimelineBlock should render vertical time grid lines.");
        Assert(scheduleSvg.Contains("data-cfx-role=\"schedule-event-pill\"", StringComparison.Ordinal), "ScheduleTimelineBlock should render rounded event pills.");
        Assert(scheduleSvg.Contains("data-cfx-role=\"schedule-event-stripe\"", StringComparison.Ordinal), "ScheduleTimelineBlock should render event status stripes.");
        Assert(scheduleSvg.Contains("data-cfx-role=\"schedule-current-time\"", StringComparison.Ordinal), "ScheduleTimelineBlock should render current-time markers.");
        Assert(scheduleSvg.Contains("data-cfx-role=\"schedule-avatar-more\"", StringComparison.Ordinal), "ScheduleTimelineBlock should collapse overflowing avatar stacks.");
        Assert(scheduleSvg.Contains("data-cfx-clipped-end=\"true\"", StringComparison.Ordinal), "ScheduleTimelineBlock should expose clipped event metadata.");
        Assert(schedule.ToHtmlFragment().Contains("chartforgex-visual-block", StringComparison.Ordinal), "ScheduleTimelineBlock should render an embeddable HTML fragment.");
        Assert(schedule.ToPng().Length > 64, "ScheduleTimelineBlock should render PNG output.");
    }

    private static void VisualGridComposesChartsAndVisualBlocks() {
        var chart = Chart.Create()
            .WithTitle("Trend")
            .WithSize(420, 260)
            .AddSmoothLine("Warnings", Points(8, 7, 5, 4), ChartColor.FromHex("#F59E0B"));
        var table = ChartTable.Create()
            .WithTitle("Volumes")
            .WithColumns("Name", "Used", "Status")
            .AddRow("C:", "72%", "OK")
            .AddRow("D:", "91%", "Warning")
            .WithStatusColumn("Status");
        var metric = MetricCard.Create()
            .WithMetric("Open items", 12)
            .WithStatus(VisualStatus.Warning);

        var grid = VisualGrid.Create()
            .WithTitle("System snapshot")
            .WithSubtitle("Charts and exact-fact blocks share one export surface")
            .WithTheme(ChartTheme.ReportDark())
            .WithColumns(2)
            .WithPanelSize(420, 260)
            .WithFrame()
            .Add(chart)
            .Add(table)
            .Add(metric, columnSpan: 2);

        var svg = grid.ToSvg("visual-grid-smoke");
        Assert(svg.Contains("data-cfx-role=\"visual-grid-frame\"", StringComparison.Ordinal), "VisualGrid should render optional premium frames in SVG.");
        Assert(svg.Contains("-visualGridSurface", StringComparison.Ordinal) && svg.Contains("class=\"cfx-guide-stroke\"", StringComparison.Ordinal), "VisualGrid SVG should use polished scalable report surfaces and frame strokes.");
        Assert(svg.Contains("data-cfx-role=\"visual-grid-panel\"", StringComparison.Ordinal), "VisualGrid should mark child panels.");
        Assert(svg.Contains("data-cfx-role=\"metric-value\"", StringComparison.Ordinal), "VisualGrid should embed visual block SVG.");
        Assert(svg.Contains("Trend", StringComparison.Ordinal), "VisualGrid should embed chart SVG.");
        var gridHtml = grid.ToHtmlPage();
        Assert(gridHtml.Contains("chartforgex-visual-grid has-fixed-panels has-frame", StringComparison.Ordinal), "VisualGrid should render optional premium frames in static HTML pages.");
        Assert(gridHtml.Contains("linear-gradient(180deg", StringComparison.Ordinal) && gridHtml.Contains("overflow:visible", StringComparison.Ordinal), "VisualGrid HTML pages should use polished surfaces without clipping child shadows.");
        Assert(grid.ToPng().Length > 64, "VisualGrid should render PNG output.");

        var sparseGrid = VisualGrid.Create()
            .WithColumns(6)
            .Add(MetricCard.Create().WithMetric("One", 1))
            .Add(MetricCard.Create().WithMetric("Two", 2));
        Assert(sparseGrid.ToHtmlFragment().Contains("--cfx-visual-grid-columns:2", StringComparison.Ordinal), "VisualGrid HTML should clamp columns to populated item count like SVG/PNG layout.");

        var fixedGrid = VisualGrid.Create()
            .WithColumns(2)
            .WithPadding(32)
            .WithPanelSize(500, 320)
            .Add(chart, 1, 2)
            .Add(metric);
        var fixedHtml = fixedGrid.ToHtmlPage();
        Assert(fixedHtml.Contains("padding:var(--cfx-visual-grid-padding,24px)", StringComparison.Ordinal), "VisualGrid HTML should apply grid padding on the grid container where the custom property is scoped.");
        Assert(fixedHtml.Contains("chartforgex-visual-grid has-fixed-panels", StringComparison.Ordinal), "VisualGrid HTML should mark grids with fixed panel sizing.");
        Assert(fixedHtml.Contains(".chartforgex-visual-grid.has-fixed-panels .chartforgex-visual-grid-body{grid-template-columns:repeat(var(--cfx-visual-grid-columns),minmax(0,var(--cfx-visual-grid-panel-width)));justify-content:center}", StringComparison.Ordinal), "VisualGrid HTML should preserve configured fixed panel widths.");
        Assert(fixedHtml.Contains("min-height:0!important", StringComparison.Ordinal), "VisualGrid mobile CSS should reset inline row-span min-height.");
        Assert(fixedHtml.Contains("grid-auto-flow:row dense", StringComparison.Ordinal), "VisualGrid HTML should use dense placement like SVG/PNG layout.");
        Assert(fixedHtml.Contains(".chartforgex-visual-grid.has-fixed-panels .chartforgex-visual-grid-panel svg{width:100%;height:100%", StringComparison.Ordinal), "VisualGrid HTML contain mode should scale embedded SVGs to fixed panel bounds.");
        Assert(fixedHtml.Contains("min-height:calc((var(--cfx-visual-grid-panel-height) * 2) + (var(--cfx-visual-grid-gap) * 1))", StringComparison.Ordinal), "VisualGrid HTML should reserve physical height for row-spanning fixed panels.");

        var stretchGrid = VisualGrid.Create()
            .WithPanelSize(500, 320)
            .WithPanelFit(VisualGridPanelFit.Stretch)
            .Add(chart);
        var stretchSvg = stretchGrid.ToSvg("visual-grid-stretch");
        Assert(stretchSvg.Contains("data-cfx-role=\"visual-grid-panel\"", StringComparison.Ordinal) && stretchSvg.Contains("preserveAspectRatio=\"none\"", StringComparison.Ordinal), "VisualGrid SVG stretch mode should remove child aspect-ratio locking.");
        Assert(stretchGrid.ToHtmlFragment().Contains("preserveAspectRatio=\"none\"", StringComparison.Ordinal), "VisualGrid HTML stretch mode should remove embedded SVG aspect-ratio locking.");

        var strip = VisualGrid.CreateMetricStrip("Endpoint", new[] {
            MetricCard.Create().WithMetric("CPU", "38%").WithMiniSparkline(new[] { 48d, 42d, 38d }),
            MetricCard.Create().WithMetric("Memory", "71%").WithMiniBars(new[] { 55d, 63d, 71d }, maximum: 100)
        }, columns: 2);
        Assert(strip.ToSvg("visual-grid-metric-strip").Contains("Endpoint", StringComparison.Ordinal), "Metric strip preset should render the section title.");
        Assert(strip.ToHtmlFragment().Contains("--cfx-visual-grid-columns:2", StringComparison.Ordinal), "Metric strip preset should honor the requested column count.");

        var autoGrid = VisualGrid.Create()
            .WithColumns(2)
            .Add(MetricCard.Create().WithMetric("Small", 1).WithSize(260, 140))
            .Add(MetricCard.Create().WithMetric("Large", 2).WithSize(420, 260));
        var autoHtml = autoGrid.ToHtmlPage();
        Assert(autoHtml.Contains("--cfx-visual-grid-panel-width:420px", StringComparison.Ordinal), "Auto-sized VisualGrid HTML should emit the computed max panel width used by SVG/PNG layout.");
        Assert(autoHtml.Contains("--cfx-visual-grid-panel-height:260px", StringComparison.Ordinal), "Auto-sized VisualGrid HTML should emit the computed max panel height used by SVG/PNG layout.");
        Assert(autoHtml.Contains(".chartforgex-visual-grid-panel svg{width:auto;height:auto", StringComparison.Ordinal), "Auto-sized VisualGrid HTML should preserve child SVG intrinsic sizes.");
        Assert(!autoHtml.Contains("chartforgex-visual-grid has-fixed-panels", StringComparison.Ordinal), "Auto-sized VisualGrid HTML should not force fixed-panel child scaling.");
    }

    private static void VisualBlocksRejectInvalidInputsCloseToCaller() {
        AssertThrows<InvalidOperationException>(() => ChartTable.Create().AddRow("orphan"), "ChartTable should require columns before rows.");
        AssertThrows<ArgumentException>(() => ChartTable.Create().WithColumns(), "ChartTable should reject empty column sets.");
        AssertThrows<ArgumentException>(() => ChartTable.Create().WithColumns("A").AddRow("a", "b"), "ChartTable should reject row values that do not match columns.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartTable.Create().AddColumn("Bad", (VisualTextAlignment)999), "ChartTable columns should reject unknown alignments.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTableColumn("Bad", width: -1), "ChartTableColumn should reject invalid widths even when constructed directly.");
        AssertThrows<InvalidOperationException>(() => ChartTable.Create().WithColumns("A").AddRow("a").AddColumn("B"), "ChartTable should reject adding columns after rows exist.");
        AssertThrows<ArgumentNullException>(() => new ChartTableCell("ok").Text = null!, "ChartTable cells should reject null text through the public setter.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTableCell("bad").Alignment = (VisualTextAlignment)999, "ChartTable cells should reject unknown alignment overrides.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTableCell("bad").Status = (VisualStatus)999, "ChartTable cells should reject unknown status values.");
        AssertThrows<ArgumentException>(() => new ChartTableCell("bad").WithMiniBars(Array.Empty<double>()), "ChartTable cell mini bars should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTableCell("bad").WithMiniBars(new[] { double.NaN }), "ChartTable cell mini bars should reject non-finite values.");
        AssertThrows<InvalidOperationException>(() => ChartTable.Create().WithColumns("A").AddRow("a").WithRow(0, row => row.Cells[0].WithSparkline(new[] { 1d })).ToSvg(), "ChartTable cell sparklines should require at least two values.");
        AssertThrows<InvalidOperationException>(() => ChartTable.Create().WithColumns("A").AddRow("a").WithRow(0, row => row.Cells[0].WithMiniBars(new[] { 1d }, minimum: 2, maximum: 1)).ToSvg(), "ChartTable cell microvisual bounds should require maximum greater than minimum.");
        var excessiveMicroVisualValues = new double[513];
        for (var i = 0; i < excessiveMicroVisualValues.Length; i++) excessiveMicroVisualValues[i] = i;
        AssertThrows<InvalidOperationException>(() => ChartTable.Create().WithColumns("A").AddRow("a").WithRow(0, row => row.Cells[0].WithMiniBars(excessiveMicroVisualValues)).ToSvg(), "ChartTable cell microvisuals should reject excessive point counts.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTableCell("bad").MicroVisualKind = (ChartTableCellMicroVisualKind)999, "ChartTable cells should reject unknown microvisual kinds.");
        AssertThrows<InvalidOperationException>(() => ChartTable.Create().WithColumns("A").AddRow("a").WithRow(0, row => row.Cells[0].WithBadge(new string('x', 25))).ToSvg(), "ChartTable cell badges should stay compact.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTableCell("bad").BadgeStyle = (VisualBadgeStyle)999, "ChartTable cells should reject unknown badge styles.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartList.Create().Marker = (VisualListMarker)999, "ChartList marker property should reject unknown marker values.");
        AssertThrows<ArgumentNullException>(() => new ChartListItem("ok").Text = null!, "ChartList items should reject null text through the public setter.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartListItem("bad").Status = (VisualStatus)999, "ChartList items should reject unknown status values.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().Status = (VisualStatus)999, "MetricCard should reject unknown status values.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().Icon = (VisualIcon)999, "MetricCard should reject unknown icon values.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().BadgePlacement = (MetricCardBadgePlacement)999, "MetricCard should reject unknown badge placements.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().WithMetric("Bad", 1).WithAction(new string('x', 49)).ToSvg(), "MetricCard action labels should stay compact.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().WithMetric("Bad", 1).WithAction("Open", "longer").ToSvg(), "MetricCard action symbols should stay compact.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().WithMetric("Bad", 1).WithAction("Open", url: "javascript:alert(1)").ToSvg(), "MetricCard action URLs should reject scriptable URLs.");
        AssertThrows<ArgumentException>(() => MetricCard.Create().WithMiniBars(Array.Empty<double>()), "MetricCard mini bars should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().WithMiniBars(new[] { double.NaN }), "MetricCard mini bars should reject non-finite values.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().WithMetric("Bad", 1).WithMiniBars(new[] { 1d }, minimum: 2, maximum: 1).ToSvg(), "MetricCard mini bars should require maximum greater than minimum.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().WithMetric("Bad", 1).WithMiniBars(new[] { 1d }, highlightIndex: 2).ToSvg(), "MetricCard mini bar highlight index should reference an existing value.");
        AssertThrows<ArgumentException>(() => MetricCard.Create().WithMiniSparkline(new[] { 1d }), "MetricCard mini sparklines should require at least two values.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().WithMiniSparkline(new[] { 1d, double.PositiveInfinity }), "MetricCard mini sparklines should reject non-finite values.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().WithMetric("Bad", 1).WithMiniSparkline(new[] { 1d, 2d }, minimum: 3, maximum: 2).ToSvg(), "MetricCard mini sparklines should require maximum greater than minimum.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().WithMetric("Missing", null).ToSvg(), "MetricCard should require a visible value.");
        AssertThrows<InvalidOperationException>(() => RadialMetricCard.Create().WithMetric("Missing", "1").ToSvg(), "RadialMetricCard should require at least one layer.");
        AssertThrows<InvalidOperationException>(() => RadialMetricCard.Create().AddLayer("Bad", 1, configure: _ => null!).ToSvg(), "RadialMetricCard should reject null fluent layer configuration results.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddLayeredRadial("Bad", _ => null!), "Layered radial builder should reject null fluent layer configuration results.");
        AssertThrows<ArgumentOutOfRangeException>(() => RadialMetricCard.Create().Icon = (VisualIcon)999, "RadialMetricCard should reject unknown icon values.");
        AssertThrows<InvalidOperationException>(() => SegmentedProgressCard.Create().ToSvg(), "SegmentedProgressCard should require at least one row.");
        AssertThrows<InvalidOperationException>(() => SegmentedProgressCard.Create().AddRow("Bad", -1).ToSvg(), "SegmentedProgressCard should reject negative values.");
        AssertThrows<InvalidOperationException>(() => SegmentedProgressCard.Create().AddRow("Bad", 1, maximum: 0).ToSvg(), "SegmentedProgressCard should require a positive maximum.");
        AssertThrows<InvalidOperationException>(() => SegmentedProgressCard.Create().AddRow("Bad", 1, segments: 0).ToSvg(), "SegmentedProgressCard should reject invalid segment counts.");
        AssertThrows<InvalidOperationException>(() => SegmentedProgressCard.Create().AddRow("Bad", 1).WithHeaderSymbol("longer").ToSvg(), "SegmentedProgressCard header symbols should stay compact.");
        AssertThrows<InvalidOperationException>(() => SegmentedProgressCard.Create().AddRow("Bad", 1).WithAction(new string('x', 49)).ToSvg(), "SegmentedProgressCard action labels should stay compact.");
        AssertThrows<InvalidOperationException>(() => SegmentedProgressCard.Create().AddRow("Bad", 1).WithAction("Open", url: "javascript:alert(1)").ToSvg(), "SegmentedProgressCard action URLs should reject scriptable URLs.");
        AssertThrows<InvalidOperationException>(() => CompositionStatusCard.Create().ToSvg(), "CompositionStatusCard should require metric text and segments.");
        AssertThrows<InvalidOperationException>(() => CompositionStatusCard.Create().WithMetric("Tasks", 0).ToSvg(), "CompositionStatusCard should require at least one segment.");
        AssertThrows<InvalidOperationException>(() => CompositionStatusCard.Create().WithMetric("Tasks", 0).AddSegment("Bad", -1).ToSvg(), "CompositionStatusCard should reject negative segment values.");
        AssertThrows<InvalidOperationException>(() => CompositionStatusCard.Create().WithMetric("Tasks", 0).AddSegment("Zero", 0).ToSvg(), "CompositionStatusCard should require at least one positive segment value.");
        AssertThrows<ArgumentOutOfRangeException>(() => new CompositionStatusSegment("Bad", 1).Pattern = (ChartFillPattern)999, "CompositionStatusSegment should reject unknown fill patterns.");
        AssertThrows<InvalidOperationException>(() => DistributionStripCard.Create().ToSvg(), "DistributionStripCard should require metric text and segments.");
        AssertThrows<InvalidOperationException>(() => DistributionStripCard.Create().WithMetric("Net", 0).ToSvg(), "DistributionStripCard should require at least one segment.");
        AssertThrows<InvalidOperationException>(() => DistributionStripCard.Create().WithMetric("Net", 0).AddSegment("Bad", -1).ToSvg(), "DistributionStripCard should reject negative segment values.");
        AssertThrows<InvalidOperationException>(() => DistributionStripCard.Create().WithMetric("Net", 0).AddSegment("Zero", 0).ToSvg(), "DistributionStripCard should require at least one positive segment value.");
        AssertThrows<InvalidOperationException>(() => DistributionStripCard.Create().WithMetric("Net", 0).AddSegment("Symbol", 1, symbol: new string('x', 9)).ToSvg(), "DistributionStripCard should keep row symbols compact.");
        AssertThrows<InvalidOperationException>(() => HeatmapInsightCard.Create().ToSvg(), "HeatmapInsightCard should require columns and rows.");
        AssertThrows<InvalidOperationException>(() => HeatmapInsightCard.Create().WithColumns("A", "B").AddRow("Bad", 1).ToSvg(), "HeatmapInsightCard rows should match the column count.");
        AssertThrows<InvalidOperationException>(() => HeatmapInsightCard.Create().WithColumns("A").WithColorKey(2, 1).AddRow("Bad", 1).ToSvg(), "HeatmapInsightCard should require a valid color key range.");
        AssertThrows<InvalidOperationException>(() => WorkloadListBlock.Create().ToSvg(), "WorkloadListBlock should require rows.");
        AssertThrows<InvalidOperationException>(() => WorkloadListBlock.Create().AddPerson("", "Role", 1).ToSvg(), "WorkloadListBlock should require row labels.");
        AssertThrows<InvalidOperationException>(() => WorkloadListBlock.Create().AddPerson("Bad", "Role", -1).ToSvg(), "WorkloadListBlock should reject negative values.");
        AssertThrows<InvalidOperationException>(() => WorkloadListBlock.Create().AddPerson("Bad", "Role", 1, 0).ToSvg(), "WorkloadListBlock should reject non-positive maximum values.");
        AssertThrows<InvalidOperationException>(() => ActivityTimelineBlock.Create().ToSvg(), "ActivityTimelineBlock should require items.");
        AssertThrows<InvalidOperationException>(() => ActivityTimelineBlock.Create().AddEvent("").ToSvg(), "ActivityTimelineBlock should require item text.");
        AssertThrows<InvalidOperationException>(() => ActivityTimelineBlock.Create().AddHiddenSummary(-1, "items").ToSvg(), "ActivityTimelineBlock should reject negative hidden counts.");
        AssertThrows<InvalidOperationException>(() => ActivityTimelineBlock.Create().AddEvent("Bad", symbol: "TOOLONG").ToSvg(), "ActivityTimelineBlock item symbols should stay compact.");
        AssertThrows<ArgumentOutOfRangeException>(() => ActivityTimelineItem.Event("Bad", null, VisualStatus.Neutral, null, null).Kind = (ActivityTimelineItemKind)999, "ActivityTimelineItem kind property should reject unknown kinds.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().ToSvg(), "ScheduleTimelineBlock should require events.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().WithTimeRange(9, 8).AddEvent("Bad", 8, 9).ToSvg(), "ScheduleTimelineBlock should reject inverted ranges.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().WithTimeRange(8, 17, 0).AddEvent("Bad", 8, 9).ToSvg(), "ScheduleTimelineBlock should reject non-positive tick intervals.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().WithTimeRange(0, 511.8, 1).AddEvent("Bad", 0, 1).ToSvg(), "ScheduleTimelineBlock should validate the rendered tick count bound.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().AddEvent("", 8, 9).ToSvg(), "ScheduleTimelineBlock should require event titles.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().AddEvent("Bad", 9, 8).ToSvg(), "ScheduleTimelineBlock should reject inverted events.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().AddEvent("Bad", 8, 9, lane: -1).ToSvg(), "ScheduleTimelineBlock should reject negative lanes.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().WithHeaderActions(new string('x', 25)).AddEvent("Bad", 8, 9).ToSvg(), "ScheduleTimelineBlock header actions should stay compact.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ScheduleTimelineEvent("Bad", 8, 9) { Status = (VisualStatus)999 }, "ScheduleTimelineEvent should reject unknown status values.");
        AssertThrows<ArgumentOutOfRangeException>(() => VisualGrid.Create().WithColumns(0), "VisualGrid should reject non-positive column counts.");
        AssertThrows<ArgumentOutOfRangeException>(() => VisualGrid.Create().PanelFit = (VisualGridPanelFit)999, "VisualGrid panel fit property should reject unknown values.");
        AssertThrows<InvalidOperationException>(() => VisualGrid.Create().ToSvg(), "VisualGrid should require at least one item.");
        AssertThrows<ArgumentException>(() => VisualGrid.CreateMetricStrip("Empty", Array.Empty<MetricCard>()), "Metric strip preset should require at least one card.");
        AssertThrows<ArgumentException>(() => VisualGrid.CreateMetricStrip("Bad", new MetricCard[] { null! }), "Metric strip preset should reject null cards.");
    }
}
