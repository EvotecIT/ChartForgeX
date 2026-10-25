using System.IO;
using ChartForgeX.Composition;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

/// <summary>Uses the same native source/program for actual compact/wide paired-theme effect outputs.</summary>
internal static class NativeDropShadowExamples {
    internal static void Write(string output) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) foreach (var variant in new[] { "compact", "wide" }) {
            var layer = NativeDropShadowExample.Create(variant, mode);
            var stem = Path.Combine(output, "native-drop-shadow-" + variant + "-" + mode.ToString().ToLowerInvariant());
            File.WriteAllText(stem + ".source.svg", layer.Source.ToSvg()); File.WriteAllBytes(stem + ".source.png", layer.Source.ToPng());
            File.WriteAllText(stem + ".svg", layer.Visual.ToSvg()); File.WriteAllBytes(stem + ".png", layer.Visual.ToPng());
            File.WriteAllText(stem + ".html", layer.Visual.ToArtifact("native-drop-shadow", VisualArtifactKind.Chart).ToHtmlPage());
            File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitNativeDropShadow(variant, mode));
        }
        var source = NativeDropShadowExample.FilterSource();
        var result = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        File.WriteAllText(Path.Combine(output, "native-drop-shadow-filter.source.svg"), source);
        File.WriteAllBytes(Path.Combine(output, "native-drop-shadow-filter.png"), ImageComposition.FromImage(result.Image).ToPng());
    }
}
