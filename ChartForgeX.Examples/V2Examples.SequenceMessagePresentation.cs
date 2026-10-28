using ChartForgeX.Themes;

public static partial class V2Examples {
    private static void WriteSequenceMessagePresentation(string output,ICollection<ProofArtifact> artifacts) {
        var recipe=ExampleRecipes.Named("sequence-message-presentation");
        foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark})foreach(var variant in new[]{"compact","wide"}) {
            var compact=variant=="compact";WriteModel(output,artifacts,recipe.Create(variant,mode),"sequence-message-presentation-"+variant+"-"+mode.ToString().ToLowerInvariant(),
                "sequence","Message presentation",variant,"Aligned captions and measured occurrence numbers",mode,compact?390:1000,820,false,recipe,compact:compact);
        }
    }
}
