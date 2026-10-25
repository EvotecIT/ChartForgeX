using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

/// <summary>Parses the shared sequence source fixture through the ordinary Mermaid adapter.</summary>
public static class SequenceLayoutTypographySourceRecipe {
    /// <summary>Creates natural source output in the selected theme, retaining scrollable intrinsic dimensions.</summary>
    public static VisualArtifact Create(string source,string variant="wide",VisualThemeMode mode=VisualThemeMode.Light,string? fontFamily=null) {
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));
        var result=MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject,
            Presentation=new MermaidPresentationOptions{ThemeMode=mode,FontFamily=fontFamily},
            Sequence=new MermaidSequenceRenderOptions{Width=variant=="compact"?390:1000,Height=620}});
        if(result.HasErrors||result.Artifact==null)throw new InvalidOperationException(string.Join("; ",result.Diagnostics.Select(item=>item.Message)));
        result.Artifact.HtmlSizing=VisualArtifactHtmlSizing.PreserveSize;
        return result.Artifact;
    }
}
