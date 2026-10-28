using ChartForgeX;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

internal static class NativeTurbulenceExamples {
    internal static void Write(string output) {
        foreach (var variant in new[] { "compact", "wide" }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var source = NativeTurbulenceExample.Create(variant, mode);
            var stem = Path.Combine(output, "native-turbulence-" + variant + "-" + mode.ToString().ToLowerInvariant());
            File.WriteAllText(stem + ".native.svg", source.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
            File.WriteAllText(stem + ".baked.svg", source.ToSvg()); File.WriteAllBytes(stem + ".png", source.ToPng());
            File.WriteAllBytes(stem + ".generated.png", NativeTurbulenceExample.Generate(variant).ToPng());
            File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitNativeTurbulence(variant, mode));
            var saved=NativeTurbulenceExample.Save(variant,mode);
            File.WriteAllText(stem+".source.json",saved);
            File.WriteAllBytes(stem+".loaded.png",NativeTurbulenceExample.Load(saved,variant,mode).ToPng());
            foreach (var name in new[] { "rotate", "skew", "reflect", "quarter-turn" }) {
                File.WriteAllText(stem + "." + name + ".source.svg", NativeTurbulenceExample.AffineSource(name));
                File.WriteAllBytes(stem + "." + name + ".native.png", NativeTurbulenceExample.RasterizeAffine(name).ToPng());
            }
        }
    }
}
