using ChartForgeX.Interactivity.Html;
using ChartForgeX.Typography;

internal static class NativeRangeComponentExamples {
    internal static void Write(string output, string[] args) {
        var font = NativeFallbackRadialExamples.RequiredFont(args, "--primary-font"); FontRegistry.Register("Owned interval font", font); Directory.CreateDirectory(output);
        var renderer = new HtmlInteractiveChartRenderer();
        foreach (var initial in new[] { false, true }) foreach (var area in new[] { false, true }) foreach (var width in new[] { 390, 760 }) {
            var mode = width == 390 ? VisualThemeMode.Light : VisualThemeMode.Dark;
            Chart Model(double lower, double upper) => NativeRangeComponentRecipe.Create(lower, upper, area, width, mode, "Owned interval font");
            void Configure(HtmlChartInteractionOptions options) { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-range-components"; }
            var from = renderer.PrepareSection(Model(10, 35), configure: Configure); var to = renderer.PrepareSection(Model(20, 60), configure: Configure);
            var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 5));
            var prefix = Path.Combine(output, "native-range-" + initial + "-" + area + "-" + width);
            var flash = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 5, ChartColor.FromHex("#FFC000")));
            File.WriteAllText(prefix + ".flash.transition.json", flash);
            File.WriteAllText(prefix + ".from.html", from.Html); File.WriteAllText(prefix + ".to.html", to.Html); File.WriteAllText(prefix + ".transition.json", payload);
            File.WriteAllText(prefix + ".from.svg", from.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".from.png", from.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".to.svg", to.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".to.png", to.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".program.cs", ExampleRecipeSource.EmitNativeRangeComponents(initial, area, width, mode));
        }
    }
}
