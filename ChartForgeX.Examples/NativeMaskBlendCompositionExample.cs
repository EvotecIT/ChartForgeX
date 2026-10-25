/// <summary>An ordinary optional-Visuals report combines a complete captured chart with one explicit destination backdrop.</summary>
public static class NativeMaskBlendCompositionExample {
    /// <summary>Creates a compact or wide report using one alpha fade and the selected explicit native operator.</summary>
    public static ChartForgeX.Composition.ImageComposition Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light,
        ChartForgeX.Effects.VisualBlendMode blendMode = ChartForgeX.Effects.VisualBlendMode.Screen) {
        var source = NativeMaskExample.PrepareSource(variant, mode); var width = variant == "compact" ? 320 : 740;
        var maskPixels = ChartForgeX.Composition.ImageComposition.CreateTransparent(width, 240)
            .FillRectangleLinearGradient(0, 0, width, 240, ChartColor.White, ChartColor.FromRgba(255, 255, 255, 96)).ToImage();
        var mask = new ChartForgeX.Effects.VisualMask(maskPixels);
        var layer = source.CaptureLayer(mask, new VisualRenderOptions(supersampling: 2));
        var background = mode == VisualThemeMode.Dark ? ChartColor.FromHex("#0F172A") : ChartColor.FromHex("#E2E8F0");
        return ChartForgeX.Composition.ImageComposition.Create(width + 64, 304, background)
            .DrawLayer(layer, 32, 32, blendMode);
    }
}
