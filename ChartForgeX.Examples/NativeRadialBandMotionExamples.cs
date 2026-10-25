using System.Text.Json;
using ChartForgeX.Interactivity.Html;

internal static class NativeRadialBandMotionExamples {
    internal static void Write(string output) {
        Directory.CreateDirectory(output); var renderer = new HtmlInteractiveChartRenderer();
        foreach (var row in NativeRadialBandMotionRecipe.Cases()) {
            var other = row.Intent == "other"; var chart = NativeRadialBandMotionRecipe.Create(row.Family,row.Automatic,other,60,45,row.Width,row.Mode);
            var from = other ? renderer.PrepareVisibilitySection(chart,NativeRadialBandMotionRecipe.View(false)) : renderer.PrepareSection(chart);
            var to = other ? renderer.PrepareVisibilitySection(chart,NativeRadialBandMotionRecipe.View(true)) : renderer.PrepareSection(NativeRadialBandMotionRecipe.Create(row.Family,row.Automatic,false,80,55,row.Width,row.Mode));
            var options = new HtmlNativeTransitionOptions(other ? HtmlNativeTransitionEffect.LegendVisibility : row.Intent == "initial" ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values,9);
            var payload = renderer.PrepareNativeTransitionJson(from,to,options); var reverse = row.Intent == "initial" ? payload : renderer.PrepareNativeTransitionJson(to,from,options);
            var action = "const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));let forward=true;document.querySelector('#play').onclick=()=>" + (row.Intent == "initial"
                ? "chart.presentInitial(" + JsonSerializer.Serialize(payload) + ",{sceneRevision:chart.sceneRevision,motion:{duration:650}});"
                : "{chart.update(forward?" + JsonSerializer.Serialize(to.Html) + ":" + JsonSerializer.Serialize(from.Html) + ",{sceneRevision:chart.sceneRevision,transition:forward?" + JsonSerializer.Serialize(payload) + ":" + JsonSerializer.Serialize(reverse) + ",motion:{duration:650}});forward=!forward;}");
            var html = "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
                + "</style></head><body><button id='play' type='button'>Present native radial bands</button>" + (row.Intent == "initial" ? to.Html : from.Html)
                + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script><script>" + action + "</script></body></html>";
            var name = Path.Combine(output,"native-radial-bands-" + row.Family + "-" + row.Automatic + "-" + row.Intent + "-" + row.Width + "-" + row.Mode);
            File.WriteAllText(name + ".html",html); File.WriteAllText(name + ".transition.json",payload); File.WriteAllText(name + ".from.svg",from.PreparedVisual.ToSvg()); File.WriteAllBytes(name + ".from.png",from.PreparedVisual.ToPng());
            File.WriteAllText(name + ".to.svg",to.PreparedVisual.ToSvg()); File.WriteAllBytes(name + ".to.png",to.PreparedVisual.ToPng()); File.WriteAllText(name + ".program.cs",ExampleRecipeSource.EmitNativeRadialBands(row.Family,row.Automatic,row.Intent,row.Width,row.Mode));
        }
    }
}
