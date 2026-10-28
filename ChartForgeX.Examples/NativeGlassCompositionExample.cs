/// <summary>Optional Visuals report: explicitly captured backdrop, local blur replacement, then tint, stroke and sharp foreground.</summary>
public static class NativeGlassCompositionExample {
    public static ChartForgeX.Composition.ImageComposition Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant != "compact" && variant != "wide") throw new ArgumentOutOfRangeException(nameof(variant));
        if (mode != VisualThemeMode.Light && mode != VisualThemeMode.Dark) throw new ArgumentOutOfRangeException(nameof(mode));
        var width = variant == "compact" ? 390 : 1100;
        var height = 400;
        var dark = mode == VisualThemeMode.Dark;
        var background = ChartColor.FromHex(dark ? "#102338" : "#E2E8F0");
        var ink = ChartColor.FromHex(dark ? "#F8FAFC" : "#0F172A");
        var accent = ChartColor.FromHex(dark ? "#38BDF8" : "#0369A1");
        var image = ChartForgeX.Composition.ImageComposition.Create(width, height, background);
        for (var x = -height; x < width; x += 28)
            image.DrawLine(x, 0, x + height, height, accent.WithAlpha(96), 6);
        var coverage = ChartForgeX.Composition.ImageComposition.CreateTransparent(width, height)
            .FillRoundedRectangle(24, 24, width - 48, height - 48, 18, ChartColor.White).ToImage();
        image.BlurRegion(new ChartForgeX.Effects.VisualMask(coverage), new ChartForgeX.Effects.VisualLayerBlur(6));
        image.FillRoundedRectangle(24, 24, width - 48, height - 48, 18,
                ChartColor.FromHex(dark ? "#102338" : "#FFFFFF").WithAlpha(144))
            .StrokeRoundedRectangle(24, 24, width - 48, height - 48, 18, accent.WithAlpha(200), 1)
            .DrawText(44, 48, width - 88, "Service window", 24, ink, emphasized: true)
            .DrawText(44, 92, width - 88, "Local backdrop, retained source facts", 15, ink)
            .DrawImage(NativeAppearanceExample.Create("outline", variant == "compact" ? "compact" : "wide", mode).ToImage(),
                44, 130, width - 88, 230);
        return image;
    }
}
