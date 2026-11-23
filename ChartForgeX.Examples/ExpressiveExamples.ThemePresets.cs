using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static partial class ExpressiveExamples {
    private static ChartGrid CreatePaletteSwatchGrid() {
        return ChartGrid.Create()
            .Title("Palette Swatch Showcase")
            .Subtitle("Reusable palettes rendered as equal-weight color wheels for quick visual selection")
            .Theme(VisualTheme.Graphite())
            .UsePanelThemes()
            .Columns(2)
            .Padding(28)
            .PanelSize(420, 260)
            .Add(CreatePalettePreview("Report", "Balanced default reporting colors", ChartPalettes.Report, VisualTheme.Graphite()))
            .Add(CreatePalettePreview("Colorblind", "Accessible categorical comparison", ChartPalettes.Colorblind, VisualTheme.Accessible()))
            .Add(CreatePalettePreview("Vivid", "High-energy launch and product views", ChartPalettes.Vivid, VisualTheme.Graphite()))
            .Add(CreatePalettePreview("Pastel", "Soft playful scorecards", ChartPalettes.Pastel, VisualTheme.Graphite()))
            .Add(CreatePalettePreview("Editorial", "Refined publication tones", ChartPalettes.Editorial, VisualTheme.Graphite()))
            .Add(CreatePalettePreview("Terminal", "Technical command-center contrast", ChartPalettes.Terminal, VisualTheme.Graphite()));
    }

    private static Chart CreatePalettePreview(string title, string subtitle, ChartColor[] palette, VisualTheme theme) => Chart.Create()
        .Title(title)
        .Subtitle(subtitle)
        .Theme(theme)
        .Palette(palette)
        .Legend(false)
        .Size(420, 260)
        .XLabels("1", "2", "3", "4", "5", "6", "7", "8")
        .AddPolarArea("Palette", Points(1, 1, 1, 1, 1, 1, 1, 1));

}
