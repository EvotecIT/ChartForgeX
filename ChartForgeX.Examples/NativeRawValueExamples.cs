using ChartForgeX.Interactivity.Html;

internal static class NativeRawValueExamples {
    internal static void Write(string output) {
        Directory.CreateDirectory(output);var renderer=new HtmlInteractiveChartRenderer();
        foreach(var (donut,kind,width,mode) in Cases()) {
            var values=NativeRawValueRecipe.Readings(kind);Chart Model(double reading)=>NativeRawValueRecipe.Create(donut,kind,reading,width,mode);
            void Configure(HtmlChartInteractionOptions options){options.CaptureNativeValueSource=true;options.Interaction.ChartId="native-raw-values";}
            var from=kind=="source-reflow"?renderer.PrepareVisibilitySection(Model(values.First),NativeRawValueRecipe.View(false),configure:Configure):renderer.PrepareSection(Model(values.First),configure:Configure);
            var to=kind=="source-reflow"?renderer.PrepareVisibilitySection(Model(values.Last),NativeRawValueRecipe.View(true),configure:Configure):renderer.PrepareSection(Model(values.Last),configure:Configure);
            var effect=kind=="source-reflow"?HtmlNativeTransitionEffect.SourceReflow:kind=="initial"?HtmlNativeTransitionEffect.InitialValues:HtmlNativeTransitionEffect.Values;
            var payload=renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(effect,9));
            var prefix=Path.Combine(output,"native-raw-"+donut+"-"+kind+"-"+width+"-"+mode);
            File.WriteAllText(prefix+".from.html",from.Html);File.WriteAllText(prefix+".to.html",to.Html);File.WriteAllText(prefix+".transition.json",payload);
            File.WriteAllText(prefix+".from.svg",from.PreparedVisual.ToSvg());File.WriteAllBytes(prefix+".from.png",from.PreparedVisual.ToPng());File.WriteAllText(prefix+".to.svg",to.PreparedVisual.ToSvg());File.WriteAllBytes(prefix+".to.png",to.PreparedVisual.ToPng());File.WriteAllText(prefix+".program.cs",ExampleRecipeSource.EmitNativeRawValue(donut,kind,width,mode));
        }
    }
    private static IEnumerable<(bool,string,int,VisualThemeMode)> Cases(){yield return(false,"zero",390,VisualThemeMode.Light);yield return(true,"zero",760,VisualThemeMode.Dark);yield return(false,"empty",760,VisualThemeMode.Dark);yield return(true,"empty",390,VisualThemeMode.Light);yield return(false,"other",390,VisualThemeMode.Dark);yield return(true,"initial",760,VisualThemeMode.Light);yield return(false,"source-reflow",760,VisualThemeMode.Light);yield return(true,"source-reflow",390,VisualThemeMode.Dark);}
}
