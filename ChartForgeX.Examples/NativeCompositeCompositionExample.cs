/// <summary>An ordinary optional-Visuals report composites an explicit native capture against an authored destination.</summary>
public static class NativeCompositeCompositionExample {
    /// <summary>Creates a real stencil/cutout report without an implicit page backdrop or second renderer.</summary>
    public static ChartForgeX.Composition.ImageComposition Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light,
        ChartForgeX.Effects.VisualCompositeOperator operation = ChartForgeX.Effects.VisualCompositeOperator.SourceIn) {
        var source = NativeRetainedMaskExample.PrepareSource(variant, mode);
        var width = checked((int)source.Size.Width);
        var layer = source.CaptureLayer(new ChartForgeX.Effects.VisualLayerBlur(0), new VisualRenderOptions(supersampling: 1));
        var theme = VisualTheme.Graphite().Resolve(mode);
        var result = ChartForgeX.Composition.ImageComposition.CreateTransparent(width, 700)
            .FillRoundedRectangle(12, 64, width - 24, 580, 24, theme.Accent.WithAlpha(192))
            .DrawLayer(layer, 0, 52, operation, orderedDither: false);
        // Foreground words are authored after the destination-frame operation, so its transparent source cannot clear them.
        return result.DrawText(20, 14, width - 40, "Explicit " + operation, 20, theme.Foreground, emphasized: true);
    }
}
