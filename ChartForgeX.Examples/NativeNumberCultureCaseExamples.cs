using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Typography;

internal static class NativeNumberCultureCaseExamples {
    internal static void Write(string output, string[] args, bool richer = false) {
        var primary = NativeFallbackRadialExamples.RequiredFont(args, "--primary-font"); var fallback = NativeFallbackRadialExamples.RequiredFont(args, "--fallback-font");
        FontRegistry.Register("Example Primary", primary); FontRegistry.Register("Example Fallback", fallback); Directory.CreateDirectory(output); var renderer = new HtmlInteractiveChartRenderer();
        foreach (var row in Cases(richer)) {
            var (kind,policy,width,mode) = row;
            Chart Model(double reading) => NativeNumberCultureCaseRecipe.Create(kind, policy, reading, width, mode, "Example Primary, Example Fallback");
            var readings = NativeNumberCultureCaseRecipe.Readings(policy); var from = renderer.PrepareSection(Model(readings.First)); var to = renderer.PrepareSection(Model(readings.Last));
            var options = new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 9); var forward = renderer.PrepareNativeTransitionJson(from, to, options); var reverse = renderer.PrepareNativeTransitionJson(to, from, options);
            var action = "const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));let forward=true;document.querySelector('#play').onclick=()=>{chart.update(forward?" + JsonSerializer.Serialize(to.Html) + ":" + JsonSerializer.Serialize(from.Html) + ",{sceneRevision:chart.sceneRevision,transition:forward?" + JsonSerializer.Serialize(forward) + ":" + JsonSerializer.Serialize(reverse) + ",motion:{duration:650}});forward=!forward;};";
            var html = "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><style>" + NativeFallbackRadialExamples.FontCss("Example Primary", primary) + NativeFallbackRadialExamples.FontCss("Example Fallback", fallback) + HtmlInteractiveChartRenderer.BuildFragmentStyle()
                + "</style></head><body><button id='play' type='button'>Present captured native numeric captions</button>" + from.Html + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script><script>" + action + "</script></body></html>";
            var prefix = Path.Combine(output, "native-number-culture-case-" + kind + "-" + policy + "-" + width + "-" + mode);
            File.WriteAllText(prefix + ".html", html); File.WriteAllText(prefix + ".transition.json", forward); File.WriteAllText(prefix + ".from.svg", from.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".from.png", from.PreparedVisual.ToPng()); File.WriteAllText(prefix + ".to.svg", to.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".to.png", to.PreparedVisual.ToPng()); File.WriteAllText(prefix + ".program.cs", ExampleRecipeSource.EmitNativeNumberCultureCase(kind, policy, width, mode));
        }
    }
    private static IEnumerable<(string Kind,string Policy,int Width,VisualThemeMode Mode)> Cases(bool richer) {
        if (richer) {
            yield return ("pie","literal-ms",390,VisualThemeMode.Light); yield return ("donut","sections",760,VisualThemeMode.Dark);
            yield return ("treemap","scaled",390,VisualThemeMode.Dark); yield return ("pie","permille",760,VisualThemeMode.Light);
            yield return ("donut","scientific",390,VisualThemeMode.Dark); yield return ("treemap","escaped",760,VisualThemeMode.Light);
            yield return ("donut","literal-ms",760,VisualThemeMode.Dark); yield return ("pie","sections",390,VisualThemeMode.Light); yield break;
        }
        foreach (var kind in new[] { "pie", "donut", "treemap" }) foreach (var policy in new[] { "fr-number", "sv-currency", "ar-percent", "fr-compact", "title", "sentence", "toggle" })
        foreach (var width in new[] { 390, 760 }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) yield return (kind,policy,width,mode);
    }
}
