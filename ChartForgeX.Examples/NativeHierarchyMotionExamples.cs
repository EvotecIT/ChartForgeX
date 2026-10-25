using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Typography;

internal static class NativeHierarchyMotionExamples {
    internal static void Write(string output, string[] args) {
        var fontIndex = Array.FindIndex(args, value => value.Equals("--primary-font", StringComparison.OrdinalIgnoreCase));
        if (fontIndex < 0 || fontIndex + 1 >= args.Length) throw new ArgumentException("--primary-font requires an explicit licensed .ttf/.otf file.");
        var fontPath = Path.GetFullPath(args[fontIndex + 1]); var extension = Path.GetExtension(fontPath);
        if (!extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".otf", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Supply a .ttf/.otf face.");
        FontRegistry.Register("Example Native", fontPath); Directory.CreateDirectory(output); var renderer = new HtmlInteractiveChartRenderer();
        foreach (var family in new[] { "treemap", "sunburst-leaf", "sunburst-total" }) foreach (var intent in family == "treemap" ? new[] { "values", "initial", "subtree", "root", "ellipsis" } : new[] { "values", "initial", "subtree", "root" })
        foreach (var width in new[] { 390, 760 }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            Chart Model(double reading) {
                var chart = NativeHierarchyMotionRecipe.Create(family, reading, width, mode, "Example Native");
                if (intent == "ellipsis") { chart.Series[0].WithPointLabel(0, string.Concat(Enumerable.Repeat("Regional reconciliation ", 12)));
                    chart.ConfigureTreemap(captionOptions => { captionOptions.LabelTrimming = TextTrimming.Ellipsis; captionOptions.MinimumLabelFontSize = 10; }); }
                return chart;
            }
            var membership = intent is "subtree" or "root"; var initial = intent == "initial";
            var from = membership ? renderer.PrepareVisibilitySection(Model(30), NativeHierarchyMotionRecipe.View()) : renderer.PrepareHierarchySection(Model(initial ? 0 : 30));
            var to = membership ? renderer.PrepareVisibilitySection(Model(30), NativeHierarchyMotionRecipe.View(intent == "subtree", intent == "root")) : renderer.PrepareHierarchySection(Model(40));
            var options = new HtmlNativeTransitionOptions(membership ? HtmlNativeTransitionEffect.LegendVisibility : initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 9);
            var forward = renderer.PrepareNativeTransitionJson(from, to, options); var reverse = initial ? forward : renderer.PrepareNativeTransitionJson(to, from, options);
            var action = "const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));let forward=true;document.querySelector('#play').onclick=()=>" + (initial
                ? "chart.presentInitial(" + JsonSerializer.Serialize(forward) + ",{sceneRevision:chart.sceneRevision,motion:{duration:650}});"
                : "{chart.update(forward?" + JsonSerializer.Serialize(to.Html) + ":" + JsonSerializer.Serialize(from.Html) + ",{sceneRevision:chart.sceneRevision,transition:forward?" + JsonSerializer.Serialize(forward) + ":" + JsonSerializer.Serialize(reverse) + ",motion:{duration:650}});forward=!forward;}");
            var html = "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><style>" + NativeFallbackRadialExamples.FontCss("Example Native", fontPath) + HtmlInteractiveChartRenderer.BuildFragmentStyle()
                + "</style></head><body><button id='play' type='button'>Present native hierarchy frames</button>" + (initial ? to.Html : from.Html) + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script><script>" + action + "</script></body></html>";
            var prefix = Path.Combine(output, "native-hierarchy-" + family + "-" + intent + "-" + width + "-" + mode);
            File.WriteAllText(prefix + ".html", html); File.WriteAllText(prefix + ".transition.json", forward); File.WriteAllText(prefix + ".from.svg", from.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".from.png", from.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".to.svg", to.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".to.png", to.PreparedVisual.ToPng()); File.WriteAllText(prefix + ".program.cs", ExampleRecipeSource.EmitNativeHierarchyMotion(family, intent, width, mode));
        }
    }
}
