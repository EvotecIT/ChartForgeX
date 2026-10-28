using System.Text.Json;
using ChartForgeX.Interactivity.Html;

internal static class NativeCartesianValueExamples {
    internal static void Write(string output) {
        Directory.CreateDirectory(output); var renderer = new HtmlInteractiveChartRenderer();
        foreach (var bar in new[] { false, true }) foreach (var width in new[] { 390, 760 })
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) foreach (var initial in new[] { false, true }) {
            var from = renderer.PrepareSection(NativeCartesianValueRecipe.Create(bar, 20, width, mode)); var to = renderer.PrepareSection(NativeCartesianValueRecipe.Create(bar, 80, width, mode));
            var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 12));
            var action = "const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));document.querySelector('#play').onclick=()=>{";
            action += initial ? "chart.presentInitial(" + JsonSerializer.Serialize(payload) + ",{sceneRevision:chart.sceneRevision,motion:{duration:650}});"
                : "chart.update(" + JsonSerializer.Serialize(to.Html) + ",{sceneRevision:chart.sceneRevision,transition:" + JsonSerializer.Serialize(payload) + ",motion:{duration:650}});document.querySelector('#play').disabled=true;";
            action += "};";
            var html = "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
                + "</style></head><body><button id='play'>Show native values</button>" + (initial ? to.Html : from.Html)
                + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script><script>" + action + "</script></body></html>";
            var prefix = Path.Combine(output, "native-cartesian-" + (bar ? "bar" : "line") + "-" + width + "-" + mode + "-" + (initial ? "initial" : "update"));
            File.WriteAllText(prefix + ".html", html); File.WriteAllText(prefix + ".transition.json", payload);
            File.WriteAllText(prefix + ".from.svg", from.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".from.png", from.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".to.svg", to.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".to.png", to.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".program.cs", ExampleRecipeSource.EmitNativeCartesianValues(bar, width, mode, initial));
        }
    }
}
