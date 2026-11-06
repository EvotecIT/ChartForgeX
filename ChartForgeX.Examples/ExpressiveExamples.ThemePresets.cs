using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static partial class ExpressiveExamples {
    private static ChartGrid CreatePaletteSwatchGrid() {
        return ChartGrid.Create()
            .WithTitle("Palette Swatch Showcase")
            .WithSubtitle("Reusable palettes rendered as equal-weight color wheels for quick visual selection")
            .WithTheme(VisualTheme.Graphite())
            .WithPanelThemes()
            .WithColumns(2)
            .WithPadding(28)
            .WithPanelSize(420, 260)
            .Add(CreatePalettePreview("Report", "Balanced default reporting colors", ChartPalettes.Report, VisualTheme.Graphite()))
            .Add(CreatePalettePreview("Colorblind", "Accessible categorical comparison", ChartPalettes.Colorblind, VisualTheme.Accessible()))
            .Add(CreatePalettePreview("Vivid", "High-energy launch and product views", ChartPalettes.Vivid, VisualTheme.Graphite()))
            .Add(CreatePalettePreview("Pastel", "Soft playful scorecards", ChartPalettes.Pastel, VisualTheme.Graphite()))
            .Add(CreatePalettePreview("Editorial", "Refined publication tones", ChartPalettes.Editorial, VisualTheme.Graphite()))
            .Add(CreatePalettePreview("Terminal", "Technical command-center contrast", ChartPalettes.Terminal, VisualTheme.Graphite()));
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
