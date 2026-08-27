using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SvgAxisLayoutUsesResolvedTypography() {
        static Chart AxisChart() => Chart.Create()
            .WithSize(540, 300)
            .WithXAxis("delivery status")
            .WithXLabels("mmmm one", "mmmm two", "mmmm three", "mmmm four", "mmmm five", "mmmm six", "mmmm seven", "mmmm eight", "mmmm nine")
            .AddLine("Values", Points(1, 2, 3, 4, 5, 6, 7, 8, 9));

        var regular = AxisChart().ToSvg();
        var styledChart = AxisChart()
            .WithTickLabelStyle(style => style.WithFontFamily("monospace").WithFontSize(26).WithItalic().WithTextCase(TextCaseTransform.Uppercase))
            .WithAxisTitleStyle(style => style.WithFontFamily("serif").WithFontSize(32).WithItalic().WithUnderline(TextDecorationStyle.Wavy).WithTextCase(TextCaseTransform.Uppercase));
        var styled = styledChart.ToSvg();

        Assert(CountOccurrences(styled, "data-cfx-role=\"x-axis-label\"") < CountOccurrences(regular, "data-cfx-role=\"x-axis-label\""), "SVG tick density should measure resolved family, size, italic, and transformed casing before selecting labels.");
        Assert(GetAttribute(styled, "data-cfx-role=\"x-axis-label\"", "y") < GetAttribute(regular, "data-cfx-role=\"x-axis-label\"", "y"), "SVG plot allocation should move upward to reserve a larger styled axis title.");
        Assert(styled.Contains(">DELIVERY STATUS</text>", StringComparison.Ordinal) && styled.Contains("font-size=\"32\"", StringComparison.Ordinal), "SVG axis titles should serialize their resolved text and size after layout.");
    }

    private static void SpecializedLegendsHonorRoleTypography() {
        static void ApplyLegendStyle(Chart chart) => chart.WithLegendStyle(style => style
            .WithColor("#d946ef")
            .WithFontFamily("monospace")
            .WithFontSize(18)
            .WithWeight("800")
            .WithItalic()
            .WithUnderline(TextDecorationStyle.Double)
            .WithStrikethrough(TextDecorationStyle.Wavy)
            .WithSuperscript()
            .WithTextCase(TextCaseTransform.Uppercase));

        var pie = Chart.Create().WithSize(560, 360).WithXLabels("north region", "south region").AddDonut("Coverage", Points(62, 38));
        var plainPiePng = pie.ToPng();
        ApplyLegendStyle(pie);
        var pieSvg = pie.ToSvg();
        Assert(pieSvg.Contains("data-cfx-role=\"slice-legend-label\"", StringComparison.Ordinal) && pieSvg.Contains(">NORTH REGION</tspan>", StringComparison.Ordinal), "Pie, donut, and polar-area legends should apply legend casing before fitting.");
        Assert(pieSvg.Contains("fill=\"#D946EF\"", StringComparison.Ordinal) && pieSvg.Contains("font-family=\"monospace\"", StringComparison.Ordinal) && pieSvg.Contains("font-style=\"italic\"", StringComparison.Ordinal) && pieSvg.Contains("baseline-shift=\"super\"", StringComparison.Ordinal), "Custom slice legends should preserve the complete legend style in SVG.");
        Assert(!plainPiePng.SequenceEqual(pie.ToPng()), "Custom slice legends should preserve the complete legend style in raster output.");

        var radial = Chart.Create().WithSize(560, 360).WithXLabels("mail controls", "dns controls").AddRadialBar("Coverage", Points(82, 71));
        var plainRadialPng = radial.ToPng();
        ApplyLegendStyle(radial);
        var radialSvg = radial.ToSvg();
        Assert(radialSvg.Contains("data-cfx-role=\"radial-bar-legend-label\"", StringComparison.Ordinal) && radialSvg.Contains(">MAIL CONTROLS</tspan>", StringComparison.Ordinal), "Radial-bar legends should apply legend casing before fitting.");
        Assert(radialSvg.Contains("fill=\"#D946EF\"", StringComparison.Ordinal) && radialSvg.Contains("text-decoration=\"line-through\"", StringComparison.Ordinal) && radialSvg.Contains("text-decoration-style=\"wavy\"", StringComparison.Ordinal), "Radial-bar legends should preserve color and independent decoration styles in SVG.");
        Assert(!plainRadialPng.SequenceEqual(radial.ToPng()), "Radial-bar legends should preserve the complete legend style in raster output.");
    }

    private static void SpecializedAxesHonorTickTypography() {
        var heatmap = Chart.Create()
            .WithSize(620, 380)
            .WithXAxis("control family")
            .WithYAxis("domain group")
            .WithXLabels("mail auth", "transport security")
            .AddHeatmapRow("primary domains", Points(92, 81))
            .AddHeatmapRow("regional domains", Points(78, 69));
        var plainPng = heatmap.ToPng();
        heatmap.WithTickLabelStyle(style => style
            .WithColor("#d946ef")
            .WithFontFamily("monospace")
            .WithFontSize(17)
            .WithWeight("800")
            .WithItalic()
            .WithUnderline(TextDecorationStyle.Dashed)
            .WithSuperscript()
            .WithTextCase(TextCaseTransform.Uppercase));

        var svg = heatmap.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"heatmap-row-label\"", StringComparison.Ordinal) && svg.Contains(">PRIMARY DOMAINS</text>", StringComparison.Ordinal), "Heatmap row axes should apply tick casing before fitting.");
        Assert(svg.Contains("data-cfx-role=\"heatmap-column-label\"", StringComparison.Ordinal) && svg.Contains(">MAIL AUTH</text>", StringComparison.Ordinal), "Heatmap column axes should apply tick casing before fitting.");
        Assert(svg.Contains("fill=\"#D946EF\"", StringComparison.Ordinal) && svg.Contains("font-family=\"monospace\"", StringComparison.Ordinal) && svg.Contains("font-style=\"italic\"", StringComparison.Ordinal) && svg.Contains("text-decoration-style=\"dashed\"", StringComparison.Ordinal) && svg.Contains("baseline-shift=\"super\"", StringComparison.Ordinal), "Heatmap axes and scale labels should preserve the complete tick style in SVG.");
        Assert(!plainPng.SequenceEqual(heatmap.ToPng()), "Heatmap row, column, and scale labels should preserve tick styles in raster output.");

        AssertSpecializedTickStyle(
            Chart.Create().WithSize(640, 360).AddCalendarHeatmap("Commits", new[] {
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 7),
                new ChartCalendarHeatmapItem(new DateTime(2026, 2, 2), 12)
            }), "calendar-heatmap-weekday-label", "MON");
        AssertSpecializedTickStyle(
            Chart.Create().WithSize(640, 360).WithXLabels("mail", "dns").AddHexbinHeatmapRow("primary domains", Points(82, 91)),
            "hexbin-heatmap-row-label", "PRIMARY DOMAINS");
        AssertSpecializedTickStyle(
            Chart.Create().WithSize(640, 360).AddTimelineItem("certificate renewal", new DateTime(2026, 1, 5), new DateTime(2026, 2, 5)),
            "timeline-row-label", "CERTIFICATE RENEWAL");
        AssertSpecializedTickStyle(
            Chart.Create().WithSize(640, 360).WithGanttToday(new DateTime(2026, 1, 15)).AddGanttTask("inventory scope", new DateTime(2026, 1, 5), new DateTime(2026, 2, 5), 0.6),
            "gantt-row-label", "INVENTORY SCOPE");
        AssertSpecializedTickStyle(
            Chart.Create().WithSize(640, 360).WithXLabels("mail", "dns", "web").AddRadar("Coverage", Points(92, 81, 74)),
            "radar-axis-label", "MAIL");
        AssertSpecializedTickStyle(
            Chart.Create().WithSize(640, 360).AddPictorial("Coverage", new[] { new ChartPictorialItem("primary domains", 4) }),
            "pictorial-label", "PRIMARY DOMAINS");
        AssertSpecializedTickStyle(
            Chart.Create().WithSize(640, 360).AddProgressBars("Coverage", new[] { new ChartProgressItem("mail auth", 82) }),
            "progress-label", "MAIL AUTH");
        AssertSpecializedTickStyle(
            Chart.Create().WithSize(700, 420).AddTileMap("Revenue", ChartTileMapCatalog.Get("us-states"), new[] { new ChartRegionMapItem("CA", 95) }),
            "tile-map-scale-label", "LESS");

        static void AssertSpecializedTickStyle(Chart chart, string role, string expectedText) {
            var plain = chart.ToPng();
            chart.WithTickLabelStyle(style => style
                .WithColor("#d946ef")
                .WithFontFamily("monospace")
                .WithFontSize(17)
                .WithWeight("800")
                .WithItalic()
                .WithUnderline(TextDecorationStyle.Dashed)
                .WithSuperscript()
                .WithTextCase(TextCaseTransform.Uppercase));
            var styled = chart.ToSvg();
            Assert(styled.Contains("data-cfx-role=\"" + role + "\"", StringComparison.Ordinal) && styled.Contains(">" + expectedText + "</text>", StringComparison.Ordinal), role + " should apply transformed tick text.");
            Assert(styled.Contains("fill=\"#D946EF\"", StringComparison.Ordinal) && styled.Contains("font-family=\"monospace\"", StringComparison.Ordinal) && styled.Contains("font-style=\"italic\"", StringComparison.Ordinal), role + " should preserve tick color, family, and italic style in SVG.");
            Assert(!plain.SequenceEqual(chart.ToPng()), role + " should preserve tick styles in raster output.");
        }
    }

    private static void SvgRasterCaseTransformsCrossTspanBoundaries() {
        var title = new SvgRasterTextTransformer();
        var titleRuns = new[] { title.Transform("hel", "capitalize"), title.Transform("lo", "capitalize"), title.Transform(" wo", "capitalize"), title.Transform("rld", "capitalize") };
        Assert(string.Concat(titleRuns) == "Hello World", "SVG raster title casing should preserve word context across adjacent text and tspan runs.");

        var sentence = new SvgRasterTextTransformer();
        var sentenceRuns = new[] { sentence.Transform("hEL", "sentence-case"), sentence.Transform("LO. wo", "sentence-case"), sentence.Transform("RLD", "sentence-case") };
        Assert(string.Concat(sentenceRuns) == "Hello. World", "SVG raster sentence casing should preserve sentence context across adjacent text and tspan runs.");

        var uppercase = new SvgRasterTextTransformer();
        Assert(uppercase.Transform("MiX", "uppercase") + uppercase.Transform("eD", "uppercase") == "MIXED", "SVG raster uppercase should remain stable across text runs.");
        var lowercase = new SvgRasterTextTransformer();
        Assert(lowercase.Transform("MiX", "lowercase") + lowercase.Transform("eD", "lowercase") == "mixed", "SVG raster lowercase should remain stable across text runs.");
        var toggle = new SvgRasterTextTransformer();
        Assert(toggle.Transform("MiX", "toggle-case") + toggle.Transform("eD", "toggle-case") == "mIxEd", "SVG raster toggle case should remain stable across text runs.");

        const string splitSentence = "<svg xmlns='http://www.w3.org/2000/svg' width='300' height='70'><text x='8' y='48' font-size='28' fill='#2563eb' text-transform='sentence-case'>hEL<tspan>LO. wo</tspan>RLD</text></svg>";
        Assert(SvgRasterizer.ToPng(splitSentence).Length > 64, "SVG raster case transforms should render through nested tspan content.");
    }

    private static void FunnelGalleryTextStaysReadable() {
        var svg = Chart.Create()
            .WithSize(920, 560)
            .WithXLabels("Opened", "Deferred", "Closed")
            .AddFunnel("Review flow", Points(100, 0, 18))
            .ToSvg();
        var document = XDocument.Parse(svg);
        var ns = document.Root!.Name.Namespace;
        var funnelText = document.Descendants(ns + "text")
            .Where(element => ((string?)element.Attribute("data-cfx-role"))?.StartsWith("funnel-", StringComparison.Ordinal) == true)
            .ToArray();
        Assert(funnelText.Length > 0, "The zero-stage funnel regression should retain its accessible visible labels.");
        Assert(funnelText.All(element => double.Parse(element.Attribute("font-size")!.Value, CultureInfo.InvariantCulture) >= 8), "Funnel labels should never shrink below the gallery's readable SVG threshold.");
    }
}
