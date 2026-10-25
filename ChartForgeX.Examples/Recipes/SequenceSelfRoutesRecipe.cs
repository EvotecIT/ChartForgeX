/// <summary>Creates explicitly curved native self messages with measured badge and mirrored lifetime geometry.</summary>
public static class SequenceSelfRoutesRecipe {
    /// <summary>Creates a compact or wide native sequence; omitted self routing remains the ordinary 56/24 law.</summary>
    public static SequenceArtifact Create(string variant="wide",VisualThemeMode mode=VisualThemeMode.Light) {
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));
        var model=SequenceArtifact.Create("sequence-self-routes").WithSize(variant=="compact"?390:1000,820).WithTitle("Self routes")
            .AddParticipant("A","Review").AddMessage("A","A","Check").AddMessage("A","A","Save");
        model.Layout=new SequenceLayoutPolicy(participantMinimumWidth:64,participantMinimumHeight:60,participantSpacing:8,wrapPadding:8,selfMessageRouting:SequenceSelfMessageRouting.Curved);
        model.Typography=new SequenceTypography(participant:new TextStyleOverride{FontSize=14},message:new TextStyleOverride{FontSize=12});model.Theme=VisualTheme.Graphite();model.ThemeMode=mode;return model;
    }
}
