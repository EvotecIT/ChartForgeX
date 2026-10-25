using System.IO;
using ChartForgeX.Effects;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static class NativeMaskBlendExamples {
    internal static void Write(string output) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) foreach (var variant in new[] { "compact", "wide" }) {
            var layer = NativeMaskExample.Create(variant, mode); var stem = Path.Combine(output, "native-mask-" + variant + "-" + mode.ToString().ToLowerInvariant());
            File.WriteAllText(stem + ".source.svg", layer.Source.ToSvg()); File.WriteAllBytes(stem + ".source.png", layer.Source.ToPng());
            File.WriteAllText(stem + ".svg", layer.Visual.ToSvg()); File.WriteAllBytes(stem + ".png", layer.Visual.ToPng());
            File.WriteAllText(stem + ".html", layer.Visual.ToArtifact("native-mask", VisualArtifactKind.Chart).ToHtmlPage());
            File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitNativeMask(variant, mode));
            foreach (var blend in (VisualBlendMode[])System.Enum.GetValues(typeof(VisualBlendMode))) {
                var report = NativeMaskBlendCompositionExample.Create(variant, mode, blend);
                File.WriteAllBytes(stem + "-" + blend.ToString().ToLowerInvariant() + ".composition.png", report.ToPng());
            }
            File.WriteAllText(stem + ".composition.csharp.txt", ExampleRecipeSource.EmitNativeMaskComposition(variant, mode));
        }
    }
}
