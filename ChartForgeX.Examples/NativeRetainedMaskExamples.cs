using System.IO;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static class NativeRetainedMaskExamples {
    internal static void Write(string output) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            foreach (var variant in new[] { "compact", "wide" }) {
                var source = NativeRetainedMaskExample.PrepareSource(variant, mode);
                var masked = source.WithMask(NativeRetainedMaskExample.CreateMask(source));
                var stem = Path.Combine(output, "native-retained-mask-" + variant + "-" + mode.ToString().ToLowerInvariant());
                var native = masked.ToSvg(new VisualSvgOptions(idPrefix: "retained-mask-example", effectProfile: VisualSvgEffectProfile.NativeFilters));
                File.WriteAllText(stem + ".svg", masked.ToSvg());
                File.WriteAllText(stem + ".native.svg", native);
                File.WriteAllBytes(stem + ".png", masked.ToPng());
                File.WriteAllText(stem + ".source.svg", source.ToSvg());
                File.WriteAllText(stem + ".source.json", source.ToArtifact().ToInterchangeJson());
                var saved = masked.ToArtifact().ToInterchangeJson();
                File.WriteAllText(stem + ".json", saved);
                var restored = NativeRetainedMaskExample.Restore(saved, variant, mode);
                File.WriteAllText(stem + ".restored.svg", restored.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
                File.WriteAllBytes(stem + ".restored.png", restored.ToPng());
                var edited = NativeRetainedMaskExample.Restore(saved, variant, mode, messageText: "Approved");
                File.WriteAllText(stem + ".edited.svg", edited.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
                File.WriteAllBytes(stem + ".edited.png", edited.ToPng());
                File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitRetainedMask(variant, mode));
                var color = VisualTheme.Graphite().Resolve(mode).Background.ToCss();
                File.WriteAllText(stem + ".html", "<!doctype html><meta charset='utf-8'><title>Retained native mask</title>" +
                    "<style>body{margin:0;background:" + color + "}svg{display:block;max-width:100%;height:auto}</style>" + native);
            }
        }
    }
}
