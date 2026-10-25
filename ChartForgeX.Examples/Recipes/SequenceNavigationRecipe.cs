/// <summary>Creates reusable native participant link facts; an optional HTML consumer decides how to present them.</summary>
public static class SequenceNavigationRecipe {
    /// <summary>Creates a compact or wide native sequence; SVG/PNG stays deterministic and menu-free.</summary>
    public static SequenceArtifact Create(string variant="wide",VisualThemeMode mode=VisualThemeMode.Light) {
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));var model=SequenceArtifact.Create("sequence-navigation").WithSize(variant=="compact"?390:1000,820).AddParticipant("A","Review").AddParticipant("B","Archive").AddMessage("A","B","Save");model.Layout=new SequenceLayoutPolicy(participantMinimumWidth:64,participantMinimumHeight:60,participantSpacing:8);model.Participants[0].NavigationLinks=new[]{new VisualNavigationLink("Guide","#guide"),new VisualNavigationLink("Status","#status")};model.ThemeMode=mode;return model;
    }
}
