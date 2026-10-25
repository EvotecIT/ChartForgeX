using System.IO;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static class NativeAppearanceExamples {
    internal static void Write(string output) {
        NativeVectorGlassExamples.Write(output);
        NativeRetainedAppearanceExamples.Write(output);
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            foreach (var variant in new[] { "compact", "wide" }) {
                foreach (var effect in new[] { "outline", "bloom", "glow", "inner" }) {
                    var layer = NativeAppearanceExample.Create(effect, variant, mode);
                    var stem = Path.Combine(output, "native-appearance-" + effect + "-" + variant + "-" + mode);
                    File.WriteAllText(stem + ".svg", layer.Visual.ToSvg());
                    File.WriteAllBytes(stem + ".png", layer.Visual.ToPng());
                    File.WriteAllText(stem + ".source.json", layer.Source.ToArtifact().ToInterchangeJson());
                    File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitNativeAppearance(effect, variant, mode));
                    if (effect == "inner") {
                        var retained = layer.Source.WithInnerShadow(NativeAppearanceExample.InnerShadowPolicy(mode));
                        File.WriteAllText(stem + ".native-filter.svg", retained.ToSvg(new ChartForgeX.Rendering.VisualSvgOptions(
                            effectProfile: ChartForgeX.Rendering.VisualSvgEffectProfile.NativeFilters)));
                        File.WriteAllText(stem + ".effect.json", layer.ToArtifact().ToInterchangeJson());
                    }
                    if (effect == "glow") {
                        var filterSource = NativeAppearanceExample.AlphaGlowFilterSource(mode);
                        var imported = ChartForgeX.SvgRaster.SvgRasterizer.Rasterize(filterSource, strict: true,
                            filters: new ChartForgeX.SvgRaster.SvgRasterFilterOptions());
                        File.WriteAllText(stem + ".filter.source.svg", filterSource);
                        File.WriteAllBytes(stem + ".filter.native.png", imported.Image.ToPng());
                        var retained = layer.Source.WithDropShadow(NativeAppearanceExample.AlphaGlowPolicy(mode));
                        File.WriteAllText(stem + ".native-filter.svg", retained.ToSvg(new ChartForgeX.Rendering.VisualSvgOptions(
                            effectProfile: ChartForgeX.Rendering.VisualSvgEffectProfile.NativeFilters)));
                        File.WriteAllText(stem + ".effect.json", layer.ToArtifact().ToInterchangeJson());
                    }
                }
                var glass = NativeGlassCompositionExample.Create(variant, mode);
                var image = glass.ToImage();
                var canvas = VisualCanvas.Create(image.Width, image.Height).WithBackdrop(VisualCanvasBackdropStyle.Transparent)
                    .AddImage(image, 0, 0, image.Width, image.Height);
                var glassStem = Path.Combine(output, "native-glass-" + variant + "-" + mode);
                File.WriteAllText(glassStem + ".svg", canvas.ToSvg());
                File.WriteAllBytes(glassStem + ".png", glass.ToPng());
                File.WriteAllText(glassStem + ".csharp.txt", ExampleRecipeSource.EmitNativeGlassComposition(variant, mode));
            }
        }
    }
}
