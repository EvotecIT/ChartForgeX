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

        var symbolMetric = MetricCard.Create()
            .WithMetric("Patch Rate", "94%", "OK")
            .WithSymbol("WRN")
            .WithBadgePlacement(MetricCardBadgePlacement.TopLeft)
            .WithStatus(VisualStatus.Warning)
            .WithTheme(ChartTheme.TransparentOverlayDark())
            .WithSize(300, 170);
        var symbolMetricSvg = symbolMetric.ToSvg("visual-block-metric-symbol");
        Assert(symbolMetricSvg.Contains("data-cfx-role=\"metric-symbol\"", StringComparison.Ordinal), "MetricCard should render text symbols when no icon is configured.");
        Assert(symbolMetricSvg.Contains("dominant-baseline=\"central\"", StringComparison.Ordinal), "MetricCard symbols should be vertically centered inside their badge.");
        Assert(symbolMetric.ToPng().Length > 64, "MetricCard symbol badges should render in PNG output.");

        var valueSurfaceMetric = MetricCard.Create()
            .WithMetric("Litres of water", "4.5", unit: "Litres")
            .WithIcon(VisualIcon.Droplet)
            .WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset)
            .WithSize(300, 140);
        var valueSurfaceSvg = valueSurfaceMetric.ToSvg("visual-block-metric-value-surface");
        Assert(valueSurfaceSvg.Contains("data-cfx-role=\"metric-value-surface\"", StringComparison.Ordinal), "MetricCard should render compact metric value surfaces.");
        Assert(valueSurfaceSvg.Contains("data-cfx-role=\"metric-value-surface-badge\"", StringComparison.Ordinal), "MetricCard value surfaces should keep icons inside the value tray.");
        Assert(valueSurfaceSvg.Contains("data-cfx-role=\"metric-unit\"", StringComparison.Ordinal), "MetricCard should render optional metric units separately from emphasized values.");
        Assert(valueSurfaceMetric.ToPng().Length > 64, "MetricCard compact value surfaces should render PNG output.");

        var narrowValueSurfaceMetric = MetricCard.Create()
            .WithMetric("Energy", "123456789", unit: "kilocalories-per-day")
            .WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset)
            .WithSize(210, 140);
        var narrowValueSurfaceSvg = narrowValueSurfaceMetric.ToSvg("visual-block-metric-narrow-value-surface");
        Assert(narrowValueSurfaceSvg.Contains("data-cfx-role=\"metric-unit\"", StringComparison.Ordinal), "MetricCard narrow value surfaces should still render unit text.");
        Assert(!narrowValueSurfaceSvg.Contains("kilocalories-per-day", StringComparison.Ordinal) && narrowValueSurfaceSvg.Contains("...", StringComparison.Ordinal), "MetricCard value surfaces should fit unit text to the remaining inset width.");
        Assert(narrowValueSurfaceMetric.ToPng().Length > 64, "MetricCard narrow value surfaces should render PNG output.");

        var compactValueSurfaceMetric = MetricCard.Create()
            .WithMetric("Compact", "123", unit: "kilocalories-per-day")
            .WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset)
            .WithSize(210, 90);
        var compactValueSurfaceSvg = compactValueSurfaceMetric.ToSvg("visual-block-metric-compact-value-surface");
        var compactValueSurface = System.Text.RegularExpressions.Regex.Match(compactValueSurfaceSvg, "data-cfx-role=\"metric-value-surface\"[^>]*y=\"([^\"]+)\"[^>]*height=\"([^\"]+)\"");
        Assert(compactValueSurface.Success, "MetricCard compact inset values should render a measurable value surface.");
        var compactValueSurfaceY = double.Parse(compactValueSurface.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        var compactValueSurfaceHeight = double.Parse(compactValueSurface.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert(compactValueSurfaceY + compactValueSurfaceHeight <= 90, "MetricCard compact inset value surfaces should stay within the card bounds.");
        Assert(compactValueSurfaceMetric.ToPng().Length > 64, "MetricCard compact inset values should render PNG output.");

        var sparkMetric = MetricCard.Create()
            .WithMetric("Network", "842 Mbps")
            .WithStatus(VisualStatus.Info)
            .WithMiniSparkline(new[] { 42d, 36d, 31d, 28d, 24d, 18d });
        var sparkSvg = sparkMetric.ToSvg("visual-block-metric-sparkline");
        Assert(sparkSvg.Contains("data-cfx-role=\"metric-mini-sparkline\"", StringComparison.Ordinal), "MetricCard should render compact sparkline groups.");
        Assert(sparkSvg.Contains("data-cfx-role=\"metric-mini-sparkline-current\"", StringComparison.Ordinal), "MetricCard sparklines should mark the current value.");
        Assert(sparkSvg.Contains("842 Mbps", StringComparison.Ordinal), "MetricCard should shrink long values enough to fit beside micro visuals.");
        Assert(sparkMetric.ToPng().Length > 64, "MetricCard sparkline should render PNG output.");

        var areaSparkMetric = MetricCard.Create()
            .WithMetric("Hydration", "4.5 L")
            .WithMiniSparkline(new[] { 1d, 2d, 3d })
            .WithSecondaryMiniSparkline(new[] { 100d, 120d, 140d });
        var areaSparkSvg = areaSparkMetric.ToSvg("visual-block-metric-area-sparkline");
        Assert(areaSparkSvg.Contains("data-cfx-style=\"area\"", StringComparison.Ordinal), "MetricCard should keep the default filled area sparkline style.");
        Assert(areaSparkSvg.Contains("data-cfx-max=\"3\"", StringComparison.Ordinal), "Area-style MetricCard sparklines should derive bounds from the visible primary series only.");
        Assert(!areaSparkSvg.Contains("data-cfx-role=\"metric-mini-sparkline-secondary\"", StringComparison.Ordinal), "Area-style MetricCard sparklines should not render hidden secondary series.");

        var heroSparkMetric = MetricCard.Create()
            .WithMetric("Running", "30 mins")
            .WithMiniSparkline(new[] { 18d, 30d, 34d, 25d, 28d, 43d, 45d, 44d, 48d })
            .WithSecondaryMiniSparkline(new[] { 15d, 27d, 31d, 23d, 25d, 40d, 42d, 41d, 45d })
            .WithMiniSparklineStyle(MetricCardSparklineStyle.Line)
            .WithMicroVisualPlacement(MetricCardMicroVisualPlacement.Hero)
            .WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset)
            .WithSize(360, 300);
        var heroSparkSvg = heroSparkMetric.ToSvg("visual-block-metric-hero-sparkline");
        Assert(heroSparkSvg.Contains("data-cfx-role=\"metric-micro-surface\"", StringComparison.Ordinal), "MetricCard hero mini visuals should support an inset surface.");
        Assert(heroSparkSvg.Contains("data-cfx-style=\"line\"", StringComparison.Ordinal), "MetricCard should render line-style sparklines without area fill.");
        Assert(heroSparkSvg.Contains("data-cfx-role=\"metric-mini-sparkline-secondary\"", StringComparison.Ordinal), "MetricCard should render a true secondary sparkline series.");
        Assert(heroSparkSvg.Contains("data-cfx-role=\"metric-mini-sparkline-start\"", StringComparison.Ordinal), "MetricCard line-style sparklines should mark the starting value.");
        Assert(heroSparkMetric.ToPng().Length > 64, "MetricCard hero line sparkline should render PNG output.");

        var compactHeroSparkMetric = MetricCard.Create()
            .WithMetric("Compact", "10")
            .WithMiniSparkline(new[] { 2d, 5d, 3d, 8d })
            .WithMicroVisualPlacement(MetricCardMicroVisualPlacement.Hero)
            .WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset)
            .AddDetail("Ready", "84%")
            .WithSize(260, 140);
        var compactHeroSparkSvg = compactHeroSparkMetric.ToSvg("visual-block-metric-compact-hero-sparkline");
        var compactSurface = System.Text.RegularExpressions.Regex.Match(compactHeroSparkSvg, "data-cfx-role=\"metric-micro-surface\"[^>]*y=\"([^\"]+)\"[^>]*height=\"([^\"]+)\"");
        Assert(compactSurface.Success, "MetricCard compact hero inset sparklines should render a measurable inset surface.");
        var compactSurfaceY = double.Parse(compactSurface.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        var compactSurfaceHeight = double.Parse(compactSurface.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert(compactSurfaceY + compactSurfaceHeight <= 140, "MetricCard compact hero inset surfaces should stay within the card bounds.");
        Assert(!compactHeroSparkSvg.Contains("data-cfx-role=\"metric-detail\"", StringComparison.Ordinal), "MetricCard compact hero sparklines should skip detail pills when there is no non-overlapping space.");
        Assert(compactHeroSparkMetric.ToPng().Length > 64, "MetricCard compact hero sparkline should render PNG output.");

        var retainedSparklineStyleMetric = MetricCard.Create()
            .WithMetric("Style", "1")
            .WithMiniSparkline(new[] { 1d, 2d })
            .WithMiniSparklineStyle(MetricCardSparklineStyle.Line)
            .WithoutMiniSparkline()
            .WithMiniSparkline(new[] { 2d, 4d });
        Assert(retainedSparklineStyleMetric.ToSvg("visual-block-metric-retained-sparkline-style").Contains("data-cfx-style=\"line\"", StringComparison.Ordinal), "MetricCard should preserve configured sparkline style after clearing and replacing sparkline data.");

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

        var dateStrip = DateStripBlock.Create()
            .WithHeader("May 9, 2026")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(620, 150)
            .AddItem("S", "9", selected: true, color: ChartColor.FromHex("#0F83F7"))
            .AddItem("M", "10")
            .AddItem("T", "11");
        var dateStripSvg = dateStrip.ToSvg("visual-block-date-strip");
        Assert(dateStripSvg.Contains("data-cfx-role=\"date-strip-block\"", StringComparison.Ordinal), "DateStripBlock should render a public block role.");
        Assert(dateStripSvg.Contains("data-cfx-role=\"date-strip-header\"", StringComparison.Ordinal), "DateStripBlock should render optional header chrome.");
        Assert(dateStripSvg.Contains("data-cfx-role=\"date-strip-item\"", StringComparison.Ordinal), "DateStripBlock should render date items.");
        Assert(dateStripSvg.Contains("data-cfx-selected=\"true\"", StringComparison.Ordinal), "DateStripBlock should preserve selected item metadata.");
        Assert(dateStrip.ToHtmlFragment().Contains("chartforgex-visual-block", StringComparison.Ordinal), "DateStripBlock should render an embeddable HTML fragment.");
        Assert(dateStrip.ToPng().Length > 64, "DateStripBlock should render PNG output.");

        var navOnlyDateStrip = DateStripBlock.Create()
            .WithNavigation()
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(620, 150)
            .AddItem("S", "9", selected: true, color: ChartColor.FromHex("#0F83F7"))
            .AddItem("M", "10");
        var navOnlyDateStripSvg = navOnlyDateStrip.ToSvg("visual-block-date-strip-nav-only");
        Assert(navOnlyDateStripSvg.Contains("data-cfx-role=\"date-strip-nav\"", StringComparison.Ordinal), "DateStripBlock navigation should render even when no header text is configured.");
        Assert(navOnlyDateStrip.ToPng().Length > 64, "DateStripBlock navigation-only headers should render PNG output.");

        var entityStrip = EntityStripBlock.Create()
            .WithTitle("Duel with friends")
            .WithAction("New")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(620, 150)
            .AddItem("Karrem", color: ChartColor.FromHex("#0F83F7"))
            .AddItem("Peter", avatarText: "P", color: ChartColor.FromHex("#FF7A1A"));
        var entityStripSvg = entityStrip.ToSvg("visual-block-entity-strip");
        Assert(entityStripSvg.Contains("data-cfx-role=\"entity-strip-block\"", StringComparison.Ordinal), "EntityStripBlock should render a public block role.");
        Assert(entityStripSvg.Contains("data-cfx-role=\"entity-strip-avatar\"", StringComparison.Ordinal), "EntityStripBlock should render avatar slots.");
        Assert(entityStripSvg.Contains("data-cfx-icon=\"person\"", StringComparison.Ordinal), "EntityStripBlock should reuse built-in person icons when avatar text is not configured.");
        Assert(entityStrip.ToHtmlFragment().Contains("chartforgex-visual-block", StringComparison.Ordinal), "EntityStripBlock should render an embeddable HTML fragment.");
        Assert(entityStrip.ToPng().Length > 64, "EntityStripBlock should render PNG output.");

        var actionOnlyEntityStrip = EntityStripBlock.Create()
            .WithAction("Open", url: "#friends")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(620, 150)
            .AddItem("Karrem", color: ChartColor.FromHex("#0F83F7"))
            .AddItem("Peter", avatarText: "P", color: ChartColor.FromHex("#FF7A1A"));
        var actionOnlyEntityStripSvg = actionOnlyEntityStrip.ToSvg("visual-block-entity-strip-action-only");
        Assert(actionOnlyEntityStripSvg.Contains("data-cfx-role=\"entity-strip-action-link\"", StringComparison.Ordinal) && actionOnlyEntityStripSvg.Contains("href=\"#friends\"", StringComparison.Ordinal), "EntityStripBlock should render linked actions even when no title is configured.");
        Assert(actionOnlyEntityStrip.ToPng().Length > 64, "EntityStripBlock action-only headers should render PNG output.");

        var sectionHeader = SectionHeaderBlock.Create()
            .WithTitle("Today's Goals")
            .WithAction("See all", url: "#goals")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(620, 48)
            .WithCard(false)
            .WithTransparentBackground();
        var sectionHeaderSvg = sectionHeader.ToSvg("visual-block-section-header");
        Assert(sectionHeaderSvg.Contains("data-cfx-role=\"section-header-title\"", StringComparison.Ordinal), "SectionHeaderBlock should render a public title role.");
        Assert(sectionHeaderSvg.Contains("data-cfx-role=\"section-header-action\"", StringComparison.Ordinal), "SectionHeaderBlock should render optional trailing actions.");
        Assert(sectionHeaderSvg.Contains("data-cfx-role=\"section-header-action-link\"", StringComparison.Ordinal) && sectionHeaderSvg.Contains("href=\"#goals\"", StringComparison.Ordinal), "SectionHeaderBlock should render safe linked actions in SVG/HTML outputs.");
        Assert(sectionHeader.ToPng().Length > 64, "SectionHeaderBlock should render PNG output.");

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
        Assert(gridHtml.Contains("linear-gradient(180deg", StringComparison.Ordinal) && gridHtml.Contains(".chartforgex-visual-grid-panel{min-width:0;width:100%;min-height:var(--cfx-visual-grid-panel-height,auto);display:grid;place-items:center;overflow:hidden}", StringComparison.Ordinal), "VisualGrid HTML pages should use polished surfaces while keeping child content inside each panel.");
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

        var backgroundTrapTheme = ChartTheme.DashboardLight()
            .WithSurfaceColors(ChartColor.FromHex("#FACC15"), ChartColor.White, ChartColor.White, ChartColor.FromHex("#E5E7EB"), ChartColor.FromHex("#E5E7EB"))
            .WithCornerRadius(18, 8);
        var childSurfaceTheme = ChartTheme.DashboardLight()
            .WithSurfaceColors(ChartColor.FromHex("#111827"), ChartColor.White, ChartColor.White, ChartColor.FromHex("#E5E7EB"), ChartColor.FromHex("#E5E7EB"))
            .WithCornerRadius(18, 8);
        var backgroundTrapBlock = MetricCard.Create()
            .WithMetric("No square", 1)
            .WithTheme(childSurfaceTheme)
            .WithSize(160, 100);
        var backgroundTrapGrid = VisualGrid.Create()
            .WithTheme(backgroundTrapTheme)
            .WithColumns(1)
            .WithPadding(24)
            .WithPanelSize(160, 100)
            .WithPanelFit(VisualGridPanelFit.Stretch)
            .Add(backgroundTrapBlock);
        var backgroundTrapSvg = backgroundTrapGrid.ToSvg("visual-grid-background-trap");
        var backgroundTrapHtml = backgroundTrapGrid.ToHtmlFragment();
        var backgroundTrapPixels = ReadPngRgba(backgroundTrapGrid.ToPng(), out var backgroundTrapWidth, out _);
        Assert(!backgroundTrapSvg.Contains("-visualBackground)", StringComparison.Ordinal), "VisualGrid SVG should suppress child block background rectangles so rounded cards do not get square backplates.");
        Assert(!backgroundTrapHtml.Contains("-visualBackground)", StringComparison.Ordinal), "VisualGrid HTML should suppress child block background rectangles so rounded cards do not get square backplates.");
        Assert(CountNearColorInRect(backgroundTrapPixels, backgroundTrapWidth, 24, 24, 8, 8, 17, 24, 39, 36) == 0, "VisualGrid PNG should not paint child visual-block surface color behind rounded card corners.");
        Assert(!backgroundTrapBlock.Options.TransparentBackground, "VisualGrid child transparency should be scoped to rendering and should not mutate the caller's visual block.");

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

        var adaptiveGrid = VisualGrid.Create()
            .WithColumns(2)
            .WithGap(20)
            .WithAdaptiveRowHeights()
            .Add(SectionHeaderBlock.Create().WithTitle("Today's Goals").WithSize(760, 44).WithCard(false).WithTransparentBackground(), columnSpan: 2)
            .Add(MetricCard.Create().WithMetric("Short", 1).WithSize(360, 140))
            .Add(MetricCard.Create().WithMetric("Tall", 2).WithSize(360, 300));
        var adaptiveSvg = adaptiveGrid.ToSvg("visual-grid-adaptive-rows");
        var adaptiveHtml = adaptiveGrid.ToHtmlFragment();
        Assert(adaptiveSvg.Contains("data-cfx-role=\"section-header-title\"", StringComparison.Ordinal), "Adaptive VisualGrid rows should compose natural section headers.");
        Assert(adaptiveHtml.Contains("--cfx-visual-grid-panel-height:auto", StringComparison.Ordinal), "Adaptive VisualGrid HTML should let natural row heights drive layout.");
        Assert(adaptiveGrid.ToPng().Length > 64, "Adaptive VisualGrid rows should render PNG output.");
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
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().MicroVisualPlacement = (MetricCardMicroVisualPlacement)999, "MetricCard should reject unknown micro visual placements.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().MiniSparklineStyle = (MetricCardSparklineStyle)999, "MetricCard should reject unknown mini sparkline styles.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().MicroVisualSurface = (MetricCardMicroVisualSurface)999, "MetricCard should reject unknown micro visual surfaces.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().WithMetric("Bad", 1, unit: new string('x', 25)).ToSvg(), "MetricCard units should stay compact.");
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
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().WithMetric("Bad", 1).WithMiniSparkline(new[] { 1d, 2d }).WithSecondaryMiniSparkline(new[] { 1d, 2d, 3d }).ToSvg(), "MetricCard secondary mini sparklines should match the primary count.");
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
        AssertThrows<InvalidOperationException>(() => DateStripBlock.Create().ToSvg(), "DateStripBlock should require items.");
        AssertThrows<InvalidOperationException>(() => DateStripBlock.Create().AddItem("", "1").ToSvg(), "DateStripBlock should require item labels.");
        AssertThrows<InvalidOperationException>(() => DateStripBlock.Create().AddItem("Monday and too long", "1").ToSvg(), "DateStripBlock item labels should stay compact.");
        AssertThrows<InvalidOperationException>(() => EntityStripBlock.Create().ToSvg(), "EntityStripBlock should require items.");
        AssertThrows<InvalidOperationException>(() => EntityStripBlock.Create().AddItem("", "A").ToSvg(), "EntityStripBlock should require item labels.");
        AssertThrows<InvalidOperationException>(() => EntityStripBlock.Create().AddItem("Bad", "TOOLONG").ToSvg(), "EntityStripBlock avatar text should stay compact.");
        AssertThrows<InvalidOperationException>(() => EntityStripBlock.Create().AddItem("Bad").WithAction("Open", url: "javascript:alert(1)").ToSvg(), "EntityStripBlock action URLs should reject scriptable URLs.");
        AssertThrows<InvalidOperationException>(() => SectionHeaderBlock.Create().ToSvg(), "SectionHeaderBlock should require a title.");
        AssertThrows<InvalidOperationException>(() => SectionHeaderBlock.Create().WithTitle("Bad").WithAction(new string('x', 49)).ToSvg(), "SectionHeaderBlock action labels should stay compact.");
        AssertThrows<InvalidOperationException>(() => SectionHeaderBlock.Create().WithTitle("Bad").WithAction("Open", url: "javascript:alert(1)").ToSvg(), "SectionHeaderBlock action URLs should reject scriptable URLs.");
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
