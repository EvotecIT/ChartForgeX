using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void RasterScriptBaselinesUseSingleDirectionalShift() {
        static ColorBounds TitleBounds(TextBaseline baseline, double fontSize = 24) {
            var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
                .WithSize(360, 220)
                .WithTitle("Directional baseline")
                .ConfigureTitleStyle(style => style.WithColor("#ff00ff").WithFontSize(fontSize).WithBaseline(baseline))
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
        var regularBullet = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(560, 260).WithDataLabels().AddBullet("nnnnnnnnnnnn", 82, 90).ToSvg();
        var uppercaseBullet = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(560, 260).WithDataLabels().ConfigureDataLabelStyle(style => style.WithTextCase(TextCaseTransform.Uppercase)).AddBullet("nnnnnnnnnnnn", 82, 90).ToSvg();
        Assert(GetAttribute(uppercaseBullet, "data-cfx-role=\"bullet-value\"", "x") > GetAttribute(regularBullet, "data-cfx-role=\"bullet-value\"", "x"), "Bullet layout should reserve the transformed series label width before placing the bar.");

        var regularHorizontal = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(520, 260).WithXLabels("nnnnnnnn", "short").AddHorizontalBar("Values", Points(12, 20)).ToSvg();
        var uppercaseHorizontal = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(520, 260).WithXLabels("nnnnnnnn", "short").ConfigureTickLabelStyle(style => style.WithTextCase(TextCaseTransform.Uppercase)).AddHorizontalBar("Values", Points(12, 20)).ToSvg();
        Assert(GetAttribute(uppercaseHorizontal, "data-cfx-role=\"horizontal-bar\"", "x") > GetAttribute(regularHorizontal, "data-cfx-role=\"horizontal-bar\"", "x"), "Horizontal charts should reserve transformed category labels before placing their plot.");

        var closePoints = new[] {
            new ChartForgeX.Primitives.ChartPoint(1, 1),
            new ChartForgeX.Primitives.ChartPoint(1.12, 1.12)
        };
        var regularLabels = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(420, 240).WithDataLabels().WithValueFormatter(_ => "mmmmmmmm").AddScatter("Dense", closePoints).ToSvg();
        var styledChart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(420, 240).WithDataLabels().WithValueFormatter(_ => "mmmmmmmm").AddScatter("Dense", closePoints);
        styledChart.Series[0].ConfigureDataLabelStyle(style => style.WithFontSize(36).WithTextCase(TextCaseTransform.Uppercase));
        var styledLabels = styledChart.ToSvg();
        Assert(CountVisibleDataLabels(regularLabels) == 2 && CountVisibleDataLabels(styledLabels) >= 1, "Measured placement should retain labels when an alternative lane fits the transformed text.");
        Assert(Rendering.ChartLabelScene.Inspect(styledLabels, FontSpec.SystemSans()).LabelLabel == 0, "Styled labels must not overlap after relocation or shortening.");
    }

    private static void SvgHeatmapAndFunnelFitStyledTextVertically() {
        var heatmap = Chart.Create()
            .WithSize(440, 360)
            .WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Always)
            .ConfigureDataLabelStyle(style => style.WithFontSize(42));
        for (var row = 0; row < 8; row++) heatmap.AddHeatmapRow("Row " + row.ToString(CultureInfo.InvariantCulture), Points(1));
        var matrix = PreparedFamily(heatmap);
        var heatmapCells = matrix.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "heatmap-cell-shape").ToArray();
        var heatmapLabels = FamilyLabels(matrix, "data-label");
        Assert(heatmapCells.Length == 8 && heatmapLabels.Length == 8, "Always-visible heatmap values should retain every fitted label.");
        Assert(heatmapLabels.All(label => label.Text.Metrics.Height <= heatmapCells[0].Bounds.Height - 6 + .000001),
            "Shared heatmap text should fit its measured height inside the cell padding.");

        var funnel = Chart.Create()
            .WithSize(560, 300)
            .WithDataLabels()
            .ConfigureDataLabelStyle(style => style.WithFontSize(42))
            .WithXLabels("Qualified", "Validated", "Closed")
            .AddFunnel("Pipeline", Points(120, 74, 32));
        var stages = PreparedFamily(funnel);
        var funnelLabels = FamilyLabels(stages, "funnel-label");
        Assert(funnelLabels.Length == 3, "Every funnel stage should retain a fitted category and value label.");
        for (var index = 0; index < funnelLabels.Length; index++) {
            var label = funnelLabels[index];
            var region = stages.Regions.Single(region => region.Id == label.Id && region.Role == "funnel-label");
            Assert(label.Text.Metrics.Height <= region.Bounds.Height + .000001 && label.Text.Metrics.Width <= region.Bounds.Width + .000001,
                "Funnel labels should fit their complete native measured bounds inside the stage.");
            Assert(region.Label!.Contains(":" , StringComparison.Ordinal), "Stage semantics should retain both the category and value when text is fitted.");
        }
    }
}
