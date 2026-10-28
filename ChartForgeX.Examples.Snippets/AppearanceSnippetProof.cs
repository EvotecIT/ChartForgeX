using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Themes;

internal static class AppearanceSnippetProof {
    internal static void Write(string output, List<RecipeResult> results) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            foreach (var variant in new[] { "compact", "wide" }) {
                foreach (var effect in new[] { "outline", "bloom" }) {
                    var id = "native-appearance-" + effect + "-" + variant + "-" + mode;
                    try {
                        var layer = NativeAppearanceExample.Create(effect, variant, mode);
                        var image = layer.ToImage();
                        File.WriteAllText(Path.Combine(output, id + ".svg"), layer.Visual.ToSvg());
                        File.WriteAllBytes(Path.Combine(output, id + ".png"), layer.Visual.ToPng());
                        File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitNativeAppearance(effect, variant, mode));
                        if (!image.Pixels.Any(value => value != 0))
                            throw new InvalidOperationException("Explicit appearance capture lost source pixels.");
                        results.Add(new(id, nameof(NativeAppearanceExample), variant, mode.ToString(), image.Width, image.Height,
                            true, layer.Visual.Regions.Count, Array.Empty<RecipeSeries>(), null));
                    } catch (Exception error) {
                        results.Add(new(id, nameof(NativeAppearanceExample), variant, mode.ToString(), 0, 0,
                            false, 0, Array.Empty<RecipeSeries>(), error.Message));
                    }
                }
            }
        }
    }
}
