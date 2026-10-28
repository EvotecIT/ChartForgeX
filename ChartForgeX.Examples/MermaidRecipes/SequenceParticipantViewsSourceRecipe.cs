using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

/// <summary>Parses one canonical adapter-owned source into the native participant view policy.</summary>
public static class SequenceParticipantViewsSourceRecipe {
    /// <summary>Creates a natural scrollable diagram, retaining all source participants in typed portable facts.</summary>
    public static VisualArtifact Create(string source,string variant="wide",VisualThemeMode mode=VisualThemeMode.Light,string? fontFamily=null) {
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));
        var result=MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject,
            Presentation=new MermaidPresentationOptions{ThemeMode=mode,FontFamily=fontFamily},Sequence=new MermaidSequenceRenderOptions{Width=variant=="compact"?390:1000,Height=820}});
        if(result.HasErrors||result.Artifact==null)throw new InvalidOperationException(string.Join("; ",result.Diagnostics.Select(item=>item.Message)));
        result.Artifact.HtmlSizing=VisualArtifactHtmlSizing.PreserveSize;return result.Artifact;
    }
}
