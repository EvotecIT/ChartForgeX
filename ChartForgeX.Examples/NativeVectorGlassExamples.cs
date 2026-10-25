using System.IO;
using ChartForgeX;
using ChartForgeX.Themes;

internal static class NativeVectorGlassExamples {
    internal static void Write(string output) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            foreach (var variant in new[] { "compact", "wide" }) {
                var canvas = NativeVectorGlassExample.Create(variant, mode);
                var stem = Path.Combine(output, "native-vector-glass-" + variant + "-" + mode);
                File.WriteAllText(stem + ".svg", canvas.ToSvg());
                File.WriteAllBytes(stem + ".png", canvas.ToPng());
                File.WriteAllText(stem + ".html", canvas.ToHtmlPage());
                File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitNativeVectorGlass(variant, mode));
            }
        }
    }
}
