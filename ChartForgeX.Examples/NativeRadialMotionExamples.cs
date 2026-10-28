using System.Text.Json;
using ChartForgeX.Interactivity.Html;

internal static class NativeRadialMotionExamples {
    internal static void Write(string output) {
        Directory.CreateDirectory(output); var renderer = new HtmlInteractiveChartRenderer();
        foreach (var donut in new[] { false, true }) foreach (var kind in new[] { "values", "initial", "other" })
        foreach (var width in new[] { 390, 760 }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var other = kind == "other"; var chart = NativeRadialMotionRecipe.Create(donut, other, 60, width, mode);
            var from = other ? renderer.PrepareVisibilitySection(chart, NativeRadialMotionRecipe.View(false)) : renderer.PrepareSection(chart);
            var to = other ? renderer.PrepareVisibilitySection(chart, NativeRadialMotionRecipe.View(true)) : renderer.PrepareSection(NativeRadialMotionRecipe.Create(donut, false, 80, width, mode));
            var effect = other ? HtmlNativeTransitionEffect.LegendVisibility : kind == "initial" ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values;
            var options = new HtmlNativeTransitionOptions(effect, 9); var payload = renderer.PrepareNativeTransitionJson(from, to, options);
            var reverse = kind == "initial" ? payload : renderer.PrepareNativeTransitionJson(to, from, options);
            var action = "const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));let forward=true;document.querySelector('#play').onclick=()=>" + (kind == "initial"
                ? "chart.presentInitial(" + JsonSerializer.Serialize(payload) + ",{sceneRevision:chart.sceneRevision,motion:{duration:650}});"
                : "{chart.update(forward?" + JsonSerializer.Serialize(to.Html) + ":" + JsonSerializer.Serialize(from.Html) + ",{sceneRevision:chart.sceneRevision,transition:forward?" + JsonSerializer.Serialize(payload) + ":" + JsonSerializer.Serialize(reverse) + ",motion:{duration:650}});forward=!forward;}");
            var html = "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
                + "</style></head><body><button id='play' type='button'>Present native frame capture</button>" + (kind == "initial" ? to.Html : from.Html)
                + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script><script>" + action + "</script></body></html>";
            var prefix = Path.Combine(output, "native-radial-" + (donut ? "donut" : "pie") + "-" + kind + "-" + width + "-" + mode);
            File.WriteAllText(prefix + ".html", html); File.WriteAllText(prefix + ".transition.json", payload);
            File.WriteAllText(prefix + ".from.svg", from.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".from.png", from.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".to.svg", to.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".to.png", to.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".program.cs", ExampleRecipeSource.EmitNativeRadialMotion(donut, kind, width, mode));
        }
    }
}
