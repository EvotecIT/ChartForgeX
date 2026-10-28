using ChartForgeX.Interactivity.Html;
using ChartForgeX.Typography;

internal static class NativeBubbleComponentExamples {
    internal static void Write(string output, string[] args) {
        var font = NativeFallbackRadialExamples.RequiredFont(args, "--primary-font"); FontRegistry.Register("Owned Bubble font", font); Directory.CreateDirectory(output);
        var renderer = new HtmlInteractiveChartRenderer();
        foreach (var initial in new[] { false, true }) foreach (var area in new[] { false, true }) foreach (var width in new[] { 390, 760 }) {
            var mode = width == 390 ? VisualThemeMode.Light : VisualThemeMode.Dark; var reversed = width == 760;
            Chart Model(double y, double size) => NativeBubbleComponentRecipe.Create(y, size, area, reversed, width, mode, "Owned Bubble font");
            void Configure(HtmlChartInteractionOptions options) { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-bubble-components"; }
            var from = renderer.PrepareSection(Model(30, 10), configure: Configure); var to = renderer.PrepareSection(Model(50, 70), configure: Configure);
            var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 5));
            var prefix = Path.Combine(output, "native-bubble-" + initial + "-" + area + "-" + width);
            File.WriteAllText(prefix + ".from.html", from.Html); File.WriteAllText(prefix + ".to.html", to.Html); File.WriteAllText(prefix + ".transition.json", payload);
            File.WriteAllText(prefix + ".from.svg", from.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".from.png", from.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".to.svg", to.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".to.png", to.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".program.cs", ExampleRecipeSource.EmitNativeBubbleComponents(initial, area, reversed, width, mode));
        }
    }
}
