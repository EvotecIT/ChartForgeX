using ChartForgeX.Themes;

public static partial class V2Examples {
    private static void WriteSequenceSourceDefaults(string output,ICollection<ProofArtifact> artifacts) {
        var recipe=ExampleRecipes.Named("sequence-source-defaults");
        foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark})foreach(var variant in new[]{"compact","wide"}) {
            var compact=variant=="compact";WriteModel(output,artifacts,recipe.Create(variant,mode),"sequence-source-defaults-"+variant+"-"+mode.ToString().ToLowerInvariant(),
                "sequence","Sequence source defaults",variant,"Measured tabs and mirrored participants",mode,compact?390:1000,820,false,recipe,compact:compact);
        }
    }
}
