using ChartForgeX.Themes;
public static partial class V2Examples {
    private static void WriteSequencePresetAppearance(string output,ICollection<ProofArtifact> artifacts) {
        var recipe=ExampleRecipes.Named("sequence-preset-appearance");foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark})foreach(var variant in new[]{"compact","wide"}) {
            var compact=variant=="compact";WriteModel(output,artifacts,recipe.Create(variant,mode),"sequence-preset-appearance-"+variant+"-"+mode.ToString().ToLowerInvariant(),"sequence","Measured participant bands",variant,"Independent participant and activation paint with portable native edit facts",mode,compact?390:1100,860,false,recipe,new[]{"Sequence"},compact);
        }
    }
}
