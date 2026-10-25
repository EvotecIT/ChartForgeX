using ChartForgeX.Themes;

public static partial class V2Examples {
    private static void WriteSequenceParticipantViews(string output,ICollection<ProofArtifact> artifacts) {
        var recipe=ExampleRecipes.Named("sequence-participant-views");
        foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark})foreach(var variant in new[]{"compact","wide"}) {
            var compact=variant=="compact";WriteModel(output,artifacts,recipe.Create(variant,mode),"sequence-participant-views-"+variant+"-"+mode.ToString().ToLowerInvariant(),
                "sequence","Participant views",variant,"Retained source records and mirrored lifetime footers",mode,compact?390:1000,820,false,recipe,compact:compact);
        }
    }
}
