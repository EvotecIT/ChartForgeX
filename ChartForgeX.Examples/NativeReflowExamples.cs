using System.Text.Json;
using ChartForgeX.Interactivity.Html;

internal static class NativeReflowExamples {
    internal static void Write(string output) {
        Directory.CreateDirectory(output); var renderer = new HtmlInteractiveChartRenderer();
        foreach (var kind in new[] { "line", "bar", "boxes", "raw" }) foreach (var width in new[] { 390, 760 })
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var chart = NativeReflowRecipe.Create(kind, width, mode); var from = renderer.PrepareVisibilitySection(chart, NativeReflowRecipe.View(kind, false));
            var to = renderer.PrepareVisibilitySection(chart, NativeReflowRecipe.View(kind, true)); var options = new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 9);
            var forward = renderer.PrepareNativeTransitionJson(from, to, options); var reverse = renderer.PrepareNativeTransitionJson(to, from, options);
            var action = "const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));let hidden=false;document.querySelector('#play').onclick=()=>{hidden=!hidden;chart.update(hidden?"
                + JsonSerializer.Serialize(to.Html) + ":" + JsonSerializer.Serialize(from.Html) + ",{sceneRevision:chart.sceneRevision,transition:hidden?" + JsonSerializer.Serialize(forward) + ":" + JsonSerializer.Serialize(reverse) + ",motion:{duration:650}})};";
            var page = "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
                + "</style></head><body><button id='play' type='button'>Toggle current observation or group</button>" + from.Html + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script><script>" + action + "</script></body></html>";
            var prefix = Path.Combine(output, "native-reflow-" + kind + "-" + width + "-" + mode); File.WriteAllText(prefix + ".html", page); File.WriteAllText(prefix + ".transition.json", forward);
            File.WriteAllText(prefix + ".from.svg", from.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".from.png", from.PreparedVisual.ToPng()); File.WriteAllText(prefix + ".to.svg", to.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".to.png", to.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".program.cs", ExampleRecipeSource.EmitNativeReflow(kind, width, mode));
        }
    }
}
