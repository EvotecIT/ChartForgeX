using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

internal static class NativeOwnedValueFlashExamples {
    internal static void Write(string output,string[] args) {
        var font=NativeFallbackRadialExamples.RequiredFont(args,"--primary-font");FontRegistry.Register("Owned flash font",font);Directory.CreateDirectory(output);var renderer=new HtmlInteractiveChartRenderer();
        foreach(var (kind,width,mode) in Cases()) {
            var raw=kind.StartsWith("raw-",StringComparison.Ordinal);Chart Model(double value)=>NativeOwnedValueFlashRecipe.Create(kind,value,width,mode,"Owned flash font");
            void Configure(HtmlChartInteractionOptions options){options.CaptureNativeValueSource=raw;options.Interaction.ChartId="native-owned-flash";}
            var from=renderer.PrepareSection(Model(raw?60:30),configure:Configure);var to=renderer.PrepareSection(Model(raw?5:40),configure:Configure);var payload=renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash,5,ChartColor.FromHex("#FFC000")));
            var prefix=Path.Combine(output,"native-owned-flash-"+kind+"-"+width+"-"+mode);File.WriteAllText(prefix+".from.html",from.Html);File.WriteAllText(prefix+".to.html",to.Html);File.WriteAllText(prefix+".transition.json",payload);File.WriteAllText(prefix+".from.svg",from.PreparedVisual.ToSvg());File.WriteAllBytes(prefix+".from.png",from.PreparedVisual.ToPng());File.WriteAllText(prefix+".to.svg",to.PreparedVisual.ToSvg());File.WriteAllBytes(prefix+".to.png",to.PreparedVisual.ToPng());File.WriteAllText(prefix+".program.cs",ExampleRecipeSource.EmitNativeOwnedValueFlash(kind,width,mode));
        }
    }
    private static IEnumerable<(string,int,VisualThemeMode)> Cases(){yield return("line",390,VisualThemeMode.Light);yield return("bar-gradient",760,VisualThemeMode.Dark);yield return("bar-capsule",390,VisualThemeMode.Dark);yield return("pie",760,VisualThemeMode.Light);yield return("raw-pie",390,VisualThemeMode.Light);yield return("raw-donut",760,VisualThemeMode.Dark);yield return("treemap",390,VisualThemeMode.Dark);yield return("sunburst",760,VisualThemeMode.Light);}
}
