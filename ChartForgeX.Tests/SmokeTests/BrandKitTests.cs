using System;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void BrandKitsApplyReusableTokens() {
        var brandKit = ChartBrandKit.Create()
            .WithPalette("#123456", "#EC4899")
            .WithFontFamily(ChartFontStacks.Mono)
            .WithTextColors(ChartColor.FromRgb(17, 17, 17), ChartColor.FromRgb(34, 34, 34))
            .WithGuideColors(ChartColor.FromRgb(51, 51, 51), ChartColor.FromRgb(68, 68, 68))
            .WithSemanticColors(ChartColor.FromRgb(5, 150, 105), ChartColor.FromRgb(245, 158, 11), ChartColor.FromRgb(220, 38, 38))
            .WithSurfaceStyle(ChartSurfaceStyle.Framed);

        var brandedModel = Chart.Create().WithBrandKit(brandKit).WithLegend().AddLine("Values", Points(1, 2, 3));
        var brandedPrepared = PreparedFamily(brandedModel);
        var brandedChart = brandedPrepared.ToSvg();
        Assert(brandedChart.Contains("#123456", StringComparison.Ordinal) && brandedChart.Contains(ChartFontStacks.Mono, StringComparison.Ordinal), "Brand kits should apply palette and font tokens to charts.");
        var framedCard = brandedPrepared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == "frame-card");
        Assert(framedCard.Radius == 8 && framedCard.Fill.Equals(brandedModel.Options.Theme.CardBackground),
            "The Framed brand surface should retain its eight-pixel radius and authored card fill in native geometry.");
        var legendLabels = FamilyLabels(brandedPrepared, "legend-label");
        Assert(brandedPrepared.Scene.Nodes.OfType<VisualSceneMark>().Any(mark => mark.Stroke.Equals(ChartColor.FromHex("#123456")))
            && legendLabels.Length > 0 && legendLabels.All(label => label.Text.Style.Font.Family == ChartFontStacks.Mono),
            "The native scene should retain the brand's series ink and font rather than substituting default tokens.");
        var framedPng = brandedPrepared.ToPng();
        brandedModel.Options.Theme.WithSurfaceStyle(ChartSurfaceStyle.Flat);
        var flatPrepared = PreparedFamily(brandedModel);
        Assert(flatPrepared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == "frame-card").Radius == 0,
            "The Flat brand surface should use square native card geometry.");
        Assert(framedPng.Length > 64 && !framedPng.SequenceEqual(flatPrepared.ToPng()), "Brand surface geometry should affect native PNG output.");
        Assert(framedCard.Radius == 8 && framedPng.SequenceEqual(brandedPrepared.ToPng()), "Changing the model's surface style should preserve an already prepared branded output.");

        var brandedGrid = ChartGrid.Create()
            .WithTitle("Brand grid")
            .WithBrandKit(brandKit)
            .Add(Chart.Create().AddLine("Values", Points(1, 2, 3)))
            .ToSvg();
        Assert(brandedGrid.Contains(ChartFontStacks.Mono, StringComparison.Ordinal), "Brand kits should apply to chart grids.");

        var brandedTheme = ChartTheme.Light().WithBrandKit(brandKit);
        Assert(brandedTheme.Palette[0].ToHex() == "#123456" && brandedTheme.FontFamily == ChartFontStacks.Mono, "Brand kits should apply directly to themes.");
        Assert(brandedTheme.Text.ToHex() == "#111111" && brandedTheme.Grid.ToHex() == "#333333", "Brand kits should apply text and guide tokens to themes.");
        Assert(brandedTheme.Positive.ToHex() == "#059669" && brandedTheme.Warning.ToHex() == "#F59E0B" && brandedTheme.Negative.ToHex() == "#DC2626", "Brand kits should apply semantic tokens to themes.");

        foreach (var preset in new[] { ChartBrandKit.Executive(), ChartBrandKit.Product(), ChartBrandKit.PeopleInfographic(), ChartBrandKit.Editorial(), ChartBrandKit.Accessible() }) {
            var presetTheme = ChartTheme.Light().WithBrandKit(preset);
            Assert(presetTheme.Palette.Length >= 8, "Brand kit presets should include broad palettes.");
            Assert(presetTheme.FontFamily.Length > 0, "Brand kit presets should include font stacks.");
            Assert(presetTheme.Text.A > 0 && presetTheme.MutedText.A > 0 && presetTheme.Grid.A > 0, "Brand kit presets should include readable visual tokens.");
            Assert(presetTheme.CornerRadius <= 24, "Brand kit presets should apply a deliberate surface style.");
        }

        var productSvg = Chart.Create()
            .WithBrandKit(ChartBrandKit.Product())
            .AddBar("Adoption", Points(42, 58, 71))
            .ToSvg();
        Assert(productSvg.Contains(ChartFontStacks.Geometric, StringComparison.Ordinal) && productSvg.Contains("#0EA5E9", StringComparison.Ordinal), "Product brand kit should render with its preset font and palette.");

        var peopleSvg = Chart.Create()
            .WithTheme(ChartTheme.PeopleInfographic())
            .AddDonut("Audience", Points(60, 40))
            .ToSvg();
        Assert(peopleSvg.Contains("#06B6D4", StringComparison.Ordinal) && peopleSvg.Contains("#DB2777", StringComparison.Ordinal), "People infographic theme should render with cyan and magenta visual tokens.");

        AssertThrows<ArgumentNullException>(() => Chart.Create().WithBrandKit(null!), "Charts should reject null brand kits.");
        AssertThrows<ArgumentNullException>(() => ChartGrid.Create().WithBrandKit(null!), "Chart grids should reject null brand kits.");
        AssertThrows<ArgumentNullException>(() => ChartTheme.Light().WithBrandKit(null!), "Themes should reject null brand kits.");
        AssertThrows<ArgumentException>(() => ChartBrandKit.Create().WithPalette(Array.Empty<ChartColor>()), "Brand kits should reject empty palettes.");
        AssertThrows<ArgumentException>(() => ChartBrandKit.Create().WithFontFamily(" "), "Brand kits should reject empty font stacks.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartBrandKit.Create().WithSurfaceStyle((ChartSurfaceStyle)999), "Brand kits should reject unknown surface styles.");
    }
}
