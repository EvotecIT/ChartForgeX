using System;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PairedThemesApplyReusablePresentation() {
        var theme = VisualTheme.Graphite().WithPalette(ChartPalettes.FromHex("#123456", "#EC4899"))
            .WithFontFamily(ChartFontStacks.Mono).Configure(builder => {
                foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
                    var colors = builder.Colors(mode);
                    colors.Foreground = ChartColor.FromRgb(17, 17, 17);
                    colors.MutedForeground = ChartColor.FromRgb(34, 34, 34);
                    colors.Grid = ChartColor.FromRgb(51, 51, 51);
                    colors.Axis = ChartColor.FromRgb(68, 68, 68);
                    colors.Positive = ChartColor.FromRgb(5, 150, 105);
                    colors.Warning = ChartColor.FromRgb(245, 158, 11);
                    colors.Negative = ChartColor.FromRgb(220, 38, 38);
                }
                builder.CardRadius = 8; builder.PlotRadius = 6; builder.CardShadowOpacity = 0;
            });

        var model = Chart.Create().WithTheme(theme).WithLegend().AddLine("Values", Points(1, 2, 3));
        var prepared = PreparedFamily(model);
        var svg = prepared.ToSvg();
        Assert(svg.Contains("#123456", StringComparison.Ordinal) && svg.Contains(ChartFontStacks.Mono, StringComparison.Ordinal),
            "A reusable theme should supply its palette and font to charts.");
        var card = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == "frame-card");
        Assert(card.Radius == 8 && card.Fill.Equals(theme.Resolve(VisualThemeMode.Light).ElevatedSurface),
            "The native frame should retain its authored radius and card fill.");
        var legendLabels = FamilyLabels(prepared, "legend-label");
        Assert(prepared.Scene.Nodes.OfType<VisualSceneMark>().Any(mark => mark.Stroke.Equals(ChartColor.FromHex("#123456")))
            && legendLabels.Length > 0 && legendLabels.All(label => label.Text.Style.Font.Family == ChartFontStacks.Mono),
            "The native scene should retain authored series ink and font.");
        var originalPng = prepared.ToPng();
        model.ConfigureTheme(builder => builder.CardRadius = 0);
        var square = PreparedFamily(model);
        Assert(square.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == "frame-card").Radius == 0,
            "Changing the model theme should change the newly prepared card geometry.");
        Assert(originalPng.Length > 64 && !originalPng.SequenceEqual(square.ToPng()), "Authored geometry should affect PNG output.");
        Assert(card.Radius == 8 && originalPng.SequenceEqual(prepared.ToPng()), "A prepared output should retain its captured geometry.");
        Assert(theme.CardRadius == 8, "Configuring a chart should preserve the reusable source theme.");

        var grid = ChartGrid.Create().WithTitle("Themed grid").WithTheme(theme)
            .Add(Chart.Create().AddLine("Values", Points(1, 2, 3)));
        Assert(grid.ToSvg().Contains(ChartFontStacks.Mono, StringComparison.Ordinal), "The grid theme should supply shared panel typography.");
        foreach (var preset in new[] { VisualTheme.Executive(), VisualTheme.Product(), VisualTheme.PeopleInfographic(), VisualTheme.Editorial(), VisualTheme.Accessible() }) {
            var colors = preset.Resolve(VisualThemeMode.Light);
            Assert(colors.Palette.Count >= 8 && preset.Typography.Family.Length > 0, "Named themes should include useful palettes and font stacks.");
            Assert(colors.Foreground.A > 0 && colors.MutedForeground.A > 0 && colors.Grid.A > 0, "Named themes should include visible text and guide colors.");
            Assert(preset.CardRadius <= 24, "Named themes should retain deliberately authored surface geometry.");
        }

        var productSvg = Chart.Create().WithTheme(VisualTheme.Product()).AddBar("Adoption", Points(42, 58, 71)).ToSvg();
        Assert(productSvg.Contains(ChartFontStacks.Geometric, StringComparison.Ordinal) && productSvg.Contains("#0EA5E9", StringComparison.Ordinal),
            "The Product theme should render its authored font and palette.");
        var peopleSvg = Chart.Create().WithTheme(VisualTheme.PeopleInfographic()).AddDonut("Audience", Points(60, 40)).ToSvg();
        Assert(peopleSvg.Contains("#06B6D4", StringComparison.Ordinal) && peopleSvg.Contains("#DB2777", StringComparison.Ordinal),
            "The PeopleInfographic theme should render its cyan and magenta palette.");

        AssertThrows<ArgumentNullException>(() => Chart.Create().WithTheme(null!), "Charts should reject null themes.");
        AssertThrows<ArgumentNullException>(() => ChartGrid.Create().WithTheme(null!), "Grids should reject null themes.");
        AssertThrows<ArgumentNullException>(() => VisualTheme.Graphite().Configure(null!), "Themes should require a configuration callback.");
        AssertThrows<ArgumentException>(() => VisualTheme.Graphite().WithPalette(Array.Empty<ChartColor>()), "Themes should reject empty categorical palettes.");
        AssertThrows<ArgumentException>(() => VisualTheme.Graphite().WithFontFamily(" "), "Themes should reject empty font stacks.");
        AssertThrows<ArgumentOutOfRangeException>(() => theme.Configure(builder => builder.CardRadius = -1), "Themes should reject negative geometry.");
    }
}
