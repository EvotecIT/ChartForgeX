using System;
using System.Collections.Generic;
using System.IO;
using ChartForgeX;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

internal static class TurbulenceSnippetProof {
    internal static void Write(string output, List<RecipeResult> results) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) foreach (var variant in new[] { "compact", "wide" }) {
            var id = "native-turbulence-" + variant + "-" + mode;
            try {
                var source = NativeTurbulenceExample.Create(variant, mode);
                File.WriteAllText(Path.Combine(output, id + ".native.svg"), source.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
                File.WriteAllText(Path.Combine(output, id + ".baked.svg"), source.ToSvg());
                File.WriteAllBytes(Path.Combine(output, id + ".png"), source.ToPng());
                File.WriteAllBytes(Path.Combine(output, id + ".generated.png"), NativeTurbulenceExample.Generate(variant).ToPng());
                File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitNativeTurbulence(variant, mode));
                var saved=NativeTurbulenceExample.Save(variant,mode);
                File.WriteAllText(Path.Combine(output,id+".source.json"),saved);
                File.WriteAllBytes(Path.Combine(output,id+".loaded.png"),NativeTurbulenceExample.Load(saved,variant,mode).ToPng());
                foreach (var name in new[] { "rotate", "skew", "reflect", "quarter-turn" }) {
                    File.WriteAllText(Path.Combine(output, id + "." + name + ".source.svg"), NativeTurbulenceExample.AffineSource(name));
                    File.WriteAllBytes(Path.Combine(output, id + "." + name + ".native.png"), NativeTurbulenceExample.RasterizeAffine(name).ToPng());
                }
                results.Add(new(id, nameof(NativeTurbulenceExample), variant, mode.ToString(),
                    (int)source.Size.Width, (int)source.Size.Height, true, source.Regions.Count, Array.Empty<RecipeSeries>(), null));
            } catch (Exception error) {
                results.Add(new(id, nameof(NativeTurbulenceExample), variant, mode.ToString(), 0, 0,
                    false, 0, Array.Empty<RecipeSeries>(), error.Message));
            }
        }
    }
}
