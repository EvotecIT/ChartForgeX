using System.IO;
using ChartForgeX;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static class NativeRetainedAppearanceExamples {
    internal static void Write(string output) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            foreach (var variant in new[] { "compact", "wide" }) {
                var source = NativeRetainedAppearanceExample.Create(variant, mode);
                var layer = NativeRetainedAppearanceExample.Capture(variant, mode);
                var stem = Path.Combine(output, "native-retained-appearance-" + variant + "-" + mode);
                File.WriteAllText(stem + ".svg", source.ToSvg());
                File.WriteAllText(stem + ".native.svg", source.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
                File.WriteAllBytes(stem + ".png", source.ToPng());
                File.WriteAllText(stem + ".json", source.ToArtifact().ToInterchangeJson());
                File.WriteAllText(stem + ".capture.svg", layer.Visual.ToSvg());
                File.WriteAllBytes(stem + ".capture.png", layer.Visual.ToPng());
                File.WriteAllText(stem + ".capture.json", layer.ToArtifact().ToInterchangeJson());
                File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitNativeRetainedAppearance(variant, mode));
            }
        }
    }
}
