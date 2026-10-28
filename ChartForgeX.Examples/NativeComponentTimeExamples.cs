using ChartForgeX.Interactivity.Html;
using ChartForgeX.Typography;

internal static class NativeComponentTimeExamples {
    internal static void Write(string output, string[] args) {
        var font = NativeFallbackRadialExamples.RequiredFont(args, "--primary-font"); FontRegistry.Register("Owned temporal font", font); Directory.CreateDirectory(output); var renderer = new HtmlInteractiveChartRenderer();
        (string Family, string Policy, bool Initial)[] cases = { ("candlestick", "elapsed", true), ("candlestick", "sessions", false), ("candlestick", "ordinal", false), ("candlestick", "calendar", false), ("ohlc", "sessions", true), ("bubble", "ordinal", false), ("range", "calendar", true), ("error", "elapsed", false) };
        foreach (var item in cases) foreach (var width in new[] { 390, 760 }) {
            var mode = width == 390 ? VisualThemeMode.Light : VisualThemeMode.Dark;
            void Configure(HtmlChartInteractionOptions options) { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-time-components"; }
            var from = renderer.PrepareSection(NativeComponentTimeRecipe.Create(item.Family, item.Policy, false, width, mode, "Owned temporal font"), configure: Configure);
            var to = renderer.PrepareSection(NativeComponentTimeRecipe.Create(item.Family, item.Policy, true, width, mode, "Owned temporal font"), configure: Configure);
            var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(item.Initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 5));
            var prefix = Path.Combine(output, "native-time-components-" + item.Family + "-" + item.Policy + "-" + item.Initial + "-" + width);
            File.WriteAllText(prefix + ".from.html", from.Html); File.WriteAllText(prefix + ".to.html", to.Html); File.WriteAllText(prefix + ".transition.json", payload);
            File.WriteAllText(prefix + ".from.svg", from.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".from.png", from.PreparedVisual.ToPng()); File.WriteAllText(prefix + ".to.svg", to.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".to.png", to.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".program.cs", ExampleRecipeSource.EmitNativeComponentTime(item.Family, item.Policy, item.Initial, width, mode));
        }
    }
}
