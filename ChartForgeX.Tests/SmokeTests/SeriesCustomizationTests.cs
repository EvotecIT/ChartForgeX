using System;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SeriesFluentStylingControlsColorStrokeAndSmoothing() {
        var chart = Chart.Create()
            .WithSize(520, 320)
            .AddLine("Styled", Points(10, 40, 22, 58));
        chart.Series[0]
            .WithColor(ChartColor.FromRgb(236, 72, 153))
            .WithStrokeWidth(6)
            .WithSmooth();

        var svg = chart.ToSvg();
        Assert(svg.Contains("stroke=\"#EC4899\" stroke-width=\"6\"", StringComparison.Ordinal), "Series fluent styling should control SVG stroke color and width.");
        Assert(svg.Contains(" C ", StringComparison.Ordinal), "Series fluent smoothing should render Bezier paths for capable series.");
        Assert(chart.ToPng().Length > 64, "Series fluent styling should render PNG output.");

        chart.Series[0].UseThemeColor().WithSmooth(false);
        var unsmoothed = chart.ToSvg();
        Assert(!unsmoothed.Contains("stroke=\"#EC4899\" stroke-width=\"6\"", StringComparison.Ordinal), "Theme color reset should clear explicit series colors.");
        Assert(!unsmoothed.Contains(" C ", StringComparison.Ordinal), "Series smoothing can be disabled fluently.");
    }

    private static void SeriesFillPatternsRenderForFilledMarks() {
        var chart = Chart.Create()
            .WithSize(540, 320)
            .AddBar("Patterned", Points(12, 44, 26), ChartColor.FromHex("#F97316"));
        chart.Series[0]
            .WithFillPattern(ChartFillPattern.DiagonalBackward)
            .WithPointFillPattern(1, ChartFillPattern.Crosshatch);
        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"bar-pattern\"", StringComparison.Ordinal), "Bar fill patterns should render SVG pattern overlays.");
        Assert(svg.Contains("data-cfx-fill-pattern=\"Crosshatch\"", StringComparison.Ordinal), "Point fill patterns should override the series pattern in SVG metadata.");
        Assert(chart.ToPng().Length > 64, "Bar fill patterns should render PNG output.");
        var segmented = Chart.Create()
            .WithSize(540, 320)
            .WithBarStyle(ChartBarStyle.SegmentedCapsule)
            .AddBar("Patterned", Points(12, 44, 26), ChartColor.FromHex("#F97316"));
        segmented.Series[0].WithFillPattern(ChartFillPattern.DiagonalBackward);
        Assert(segmented.ToSvg().Contains("data-cfx-role=\"bar-pattern\"", StringComparison.Ordinal), "Segmented bar styles should preserve SVG fill pattern overlays.");
        var segmentedPlainPng = Chart.Create()
            .WithSize(540, 320)
            .WithBarStyle(ChartBarStyle.SegmentedCapsule)
            .AddBar("Patterned", Points(12, 44, 26), ChartColor.FromHex("#F97316"))
            .ToPng();
        Assert(!segmented.ToPng().SequenceEqual(segmentedPlainPng), "Segmented bar styles should preserve PNG fill pattern rendering.");

        var horizontal = Chart.Create()
            .WithSize(540, 320)
            .WithXLabels("A", "B", "C")
            .AddHorizontalBar("Patterned", Points(12, 44, 26), ChartColor.FromHex("#14B8A6"));
        horizontal.Series[0].WithFillPattern(ChartFillPattern.DiagonalForward);
        Assert(horizontal.ToSvg().Contains("data-cfx-role=\"horizontal-bar-pattern\"", StringComparison.Ordinal), "Horizontal bars should render fill pattern overlays.");
        Assert(horizontal.ToPng().Length > 64, "Horizontal bar fill patterns should render PNG output.");
        horizontal.WithBarStyle(ChartBarStyle.SegmentedCapsule);
        Assert(horizontal.ToSvg().Contains("data-cfx-role=\"horizontal-bar-pattern\"", StringComparison.Ordinal), "Segmented horizontal bars should preserve SVG fill pattern overlays.");
        Assert(horizontal.ToPng().Length > 64, "Segmented horizontal bar fill patterns should render PNG output.");

        var range = Chart.Create()
            .WithSize(540, 320)
            .AddRangeBar("Patterned", new[] {
                new ChartInterval(1, 20, 42),
                new ChartInterval(2, 30, 55)
            }, ChartColor.FromHex("#8B5CF6"));
        range.Series[0].WithFillPattern(ChartFillPattern.DiagonalBackward);
        Assert(range.ToSvg().Contains("data-cfx-role=\"range-bar-pattern\"", StringComparison.Ordinal), "Range bars should render fill pattern overlays.");
        Assert(range.ToPng().Length > 64, "Range-bar fill patterns should render PNG output.");
        range.WithBarStyle(ChartBarStyle.SegmentedCapsule);
        Assert(range.ToSvg().Contains("data-cfx-role=\"range-bar-pattern\"", StringComparison.Ordinal), "Segmented range bars should preserve SVG fill pattern overlays.");
        var segmentedRangePlainPng = Chart.Create()
            .WithSize(540, 320)
            .WithBarStyle(ChartBarStyle.SegmentedCapsule)
            .AddRangeBar("Patterned", new[] {
                new ChartInterval(1, 20, 42),
                new ChartInterval(2, 30, 55)
            }, ChartColor.FromHex("#8B5CF6"))
            .ToPng();
        Assert(!range.ToPng().SequenceEqual(segmentedRangePlainPng), "Segmented range bars should preserve PNG fill pattern rendering.");

        chart.Series[0].UseSeriesFillPattern(1).UseSolidFill();
        Assert(!chart.ToSvg().Contains("data-cfx-role=\"bar-pattern\"", StringComparison.Ordinal), "Fill patterns can be cleared fluently.");
        AssertThrows<ArgumentOutOfRangeException>(() => chart.Series[0].WithFillPattern((ChartFillPattern)999), "Series fill patterns should reject unknown values.");
        AssertThrows<ArgumentOutOfRangeException>(() => chart.Series[0].WithPointFillPattern(99, ChartFillPattern.DiagonalForward), "Point fill patterns should reject missing point indexes.");
        AssertThrows<ArgumentOutOfRangeException>(() => chart.Series[0].WithPointFillPattern(0, (ChartFillPattern)999), "Point fill patterns should reject unknown values.");
    }

    private static void SeriesDataLabelStylesOverrideChartDefaults() {
        var chart = Chart.Create()
            .WithSize(520, 320)
            .WithDataLabels()
            .WithDataLabelStyle(style => style.WithColor("#64748b"))
            .AddBar("Styled labels", Points(10, 40, 22, 58));
        chart.Series[0].WithDataLabelStyle(style => style.WithColor("#dc2626").WithWeight("900").WithUnderline().WithFontSize(13));
        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Series data-label styling should still render labels.");
        Assert(svg.Contains("fill=\"#DC2626\"", StringComparison.Ordinal), "Series data-label styles should override chart-level label color.");
        var labels = FamilyLabels(PreparedFamily(chart), "data-label");
        Assert(labels.Length == 4 && labels.All(label => label.Text.Style.Font.Weight == 900 && label.Text.Style.Underline
            && label.Text.Style.FontSize == 13), "Series data-label styles should override weight, decoration and size in the shared text snapshot.");
        Assert(chart.ToPng().Length > 64, "Series data-label styles should render PNG output.");
        AssertThrows<ArgumentNullException>(() => chart.Series[0].WithDataLabelStyle(null!), "Series data-label style callbacks should reject null callbacks.");
        AssertThrows<ArgumentOutOfRangeException>(() => chart.Series[0].WithDataLabelStyle(style => style.WithFontSize(0)), "Series data-label styles should reject invalid font sizes.");
    }

    private static void SpecializedSeriesDataLabelStylesOverrideChartDefaults() {
        var pie = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(420, 280)
            .WithDataLabels()
            .WithDataLabelStyle(style => style.WithColor("#64748b"))
            .AddPie("Slices", Points(70, 30));
        pie.Series[0].WithDataLabelStyle(style => style.WithColor("#0f766e").WithWeight("900").WithUnderline().WithFontSize(14));
        var pieSvg = PreparedFamily(pie).ToSvg(new ChartForgeX.Rendering.VisualSvgOptions());
        Assert(pieSvg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Pie data labels should render with series styles enabled.");
        var pieLabels = FamilyLabels(PreparedFamily(pie), "data-label");
        Assert(pieLabels.Length == 2 && pieLabels.All(label => label.Text.Style.Color.Equals(ChartColor.FromHex("#0F766E")) && label.Text.Style.Font.Weight == 900 && label.Text.Style.Underline), "Pie labels should honor per-series color, weight and underline in the shared text snapshot.");
        Assert(pie.ToPng().Length > 64, "Pie series data-label styles should render PNG output.");

        var heatmap = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(460, 300)
            .WithDataLabels()
            .WithDataLabelStyle(style => style.WithColor("#64748b"))
            .AddHeatmapRow("Styled", Points(95, 86, 72));
        heatmap.Series[0].WithDataLabelStyle(style => style.WithColor("#dc2626").WithWeight("900"));
        var heatmapSvg = heatmap.ToSvg();
        Assert(heatmapSvg.Contains("fill=\"#DC2626\"", StringComparison.Ordinal) && heatmapSvg.Contains("font-weight=\"900\"", StringComparison.Ordinal), "Heatmap cell labels should honor per-series data-label style overrides.");
        Assert(heatmap.ToPng().Length > 64, "Heatmap series data-label styles should render PNG output.");

        var radar = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(460, 320)
            .WithXLabels("Reach", "Depth", "Trust")
            .WithDataLabels()
            .WithDataLabelStyle(style => style.WithColor("#64748b"))
            .AddRadar("Current", Points(92, 74, 88));
        radar.Series[0].WithDataLabelStyle(style => style.WithColor("#7c3aed").WithWeight("900"));
        Assert(FamilyLabels(PreparedFamily(radar), "radar-data-label").Any(label => label.Text.Style.Color.Equals(ChartColor.FromHex("#7C3AED"))), "Radar data labels should honor per-series data-label color overrides.");
        Assert(radar.ToPng().Length > 64, "Radar series data-label styles should render PNG output.");

        var regularPolarArea = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(460, 320)
            .WithLegend(false)
            .WithDataLabels()
            .AddPolarArea("Polar", Points(92, 74, 88))
            .ToPng();
        var polarArea = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(460, 320)
            .WithLegend(false)
            .WithDataLabels()
            .AddPolarArea("Polar", Points(92, 74, 88));
        polarArea.Series[0].WithDataLabelStyle(style => style.WithColor("#0f766e").WithFontFamily("monospace").WithWeight("normal").WithItalic().WithUnderline().WithFontSize(16));
        var polarAreaSvg = PreparedFamily(polarArea).ToSvg(new ChartForgeX.Rendering.VisualSvgOptions());
        var polarAreaPng = polarArea.ToPng();
        Assert(polarAreaSvg.Contains("fill=\"#0F766E\"", StringComparison.Ordinal) && polarAreaSvg.Contains("font-style=\"italic\"", StringComparison.Ordinal), "Polar-area labels should honor per-series data-label style overrides.");
        Assert(!regularPolarArea.SequenceEqual(polarAreaPng), "Polar-area PNG labels should render and position with resolved series text styling.");

        var waterfall = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(480, 320)
            .WithDataLabels()
            .WithDataLabelStyle(style => style.WithColor("#64748b"))
            .AddWaterfall("Delta", Points(18, -7, 12));
        waterfall.Series[0].WithDataLabelStyle(style => style.WithColor("#b45309").WithWeight("900"));
        Assert(waterfall.ToSvg().Contains("fill=\"#B45309\"", StringComparison.Ordinal), "Waterfall data labels should honor per-series data-label color overrides.");
        Assert(waterfall.ToPng().Length > 64, "Waterfall series data-label styles should render PNG output.");
    }

    private static void PointDataLabelStylesOverrideSeriesDefaults() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .WithDataLabels()
            .AddBar("Styled labels", Points(12, 44, 26));
        chart.Series[0]
            .WithDataLabelStyle(style => style.WithColor("#654321").WithWeight("700"))
            .WithPointDataLabelStyle(1, style => style.WithColor("#123456").WithWeight("900").WithUnderline().WithFontSize(14));

        var svg = chart.ToSvg();
        Assert(svg.Contains("fill=\"#123456\"", StringComparison.Ordinal), "Point data-label styles should override series label color.");
        Assert(svg.Contains("fill=\"#654321\"", StringComparison.Ordinal), "Unstyled point labels should continue using the series label style.");
        var styledLabel = FamilyLabels(PreparedFamily(chart), "data-label").Single(label => label.Text.Style.Color.Equals(ChartColor.FromHex("#123456")));
        Assert(styledLabel.Text.Style.Underline && styledLabel.Text.Style.Font.Weight == 900 && styledLabel.Text.Style.FontSize == 14,
            "Point data-label styles should include decoration, weight and size overrides in the shared text snapshot.");
        Assert(chart.ToPng().Length > 64, "Point data-label styles should render PNG output.");

        chart.Series[0].UseSeriesDataLabelStyle(1);
        Assert(!chart.ToSvg().Contains("fill=\"#123456\"", StringComparison.Ordinal), "Clearing a point label style should restore series-level label styling.");

        var pie = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(420, 280)
            .WithDataLabels()
            .AddPie("Slices", Points(70, 30));
        pie.Series[0].WithPointDataLabelStyle(1, style => style.WithColor("#0f3d5e").WithWeight("900"));
        Assert(FamilyLabels(PreparedFamily(pie), "data-label").Any(label => label.Text.Style.Color.Equals(ChartColor.FromHex("#0F3D5E"))), "Pie slice labels should honor point-level data-label style overrides.");
        Assert(pie.ToPng().Length > 64, "Pie point data-label styles should render PNG output.");

        AssertThrows<ArgumentOutOfRangeException>(() => chart.Series[0].WithPointDataLabelStyle(-1, _ => { }), "Point data-label styles should reject negative indexes.");
        AssertThrows<ArgumentOutOfRangeException>(() => chart.Series[0].WithPointDataLabelStyle(99, _ => { }), "Point data-label styles should reject missing point indexes.");
        AssertThrows<ArgumentNullException>(() => chart.Series[0].WithPointDataLabelStyle(0, null!), "Point data-label styles should reject null callbacks.");
        AssertThrows<ArgumentOutOfRangeException>(() => chart.Series[0].UseSeriesDataLabelStyle(99), "Clearing point data-label styles should reject missing point indexes.");
    }

    private static void PointColorsOverrideSeriesColorForBars() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .AddBar("Scores", Points(12, 44, 26), ChartColor.FromHex("#14B8A6"));
        chart.Series[0].WithPointColor(1, "#F97316");
        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"bar\"", StringComparison.Ordinal), "Bar points should still render when point colors are configured.");
        Assert(CartesianPoint(svg, 0, 1).Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "bar" && element.Attribute("fill") != null),
            "The authored bar point should retain a native filled mark.");
        var nativeBar = PreparedFamily(chart).Scene.Nodes.OfType<ChartForgeX.Rendering.VisualSceneRectangle>().Where(mark => mark.Role == "bar").ElementAt(1);
        Assert(nativeBar.Fill!.Value.Equals(ChartColor.FromHex("#F97316")),
            "The orange point override should determine that native flat bar rather than the teal series color.");
        Assert(chart.ToPng().Length > 64, "Bar point colors should render PNG output.");

        var horizontal = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .WithXLabels("A", "B", "C")
            .AddHorizontalBar("Scores", Points(12, 44, 26), ChartColor.FromHex("#14B8A6"));
        horizontal.Series[0].WithPointColor(2, ChartColor.FromHex("#8B5CF6"));
        var horizontalSvg = horizontal.ToSvg();
        Assert(CartesianPoint(horizontalSvg, 0, 2).Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "horizontal-bar" && element.Attribute("fill") != null),
            "The authored horizontal bar point should retain a native filled mark.");
        var nativeHorizontalBar = PreparedFamily(horizontal).Scene.Nodes.OfType<ChartForgeX.Rendering.VisualSceneRectangle>().Where(mark => mark.Role == "horizontal-bar").ElementAt(2);
        Assert(nativeHorizontalBar.Fill!.Value.Equals(ChartColor.FromHex("#8B5CF6")),
            "The purple horizontal point override should determine that native flat observation.");
        Assert(horizontal.ToPng().Length > 64, "Horizontal bar point colors should render PNG output.");

        var funnel = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .WithXLabels("Visit", "Qualify", "Close")
            .AddFunnel("Pipeline", Points(120, 74, 32));
        funnel.Series[0].WithPointColor(1, "#E11D48");
        var funnelSvg = funnel.ToSvg();
        var funnelPoint = System.Xml.Linq.XDocument.Parse(funnelSvg).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "funnel-stage"
            && (string?)element.Attribute("data-cfx-point") == "1");
        Assert(funnelPoint.Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "funnel-segment" && (string?)element.Attribute("fill") == "#E11D48"),
            "The authored funnel stage should use its point-specific color in SVG.");
        Assert(PreparedFamily(funnel).Scene.Nodes.OfType<ChartForgeX.Rendering.VisualScenePath>().Where(node => node.Role == "funnel-segment").ElementAt(1).Fill!.Value.Equals(ChartColor.FromHex("#E11D48")),
            "The same funnel stage should retain its point-specific color for native raster rendering.");
        Assert(funnel.ToPng().Length > 64, "Funnel point colors should render PNG output.");

        var treemap = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .AddTreemap("Spend", new[] {
                new ChartTreemapItem("Core", "Core", value: 48),
                new ChartTreemapItem("Edge", "Edge", value: 28),
                new ChartTreemapItem("Long tail", "Long tail", value: 12)
            });
        treemap.Series[0].WithPointColor(1, "#8B5CF6");
        var treemapTile = System.Xml.Linq.XDocument.Parse(treemap.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "treemap-tile"
            && (string?)element.Attribute("data-cfx-target-id") == "Edge");
        Assert(treemapTile.Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "treemap-tile-mark" && (string?)element.Attribute("fill") == "#8B5CF6"),
            "The authored treemap tile should use its point-specific color in SVG.");
        Assert(PreparedFamily(treemap).Scene.Nodes.OfType<ChartForgeX.Rendering.VisualSceneRectangle>().Any(node => node.Role == "treemap-tile-mark" && node.Fill!.Value.Equals(ChartColor.FromHex("#8B5CF6"))),
            "Native treemap geometry should retain the authored purple point color.");
        Assert(treemap.ToPng().Length > 64, "Treemap point colors should render PNG output.");

        var scatter = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .AddScatter("Observed", Points(12, 44, 26), ChartColor.FromHex("#14B8A6"));
        scatter.Series[0].WithPointColor(1, "#0EA5E9");
        Assert(scatter.ToSvg().Contains("fill=\"#0EA5E9\"", StringComparison.Ordinal), "Scatter markers should honor point-specific colors in SVG.");
        Assert(scatter.ToPng().Length > 64, "Scatter point colors should render PNG output.");

        var line = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .AddLine("Trend", Points(12, 44, 26), ChartColor.FromHex("#14B8A6"));
        line.Series[0].WithPointColor(2, "#DB2777");
        Assert(CartesianPoint(line.ToSvg(), 0, 2).Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "marker"
            && (string?)element.Attribute("fill") == "#DB2777"), "The authored line observation should retain its point-specific marker color in SVG.");
        Assert(PreparedFamily(line).Scene.Nodes.OfType<ChartForgeX.Rendering.VisualSceneEllipse>().Single(node => node.Role == "marker").Fill!.Value.Equals(ChartColor.FromHex("#DB2777")),
            "The same line observation should retain its point-specific color for native raster rendering.");
        Assert(line.ToPng().Length > 64, "Line marker point colors should render PNG output.");

        var lollipop = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .AddLollipop("Coverage", Points(12, 44, 26), ChartColor.FromHex("#14B8A6"));
        lollipop.Series[0].WithPointColor(1, "#F59E0B");
        var lollipopSvg = lollipop.ToSvg();
        Assert(lollipopSvg.Contains("data-cfx-role=\"lollipop-marker\"", StringComparison.Ordinal) && lollipopSvg.Contains("fill=\"#F59E0B\"", StringComparison.Ordinal), "Lollipop markers should honor point-specific colors in SVG.");
        Assert(lollipop.ToPng().Length > 64, "Lollipop point colors should render PNG output.");

        var bubble = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .AddBubble("Risk", new[] {
                new ChartBubble(1, 18, 8),
                new ChartBubble(2, 34, 22),
                new ChartBubble(3, 26, 14)
            }, ChartColor.FromHex("#14B8A6"));
        bubble.Series[0].WithPointColor(1, "#7C3AED");
        var bubbleColor = ChartColor.FromHex("#7C3AED");
        Assert(CartesianPoint(bubble.ToSvg(), 0, 1).Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "bubble"
            && (string?)element.Attribute("stroke") == ChartColorMath.WithOpacity(bubbleColor, ChartVisualPrimitives.BubbleStrokeOpacity).ToCss()),
            "The authored bubble observation should retain its point-specific stroke and style opacity in SVG.");
        var nativeBubble = PreparedFamily(bubble).Scene.Nodes.OfType<ChartForgeX.Rendering.VisualSceneEllipse>().Where(node => node.Role == "bubble").ElementAt(1);
        Assert(nativeBubble.Fill!.Value.Equals(ChartColorMath.WithOpacity(bubbleColor, ChartVisualPrimitives.BubbleFillOpacity))
            && nativeBubble.Stroke!.Value.Equals(ChartColorMath.WithOpacity(bubbleColor, ChartVisualPrimitives.BubbleStrokeOpacity)),
            "Native bubble paint should use the authored point color for both translucent fill and stroke.");
        Assert(bubble.ToPng().Length > 64, "Bubble point colors should render PNG output.");

        var errorBar = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .AddErrorBar("Confidence", new[] {
                new ChartErrorBar(1, 42, 35, 51),
                new ChartErrorBar(2, 58, 49, 66)
            }, ChartColor.FromHex("#14B8A6"));
        errorBar.Series[0].WithPointColor(1, "#DC2626");
        Assert(errorBar.ToSvg().Contains("stroke=\"#DC2626\"", StringComparison.Ordinal) && errorBar.ToSvg().Contains("fill=\"#DC2626\"", StringComparison.Ordinal), "Error-bar marks should honor point-specific colors in SVG.");
        Assert(errorBar.ToPng().Length > 64, "Error-bar point colors should render PNG output.");

        var rangeBar = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .AddRangeBar("Observed", new[] {
                new ChartInterval(1, 20, 42),
                new ChartInterval(2, 30, 55)
            }, ChartColor.FromHex("#14B8A6"));
        rangeBar.Series[0].WithPointColor(1, "#F97316");
        Assert(rangeBar.ToSvg().Contains("data-cfx-role=\"range-bar\"", StringComparison.Ordinal) && rangeBar.ToSvg().Contains("fill=\"#F97316\"", StringComparison.Ordinal), "Range-bar intervals should honor point-specific colors in SVG.");
        Assert(rangeBar.ToPng().Length > 64, "Range-bar point colors should render PNG output.");

        var dumbbell = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .AddDumbbell("Before/after", new[] {
                new ChartDumbbell(1, 32, 44),
                new ChartDumbbell(2, 38, 58)
            }, ChartColor.FromHex("#14B8A6"));
        dumbbell.Series[0].WithPointColor(1, "#0EA5E9");
        Assert(dumbbell.ToSvg().Contains("data-cfx-role=\"dumbbell-end\"", StringComparison.Ordinal) && dumbbell.ToSvg().Contains("fill=\"#0EA5E9\"", StringComparison.Ordinal), "Dumbbell comparison marks should honor point-specific colors in SVG.");
        Assert(dumbbell.ToPng().Length > 64, "Dumbbell point colors should render PNG output.");

        var boxPlot = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .AddBoxPlot("Latency", new[] {
                new ChartBoxPlot(1, 18, 24, 31, 38, 48),
                new ChartBoxPlot(2, 42, 56, 64, 82, 104)
            }, ChartColor.FromHex("#14B8A6"));
        boxPlot.Series[0].WithPointColor(1, "#8B5CF6");
        Assert(CartesianPoint(boxPlot.ToSvg(), 0, 1).Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "boxplot-body"
            && (string?)element.Attribute("stroke") == "#8B5CF6"), "Box-plot summaries should honor point-specific colors in SVG.");
        Assert(PreparedFamily(boxPlot).Scene.Nodes.OfType<ChartForgeX.Rendering.VisualSceneRectangle>().Where(node => node.Role == "boxplot-body").ElementAt(1).Stroke!.Value.Equals(ChartColor.FromHex("#8B5CF6")),
            "Native box-plot outlines should retain the authored observation color.");
        Assert(boxPlot.ToPng().Length > 64, "Box-plot point colors should render PNG output.");

        var candles = new[] {
            new ChartCandlestick(1, 42, 51, 35, 48),
            new ChartCandlestick(2, 58, 66, 49, 54)
        };
        var candlestick = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .AddCandlestick("Windows", candles);
        candlestick.Series[0].WithPointColor(1, "#DB2777");
        Assert(candlestick.ToSvg().Contains("data-cfx-role=\"candlestick-body\"", StringComparison.Ordinal) && candlestick.ToSvg().Contains("stroke=\"#DB2777\"", StringComparison.Ordinal), "Candlestick windows should allow point colors to override semantic rising/falling colors in SVG.");
        Assert(candlestick.ToPng().Length > 64, "Candlestick point colors should render PNG output.");

        var ohlc = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .AddOhlc("Windows", candles);
        ohlc.Series[0].WithPointColor(1, "#9333EA");
        Assert(ohlc.ToSvg().Contains("data-cfx-role=\"ohlc-stem\"", StringComparison.Ordinal) && ohlc.ToSvg().Contains("stroke=\"#9333EA\"", StringComparison.Ordinal), "OHLC windows should allow point colors to override semantic rising/falling colors in SVG.");
        Assert(ohlc.ToPng().Length > 64, "OHLC point colors should render PNG output.");

        var slope = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(540, 320)
            .AddSlope("Before/after", 24, 52, ChartColor.FromHex("#14B8A6"));
        slope.Series[0].WithPointColor(1, "#E11D48");
        Assert(CartesianPoint(slope.ToSvg(), 0, 1).Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "slope-marker"
            && (string?)element.Attribute("fill") == "#E11D48"), "Slope endpoint markers should honor point-specific colors in SVG.");
        Assert(slope.ToPng().Length > 64, "Slope endpoint point colors should render PNG output.");

        var pointLegend = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(520, 320)
            .WithPointLegend()
            .WithLegendPosition(ChartLegendPosition.Right)
            .WithXLabels("Critical", "High", "Medium")
            .AddBar("Severity", Points(8, 32, 84), ChartColor.FromHex("#2563EB"));
        pointLegend.Series[0].WithPointColor(1, "#F97316");
        var pointLegendSvg = pointLegend.ToSvg();
        Assert(System.Xml.Linq.XDocument.Parse(pointLegendSvg).Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "legend-entry"
            && (string?)element.Attribute("data-cfx-series-key") == "Severity" && (string?)element.Attribute("data-cfx-source-id") == "legend-series-0-point-1"),
            "Point legends should retain the exact observation identity and semantic series key.");
        Assert(pointLegendSvg.Contains(">High</text>", StringComparison.Ordinal) && pointLegendSvg.Contains("fill=\"#F97316\"", StringComparison.Ordinal), "Point legends should use x-axis labels and point colors.");
        Assert(pointLegend.ToPng().Length > 64, "Point legends should render PNG output.");

        var aggregateLineLegend = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithLegend(true)
            .WithSize(520, 320)
            .WithPointLegend()
            .AddLine("Latency", Points(12, 18, 15), ChartColor.FromHex("#2563EB"));
        var aggregateLineLegendSvg = aggregateLineLegend.ToSvg();
        var lineLegendEntry = System.Xml.Linq.XDocument.Parse(aggregateLineLegendSvg).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "legend-entry");
        Assert((string?)lineLegendEntry.Attribute("data-cfx-source-id") == "legend-series-0"
            && (string?)lineLegendEntry.Attribute("data-cfx-series-key") == "Latency" && !lineLegendEntry.DescendantsAndSelf().Any(element => element.Attribute("data-cfx-point") != null),
            "Aggregate line geometry should retain one series legend identity instead of advertising point-level muting.");
        Assert(aggregateLineLegend.ToPng().Length > 64, "Aggregate line point-legend fallback should preserve PNG parity.");

        chart.Series[0].UseSeriesColor(1);
        Assert(!chart.ToSvg().Contains("fill=\"#F97316\"", StringComparison.Ordinal), "Clearing a point color should restore the series fill.");
        AssertThrows<ArgumentOutOfRangeException>(() => chart.Series[0].WithPointColor(99, "#F97316"), "Point colors should reject missing point indexes.");
        AssertThrows<ArgumentOutOfRangeException>(() => chart.Series[0].UseSeriesColor(99), "Clearing point colors should reject missing point indexes.");
        AssertThrows<ArgumentOutOfRangeException>(() => bubble.Series[0].WithPointColor(3, "#F97316"), "Tuple-backed point colors should reject indexes outside the logical item count.");
        AssertThrows<ArgumentOutOfRangeException>(() => errorBar.Series[0].WithPointDataLabelStyle(2, _ => { }), "Tuple-backed point label styles should reject indexes outside the logical item count.");
        AssertThrows<ArgumentOutOfRangeException>(() => candlestick.Series[0].WithPointSliceOffset(2, 0.1), "Tuple-backed slice offsets should reject indexes outside the logical item count.");
    }

    private static void MapPointColorsOverrideSeriesColor() {
        var dotted = Chart.Create()
            .WithSize(520, 320)
            .AddDottedMap("Visited", new[] {
                new ChartMapPoint("Spain", -3.7038, 40.4168),
                new ChartMapPoint("Indonesia", 113.9213, -0.7893)
            }, ChartColor.FromHex("#14B8A6"));
        dotted.Series[0].WithPointColor(1, "#E11D48");
        var dottedSvg = dotted.ToSvg();
        Assert(MapMarkers(PreparedFamily(dotted))[1].Fill.Equals(ChartColor.FromHex("#E11D48")) && dottedSvg.Contains("#E11D48", StringComparison.Ordinal), "Dotted map points should honor point-specific colors in both prepared paint and SVG.");
        Assert(dotted.ToPng().Length > 64, "Dotted map point colors should render PNG output.");

        var calendar = Chart.Create()
            .WithSize(520, 320)
            .AddCalendarHeatmap("Commits", new[] {
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 1), 0),
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 2), 100)
            }, ChartColor.FromHex("#14B8A6"));
        calendar.Series[0].WithPointColor(1, "#7C3AED");
        var calendarSvg = calendar.ToSvg();
        Assert(calendarSvg.Contains("data-cfx-date=\"2026-01-02\"", StringComparison.Ordinal) && calendarSvg.Contains("fill=\"#7C3AED\"", StringComparison.Ordinal), "Calendar heatmap cells should honor point-specific colors in SVG.");
        Assert(calendar.ToPng().Length > 64, "Calendar heatmap point colors should render PNG output.");

        var tile = Chart.Create()
            .WithSize(520, 320)
            .AddTileMap("Revenue", ChartTileMapCatalog.Get("us-states"), new[] {
                new ChartRegionMapItem("CA", 10),
                new ChartRegionMapItem("NY", 100)
            }, ChartColor.FromHex("#14B8A6"));
        tile.Series[0].WithPointColor(1, "#F97316");
        var tileSvg = tile.ToSvg();
        Assert(tileSvg.Contains("data-cfx-region=\"NY\"", StringComparison.Ordinal) && tileSvg.Contains("fill=\"#F97316\"", StringComparison.Ordinal), "Tile map regions should honor point-specific colors in SVG.");
        Assert(tile.ToPng().Length > 64, "Tile map point colors should render PNG output.");

        var geo = Chart.Create()
            .WithSize(520, 320)
            .WithMapLabels(false)
            .AddRegionMap("Revenue", ChartMapCatalog.Get("us-states"), new[] {
                new ChartRegionMapItem("CA", 10),
                new ChartRegionMapItem("NY", 100)
            }, ChartColor.FromHex("#14B8A6"));
        geo.Series[0].WithPointColor(1, "#0EA5E9");
        var geoSvg = geo.ToSvg();
        Assert(geoSvg.Contains("data-cfx-region=\"NY\"", StringComparison.Ordinal) && geoSvg.Contains("fill=\"#0EA5E9\"", StringComparison.Ordinal), "Region map regions should honor point-specific colors in SVG.");
        Assert(geo.ToPng().Length > 64, "Region map point colors should render PNG output.");
    }

    private static void DataLabelPlacementCanBeConfigured() {
        var chart = Chart.Create()
            .WithSize(520, 320)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Center)
            .AddBar("Centered", Points(24, 58, 36));

        Assert(chart.Options.DataLabelPlacement == ChartDataLabelPlacement.Center, "Chart-level data-label placement should be configurable.");
        Assert(chart.ToSvg().Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Configured data-label placement should still render SVG labels.");
        Assert(chart.ToPng().Length > 64, "Configured data-label placement should render PNG output.");

        chart.Series[0].WithDataLabelPlacement(ChartDataLabelPlacement.Right);
        Assert(chart.Series[0].DataLabelPlacement == ChartDataLabelPlacement.Right, "Series-level data-label placement should override the chart setting.");
        Assert(chart.ToSvg().Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Series-level data-label placement should still render SVG labels.");
        chart.Series[0].UseChartDataLabelPlacement();
        Assert(chart.Series[0].DataLabelPlacement == null, "Series-level data-label placement can be cleared.");

        AssertThrows<ArgumentOutOfRangeException>(() => chart.WithDataLabelPlacement((ChartDataLabelPlacement)999), "Chart data-label placement should reject unknown values.");
        AssertThrows<ArgumentOutOfRangeException>(() => chart.Series[0].WithDataLabelPlacement((ChartDataLabelPlacement)999), "Series data-label placement should reject unknown values.");
    }

    private static void SpecializedDataLabelPlacementCanBeConfigured() {
        var pie = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(420, 280)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Outside)
            .AddPie("Slices", Points(70, 30));
        var pieSvg = pie.ToSvg();
        Assert(pieSvg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Pie labels should render when outside placement is configured.");
        Assert(pieSvg.Contains("data-cfx-role=\"data-label-connector\"", StringComparison.Ordinal), "Pie outside labels should render connector lines.");
        Assert(pie.ToPng().Length > 64, "Pie outside label placement should render PNG output.");

        var heatmap = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(460, 300)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Right)
            .AddHeatmapRow("Styled", Points(95, 86, 72));
        var heatmapSvg = heatmap.ToSvg();
        Assert(heatmapSvg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Heatmap labels should render when side placement is configured.");
        Assert(heatmapSvg.Contains("data-cfx-role=\"data-label-connector\"", StringComparison.Ordinal), "Heatmap side labels should render connector lines.");
        Assert(heatmapSvg.Contains(">Styled</text>", StringComparison.Ordinal), "Heatmap side label lanes should not steal space from row labels.");
        Assert(heatmap.ToPng().Length > 64, "Heatmap side label placement should render PNG output.");

        var radar = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(460, 320)
            .WithXLabels("Reach", "Depth", "Trust")
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Below)
            .AddRadar("Current", Points(92, 74, 88));
        Assert(FamilyLabels(PreparedFamily(radar), "radar-data-label").Length == 3, "Radar labels should render for each point when below placement is configured.");
        Assert(radar.ToPng().Length > 64, "Radar below label placement should render PNG output.");

        var bubble = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(480, 320)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Center)
            .AddBubble("Risk", new[] {
                new ChartBubble(1, 18, 8),
                new ChartBubble(2, 34, 22)
            });
        Assert(bubble.ToSvg().Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Bubble labels should render when center placement is configured.");
        Assert(bubble.ToPng().Length > 64, "Bubble center label placement should render PNG output.");

        var errorBar = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(480, 320)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Right)
            .AddErrorBar("Confidence", new[] {
                new ChartErrorBar(1, 42, 35, 51),
                new ChartErrorBar(2, 58, 49, 66)
            });
        Assert(errorBar.ToSvg().Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Error-bar labels should render when side placement is configured.");
        Assert(errorBar.ToPng().Length > 64, "Error-bar side label placement should render PNG output.");

        var rangeBand = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(480, 320)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Below)
            .AddRangeBand("Forecast", new[] {
                new ChartRangeBand(1, 32, 44),
                new ChartRangeBand(2, 38, 58)
            });
        Assert(rangeBand.ToSvg().Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Range-band labels should render when below placement is configured.");
        Assert(rangeBand.ToPng().Length > 64, "Range-band below label placement should render PNG output.");

        var rangeArea = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(480, 320)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Left)
            .AddRangeArea("Prediction", new[] {
                new ChartRangeBand(1, 32, 44),
                new ChartRangeBand(2, 38, 58)
            });
        Assert(rangeArea.ToSvg().Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Range-area labels should render when side placement is configured.");
        Assert(rangeArea.ToPng().Length > 64, "Range-area side label placement should render PNG output.");

        var waterfall = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(480, 320)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Inside)
            .AddWaterfall("Delta", Points(18, -7, 12));
        Assert(waterfall.ToSvg().Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Waterfall labels should render when inside placement is configured.");
        Assert(waterfall.ToPng().Length > 64, "Waterfall inside label placement should render PNG output.");

        var rangeBar = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(480, 320)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Center)
            .AddRangeBar("Observed", new[] {
                new ChartInterval(1, 20, 42),
                new ChartInterval(2, 30, 55)
            });
        Assert(rangeBar.ToSvg().Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Range-bar labels should render when center placement is configured.");
        Assert(rangeBar.ToPng().Length > 64, "Range-bar center label placement should render PNG output.");

        var dumbbell = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(480, 320)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Right)
            .AddDumbbell("Before/after", new[] {
                new ChartDumbbell(1, 32, 44),
                new ChartDumbbell(2, 38, 58)
            });
        Assert(dumbbell.ToSvg().Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Dumbbell labels should render when side placement is configured.");
        Assert(dumbbell.ToPng().Length > 64, "Dumbbell side label placement should render PNG output.");

        var boxPlot = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(480, 320)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Left)
            .AddBoxPlot("Latency", new[] {
                new ChartBoxPlot(1, 18, 24, 31, 38, 48),
                new ChartBoxPlot(2, 42, 56, 64, 82, 104)
            });
        Assert(boxPlot.ToSvg().Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Box-plot labels should render when side placement is configured.");
        Assert(boxPlot.ToPng().Length > 64, "Box-plot side label placement should render PNG output.");

        var candles = new[] {
            new ChartCandlestick(1, 42, 51, 35, 48),
            new ChartCandlestick(2, 58, 66, 49, 54)
        };
        var candlestick = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(480, 320)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Right)
            .AddCandlestick("Windows", candles);
        Assert(candlestick.ToSvg().Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Candlestick labels should render when side placement is configured.");
        Assert(candlestick.ToPng().Length > 64, "Candlestick side label placement should render PNG output.");

        var ohlc = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(480, 320)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Above)
            .AddOhlc("Windows", candles);
        Assert(ohlc.ToSvg().Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "OHLC labels should render when vertical placement is configured.");
        Assert(ohlc.ToPng().Length > 64, "OHLC vertical label placement should render PNG output.");
    }

    private static void SeriesFluentStylingRejectsInvalidStrokeWidths() {
        var series = new ChartSeries("Values", ChartSeriesKind.Line, Points(1, 2, 3));
        AssertThrows<ArgumentOutOfRangeException>(() => series.WithStrokeWidth(0), "Series fluent stroke helpers should reject non-positive widths.");
        AssertThrows<ArgumentOutOfRangeException>(() => series.WithStrokeWidth(double.NaN), "Series fluent stroke helpers should reject non-finite widths.");
    }
}
