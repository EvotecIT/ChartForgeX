using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Typography;

internal static class NativeFallbackRadialExamples {
    internal static void Write(string output, string[] args, bool numericValueText = false) {
        var primaryPath = RequiredFont(args, "--primary-font"); var fallbackPath = RequiredFont(args, "--fallback-font");
        FontRegistry.Register("Example Primary", primaryPath); FontRegistry.Register("Example Fallback", fallbackPath);
        const string family = "Example Primary, Example Fallback"; Directory.CreateDirectory(output); var renderer = new HtmlInteractiveChartRenderer();
        foreach (var donut in new[] { false, true }) foreach (var width in new[] { 390, 760 }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            Chart Model(double reading) {
                var model = NativeRadialMotionRecipe.Create(donut, false, reading, width, mode).WithFontFamily(family).WithXLabels("Asia 世界", "Europe", "Americas");
                return numericValueText ? model.WithTitle(string.Empty).WithSubtitle(string.Empty).WithHeader(false).WithLegend(false).WithDonutCenterLabel(false)
                    .WithPieSliceLabelContent(ChartPieSliceLabelContent.Value).WithDataLabelPlacement(ChartDataLabelPlacement.Inside).WithValueFormat(ChartValueFormat.Compact()) : model;
            }
            var from = renderer.PrepareSection(Model(numericValueText ? 900 : 60)); var to = renderer.PrepareSection(Model(numericValueText ? 1100 : 80)); var options = new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 9);
            var forward = renderer.PrepareNativeTransitionJson(from, to, options); var reverse = renderer.PrepareNativeTransitionJson(to, from, options);
            var action = "const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));let forward=true;document.querySelector('#play').onclick=()=>{chart.update(forward?"
                + JsonSerializer.Serialize(to.Html) + ":" + JsonSerializer.Serialize(from.Html) + ",{sceneRevision:chart.sceneRevision,transition:forward?" + JsonSerializer.Serialize(forward) + ":" + JsonSerializer.Serialize(reverse) + ",motion:{duration:650}});forward=!forward;};";
            var html = "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><style>" + FontCss("Example Primary", primaryPath) + FontCss("Example Fallback", fallbackPath) + HtmlInteractiveChartRenderer.BuildFragmentStyle()
                + "</style></head><body><button id='play' type='button'>Present captured native captions</button>" + from.Html + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script><script>" + action + "</script></body></html>";
            var prefix = Path.Combine(output, (numericValueText ? "native-value-text-" : "native-fallback-") + (donut ? "donut" : "pie") + "-" + width + "-" + mode);
            File.WriteAllText(prefix + ".html", html); File.WriteAllText(prefix + ".transition.json", forward); File.WriteAllText(prefix + ".from.svg", from.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".from.png", from.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".to.svg", to.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".to.png", to.PreparedVisual.ToPng()); File.WriteAllText(prefix + ".program.cs", numericValueText ? ExampleRecipeSource.EmitNativeValueTextRepertoire(donut, width, mode) : ExampleRecipeSource.EmitNativeFallbackRadial(donut, width, mode));
        }
    }
    internal static string RequiredFont(string[] args, string name) { var index = Array.FindIndex(args, value => value.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException(name + " requires an explicit licensed .ttf/.otf file.");
            var fontPath = Path.GetFullPath(args[index + 1]); var extension = Path.GetExtension(fontPath);
            if (!extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".otf", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException(name + " accepts .ttf/.otf files in this example.");
            return fontPath; }
    internal static string FontCss(string name, string path) => "@font-face{font-family:'" + name + "';src:url(data:font/" + (Path.GetExtension(path).Equals(".otf", StringComparison.OrdinalIgnoreCase) ? "otf" : "ttf")
        + ";base64," + Convert.ToBase64String(File.ReadAllBytes(path)) + ") format('opentype')}";
}
