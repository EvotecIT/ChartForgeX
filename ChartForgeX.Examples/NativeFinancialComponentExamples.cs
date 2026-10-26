using ChartForgeX.Interactivity.Html;
using ChartForgeX.Typography;

internal static class NativeFinancialComponentExamples {
    internal static void Write(string output, string[] args) {
        var font = NativeFallbackRadialExamples.RequiredFont(args, "--primary-font"); FontRegistry.Register("Owned financial font", font); Directory.CreateDirectory(output); var renderer = new HtmlInteractiveChartRenderer();
        foreach (var effect in new[] { HtmlNativeTransitionEffect.Values, HtmlNativeTransitionEffect.InitialValues, HtmlNativeTransitionEffect.ValueFlash }) foreach (var ohlc in new[] { false, true }) foreach (var width in new[] { 390, 760 }) {
            var mode = width == 390 ? VisualThemeMode.Light : VisualThemeMode.Dark;
            void Configure(HtmlChartInteractionOptions options) { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-financial-components"; }
            var from = renderer.PrepareSection(NativeFinancialComponentRecipe.Create(ohlc, false, width, mode, "Owned financial font"), configure: Configure);
            var to = renderer.PrepareSection(NativeFinancialComponentRecipe.Create(ohlc, true, width, mode, "Owned financial font"), configure: Configure);
            var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(effect, 5, effect == HtmlNativeTransitionEffect.ValueFlash ? ChartColor.Black : null));
            var prefix = Path.Combine(output, "native-financial-" + effect + "-" + ohlc + "-" + width);
            File.WriteAllText(prefix + ".from.html", from.Html); File.WriteAllText(prefix + ".to.html", to.Html); File.WriteAllText(prefix + ".transition.json", payload);
            File.WriteAllText(prefix + ".from.svg", from.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".from.png", from.PreparedVisual.ToPng()); File.WriteAllText(prefix + ".to.svg", to.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".to.png", to.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".program.cs", ExampleRecipeSource.EmitNativeFinancialComponents(ohlc, effect, width, mode));
        }
    }
}
