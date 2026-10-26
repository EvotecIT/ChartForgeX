using ChartForgeX.Interactivity.Html;
using ChartForgeX.Typography;

internal static class NativeErrorComponentExamples {
    internal static void Write(string output, string[] args) {
        var font = NativeFallbackRadialExamples.RequiredFont(args, "--primary-font"); FontRegistry.Register("Owned uncertainty font", font); Directory.CreateDirectory(output); var renderer = new HtmlInteractiveChartRenderer();
        foreach (var effect in new[] { HtmlNativeTransitionEffect.Values, HtmlNativeTransitionEffect.InitialValues, HtmlNativeTransitionEffect.ValueFlash }) foreach (var form in new[] { "x", "y", "xy" }) foreach (var width in new[] { 390, 760 }) {
            var mode = width == 390 ? VisualThemeMode.Light : VisualThemeMode.Dark;
            void Configure(HtmlChartInteractionOptions options) { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-error-components"; }
            var from = renderer.PrepareSection(NativeErrorComponentRecipe.Create(form, false, width, mode, "Owned uncertainty font"), configure: Configure);
            var to = renderer.PrepareSection(NativeErrorComponentRecipe.Create(form, true, width, mode, "Owned uncertainty font"), configure: Configure);
            var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(effect, 5, effect == HtmlNativeTransitionEffect.ValueFlash ? ChartColor.Black : null));
            var prefix = Path.Combine(output, "native-error-" + effect + "-" + form + "-" + width);
            File.WriteAllText(prefix + ".from.html", from.Html); File.WriteAllText(prefix + ".to.html", to.Html); File.WriteAllText(prefix + ".transition.json", payload);
            File.WriteAllText(prefix + ".from.svg", from.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".from.png", from.PreparedVisual.ToPng()); File.WriteAllText(prefix + ".to.svg", to.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".to.png", to.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".program.cs", ExampleRecipeSource.EmitNativeErrorComponents(form, effect, width, mode));
        }
    }
}
