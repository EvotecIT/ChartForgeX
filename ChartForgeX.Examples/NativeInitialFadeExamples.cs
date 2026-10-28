using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;

/// <summary>Whole-visual native initial presentation with data already accepted by the existing chart lifetime.</summary>
internal static class NativeInitialFadeExamples {
    internal static void Write(string output) {
        Directory.CreateDirectory(output); var renderer = new HtmlInteractiveChartRenderer();
        foreach (var kind in new[] { ChartSeriesKind.Line, ChartSeriesKind.Pie, ChartSeriesKind.Gauge, ChartSeriesKind.Sunburst })
        foreach (var width in new[] { 390, 760 }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var chart = (Chart)ExampleRecipes.ForKind(kind).Create(width == 390 ? "compact" : "wide", mode); chart.WithSize(width, 420);
            var final = renderer.PrepareSection(chart, configure: options => options.Interaction.ChartId = "native-initial-fade");
            var payload = renderer.PrepareNativeTransitionJson(final, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialFade, 12));
            var script = "const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));const play=()=>chart.presentInitial("
                + JsonSerializer.Serialize(payload) + ",{sceneRevision:chart.sceneRevision,motion:{duration:650}});document.querySelector('#replay').onclick=play;play();";
            var page = "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><style>"
                + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style></head><body><button id='replay' type='button'>Replay initial fade</button>" + final.Html
                + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script><script>" + script + "</script></body></html>";
            var prefix = Path.Combine(output, "native-initial-fade-" + kind + "-" + width + "-" + mode);
            File.WriteAllText(prefix + ".html", page); File.WriteAllText(prefix + ".final.html", final.Html); File.WriteAllText(prefix + ".transition.json", payload);
            File.WriteAllText(prefix + ".svg", final.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".png", final.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".program.cs", ExampleRecipeSource.EmitInitialFade(kind, width, mode));
        }
    }
}
