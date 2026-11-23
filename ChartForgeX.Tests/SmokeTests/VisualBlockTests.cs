using System;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisualBlocksRenderTablesListsAndMetricCards() {
        var table = ChartTable.Create().Theme(VisualTheme.Graphite())
            .Title("Drive summary")
            .Subtitle("Exact facts in a visual block, not a chart series")
            .Theme(VisualTheme.TransparentOverlay()).ThemeMode(ThemeMode.Dark)
            .TransparentBackground()
            .AddColumn("Drive")
            .AddColumn("Used", TextAlignment.Right, format: "0%")
            .AddColumn("Free", TextAlignment.Right)
            .AddColumn("Status")
            .AddRow("C:", 0.72, "128 GB", "OK")
            .AddRow("D:", 0.91, "34 GB", "Warning")
            .StatusColumn("Status")
            .Dense();
        table.UpdateRow(1, row => row.Cells[3].Status = Tone.Warning);
        table.UpdateRow(0, row => row.Cells[1].Badge("72%", Tone.Success, ChartColor.FromHex("#22C55E")));
        table.UpdateRow(0, row => row.Cells[2].Sparkline(new[] { 96d, 118d, 128d }, color: ChartColor.FromHex("#38BDF8")));
        table.UpdateRow(1, row => row.Cells[2].MiniBars(new[] { 46d, 39d, 34d }, color: ChartColor.FromHex("#F97316")));

        var tableSvg = table.ToSvg("visual-block-table");
        Assert(tableSvg.Contains("<svg", StringComparison.Ordinal), "ChartTable should render SVG.");
        var tableCard = System.Xml.Linq.XDocument.Parse(tableSvg).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "visual-card");
        var tableColors = table.Options.Theme.Resolve(table.Options.ThemeMode);
        Assert((string?)tableCard.Attribute("fill") == tableColors.ElevatedSurface.ToCss()
            && (string?)tableCard.Attribute("stroke") == tableColors.Border.ToCss()
            && (string?)tableCard.Attribute("class") == "cfx-guide-stroke", "Visual block cards should retain their configured flat surface and crisp border.");
        Assert(tableSvg.Contains("data-cfx-role=\"table-status\"", StringComparison.Ordinal), "ChartTable should render status markers.");
        Assert(tableSvg.Contains("data-cfx-role=\"table-cell-microvisual\"", StringComparison.Ordinal), "ChartTable should render cell microvisual groups.");
        Assert(tableSvg.Contains("data-cfx-role=\"table-cell-sparkline\"", StringComparison.Ordinal), "ChartTable should render cell sparklines.");
        Assert(tableSvg.Contains("data-cfx-role=\"table-cell-mini-bar\"", StringComparison.Ordinal), "ChartTable should render cell mini bars.");
        Assert(tableSvg.Contains("data-cfx-role=\"visual-badge\"", StringComparison.Ordinal), "ChartTable should render shared visual badges.");
        Assert(tableSvg.Contains("data-cfx-style=\"Soft\"", StringComparison.Ordinal), "ChartTable badges should expose style metadata.");
        Assert(!tableSvg.Contains("<script", StringComparison.OrdinalIgnoreCase), "Visual block SVG should stay script-free.");
        var tableHtml = table.ToHtmlPage();
        Assert(tableHtml.Contains("chartforgex-visual-block", StringComparison.Ordinal), "ChartTable should render its static HTML content.");
        AssertFlatHtmlBackground(tableHtml, tableColors.ElevatedSurface, "Transparent table standalone pages");
        Assert(table.ToPng().Length > 64, "ChartTable should render PNG output.");

        var list = ChartList.Create().Theme(VisualTheme.Graphite())
            .Title("Security checks")
            .Marker(VisualListMarker.Status)
            .AddStatusItem("Disk encryption", Tone.Success, "ready")
            .AddStatusItem("Patch window", Tone.Warning, "due")
            .AddStatusItem("Backup age", Tone.Danger, "stale");
        var listSvg = list.ToSvg("visual-block-list");
        Assert(listSvg.Contains("data-cfx-role=\"list-marker\"", StringComparison.Ordinal), "ChartList should render list markers.");
        Assert(list.ToPng().Length > 64, "ChartList should render PNG output.");

        var checklist = ChartList.Create().Theme(VisualTheme.Graphite())
            .Title("Release checklist")
            .AddCheckItem("Static HTML", true)
            .AddCheckItem("External dependencies", false);
        Assert(checklist.ToSvg("visual-block-checklist").Contains("data-cfx-role=\"list-check\"", StringComparison.Ordinal), "Checklist lists should render check paths without relying on font glyphs.");

        VisualMetricCardsRenderWithUnitsBadgesAndHistory();

        var radialMetric = RadialMetricCard.Create().Theme(VisualTheme.Graphite())
            .Metric("Capacity left", "42%")
            .Icon(MetricCardIcons.Flame)
            .AddLayer("Track", 100, color: ChartColor.FromHex("#E2E8F0"), configure: layer => layer.RadiusRatio(1).StrokeRatio(0.16).LineCap(ChartRadialLayerCap.Butt))
            .AddLayer("Current", 42, color: ChartColor.FromHex("#F97316"), configure: layer => layer.RadiusRatio(1).StrokeRatio(0.12));
        var radialSvg = radialMetric.ToSvg("visual-block-radial-metric");
        Assert(radialSvg.Contains("data-cfx-role=\"radial-metric-layer\"", StringComparison.Ordinal), "RadialMetricCard should render public radial layers.");
        Assert(radialSvg.Contains("data-cfx-icon=\"flame\"", StringComparison.Ordinal), "RadialMetricCard should render reusable built-in icons.");
        Assert(radialMetric.ToPng().Length > 64, "RadialMetricCard should render PNG output.");

        var segmented = SegmentedMetricBlock.Create(SegmentedMetricStyle.ProgressRows)
            .Title("Project Progress")
            .Subtitle("Reusable fixed-count progress rows")
            .Theme(VisualTheme.Graphite())
            .Size(620, 250)
            .HeaderSymbol("%")
            .AddItem("Performing Progress", 89, segments: 44, color: ChartColor.FromHex("#34C77B"), delta: "+10.2%", status: Tone.Success)
            .AddItem("Target Sales", 67, segments: 44, color: ChartColor.FromHex("#5EA2F6"), delta: "+2.2%", status: Tone.Info)
            .Subtitle("Up by 6% compared to last week");
        var segmentedSvg = segmented.ToSvg("visual-block-segmented-metric");
        Assert(segmentedSvg.Contains("data-cfx-role=\"segmented-metric-progress-rows\"", StringComparison.Ordinal), "SegmentedMetricBlock should render progress-row treatments.");
        Assert(segmentedSvg.Contains("data-cfx-role=\"segmented-metric-header-badge\"", StringComparison.Ordinal), "SegmentedMetricBlock should render optional header badges.");
        Assert(segmentedSvg.Contains("data-cfx-role=\"segmented-metric-progress-strip\"", StringComparison.Ordinal), "SegmentedMetricBlock should render segmented strips.");
        var progressStrips = System.Xml.Linq.XDocument.Parse(segmentedSvg).Descendants()
            .Where(element => (string?)element.Attribute("data-cfx-role") == "segmented-metric-progress-strip").ToArray();
        var filledCounts = new[] { 39, 29 };
        Assert(progressStrips.Length == 2, "SegmentedMetricBlock should retain both authored progress rows.");
        var segmentedPixels = ReadPngRgba(segmented.ToPng(), out var segmentedWidth, out _);
        for (var rowIndex = 0; rowIndex < progressStrips.Length; rowIndex++) {
            var segments = progressStrips[rowIndex].Elements().ToArray();
            var filled = segments.Where(element => (string?)element.Attribute("data-cfx-role") == "segmented-metric-segment-filled").ToArray();
            var empty = segments.Where(element => (string?)element.Attribute("data-cfx-role") == "segmented-metric-segment-empty").ToArray();
            var color = segmented.Items[rowIndex].Color!.Value;
            Assert(segments.Length == 44 && filled.Length == filledCounts[rowIndex] && empty.Length == 44 - filledCounts[rowIndex],
                "Flat progress strips should retain the exact authored segment count and filled share.");
            Assert(filled.All(element => (string?)element.Attribute("fill") == color.ToCss()
                && (string?)element.Attribute("stroke") == color.WithAlpha(120).ToCss())
                && empty.All(element => (string?)element.Attribute("fill") == segmented.Options.Theme.Resolve(segmented.Options.ThemeMode).ElevatedSurface.ToCss()),
                "Flat progress segments should preserve their authored accent, border and empty surface colors.");
            var first = filled[0];
            double Coordinate(string name) => double.Parse(first.Attribute(name)!.Value, System.Globalization.CultureInfo.InvariantCulture);
            var centerX = (int)Math.Floor(Coordinate("x") + Coordinate("width") / 2);
            var centerY = (int)Math.Floor(Coordinate("y") + Coordinate("height") / 2);
            var pixel = (centerY * segmentedWidth + centerX) * 4;
            Assert(segmentedPixels[pixel] == color.R && segmentedPixels[pixel + 1] == color.G
                && segmentedPixels[pixel + 2] == color.B && segmentedPixels[pixel + 3] == 255,
                "Native PNG should paint the same opaque authored accent inside each progress strip.");
        }
        Assert(segmentedSvg.Contains("data-cfx-segments=\"44\"", StringComparison.Ordinal), "SegmentedMetricBlock should preserve fixed segment counts in SVG metadata.");
        Assert(segmentedSvg.Contains("data-cfx-filled=\"39\"", StringComparison.Ordinal), "SegmentedMetricBlock should derive filled segment counts from value and maximum.");
        Assert(segmentedSvg.Contains("data-cfx-role=\"segmented-metric-delta-pill\"", StringComparison.Ordinal), "SegmentedMetricBlock should render delta pills.");
        Assert(segmentedSvg.Contains("Up by 6% compared to last week", StringComparison.Ordinal), "SegmentedMetricBlock should render comparison facts as subtitle text.");

        var subtitleOnlyHeader = SegmentedMetricBlock.Create(SegmentedMetricStyle.ProgressRows)
            .Subtitle("Subtitle without title")
            .Theme(VisualTheme.Graphite())
            .Size(420, 180)
            .AddItem("Ready", 1, segments: 8);
        var subtitleOnlyHeaderSvg = subtitleOnlyHeader.ToSvg("visual-block-segmented-subtitle-only-header");
        var subtitleOnlyText = System.Xml.Linq.XDocument.Parse(subtitleOnlyHeaderSvg).Descendants().Single(element =>
            element.Name.LocalName == "text" && element.Value == "Subtitle without title");
        var subtitleBaseline = double.Parse(subtitleOnlyText.Attribute("y")!.Value, System.Globalization.CultureInfo.InvariantCulture);
        var firstProgressY = GetAttribute(subtitleOnlyHeaderSvg, "data-cfx-role=\"segmented-metric-progress-strip\"", "data-cfx-strip-y");
        Assert(firstProgressY > subtitleBaseline, "SegmentedMetricBlock subtitle-only headings should retain factual text above the progress plot.");
        Assert(subtitleOnlyHeader.ToPng().Length > 64, "SegmentedMetricBlock subtitle-only headers should render PNG output.");

        var performanceRows = SegmentedMetricBlock.Create(SegmentedMetricStyle.ProgressRows)
            .Theme(VisualTheme.Graphite())
            .Size(620, 260)
            .AddItem(new SegmentedMetricItem("Posts", 86)
                .Maximum(100).Segments(44)
                .Color(ChartColor.FromHex("#5EA2F6"))
                .DisplayValue("132,034")
                .Delta("+4.3%")
                .Status(Tone.Success))
            .AddItem(new SegmentedMetricItem("Replies", 62)
                .Maximum(100).Segments(44)
                .Color(ChartColor.FromHex("#7894F8"))
                .DisplayValue("56,234")
                .Delta("-6.4%")
                .Status(Tone.Danger));
        var performanceRowsSvg = performanceRows.ToSvg("visual-block-segmented-performance-rows");
        Assert(performanceRowsSvg.Contains("132,034", StringComparison.Ordinal), "SegmentedMetricBlock progress rows should render item display values when supplied.");
        Assert(performanceRowsSvg.Contains("+4.3%", StringComparison.Ordinal) && performanceRowsSvg.Contains("-6.4%", StringComparison.Ordinal), "SegmentedMetricBlock progress rows should render semantic deltas alongside exact values.");
        Assert(performanceRows.ToPng().Length > 64, "SegmentedMetricBlock performance rows should render PNG output.");

        var capsule = SegmentedMetricBlock.Create(SegmentedMetricStyle.CapsuleLoop)
            .Title("Certificate Count")
            .Subtitle("Reusable capsule loop")
            .Theme(VisualTheme.Graphite())
            .Size(620, 260)
            .AddItem("Valid", 42, color: ChartColor.FromHex("#34C77B"), displayValue: "42")
            .AddItem("Expiring", 12, color: ChartColor.FromHex("#FFB05C"), displayValue: "12")
            .AddItem("Revoked", 6, color: ChartColor.FromHex("#EF5DA8"), displayValue: "6")
            .AddItem("Unknown", 10, color: ChartColor.FromHex("#6D83F2"), displayValue: "10");
        var capsuleSvg = capsule.ToSvg("visual-block-segmented-capsule");
        Assert(capsuleSvg.Contains("data-cfx-role=\"segmented-metric-capsule-loop\"", StringComparison.Ordinal), "SegmentedMetricBlock should render capsule loop treatments.");
        Assert(capsuleSvg.Contains("data-cfx-role=\"segmented-metric-capsule-track\"", StringComparison.Ordinal), "SegmentedMetricBlock capsule loops should render a base track.");
        Assert(capsuleSvg.Contains("data-cfx-role=\"segmented-metric-capsule-segment\"", StringComparison.Ordinal), "SegmentedMetricBlock capsule loops should render part-to-whole segments.");
        Assert(GetAttribute(capsuleSvg, "data-cfx-label=\"Revoked\"", "data-cfx-end") > GetAttribute(capsuleSvg, "data-cfx-label=\"Revoked\"", "data-cfx-start"), "SegmentedMetricBlock capsule loops should keep small slices visible on the loop track.");
        Assert(capsuleSvg.Contains("data-cfx-role=\"segmented-metric-capsule-label\"", StringComparison.Ordinal), "SegmentedMetricBlock capsule loops should render readable share labels when there is room.");
        Assert(capsuleSvg.Contains("paint-order=\"stroke\"", StringComparison.Ordinal), "SegmentedMetricBlock capsule labels should render with a readable halo.");
        Assert(capsuleSvg.Contains("data-cfx-role=\"segmented-metric-capsule-legend\"", StringComparison.Ordinal), "SegmentedMetricBlock capsule loops should render generic legend values.");
        Assert(capsule.ToPng().Length > 64, "SegmentedMetricBlock capsule loops should render PNG output.");

        var paletteFallback = SegmentedMetricBlock.Create(SegmentedMetricStyle.CapsuleLoop)
            .Theme(VisualTheme.Graphite())
            .Palette(ChartPalettes.FromHex("#112233", "#445566"))
            .Size(420, 220)
            .AddItem("First", 3)
            .AddItem("Second", 2);
        var paletteFallbackSvg = paletteFallback.ToSvg("visual-block-segmented-palette-fallback");
        Assert(paletteFallbackSvg.Contains("stroke=\"#112233\"", StringComparison.Ordinal), "SegmentedMetricBlock items without color or status should use the theme palette.");
        Assert(paletteFallbackSvg.Contains("stroke=\"#445566\"", StringComparison.Ordinal), "SegmentedMetricBlock should cycle the theme palette across generic items.");

        var funnel = SegmentedMetricBlock.Create(SegmentedMetricStyle.FunnelColumns)
            .Title("Conversion Funnel")
            .Subtitle("Ordered stage counts")
            .Theme(VisualTheme.Graphite())
            .Size(620, 280)
            .AddItem("Clicks", 82000, segments: 24, color: ChartColor.FromHex("#34C77B"), displayValue: "82,000")
            .AddItem("Added", 7200, segments: 16, color: ChartColor.FromHex("#FFB05C"), displayValue: "7,200")
            .AddItem("Payment", 1230, segments: 12, color: ChartColor.FromHex("#6D83F2"), displayValue: "1,230");
        var funnelSvg = funnel.ToSvg("visual-block-segmented-funnel-columns");
        Assert(funnelSvg.Contains("data-cfx-role=\"segmented-metric-funnel-columns\"", StringComparison.Ordinal), "SegmentedMetricBlock should render ordered funnel-column treatments.");
        Assert(funnelSvg.Contains("data-cfx-role=\"segmented-metric-funnel-stage\"", StringComparison.Ordinal), "SegmentedMetricBlock funnel columns should render generic stage groups.");
        Assert(funnelSvg.Contains("data-cfx-role=\"segmented-metric-funnel-bar\"", StringComparison.Ordinal), "SegmentedMetricBlock funnel columns should render repeated stage bars.");
        Assert(funnelSvg.Contains("data-cfx-segments=\"24\"", StringComparison.Ordinal), "SegmentedMetricBlock funnel columns should preserve per-stage segment counts.");
        Assert(funnel.ToPng().Length > 64, "SegmentedMetricBlock funnel columns should render PNG output.");

        var compactFunnel = SegmentedMetricBlock.Create(SegmentedMetricStyle.FunnelColumns)
            .Title("Compact Funnel")
            .Subtitle("Longer factual header")
            .Theme(VisualTheme.Graphite())
            .Size(360, 170)
            .AddItem("First", 10, segments: 12)
            .AddItem("Second", 8, segments: 10)
            .AddItem("Third", 6, segments: 8);
        var compactFunnelSvg = compactFunnel.ToSvg("visual-block-segmented-compact-funnel");
        var compactBarY = GetAttribute(compactFunnelSvg, "data-cfx-role=\"segmented-metric-funnel-columns\"", "data-cfx-bar-y");
        var compactBarHeight = GetAttribute(compactFunnelSvg, "data-cfx-role=\"segmented-metric-funnel-columns\"", "data-cfx-bar-height");
        Assert(compactBarY >= 0 && compactBarHeight > 0 && compactBarY + compactBarHeight <= 170, "SegmentedMetricBlock compact funnel bars should cap to the available plot height instead of overlapping the header.");
        var lastStageX = GetAttribute(compactFunnelSvg, "data-cfx-label=\"Third\"", "data-cfx-x");
        var lastStageWidth = GetAttribute(compactFunnelSvg, "data-cfx-label=\"Third\"", "data-cfx-width");
        Assert(lastStageX + lastStageWidth <= compactFunnel.Options.Size.Width - compactFunnel.Options.Padding.Right + 0.1, "SegmentedMetricBlock funnel stage geometry should fit within the card content width.");

        var denseFunnel = SegmentedMetricBlock.Create(SegmentedMetricStyle.FunnelColumns)
            .Theme(VisualTheme.Graphite())
            .Size(360, 180)
            .AddItem("First", 100, segments: 120)
            .AddItem("Second", 80, segments: 120)
            .AddItem("Third", 60, segments: 120);
        var denseFunnelSvg = denseFunnel.ToSvg("visual-block-segmented-dense-funnel");
        var denseLastStageX = GetAttribute(denseFunnelSvg, "data-cfx-label=\"Third\"", "data-cfx-x");
        var denseLastStageWidth = GetAttribute(denseFunnelSvg, "data-cfx-label=\"Third\"", "data-cfx-width");
        Assert(denseLastStageX + denseLastStageWidth <= denseFunnel.Options.Size.Width - denseFunnel.Options.Padding.Right + 0.1, "SegmentedMetricBlock dense funnel stages should preserve fitted widths instead of overflowing after a minimum bar clamp.");

        var compactCapsule = SegmentedMetricBlock.Create(SegmentedMetricStyle.CapsuleLoop)
            .Title("Compact Capsule")
            .Subtitle("Tight card")
            .Theme(VisualTheme.Graphite())
            .Size(360, 170)
            .AddItem("First", 10)
            .AddItem("Second", 8)
            .AddItem("Third", 6);
        var compactCapsuleSvg = compactCapsule.ToSvg("visual-block-segmented-compact-capsule");
        var compactCapsuleHeight = GetAttribute(compactCapsuleSvg, "data-cfx-role=\"segmented-metric-capsule-loop\"", "data-cfx-loop-height");
        Assert(compactCapsuleHeight > 0 && compactCapsuleHeight < 72, "SegmentedMetricBlock compact capsule loops should fit available plot height.");

        var configuredItems = SegmentedMetricBlock.Create(SegmentedMetricStyle.ProgressRows)
            .Theme(VisualTheme.Graphite())
            .Size(460, 220)
            .AddItem("Configured", 5, item => {
                item.Maximum = 10;
                item.Segments = 20;
                item.Delta = "+1";
            })
            .AddItem(new SegmentedMetricItem("Prebuilt", 4)
                .Maximum(8).Segments(16)
                .Status(Tone.Success)
                .Delta("+2"));
        var configuredItemsSvg = configuredItems.ToSvg("visual-block-segmented-configured-items");
        Assert(configuredItemsSvg.Contains("data-cfx-segments=\"20\"", StringComparison.Ordinal), "SegmentedMetricBlock should support callback-configured items without growing AddItem overloads.");
        Assert(configuredItemsSvg.Contains("data-cfx-segments=\"16\"", StringComparison.Ordinal), "SegmentedMetricBlock should accept preconfigured item instances.");
        Assert(configuredItemsSvg.Contains("data-cfx-role=\"segmented-metric-delta-pill\"", StringComparison.Ordinal), "SegmentedMetricBlock should render configured item deltas.");

        var formattedDisplayValue = SegmentedMetricBlock.Create(SegmentedMetricStyle.FunnelColumns)
            .Theme(VisualTheme.Graphite())
            .Size(420, 220)
            .AddItem("Formatted", 1234.56, segments: 6, displayValue: 1234.56, displayFormat: "0.0");
        Assert(formattedDisplayValue.ToSvg("visual-block-segmented-formatted-display-value").Contains(">1234.6<", StringComparison.Ordinal), "SegmentedMetricBlock display values should accept formatted objects.");

        var fluentItems = SegmentedMetricBlock.Create(SegmentedMetricStyle.DistributionRows)
            .Theme(VisualTheme.Graphite())
            .Size(460, 260)
            .Metric("Inventory", 30)
            .AddItem(new SegmentedMetricItem("Certificates (TLS)", 18)
                .Symbol("TLS")
                .DisplayValue(18, "0")
                .Pattern(ChartFillPattern.DiagonalForward))
            .AddItem(new SegmentedMetricItem("Certificates (S/MIME)", 12)
                .Symbol("SM")
                .DisplayValue("12")
                .Color(ChartColor.FromHex("#445566")));
        var fluentItemsSvg = fluentItems.ToSvg("visual-block-segmented-fluent-items");
        Assert(fluentItemsSvg.Contains("data-cfx-pattern=\"DiagonalForward\"", StringComparison.Ordinal) || fluentItemsSvg.Contains("TLS", StringComparison.Ordinal), "SegmentedMetricItem fluent helpers should configure reusable item metadata.");
        Assert(fluentItemsSvg.Contains("TLS", StringComparison.Ordinal), "SegmentedMetricItem fluent helpers should configure symbols.");
        Assert(fluentItemsSvg.Contains("18", StringComparison.Ordinal), "SegmentedMetricItem fluent helpers should configure display values.");

        var composition = SegmentedMetricBlock.Create(SegmentedMetricStyle.CompositionStrip)
            .Title("Overall Tasks")
            .Subtitle("Spread across 6 projects.")
            .Theme(VisualTheme.Graphite())
            .Size(620, 360)
            .Metric("Tasks", 23, "Task")
            .AddItem("On Going", 12, color: ChartColor.FromHex("#5EA2F6"), status: Tone.Info, pattern: ChartFillPattern.DiagonalForward)
            .AddItem("Under Review", 6, color: ChartColor.FromHex("#FFB05C"), status: Tone.Warning)
            .AddItem("Finish", 4, color: ChartColor.FromHex("#34C77B"), status: Tone.Success);
        var compositionSvg = composition.ToSvg("visual-block-composition-status");
        Assert(compositionSvg.Contains("data-cfx-role=\"segmented-metric-composition\"", StringComparison.Ordinal), "SegmentedMetricBlock should render a composition treatment.");
        Assert(compositionSvg.Contains("data-cfx-role=\"segmented-metric-composition-strip\"", StringComparison.Ordinal), "SegmentedMetricBlock should render a stacked composition strip.");
        Assert(compositionSvg.Contains("Finish", StringComparison.Ordinal), "SegmentedMetricBlock composition strips should fit all legend rows below the factual heading.");
        Assert(compositionSvg.Contains("data-cfx-pattern=\"DiagonalForward\"", StringComparison.Ordinal), "SegmentedMetricBlock should preserve segment pattern hints in SVG metadata.");
        Assert(compositionSvg.Contains("data-cfx-role=\"segmented-metric-legend-swatch\"", StringComparison.Ordinal), "SegmentedMetricBlock should render legend swatches.");
        var formattedComposition = SegmentedMetricBlock.Create(SegmentedMetricStyle.CompositionStrip)
            .Theme(VisualTheme.Graphite())
            .Size(520, 300)
            .Metric("Certificates", 150)
            .AddItem("Valid", 100, displayValue: 99.5, displayFormat: "0.0")
            .AddItem("Expiring", 50);
        Assert(formattedComposition.ToSvg("visual-block-composition-formatted-display-value").Contains(">99.5 67%<", StringComparison.Ordinal), "SegmentedMetricBlock composition legends should honor formatted display values.");
        Assert(composition.ToHtmlFragment().Contains("chartforgex-visual-block", StringComparison.Ordinal), "SegmentedMetricBlock should render an embeddable HTML fragment.");
        Assert(composition.ToPng().Length > 64, "SegmentedMetricBlock composition strips should render PNG output.");

        var distribution = SegmentedMetricBlock.Create(SegmentedMetricStyle.DistributionRows)
            .Title("Net Earning")
            .Subtitle("Currency split")
            .Theme(VisualTheme.Graphite())
            .Size(620, 360)
            .Metric("Net earning", "EUR 56,980.00", caption: "Last month")
            .AddItem("Russian Ruble (RUB)", 9.74, color: ChartColor.FromHex("#FF3B13"), symbol: "RUB", displayValue: "EUR 12.23")
            .AddItem("Euro (EUR)", 38.48, color: ChartColor.FromHex("#1389F2"), symbol: "EUR", displayValue: "EUR 20.23")
            .AddItem("United States Dollar (USD)", 14.11, color: ChartColor.FromHex("#24D47B"), symbol: "USD", displayValue: "EUR 12.00");
        var distributionSvg = distribution.ToSvg("visual-block-distribution-strip");
        Assert(distributionSvg.Contains("data-cfx-role=\"segmented-metric-distribution\"", StringComparison.Ordinal), "SegmentedMetricBlock should render a distribution treatment.");
        Assert(distributionSvg.Contains("data-cfx-role=\"segmented-metric-distribution-segment\"", StringComparison.Ordinal), "SegmentedMetricBlock should render stacked strip segments.");
        Assert(distributionSvg.Contains("data-cfx-role=\"segmented-metric-distribution-chip\"", StringComparison.Ordinal), "SegmentedMetricBlock should render legend chips.");
        Assert(distributionSvg.Contains("data-cfx-role=\"segmented-metric-distribution-row\"", StringComparison.Ordinal), "SegmentedMetricBlock should render detail rows.");
        Assert(distributionSvg.Contains("data-cfx-role=\"segmented-metric-ring\"", StringComparison.Ordinal), "SegmentedMetricBlock should render row share rings.");
        Assert(distribution.ToHtmlFragment().Contains("chartforgex-visual-block", StringComparison.Ordinal), "SegmentedMetricBlock should render an embeddable HTML fragment.");
        Assert(distribution.ToPng().Length > 64, "SegmentedMetricBlock distribution rows should render PNG output.");

        var heatmap = HeatmapInsightCard.Create()
            .Title("Appointment Volume")
            .Theme(VisualTheme.Graphite())
            .Size(620, 320)
            .PeriodLabel("Week 1")
            .Columns("S", "M", "T")
            .Minimum(0).Maximum(12).LowColor(ChartColor.FromHex("#D7F5F7")).HighColor(ChartColor.FromHex("#08798C"))
            .AddRow("9 AM", 9, 3, 12)
            .AddRow("10 AM", 11, 2, 9)
            .AddInsight("Fri, 5 PM - 6 PM", "16 appointments");
        var heatmapSvg = heatmap.ToSvg("visual-block-heatmap-insight");
        Assert(heatmapSvg.Contains("data-cfx-role=\"heatmap-insight-card\"", StringComparison.Ordinal), "HeatmapInsightCard should render a public card role.");
        Assert(heatmapSvg.Contains("data-cfx-role=\"heatmap-insight-cell\"", StringComparison.Ordinal), "HeatmapInsightCard should render heatmap cells.");
        Assert(heatmapSvg.Contains("data-cfx-role=\"heatmap-insight-rail\"", StringComparison.Ordinal), "HeatmapInsightCard should render the insight rail.");
        Assert(heatmapSvg.Contains("data-cfx-role=\"heatmap-color-key\"", StringComparison.Ordinal), "HeatmapInsightCard should render the color key.");
        Assert(heatmapSvg.Contains("16 appointments", StringComparison.Ordinal), "HeatmapInsightCard should render insight details.");
        Assert(heatmapSvg.Contains("Week 1", StringComparison.Ordinal), "HeatmapInsightCard should preserve its factual reporting period.");
        Assert(heatmap.ToHtmlFragment().Contains("chartforgex-visual-block", StringComparison.Ordinal), "HeatmapInsightCard should render an embeddable HTML fragment.");
        Assert(heatmap.ToPng().Length > 64, "HeatmapInsightCard should render PNG output.");

        var dateStrip = DateStripBlock.Create()
            .Header("May 9, 2026")
            .Theme(VisualTheme.Graphite())
            .Size(620, 150)
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

        var entityStrip = EntityStripBlock.Create()
            .Title("Duel with friends")
            .Theme(VisualTheme.Graphite())
            .Size(620, 150)
            .AddItem("Karrem", color: ChartColor.FromHex("#0F83F7"))
            .AddItem("Peter", avatarText: "P", color: ChartColor.FromHex("#FF7A1A"));
        var entityStripSvg = entityStrip.ToSvg("visual-block-entity-strip");
        Assert(entityStripSvg.Contains("data-cfx-role=\"entity-strip-block\"", StringComparison.Ordinal), "EntityStripBlock should render a public block role.");
        Assert(entityStripSvg.Contains("data-cfx-role=\"entity-strip-avatar\"", StringComparison.Ordinal), "EntityStripBlock should render avatar slots.");
        Assert(entityStripSvg.Contains("data-cfx-icon=\"person\"", StringComparison.Ordinal), "EntityStripBlock should reuse built-in person icons when avatar text is not configured.");
        Assert(entityStrip.ToHtmlFragment().Contains("chartforgex-visual-block", StringComparison.Ordinal), "EntityStripBlock should render an embeddable HTML fragment.");
        Assert(entityStrip.ToPng().Length > 64, "EntityStripBlock should render PNG output.");

        var sectionHeader = SectionHeaderBlock.Create()
            .Title("Today's Goals")
            .Theme(VisualTheme.Graphite())
            .Size(620, 48)
            .ShowCard(false)
            .TransparentBackground();
        var sectionHeaderSvg = sectionHeader.ToSvg("visual-block-section-header");
        Assert(sectionHeaderSvg.Contains("data-cfx-role=\"section-header-title\"", StringComparison.Ordinal), "SectionHeaderBlock should render a public title role.");
        Assert(sectionHeader.ToPng().Length > 64, "SectionHeaderBlock should render PNG output.");

        var workload = WorkloadListBlock.Create()
            .Title("Today Staff Workload")
            .Subtitle("Capacity share by staff member")
            .Theme(VisualTheme.Graphite())
            .Size(620, 320)
            .AddPerson("Panji Dwi", "Zumba Trainer", 4, 8, Tone.Neutral, "PD", "4/8", ChartColor.FromHex("#0E7490"))
            .AddPerson("Raihan Fikri", "Aerobik Trainer", 10, 8, Tone.Danger, "RF", "10/8", ChartColor.FromHex("#DC2626"), "Overload")
            .AddPerson("Mufti Hidayat", "Massage Specialist", 6, 8, Tone.Success);
        var workloadSvg = workload.ToSvg("visual-block-workload-list");
        Assert(workloadSvg.Contains("data-cfx-role=\"workload-list-block\"", StringComparison.Ordinal), "WorkloadListBlock should render a public block role.");
        Assert(workloadSvg.Contains("data-cfx-role=\"workload-avatar\"", StringComparison.Ordinal), "WorkloadListBlock should render avatar slots.");
        Assert(workloadSvg.Contains("data-cfx-role=\"workload-progress-rail\"", StringComparison.Ordinal), "WorkloadListBlock should render progress rails.");
        Assert(workloadSvg.Contains("data-cfx-role=\"workload-progress-fill\"", StringComparison.Ordinal), "WorkloadListBlock should render progress fills.");
        Assert(workloadSvg.Contains("data-cfx-ratio=\"1\"", StringComparison.Ordinal), "WorkloadListBlock should clamp overloaded progress ratios for renderers.");
        Assert(workload.ToPng().Length > 64, "WorkloadListBlock should render PNG output.");

        var activity = ActivityTimelineBlock.Create()
            .Title("Shipment Timeline")
            .Subtitle("Shipment status feed")
            .Theme(VisualTheme.Graphite())
            .Size(620, 620)
            .ShowEventSurfaces(false)
            .AddSection("In-progress")
            .AddEvent("Shipment", "Just now", Tone.Info, "In-Progress", "Delivery by Royal Mail Standard", "S")
            .AddEvent("Shipment 1", status: Tone.Neutral)
            .AddChecklistItem("Carrier confirmed", completed: true, muted: true)
            .AddChecklistItem("Packing in progress", completed: false)
            .AddHiddenSummary(6, "items hidden")
            .AddSection("Completed")
            .AddEvent("Order created", "Mar 10, 2026 10:20 am", Tone.Success);
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
            .Title("Project Timeline")
            .Subtitle("Dense schedule lanes")
            .Theme(VisualTheme.Graphite())
            .Size(760, 360)
            .Start(8).End(17).TickInterval(1)
            .CurrentTime(14.2)
            .Subtitle("12/Feb/2025")
            .AddEvent("Meeting Brief Project", 8, 10, 0, ChartColor.FromHex("#5EA2F6"), Tone.Info, avatars: new[] { "AM", "RF", "PD", "MR" })
            .AddEvent("Research Analyze Content", 9, 11, 1, ChartColor.FromHex("#8B5CF6"), Tone.Info, avatars: new[] { "SC", "MR" })
            .AddEvent("Report Review", 16, 17.2, 0, ChartColor.FromHex("#5EA2F6"), Tone.Info, badge: "Report", avatars: new[] { "MR", "SC" });
        var scheduleSvg = schedule.ToSvg("visual-block-schedule-timeline");
        Assert(scheduleSvg.Contains("data-cfx-role=\"schedule-timeline-block\"", StringComparison.Ordinal), "ScheduleTimelineBlock should render a public block role.");
        Assert(scheduleSvg.Contains("12/Feb/2025", StringComparison.Ordinal), "ScheduleTimelineBlock should preserve its factual date in the subtitle.");
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
            .Title("Trend")
            .Size(420, 260)
            .AddSmoothLine("Warnings", Points(8, 7, 5, 4), ChartColor.FromHex("#F59E0B"));
        var table = ChartTable.Create()
            .Title("Volumes")
            .Columns("Name", "Used", "Status")
            .AddRow("C:", "72%", "OK")
            .AddRow("D:", "91%", "Warning")
            .StatusColumn("Status");
        var metric = MetricCard.Create()
            .Metric("Open items", 12)
            .Status(Tone.Warning);

        var grid = VisualGrid.Create()
            .Title("System snapshot")
            .Subtitle("Charts and exact-fact blocks share one export surface")
            .Theme(VisualTheme.Graphite()).ThemeMode(ThemeMode.Dark)
            .Columns(2)
            .PanelSize(420, 260)
            .FrameVisible()
            .Add(chart)
            .Add(table)
            .Add(metric, columnSpan: 2);

        var svg = grid.ToSvg("visual-grid-smoke");
        Assert(svg.Contains("data-cfx-role=\"visual-grid-frame\"", StringComparison.Ordinal), "VisualGrid should render optional premium frames in SVG.");
        var gridDocument = System.Xml.Linq.XDocument.Parse(svg);
        var gridBackground = gridDocument.Root!.Elements().Single(element => element.Name.LocalName == "rect" && (string?)element.Attribute("width") == "100%" && (string?)element.Attribute("height") == "100%");
        var gridFrame = gridDocument.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "visual-grid-frame");
        var gridColors = grid.Theme.Resolve(grid.ThemeMode);
        Assert((string?)gridBackground.Attribute("fill") == gridColors.Background.ToCss()
            && (string?)gridFrame.Attribute("stroke") == gridColors.Border.ToCss()
            && (string?)gridFrame.Attribute("class") == "cfx-guide-stroke", "VisualGrid should paint its flat report background and configured frame border.");
        Assert(svg.Contains("data-cfx-role=\"visual-grid-panel\"", StringComparison.Ordinal), "VisualGrid should mark child panels.");
        Assert(svg.Contains("data-cfx-role=\"metric-value\"", StringComparison.Ordinal), "VisualGrid should embed visual block SVG.");
        Assert(svg.Contains("Trend", StringComparison.Ordinal), "VisualGrid should embed chart SVG.");
        var gridHtml = grid.ToHtmlPage();
        Assert(gridHtml.Contains("chartforgex-visual-grid has-fixed-panels has-frame", StringComparison.Ordinal), "VisualGrid should render optional premium frames in static HTML pages.");
        AssertFlatHtmlBackground(gridHtml, gridColors.Background, "Visual grid standalone pages");
        Assert(gridHtml.Contains(".chartforgex-visual-grid-panel{min-width:0;width:100%;min-height:var(--cfx-visual-grid-panel-height,auto);display:grid;place-items:center;overflow:hidden}", StringComparison.Ordinal), "VisualGrid HTML should keep child content inside each panel.");
        Assert(grid.ToPng().Length > 64, "VisualGrid should render PNG output.");

        var sparseGrid = VisualGrid.Create()
            .Columns(6)
            .Add(MetricCard.Create().Metric("One", 1))
            .Add(MetricCard.Create().Metric("Two", 2));
        Assert(sparseGrid.ToHtmlFragment().Contains("--cfx-visual-grid-columns:2", StringComparison.Ordinal), "VisualGrid HTML should clamp columns to populated item count like SVG/PNG layout.");

        var fixedGrid = VisualGrid.Create()
            .Columns(2)
            .Padding(32)
            .PanelSize(500, 320)
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

        var backgroundTrapTheme = VisualTheme.Graphite()
            .Configure(builder => { builder.Light.Background = builder.Dark.Background = ChartColor.FromHex("#FACC15"); builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = ChartColor.White; builder.Light.Surface = builder.Dark.Surface = ChartColor.White; builder.Light.Border = builder.Dark.Border = ChartColor.FromHex("#E5E7EB"); builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.FromHex("#E5E7EB"); })
            .Configure(builder => { builder.CardRadius = 18; builder.PlotRadius = builder.BarRadius = 8; });
        var childSurfaceTheme = VisualTheme.Graphite()
            .Configure(builder => { builder.Light.Background = builder.Dark.Background = ChartColor.FromHex("#111827"); builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = ChartColor.White; builder.Light.Surface = builder.Dark.Surface = ChartColor.White; builder.Light.Border = builder.Dark.Border = ChartColor.FromHex("#E5E7EB"); builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.FromHex("#E5E7EB"); })
            .Configure(builder => { builder.CardRadius = 18; builder.PlotRadius = builder.BarRadius = 8; });
        var backgroundTrapBlock = MetricCard.Create()
            .Metric("No square", 1)
            .Theme(childSurfaceTheme)
            .Size(160, 100);
        var backgroundTrapGrid = VisualGrid.Create()
            .Theme(backgroundTrapTheme)
            .Columns(1)
            .Padding(24)
            .PanelSize(160, 100)
            .PanelFit(VisualPanelFit.Stretch)
            .Add(backgroundTrapBlock);
        var backgroundTrapSvg = backgroundTrapGrid.ToSvg("visual-grid-background-trap");
        var backgroundTrapHtml = backgroundTrapGrid.ToHtmlFragment();
        var backgroundTrapPixels = ReadPngRgba(backgroundTrapGrid.ToPng(), out var backgroundTrapWidth, out _);
        Assert(!backgroundTrapSvg.Contains("data-cfx-role=\"visual-background\"", StringComparison.Ordinal), "VisualGrid SVG should suppress child block background rectangles so rounded cards do not get square backplates.");
        Assert(!backgroundTrapHtml.Contains("data-cfx-role=\"visual-background\"", StringComparison.Ordinal), "VisualGrid HTML should suppress child block background rectangles so rounded cards do not get square backplates.");
        Assert(CountNearColorInRect(backgroundTrapPixels, backgroundTrapWidth, 24, 24, 8, 8, 17, 24, 39, 36) == 0, "VisualGrid PNG should not paint child visual-block surface color behind rounded card corners.");
        Assert(!backgroundTrapBlock.Options.TransparentBackground, "VisualGrid child transparency should be scoped to rendering and should not mutate the caller's visual block.");

        var stretchGrid = VisualGrid.Create()
            .PanelSize(500, 320)
            .PanelFit(VisualPanelFit.Stretch)
            .Add(chart);
        var stretchSvg = stretchGrid.ToSvg("visual-grid-stretch");
        Assert(stretchSvg.Contains("data-cfx-role=\"visual-grid-panel\"", StringComparison.Ordinal) && stretchSvg.Contains("preserveAspectRatio=\"none\"", StringComparison.Ordinal), "VisualGrid SVG stretch mode should remove child aspect-ratio locking.");
        Assert(stretchGrid.ToHtmlFragment().Contains("preserveAspectRatio=\"none\"", StringComparison.Ordinal), "VisualGrid HTML stretch mode should remove embedded SVG aspect-ratio locking.");

        var strip = VisualGrid.CreateMetricStrip("Endpoint", new[] {
            MetricCard.Create().Metric("CPU", "38%").MiniSparkline(new[] { 48d, 42d, 38d }),
            MetricCard.Create().Metric("Memory", "71%").MiniBars(new[] { 55d, 63d, 71d }, maximum: 100)
        }, columns: 2);
        Assert(strip.ToSvg("visual-grid-metric-strip").Contains("Endpoint", StringComparison.Ordinal), "Metric strip preset should render the section title.");
        Assert(strip.ToHtmlFragment().Contains("--cfx-visual-grid-columns:2", StringComparison.Ordinal), "Metric strip preset should honor the requested column count.");

        var autoGrid = VisualGrid.Create()
            .Columns(2)
            .Add(MetricCard.Create().Metric("Small", 1).Size(260, 140))
            .Add(MetricCard.Create().Metric("Large", 2).Size(420, 260));
        var autoHtml = autoGrid.ToHtmlPage();
        Assert(autoHtml.Contains("--cfx-visual-grid-panel-width:420px", StringComparison.Ordinal), "Auto-sized VisualGrid HTML should emit the computed max panel width used by SVG/PNG layout.");
        Assert(autoHtml.Contains("--cfx-visual-grid-panel-height:260px", StringComparison.Ordinal), "Auto-sized VisualGrid HTML should emit the computed max panel height used by SVG/PNG layout.");
        Assert(autoHtml.Contains(".chartforgex-visual-grid-panel svg{width:auto;height:auto", StringComparison.Ordinal), "Auto-sized VisualGrid HTML should preserve child SVG intrinsic sizes.");
        Assert(!autoHtml.Contains("chartforgex-visual-grid has-fixed-panels", StringComparison.Ordinal), "Auto-sized VisualGrid HTML should not force fixed-panel child scaling.");

        var adaptiveGrid = VisualGrid.Create()
            .Columns(2)
            .Gap(20)
            .AdaptiveRowHeights()
            .Add(SectionHeaderBlock.Create().Title("Today's Goals").Size(760, 44).ShowCard(false).TransparentBackground(), columnSpan: 2)
            .Add(MetricCard.Create().Metric("Short", 1).Size(360, 140))
            .Add(MetricCard.Create().Metric("Tall", 2).Size(360, 300));
        var adaptiveSvg = adaptiveGrid.ToSvg("visual-grid-adaptive-rows");
        var adaptiveHtml = adaptiveGrid.ToHtmlFragment();
        Assert(adaptiveSvg.Contains("data-cfx-role=\"section-header-title\"", StringComparison.Ordinal), "Adaptive VisualGrid rows should compose natural section headers.");
        Assert(adaptiveHtml.Contains("--cfx-visual-grid-panel-height:auto", StringComparison.Ordinal), "Adaptive VisualGrid HTML should let natural row heights drive layout.");
        Assert(adaptiveGrid.ToPng().Length > 64, "Adaptive VisualGrid rows should render PNG output.");
    }

    private static void VisualBlocksRejectInvalidInputsCloseToCaller() {
        AssertThrows<InvalidOperationException>(() => ChartTable.Create().AddRow("orphan"), "ChartTable should require columns before rows.");
        AssertThrows<ArgumentException>(() => ChartTable.Create().Columns(), "ChartTable should reject empty column sets.");
        AssertThrows<ArgumentException>(() => ChartTable.Create().Columns("A").AddRow("a", "b"), "ChartTable should reject row values that do not match columns.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartTable.Create().AddColumn("Bad", (TextAlignment)999), "ChartTable columns should reject unknown alignments.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTableColumn("Bad", width: -1), "ChartTableColumn should reject invalid widths even when constructed directly.");
        AssertThrows<InvalidOperationException>(() => ChartTable.Create().Columns("A").AddRow("a").AddColumn("B"), "ChartTable should reject adding columns after rows exist.");
        AssertThrows<ArgumentNullException>(() => new ChartTableCell("ok").Text = null!, "ChartTable cells should reject null text through the public setter.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTableCell("bad").Alignment = (TextAlignment)999, "ChartTable cells should reject unknown alignment overrides.");
        AssertThrows<ArgumentException>(() => new ChartTableCell("bad").MiniBars(Array.Empty<double>()), "ChartTable cell mini bars should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTableCell("bad").MiniBars(new[] { double.NaN }), "ChartTable cell mini bars should reject non-finite values.");
        AssertThrows<InvalidOperationException>(() => ChartTable.Create().Columns("A").AddRow("a").UpdateRow(0, row => row.Cells[0].Sparkline(new[] { 1d })).ToSvg(), "ChartTable cell sparklines should require at least two values.");
        AssertThrows<InvalidOperationException>(() => ChartTable.Create().Columns("A").AddRow("a").UpdateRow(0, row => row.Cells[0].MiniBars(new[] { 1d }, minimum: 2, maximum: 1)).ToSvg(), "ChartTable cell microvisual bounds should require maximum greater than minimum.");
        var excessiveMicroVisualValues = new double[513];
        for (var i = 0; i < excessiveMicroVisualValues.Length; i++) excessiveMicroVisualValues[i] = i;
        AssertThrows<InvalidOperationException>(() => ChartTable.Create().Columns("A").AddRow("a").UpdateRow(0, row => row.Cells[0].MiniBars(excessiveMicroVisualValues)).ToSvg(), "ChartTable cell microvisuals should reject excessive point counts.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTableCell("bad").MicroVisualKind = (ChartTableCellMicroVisualKind)999, "ChartTable cells should reject unknown microvisual kinds.");
        AssertThrows<InvalidOperationException>(() => ChartTable.Create().Columns("A").AddRow("a").UpdateRow(0, row => row.Cells[0].Badge(new string('x', 25))).ToSvg(), "ChartTable cell badges should stay compact.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTableCell("bad").BadgeStyle = (VisualBadgeStyle)999, "ChartTable cells should reject unknown badge styles.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartList.Create().Marker = (VisualListMarker)999, "ChartList marker property should reject unknown marker values.");
        AssertThrows<ArgumentNullException>(() => new ChartListItem("ok").Text = null!, "ChartList items should reject null text through the public setter.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().BadgePlacement = (MetricCardBadgePlacement)999, "MetricCard should reject unknown badge placements.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().MicroVisualPlacement = (MetricCardMicroVisualPlacement)999, "MetricCard should reject unknown micro visual placements.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().MiniSparklineStyle = (ChartLineAreaForm)999, "MetricCard should reject unknown mini sparkline styles.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().MicroVisualSurface = (MetricCardMicroVisualSurface)999, "MetricCard should reject unknown micro visual surfaces.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().Metric("Bad", 1, unit: new string('x', 25)).ToSvg(), "MetricCard units should stay compact.");
        AssertThrows<ArgumentException>(() => MetricCard.Create().MiniBars(Array.Empty<double>()), "MetricCard mini bars should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().MiniBars(new[] { double.NaN }), "MetricCard mini bars should reject non-finite values.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().Metric("Bad", 1).MiniBars(new[] { 1d }, minimum: 2, maximum: 1).ToSvg(), "MetricCard mini bars should require maximum greater than minimum.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().Metric("Bad", 1).MiniBars(new[] { 1d }, highlightIndex: 2).ToSvg(), "MetricCard mini bar highlight index should reference an existing value.");
        AssertThrows<ArgumentException>(() => MetricCard.Create().MiniSparkline(new[] { 1d }), "MetricCard mini sparklines should require at least two values.");
        AssertThrows<ArgumentOutOfRangeException>(() => MetricCard.Create().MiniSparkline(new[] { 1d, double.PositiveInfinity }), "MetricCard mini sparklines should reject non-finite values.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().Metric("Bad", 1).MiniSparkline(new[] { 1d, 2d }, minimum: 3, maximum: 2).ToSvg(), "MetricCard mini sparklines should require maximum greater than minimum.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().Metric("Bad", 1).MiniSparkline(new[] { 1d, 2d }).SecondaryMiniSparkline(new[] { 1d, 2d, 3d }).ToSvg(), "MetricCard secondary mini sparklines should match the primary count.");
        AssertThrows<InvalidOperationException>(() => MetricCard.Create().Metric("Missing", null).ToSvg(), "MetricCard should require a visible value.");
        AssertThrows<InvalidOperationException>(() => RadialMetricCard.Create().Metric("Missing", "1").ToSvg(), "RadialMetricCard should require at least one layer.");
        AssertThrows<InvalidOperationException>(() => RadialMetricCard.Create().AddLayer("Bad", 1, configure: _ => null!).ToSvg(), "RadialMetricCard should reject null fluent layer configuration results.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddLayeredRadial("Bad", _ => null!), "Layered radial builder should reject null fluent layer configuration results.");
        AssertThrows<InvalidOperationException>(() => SegmentedMetricBlock.Create().ToSvg(), "SegmentedMetricBlock should require at least one item.");
        AssertThrows<InvalidOperationException>(() => SegmentedMetricBlock.Create().AddItem("Bad", -1).ToSvg(), "SegmentedMetricBlock should reject negative values.");
        AssertThrows<InvalidOperationException>(() => SegmentedMetricBlock.Create().AddItem("Bad", 1, maximum: 0).ToSvg(), "SegmentedMetricBlock should require a positive maximum for progress rows.");
        AssertThrows<InvalidOperationException>(() => SegmentedMetricBlock.Create().AddItem("Bad", 1, segments: 0).ToSvg(), "SegmentedMetricBlock should reject invalid progress segment counts.");
        AssertThrows<InvalidOperationException>(() => SegmentedMetricBlock.Create().AddItem("Bad", 1).HeaderSymbol("longer").ToSvg(), "SegmentedMetricBlock header symbols should stay compact.");
        AssertThrows<InvalidOperationException>(() => SegmentedMetricBlock.Create().AddItem(new SegmentedMetricItem("Bad", 1).DisplayValue(new string('x', 37))).ToSvg(), "SegmentedMetricBlock display values should stay compact.");
        AssertThrows<InvalidOperationException>(() => SegmentedMetricBlock.Create(SegmentedMetricStyle.CompositionStrip).AddItem("Zero", 0).ToSvg(), "SegmentedMetricBlock composition styles should require metric text.");
        AssertThrows<InvalidOperationException>(() => SegmentedMetricBlock.Create(SegmentedMetricStyle.CompositionStrip).Metric("Tasks", 0).ToSvg(), "SegmentedMetricBlock composition styles should require at least one item.");
        AssertThrows<InvalidOperationException>(() => SegmentedMetricBlock.Create(SegmentedMetricStyle.CompositionStrip).Metric("Tasks", 0).AddItem("Bad", -1).ToSvg(), "SegmentedMetricBlock should reject negative composition values.");
        AssertThrows<InvalidOperationException>(() => SegmentedMetricBlock.Create(SegmentedMetricStyle.CompositionStrip).Metric("Tasks", 0).AddItem("Zero", 0).ToSvg(), "SegmentedMetricBlock composition styles should require at least one positive item value.");
        AssertThrows<InvalidOperationException>(() => SegmentedMetricBlock.Create(SegmentedMetricStyle.CapsuleLoop).AddItem("Zero", 0).ToSvg(), "SegmentedMetricBlock capsule loops should require at least one positive item value.");
        AssertThrows<InvalidOperationException>(() => SegmentedMetricBlock.Create(SegmentedMetricStyle.FunnelColumns).AddItem("Bad", 1, segments: 0).ToSvg(), "SegmentedMetricBlock funnel columns should reject invalid segment counts.");
        AssertThrows<ArgumentOutOfRangeException>(() => new SegmentedMetricItem("Bad", 1).Pattern = (ChartFillPattern)999, "SegmentedMetricItem should reject unknown fill patterns.");
        AssertThrows<ArgumentOutOfRangeException>(() => SegmentedMetricBlock.Create().Style = (SegmentedMetricStyle)999, "SegmentedMetricBlock should reject unknown styles.");
        AssertThrows<InvalidOperationException>(() => SegmentedMetricBlock.Create(SegmentedMetricStyle.DistributionRows).Metric("Net", 0).AddItem("Symbol", 1, symbol: new string('x', 9)).ToSvg(), "SegmentedMetricBlock should keep row symbols compact.");
        AssertThrows<InvalidOperationException>(() => HeatmapInsightCard.Create().ToSvg(), "HeatmapInsightCard should require columns and rows.");
        AssertThrows<InvalidOperationException>(() => HeatmapInsightCard.Create().Columns("A", "B").AddRow("Bad", 1).ToSvg(), "HeatmapInsightCard rows should match the column count.");
        AssertThrows<InvalidOperationException>(() => HeatmapInsightCard.Create().Columns("A").Minimum(2).Maximum(1).AddRow("Bad", 1).ToSvg(), "HeatmapInsightCard should require a valid color key range.");
        AssertThrows<InvalidOperationException>(() => DateStripBlock.Create().ToSvg(), "DateStripBlock should require items.");
        AssertThrows<InvalidOperationException>(() => DateStripBlock.Create().AddItem("", "1").ToSvg(), "DateStripBlock should require item labels.");
        AssertThrows<InvalidOperationException>(() => DateStripBlock.Create().AddItem("Monday and too long", "1").ToSvg(), "DateStripBlock item labels should stay compact.");
        AssertThrows<InvalidOperationException>(() => EntityStripBlock.Create().ToSvg(), "EntityStripBlock should require items.");
        AssertThrows<InvalidOperationException>(() => EntityStripBlock.Create().AddItem("", "A").ToSvg(), "EntityStripBlock should require item labels.");
        AssertThrows<InvalidOperationException>(() => EntityStripBlock.Create().AddItem("Bad", "TOOLONG").ToSvg(), "EntityStripBlock avatar text should stay compact.");
        AssertThrows<InvalidOperationException>(() => SectionHeaderBlock.Create().ToSvg(), "SectionHeaderBlock should require a title.");
        AssertThrows<InvalidOperationException>(() => WorkloadListBlock.Create().ToSvg(), "WorkloadListBlock should require rows.");
        AssertThrows<InvalidOperationException>(() => WorkloadListBlock.Create().AddPerson("", "Role", 1).ToSvg(), "WorkloadListBlock should require row labels.");
        AssertThrows<InvalidOperationException>(() => WorkloadListBlock.Create().AddPerson("Bad", "Role", -1).ToSvg(), "WorkloadListBlock should reject negative values.");
        AssertThrows<InvalidOperationException>(() => WorkloadListBlock.Create().AddPerson("Bad", "Role", 1, 0).ToSvg(), "WorkloadListBlock should reject non-positive maximum values.");
        AssertThrows<InvalidOperationException>(() => ActivityTimelineBlock.Create().ToSvg(), "ActivityTimelineBlock should require items.");
        AssertThrows<InvalidOperationException>(() => ActivityTimelineBlock.Create().AddEvent("").ToSvg(), "ActivityTimelineBlock should require item text.");
        AssertThrows<InvalidOperationException>(() => ActivityTimelineBlock.Create().AddHiddenSummary(-1, "items").ToSvg(), "ActivityTimelineBlock should reject negative hidden counts.");
        AssertThrows<InvalidOperationException>(() => ActivityTimelineBlock.Create().AddEvent("Bad", symbol: "TOOLONG").ToSvg(), "ActivityTimelineBlock item symbols should stay compact.");
        AssertThrows<ArgumentOutOfRangeException>(() => ActivityTimelineItem.Event("Bad", null, Tone.Neutral, null, null).Kind = (ActivityTimelineItemKind)999, "ActivityTimelineItem kind property should reject unknown kinds.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().ToSvg(), "ScheduleTimelineBlock should require events.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().Start(9).End(8).TickInterval(1).AddEvent("Bad", 8, 9).ToSvg(), "ScheduleTimelineBlock should reject inverted ranges.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().Start(8).End(17).TickInterval(0).AddEvent("Bad", 8, 9).ToSvg(), "ScheduleTimelineBlock should reject non-positive tick intervals.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().Start(0).End(511.8).TickInterval(1).AddEvent("Bad", 0, 1).ToSvg(), "ScheduleTimelineBlock should validate the rendered tick count bound.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().AddEvent("", 8, 9).ToSvg(), "ScheduleTimelineBlock should require event titles.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().AddEvent("Bad", 9, 8).ToSvg(), "ScheduleTimelineBlock should reject inverted events.");
        AssertThrows<InvalidOperationException>(() => ScheduleTimelineBlock.Create().AddEvent("Bad", 8, 9, lane: -1).ToSvg(), "ScheduleTimelineBlock should reject negative lanes.");
        AssertThrows<ArgumentOutOfRangeException>(() => VisualGrid.Create().Columns(0), "VisualGrid should reject non-positive column counts.");
        AssertThrows<ArgumentOutOfRangeException>(() => VisualGrid.Create().PanelFit = (VisualPanelFit)999, "VisualGrid panel fit property should reject unknown values.");
        AssertThrows<InvalidOperationException>(() => VisualGrid.Create().ToSvg(), "VisualGrid should require at least one item.");
        AssertThrows<ArgumentException>(() => VisualGrid.CreateMetricStrip("Empty", Array.Empty<MetricCard>()), "Metric strip preset should require at least one card.");
        AssertThrows<ArgumentException>(() => VisualGrid.CreateMetricStrip("Bad", new MetricCard[] { null! }), "Metric strip preset should reject null cards.");
    }
}
