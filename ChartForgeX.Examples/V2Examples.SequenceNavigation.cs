using ChartForgeX.Themes;

public static partial class V2Examples {
    private static void WriteSequenceNavigation(string output,ICollection<ProofArtifact> artifacts) {
        var recipe=ExampleRecipes.Named("sequence-navigation");
        foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark})foreach(var variant in new[]{"compact","wide"}) {
            var compact=variant=="compact";WriteModel(output,artifacts,recipe.Create(variant,mode),"sequence-navigation-"+variant+"-"+mode.ToString().ToLowerInvariant(),
                "sequence","Participant navigation",variant,"Detached participant links",mode,compact?390:1000,820,false,recipe,compact:compact);
        }
    }
}
