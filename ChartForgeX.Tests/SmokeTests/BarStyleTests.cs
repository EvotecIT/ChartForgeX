using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SegmentedCapsuleBarsRenderAsReusableStyle() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithTheme(ChartTheme.DashboardLight())
            .WithPalette("#FBBF24", "#22C55E", "#3B82F6", "#8B5CF6")
            .WithStackedBars()
            .WithDashboardBarStyle()
            .WithFocusedXAxisCategory(2, paletteIndex: 3)
            .WithXLabels("Sun", "Mon", "Tue")
            .AddBar("Applied", Points(20, 24, 31))
            .AddBar("Reviewed", Points(28, 22, 18))
            .AddBar("Saved", Points(38, 25, 30))
            .AddBar("Outreach", Points(18, 24, 15));

        var svg = chart.ToSvg();
        Assert(chart.Options.BarStyle == ChartBarStyle.SegmentedCapsule, "Bar style should be stored on renderer-independent chart options.");
        Assert(chart.Options.BarVisualStyle.CapThickness == 7, "Reusable bar style presets should carry cap sizing tokens.");
        Assert(chart.Options.BarVisualStyle.CornerRadius == 0, "Dashboard capsule bodies should stay square so stacked segment joins do not create rounded connector bulges.");
        Assert(chart.Options.BarVisualStyle.CapShadowSpread == 3 && chart.Options.BarVisualStyle.CapHighlightOpacity > 0, "Dashboard capsule style should carry premium cap lighting tokens.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().Options.BarStyle = (ChartBarStyle)999, "Bar style should reject invalid enum values instead of silently falling back to solid bars.");
        var segmentedGeometry = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX", "Rendering", "ChartSegmentedBarGeometry.cs"));
        Assert(segmentedGeometry.Contains("Vertical(", StringComparison.Ordinal) && segmentedGeometry.Contains("Horizontal(", StringComparison.Ordinal) && segmentedGeometry.Contains("RangeCap(", StringComparison.Ordinal), "Segmented capsule geometry should stay shared across vertical, horizontal, and range bars.");
        var segmentedSvg = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX", "Svg", "SvgChartRenderer.SegmentedBars.cs"));
        Assert(segmentedSvg.Contains("WriteSvgSegmentedCapLayers", StringComparison.Ordinal) && segmentedSvg.Contains("rolePrefix + \"-cap-highlight\"", StringComparison.Ordinal), "Segmented capsule SVG cap layers should stay centralized across bar renderers.");
        Assert(!chart.Options.ShowCard && !chart.Options.ShowPlotBackground && !chart.Options.ShowAxisLines, "Dashboard bar style should remove default chart chrome without hiding axes or legends.");
        Assert(svg.Contains("fill=\"#8B5CF6\"", StringComparison.Ordinal), "Charts should render reusable x-axis label highlights from theme palette tokens.");
        Assert(svg.Contains("fill=\"#FBBF24\"", StringComparison.Ordinal) && svg.Contains("fill=\"#8B5CF6\"", StringComparison.Ordinal), "Dashboard bar styles should compose with reusable palette tokens instead of one-off mark colors.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"annotation-line\"") == 2, "Focused x-axis categories should render reusable boundary guide lines.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bar\"") == 12, "Segmented capsule style should preserve bar metadata roles.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bar-cap-shadow-soft\"") == 12, "Segmented capsule bars should render a softer premium cap shadow layer.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bar-cap\"") == 12, "Segmented capsule bars should render one value-edge cap per segment.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bar-cap-highlight\"") == 12, "Segmented capsule bars should render cap highlight sheen.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"bar-cap-shadow\"") == 12, "Segmented capsule bars should render soft cap shadows.");
        Assert(svg.Contains("opacity=\"0.22\"", StringComparison.Ordinal), "Segmented capsule bars should render translucent segment bodies from style tokens.");
        Assert(svg.Contains("stroke-dasharray=\"4 6\"", StringComparison.Ordinal), "Dashboard grid styles should render dashed guide lines.");
        var axisHighlightOptions = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX", "Core", "ChartOptions.AxisLabels.cs"));
        Assert(axisHighlightOptions.Contains("AxisValueEquals", StringComparison.Ordinal), "X-axis label highlights should resolve generated tick values with a floating-point tolerance.");
        Assert(ChartOptions.AxisValueEquals(2, 2 + 5e-10) && !ChartOptions.AxisValueEquals(1_000_000_000_000, 1_000_000_000_000 + 0.01), "X-axis label highlight tolerance should cover generated tick drift without bleeding across dense high-magnitude axes.");
        var highlightedRangeChart = Chart.Create()
            .WithXLabels("8am", "9am", "10am", "11am")
            .WithHighlightedXAxisRange(1.5, 3.5, ChartColor.FromHex("#DE442F"), 0.08, "peak-window")
            .AddBar("Reviews", Points(3, 9, 10, 4));
        var highlightedRangeSvg = highlightedRangeChart.ToSvg();
        Assert(highlightedRangeSvg.Contains("data-cfx-role=\"annotation-band\" data-cfx-kind=\"vertical-band\" data-cfx-value=\"1.5\" data-cfx-end=\"3.5\" data-cfx-label=\"peak-window\"", StringComparison.Ordinal), "Highlighted x-axis ranges should render selected-window metadata as annotation bands.");
        Assert(highlightedRangeChart.Options.XAxisLabelHighlights.Count == 2 && highlightedRangeChart.Options.XAxisLabelHighlights.ContainsKey(2) && highlightedRangeChart.Options.XAxisLabelHighlights.ContainsKey(3), "Highlighted x-axis ranges should color explicit labels inside the selected window.");
        Assert(highlightedRangeChart.ClearHighlightedXAxisLabels().Options.XAxisLabelHighlights.Count == 0 && highlightedRangeChart.Annotations.Count == 0, "Clearing x-axis highlights should remove selected-window bands.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithHighlightedXAxisRange(1, 2, paletteIndex: -1), "Palette-based x-axis range highlights should reject negative palette indexes.");
        var pointRange = Chart.Create().AddBar("Peak", Points(3, 6, 9, 4)).Series[0].WithPointColorRange(1, 2, ChartColor.FromHex("#DE442F"));
        Assert(pointRange.PointColors.Count == 3 && pointRange.PointColors[1]!.Value.ToHex() == "#DE442F" && pointRange.PointColors[2]!.Value.ToHex() == "#DE442F", "Point color ranges should support compact highlighted bar windows.");
        AssertThrows<ArgumentOutOfRangeException>(() => pointRange.WithPointColorRange(2, 0, ChartColor.FromHex("#DE442F")), "Point color ranges should reject empty ranges.");
        AssertThrows<ArgumentOutOfRangeException>(() => pointRange.WithPointColorRange(3, 2, ChartColor.FromHex("#DE442F")), "Point color ranges should reject ranges outside the series.");
        var clearedFocus = Chart.Create().WithFocusedXAxisCategory(2, paletteIndex: 1).ClearHighlightedXAxisLabels();
        Assert(clearedFocus.Options.XAxisLabelHighlights.Count == 0 && clearedFocus.Annotations.Count == 0, "Clearing x-axis label highlights should also clear focus guide annotations.");
        var keptManualGuide = Chart.Create()
            .AddVerticalLine(1.5, "", ChartColor.FromHex("#111827"))
            .WithFocusedXAxisCategory(2, halfWidth: 0.5, color: ChartColor.FromHex("#8B5CF6"))
            .ClearHighlightedXAxisLabels();
        Assert(keptManualGuide.Annotations.Count == 1 && Math.Abs(keptManualGuide.Annotations[0].Value - 1.5) < 1e-9, "Clearing x-axis focus should preserve caller-added unlabeled vertical annotations even at the same value.");

        var trendSvg = Chart.Create()
            .WithSize(420, 260)
            .WithTheme(ChartTheme.DashboardLight())
            .WithDashboardCartesianStyle()
            .AddSmoothArea("Saved", Points(10, 24, 32), ChartColor.FromHex("#22C55E"))
            .ToSvg();
        Assert(Chart.Create().WithDashboardCartesianStyle().Options.BarStyle == ChartBarStyle.Solid, "Dashboard cartesian style should not force bar mark styling.");
        Assert(trendSvg.Contains("stroke-dasharray=\"4 6\"", StringComparison.Ordinal), "Dashboard cartesian style should be reusable by non-bar charts.");
        Assert(trendSvg.Contains("data-cfx-role=\"area-line-highlight\"", StringComparison.Ordinal), "Dashboard cartesian line and area charts should pick up shared premium stroke layers.");
        var panel = Chart.Create().WithDashboardBarPanelStyle();
        Assert(panel.Options.ShowCard && !panel.Options.ShowPlotBackground && !panel.Options.ShowAxisLines && panel.Options.BarStyle == ChartBarStyle.SegmentedCapsule, "Dashboard panel style should preserve premium card composition without forcing inner plot chrome.");
        var panelSvg = Chart.Create().WithSize(320, 200).WithTheme(ChartTheme.DashboardLight()).WithDashboardPanelStyle().AddLine("A", Points(10, 20)).ToSvg();
        Assert(CountOccurrences(panelSvg, "<feDropShadow") == 2 && panelSvg.Contains("dy=\"4\"", StringComparison.Ordinal) && panelSvg.Contains("stdDeviation=\"6\"", StringComparison.Ordinal) && panelSvg.Contains("dy=\"14\"", StringComparison.Ordinal) && panelSvg.Contains("stdDeviation=\"18\"", StringComparison.Ordinal) && panelSvg.Contains("flood-color=\"#0F172A\"", StringComparison.Ordinal), "SVG dashboard panels should use layered premium card shadow primitives.");
        Assert(panelSvg.Contains("data-cfx-role=\"card-surface\"", StringComparison.Ordinal) && panelSvg.Contains("data-cfx-role=\"card-border\"", StringComparison.Ordinal), "SVG dashboard panels should expose reusable card shell roles.");
        var trendPanel = Chart.Create()
            .WithSize(520, 300)
            .WithTheme(ChartTheme.DashboardLight())
            .WithDashboardTrendPanelStyle(showLegend: true)
            .WithXLabels("Jan", "Feb", "Mar", "Apr")
            .AddSmoothLine("On-time", Points(21, 23, 25, 37), ChartColor.FromHex("#7057E6"))
            .AddSmoothLine("Absent", Points(18, 22, 17, 14), ChartColor.FromHex("#5FD3D9"))
            .WithDashboardTrendFocus(4, 37, "Apr", ChartColor.FromHex("#7057E6"), ChartDataLabelPlacement.Right);
        var trendPanelSvg = trendPanel.ToSvg();
        Assert(trendPanel.Options.ShowCard && trendPanel.Options.ShowLegend && !trendPanel.Options.ShowYAxis && !trendPanel.Options.ShowAxisLines, "Dashboard trend panels should preserve card composition with compact axes.");
        Assert(trendPanelSvg.Contains("data-cfx-role=\"point-callout-label\"", StringComparison.Ordinal), "Dashboard trend focus should render a point callout label.");
        Assert(trendPanelSvg.Contains("data-cfx-role=\"annotation-line\" data-cfx-kind=\"vertical-line\" data-cfx-value=\"4\" data-cfx-label=\"Apr\"", StringComparison.Ordinal), "Dashboard trend focus should render crosshair marker metadata.");
        Assert(trendPanelSvg.Contains("data-cfx-role=\"line-highlight\"", StringComparison.Ordinal), "Dashboard trend panels should reuse premium line highlight layers.");
        AssertThrows<ArgumentNullException>(() => Chart.Create().WithDashboardTrendFocus(1, 2, null!), "Dashboard trend focus should reject null labels.");

        var horizontalSvg = Chart.Create()
            .WithSize(640, 360)
            .WithStackedHorizontalBars()
            .WithBarStyle(ChartBarStyle.SegmentedCapsule)
            .WithHighlightedXAxisLabel(50, ChartColor.FromHex("#E11D48"))
            .WithXLabels("Alpha", "Beta")
            .AddHorizontalBar("Done", Points(32, 45), ChartColor.FromHex("#22C55E"))
            .AddHorizontalBar("Open", Points(18, 22), ChartColor.FromHex("#8B5CF6"))
            .ToSvg();
        Assert(CountOccurrences(horizontalSvg, "data-cfx-role=\"horizontal-bar-cap\"") == 4, "Segmented capsule style should also apply to horizontal bars.");
        Assert(CountOccurrences(horizontalSvg, "data-cfx-role=\"horizontal-bar-cap-highlight\"") == 4, "Segmented capsule style should apply premium cap highlights to horizontal bars.");
        Assert(horizontalSvg.Contains("fill=\"#E11D48\"", StringComparison.Ordinal), "SVG horizontal-bar x-axis labels should honor highlighted x-axis label colors.");
        var horizontalPanelSvg = Chart.Create()
            .WithSize(640, 240)
            .WithPadding(160, 34, 26, 34)
            .WithDashboardBarPanelStyle()
            .WithStackedHorizontalBars()
            .WithXAxisVisible(false)
            .WithXLabels("Alpha", "Beta", "Gamma", "Delta")
            .AddHorizontalBar("Done", Points(32, 45, 28, 36), ChartColor.FromHex("#22C55E"))
            .AddHorizontalBar("Open", Points(18, 22, 31, 20), ChartColor.FromHex("#8B5CF6"))
            .ToSvg();
        var horizontalPanelYPositions = ExtractHorizontalBarYPositions(horizontalPanelSvg);
        Assert(horizontalPanelYPositions.Length == 8 && horizontalPanelYPositions.Max() - horizontalPanelYPositions.Min() > 80, "Dashboard horizontal bars should use left padding for category labels and preserve usable vertical row spacing.");
        var stackedRowSvg = Chart.Create()
            .WithSize(640, 300)
            .WithTheme(ChartTheme.DashboardLight())
            .WithDashboardStackedRowStyle(showTotals: true)
            .WithXLabels("Engineering", "Maintenance", "HSEQ")
            .AddHorizontalBar("All employee", Points(68, 62, 74), ChartColor.FromHex("#7057E6"))
            .AddHorizontalBar("Terminated", Points(25, 28, 24), ChartColor.FromHex("#5FD3D9"))
            .AddHorizontalBar("New hires", Points(14, 12, 15), ChartColor.FromHex("#FFB05C"))
            .ToSvg();
        Assert(Chart.Create().WithDashboardStackedRowStyle(showTotals: true).Options.ShowStackTotals, "Dashboard stacked row style should enable trailing totals when requested.");
        Assert(Chart.Create().WithDashboardStackedRowStyle(showLegend: false).Options.ShowLegend == false, "Dashboard stacked row style should make inline legends optional.");
        Assert(stackedRowSvg.Contains("data-cfx-role=\"horizontal-bar-cap\"", StringComparison.Ordinal), "Dashboard stacked rows should reuse segmented horizontal bar caps.");
        Assert(stackedRowSvg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Dashboard stacked rows should render trailing totals through existing data-label primitives.");
        Assert(!stackedRowSvg.Contains("data-cfx-role=\"x-axis\"", StringComparison.Ordinal), "Dashboard stacked rows should hide the numeric x-axis by default.");

        var rangeSvg = Chart.Create()
            .WithSize(640, 360)
            .WithBarVisualStyle(ChartBarVisualStyle.DashboardCapsule())
            .AddRangeBar("Window", new[] {
                new ChartInterval(1, 20, 42),
                new ChartInterval(2, 30, 70)
            }, ChartColor.FromHex("#3B82F6"))
            .ToSvg();
        Assert(CountOccurrences(rangeSvg, "data-cfx-role=\"range-bar\"") == 2, "Reusable bar styles should preserve range-bar metadata roles.");
        Assert(CountOccurrences(rangeSvg, "data-cfx-role=\"range-bar-cap-shadow\"") == 4, "Range bars should reuse segmented capsule cap shadow tokens.");
        Assert(CountOccurrences(rangeSvg, "data-cfx-role=\"range-bar-cap-highlight\"") == 4, "Range bars should reuse segmented capsule cap highlight tokens.");

        var rangeAreaSvg = Chart.Create()
            .WithSize(420, 260)
            .WithLineVisualStyle(ChartLineVisualStyle.Premium().WithHalo(0.22, 11))
            .AddRangeArea("Band", new[] { new ChartRangeBand(1, 10, 24), new ChartRangeBand(2, 18, 38) }, ChartColor.FromHex("#3B82F6"))
            .ToSvg();
        Assert(rangeAreaSvg.Contains("data-cfx-role=\"range-area-upper-halo\"", StringComparison.Ordinal) && rangeAreaSvg.Contains("stroke-width=\"14\"", StringComparison.Ordinal), "SVG range-area halos should honor reusable line halo width tokens.");
        var transparentStroke = ChartColor.FromRgba(59, 130, 246, 0);
        var transparentLineSvg = Chart.Create().WithSize(420, 260).WithLineVisualStyle(ChartLineVisualStyle.Premium()).AddLine("Hidden", Points(10, 20, 30), transparentStroke).ToSvg();
        var transparentTrendSvg = Chart.Create().WithSize(420, 260).WithLineVisualStyle(ChartLineVisualStyle.Premium()).AddTrendLine("Hidden trend", Points(10, 20, 30), transparentStroke).ToSvg();
        var transparentRangeSvg = Chart.Create().WithSize(420, 260).WithLineVisualStyle(ChartLineVisualStyle.Premium()).AddRangeArea("Hidden band", new[] { new ChartRangeBand(1, 10, 24), new ChartRangeBand(2, 18, 38) }, transparentStroke).ToSvg();
        Assert(!transparentLineSvg.Contains("data-cfx-role=\"line-highlight\"", StringComparison.Ordinal) && !transparentTrendSvg.Contains("data-cfx-role=\"trend-line-highlight\"", StringComparison.Ordinal) && !transparentRangeSvg.Contains("data-cfx-role=\"range-area-upper-highlight\"", StringComparison.Ordinal), "Premium SVG highlight layers should stay hidden when the source series stroke is transparent.");
        var pngCartesian = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX", "Raster", "PngChartRenderer.Cartesian.cs"));
        Assert(!pngCartesian.Contains("Math.Max(24", StringComparison.Ordinal) && !pngCartesian.Contains("Math.Max(10", StringComparison.Ordinal), "PNG premium line halos should honor low opacity style tokens without renderer-specific alpha floors.");
        var sharedLineLayers = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX", "Rendering", "ChartLineVisualLayers.cs"));
        Assert(sharedLineLayers.Contains("HighlightOpacity(color, style)", StringComparison.Ordinal) && pngCartesian.Contains("ChartLineVisualLayers.Build", StringComparison.Ordinal), "PNG premium line highlights should derive opacity from the shared source-stroke-aware line layer model.");
        var pngRenderer = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX", "Raster", "PngChartRenderer.cs"));
        Assert(pngRenderer.Contains("HorizontalValueGridOpacity", StringComparison.Ordinal) && pngRenderer.Contains("HorizontalCategoryGridOpacity", StringComparison.Ordinal), "PNG horizontal-bar grids should preserve tuned default value/category guide emphasis.");
        var compactSegmentedSvg = Chart.Create()
            .WithSize(220, 140)
            .WithYAxisBounds(0, 100000)
            .WithBarStyle(ChartBarStyle.SegmentedCapsule)
            .AddBar("Tiny", Points(1))
            .ToSvg();
        Assert(CountOccurrences(compactSegmentedSvg, "data-cfx-role=\"bar\"") == 1 && compactSegmentedSvg.Contains("height=\"1\"", StringComparison.Ordinal), "Segmented capsule bars should keep tiny non-zero values visible instead of dropping them.");

        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithFocusedXAxisCategory(1, paletteIndex: -1), "Palette-based x-axis focus should reject negative palette indexes.");

        var reusableStyle = ChartBarVisualStyle.DashboardCapsule().WithCapThickness(9).WithBodyOpacity(0.3);
        var reused = Chart.Create().WithBarVisualStyle(reusableStyle);
        reusableStyle.WithCapThickness(3);
        Assert(reused.Options.BarVisualStyle.CapThickness == 9, "Charts should clone reusable bar style instances so later caller changes do not mutate chart output.");

        Assert(chart.Options.Theme.CardBackground.ToHex() == "#FFFFFF" && chart.Options.Theme.Palette[3].ToHex() == "#8B5CF6" && chart.Options.Theme.ShadowOpacity > 0.04 && chart.Options.Theme.ShadowColor.ToHex() == "#0F172A", "Dashboard styles should expose reusable surface, shadow, and palette tokens.");

        var defaultPng = Chart.Create().WithSize(360, 220).WithStackedBars().AddBar("A", Points(20, 24)).AddBar("B", Points(18, 22)).ToPng();
        var segmentedPng = Chart.Create().WithSize(360, 220).WithStackedBars().WithBarVisualStyle(ChartBarVisualStyle.DashboardCapsule()).AddBar("A", Points(20, 24)).AddBar("B", Points(18, 22)).ToPng();
        Assert(defaultPng.Length > 64 && segmentedPng.Length > 64 && !defaultPng.SequenceEqual(segmentedPng), "PNG output should render the segmented capsule style distinctly from default bars.");
        var flatPanelPng = Chart.Create().WithSize(320, 200).WithTheme(ChartTheme.DashboardLight().WithShadowOpacity(0)).WithDashboardPanelStyle().AddLine("A", Points(10, 20)).ToPng();
        var shadowPanelPng = Chart.Create().WithSize(320, 200).WithTheme(ChartTheme.DashboardLight().WithShadowOpacity(0.22)).WithDashboardPanelStyle().AddLine("A", Points(10, 20)).ToPng();
        Assert(!flatPanelPng.SequenceEqual(shadowPanelPng), "PNG dashboard panels should honor theme shadow opacity instead of rendering flat cards.");
        Assert(!shadowPanelPng.SequenceEqual(Chart.Create().WithSize(320, 200).WithTheme(ChartTheme.DashboardLight().WithShadowOpacity(0.22).WithShadowColor(ChartColor.FromHex("#7C3AED"))).WithDashboardPanelStyle().AddLine("A", Points(10, 20)).ToPng()), "PNG dashboard panels should honor theme shadow color.");
    }

    private static double[] ExtractHorizontalBarYPositions(string svg) {
        var values = new List<double>();
        var index = 0;
        const string marker = "<rect data-cfx-role=\"horizontal-bar\"";
        while ((index = svg.IndexOf(marker, index, StringComparison.Ordinal)) >= 0) {
            var y = ExtractSvgDoubleAttribute(svg, index, "y");
            if (y.HasValue) values.Add(y.Value);
            index += marker.Length;
        }

        return values.ToArray();
    }

    private static double? ExtractSvgDoubleAttribute(string svg, int startIndex, string attribute) {
        var index = svg.IndexOf(" " + attribute + "=\"", startIndex, StringComparison.Ordinal);
        if (index < 0) return null;
        index += attribute.Length + 3;
        var end = svg.IndexOf("\"", index, StringComparison.Ordinal);
        if (end < 0) return null;
        return double.Parse(svg.Substring(index, end - index), CultureInfo.InvariantCulture);
    }
}
