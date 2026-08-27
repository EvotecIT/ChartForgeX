using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void RasterScriptBaselinesUseSingleDirectionalShift() {
        static ColorBounds TitleBounds(TextBaseline baseline, double fontSize = 24) {
            var chart = Chart.Create()
                .WithSize(360, 220)
                .WithTitle("Directional baseline")
                .WithTitleStyle(style => style.WithColor("#ff00ff").WithFontSize(fontSize).WithBaseline(baseline))
                .AddLine("Values", Points(1, 3, 2));
            var pixels = ReadPngRgba(chart.ToPng(), out var width, out _);
            return FindNearColorBounds(pixels, width, 255, 0, 255, 12);
        }

        var superscript = TitleBounds(TextBaseline.Superscript);
        var subscript = TitleBounds(TextBaseline.Subscript);
        var normalAtEffectiveSize = TitleBounds(TextBaseline.Normal, 24 * 0.65);
        Assert(!superscript.IsEmpty && !subscript.IsEmpty, "PNG script layout proof should find both configured title colors.");
        Assert(superscript.Top < normalAtEffectiveSize.Top && subscript.Top > normalAtEffectiveSize.Top, "PNG superscript and subscript should retain opposite directional shifts around an equivalently sized normal baseline.");
        Assert(superscript.Top < subscript.Top && superscript.Bottom < subscript.Bottom, "PNG superscript should remain above subscript after fitting the complete directional extents.");
        var maximumSingleShiftSpan = (int)Math.Ceiling(24 * 0.65 * (0.35 + 0.22)) + 1;
        Assert(subscript.Top - superscript.Top <= maximumSingleShiftSpan, "PNG script placement should apply one directional baseline shift instead of subtracting the absolute extent and shifting the glyph a second time.");
    }

    private static void SvgSpecializedLayoutsReserveTransformedText() {
        var regularBullet = Chart.Create().WithSize(560, 260).WithDataLabels().AddBullet("mmmmmmmmmmmmmmmm", 82, 90).ToSvg();
        var uppercaseBullet = Chart.Create().WithSize(560, 260).WithDataLabels().WithDataLabelStyle(style => style.WithTextCase(TextCaseTransform.Uppercase)).AddBullet("mmmmmmmmmmmmmmmm", 82, 90).ToSvg();
        Assert(GetAttribute(uppercaseBullet, "data-cfx-role=\"bullet-value\"", "x") > GetAttribute(regularBullet, "data-cfx-role=\"bullet-value\"", "x"), "Bullet layout should reserve the transformed series label width before placing the bar.");

        var regularHorizontal = Chart.Create().WithSize(520, 260).WithXLabels("mmmmmmmm", "short").AddHorizontalBar("Values", Points(12, 20)).ToSvg();
        var uppercaseHorizontal = Chart.Create().WithSize(520, 260).WithXLabels("mmmmmmmm", "short").WithTickLabelStyle(style => style.WithTextCase(TextCaseTransform.Uppercase)).AddHorizontalBar("Values", Points(12, 20)).ToSvg();
        Assert(GetAttribute(uppercaseHorizontal, "data-cfx-role=\"horizontal-bar\"", "x") > GetAttribute(regularHorizontal, "data-cfx-role=\"horizontal-bar\"", "x"), "Horizontal charts should reserve transformed category labels before placing their plot.");

        var closePoints = new[] {
            new ChartForgeX.Primitives.ChartPoint(1, 1),
            new ChartForgeX.Primitives.ChartPoint(1.12, 1.12)
        };
        var regularLabels = Chart.Create().WithSize(420, 240).WithDataLabels().WithValueFormatter(_ => "mmmmmmmm").AddScatter("Dense", closePoints).ToSvg();
        var styledChart = Chart.Create().WithSize(420, 240).WithDataLabels().WithValueFormatter(_ => "mmmmmmmm").AddScatter("Dense", closePoints);
        styledChart.Series[0].WithDataLabelStyle(style => style.WithFontSize(36).WithTextCase(TextCaseTransform.Uppercase));
        var styledLabels = styledChart.ToSvg();
        Assert(CountOccurrences(regularLabels, "data-cfx-role=\"data-label\"") == 2 && CountOccurrences(styledLabels, "data-cfx-role=\"data-label\"") == 1, "SVG collision reservations should use the transformed text and resolved point or series font size before accepting labels.");
    }

    private static void SvgHeatmapAndFunnelFitStyledTextVertically() {
        var heatmap = Chart.Create()
            .WithSize(440, 360)
            .WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Always)
            .WithDataLabelStyle(style => style.WithFontSize(42));
        for (var row = 0; row < 8; row++) heatmap.AddHeatmapRow("Row " + row.ToString(CultureInfo.InvariantCulture), Points(1));
        var heatmapDocument = XDocument.Parse(heatmap.ToSvg());
        XNamespace ns = heatmapDocument.Root!.Name.Namespace;
        var heatmapCell = heatmapDocument.Descendants(ns + "rect").First(element => (string?)element.Attribute("data-cfx-role") == "heatmap-cell");
        var heatmapLabel = heatmapDocument.Descendants(ns + "text").First(element => (string?)element.Attribute("data-cfx-role") == "data-label");
        var cellHeight = double.Parse(heatmapCell.Attribute("height")!.Value, CultureInfo.InvariantCulture);
        var cellFontSize = double.Parse(heatmapLabel.Attribute("font-size")!.Value, CultureInfo.InvariantCulture);
        Assert(cellFontSize * 1.2 <= cellHeight, "SVG heatmap labels should fit their resolved font height inside the cell instead of fitting width alone. Font size: " + cellFontSize.ToString(CultureInfo.InvariantCulture) + "; cell height: " + cellHeight.ToString(CultureInfo.InvariantCulture) + ".");

        var funnel = Chart.Create()
            .WithSize(560, 300)
            .WithDataLabels()
            .WithDataLabelStyle(style => style.WithFontSize(42))
            .WithXLabels("Qualified", "Validated", "Closed")
            .AddFunnel("Pipeline", Points(120, 74, 32));
        var funnelDocument = XDocument.Parse(funnel.ToSvg());
        var funnelLabel = funnelDocument.Descendants(ns + "text").First(element => (string?)element.Attribute("data-cfx-role") == "funnel-label");
        var funnelValue = funnelDocument.Descendants(ns + "text").First(element => (string?)element.Attribute("data-cfx-role") == "funnel-value");
        var labelY = double.Parse(funnelLabel.Attribute("y")!.Value, CultureInfo.InvariantCulture);
        var valueY = double.Parse(funnelValue.Attribute("y")!.Value, CultureInfo.InvariantCulture);
        var labelFontSize = double.Parse(funnelLabel.Attribute("font-size")!.Value, CultureInfo.InvariantCulture);
        var valueFontSize = double.Parse(funnelValue.Attribute("font-size")!.Value, CultureInfo.InvariantCulture);
        Assert(valueY - labelY >= (labelFontSize + valueFontSize) * 0.6, "SVG funnel label rows should be spaced from their fitted heights instead of fixed offsets.");
    }
}
