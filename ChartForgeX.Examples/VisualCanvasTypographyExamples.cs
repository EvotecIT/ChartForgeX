using System.IO;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static class VisualCanvasTypographyExamples {
    private static readonly double[] ProofScales = { 1, 1.25, 1.5 };

    public static void Write(string output) {
        Directory.CreateDirectory(output);
        Save(output, VisualThemeMode.Light);
        Save(output, VisualThemeMode.Dark);
    }

    public static void WriteProofMatrix(string output) {
        Directory.CreateDirectory(output);
        foreach (var size in new[] { (Width: 1920, Height: 1080), (Width: 2560, Height: 1440) }) {
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
                foreach (var scale in ProofScales) SaveProof(output, size.Width, size.Height, mode, scale);
            }
        }

        var tooSmall = CreateCanvas(1920, 1080, VisualThemeMode.Dark)
            .AddInfoTile(48, 64, 320, 72, "PC", "HOSTNAME", "A very long workstation name",
                "Windows 11 Enterprise workstation", textScale: 1.5);
        tooSmall.SavePng(Path.Combine(output, "tile-text-too-small-1.5-dark.png"));
        tooSmall.SaveSvg(Path.Combine(output, "tile-text-too-small-1.5-dark.svg"));
    }

    private static void Save(string output, VisualThemeMode mode) {
        var canvas = CreateCanvas(1200, 630, mode)
            .AddInfoTile(48, 100, 520, 164, "PC", "WORKSTATION", "DEV-WKS-01", "Windows 11 Enterprise",
                ChartColor.FromHex("#2F80FF"), surfaceStyle: VisualCanvasInfoTileSurfaceStyle.Raised)
            .AddInfoTile(632, 100, 520, 164, "CPU", "PROCESSOR", "Intel Core i7", "Average load 23%",
                ChartColor.FromHex("#22A7FF"), textScale: 1.25,
                surfaceStyle: VisualCanvasInfoTileSurfaceStyle.Raised,
                miniChartKind: VisualCanvasInfoTileMiniChartKind.Sparkline,
                miniChartValues: new[] { 18d, 26d, 22d, 37d, 29d, 23d });
        var suffix = mode == VisualThemeMode.Dark ? "dark" : "light";
        canvas.SaveSvg(Path.Combine(output, "visual-canvas-tile-text-" + suffix + ".svg"));
        canvas.SavePng(Path.Combine(output, "visual-canvas-tile-text-" + suffix + ".png"));
    }

    private static void SaveProof(string output, int width, int height, VisualThemeMode mode,
        double scale) {
        var canvas = CreateCanvas(width, height, mode)
            .AddInfoTile(48, 64, 680, 168, "CPU", "PROCESSOR", "Intel Core i7 14700K",
                "20 cores, average load 23%", ChartColor.FromHex("#2F80FF"), 0.23,
                surfaceStyle: VisualCanvasInfoTileSurfaceStyle.Raised,
                miniChartKind: VisualCanvasInfoTileMiniChartKind.Sparkline,
                miniChartValues: new[] { 18d, 26d, 22d, 37d, 29d, 23d },
                textScale: scale);
        var suffix = mode == VisualThemeMode.Dark ? "dark" : "light";
        var name = "tile-text-" + scale.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            + "-" + width + "x" + height + "-" + suffix;
        canvas.SavePng(Path.Combine(output, name + ".png"));
        canvas.SaveSvg(Path.Combine(output, name + ".svg"));
    }

    private static VisualCanvas CreateCanvas(int width, int height, VisualThemeMode mode) =>
        VisualCanvas.Create(width, height)
            .WithTheme(VisualTheme.Graphite(), mode)
            .WithBackdrop(VisualCanvasBackdropStyle.Plain)
            .WithBackground(
                mode == VisualThemeMode.Dark ? ChartColor.FromHex("#071426") : ChartColor.FromHex("#EAF1FA"),
                mode == VisualThemeMode.Dark ? ChartColor.FromHex("#020713") : ChartColor.FromHex("#F8FAFC"));
}
