using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

/// <summary>Parses the canonical adapter-owned aligned-message and number-badge source.</summary>
public static class SequenceMessagePresentationSourceRecipe {
    /// <summary>Creates a natural scrollable native SVG/PNG artifact with complete source numbering facts.</summary>
    public static VisualArtifact Create(string source,string variant="wide",VisualThemeMode mode=VisualThemeMode.Light,string? fontFamily=null) {
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));
        var result=MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject,Presentation=new MermaidPresentationOptions{ThemeMode=mode,FontFamily=fontFamily},Sequence=new MermaidSequenceRenderOptions{Width=variant=="compact"?390:1000,Height=820}});
        if(result.HasErrors||result.Artifact==null)throw new InvalidOperationException(string.Join("; ",result.Diagnostics.Select(item=>item.Message)));
        result.Artifact.HtmlSizing=VisualArtifactHtmlSizing.PreserveSize;return result.Artifact;
    }
}
