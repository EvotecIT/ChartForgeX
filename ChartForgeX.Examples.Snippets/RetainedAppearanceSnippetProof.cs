using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static class RetainedAppearanceSnippetProof {
    internal static void Write(string output, List<RecipeResult> results) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            foreach (var variant in new[] { "compact", "wide" }) {
                var id = "retained-appearance-" + variant + "-" + mode;
                try {
                    var root = NativeRetainedAppearanceExample.Create(variant, mode);
                    var json = root.ToArtifact().ToInterchangeJson();
                    var replayed = NativeRetainedAppearanceExample.RestoreRoot(json, variant, mode);
                    var layer = NativeRetainedAppearanceExample.Capture(variant, mode);
                    var saved = layer.ToArtifact().ToInterchangeJson();
                    var restored = NativeRetainedAppearanceExample.RestoreLayer(saved, variant, mode);
                    File.WriteAllText(Path.Combine(output, id + ".native.svg"), root.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
                    File.WriteAllText(Path.Combine(output, id + ".json"), json);
                    File.WriteAllText(Path.Combine(output, id + ".capture.json"), saved);
                    File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitNativeRetainedAppearance(variant, mode));
                    if (!root.ToPng().SequenceEqual(replayed.ToPng()) || !layer.ToImage().Pixels.SequenceEqual(restored.ToImage().Pixels))
                        throw new InvalidOperationException("Explicit retained/terminal replay changed native pixels.");
                    results.Add(new(id, nameof(NativeRetainedAppearanceExample), variant, mode.ToString(),
                        (int)root.Size.Width, (int)root.Size.Height, true, root.Regions.Count, Array.Empty<RecipeSeries>(), null));
                } catch (Exception error) {
                    results.Add(new(id, nameof(NativeRetainedAppearanceExample), variant, mode.ToString(), 0, 0,
                        false, 0, Array.Empty<RecipeSeries>(), error.Message));
                }
            }
        }
    }
}
