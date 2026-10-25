using ChartForgeX.Themes;

public static partial class V2Examples {
    private static void WriteSequenceSelfRoutes(string output,ICollection<ProofArtifact> artifacts) {
        var recipe=ExampleRecipes.Named("sequence-self-routes");
        foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark})foreach(var variant in new[]{"compact","wide"}) {
            var compact=variant=="compact";WriteModel(output,artifacts,recipe.Create(variant,mode),"sequence-self-routes-"+variant+"-"+mode.ToString().ToLowerInvariant(),
                "sequence","Self routes",variant,"Finite native self routes",mode,compact?390:1000,820,false,recipe,compact:compact);
        }
    }
}
