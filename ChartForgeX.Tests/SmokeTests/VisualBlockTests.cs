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

        var tableSvg = table.ToSvg("visual-block-table");
        Assert(tableSvg.Contains("<svg", StringComparison.Ordinal), "ChartTable should render SVG.");
        Assert(tableSvg.Contains("data-cfx-role=\"table-status\"", StringComparison.Ordinal), "ChartTable should render status markers.");
        Assert(!tableSvg.Contains("<script", StringComparison.OrdinalIgnoreCase), "Visual block SVG should stay script-free.");
        Assert(table.ToHtmlPage().Contains("chartforgex-visual-block", StringComparison.Ordinal), "ChartTable should render a static HTML page.");
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
            .WithTrend("+2.4 pp")
            .WithCaption("since previous run")
            .WithStatus(VisualStatus.Positive)
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(360, 190);
        var metricSvg = metric.ToSvg("visual-block-metric");
        Assert(metricSvg.Contains("data-cfx-role=\"metric-status-bar\"", StringComparison.Ordinal), "MetricCard should render status styling.");
        Assert(metricSvg.Contains("data-cfx-role=\"visual-icon\"", StringComparison.Ordinal), "MetricCard should render reusable built-in icons.");
        Assert(metric.ToHtmlFragment().Contains("chartforgex-visual-block", StringComparison.Ordinal), "MetricCard should render an embeddable HTML fragment.");
        Assert(metric.ToPng().Length > 64, "MetricCard should render PNG output.");

        var radialMetric = RadialMetricCard.Create()
            .WithMetric("Capacity left", "42%")
            .WithIcon(VisualIcon.Flame)
            .AddLayer("Track", 100, color: ChartColor.FromHex("#E2E8F0"), configure: layer => layer.WithGeometry(1, 0.16).WithLineCap(ChartRadialLayerCap.Butt))
            .AddLayer("Current", 42, color: ChartColor.FromHex("#F97316"), configure: layer => layer.WithGeometry(1, 0.12));
        var radialSvg = radialMetric.ToSvg("visual-block-radial-metric");
        Assert(radialSvg.Contains("data-cfx-role=\"radial-metric-layer\"", StringComparison.Ordinal), "RadialMetricCard should render public radial layers.");
        Assert(radialSvg.Contains("data-cfx-icon=\"flame\"", StringComparison.Ordinal), "RadialMetricCard should render reusable built-in icons.");
        Assert(radialMetric.ToPng().Length > 64, "RadialMetricCard should render PNG output.");
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
            .Add(chart)
            .Add(table)
            .Add(metric, columnSpan: 2);

        var svg = grid.ToSvg("visual-grid-smoke");
        Assert(svg.Contains("data-cfx-role=\"visual-grid-panel\"", StringComparison.Ordinal), "VisualGrid should mark child panels.");
        Assert(svg.Contains("data-cfx-role=\"metric-value\"", StringComparison.Ordinal), "VisualGrid should embed visual block SVG.");
        Assert(svg.Contains("Trend", StringComparison.Ordinal), "VisualGrid should embed chart SVG.");
        Assert(grid.ToHtmlPage().Contains("chartforgex-visual-grid", StringComparison.Ordinal), "VisualGrid should render a static HTML page.");
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
            .Add(chart)
            .Add(metric);
        var fixedHtml = fixedGrid.ToHtmlPage();
        Assert(fixedHtml.Contains("padding:var(--cfx-visual-grid-padding,24px)", StringComparison.Ordinal), "VisualGrid HTML should apply grid padding on the grid container where the custom property is scoped.");
        Assert(fixedHtml.Contains("chartforgex-visual-grid has-fixed-panels", StringComparison.Ordinal), "VisualGrid HTML should mark grids with fixed panel sizing.");
        Assert(fixedHtml.Contains("grid-template-columns:repeat(var(--cfx-visual-grid-columns),var(--cfx-visual-grid-panel-width,minmax(0,1fr)))", StringComparison.Ordinal), "VisualGrid HTML should honor fixed panel widths when PanelSize is configured.");
        Assert(fixedHtml.Contains("grid-auto-flow:row dense", StringComparison.Ordinal), "VisualGrid HTML should use dense placement like SVG/PNG layout.");
        Assert(fixedHtml.Contains(".chartforgex-visual-grid.has-fixed-panels .chartforgex-visual-grid-panel svg{width:100%;height:100%", StringComparison.Ordinal), "VisualGrid HTML contain mode should scale embedded SVGs to fixed panel bounds.");

        var stretchGrid = VisualGrid.Create()
            .WithPanelSize(500, 320)
            .WithPanelFit(VisualGridPanelFit.Stretch)
            .Add(chart);
        var stretchSvg = stretchGrid.ToSvg("visual-grid-stretch");
        Assert(stretchSvg.Contains("data-cfx-role=\"visual-grid-panel\"", StringComparison.Ordinal) && stretchSvg.Contains("preserveAspectRatio=\"none\"", StringComparison.Ordinal), "VisualGrid SVG stretch mode should remove child aspect-ratio locking.");
        Assert(stretchGrid.ToHtmlFragment().Contains("preserveAspectRatio=\"none\"", StringComparison.Ordinal), "VisualGrid HTML stretch mode should remove embedded SVG aspect-ratio locking.");

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
        AssertThrows<ArgumentOutOfRangeException>(() => ChartList.Create().Marker = (VisualListMarker)999, "ChartList marker property should reject unknown marker values.");
        AssertThrows<ArgumentNullException>(() => new ChartListItem("ok").Text = null!, "ChartList items should reject null text through the public setter.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartListItem("bad").Status = (VisualStatus)999, "ChartList items should reject unknown status values.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().Status = (VisualStatus)999, "MetricCard should reject unknown status values.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().Icon = (VisualIcon)999, "MetricCard should reject unknown icon values.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().WithMetric("Missing", null).ToSvg(), "MetricCard should require a visible value.");
        AssertThrows<InvalidOperationException>(() => RadialMetricCard.Create().WithMetric("Missing", "1").ToSvg(), "RadialMetricCard should require at least one layer.");
        AssertThrows<InvalidOperationException>(() => RadialMetricCard.Create().AddLayer("Bad", 1, configure: _ => null!).ToSvg(), "RadialMetricCard should reject null fluent layer configuration results.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddLayeredRadial("Bad", _ => null!), "Layered radial builder should reject null fluent layer configuration results.");
        AssertThrows<ArgumentOutOfRangeException>(() => RadialMetricCard.Create().Icon = (VisualIcon)999, "RadialMetricCard should reject unknown icon values.");
        AssertThrows<ArgumentOutOfRangeException>(() => VisualGrid.Create().WithColumns(0), "VisualGrid should reject non-positive column counts.");
        AssertThrows<ArgumentOutOfRangeException>(() => VisualGrid.Create().PanelFit = (VisualGridPanelFit)999, "VisualGrid panel fit property should reject unknown values.");
        AssertThrows<InvalidOperationException>(() => VisualGrid.Create().ToSvg(), "VisualGrid should require at least one item.");
    }
}
