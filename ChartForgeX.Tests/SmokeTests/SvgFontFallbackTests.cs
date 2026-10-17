using System;
using System.Linq;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SvgFontStacksEndWithGenericFamily() {
        const string Missing = "CFX Missing Proof Face 7C1D";
        var chart = Chart.Create().WithTitle("Fallback").WithLegend(true).WithFontFamily(Missing).AddLine("Values", Points(1, 2, 3));
        var svg = chart.ToSvg();
        var families = Regex.Matches(svg, "font-family=\"([^\"]*)\"").Cast<Match>().Select(match => match.Groups[1].Value).Distinct().ToArray();
        Assert(families.Length > 0 && families.All(EndsWithGenericFamily), "Every SVG font stack should end with a generic family: " + string.Join(" | ", families));
        Assert(svg.Contains("font-family=\"" + Missing + ", sans-serif\"", StringComparison.Ordinal), "A single named family should fall back to sans-serif in SVG output.");

        var prepared = Chart.Create().AddBar("Values", Points(4, 5, 6)).Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(480, 300)),
            VisualTheme.Graphite(), VisualThemeMode.Light, new VisualFrame("Prepared", "Registered proof fonts", showSurface: false), FontSpec.FromFamily(Missing)));
        Assert(prepared.ToSvg().Contains("font-family=\"" + Missing + ", sans-serif\"", StringComparison.Ordinal), "Prepared-scene SVG should end registered proof font stacks with a generic family.");

        Assert(TypographyCss.FamilyWithGenericFallback("Consolas") == "Consolas, monospace", "Monospace faces should fall back to monospace.");
        Assert(TypographyCss.FamilyWithGenericFallback("Georgia") == "Georgia, serif", "Serif faces should fall back to serif.");
        Assert(TypographyCss.FamilyWithGenericFallback(ChartFontStacks.SystemSans) == ChartFontStacks.SystemSans, "Stacks that already end in a generic family should stay unchanged.");
        Assert(TypographyCss.FamilyWithGenericFallback("'Brand, Inc', system-ui") == "'Brand, Inc', system-ui", "Quoted commas should not hide an existing generic family.");
        Assert(TypographyCss.FamilyWithGenericFallback("inherit") == "inherit", "CSS-wide keywords should stay unchanged.");

        // The appended generic family never changes which face the rasterizer picks, so SVG-derived PNG output keeps parity.
        var withFallback = SvgRasterizer.ToImage(svg).Pixels;
        var withoutFallback = SvgRasterizer.ToImage(svg.Replace(Missing + ", sans-serif", Missing)).Pixels;
        Assert(withFallback.SequenceEqual(withoutFallback), "The generic fallback should not change rasterized SVG text.");
    }

    private static bool EndsWithGenericFamily(string stack) {
        var last = stack.Split(',').Last().Trim().Trim('\'', '"');
        return TypographyFontResolver.IsGenericFamily(last);
    }
}
