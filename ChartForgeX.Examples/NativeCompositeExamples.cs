using System.IO;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static class NativeCompositeExamples {
    internal static void Write(string output) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) foreach (var variant in new[] { "compact", "wide" }) {
            foreach (var operation in new[] { VisualCompositeOperator.SourceIn, VisualCompositeOperator.DestinationOut, VisualCompositeOperator.Xor }) {
                var composition = NativeCompositeCompositionExample.Create(variant, mode, operation);
                var image = composition.ToImage();
                var stem = Path.Combine(output, "native-composite-" + operation + "-" + variant + "-" + mode);
                File.WriteAllBytes(stem + ".png", composition.ToPng());
                var canvas = VisualCanvas.Create(image.Width, image.Height).WithBackdrop(VisualCanvasBackdropStyle.Transparent)
                    .AddImage(image, 0, 0, image.Width, image.Height);
                File.WriteAllText(stem + ".svg", canvas.ToSvg());
                File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitNativeCompositeComposition(variant, mode));
                File.WriteAllText(stem + ".source.json", NativeRetainedMaskExample.PrepareSource(variant, mode).ToArtifact().ToInterchangeJson());
            }
            var opacityStem = Path.Combine(output, "finite-composite-source-opacity-" + variant + "-" + mode);
            File.WriteAllText(opacityStem + ".svg", NativeCompositeSourceExample.SourceWithOpacity(variant, mode));
            File.WriteAllBytes(opacityStem + ".native.png", NativeCompositeSourceExample.RasterizeWithOpacity(variant, mode).Image.ToPng());
            File.WriteAllText(opacityStem + ".csharp.txt", ExampleRecipeSource.EmitNativeCompositeSource(variant, mode));
            foreach (var operation in new[] { "over", "in", "out", "atop", "xor" }) {
                var stem = Path.Combine(output, "finite-composite-source-" + operation + "-" + variant + "-" + mode);
                File.WriteAllText(stem + ".svg", NativeCompositeSourceExample.Source(variant, mode, operation));
                File.WriteAllBytes(stem + ".native.png", NativeCompositeSourceExample.Rasterize(variant, mode, operation).Image.ToPng());
                File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitNativeCompositeSource(variant, mode));
            }
        }
    }
}
