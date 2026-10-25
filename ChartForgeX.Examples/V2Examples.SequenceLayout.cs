using ChartForgeX.Themes;

public static partial class V2Examples {
    private static void WriteSequenceLayoutTypography(string output,ICollection<ProofArtifact> artifacts) {
        var recipe=ExampleRecipes.Named("sequence-layout-typography");
        foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark})foreach(var variant in new[]{"compact","wide"}) {
            var compact=variant=="compact";
            WriteModel(output,artifacts,recipe.Create(variant,mode),"sequence-layout-typography-"+variant+"-"+mode.ToString().ToLowerInvariant(),
                "sequence","Measured review sequence",variant,"Independent native role typography and spacing",mode,compact?390:1000,620,false,recipe,compact:compact);
        }
    }
}
