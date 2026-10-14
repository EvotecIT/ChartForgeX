using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static partial class ExpressiveExamples {
    private static ChartGrid CreateThemePresetShowcaseGrid() {
        var executive = CreateThemePresetPreview(
            "Executive",
            "Restrained report palette for board-ready snapshots",
            VisualTheme.Executive(),
            new[] { "Q1", "Q2", "Q3", "Q4" },
            Points(52, 61, 67, 74));

        var product = CreateThemePresetPreview(
            "Product",
            "Vivid geometric styling for launch and adoption views",
            VisualTheme.Product(),
            new[] { "Beta", "GA", "Scale", "Retain" },
            Points(38, 58, 76, 88));

        var editorial = CreateThemePresetPreview(
            "Editorial",
            "Publication-style color and serif typography",
            VisualTheme.Editorial(),
            new[] { "North", "South", "East", "West" },
            Points(44, 53, 62, 68));

        var accessible = CreateThemePresetPreview(
            "Accessible",
            "Colorblind-friendly high-contrast comparisons",
            VisualTheme.Accessible(),
            new[] { "A", "B", "C", "D" },
            Points(31, 46, 59, 72));

        return ChartGrid.Create()
            .WithTitle("Theme Preset Showcase")
            .WithSubtitle("Reusable paired themes share palette, typography, surfaces, guides, and semantic colors")
            .WithTheme(VisualTheme.Executive())
            .WithPanelThemes()
            .WithColumns(2)
            .WithPadding(28)
            .WithPanelSize(420, 260)
            .Add(executive)
            .Add(product)
            .Add(editorial)
            .Add(accessible);
    }

    private static Chart CreateThemePresetPreview(string title, string subtitle, VisualTheme theme, string[] labels, IEnumerable<ChartPoint> points) => Chart.Create()
        .WithTitle(title)
        .WithSubtitle(subtitle)
        .WithXAxis("Stage")
        .WithYAxis("Score")
        .WithTheme(theme)
        .WithSize(420, 260)
        .WithXLabels(labels)
        .AddSmoothArea("Momentum", points);

    private static ChartGrid CreatePaletteSwatchGrid() {
        return ChartGrid.Create()
            .WithTitle("Palette Swatch Showcase")
            .WithSubtitle("Reusable palettes rendered as equal-weight color wheels for quick visual selection")
            .WithTheme(VisualTheme.Executive())
            .WithPanelThemes()
            .WithColumns(2)
            .WithPadding(28)
            .WithPanelSize(420, 260)
            .Add(CreatePalettePreview("Report", "Balanced default reporting colors", ChartPalettes.Report, VisualTheme.Report()))
            .Add(CreatePalettePreview("Colorblind", "Accessible categorical comparison", ChartPalettes.Colorblind, VisualTheme.Colorblind()))
            .Add(CreatePalettePreview("Vivid", "High-energy launch and product views", ChartPalettes.Vivid, VisualTheme.Aurora()))
            .Add(CreatePalettePreview("Pastel", "Soft playful scorecards", ChartPalettes.Pastel, VisualTheme.Candy()))
            .Add(CreatePalettePreview("Editorial", "Refined publication tones", ChartPalettes.Editorial, VisualTheme.Editorial()))
            .Add(CreatePalettePreview("Terminal", "Technical command-center contrast", ChartPalettes.Terminal, VisualTheme.Terminal()));
    }

    private static Chart CreatePalettePreview(string title, string subtitle, ChartColor[] palette, VisualTheme theme) => Chart.Create()
        .WithTitle(title)
        .WithSubtitle(subtitle)
        .WithTheme(theme)
        .WithPalette(palette)
        .WithLegend(false)
        .WithSize(420, 260)
        .WithXLabels("1", "2", "3", "4", "5", "6", "7", "8")
        .AddPolarArea("Palette", Points(1, 1, 1, 1, 1, 1, 1, 1));

}
