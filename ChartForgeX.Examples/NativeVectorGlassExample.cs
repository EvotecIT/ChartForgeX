/// <summary>Local backdrop filtering with a separately retained native foreground, using the ordinary optional Visuals canvas.</summary>
public static class NativeVectorGlassExample {
    public static ChartForgeX.Composition.VisualCanvas Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant != "compact" && variant != "wide") throw new ArgumentOutOfRangeException(nameof(variant));
        if (mode != VisualThemeMode.Light && mode != VisualThemeMode.Dark) throw new ArgumentOutOfRangeException(nameof(mode));
        var width = variant == "compact" ? 390 : 1100;
        var dark = mode == VisualThemeMode.Dark;
        var background = ChartColor.FromHex(dark ? "#102338" : "#E2E8F0");
        var accent = ChartColor.FromHex(dark ? "#38BDF8" : "#0369A1");
        var ink = ChartColor.FromHex(dark ? "#F8FAFC" : "#0F172A");
        var backdrop = ChartForgeX.Composition.ImageComposition.Create(width, 400, background);
        for (var x = -400; x < width; x += 28)
            backdrop.DrawLine(x, 0, x + 400, 400, accent.WithAlpha(96), 6);
        var coverage = ChartForgeX.Composition.ImageComposition.CreateTransparent(width, 400)
            .FillRoundedRectangle(24, 24, width - 48, 352, 18, ChartColor.White).ToImage();
        backdrop.BlurRegion(new ChartForgeX.Effects.VisualMask(coverage), new ChartForgeX.Effects.VisualLayerBlur(6));
        backdrop.FillRoundedRectangle(24, 24, width - 48, 352, 18,
                ChartColor.FromHex(dark ? "#102338" : "#FFFFFF").WithAlpha(144))
            .StrokeRoundedRectangle(24, 24, width - 48, 352, 18, accent.WithAlpha(200));
        // Only the explicitly authored backdrop is a bitmap. Foreground captions use the incumbent native text owner.
        return ChartForgeX.Composition.VisualCanvas.Create(width, 400)
            .WithPngOutputScale(1).WithBackdrop(ChartForgeX.Composition.VisualCanvasBackdropStyle.Transparent)
            .WithTheme(VisualTheme.Graphite(), mode).AddImage(backdrop.ToImage(), 0, 0, width, 400)
            .AddText(44, 52, width - 88, "Service window", 24, ink, emphasized: true)
            .AddText(44, 106, width - 88, "Explicit local backdrop", 16, ink)
            .AddText(44, 176, width - 88, "34 ready", 40, accent, emphasized: true)
            .AddText(44, 252, width - 88, "Foreground remains native text", 15, ink);
    }
}
