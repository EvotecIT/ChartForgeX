using ChartForgeX.Interactivity.Html;
using ChartForgeX.Typography;

internal static class NativeComponentClipExamples {
    internal static void Write(string output, string[] args) {
        var font = NativeFallbackRadialExamples.RequiredFont(args, "--primary-font"); FontRegistry.Register("Owned clipped font", font); Directory.CreateDirectory(output); var renderer = new HtmlInteractiveChartRenderer();
        foreach (var family in new[] { "bubble", "range", "error", "candlestick", "ohlc" }) foreach (var width in new[] { 390, 760 }) {
            var mode = width == 390 ? VisualThemeMode.Light : VisualThemeMode.Dark;
            void Configure(HtmlChartInteractionOptions options) { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-clipped-components"; }
            var from = renderer.PrepareSection(NativeComponentClipRecipe.Create(family, false, width, mode, "Owned clipped font"), configure: Configure); var to = renderer.PrepareSection(NativeComponentClipRecipe.Create(family, true, width, mode, "Owned clipped font"), configure: Configure);
            var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 5)); var prefix = Path.Combine(output, "native-component-clip-" + family + "-" + width);
            File.WriteAllText(prefix + ".from.html", from.Html); File.WriteAllText(prefix + ".to.html", to.Html); File.WriteAllText(prefix + ".transition.json", payload); File.WriteAllText(prefix + ".from.svg", from.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".from.png", from.PreparedVisual.ToPng()); File.WriteAllText(prefix + ".to.svg", to.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".to.png", to.PreparedVisual.ToPng()); File.WriteAllText(prefix + ".program.cs", ExampleRecipeSource.EmitNativeComponentClip(family, width, mode));
        }
    }
}
